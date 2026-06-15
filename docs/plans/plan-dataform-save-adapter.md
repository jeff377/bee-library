# 計畫：DataFormRepository.Save 改用 DataAdapter（UpdateDataTable）

**狀態：🚧 進行中（2026-06-15，決策已定：D1=共享交易多表變體、D2=no-op 回傳 0、D3=移除未用逐列 builder）**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 1 | `DbAccess.UpdateDataTables`（共享交易多表變體 + 無 adapter 手動 fallback） | ✅ 已完成 |
| 2 | 重寫 `DataFormRepository.Save`（TableSchemaCommandBuilder + UpdateDataTables；無變更 no-op 回 0） | ✅ 已完成 |
| 3 | 測試：3 套件全綠 + 新增 manual-apply 整合測試 + NoChanges 改為 no-op | ✅ 已完成 |
| 4 | D3 清理：移除 production 已不用的逐列 UPDATE 路徑 | ⏸️ 改為跟進（見下） |
| 5 | build + 全測試 + Northwind 實機六情境 | 🚧 進行中 |

> **bug 已修復並驗證**：Northwind（SQLite）刪明細 + no-op sibling write-back 的原始錯誤情境通過；
> Db / Repository / Business 三套件全綠。
>
> **D3 改為跟進 PR**：實作後發現逐列 builder 的拆除牽連遠大於預期 ——
> `InsertCommandBuilder` / `UpdateCommandBuilder` 被約 12 個測試檔當「單列 seeding / round-trip 工具」使用，
> 且每個 provider 的 `BuildInsert` / `BuildUpdate` 各有專屬 delegation 測試。本次 Save 改寫已移除唯一的
> **production** 使用者（`CollectRowCommands`），故逐列路徑在 production 已死碼；但完整刪除 class 與
> interface 方法會 churn ~12 個測試檔、且與「修 bug」目標正交、徒增風險。**建議另開乾淨 PR 處理**，
> 不綁進本次修正。

## 背景與動機

`DataFormRepository.Save` 目前逐筆判斷 RowState、用 `IFormCommandBuilder` 動態組每列 SQL
（UPDATE 只含「有變更的欄位」），再 batch 執行。這導致：一個 Modified 但實際無欄位變更的列
（grid re-realize / 重算回寫相同值造成）會組出空 SET → 框架丟 `UPDATE would be empty`。
這正是 Orders 刪除明細後 Save 失敗的根因（已暫時用 ComputeAmounts guard 緩解主表，但底層仍脆弱）。

**新方向（使用者指定）**：改成「每個 DataTable 建立 Insert/Update/Delete 三個參數化命令，
交給 `DbAccess.UpdateDataTable`（ADO.NET `DataAdapter.Update`）異動」。

**為何更好**：`DataAdapter.Update` 對 Modified 列以「全欄位參數化 UPDATE」回寫，
即使值相同也只是無害的同值更新，**不會產生空 SET、不會報錯** —— 整類 bug 由設計面消除，
不需逐列特判。

## ⚠️ 關鍵發現（2026-06-15）：UpdateDataTable 在 SQLite 不可用

實作後實測：`DbAccess.UpdateDataTable` 底層用 ADO.NET `DataAdapter.Update`，但
**Microsoft.Data.Sqlite 不提供 DbDataAdapter**（`Provider.CreateDataAdapter()` 回 null，
程式內 [DbAccess.cs](../../src/Bee.Db/DbAccess.cs) 早有此註記）。實機跑 Northwind（SQLite）Save 直接拋
`DbProviderFactory.CreateDataAdapter() returned null`。既有 `UpdateDataTable` 測試也是
`[DbFact(DatabaseType.SQLServer)]` —— 只在 SQL Server 跑，從未在 SQLite 驗證。

**實測各 provider 的 `CreateDataAdapter()`（probe 過，已更正先前誤判）**：

| Provider | DataAdapter |
|----------|-------------|
| SQL Server（SqlClient） | ✅ `SqlDataAdapter` |
| PostgreSQL（Npgsql） | ✅ `NpgsqlDataAdapter` |
| **SQLite（Microsoft.Data.Sqlite）** | ❌ **null（唯一缺）** |
| MySQL（MySqlConnector） | ✅ `MySqlDataAdapter` |
| Oracle（ODP.NET managed） | ✅ `OracleDataAdapter` |

**只有 SQLite 缺 adapter**（5 個 provider 中唯一），但 SQLite 正是 demo / 多數本機測試用的 DB，
故 fallback 仍為必要；其餘 4 個 provider 走 adapter 路徑。

**結論**：字面上「透過 UpdateDataTable」對 demo / 本機測試用的 SQLite 行不通。
但 DbCommandSpec 參數已帶 `SourceColumn` + `SourceVersion`，且 SELECT 路徑早已有
「無 adapter → 改用 DataReader」的 fallback 慣例。故解法：**讓 UpdateDataTable 在無 adapter 時，
改以手動逐列套用預建的三個參數化命令**（依 RowState 選命令、用 SourceColumn/SourceVersion 從
DataRow 綁值、ExecuteNonQuery），與 DataAdapter 等價但 provider-agnostic。如此「每表三命令、
全欄位 UPDATE、no-op 安全、不逐列組 SQL 字串」的設計意圖完全保留，且全 provider 可跑。

## 既有可用機制（已盤點）

- `FormTable.GenerateDbTable()` → 直接從 FormTable 取得對應 `TableSchema`（DbFields），無需 IDefineAccess。
- `TableSchemaCommandBuilder(dbType, tableSchema).BuildUpdateSpec(dataTable)` → 產出
  `DataTableUpdateSpec`（Insert/Update/Delete 三命令 + DataTable）。Insert 跳過 AutoIncrement；
  Update/Delete 以 `sys_rowid` 的 Original 版本為 WHERE。參數以 SourceColumn 綁定 DataColumn。
- `DbAccess.UpdateDataTable(DataTableUpdateSpec)` → 跑 `DataAdapter.Update`，自帶（可選）交易。
- DataSet 內的 `ref_*` RelationField / virtual 欄位不在命令參數中，DataAdapter 自動忽略，無副作用。
- 唯一 production 呼叫端：`FormBusinessObject.Save`（blast radius 小）。

## 設計重點與待確認決策

### D1 — master-detail 的「跨表原子性」怎麼處理？（關鍵）

`UpdateDataTable` 是「單表 + 自帶單一交易」。master-detail（如 Order）一次 Save 涉及多張表，
需要**一個交易涵蓋全部**，否則明細寫入失敗時主表已 commit → 資料不一致。

- **(a)【建議】新增共享交易的多表變體** `DbAccess.UpdateDataTables(IReadOnlyList<DataTableUpdateSpec>)`：
  開一個交易，依序對每個 spec 跑 `DataAdapter.Update`，全成功才 commit。
  Save 依序傳入 [master, detail...]。原子性與既有 batch 作法一致，且 FK 順序正確
  （master 先 insert，明細才 insert；Save 不刪主表 —— 刪整筆走 `Delete()`，故無「明細需先於主表刪」問題）。
- **(b) 每表各自獨立交易**（最字面簡單）：逐表呼叫 `UpdateDataTable`。實作最少，但**跨表非原子**，
  master-detail 半途失敗會留下部分資料。不建議。

### D2 — 整個 DataSet「無任何變更」時的行為？

現行：丟 `InvalidOperationException`（測試 `Save_Sqlite_NoChanges_Throws` 斷言此行為）。
DataAdapter 自然行為：什麼都不做、回傳 0。

- **(a)【建議】改為 no-op 回傳 0**（符合 DataAdapter 慣例、移除特判），同步更新該測試。
- **(b) 保留丟例外**（維持現有契約，Save 前先檢查 DataSet 有無 pending changes）。

### D3 — 退役逐列 UPDATE 路徑？

改寫後 Save 不再用 `IFormCommandBuilder.BuildUpdate` / `UpdateCommandBuilder`（逐列、只含變更欄位）。
`BuildInsert` 也不再被 Save 使用；`BuildDelete` / `BuildSelect` 仍由 `Delete()` / `GetData()` 使用。

- **(a)【建議】本次先不刪**：保留 `UpdateCommandBuilder` 等（仍有測試覆蓋），只改 Save 的實作；
  待確認無其他依賴後另開 PR 清理。降低本次風險。
- **(b) 一併移除**未再被 production 使用的逐列 builder 與其測試（範圍較大）。

## 實作範圍（待 D1–D3 確認後）

1. （D1=a）`src/Bee.Db/DbAccess.cs`：新增 `UpdateDataTables(IReadOnlyList<DataTableUpdateSpec>)`，
   單一交易跨多表 `DataAdapter.Update`，回傳每表 affected。
2. `src/Bee.Repository/Form/DataFormRepository.cs`：重寫 `Save` —— 依 [master, details] 順序，
   每表 `GenerateDbTable()` → `TableSchemaCommandBuilder.BuildUpdateSpec(dataTable)` →
   交給共享交易執行；組回 `AffectedRows` 字典；保留 `Save` 結尾的 `GetData` 重撈 refreshed。
   移除 `CollectRowCommands` 逐列邏輯。
3. （D2）依選擇調整無變更行為與 `Save_Sqlite_NoChanges_Throws` 測試。
4. 測試：保留並驗證既有 `FormBusinessObjectSaveTests`（Added/Modified/Deleted round-trip）；
   新增「Modified 但無欄位變更 → 不報錯」與（若可）master-detail 明細刪除的整合測試。
5. 以 Northwind Order 實機驗證：刪明細 Save、改主表 Save、改明細 Save、no-op 重存皆正常。

## 驗證

- `./test.sh`（或受影響專案 `--settings .runsettings`）全綠：`Bee.Db` / `Bee.Repository` / `Bee.Business`。
- Northwind 實機四情境（同上）通過。

## 不在範圍

- 不動 `Delete()`（已正確 cascade 明細先於主表）。
- 不動 `GetData` / `GetNewData` / `GetList`。
- 既有 `ComputeAmounts` guard（已 commit）維持；底層修好後它只是額外保險，不衝突。
