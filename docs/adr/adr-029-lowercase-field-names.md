# ADR-029：欄位名稱一律小寫（定義 / 資料 / UI 三層一致）

## 狀態

已採納（2026-07-09）

> 原則即刻生效（欄名一律小寫 `snake_case`）。記憶體 `DataSet` 欄名已由大寫**遷移為小寫**（`AddColumn` / `LowercaseColumnNames` 於 `DbAccess` 讀取邊界套用），此為破壞性 wire 變更、第一方 client 已同步；執行與剩餘回歸見 `docs/plans/plan-dataset-lowercase-columns.md`。

## 背景

欄名的「大小寫」在系統各層若不一致，會反覆造成問題，因為**有多個子系統以欄名字串、且區分大小寫做比對**：

- **早期 UI 控制項資料繫結** 區分大小寫。框架當時的解法是把記憶體 `DataSet` 欄名一律正規化為**大寫**（讀取資料庫後 `UppercaseColumnNames()`、以及 `DataTableExtensions.AddColumn` 內 `ToUpper()`），讓繫結一致。
- **運算式引擎（ADR-028）** 的識別字區分大小寫（DynamicExpresso）。`FormExpressionCalculator.BuildVariables` 一度以大寫 `DataColumn.ColumnName` 當變數 key，但運算式引用的是宣告的小寫欄名（如 `quantity`）→ `UnknownIdentifierException`；伺服器存檔時未處理即成 JSON-RPC `-32000`。

這兩次是**同一類問題**：大小寫敏感的名稱比對，遇上「同一欄名在不同層有不同大小寫」。資料庫命名規範（見 `docs/database-naming-conventions.md` §1–2）本就要求全小寫 `snake_case`，`FormField.FieldName` 慣例上也是小寫；不一致的只有「記憶體 `DataSet` 欄名存大寫」這個歷史正規化，而它還會透過序列化洩漏到 wire。

## 考慮過的選項

1. **逐一為每個區分大小寫的消費端解耦**（現況補丁式）：如運算式引擎改以 `FormField.FieldName` 綁定變數（已於 commit 96821c04 修好）。**部分採納作為當前止血**——它讓運算式層與 DataSet 儲存大小寫解耦、正確且零風險；但無法根治「未來新子系統若做大小寫敏感比對又要各自解耦」。

2. **全面 case-insensitive**：不動儲存大小寫，改讓每個名稱比對點都大小寫無關。**否決為長期方向**——沒達成「三層欄名字面一致」，且「哪裡仍大小寫敏感」需持續盯防，容易再出漏網。

3. **全層小寫正規化（採納）**：讓定義（`FormField.FieldName`）、資料（實體 DB + 記憶體 `DataSet`）、UI 三層欄名一律小寫 `snake_case`，即資料庫既有的寫法。單一標準大小寫，從源頭消除整類問題。**代價**：記憶體 `DataSet` 欄名由大寫改小寫會改變 wire 上的欄名（JSON / MessagePack payload key），破壞現有讀大寫 key 的 JS/TS 前端 → 屬破壞性變更，須於 major 版邊界協調釋出。

## 決策

**欄位名稱在所有層一律採小寫 `snake_case`，作為系統唯一的標準寫法**：

| 層級 | 欄名載體 |
|------|---------|
| 定義 | `FormField.FieldName`、`DbField.FieldName`、`TableSchema` 欄位 |
| 資料（實體） | 資料庫表欄位 |
| 資料（記憶體） | `DataSet` / `DataTable` 的 `DataColumn.ColumnName`（**目標狀態；遷移中**） |
| 運算式 | `ValueExpression` / `FormRule.Condition` 內識別字＝精確的宣告 `FieldName` |
| UI | 欄位編輯器／表格欄繫結 key |

配套原則：

- **撰寫端**：schema / layout / 運算式各處一律以小寫 `snake_case` 寫欄名。
- **程式端**：比對欄名一律大小寫無關（`DataColumnCollection` 索引器本就如此）；**禁止**依賴特定大小寫做字面比較。
- **運算式綁定**：`BuildVariables` 以 `FormField.FieldName`（宣告大小寫）為變數 key，與 `DataColumn` 儲存大小寫解耦——即使記憶體 DataSet 尚未遷移到小寫，運算式仍正確（此為選項 1 的止血，長期在選項 3 落地後自然一致）。

## 後果

- **正向**：欄名全系統只有一種寫法；消除「大小寫敏感名稱比對」整類 bug 的根源；新子系統無需各自解耦；wire 上的欄名對齊 DB / schema，對前端消費端長期更直覺（`row.current.sys_rowid`）。
- **已執行（破壞性）**：記憶體 `DataSet` 欄名由大寫改小寫是 **wire breaking change**——影響 JSON + MessagePack 兩種 payload 的 key、第一方與第三方 JS/TS 前端、以及變更稽核既有 DiffGram 歷史（欄名大寫）。落地情形：
  - 核心切換（`AddColumn` / `LowercaseColumnNames` / `DbAccess`）＋第一方前端（`Web.Js.Demo`）已同步；wire converter 直出 `ColumnName` 故自動小寫。
  - 稽核既有資料以「解析端相容新舊大小寫」處理（下游比對本就大小寫無關），不回填改寫不可變的稽核歷史。
  - **前置稽核已完成**：C# 端 0 處大寫字面比較（皆走大小寫無關 `DataColumnCollection`）；Avalonia head 繫結大小寫無關；其餘 UI head（WinForms / Blazor / MAUI）尚未實作，趁此時遷移使其天生一致。
  - **剩餘**：多 DB provider 容器全回歸（SQLite 已驗證）；**發佈時**於 CHANGELOG 標 breaking + 附遷移指南（依 `releasing.md`，CHANGELOG 累積至發版統整）。
- **執行載體**：`docs/plans/plan-dataset-lowercase-columns.md`（含 Phase 0 稽核 → 決策 → 核心切換 → wire/前端 → 相容回歸）。

## 相關

- ADR-028（自訂運算式與規則引擎）——大小寫敏感比對第二次咬人的來源。
- `docs/database-naming-conventions.md` §1–2、§6——欄名小寫規範與跨層一致性。
- `docs/plans/plan-dataset-lowercase-columns.md`——本 ADR 的執行計畫。
