# 計畫：補齊 MySQL / Oracle 支援（總綱）

**狀態：📝 擬定中**

## 背景

`DatabaseType` enum（[`src/Bee.Definition/Database/DatabaseType.cs`](../../src/Bee.Definition/Database/DatabaseType.cs)）一開始就預埋了五個值：`SQLServer`、`MySQL`、`SQLite`、`Oracle`、`PostgreSQL`。截至目前：

| DatabaseType | 實作狀態 | CI 整合測試 |
|---|---|---|
| `SQLServer` | ✅ 已實作 | ✅ service container（`mssql/server:2022`）|
| `PostgreSQL` | ✅ 已實作 | ✅ service container（`postgres:17`）|
| `SQLite` | ✅ 已實作 | ✅ in-memory，無 container |
| `MySQL` | ❌ 未實作 | ❌ 不納入 |
| `Oracle` | ❌ 未實作 | ❌ 不納入 |

由於 AI Coding 輔助目前對「依既有 pattern 複製、針對方言差異微調」的工作特別有效率，且抽象層在進入下一階段架構擴展前先補齊更划算（避免之後動到 dialect 介面又要回頭改），決定在此階段補完 MySQL 與 Oracle。

### 既有抽象層的乾淨程度（為何此時補划算）

調查結論：目前**沒有任何 `switch (DatabaseType)` 分支污染** business / repository 層，所有方言路由都走兩個註冊表（[`DbProviderRegistry`](../../src/Bee.Db/Manager/DbProviderRegistry.cs)、[`DbDialectRegistry`](../../src/Bee.Db/Manager/DbDialectRegistry.cs)）。新增一個 DB 等於：

1. 在 `src/Bee.Db/Providers/<DbName>/` 下新增 9 個檔案（複製 SQLite 資料夾結構 + 改方言）
2. 在兩個 Registry 註冊
3. 補一處 [`tests/Bee.Tests.Shared/DbGlobalFixture.cs:106-121`](../../tests/Bee.Tests.Shared/DbGlobalFixture.cs) `GetSeedExpressions()` 加 case
4. 補純語法單元測試（CI 跑、不需連線）
5. 補整合測試（本機跑，CI 因環境變數未設自動 skip）

幾乎沒有重構成本——這也是為什麼選在現在動手。

## 目標

1. `DatabaseType.MySQL` 與 `DatabaseType.Oracle` 達到與 SQLite 相同的支援等級：
   - 完整的 `IDialectFactory` + 五個 CommandBuilder + Schema Provider
   - 純語法單元測試覆蓋 CREATE / ALTER / Rebuild / Form 四類 SQL
   - 整合測試（DbAccess、TableUpgradeOrchestrator、SessionRepository）通過
2. 不破壞既有三種 DB 的測試與行為
3. 維持「無 `switch (DatabaseType)`」的設計乾淨度

### 不涵蓋

- **CI 整合測試**：MySQL / Oracle 的實際 DB 存取測試**只在本機跑**，不啟動 CI service container（理由詳見下節）
- **Bee.Repository.Server 之外的 Repository 重構**：本計畫不擴大範圍到查詢產生器（如 SelectBuilder 分頁演進）
- **歷史版本相容**：MySQL 假設 8.0+、Oracle 假設 19c+；不支援更舊版本（決策理由見各 DB plan）
- **Entity Framework 整合**：本框架不走 EF 路線，僅 ADO.NET 直連
- **效能調優**：先求對，不求快；prepared statement、batch insert 等優化留給後續

## 策略：CI 純語法、整合本機

### 規則

- **純語法測試**（builder 吐出 SQL 字串、字串比對）→ 一律普通 `[Fact]` / `[Theory]`，CI 必跑
- **整合測試**（實際開連線、執行 SQL）→ `[DbFact(DatabaseType.MySQL)]` / `[DbFact(DatabaseType.Oracle)]`，本機透過 `BEE_TEST_CONNSTR_MYSQL` / `BEE_TEST_CONNSTR_ORACLE` 啟用

### 為什麼 MySQL / Oracle 不進 CI

- **MySQL**：service container 可行，但目前沒有實際使用者場景，多一個 container 就多一份 CI 時間與 flaky 風險
- **Oracle**：官方 image 啟動時間 >2 分鐘、License 與資源開銷重，對 PR 流程是明顯負擔
- **SQLite 為何能進 CI**：純驅動、in-memory、零容器成本——MySQL/Oracle 沒這個性質
- **後續若有變化**：補齊後若實際使用者出現，再單獨評估納入 CI（service container 設定本身已有 SQL Server / PostgreSQL 的 pattern 可參考）

### 落地細節（兩份子 plan 共通）

`build-ci.yml` **不變動**。`.runsettings` 新增兩行（註解掉的 placeholder，由開發者本機自行填）：

```xml
<!-- MySQL 連線字串（[DbFact(DatabaseType.MySQL)] 使用） — 本機設定後啟用 -->
<!-- <BEE_TEST_CONNSTR_MYSQL>Server=localhost;Port=3306;Database=common;User=testuser;Password=testpass;</BEE_TEST_CONNSTR_MYSQL> -->

<!-- Oracle 連線字串（[DbFact(DatabaseType.Oracle)] 使用） — 本機設定後啟用 -->
<!-- <BEE_TEST_CONNSTR_ORACLE>Data Source=localhost:1521/XEPDB1;User Id=testuser;Password=testpass;</BEE_TEST_CONNSTR_ORACLE> -->
```

`DbFact` 機制本身已支援「未設環境變數即 skip」，不需修改任何測試基礎設施。

## 前置步驟：`DatabaseType` 列舉順序調整

在動兩份子 plan 之前，先做一次 enum 重排——把 SQLite 移到最後（嵌入式 DB 與其他 client-server RDBMS 定位不同），剩餘四種按「實作成熟度 / 預設優先級」排序：

```csharp
// 現況
public enum DatabaseType { SQLServer, MySQL, SQLite, Oracle, PostgreSQL }

// 調整後
public enum DatabaseType { SQLServer, PostgreSQL, MySQL, Oracle, SQLite }
```

理由：
- **SQLite 異類**：in-process / 單檔 / in-memory，其他四種是 client-server，並列時應放最後
- **同類內排序對齊預設值**：`BackendInfo.DatabaseType` / `DatabaseServer.DatabaseType` / `BackendConfiguration.DatabaseType` 預設皆為 `SQLServer`，PostgreSQL 是 CI 雙主軸之一，自然排在第二
- **時機**：本專案未發佈 NuGet 4.x release 以外的版本、現有 JSON 序列化採名稱字串、MessagePack 路徑無 `DatabaseType`、grep 無 `(int)DatabaseType` ordinal 依賴——重排成本最低時機就是現在

### 安全性驗證（已確認）

| 風險面 | 結論 | 證據 |
|---|---|---|
| JSON 序列化 | 安全（用字串） | [`SerializeFunc.cs:166`](../../src/Bee.Base/Serialization/SerializeFunc.cs) 註冊 `JsonStringEnumConverter` |
| MessagePack 序列化 | 不影響 | `DatabaseServer` / `DatabaseItem` / `BackendConfiguration` 均**未**標記 `[MessagePackObject]` |
| Ordinal 依賴 | 無 | grep 結果無 `(int)DatabaseType` 或數值比較 |
| `[DefaultValue]` | 不影響 | `[DefaultValue(DatabaseType.SQLServer)]` 用 enum 值參考、不依賴序數 |

### 實作步驟

1. 修改 [`src/Bee.Definition/Database/DatabaseType.cs`](../../src/Bee.Definition/Database/DatabaseType.cs)，調整成員順序
2. 同步調整 [`src/Bee.Db/DbFunc.cs:17-24, 52-59`](../../src/Bee.Db/DbFunc.cs) 兩個 dictionary 內的初始化順序（功能無關，純可讀性對齊）
3. 跑 `./test.sh` 全綠
4. 獨立 commit，commit message 範例：`refactor(Bee.Definition): reorder DatabaseType members (SQLite to last)`

此步驟與兩份子 plan 解耦，可先合進 main、確認 CI 綠後再開工。

## Case-insensitivity 策略（採 A：DB collation 統一）

ERP 場景的字串查詢期望大小寫不敏感（查 `jeff` 應命中 `Jeff`），但五種 DB 的預設行為不一致：

| DB | 預設行為 | ERP 期望 |
|---|---|---|
| SQL Server | 看 instance collation，多數 `_CI_AS` | ✅ 通常吻合 |
| MySQL 8.0 | `utf8mb4_0900_ai_ci`（CI） | ✅ 吻合 |
| PostgreSQL | case-sensitive | ❌ 不吻合 |
| SQLite | `BINARY`（CS） | ❌ 不吻合 |
| Oracle | case-sensitive | ❌ 不吻合 |

採 **A 路線：DB 層級處理**——框架在 CREATE TABLE 階段（或連線初始化階段，Oracle 例外）讓查詢預設 CI 比對。優點：
- 保留資料原樣（`Jeff` 不會被改成 `jeff`）
- 索引能自然吃 CI 比對（不需函式索引）
- 不影響 WhereBuilder / SelectBuilder / DbAccess——程式只動 CREATE TABLE / 連線初始化兩個切面

### 落地方式（五種 DB）

| DB | 處理階段 | 具體做法 | 動的檔案 |
|---|---|---|---|
| SQL Server | CREATE DATABASE | 依賴 DB collation 設定（多數 `_CI_AS`） | 0 行（部署約定） |
| MySQL | CREATE TABLE | 表後綴 `DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci` | `MysqlCreateTableCommandBuilder` |
| Oracle | **連線初始化** | `OracleProvider` 開連線後 `ALTER SESSION SET NLS_COMP='LINGUISTIC' NLS_SORT='BINARY_CI'` | `OracleProvider`（連線工廠） |
| SQLite | CREATE TABLE | text 欄位後綴 `COLLATE NOCASE` | `SqliteCreateTableCommandBuilder` / `SqliteTypeMapping` |
| PostgreSQL | CREATE TABLE | text 欄位帶 ICU CI collation（PG 12+ 內建，如 `COLLATE "und-x-icu"` deterministic=false） | `PgCreateTableCommandBuilder` / `PgTypeMapping` |

合計約 35–55 行程式 + 對應測試。

### 副作用與接受範圍

CI collation 同時影響 `UNIQUE` 約束、PK 比對、JOIN 比對——`Jeff` 與 `jeff` 在 UNIQUE 下視為同一個值。對 ERP 99% 場景剛好對；少數需 case-sensitive 的欄位：

- **密碼**：本來就 hash 後存 binary，不走字串比對 → 不影響
- **Token / GUID**：值域格式固定（hex / Base64 / GUID 標準格式），不會出現大小寫歧義 → 不影響

### 範圍排除（本輪不做）

- **FormSchema `CaseSensitive` flag**：等真踩到 CS 需求（密碼 / Token 之外的場景）再加，獨立小 PR
- **跨 DB 行為一致性測試**：不引入「同查詢在多 DB 結果一致」的測試框架

### PG / SQLite 對齊任務（提前到第 2 步、獨立執行）

PG 與 SQLite 的 case-insensitive 對齊屬於「修改既有 dialect」、會動到既有測試斷言。本輪**提前到 enum 重排之後、MySQL 開工之前**執行，理由：

- 先建立五 DB 一致 CI 基準，MySQL/Oracle 進來時不踩到「行為分裂」的測試 churn
- 既有測試斷言更新一次後，MySQL/Oracle 直接套相同 pattern
- 工作量小（合計 ~25–40 行 + 對應測試），不會卡住前面進度

具體改動：

- `PgCreateTableCommandBuilder` / `PgTypeMapping`：text 欄位輸出時帶 `COLLATE "und-x-icu"`（或等價 ICU CI collation，PG 12+ 內建）
- `SqliteCreateTableCommandBuilder` / `SqliteTypeMapping`：text 欄位輸出時帶 `COLLATE NOCASE`
- 既有純語法測試的字串斷言更新對應 SQL（含 COLLATE 子句）
- 既有 `[DbFact]` 整合測試補上「CI 比對驗證」案例（如 `WHERE name = 'jeff'` 命中 `Jeff`）

獨立 commit，commit message 範例：`feat(Bee.Db): align PG/SQLite to case-insensitive collation`

> **不併入 MySQL/Oracle plan 的理由**：兩份子 plan 範圍是「新建 DB 支援」，混入「修改既有 dialect」會稀釋 commit 邊界與測試矩陣的清晰度。獨立步驟對應獨立 commit，邊界乾淨。

## 共通基礎建設修改

兩份子 plan **共享**以下修改點，由先動工的子 plan（建議 MySQL 先，較單純）順帶完成：

### 1. `DbGlobalFixture.GetSeedExpressions()` 補 case

[`tests/Bee.Tests.Shared/DbGlobalFixture.cs:106-121`](../../tests/Bee.Tests.Shared/DbGlobalFixture.cs)

```csharp
// 現況
case DatabaseType.SQLServer:    return ("NEWID()", "GETDATE()");
case DatabaseType.PostgreSQL:   return ("gen_random_uuid()", "CURRENT_TIMESTAMP");
case DatabaseType.SQLite:       return ("hex(randomblob(16))", "CURRENT_TIMESTAMP");
// MySQL / Oracle: throw NotSupportedException

// 補上
case DatabaseType.MySQL:        return ("UUID()", "CURRENT_TIMESTAMP(6)");
case DatabaseType.Oracle:       return ("SYS_GUID()", "SYSTIMESTAMP");
```

> 預期：MySQL `UUID()` 回傳 36 字元字串、Oracle `SYS_GUID()` 回傳 16-byte RAW；確保 seed 欄位型別與 fixture 預期一致（在各 DB 的 `*TypeMapping` 內收斂）。

### 2. Registry 註冊

[`DbProviderRegistry.cs:24-50`](../../src/Bee.Db/Manager/DbProviderRegistry.cs) 與 [`DbDialectRegistry.cs:24-48`](../../src/Bee.Db/Manager/DbDialectRegistry.cs)：依既有 pattern 加入 MySQL / Oracle 的註冊（每個 DB 兩處）。

### 3. `.runsettings` placeholder（如上節）

### 4.（不做）抽出 `ISeedExpressionProvider`

評估過：目前 `GetSeedExpressions()` 只有一處 switch、合計 5 個 case，抽介面屬於過度工程。維持現狀，但在 method 上補一行註解說明「新 DB 必須在此補 case」。

## 子 plan

| 子 plan | 範圍 | 預期工時 |
|---|---|---|
| [plan-mysql-support.md](plan-mysql-support.md) | MySQL 8.0+ 完整支援，使用 `MySqlConnector` 套件 | 1.5–2 週 |
| [plan-oracle-support.md](plan-oracle-support.md) | Oracle 19c+ 完整支援，使用 `Oracle.ManagedDataAccess.Core` 套件 | 2–2.5 週 |

兩份子 plan **可獨立執行、互不依賴**。建議的執行順序：

1. **enum 重排**（前置步驟、獨立 commit）
2. **PG / SQLite case-insensitive 對齊**：修改既有 dialect、建立五 DB 一致 CI 基準（詳見上節「PG / SQLite 對齊任務」）
3. **MySQL 完整實作**：方言相對接近 PostgreSQL（identifier 略不同、分頁同 LIMIT），可順帶完成「共通基礎建設修改」上述 4 點；Case-insensitivity 從 day 1 內建（CREATE TABLE 帶 `COLLATE=utf8mb4_0900_ai_ci`）
4. **Oracle 完整實作**：方言怪癖最多（IDENTITY/SEQUENCE、識別符長度、NUMBER 型別、RAW vs VARCHAR2）；Case-insensitivity 從 day 1 內建（`OracleProvider` 連線初始化執行 `ALTER SESSION` NLS 設定）

每個步驟完成後獨立 commit、獨立通過所有現有測試，再動下一步。

## 驗收標準

總綱完成定義為：

- [ ] MySQL plan 完成（標記為 ✅ 已完成）
- [ ] Oracle plan 完成（標記為 ✅ 已完成）
- [ ] `DbGlobalFixture.GetSeedExpressions()` 不再 throw `NotSupportedException`
- [ ] CI 持續綠（既有三種 DB 測試不受影響）
- [ ] 本機跑 `./test.sh` 在「未設 MySQL/Oracle 連線字串」時，相關 `[DbFact]` 自動 Skipped 而非 Failed
- [ ] 本機跑 `./test.sh` 在「設好 MySQL/Oracle 連線字串」時，新增的整合測試全綠
- [ ] PG / SQLite 既有 dialect 補上 case-insensitive collation，五種 DB 字串查詢行為對齊（`SELECT * FROM x WHERE name = 'jeff'` 在五種 DB 上皆能命中 `Jeff`）
- [ ] `docs/architecture-overview.md` 等架構文件不需更新（既有設計已預期五種 DB；補完即兌現）

## 風險與緩解

| 風險 | 影響 | 緩解 |
|---|---|---|
| Oracle container 啟動慢、本機開發體感差 | 中 | 文件中標註「Oracle 整合測試僅做大改動時跑」、平時靠純語法測試把關 |
| 新 DB 暴露 dialect 抽象不足（如 SelectBuilder 分頁） | 中 | 子 plan 動工初期先確認；若需要重構 dialect 介面，**先停下回來修總綱** |
| `MySqlConnector` 與 `Oracle.ManagedDataAccess.Core` 與既有 NuGet 衝突 | 低 | 兩個套件均純 .NET 實作、無原生依賴，加入 `Bee.Tests.Shared` 即可 |
| Oracle Reserved Word 比 SQL Server 多（`COMMENT`、`SIZE` 等常用詞） | 中 | identifier quoting 統一用 `""`，由 `DbFunc.QuoteIdentifiers` 處理；測試 schema 若踩到 reserved word 會在整合測試階段顯現 |
| MySQL `SQL_MODE` 預設值跨版本/部署差異（`ANSI_QUOTES` 是否啟用影響 quoting） | 低 | 子 plan 明確假設「不啟用 `ANSI_QUOTES`」、用 backtick；若使用者環境啟用，文件提醒需自行處理 |

## 參考

- 抽象層調查報告：見本 plan 擬定階段對話紀錄（git log 上一輪 commit 之前）
- 既有 dialect 實作 pattern 範例：[`src/Bee.Db/Providers/Sqlite/`](../../src/Bee.Db/Providers/Sqlite/)（最新、最乾淨）
- 既有測試 pattern 範例：[`tests/Bee.Db.UnitTests/SqliteCreateTableCommandBuilderTests.cs`](../../tests/Bee.Db.UnitTests/SqliteCreateTableCommandBuilderTests.cs)
