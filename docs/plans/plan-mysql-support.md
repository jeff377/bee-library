# 計畫：補上 MySQL 支援

**狀態：📝 擬定中**

> 本計畫為 [plan-multi-db-overview.md](plan-multi-db-overview.md) 的子 plan。執行前請先完成總綱定義的「前置步驟：DatabaseType 列舉順序調整」。

## 背景

`DatabaseType.MySQL` 自 enum 建立以來就是預埋值，但目前無對應 dialect / provider 實作。本計畫補完到與 SQLite 等同的支援等級——9 個 Provider 檔 + 純語法測試 + 整合測試（本機跑、CI 跳過）。

策略原則（CI 純語法、整合本機）見總綱。

## 目標

1. `DatabaseType.MySQL` 透過 `DbProviderRegistry.Get()` 與 `DbDialectRegistry.Get()` 取得有效實作
2. CREATE TABLE / ALTER TABLE / Rebuild / Form（INSERT/UPDATE/DELETE）四類 SQL 產生器在純語法測試中通過
3. 本機設好 `BEE_TEST_CONNSTR_MYSQL` 後，所有 `[DbFact(DatabaseType.MySQL)]` 整合測試（DbAccess、TableUpgradeOrchestrator、SessionRepository 等）通過
4. CI 不變動（無 service container），未設環境變數時整合測試自動 skip

## 不涵蓋

- **MySQL 5.7 以下**：本計畫鎖定 8.0+，理由見下節
- **MariaDB**：方言相近但非完全相容（CTE、IDENTITY、JSON 行為差異），有需要時另立計畫
- **特殊儲存引擎**：僅針對 InnoDB 設計（預設、ACID）；MyISAM / Memory engine 不支援
- **效能調優**：prepared statement 快取、batch insert 等留給後續

## 套件選擇

採用 **`MySqlConnector`**（NuGet：`MySqlConnector`）。

| 比較 | MySqlConnector | MySql.Data |
|---|---|---|
| 授權 | MIT | GPL 雙授權（商用需確認） |
| 維護方 | 社群（與 Pomelo EF 同源） | Oracle 官方 |
| async 設計 | async-first | 同步為主、async 是 wrapper |
| .NET 10 相容性 | ✅ | ✅ |

選擇理由：MIT 授權對開源 NuGet 套件更友善（避免下游使用者擔心 GPL 傳染）、async 模型乾淨、與既有 `Microsoft.Data.SqlClient` / `Npgsql` / `Microsoft.Data.Sqlite` 風格一致。

加入位置：[`tests/Bee.Tests.Shared/Bee.Tests.Shared.csproj`](../../tests/Bee.Tests.Shared/Bee.Tests.Shared.csproj)（與其他 driver 並列）。`Bee.Db` 本身仍保持 ADO.NET 無關。

## 版本假設：MySQL 8.0+

理由：
- **Window Function / CTE**：8.0 才支援，是後續查詢產生器演進的基礎
- **utf8mb4 預設**：8.0 預設字符集為 utf8mb4，避免 4-byte UTF-8（emoji / 部分中日韓字）截斷問題
- **`CHAR_LENGTH` / `INFORMATION_SCHEMA` 完整度**：8.0 schema 查詢更穩定
- **EOL 視窗**：5.7 已於 2023-10 EOL，新專案無需相容

文件中明確標註此前提；遇 5.7 環境建議升級而非降級支援。

## 方言決策

| 項目 | 決策 | 備註 |
|---|---|---|
| Parameter prefix | `@` | 已預埋於 [`DbFunc.cs:20`](../../src/Bee.Db/DbFunc.cs) |
| Identifier quoting | `` `name` `` | 已預埋於 [`DbFunc.cs:55`](../../src/Bee.Db/DbFunc.cs)；前提：`SQL_MODE` 不啟用 `ANSI_QUOTES` |
| 字符集 / Collation | `utf8mb4` / `utf8mb4_0900_ai_ci`（CI） | CREATE TABLE 後綴 `DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci`；`_ai_ci` = accent-insensitive + case-insensitive，**ERP 字串查詢預設 CI 比對**（如查 `jeff` 命中 `Jeff`）。詳見總綱「Case-insensitivity 策略」 |
| 儲存引擎 | `InnoDB` | CREATE TABLE 帶 `ENGINE=InnoDB` |
| AutoIncrement | `AUTO_INCREMENT` | 配合 `BIGINT` PK |
| Boolean | `TINYINT(1)` | MySQL 的 `BOOLEAN` 為 `TINYINT(1)` 別名；統一寫 `TINYINT(1)` 避免歧義 |
| DateTime | `DATETIME(6)` | 微秒精度；`TIMESTAMP` 受時區影響不採用 |
| UUID | `CHAR(36)` | 簡單路線；不採 `BINARY(16)` 避免跨 DB seed 對齊複雜度 |
| 大字串 | `LONGTEXT` | 避免 `TEXT`/`MEDIUMTEXT` 長度上限踩雷 |
| 大二進位 | `LONGBLOB` | 同上 |
| Default expressions | `UUID()`, `CURRENT_TIMESTAMP(6)` | 對齊 `DATETIME(6)` 精度 |
| 分頁 | `LIMIT n OFFSET m` | 與 PostgreSQL 一致 |
| Schema 概念 | DB = Schema | MySQL 無「schema in database」階層；連線字串的 `Database` 即為 schema |
| Schema 查詢 | `INFORMATION_SCHEMA` | 標準 SQL；參考 PostgreSQL 既有實作 |
| ALTER 能力 | 原生支援大部分操作 | `MysqlAlterCompatibilityRules` 大致比 SQLite 寬鬆、比 SQL Server 嚴格；以 SQL Server 為基準微調 |
| Reserved word | identifier 一律 quote | 由 `DbFunc.QuoteIdentifier` 統一處理，無需特殊處理 |

### `SQL_MODE` 假設

預設假設目標 MySQL 實例**未啟用** `ANSI_QUOTES`。若使用者環境啟用，backtick quoting 會失效——這是部署前提，由使用者文件提醒，不在本計畫處理範圍。

## 實作清單

### Provider 實作（複製 [`src/Bee.Db/Providers/Sqlite/`](../../src/Bee.Db/Providers/Sqlite/) 結構）

新增 `src/Bee.Db/Providers/MySql/` 資料夾，9 個檔案：

| 檔案 | 對應 SQLite 範本 | 主要差異 |
|---|---|---|
| `MySqlDialectFactory.cs` | `SqliteDialectFactory.cs` | 工廠樣板，幾乎只改名 |
| `MySqlCreateTableCommandBuilder.cs` | `SqliteCreateTableCommandBuilder.cs` | `AUTO_INCREMENT`、`ENGINE=InnoDB`、`DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci`（CI 比對 day 1 內建） |
| `MySqlTableAlterCommandBuilder.cs` | `SqliteTableAlterCommandBuilder.cs` | MySQL 原生 `ALTER TABLE ... CHANGE COLUMN`，不需 SQLite rebuild |
| `MySqlTableRebuildCommandBuilder.cs` | `SqliteTableRebuildCommandBuilder.cs` | 多數情境用不到，但保留以供少數 ALTER 限制（如 PK 變更） |
| `MySqlFormCommandBuilder.cs` | `SqliteFormCommandBuilder.cs` | 細節幾乎相同，差在 quoting 與 `LAST_INSERT_ID()` |
| `MySqlTableSchemaProvider.cs` | `PgTableSchemaProvider.cs`（更接近） | 走 `INFORMATION_SCHEMA.COLUMNS / KEY_COLUMN_USAGE / STATISTICS` |
| `MySqlSchemaHelper.cs` | `SqliteSchemaHelper.cs` | `GetDefaultValueExpression()` 對應表 |
| `MySqlTypeMapping.cs` | `SqliteTypeMapping.cs` | `FieldDbType` → MySQL 型別字串 |
| `MySqlAlterCompatibilityRules.cs` | `SqlAlterCompatibilityRules.cs`（SQL Server） | 列出 MySQL 8.0 原生 ALTER 支援的能力 |

### Registry 註冊

[`DbProviderRegistry.cs`](../../src/Bee.Db/Manager/DbProviderRegistry.cs) 與 [`DbDialectRegistry.cs`](../../src/Bee.Db/Manager/DbDialectRegistry.cs) 各加一行（依既有 pattern）。

### 測試實作（複製 SQLite 測試範本）

新增 `tests/Bee.Db.UnitTests/`：

| 檔案 | 對應 SQLite 範本 | 測試類型 |
|---|---|---|
| `MySqlCreateTableCommandBuilderTests.cs` | `SqliteCreateTableCommandBuilderTests.cs` | 純語法（CI 跑） |
| `MySqlTableAlterCommandBuilderTests.cs` | `SqliteTableAlterCommandBuilderTests.cs` | 純語法 |
| `MySqlFormCommandBuilderTests.cs` | `SqliteFormCommandBuilderTests.cs` | 純語法 |
| `MySqlTableSchemaProviderStaticTests.cs` | `SqliteTableSchemaProviderStaticTests.cs` | 純語法 |

整合測試**不開新檔**，沿用既有 `DbAccessTests.cs`、`TableUpgradeOrchestratorIntegrationTests.cs`、`SessionRepositoryTests.cs` 等——這些檔內既有的 `[DbFact(DatabaseType.X)]` pattern 會自動套到 MySQL，本機設好連線字串後就會跑（未設則 skip）。

## 共通基礎建設修改（由本子 plan 順帶完成）

依總綱「共通基礎建設修改」清單：

1. **`DbGlobalFixture.GetSeedExpressions()` 補 MySQL case**（總綱第 1 項）：
   ```csharp
   case DatabaseType.MySQL: return ("UUID()", "CURRENT_TIMESTAMP(6)");
   ```
2. **Registry 註冊**（總綱第 2 項）
3. **`.runsettings` MySQL placeholder**（總綱第 3 項）
4. **`GetSeedExpressions()` 加註解**（總綱第 4 項）：提醒「新 DB 必須在此補 case」

Oracle case 留給其子 plan 補完。

## 測試規劃

### CI（純語法）

- 執行範圍：`MySql*Tests` 系列（4 個檔）
- 依賴：無連線、無 driver 載入（builder 純字串輸出）
- 預期：與既有 SQLite / SQL Server 純語法測試同樣 ms 級執行時間

### 本機（整合）

- **本機環境**：MySQL 8.0 透過 Docker（與既有 SQL Server / PostgreSQL container 並列）
  ```bash
  docker run -d --name mysql8 \
    -e MYSQL_ROOT_PASSWORD=BeeTest_Pass123 \
    -e MYSQL_DATABASE=common \
    -e MYSQL_USER=testuser \
    -e MYSQL_PASSWORD=testpass \
    -p 3306:3306 \
    mysql:8.0 \
    --default-authentication-plugin=mysql_native_password
  ```
- 資源占用估計：RAM 500MB–1GB、磁碟 ~600MB，Docker Desktop 預設配置即可容納
- 啟用方式：`.runsettings` 取消註解 `BEE_TEST_CONNSTR_MYSQL` 並填入連線字串
- `test.sh` 是否要加自動偵測 MySQL container：先不加，等使用者實際採用後再評估（避免提前複雜化）

### 不涵蓋的測試類型

- **跨 DB 一致性測試**：暫不引入「同一查詢在多 DB 結果一致」的測試框架；個別 DB 的整合測試各自獨立
- **效能基準測試**：本計畫不處理

## 驗收標準

- [ ] 純語法測試 4 個檔全綠（CI 通過）
- [ ] 本機 MySQL 8.0 環境下，所有 `[DbFact(DatabaseType.MySQL)]` 整合測試通過
- [ ] 未設 `BEE_TEST_CONNSTR_MYSQL` 時，相關 `[DbFact]` Skipped（非 Failed）
- [ ] `DbGlobalFixture.GetSeedExpressions()` 對 MySQL 不再 throw
- [ ] CI 全綠（既有三種 DB 不受影響）
- [ ] `Bee.Db` 對 MySQL 的支援文件更新（如有 README / development-cookbook 提及 DB 列表）
- [ ] commit message：`feat(Bee.Db): add MySQL provider and dialect`

## 風險與緩解

| 風險 | 影響 | 緩解 |
|---|---|---|
| `MysqlTableSchemaProvider` 是工作量最大檔（~400 行 INFORMATION_SCHEMA 查詢） | 中 | 優先參考 `PgTableSchemaProvider` 而非 SQLite（PG 也走 INFORMATION_SCHEMA） |
| MySQL 版本差異（8.0 vs 5.7 vs 9.x）造成 INFORMATION_SCHEMA 欄位差 | 低 | 鎖定 8.0+；測試於 8.0 LTS 版進行 |
| `LAST_INSERT_ID()` 取回新 PK 時跨連線安全問題 | 中 | 沿用 ADO.NET 標準 pattern（同 connection / transaction 內取回），與 SQL Server `SCOPE_IDENTITY()` 同層次 |
| `utf8mb4_0900_ai_ci` collation 在較舊 server 不存在 | 低 | 8.0 已是預設，不額外處理；遇舊 server 由使用者調整 |
| `CHAR(36)` UUID vs `BINARY(16)` 效能差異 | 低 | 採可讀性優先（CHAR(36)）；索引長度需求未來如成為瓶頸再評估改 BINARY(16) |
| MySQL 的 `DATETIME` 不含時區 | 低 | 框架層面已假設應用層處理時區；不引入 `TIMESTAMP` 增加複雜度 |

## 參考

- 既有 dialect 實作 pattern：[`src/Bee.Db/Providers/Sqlite/`](../../src/Bee.Db/Providers/Sqlite/)
- INFORMATION_SCHEMA 查詢範本：[`src/Bee.Db/Providers/PostgreSql/PgTableSchemaProvider.cs`](../../src/Bee.Db/Providers/PostgreSql/PgTableSchemaProvider.cs)
- ALTER 能力對照：[`src/Bee.Db/Providers/SqlServer/SqlAlterCompatibilityRules.cs`](../../src/Bee.Db/Providers/SqlServer/SqlAlterCompatibilityRules.cs)
- MySqlConnector 文件：https://mysqlconnector.net/
