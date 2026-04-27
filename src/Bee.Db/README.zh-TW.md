# Bee.Db

> 資料庫抽象層，提供動態 SQL 生成、參數化查詢、多資料庫支援，以及基於 IL 的物件映射。

[English](README.md)

## 架構定位

- **層級**：資料存取層（基礎設施）
- **下游**（依賴此套件）：`Bee.Repository`
- **上游**（此套件依賴）：`Bee.Definition`

## 目標框架

- `net10.0` -- 使用現代執行階段 API 與效能改進

## 主要功能

### 資料庫存取

- `DbAccess` -- 主要進入點，執行查詢、批次命令與 DataTable 更新
- `DbConnectionScope` -- 限定範圍的連線生命週期管理
- `DbCommandSpec` -- 參數化命令規格，支援位置型（`{0}`）與具名（`{Name}`）佔位符自動轉換
- `DbBatchSpec` -- 批次執行，可選擇性包裹交易並設定隔離等級

### 連線與提供者管理

- `DbConnectionManager` -- 集中式連線資訊註冊
- `DbProviderManager` -- 資料庫提供者工廠解析
- `DbConnectionInfo` -- 連線中繼資料（連線字串、資料庫類型、提供者）

### 查詢組合

> Bee.Db 採用 **FormMap** 模式：以 `FormSchema` 為單位描述業務實體，由查詢上下文沿 `FormSchema` 鏈遞迴展開 JOIN，產生與 ORM 不同的「表單級關聯」資料存取體驗。詳見 [FormMap 設計文件](../../docs/formmap.zh-TW.md)。

- `SelectCommandBuilder` -- 根據 `FormSchema` 定義建構 SELECT 命令
- `ISelectBuilder` / `IFromBuilder` / `IWhereBuilder` / `ISortBuilder` -- 可組合的建構器介面，分別負責 SELECT、FROM、WHERE 與 ORDER BY 子句
- `SelectContext` -- 查詢上下文，追蹤欄位映射與資料表聯結
- `WhereBuilder` -- 篩選條件轉 SQL，產出參數化結果

### 多資料庫支援

框架透過 dialect factory 層依 `DatabaseType` 路由 SQL 生成與結構描述讀取：

- `IDialectFactory` -- 每個 provider 的工廠，提供 `IFormCommandBuilder`、`ICreateTableCommandBuilder`、`ITableAlterCommandBuilder`、`ITableRebuildCommandBuilder`、`ITableSchemaProvider` 與 `GetDefaultValueExpression(FieldDbType)`
- `DbDialectRegistry` -- 將 `DatabaseType` 映射到對應的 `IDialectFactory`（與 `DbProviderManager` 映射 ADO.NET `DbProviderFactory` 對稱）；註冊由 host 應用程式明示完成
- `DbFunc` -- 資料庫感知工具方法（依 `DatabaseType` 索引參數前綴、識別符引號、型別推斷）
- 內建 dialect 實作：
  - **SQL Server**（`Providers/SqlServer/`）-- 完整支援：表單 SELECT / INSERT / UPDATE / DELETE、CREATE/ALTER/REBUILD DDL、結構描述探查
  - **PostgreSQL**（`Providers/PostgreSql/`）-- 完整支援：表單 SELECT / INSERT / UPDATE / DELETE、CREATE/ALTER/REBUILD DDL、透過 `information_schema` + `pg_catalog` 進行結構描述探查
  - MySQL / SQLite / Oracle -- 參數前綴與識別符引號已預先註冊在 `DbFunc`，連線層可直接運作；SQL 生成類別尚未實作

`Bee.Db` 本身**不引用任何 ADO.NET driver**，driver 由 host 應用程式自行引用。

### 提供者註冊

Host 應用程式在啟動時註冊兩件事：ADO.NET 的 `DbProviderFactory`（用於建立連線）與 `IDialectFactory`（用於 SQL 生成）。要啟用哪些 DB 完全由 host 決定。

```csharp
using Bee.Db.Manager;
using Bee.Db.Providers.SqlServer;
using Bee.Db.Providers.PostgreSql;
using Bee.Definition;
using Microsoft.Data.SqlClient;
using Npgsql;

// SQL Server
DbProviderManager.RegisterProvider(DatabaseType.SQLServer, SqlClientFactory.Instance);
DbDialectRegistry.Register(DatabaseType.SQLServer, new SqlDialectFactory());

// PostgreSQL
DbProviderManager.RegisterProvider(DatabaseType.PostgreSQL, NpgsqlFactory.Instance);
DbDialectRegistry.Register(DatabaseType.PostgreSQL, new PgDialectFactory());

// 在 DatabaseSettings 中設定每筆 DatabaseItem（通常從 XML 載入）；
// 每筆指定其 DatabaseType 與對應 ConnectionString。
```

`DatabaseItem` 帶有 `Id`、`DatabaseType`、`ConnectionString`。框架在建立 `DbAccess` / `TableSchemaBuilder` / `TableUpgradeOrchestrator` 時，會依該 `Id` 對應到的 `DatabaseType` 解析註冊好的 provider 與 dialect。PostgreSQL 連線字串範本：

```
Host=localhost;Port=5432;Database={@DbName};Username={@UserId};Password={@Password}
```

### 結構描述探查與升級

- `ITableSchemaProvider` -- 各 provider 的結構描述讀取器（SQL Server 使用 `sys.*`，PostgreSQL 使用 `information_schema` + `pg_catalog`）
- `TableSchemaBuilder` -- 比對定義結構與即時資料庫，產生或執行升級命令
- `TableSchemaComparer` -- 結構化差異（`TableSchemaDiff`），列出 add/alter/drop 變更
- `TableUpgradeOrchestrator` -- 預設走 ALTER 升級，必要時 fallback 到 rebuild；透過 dialect factory 路由
- `ITableAlterCommandBuilder` / `ITableRebuildCommandBuilder` -- 各 provider 的 DDL 產生（in-place ALTER 與整表重建）
- `TableSchemaCommandBuilder` -- 根據 `TableSchema` 產生 IUD 命令

### IL 物件映射

- `ILMapper<T>` -- 透過 IL emit 實現高效能 `DbDataReader` 至物件映射
- 自動欄位對屬性比對（不區分大小寫）
- 以 `ConcurrentDictionary` 依查詢結構快取委派
- 支援 `List<T>` 與 `IEnumerable<T>`（延遲）具體化

### 記錄與診斷

- `DbAccessLogger` -- 命令執行記錄
- `DbLogContext` -- 慢查詢追蹤與診斷上下文

## 主要公開 API

| 類別 / 介面 | 用途 |
|-------------|------|
| `DbAccess` | 執行查詢、批次命令與 DataTable 更新 |
| `DbCommandSpec` | 參數化命令規格，佔位符自動轉換 |
| `DbBatchSpec` | 批次命令執行與交易支援 |
| `SelectCommandBuilder` | 以 FormSchema 驅動的 SELECT 命令建構 |
| `IDialectFactory` | 各 provider 的 SQL／結構描述建構器工廠（SQL Server、PostgreSQL） |
| `IFormCommandBuilder` | 提供者專屬 CRUD 產生介面 |
| `ITableSchemaProvider` | 各 provider 的即時資料庫結構描述讀取器 |
| `DbDialectRegistry` | `DatabaseType` → `IDialectFactory` 註冊中心 |
| `DbConnectionManager` | 連線資訊註冊中心 |
| `DbProviderManager` | ADO.NET `DbProviderFactory` 解析 |
| `ILMapper<T>` | 基於 IL emit 的 DataReader 至物件映射 |
| `DbFunc` | 資料庫感知工具方法 |
| `TableSchemaCommandBuilder` | 依結構描述產生 IUD 命令 |

## 設計慣例

- **Builder Pattern** -- 透過 `ISelectBuilder`、`IFromBuilder`、`IWhereBuilder`、`ISortBuilder` 介面組合查詢，各自負責單一 SQL 子句。
- **Specification Pattern** -- `DbCommandSpec`、`DbBatchSpec`、`DataTableUpdateSpec` 將執行意圖封裝為資料，解耦命令定義與執行。
- **IL Emit 映射** -- `ILMapper<T>` 於執行階段產生 `DynamicMethod` 委派，實現零反射 DataReader 映射；委派依查詢結構快取。
- **佔位符自動轉換** -- `DbCommandSpec` 接受位置型（`{0}`、`{1}`）與具名（`{Name}`）佔位符，自動轉換為提供者專屬參數語法（`@p0`、`:p0`）。
- **Provider Pattern** -- 資料庫專屬行為（引號、參數前綴、DDL、結構描述探查）隔離於提供者介面之後，路由集中於 `DbDialectRegistry`。Host 應用程式只註冊實際會用到的 dialect；`Bee.Db` 不會自動註冊任何 dialect。
- 啟用 **Nullable Reference Types**（`<Nullable>enable</Nullable>`）。

## 目錄結構

```
Bee.Db/
  DbAccess/        # DbAccess、DbCommandSpec、DbBatchSpec、DbConnectionScope、
                   # DbCommandResult、DbParameterSpec、DataTableUpdateSpec
  Manager/         # DbConnectionManager、DbProviderManager、DbConnectionInfo、
                   # DbDialectRegistry
  Providers/       # IDialectFactory、IFormCommandBuilder、ICreateTableCommandBuilder、
                   # ITableAlterCommandBuilder、ITableRebuildCommandBuilder、
                   # ITableSchemaProvider、SelectCommandBuilder
    SqlServer/     # SQL Server 專屬實作
    PostgreSql/    # PostgreSQL 專屬實作
  Query/           # 查詢元件建構器
    Context/       # SelectContext、QueryFieldMapping、TableJoin
    From/          # IFromBuilder、FromBuilder
    Select/        # ISelectBuilder、SelectBuilder
    Sort/          # ISortBuilder、SortBuilder
    Where/         # IWhereBuilder、WhereBuilder、IParameterCollector
  Schema/          # TableSchemaBuilder、TableSchemaComparer
  Logging/         # DbAccessLogger、DbLogContext
  *.cs (root)      # DbFunc、ILMapper、DbCommandKind、JoinType、
                   # CommandTextVariable、TableSchemaCommandBuilder
```
