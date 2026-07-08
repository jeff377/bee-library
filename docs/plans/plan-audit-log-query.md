# 計畫：稽核日誌查詢（`st_log_*` 讀取 / 檢視）

**狀態：📝 擬定中（2026-07-08）**

> 稽核軌跡的**讀取側**。寫入側（項 0–4）已完成、上線；設計理由見 [ADR-027](../adr/adr-027-audit-trail.md)、母計畫 [plan-audit-trail.md](plan-audit-trail.md)。
> 本 plan 提供「查詢 / 檢視稽核記錄」的 API；**待逐項定案後再實作**（沿用專案「出細部 plan → 逐點定案 → 執行」節奏）。

---

## 1. 背景

寫入已就緒，log 資料庫現有 5 張 append-only、去正規化自足的表：

| 表 | 內容 | 主要查詢鍵 |
|----|------|-----------|
| `st_log_login` | 登入 / 登出 / 失敗 / 鎖定 | `user_id`、`log_time`、`event` |
| `st_log_change` | 資料異動（`changes_xml` = DataSet DiffGram 新舊值） | `prog_id` + `row_key`、`user_id`、`log_time`、`change_kind` |
| `st_log_access` | 檢視記錄（誰看了哪筆） | `prog_id` + `row_key`、`user_id`、`log_time` |
| `st_log_anomaly_api` | API 異常 | `method`、`anomaly_kind`、`log_time` |
| `st_log_anomaly_db` | DB 異常 | `database_id`、`anomaly_kind`、`log_time` |

尚無讀取路徑。BO / repository / API 慣例可重用：`FilterNode`/`FilterGroup`/`FilterCondition`（[Bee.Definition.Filters](../../src/Bee.Definition/Filters/)）、`PagingInfo`、`SortField`、BO 查詢方法樣式 `GetXxxResult GetXxx(GetXxxArgs)`、`DbScope.Log` 路由。

---

## 2. 查詢使用情境（驅動設計）

1. **記錄異動歷程（最高價值）**：某 `prog_id` + `row_key` 的所有異動（`st_log_change`），並把 `changes_xml`（DiffGram）**還原為結構化新舊值**呈現——「這張單被誰在何時、把哪個欄位從什麼改成什麼」。
2. **誰看過某筆記錄**：`st_log_access WHERE prog_id + row_key`。
3. **登入稽核**：某使用者 / 期間的登入事件（含失敗 / 鎖定）。
4. **異常監控**：近期 API / DB 異常，按 `anomaly_kind` / 時間 / method / database_id。
5. **通用列表**：任一 log 表以共通條件（user、期間、prog_id、row_key、kind）+ 分頁查詢。

---

## 3. 設計要點與待定案

- **Q1 查詢層形狀**：
  - (a) 專屬 `LogBusinessObject`（新 progId，如 `Log` / `AuditLog`）集中所有查詢方法（**建議**，語意集中、獨立權限）。
  - (b) 併入 `SystemBusinessObject` 加方法。
  - (c) 每張 log 表一個 read-only `FormSchema` + 標準 `GetList`（重用既有 grid；但 log 在 `log` scope、且 `changes_xml` 需特別呈現，read-only 表較難處理 DiffGram 還原）。
- **Q2 方法集**（建議起手）：`GetRecordHistory(progId, rowKey)`（情境 1）、`GetChangeLog(filter)`、`GetLoginLog(filter)`、`GetAccessLog(filter)`、`GetAnomalyLog(filter, layer)`。共通 `LogQueryArgs`（filter + paging + sort）。
- **Q3 DiffGram 還原**：`changes_xml` → 結構化新舊值——
  - (a) **server 端解回 DataSet**，回傳結構化 before/after（欄位 → 舊值 / 新值）（**建議**，client 零解析、可跨前端；注意 AOT/XmlSerializer 相容見 ADR-025）。
  - (b) 回傳原始 `changes_xml`，由 client 解。
- **Q4 log DB 路由 / 跨年**：先**單一 log DB**（`DbScope.Log` → 固定 `"log"`）；年份分庫（`log_YYYY`）跨年聚合列為後續（讀取端聚合多 DatabaseItem，見 [database-settings-guide 情境 4](../database-settings-guide.md)）。
- **Q5 權限**：稽核查詢的存取控制——auditor 角色 / 專屬 permission model（`ApiAccessControl` + 權限模型），**唯讀**。避免一般使用者讀他人軌跡。
- **Q6 分頁 / 上限**：大量記錄必分頁（重用 `PagingInfo`）；預設排序 `log_time DESC`；查詢須有時間 / 鍵條件避免全表掃。
- **Q7 read-only**：純查詢，不提供任何改寫（log append-only）。

---

## 4. 掛勾 / 影響檔案（預估）

- 新 BO（`LogBusinessObject`）+ contract / args / result / wire（依 `bee-add-bo-method` skill 的跨層流程）。
- log-scope repository（`DbScope.Log`，`DbCommandSpec` 參數化查詢，禁字串拼接 SQL）。
- DiffGram 還原 helper（`changes_xml` → DataSet → 結構化 before/after）。
- 權限模型 / `ApiAccessControl` 標註。
- 測試：`[DbFact]`（SQL Server / PostgreSQL）寫入樣本 → 查詢讀回；DiffGram 還原單元測試；filter / 分頁單元測試。

---

## 5. 分階段

| 階段 | 範圍 |
|------|------|
| 1 | **記錄異動歷程**：`GetRecordHistory(progId, rowKey)` + DiffGram 還原（情境 1，最高價值） |
| 2 | 各 log 表**通用列表查詢**：共通 filter + 分頁 + 排序（情境 2–5） |
| 3 | 異常監控查詢、跨年 `log_YYYY` 聚合、權限強化 |

---

## 6. 相依與約束

- 前置：稽核寫入（項 0–4，已完成）。
- 安全：查詢 SQL 一律 `DbCommandSpec` 參數化（`scanning.md`）；回傳不外洩非授權資料（權限 Q5）。
- 對齊：`bee-add-bo-method`、`bee-add-form`（若走 read-only FormSchema 路線）等既有 skill 與慣例。
- 相關：[ADR-027](../adr/adr-027-audit-trail.md)（寫入側設計）、[framework-reserved-names §1.3](../framework-reserved-names.md)、[ADR-025](../adr/adr-025-define-types-aot-xmlserializer-compat.md)（DiffGram 還原若涉 XmlSerializer / AOT）。
