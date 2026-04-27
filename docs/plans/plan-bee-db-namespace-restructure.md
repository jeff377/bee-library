# 計畫：Bee.Db DDL / DML 命名空間重整

**狀態：✅ 已完成（2026-04-27）**

## 背景

目前 `Bee.Db` 的命名空間結構在 DDL 與 DML 的劃分上有以下問題：

### 1. `Bee.Db.Sql` 命名過於籠統

實際上整包僅放 DML 構件（query composition + IUD 子句構件）：

```
src/Bee.Db/Sql/
  SelectCommandBuilder.cs / InsertCommandBuilder.cs / UpdateCommandBuilder.cs / DeleteCommandBuilder.cs
  ISelectBuilder.cs / SelectBuilder.cs
  IFromBuilder.cs / FromBuilder.cs
  IWhereBuilder.cs / WhereBuilder.cs / InternalWhereBuilder.cs / WhereBuildResult.cs
  ISortBuilder.cs / SortBuilder.cs
  SelectContext.cs / SelectContextBuilder.cs
  QueryFieldMapping.cs / QueryFieldMappingCollection.cs
  TableJoin.cs / TableJoinCollection.cs
  IParameterCollector.cs / DefaultParameterCollector.cs
```

`Sql` 字面上會讓人誤以為連 DDL 也包含其中，但實際 DDL 構件並不在這裡。

### 2. `Bee.Db.Providers` 根層混放 DDL / DML / 模型讀取 / 工廠介面

```
src/Bee.Db/Providers/
  IDialectFactory.cs              ← 工廠（meta）
  ICreateTableCommandBuilder.cs   ← DDL 字串產生契約
  ITableAlterCommandBuilder.cs    ← DDL 字串產生契約
  ITableRebuildCommandBuilder.cs  ← DDL 字串產生契約
  ITableSchemaProvider.cs         ← Schema 模型讀取（非 DDL 產生）
  IFormCommandBuilder.cs          ← DML 字串產生契約（form 級 CRUD）
```

四種職能介面平鋪同層，無法從命名空間判斷介面屬性。

### 3. `Bee.Db.Schema/*` 不產生 SQL，但承擔 DDL 字串產生的「客戶」角色

實地檢視 `src/Bee.Db/Schema/*.cs`，所有 `ALTER` / `CREATE TABLE` 等字樣**僅出現在 XML doc comment**，沒有任何 SQL 字串生成。`Schema/*` 做的是：

- `TableSchemaBuilder` — 高階 façade（驅動比對 → 升級流程）
- `TableSchemaComparer` / `TableSchemaDiff` — 模型比對與差異結果
- `TableUpgradeOrchestrator` — 流程協調（呼叫 DDL builder 產 SQL，但自身不產 SQL）
- `UpgradePlan` / `UpgradeStage*` / `UpgradeOptions` / `UpgradeExecutionMode` / `ChangeExecutionKind` / `DescriptionLevel` / `DescriptionChange` — 流程模型與 enum

亦即 `Bee.Db.Schema` 的職能是「**TableSchema 模型 + 比對 + 升級流程**」，而非「DDL 語法產生」。

### 4. 根層含有兩個 DML 相關的孤兒檔案

- `src/Bee.Db/TableSchemaCommandBuilder.cs` — 產 INSERT/UPDATE/DELETE 字串（純 DML 語法層），名稱含 `Schema` 屬於誤導
- `src/Bee.Db/JoinType.cs` — SELECT 專用 enum，僅 `FromBuilder` / `TableJoin` 使用

兩者都應屬於 DML 命名空間，目前被孤立在根層。

### 5. README 標示的目錄結構與實際不符

[`src/Bee.Db/README.md`](../../src/Bee.Db/README.md) 第 133–156 行描述了一個包含 `DbAccess/`、`Query/Context/`、`Query/From/`、`Query/Select/`、`Query/Sort/`、`Query/Where/` 的結構，但實際檔案系統並無這些資料夾。本次重整應該一併讓 README 與實際結構對齊。

## 目標

1. 命名空間能直接表達 **模型 / DDL 語法 / DML 語法 / 工廠** 四個關注點
2. 同類介面（DDL 全套、DML 全套）集中，降低跨命名空間查找成本
3. 對外 API 的 provider 註冊入口不變（仍是 `using Bee.Db.Providers.{SqlServer|PostgreSql|Sqlite}; new XxxDialectFactory();`）
4. 實作層命名空間（`Bee.Db.Providers.Sqlite` 等）保留為 provider 對稱結構，不對 DDL / DML 再做二次切割
5. 根層僅保留跨切面基礎設施（`DbAccess`、`DbCommandSpec`、`DbFunc`、`ILMapper` 等），DML / DDL 相關檔案不留在根層
6. README 與 ADR 與實際結構同步
7. **不異動套件版本號**（沿用 `4.0.x`）

### 不涵蓋

- 行為變更（純命名空間 / 檔案位置調整，無邏輯修改）
- 公開類別 / 介面 / 方法的拆分或合併
- `Bee.Db.Schema` 既有命名（保留）
- `Bee.Db.Schema.Changes` 既有命名（保留）
- `Bee.Db.Manager` / `Bee.Db.Logging` 的調整（職能清晰，維持原樣）
- 跨 package（`Bee.Repository` 等）的命名空間調整 — 僅同步更新 `using`
- 增加新功能（無新介面、無新類別）

## 設計原則

> **語法層 vs 模型層分離；契約依職能歸類，實作依 provider 歸類。**

| 層次 | 命名空間 | 職能特徵 |
|------|----------|----------|
| 語法層 | `Bee.Db.Ddl` / `Bee.Db.Dml` | 產生 SQL 字串（含其抽象契約） |
| 模型層 | `Bee.Db.Schema` | 操作 `TableSchema` 資料模型（比對、流程） |
| 工廠 | `Bee.Db.Providers` | 將語法層契約綁定到 provider |
| 實作 | `Bee.Db.Providers.{X}` | 各 provider 同時負責該家族的 DDL / DML / 讀取實作 |

依賴方向：`Bee.Db.Schema` → `Bee.Db.Ddl`（升級流程使用 DDL builder），單向。

完整命名空間分配：

| 關注點 | 命名空間 | 內容 |
|--------|----------|------|
| 根層基礎設施 | `Bee.Db` | DbAccess、DbCommandSpec、DbBatchSpec、DbConnectionScope、DbFunc、ILMapper 等執行核心 |
| 連線 / 註冊管理 | `Bee.Db.Manager` | DbConnectionManager、DbProviderManager、DbDialectRegistry |
| 日誌 / 診斷 | `Bee.Db.Logging` | DbAccessLogger、DbLogContext |
| **DDL 字串產生契約** | **`Bee.Db.Ddl`**（新增） | `ICreateTableCommandBuilder`、`ITableAlterCommandBuilder`、`ITableRebuildCommandBuilder`（自 Providers 搬入） |
| Schema 模型 / 比對 / 流程 | `Bee.Db.Schema` | 既有檔案不變 + **`ITableSchemaProvider`**（自 Providers 搬入；其輸出為 `TableSchema` 模型，與本命名空間語意一致） |
| Schema 變更模型 | `Bee.Db.Schema.Changes` | 不變 |
| **DML 字串產生 + 構件** | **`Bee.Db.Dml`**（取代 `Bee.Db.Sql`） | 原 `Sql/*` 全部 + `IFormCommandBuilder`（自 Providers 搬入）+ `TableSchemaCommandBuilder`（自根層搬入）+ `JoinType`（自根層搬入） |
| Provider 工廠契約 | `Bee.Db.Providers` | 僅留 `IDialectFactory` |
| Provider 實作 | `Bee.Db.Providers.{SqlServer\|PostgreSql\|Sqlite}` | 該 provider 對應的所有 DDL / DML / 讀取 / Helper / TypeMapping 實作（不變） |

## 檔案搬移清單

### A. `Bee.Db.Sql` → `Bee.Db.Dml`（含目錄改名）

實體目錄一併重新命名為 `src/Bee.Db/Dml/`：

| 來源 | 目標 |
|------|------|
| `src/Bee.Db/Sql/SelectCommandBuilder.cs` | `src/Bee.Db/Dml/SelectCommandBuilder.cs` |
| `src/Bee.Db/Sql/InsertCommandBuilder.cs` | `src/Bee.Db/Dml/InsertCommandBuilder.cs` |
| `src/Bee.Db/Sql/UpdateCommandBuilder.cs` | `src/Bee.Db/Dml/UpdateCommandBuilder.cs` |
| `src/Bee.Db/Sql/DeleteCommandBuilder.cs` | `src/Bee.Db/Dml/DeleteCommandBuilder.cs` |
| `src/Bee.Db/Sql/ISelectBuilder.cs` | `src/Bee.Db/Dml/ISelectBuilder.cs` |
| `src/Bee.Db/Sql/SelectBuilder.cs` | `src/Bee.Db/Dml/SelectBuilder.cs` |
| `src/Bee.Db/Sql/IFromBuilder.cs` | `src/Bee.Db/Dml/IFromBuilder.cs` |
| `src/Bee.Db/Sql/FromBuilder.cs` | `src/Bee.Db/Dml/FromBuilder.cs` |
| `src/Bee.Db/Sql/IWhereBuilder.cs` | `src/Bee.Db/Dml/IWhereBuilder.cs` |
| `src/Bee.Db/Sql/WhereBuilder.cs` | `src/Bee.Db/Dml/WhereBuilder.cs` |
| `src/Bee.Db/Sql/InternalWhereBuilder.cs` | `src/Bee.Db/Dml/InternalWhereBuilder.cs` |
| `src/Bee.Db/Sql/WhereBuildResult.cs` | `src/Bee.Db/Dml/WhereBuildResult.cs` |
| `src/Bee.Db/Sql/ISortBuilder.cs` | `src/Bee.Db/Dml/ISortBuilder.cs` |
| `src/Bee.Db/Sql/SortBuilder.cs` | `src/Bee.Db/Dml/SortBuilder.cs` |
| `src/Bee.Db/Sql/SelectContext.cs` | `src/Bee.Db/Dml/SelectContext.cs` |
| `src/Bee.Db/Sql/SelectContextBuilder.cs` | `src/Bee.Db/Dml/SelectContextBuilder.cs` |
| `src/Bee.Db/Sql/QueryFieldMapping.cs` | `src/Bee.Db/Dml/QueryFieldMapping.cs` |
| `src/Bee.Db/Sql/QueryFieldMappingCollection.cs` | `src/Bee.Db/Dml/QueryFieldMappingCollection.cs` |
| `src/Bee.Db/Sql/TableJoin.cs` | `src/Bee.Db/Dml/TableJoin.cs` |
| `src/Bee.Db/Sql/TableJoinCollection.cs` | `src/Bee.Db/Dml/TableJoinCollection.cs` |
| `src/Bee.Db/Sql/IParameterCollector.cs` | `src/Bee.Db/Dml/IParameterCollector.cs` |
| `src/Bee.Db/Sql/DefaultParameterCollector.cs` | `src/Bee.Db/Dml/DefaultParameterCollector.cs` |

對應測試一併調整：

| 來源 | 目標 |
|------|------|
| `tests/Bee.Db.UnitTests/Sql/*` | `tests/Bee.Db.UnitTests/Dml/*`（namespace 改 `Bee.Db.UnitTests.Dml`） |

### B. 根層 DML 相關搬入 `Bee.Db.Dml`

| 來源 | 目標 | 理由 |
|------|------|------|
| `src/Bee.Db/TableSchemaCommandBuilder.cs` | `src/Bee.Db/Dml/TableSchemaCommandBuilder.cs` | 產 INSERT/UPDATE/DELETE，純 DML 語法層；與 `SelectCommandBuilder` 等同類，理應同層 |
| `src/Bee.Db/JoinType.cs` | `src/Bee.Db/Dml/JoinType.cs` | SELECT 專用 enum，僅 `FromBuilder` / `TableJoin` 使用 |

### C. DML 契約：`Bee.Db.Providers` → `Bee.Db.Dml`

| 來源 | 目標 |
|------|------|
| `src/Bee.Db/Providers/IFormCommandBuilder.cs` | `src/Bee.Db/Dml/IFormCommandBuilder.cs` |

### D. DDL 契約：`Bee.Db.Providers` → `Bee.Db.Ddl`（新增命名空間）

新增 `src/Bee.Db/Ddl/` 資料夾：

| 來源 | 目標 |
|------|------|
| `src/Bee.Db/Providers/ICreateTableCommandBuilder.cs` | `src/Bee.Db/Ddl/ICreateTableCommandBuilder.cs` |
| `src/Bee.Db/Providers/ITableAlterCommandBuilder.cs` | `src/Bee.Db/Ddl/ITableAlterCommandBuilder.cs` |
| `src/Bee.Db/Providers/ITableRebuildCommandBuilder.cs` | `src/Bee.Db/Ddl/ITableRebuildCommandBuilder.cs` |

> 註：`ITableSchemaProvider` **不**搬入 `Ddl`，見 E 段。

### E. Schema 模型讀取契約：`Bee.Db.Providers` → `Bee.Db.Schema`

`ITableSchemaProvider` 雖然定義在 `Providers/`，本質是「讀取 schema 模型，輸出 `TableSchema`」，並不產生 DDL 字串；其消費方（`TableSchemaComparer`、`TableUpgradeOrchestrator`）皆位於 `Bee.Db.Schema`，把它就近搬入 `Schema/` 與既有檔案同層。

| 來源 | 目標 |
|------|------|
| `src/Bee.Db/Providers/ITableSchemaProvider.cs` | `src/Bee.Db/Schema/ITableSchemaProvider.cs` |

### F. 不動的部分

- `src/Bee.Db/Providers/IDialectFactory.cs` — 留在 `Bee.Db.Providers`，當作工廠契約
- `src/Bee.Db/Providers/{SqlServer|PostgreSql|Sqlite}/*` — 全部留在原命名空間
  - 即使 `SqliteCreateTableCommandBuilder`（DDL 實作）、`SqliteFormCommandBuilder`（DML 實作）、`SqliteTableSchemaProvider`（讀取實作）並存於 `Bee.Db.Providers.Sqlite`，這是「實作依 provider 歸類」的設計意圖
- `src/Bee.Db/Schema/` 既有所有檔案 — 命名空間與檔名皆不變
- `src/Bee.Db/Schema/Changes/` 全部 — 不變
- `src/Bee.Db/Manager/*` / `src/Bee.Db/Logging/*` — 不變
- `src/Bee.Db/` 根層其餘檔案（`DbAccess`、`DbCommandSpec`、`DbBatchSpec`、`DbCommandResult`、`DbCommandKind`、`DbBatchResult`、`DbParameterSpec`、`DbConnectionScope`、`DataTableUpdateSpec`、`DbFunc`、`ILMapper`、`CommandTextVariable`、`DbCommandResultCollection`、`DbCommandSpecCollection`、`DbParameterSpecCollection`）— 不變

## 影響面

### 受影響檔案統計

- 移動 / 重命名檔案：**30 份**
  - A 段：22 份（Sql/*）
  - B 段：2 份（TableSchemaCommandBuilder + JoinType）
  - C 段：1 份（IFormCommandBuilder）
  - D 段：3 份（DDL 契約）
  - E 段：1 份（ITableSchemaProvider）
  - 對應測試檔搬目錄：1 個目錄（含多檔，narrow 掉算入 A）
- 受影響原始碼 / 測試檔（含 `using Bee.Db.Sql` / `Bee.Db.Providers`）：約 **95 份**檔案需更新 `using`
- 文件：`src/Bee.Db/README.md`、`src/Bee.Db/README.zh-TW.md`、`docs/architecture-overview.md` 與 `docs/dependency-map.md`（如有提及）、新增 `docs/adr/adr-008-bee-db-namespace-layout.md`

### 對外 API 不變的部分

- Provider 註冊：`using Bee.Db.Providers.Sqlite; DbDialectRegistry.Register(DatabaseType.SQLite, new SqliteDialectFactory());` 完全不變
- `IDialectFactory` 仍在 `Bee.Db.Providers`
- `DbAccess`、`DbCommandSpec`、`DbBatchSpec` 等核心類別命名空間不變
- `Bee.Db.Schema` / `Bee.Db.Schema.Changes` 既有命名空間不變
- 所有公開型別名稱不變

### 對外 API 變動的部分（需要使用者改 `using`）

| 舊 using | 新 using |
|----------|----------|
| `using Bee.Db.Sql;` | `using Bee.Db.Dml;` |
| 引用 `TableSchemaCommandBuilder` 或 `JoinType` 時靠 `using Bee.Db;`（root） | 改加 `using Bee.Db.Dml;` |
| 引用 `IFormCommandBuilder` 時 `using Bee.Db.Providers;` | 改為 `using Bee.Db.Dml;` |
| 引用 `ICreateTableCommandBuilder` / `ITableAlterCommandBuilder` / `ITableRebuildCommandBuilder` 時 `using Bee.Db.Providers;` | 改為 `using Bee.Db.Ddl;` |
| 引用 `ITableSchemaProvider` 時 `using Bee.Db.Providers;` | 改為 `using Bee.Db.Schema;` |

實質：**外部 host 應用程式只有極少數的 `using` 需要更新**（除非有自訂 dialect 或直接呼叫 schema 升級流程）。雖屬 breaking change，**不另升版號**，沿用 `4.0.x`，於 release notes 列出對應表即可。

## 執行步驟

> 因屬純結構調整，建議**單一 PR / 單一 commit** 完成（拆 PR 反而每一階段中間都會 broken）。本機可驗證者可直接改 main + push 後等 CI 驗證。

1. **A 階段：Sql → Dml**
   1. 建立 `src/Bee.Db/Dml/` 資料夾
   2. 將 `src/Bee.Db/Sql/*.cs` 全部搬入 `src/Bee.Db/Dml/`
   3. 全域取代 `namespace Bee.Db.Sql` → `namespace Bee.Db.Dml`
   4. 全域取代 `using Bee.Db.Sql;` → `using Bee.Db.Dml;`（src + tests + samples + 上游 packages）
   5. 同步搬 `tests/Bee.Db.UnitTests/Sql/` → `tests/Bee.Db.UnitTests/Dml/` 並改 namespace
   6. 刪除空的 `src/Bee.Db/Sql/`

2. **B 階段：根層 DML 相關搬入 `Bee.Db.Dml`**
   1. `src/Bee.Db/TableSchemaCommandBuilder.cs` → `src/Bee.Db/Dml/TableSchemaCommandBuilder.cs`，namespace 改 `Bee.Db.Dml`
   2. `src/Bee.Db/JoinType.cs` → `src/Bee.Db/Dml/JoinType.cs`，namespace 改 `Bee.Db.Dml`
   3. 全域更新引用方：`Bee.Repository`、測試、`FromBuilder` / `TableJoin` 等檔案需新增 `using Bee.Db.Dml;`

3. **C 階段：DML 契約搬入 `Bee.Db.Dml`**
   1. `src/Bee.Db/Providers/IFormCommandBuilder.cs` → `src/Bee.Db/Dml/IFormCommandBuilder.cs`
   2. namespace 改為 `Bee.Db.Dml`
   3. 全域更新引用方（provider 子目錄的 FormCommandBuilder 實作 + 測試 + `IDialectFactory`）

4. **D 階段：DDL 字串產生契約獨立到 `Bee.Db.Ddl`**
   1. 建立 `src/Bee.Db/Ddl/` 資料夾
   2. 將 3 個 DDL 介面檔（`ICreateTableCommandBuilder` / `ITableAlterCommandBuilder` / `ITableRebuildCommandBuilder`）從 `Providers/` 搬入 `Ddl/`
   3. 改檔內 `namespace Bee.Db.Providers` → `namespace Bee.Db.Ddl`
   4. 全域更新引用方：`Schema/TableUpgradeOrchestrator.cs`、provider 子目錄實作類別、測試 — 新增 `using Bee.Db.Ddl;`，視情況移除多餘的 `using Bee.Db.Providers;`（仍引用 `IDialectFactory` 才保留）
   5. `IDialectFactory.cs` 內含 cref 引用需更新，方法回傳型別改用全限定 `Bee.Db.Ddl.ICreateTableCommandBuilder` 等以避免循環 using

5. **E 階段：`ITableSchemaProvider` 搬入 `Bee.Db.Schema`**
   1. `src/Bee.Db/Providers/ITableSchemaProvider.cs` → `src/Bee.Db/Schema/ITableSchemaProvider.cs`
   2. namespace 改為 `Bee.Db.Schema`
   3. 全域更新引用方：provider 實作（`SqlTableSchemaProvider` 等）新增 `using Bee.Db.Schema;`、測試同步、`IDialectFactory.cs` 內 `CreateTableSchemaProvider()` 回傳型別調整

6. **驗證**
   1. `dotnet build --configuration Release`（必須無 warning，因 `TreatWarningsAsErrors=true`）
   2. `./test.sh`（單元測試 + DB 整合測試在本機應全綠；CI 端 SQLite 必跑、SQL Server / PostgreSQL 視 container 而定）
   3. 確認 `Bee.Repository` 等上游 package build 通過（受影響的 `using` 已同步更新）

7. **文件同步**
   1. 撰寫 `docs/adr/adr-008-bee-db-namespace-layout.md`（**必做**），記錄三項要點：
      - 語法層（`Bee.Db.Ddl` / `Bee.Db.Dml`）vs 模型層（`Bee.Db.Schema`）vs 工廠（`Bee.Db.Providers`）分離
      - 契約依職能歸類，實作依 provider 歸類（對應 .NET BCL `System.Data` / `System.Data.SqlClient` 慣例）
      - 邊界判準：「會產 SQL 字串嗎？」是 → 語法層（DDL 或 DML）；否、且操作 schema 模型 → `Bee.Db.Schema`
   2. 更新 `src/Bee.Db/README.md` 與 `README.zh-TW.md` 的「Directory Structure」段落，與實際檔案對齊（包含本次調整 + 修正既有與實際不符之處）
   3. 檢查 `docs/architecture-overview.md`、`docs/dependency-map.md` 中提及 `Bee.Db.Sql` 或 `Bee.Db.Providers` 介面位置的段落並同步更新

## 風險與權衡

| 風險 | 緩解 |
|------|------|
| 大量 `using` 更新可能導致 build 連環錯 | 全域取代後立即 `dotnet build` 確認；採單一 PR 避免中間狀態 broken |
| 上游 package（`Bee.Repository`）未涵蓋的引用點漏改 | 全 repo `grep "Bee.Db.Sql\|Bee.Db.Providers"` 後逐一檢查，並倚賴 build 失敗作為安全網 |
| 已發布的 NuGet 套件對外 API 公開介面命名空間變動 → **breaking change** | 不升版號（沿用 4.0.x），於 release notes 清楚列出 namespace 對應表，請使用者一次更新 `using` |
| `IDialectFactory` 介面內部引用 DDL 契約 與 `ITableSchemaProvider`，可能造成 `Bee.Db.Providers` ↔ `Bee.Db.Ddl` / `Bee.Db.Schema` 雙向 using | 介面回傳型別改用全限定名（如 `Bee.Db.Ddl.ICreateTableCommandBuilder`、`Bee.Db.Schema.ITableSchemaProvider`）即可避免 |
| `Bee.Db.Schema` 與 `Bee.Db.Ddl` 兩個近似命名空間並存，使用者可能困惑邊界 | ADR-008 明確界定（語法層 vs 模型層）；README 加說明；命名空間 doc comment 補上 summary |

### 不採用的替代方案

1. **「最小改動」方案**（僅改名 `Sql → Dml`，不動 Providers 內混置）
   - 雖然影響面最小，但本次反映出的核心問題（DDL/DML 契約混置、與 schema 讀取介面混置）並未解決，未來新增 DDL 契約（如 index 管理）仍會遇到一樣的歸屬模糊問題。
2. **`Bee.Db.Schema` → `Bee.Db.Ddl` 整批改名**
   - 表面上與 `Bee.Db.Dml` 形成對稱，但 `Schema/*` 並不產 SQL（只做模型比對與流程），改名為 `Ddl` 會把「模型層」與「語法層」兩個關注點混在一起，本質上不正確。本版改採三層命名（Schema 模型層 / Ddl 語法層 / Dml 語法層）。
3. **provider 子目錄內進一步分 DDL/DML 兩個子命名空間**（如 `Bee.Db.Providers.Sqlite.Ddl` / `Bee.Db.Providers.Sqlite.Dml`）
   - 對稱性差且每個 provider 子目錄檔案數不大（約 8–9 份），切割成本大於價值。違反「實作依 provider 歸類」的設計簡潔性。
4. **`TableSchemaCommandBuilder` / `JoinType` 維持根層**
   - 根層應保留給跨切面基礎設施（DbAccess、DbCommandSpec、DbFunc、ILMapper），命令 builder 與 SELECT 專用 enum 不屬於這個層級。本版搬入 `Bee.Db.Dml` 與 `SelectCommandBuilder` 等同類同層。

## 驗收標準

- [ ] `dotnet build --configuration Release` 全綠且無 warning
- [ ] `./test.sh` 全綠（含 DB 整合測試）
- [ ] `Bee.Repository` 等上游 package 同步更新並通過編譯
- [ ] 全 repo 不再出現 `namespace Bee.Db.Sql`
- [ ] `Bee.Db.Providers` 命名空間下僅剩 `IDialectFactory`
- [ ] `Bee.Db.Ddl` 命名空間包含原 3 個 DDL 字串產生契約
- [ ] `Bee.Db.Schema` 命名空間新增 `ITableSchemaProvider`，其餘檔案不變
- [ ] `Bee.Db.Schema.Changes` 命名空間完全不變
- [ ] `Bee.Db.Dml` 命名空間包含原 `Sql/*` 全部 + `IFormCommandBuilder` + `TableSchemaCommandBuilder` + `JoinType`
- [ ] `src/Bee.Db/` 根層不再含 `TableSchemaCommandBuilder.cs` 與 `JoinType.cs`
- [ ] 公開型別名稱完全不變
- [ ] `docs/adr/adr-008-bee-db-namespace-layout.md` 已撰寫
- [ ] `src/Bee.Db/README.md` 與 `README.zh-TW.md` 的 Directory Structure 與實際對齊
- [ ] 套件版本號未變動（仍為 `4.0.x`）
- [ ] CI 通過
