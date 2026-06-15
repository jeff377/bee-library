# ADR-024：DataForm 持久化改走 DataTable 級 DataAdapter

## 狀態

已採納（2026-06-15）

## 背景

[ADR-001](adr-001-dataset-as-dto.md) 以 DataSet 承載 master-detail；`DataFormRepository.Save` 負責把 DataSet 的變更落庫。原作法是**逐列**判斷 `DataRow.RowState`、用 `IFormCommandBuilder` 動態組每列 SQL（UPDATE 只含「有變更的欄位」），再 batch 執行。

此設計有一個結構性缺陷：一個 `Modified` 但**實際無欄位變更**的列（grid 重新 realize、重算回寫相同值等情境造成 RowState=Modified 但每格 Original==Current），逐列 builder 會組出**空的 SET 子句** → 框架丟 `UPDATE would be empty`。這正是 `apps/Bee.Northwind` 訂單刪明細後重存失敗的根因——主表雖無實質變更卻被判 Modified。當時以 `ComputeAmounts` guard 緩解主表，但底層仍脆弱：任何「Modified 但無變更」的列都會踩到。

## 考慮過的選項

1. **逐列特判「無變更則跳過」**：在逐列 builder 前先 diff Original vs Current，全等就 skip。治標——每個 RowState 路徑都要記得加判斷，且空 SET 的風險面仍在（未來新增寫入路徑容易再踩）。
2. **改用 ADO.NET `DataAdapter.Update`（採用）**：每張 DataTable 建立 Insert/Update/Delete 三個**全欄位參數化**命令，交給 `DataAdapter.Update` 依 RowState 套用。Modified 列以「全欄位 UPDATE」回寫，即使值相同也只是無害的同值更新，**結構上不可能產生空 SET**——整類 bug 從設計面消除，不需逐列特判。
3. **換 ORM**（EF Core 等）：編輯模型自帶變更追蹤，但與框架「DataSet-as-DTO、定義驅動」的核心模型衝突，重構量不成比例。

## 決策

採**選項 2**：

- `DataFormRepository.Save` 依 `[master, details...]` 順序，每張表 `FormTable.GenerateDbTable()` 取 `TableSchema` → `TableSchemaCommandBuilder.BuildUpdateSpec(dataTable)` 產出 `DataTableUpdateSpec`（Insert/Update/Delete 三命令 + DataTable，參數以 `SourceColumn`/`SourceVersion` 綁定 DataColumn）。
- **跨表原子性（D1）**：新增 `DbAccess.UpdateDataTables(IReadOnlyList<DataTableUpdateSpec>)`——開**單一交易**依序對每個 spec 跑 `DataAdapter.Update`，全成功才 commit。master-detail 一次 Save 涉及多表，單一交易確保半途失敗不留部分資料；FK 順序天然正確（master 先 insert，明細才 insert）。
- **無變更為 no-op（D2）**：整個 DataSet 無 pending changes 時回傳 0，不再丟例外（對齊 `DataAdapter` 慣例）。
- **SQLite adapter 補位**：Microsoft.Data.Sqlite **不提供 `DbDataAdapter`**（`CreateDataAdapter()` 回 null），而 SQLite 是 demo／多數本機測試用庫。原評估的「無 adapter → 手動逐列套用預建命令」fallback，最終改以**自製 `SqliteDataAdapter`**（經 `SqliteProviderFactory` 包裝）解決——5 個 provider（SQL Server／PostgreSQL／SQLite／MySQL／Oracle）**統一走 adapter 路徑**，無 provider 專屬分支。
- **退役逐列 builder（D3）**：Save 改寫後，逐列 `InsertCommandBuilder` / `UpdateCommandBuilder` 在 production 已無使用者，從 `src/Bee.Db/Dml` 移除（relocate 為 `tests/Bee.Tests.Shared/` 的測試 seeding／round-trip 工具）。`DeleteCommandBuilder` / `SelectCommandBuilder` 仍由 `Delete()` / `GetData()` 使用，保留。

## 影響

- 「Modified 但無欄位變更 → 空 SET → 報錯」整類錯誤從設計面消除；master-detail 重存既有單據不再脆弱。
- 5 個 DB provider 的讀寫路徑一致（皆 `DataAdapter`），少一條 provider 專屬 fallback 程式碼。
- DataSet 內的 `ref_*` RelationField／virtual 欄不在命令參數中，`DataAdapter` 自動忽略，無副作用。
- 唯一 production 呼叫端 `FormBusinessObject.Save`，blast radius 小；`Delete()` / `GetData()` / `GetNewData` / `GetList` 不受影響。
- `Bee.Db` 移除逐列 IUD builder 屬 breaking（見 CHANGELOG 4.10.0），但限框架建構面、無外部消費者。
- 全欄位 UPDATE 比「只更新變更欄」多寫幾欄，對單筆表單 Save 的成本可忽略；換得的是設計面的健壯性。
