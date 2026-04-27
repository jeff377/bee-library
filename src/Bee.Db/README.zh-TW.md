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
  - **SQLite**（`Providers/Sqlite/`）-- 完整支援：表單 SELECT / INSERT / UPDATE / DELETE、CREATE DDL、ALTER（限 ADD / RENAME COLUMN / Index）、其餘欄位修改一律走 REBUILD、透過 `sqlite_master` + `PRAGMA` 進行結構描述探查；定位於檔案式單機與嵌入式情境，請見下方限制清單
  - MySQL / Oracle -- 參數前綴與識別符引號已預先註冊在 `DbFunc`，連線層可直接運作；SQL 生成類別尚未實作

#### SQLite 已知限制

下列為 SQLite 引擎本身或 `Microsoft.Data.Sqlite` driver 的能力差異，框架已沿著對應路徑做出有意決策：

- **ALTER TABLE 嚴重受限**：SQLite 僅支援 `ADD COLUMN` / `RENAME COLUMN`（3.25+）/ `DROP COLUMN`（3.35+）/ `RENAME TO`，無法變更欄位型別、nullability、default 或 PK。所有 `AlterFieldChange` 一律走 rebuild 路徑（drop / create temp / copy / drop old / rename）。
- **AutoIncrement 必須內聯為 PK**：`INTEGER PRIMARY KEY AUTOINCREMENT` 必須直接寫在欄位定義裡，不能透過外部 `CONSTRAINT pk_xxx PRIMARY KEY (...)` 達成。`SqliteCreateTableCommandBuilder` 自動內聯，並偵測「AutoIncrement 欄位 + PK 指向其他欄位」這種衝突的 schema 並 throw `InvalidOperationException`。
- **無 `COMMENT ON`**：SQLite 不持久化 `DisplayName` / `Caption`；`SqliteCreateTableCommandBuilder` silent no-op，`SqliteTableSchemaProvider` 讀回時這兩個欄位永遠為空字串。應用層應從 FormSchema XML 讀取 captions。
- **TYPE AFFINITY 而非嚴格型別**：宣告型別字串如 `VARCHAR(50)` / `NUMERIC(18,2)` 仍照寫，SQLite 依 affinity 規則對應。`SqliteTableSchemaProvider` 從 `PRAGMA table_info` 反向解析。
- **沒有 schema 概念**：所有表在 `main` 資料庫，identifier 直接 unqualified（仍會用 `"..."` quote）。
- **`UpdateDataTable` 不可用**：`Microsoft.Data.Sqlite.SqliteFactory` 不提供 `DbDataAdapter` 實作，因此基於 `DbDataAdapter.Update()` 的批次回寫 API（`DbAccess.UpdateDataTable`）無法在 SQLite 上執行。讀取（`Execute(...)` 回 `DataTable`）已透過 `DbDataReader` + `DataTable.Load` fallback 支援。
- **PK 索引名稱**：SQLite 自動建的 PK 索引是 `sqlite_autoindex_*`；`SqliteTableSchemaProvider` 將其正規化為框架慣例 `pk_{table}` 以利 `TableSchemaComparer` 比對。
- **驅動套件**：使用 [`Microsoft.Data.Sqlite`](https://learn.microsoft.com/dotnet/standard/data/sqlite/)；連線字串建議採 in-memory shared cache `Data Source=file:bee_test_sqlite?mode=memory&cache=shared` 用於測試，或 `Data Source={path}.db` 用於檔案式部署。

`Bee.Db` 本身**不引用任何 ADO.NET driver**，driver 由 host 應用程式自行引用。

### 提供者註冊

Host 應用程式在啟動時註冊兩件事：ADO.NET 的 `DbProviderFactory`（用於建立連線）與 `IDialectFactory`（用於 SQL 生成）。要啟用哪些 DB 完全由 host 決定。

```csharp
using Bee.Db.Manager;
using Bee.Db.Providers.PostgreSql;
using Bee.Db.Providers.Sqlite;
using Bee.Db.Providers.SqlServer;
using Bee.Definition;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Npgsql;

// SQL Server
DbProviderManager.RegisterProvider(DatabaseType.SQLServer, SqlClientFactory.Instance);
DbDialectRegistry.Register(DatabaseType.SQLServer, new SqlDialectFactory());

// PostgreSQL
DbProviderManager.RegisterProvider(DatabaseType.PostgreSQL, NpgsqlFactory.Instance);
DbDialectRegistry.Register(DatabaseType.PostgreSQL, new PgDialectFactory());

// SQLite
DbProviderManager.RegisterProvider(DatabaseType.SQLite, SqliteFactory.Instance);
DbDialectRegistry.Register(DatabaseType.SQLite, new SqliteDialectFactory());

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
  Ddl/             # DDL 字串產生契約：
                   # ICreateTableCommandBuilder、ITableAlterCommandBuilder、
                   # ITableRebuildCommandBuilder
  Dml/             # DML 字串產生契約與構件：
                   # IFormCommandBuilder，
                   # SelectCommandBuilder / InsertCommandBuilder /
                   # UpdateCommandBuilder / DeleteCommandBuilder，
                   # ISelectBuilder/SelectBuilder、IFromBuilder/FromBuilder、
                   # IWhereBuilder/WhereBuilder/InternalWhereBuilder/WhereBuildResult、
                   # ISortBuilder/SortBuilder、
                   # SelectContext、SelectContextBuilder、
                   # QueryFieldMapping、QueryFieldMappingCollection、
                   # TableJoin、TableJoinCollection、
                   # IParameterCollector、DefaultParameterCollector、
                   # TableSchemaCommandBuilder、JoinType
  Schema/          # TableSchema 模型 + 比對 + 升級流程（不產 SQL）：
                   # TableSchemaBuilder、TableSchemaComparer、TableSchemaDiff、
                   # TableUpgradeOrchestrator、UpgradePlan、UpgradeStage、
                   # UpgradeStageKind、UpgradeOptions、UpgradeExecutionMode、
                   # ChangeExecutionKind、DescriptionLevel、DescriptionChange、
                   # ITableSchemaProvider（live-DB schema 讀取契約）
    Changes/       # AddFieldChange、AlterFieldChange、RenameFieldChange、
                   # AddIndexChange、DropIndexChange、ITableChange
  Providers/       # IDialectFactory（provider 工廠契約）
    SqlServer/     # SQL Server 實作（DDL + DML + SchemaProvider + Helper）
    PostgreSql/    # PostgreSQL 實作
    Sqlite/        # SQLite 實作
  Manager/         # DbConnectionManager、DbProviderManager、DbConnectionInfo、
                   # DbDialectRegistry
  Logging/         # DbAccessLogger、DbLogContext
  *.cs (root)      # 跨切面基礎設施：
                   # DbAccess、DbCommandSpec、DbCommandSpecCollection、
                   # DbBatchSpec、DbBatchResult、DbCommandResult、
                   # DbCommandResultCollection、DbCommandKind、
                   # DbConnectionScope、DbParameterSpec、DbParameterSpecCollection、
                   # DataTableUpdateSpec、DbFunc、ILMapper、CommandTextVariable
```

命名空間佈局遵循三項原則（見 [ADR-008](../../docs/adr/adr-008-bee-db-namespace-layout.md)）：

1. **語法層（`Bee.Db.Ddl` / `Bee.Db.Dml`）vs 模型層（`Bee.Db.Schema`）** — 產 SQL 字串者歸 `Ddl` / `Dml`；操作 `TableSchema` 模型者歸 `Schema`。
2. **契約依職能歸類，實作依 provider 歸類** — 抽象契約進對應職能命名空間；具體 per-provider 實作不論是 DDL、DML、或 schema 讀取都統一歸 `Bee.Db.Providers.{X}`。
3. **`Bee.Db.Providers` 僅留 `IDialectFactory`** — 它是工廠綁定契約，不再作為 per-provider 介面的雜物袋。
