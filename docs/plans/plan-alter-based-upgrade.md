# 計畫：將資料表升級策略改為 ALTER-based 增量升級

**狀態：📝 擬定中（僅藍圖，未定案、未開工）**

## 背景

目前 [SqlCreateTableCommandBuilder.GetUpgradeCommandText():78-104](../../src/Bee.Db/Providers/SqlServer/SqlCreateTableCommandBuilder.cs#L78-L104) 採 **rebuild** 策略：任何 schema 變更都會執行「建 tmp 表 → `INSERT INTO tmp SELECT FROM original` → drop 舊表 → rename」。

此策略在大資料表（千萬筆以上）有顯著問題：
- 全表搬資料耗時（數十分鐘～小時）
- Transaction log 暴漲
- tempdb 壓力
- 期間表級鎖定，業務受影響
- 若表有 FK／trigger／view 相依（目前專案未支援，但擴充時會卡住）

多數 schema 變更（新增欄位、加索引、放大長度）實際上可用 `ALTER TABLE` 在秒級完成，不需搬資料。

## 目標

- 將預設升級策略改為 **ALTER-based 增量升級**
- 僅在必要時 fallback 到 rebuild（如欄位型別不相容變更）
- `TableSchemaComparer` 產出「結構變化清單」（change set），足以驅動 ALTER
- 維持跨 provider 抽象（目前僅 SQL Server，未來 PostgreSQL / MySQL 不致被鎖死）
- 可觀測：能預先顯示「本次升級會執行哪些變化」供人工審核

## 核心挑戰

### 1. 每種變化的 ALTER 策略

| 類別 | 變化 | SQL Server 策略 | 難度 |
|------|------|----------------|------|
| 欄位 | 新增 | `ALTER TABLE ADD` | 低 |
| 欄位 | 刪除 | `ALTER TABLE DROP COLUMN`（資料遺失） | 中，需人工確認 |
| 欄位 | 改型別 | `ALTER TABLE ALTER COLUMN`；不相容時 fallback rebuild | 高 |
| 欄位 | 改 nullable（NULL → NOT NULL） | 需先處理現有 NULL 資料 | 中 |
| 欄位 | 改長度（放大） | `ALTER COLUMN` | 低 |
| 欄位 | 改長度（縮小） | 可能失敗；fallback rebuild 或拒絕 | 中 |
| 欄位 | 改 default | 先 `DROP CONSTRAINT` → `ADD CONSTRAINT` | 中 |
| 欄位 | 改名 | `sp_rename`；但 Comparer 目前無法偵測（視為 add + 殘留） | 高，需 naming convention |
| 索引 | 新增 / 刪除 | `CREATE INDEX` / `DROP INDEX` | 低 |
| 索引 | 改定義 | drop + recreate | 中 |
| PK | 改動 | drop constraint → add constraint；FK 相依時更複雜 | 高 |
| 註解 | 同步 | 已由 [plan-tableschema-description.md](plan-tableschema-description.md) 涵蓋 | — |

### 2. `TableSchemaComparer` 重構

目前只輸出三態 `UpgradeAction.None/New/Upgrade`，不足以驅動 ALTER。需要：

- 產出結構化 **change set**（`AddFieldChange`, `AlterFieldTypeChange`, `DropIndexChange`, ...）
- 判斷每個 change 是否能走 ALTER，或必須 fallback rebuild
- 產出 **rebuild 必要性旗標**：若任何一個 change 需要 rebuild，整表 fallback

保留現有 `UpgradeAction` 作為 legacy 欄位？或直接取代？需在實作時決定 API 演進策略。

### 3. 欄位改名偵測

ALTER 策略下，欄位改名必須能被偵測，否則會走成「加新欄位 + 殘留舊欄位（資料仍在）」或「加新欄位 + drop 舊欄位（資料遺失）」。

候選方案：
- **A. `DbField` 加 `OriginalFieldName` 屬性**：改名時設定，Comparer 偵測到此屬性有值時走 `sp_rename` 路徑
- **B. Field Key（如 GUID）**：每個欄位有不變的 key，name 只是顯示用 — 需要大改定義結構
- **C. 只靠啟發式（型別+順序推測）**：不可靠，不建議

傾向 A。需決定 `OriginalFieldName` 的生命週期（何時清除、何時保留作為審計）。

### 4. Rebuild fallback 規則

哪些情況必須 fallback rebuild？初步列舉：

- 欄位型別不相容變更（如 `String(50)` → `Integer`）
- 欄位長度縮小（可能資料遺失）
- PK 改動且有 FK 相依（若支援 FK 時）
- 要求明確的 fallback 決策（config 或 runtime option）

### 5. 不可回滾變化的處理

ALTER `DROP COLUMN` 一旦執行資料就沒了。應提供：
- **Dry-run 模式**：只輸出 SQL，不執行，供人工審核
- **強制確認機制**：destructive 變化（drop column、縮小長度）預設拒絕，需明確 flag

### 6. 跨 Provider 抽象

SQL Server、PostgreSQL、MySQL 的 ALTER 語法差異極大（特別是改 default、加 NOT NULL 欄位）。需：
- `ITableAlterCommandBuilder` 介面
- Provider-specific 實作（先做 SQL Server，其他 provider 待擴充時再加）
- `change set` 型別與 provider 解耦（純定義資訊，不含 SQL）

### 7. 事務與原子性

- 多個 ALTER 是否要包在一個 transaction？
- SQL Server 的 DDL 大多支援 transaction，但某些操作（如 `ALTER INDEX REBUILD`）不建議長 transaction
- 失敗時的回滾策略？

### 8. 索引與欄位變更順序

ALTER 多個變化時順序很重要：
- 刪索引 → 改欄位 → 建新索引（避免改欄位時索引阻擋）
- 先加欄位 → 填 default/backfill → 改 NOT NULL
- 這些規則要編碼進 orchestrator

## 影響範圍

預估 5-10 個檔案：
- **擴充**：`TableSchemaComparer`（大改 API）
- **新增**：`ITableAlterCommandBuilder` 介面、`SqlTableAlterCommandBuilder` 實作
- **新增**：Change set 型別（`TableChange`, `FieldChange`, `IndexChange` 等）
- **重構**：`SqlCreateTableCommandBuilder`（去除 rebuild 邏輯，只負責 CREATE）或新增 `SqlTableRebuildCommandBuilder` 保留 rebuild 作為 fallback
- **新增**：`TableUpgradeOrchestrator`（決定 ALTER vs rebuild、串接 change 執行順序）
- **影響**：`DbField` 可能需要加 `OriginalFieldName`
- **測試**：大量單元 + 整合測試

## 需先決定的設計問題

在開工前需逐一確認：

1. **Comparer API 演進**：新增欄位還是重構？是否保留 `UpgradeAction` 的 legacy 相容？
2. **欄位改名機制**：採 `OriginalFieldName` 還是 Field Key？
3. **Destructive 變化的預設行為**：拒絕 / 警告 / 執行？
4. **Dry-run 介面**：輸出 SQL 字串？輸出結構化 change list？兩者皆可？
5. **Transaction 策略**：整體包 transaction，還是 per-change？
6. **Rebuild 的定位**：只作為不得已 fallback，還是保留使用者主動選擇？
7. **Provider 抽象時機**：這次只做 SQL Server，介面先不抽？還是一開始就抽？
8. **FK／trigger／view 相依性**：本專案目前不支援，是否維持不支援？若將來要加需預留擴展點。
9. **版本升級路徑**：既有使用者升級此版本後，行為是否相容？需不需要 migration 指引？

## 建議開工前的動作

1. 先以 issue / RFC 形式針對上述 9 個設計問題逐一討論、落定
2. 落定後重寫此 plan 為可執行版本（含具體步驟、檔案清單、驗收條件）
3. 拆成多個 PR：
   - PR 1：Change set 型別 + `TableSchemaComparer` 新 API（不改 behavior）
   - PR 2：`SqlTableAlterCommandBuilder` 基本變化（add field、add/drop index）
   - PR 3：進階變化（alter column、drop field、rename）
   - PR 4：orchestrator + rebuild fallback 整合
   - PR 5：預設策略切換 + 文件

## 未涵蓋／待日後處理

- FK、trigger、view 相依性管理
- 多 schema 支援
- Online schema change（不鎖表的大型變更，如 PostgreSQL pg-osc、SQL Server 的 online index rebuild）
- 升級失敗的自動回復（目前 rebuild 策略有 tmp 表隔離，ALTER 路徑失敗回復更難）

## 參考

- 現行 rebuild 邏輯：[SqlCreateTableCommandBuilder.GetUpgradeCommandText()](../../src/Bee.Db/Providers/SqlServer/SqlCreateTableCommandBuilder.cs#L78-L104)
- 現行 comparer：[TableSchemaComparer](../../src/Bee.Db/Schema/TableSchemaComparer.cs)
- 相關計畫：[plan-tableschema-description.md](plan-tableschema-description.md)