# 計畫：補齊 Oracle quoted-lowercase 整合測試

**狀態：✅ 已完成（2026-04-29）**

## 完成摘要

實作過程中**意外揪出一個 production 漏洞**：[DbCommandSpec.CreateCommand](../../src/Bee.Db/DbCommandSpec.cs) 直接把 .NET `Guid` 與 `DbType.Guid` 塞進 `OracleParameter`，Oracle.ManagedDataAccess 全部拒絕（Oracle 無原生 UUID type，框架對映為 `RAW(16)` 對應 `DbType.Binary`）。任何透過 framework FormCRUD + Guid PK 對 Oracle 跑 INSERT/UPDATE/DELETE 都會失敗，但既有測試從未觸發此路徑（fixture seed 用 `SYS_GUID()` SQL 表達式而非 .NET Guid 參數）。

### 落地內容

- **production 修正**：`DbCommandSpec.NormalizeParameterValue` / `NormalizeDbType` 兩個 internal helper，對 Oracle 路徑將 `Guid → byte[]`、`DbType.Guid → DbType.Binary`，其他 DB 不受影響
- **新增單元測試**：8 個 `[Theory]`/`[Fact]` 涵蓋 Oracle/非 Oracle、Guid/非 Guid 各組合
- **新增整合測試**：4 個於 [tests/Bee.Db.UnitTests/OracleIntegrationTests.cs](../../tests/Bee.Db.UnitTests/OracleIntegrationTests.cs)
  - `FormCrud_QuotedLowercaseTable_InsertSelectUpdateDelete_Succeeds`
  - `Join_QuotedLowercaseTables_FromBuilderEmitsExecutableSql`
  - `ReservedWordFieldName_QuotedLowercase_CrudSucceeds`（覆蓋 `comment`、`order` 等 Oracle reserved word）
  - `AlterAddColumn_QuotedLowercaseTable_Succeeds`（原計畫的 `AlterAndRebuild`，因 Rebuild 端到端執行涉及單獨設計議題而縮限，見下節）
- **mutation test 已驗證**：分別將 `DbFunc.QuoteIdentifier(Oracle)` 與 `OracleSchemaHelper.QuoteName` 改為回傳 raw 名稱，4 個整合測試全數失敗；revert 後全綠。確認測試確實鎖到 quoting 行為而非碰巧通過

### 範圍變更：Rebuild 端到端執行

原計畫第 4 個測試包含「呼叫 `OracleTableRebuildCommandBuilder.GetCommandText()` 產生 rebuild SQL → `DbAccess.Execute` → 驗證結果」。實作時發現 Rebuild 輸出是**多語句腳本**（混合 PL/SQL anonymous blocks + 純 SQL `INSERT`/`ALTER`/`CREATE INDEX`），Oracle.ManagedDataAccess **單一 OracleCommand 只能執行一條語句**。

兩種解法（皆超出本計畫範圍）：
1. **builder API 改動**：`GetCommandText` → `GetStatements` 回傳 `IEnumerable<string>`，呼叫端 foreach 執行（與 `OracleTableAlterCommandBuilder.GetStatements` 對齊）；MySQL/PostgreSQL/SQLite 也得跟著改
2. **builder 內部包裝**：把整個 rebuild 包成單一 PL/SQL anonymous block（每條原 statement 變成 `EXECUTE IMMEDIATE '...'`），需處理 nested block 與 escape

目前沒有任何呼叫端真正端到端執行 Rebuild（`Bee.Repository` / `Bee.Business` 都未包裝），也就是這條路徑**production 上尚未啟用**，因此把 Rebuild 端到端驗證留待未來啟用前另案處理。本計畫保留 Alter（已採 GetStatements 模式可行）的整合測試。

### 連帶清理

mutation test 過程中產生 4 個 unquoted (uppercase) orphan tables（`TB_IT_*`），revert 後手動 DROP 清掉。整合測試本身的 `DropQuotedTable` helper 只認 quoted 名稱，正常情況不會留 orphan；只有「測試本身故意關掉 quoting」這種非常路徑會殘留。

---


## 背景

Bee.Db 的 Oracle 支援採用 **quoted-lowercase** identifier 策略（`"st_user"`），與 SQL Server / PostgreSQL / SQLite 的 lowercase 慣例對齊，使 FormSchema 在跨 DB 間 schema 名稱統一。代價是：Oracle 上**每一支由 framework 產生的 SQL（DDL、CRUD、JOIN、WHERE、ORDER BY）對所有 identifier 都必須加雙引號**，少一處就會 ORA-00942 / ORA-00904。

### 現況評估（2026-04-29 完整掃描）

**Quoting 機制**：完整、無漏洞。所有 SQL 產生點統一走兩個入口：

- `DbFunc.QuoteIdentifier(DatabaseType, string)`（[src/Bee.Db/DbFunc.cs:52](../../src/Bee.Db/DbFunc.cs)）—— CRUD / SelectBuilder / FromBuilder / WhereBuilder / SortBuilder / TableSchemaCommandBuilder
- `OracleSchemaHelper.QuoteName(string)`（[src/Bee.Db/Providers/Oracle/OracleSchemaHelper.cs:24](../../src/Bee.Db/Providers/Oracle/OracleSchemaHelper.cs)）—— Oracle 專屬 DDL（CREATE / ALTER / REBUILD / INDEX / COMMENT）

**單元測試**：80 個 Oracle 相關語法測試覆蓋完整（CREATE/ALTER/CRUD 語法產出皆有驗）。

**實際 gap**：整合測試只有 3 個（[tests/Bee.Db.UnitTests/OracleIntegrationTests.cs](../../tests/Bee.Db.UnitTests/OracleIntegrationTests.cs)），且全部聚焦在「schema reader」與「session NLS case-insensitive 比對」，**沒有任何一個整合測試實際對 quoted-lowercase 表跑完整的 INSERT/SELECT/UPDATE/DELETE 鏈路**。等於 framework 層的 quoting 正確性目前只靠語法測試（產出符合預期的字串）保證，沒有「這條 SQL 拿到真實 Oracle 上能跑」的驗證。

風險場景：
- 哪天有人改 SQL builder 不小心漏 quote 一處（如 alias 後加上欄位、或新增的 EXISTS / 子查詢路徑）
- 純語法測試只比對固定字串樣板，可能無法察覺跨 builder 邊界的漏洞
- 在 oracle23ai container 上手動驗證才會浮現，但目前 CI 是 manual workflow，不會每次跑

## 目標

補一組對 quoted-lowercase 表「**跑得起來**」的整合測試，鎖住 framework quoting 鏈路。後續若有人改 SQL builder 不小心漏 quote，跑 Oracle 整合測試立刻失敗。

不在範圍：

- 改 production code（掃描已確認無漏洞）
- 寫新的 ADR（quoting 決策已記在 `docs/archive/plan-oracle-support.md`）

## 範圍：四個整合測試方法

全部加在 [tests/Bee.Db.UnitTests/OracleIntegrationTests.cs](../../tests/Bee.Db.UnitTests/OracleIntegrationTests.cs)，沿用既有 `[DbFact(DatabaseType.Oracle)]` + `[Collection("Initialize")]` 模式。每個測試建立自己的 isolated table（命名 `it_*`，避開 fixture 的 `st_*`），`finally` 清理。

### 1. `FormCrud_QuotedLowercaseTable_InsertSelectUpdateDelete_Succeeds`

**意圖**：驗證 FormSchema 驅動的 CRUD 完整鏈路在 quoted-lowercase 表上能跑。

**步驟**：
1. 用 `OracleCreateTableCommandBuilder` 從一個 `FormSchema`（`tb_it_crud`，含 `sys_rowid` GUID + `name` VARCHAR2 + `qty` NUMBER）產生 CREATE 並執行
2. 用 `OracleFormCommandBuilder` 產生 INSERT、執行
3. 產生 SELECT 並執行，驗證 row 取回
4. 產生 UPDATE 並執行
5. 再 SELECT 驗證更新生效
6. 產生 DELETE 並執行
7. 最後 SELECT 驗證 row 已刪
8. `finally` 中 DROP

**鎖住的範圍**：`InsertCommandBuilder` / `UpdateCommandBuilder` / `DeleteCommandBuilder` / `SelectCommandBuilder` / `FromBuilder` / `WhereBuilder`（WHERE 走 RowId 等值條件）跨 builder 的 quote 一致性。

### 2. `Join_QuotedLowercaseTables_SelectWithJoin_Succeeds`

**意圖**：驗證 RelationField 導出的 JOIN 對兩張 quoted-lowercase 表的 ON 子句 quoting 正確。

**步驟**：
1. 建兩張表：`tb_it_master`（`sys_rowid`、`name`）、`tb_it_detail`（`sys_rowid`、`master_id` FK、`amount`）
2. 各 INSERT 一筆有對應關係的資料
3. 用 `OracleFormCommandBuilder` 產生帶 JOIN 的 SELECT（FormSchema 帶 RelationField）
4. 執行並驗證 JOIN 結果欄位 + row count
5. `finally` 中 DROP 兩張

**鎖住的範圍**：`FromBuilder` 對 join 兩端 table + ON 兩端 field 的 quote。

### 3. `ReservedWordFieldName_QuotedLowercase_CrudSucceeds`

**意圖**：驗證欄位名為 Oracle reserved word（如 `comment`、`size`、`order`）時，quoted 形式能正常 INSERT/SELECT。沒加 quote 會直接是 syntax error。

**步驟**：
1. 建表 `tb_it_reserved`，含 `sys_rowid`、`comment`（VARCHAR2）、`order`（NUMBER）
2. INSERT 一筆
3. SELECT 驗證
4. WHERE 條件用 reserved word 欄位
5. `finally` DROP

**鎖住的範圍**：證實 quote 機制不只是「跨 DB schema 統一」的便利選項，也是 Oracle reserved word 安全的剛性需求。

### 4. `AlterAndRebuild_QuotedLowercaseTable_Succeeds`

**意圖**：驗證 `OracleTableAlterCommandBuilder` 與 `OracleTableRebuildCommandBuilder` 對 quoted-lowercase 表的 ALTER / REBUILD 操作能跑。

**步驟**：
1. 建表 `tb_it_alter`（基本兩欄）
2. 透過 `OracleTableAlterCommandBuilder` 加一個欄位 → 執行
3. 用 `OracleTableSchemaProvider` 讀回 schema，驗證新欄位存在
4. 透過 `OracleTableRebuildCommandBuilder` 對 schema 變更後做 rebuild → 執行
5. 驗證 rebuild 後表仍在、資料保留
6. `finally` DROP

**鎖住的範圍**：DDL 路徑 `OracleSchemaHelper.QuoteName()` 的 alter / rebuild 子功能。

## 共用工具

四個測試都需要 `BuildAndRun(DbCommandSpec)` 與 `DropQuoted(string)` 的小 helper（避免 ORA-00942 在 DROP 不存在的表時噴錯，沿用 [既有 PL/SQL 吞 -942 模式](../../tests/Bee.Db.UnitTests/OracleIntegrationTests.cs)）。Helper 直接放在 `OracleIntegrationTests` private static method，不抽公用類別（一個檔案內共用即可，避免過度抽象）。

## 驗收條件

- [ ] 四個整合測試在 oracle23ai container 上全綠
- [ ] 在 SQL Server / PostgreSQL / MySQL / SQLite 上跑 `./test.sh` 不會誤觸（測試使用 `[DbFact(DatabaseType.Oracle)]`，無 Oracle 連線時自動跳過）
- [ ] 故意把 `DbFunc.QuoteIdentifier(Oracle, ...)` 改成回傳 raw 名稱（不 quote），新增的測試應**全數失敗**（驗證測試確實有鎖到 quoting 行為，而非碰巧通過）
- [ ] 測試完成後恢復 `DbFunc.QuoteIdentifier`，確認重新轉綠

最後一條是 mutation testing 的精神 —— 驗證測試本身有效，不是「跑過就好」。

## 後續（不在本次範圍）

- Manual Oracle CI workflow（[docs/archive/plan-ci-mysql-and-oracle-manual.md](../archive/plan-ci-mysql-and-oracle-manual.md)）已落地，下一步若要把 Oracle 納入 main CI，可參考 [plan-oracle-main-ci-evaluation.md](plan-oracle-main-ci-evaluation.md)。屆時這四個整合測試會自然成為 main CI 的一部分。
