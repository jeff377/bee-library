# 計畫：稽核日誌查詢（`st_log_*` 讀取 / 檢視）

**狀態：✅ 已完成（2026-07-08）** — 讀取側主體（情境 1–5）交付完畢；`AuditLog` 軸共 10 個查詢方法。下方「延後」項為有需求再另案，非本計畫範圍。

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 1 | 記錄異動歷程：`GetRecordHistory(progId, rowKey)` + DiffGram 還原（全還原版） | ✅ 已完成（2026-07-08） |
| 2a | change 軸清單/明細二段式：`GetChangeLog`（清單）+ `GetChangeDetail`（明細還原）+ `GetRecordHistory` 改標頭清單（共用明細）；`LogQueryArgs` + 分頁 | ✅ 已完成（2026-07-08） |
| 2b | 其餘軸清單：`GetLoginLog` / `GetAccessLog` / `GetApiAnomalyLog` / `GetDbAnomalyLog`（沿用 2a 的 typed filter + 分頁基礎） | ✅ 已完成（2026-07-08） |
| 3a | 異常彙總：`GetApiAnomalySummary` / `GetDbAnomalySummary` / `GetTopApiMethods`（標準 `GROUP BY`，無 dialect 時間桶） | ✅ 已完成（2026-07-08） |
| — | 延後（有需求再另案，非本計畫範圍）：異常趨勢圖（需 `DateBucketBuilder` dialect helper）、權限強化（auditor 角色 / 欄位級遮罩，等實際 UI/角色需求）、跨年 `log_YYYY` 聚合 | ⏸️ 延後 |

> **跨年 `log_YYYY` 聚合已移出範圍**（2026-07-08）：延後到實際出現年份分庫需求再做（見 §3 Q4）。
> **Phase 2a 會回頭調整 Phase 1 已上線的 `GetRecordHistory` 合約**：從「全還原歷程」改為「標頭清單 + `GetChangeDetail` 取單筆明細」（見 §3 Q2 補充、§8 設計）。因套件未發佈、無外部使用者，此 breaking change 可接受。

> 稽核軌跡的**讀取側**。寫入側（項 0–4）已完成、上線；設計理由見 [ADR-027](../adr/adr-027-audit-trail.md)、母計畫 [plan-audit-trail.md](plan-audit-trail.md)。
> Q1–Q7 已於 2026-07-08 逐項定案（見 §3 決策紀錄），Phase 1 進入實作。

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

## 3. 設計決策紀錄（2026-07-08 定案）

- **Q1 查詢層形狀 → 專屬 `LogBusinessObject`**：新增保留 progId（`AuditLog`，登記於 `SysProgIds`），集中所有 log 查詢方法。理由：語意集中、可掛獨立權限模型、與 System 軸（login / define）職責分離。需新增一條 BO 軸（`LogActions` 常數類、`ILogBusinessObject`、`BoFactory` 註冊）。
- **Q2 方法集 → 每軸 typed 方法 + 共通 `LogQueryArgs`**：`GetRecordHistory(progId, rowKey)`（情境 1）、`GetChangeLog` / `GetLoginLog` / `GetAccessLog` / `GetAnomalyLog`（各 typed，`filter + paging + sort`）。合約面清楚、每軸欄位語意明確。**Phase 1 只做 `GetRecordHistory`**，列表查詢併入 Phase 2。
- **Q2 補充（2026-07-08，Phase 2a 定案）→ change 軸改「清單 / 明細」二段式**：異動記錄量體大、逐列還原 DiffGram 昂貴且 payload 爆，故 change 軸拆兩段：
  - **清單**（`GetChangeLog` / 及改版後的 `GetRecordHistory`）**只回事件標頭**（who / when / kind / progId / rowKey / source / is_sensitive），**不還原 `changes_xml`**；回傳 **`DataTable` + `PagingInfo`**（對齊 `GetList` 慣例）。
  - **明細**（`GetChangeDetail(sysRowId)`）以 `st_log_change.sys_rowid`（Guid）為鍵取單筆，才還原該筆 `changes_xml` 為結構化 before/after（`List<RecordFieldChange>`，沿用 `ChangeDiffGramReader`）。
  - **Phase 1 的 `GetRecordHistory` 一併改為標頭清單**（headers-only + `PagingInfo`），明細統一走 `GetChangeDetail`；原「全還原」版與 `RecordHistoryEntry` typed DTO 退場（`RecordFieldChange` 保留給明細）。分界原則：*未受限的跨記錄查詢用清單+明細；單筆記錄查詢也走清單*（全軸一致）。
- **Q3 DiffGram 還原 → server 端還原為結構化 before/after**：回傳結構化「表 → 列 → 欄位（舊值 / 新值）」，client 零解析、可跨前端。**實作細節**（實測修正）：寫入側 `WriteXml(XmlWriteMode.DiffGram)` 產出的是**無 schema** 的 DiffGram，`DataSet.ReadXml` 讀不回（回 0 tables）。故還原改以 `XDocument` 直接解析 DiffGram（`data` block 為 current、`diffgr:before` 為 original，以 `diffgr:id` 配對；insert / update / delete 三態）——見 `ChangeDiffGramReader`。**仍 AOT 安全**：走 `XDocument` / `XmlReader`（XXE 已 hardening），非 `XmlSerializer` 反射路徑，ADR-025 的 trim/AOT 雷不適用。
- **Q4 log DB 路由 → 單一 log DB（跨年聚合已移出範圍）**：查詢固定走 `DbScope.Log → "log"`（同寫入側）。年份分庫 `log_YYYY` 的跨年讀取聚合**延後、非本計畫範圍**——待實際出現分庫寫入需求再另案評估（讀取端需聚合多個 `DatabaseItem`）。
- **Q5 權限 → `Authenticated` + `AuditLog` 權限 gate**：BO 方法標 `[ApiAccessControl(ApiProtectionLevel.Encrypted, ApiAccessRequirement.Authenticated)]`，方法內再以 `IAuthorizationService.Can(AccessToken, "AuditLog", PermissionAction.Read)` 檢查——須被授予稽核讀取權的角色才可查，避免一般使用者讀他人軌跡。此檢查為 company-scoped（需先 `EnterCompany`、`SessionInfo.Roles` 已快照）。
- **Q6 分頁 → 強制分頁 + 預設 `log_time DESC`**：列表查詢重用 `PagingInfo`，回傳帶分頁資訊；`PageSize` 伺服器端 clamp 上限；查詢須帶時間 / 鍵條件避免全表掃。`GetRecordHistory` 依 `prog_id + row_key` 已天然限縮。
- **Q7 read-only → 純唯讀**：只提供查詢，不提供任何改寫（log append-only）。retention / 清理屬寫入 / 管理面，另案。

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

## 7. Phase 1 實作分解（`GetRecordHistory` + 新 `AuditLog` 軸）

Q1 定案為專屬 BO 軸，故 Phase 1 除方法本身外需新增一條 BO 軸接線（skill 只涵蓋 System/Form 兩軸）。

**新軸接線**
- `src/Bee.Definition/SysProgIds.cs` — 加 `AuditLog` 保留 progId（兼作權限 modelId）
- `src/Bee.Definition/LogActions.cs` — 新增，`GetRecordHistory` 常數
- `src/Bee.Definition/IBusinessObjectFactory.cs` — 加 `CreateLogBusinessObject(token, isLocalCall)`
- `src/Bee.Business/BusinessObjectFactory.cs` — 實作 `CreateLogBusinessObject`
- `src/Bee.Api.Core/JsonRpc/JsonRpcExecutor.cs` — dispatch 加 `AuditLog` 分支

> **命名注意**：資料夾一律用 `AuditLog/`（對映命名空間 `...AuditLog`），**不要**用 `Log/` —— `.gitignore` 的 `[Ll]og/` 規則會把任何 `Log/` 目錄整個忽略掉（`git status` 看不到、CI 缺檔）。`AuditLog` 亦對齊 progId / 軸名，與 `Messages/System`、`Messages/Form` 一致。

**合約 + wire**（巢狀結構化型別放 `Bee.Api.Contracts`，比照 `PackageUpdateInfo`）
- `Bee.Api.Contracts/IGetRecordHistoryRequest.cs`、`IGetRecordHistoryResponse.cs`
- `Bee.Api.Contracts/RecordHistoryEntry.cs`（一筆 `st_log_change` 事件：header + `List<RecordFieldChange>`）
- `Bee.Api.Contracts/RecordFieldChange.cs`（單欄 before/after：TableName、RowKey、RowState、FieldName、OldValue、NewValue）
- `Bee.Api.Core/Messages/AuditLog/GetRecordHistoryRequest.cs`、`GetRecordHistoryResponse.cs`

**BO**
- `Bee.Business/AuditLog/GetRecordHistoryArgs.cs`、`GetRecordHistoryResult.cs`
- `Bee.Business/AuditLog/LogBusinessObject.cs`（`[ApiAccessControl(Encrypted,Authenticated)]` + `IAuthorizationService.Can(token,"AuditLog",Read)` gate + 依 session company 過濾）
- `Bee.Business/AuditLog/ILogBusinessObject.cs`
- `Bee.Business/AuditLog/ChangeDiffGramReader.cs`（`XDocument` 直接解析 DiffGram → `List<RecordFieldChange>`；schemaless DiffGram `DataSet.ReadXml` 解不回，非 XmlSerializer 路徑）

**Repository（log scope）**
- `Bee.Repository.Abstractions/AuditLog/IAuditLogRepository.cs`、`Factories/IAuditLogRepositoryFactory.cs`
- `Bee.Repository/AuditLog/AuditLogRepository.cs`（ctor 接 `IDbConnectionManager` + `databaseId`；`SELECT ... FROM st_log_change WHERE prog_id={0} AND row_key={1} [AND company_id={2}] ORDER BY log_time DESC`，參數化）
- `Bee.Repository/Factories/AuditLogRepositoryFactory.cs`（`CreateAuditLogRepository()` → databaseId = `DbCategoryIds.Log`）
- `Bee.Hosting/BeeFrameworkServiceCollectionExtensions.cs` — 註冊 `IAuditLogRepositoryFactory`

**Client**
- `Bee.Api.Client/Connectors/LogApiConnector.cs`（`GetRecordHistoryAsync`；progId = `SysProgIds.AuditLog`）+ `ClientInfo.CreateLogApiConnector()`

**測試 + surface**
- wire round-trip（`tests/Bee.Api.Core.UnitTests/AuditLog/`）、executor dispatch（stub repo，驗新 `AuditLog` 分支）、BO 邏輯 + DiffGram 還原 + 權限 gate（`tests/Bee.Business.UnitTests/AuditLog/`，stub repo）、`[DbFact]` repo 真 DB（`tests/Bee.Hosting.UnitTests/AuditLogQueryDbFactTests.cs`，SQL Server + PostgreSQL）
- `docs/api-method-reference.md`(+zh-TW) 加 AuditLog 軸；`framework-reserved-names.md`(+zh-TW) §2 登記 `AuditLog` progId；`BoApiSurfaceTests` baseline 加 `LogBusinessObject.GetRecordHistory`

## 8. Phase 2a 設計（change 軸清單/明細 + Phase 1 realignment）

**方法集（`AuditLog` 軸，`LogActions`）**

| 方法 | 形狀 | 說明 |
|------|------|------|
| `GetChangeLog(GetChangeLogArgs)` | `DataTable` + `PagingInfo` | 跨記錄的異動事件清單（標頭欄），依 filter + 分頁 + `log_time DESC`。 |
| `GetRecordHistory(GetRecordHistoryArgs)` | `DataTable` + `PagingInfo` | **改版**：單筆記錄（`progId`+`rowKey`）的異動事件清單（標頭欄）+ 分頁。等同 `GetChangeLog` 以 `progId`+`rowKey` 限縮，但保留為高價值具名便捷 API。 |
| `GetChangeDetail(GetChangeDetailArgs)` | typed（header + `List<RecordFieldChange>`） | 以 `sysRowId`（`st_log_change.sys_rowid`）取單筆，還原 `changes_xml` 為結構化 before/after。 |

**清單標頭欄位（DataTable）**：`sys_rowid`、`log_time`、`user_id`、`user_name`、`company_id`、`company_name`、`prog_id`、`row_key`、`change_kind`、`is_sensitive`、`source`（**不含** `changes_xml`）。

**filter 形狀（定案：typed 欄位，非 `FilterNode`）**：log 表非 FormSchema-driven，無 `FilterNode→SQL` builder 可重用；且 Q6 要求「查詢須帶時間/鍵條件避免全表掃」。故 `GetChangeLogArgs` 用**明確 typed 欄位**：`FromUtc?` / `ToUtc?` / `UserId?` / `ProgId?` / `RowKey?` / `ChangeKind?` + 分頁（`Page` / `PageSize` / `IncludeTotalCount`）。直接對映索引欄、杜絕任意/昂貴查詢與注入面，較 `FilterNode` 簡單安全。

**分頁**：重用 `PagingInfo`；repo 依 dialect 產生 `OFFSET/FETCH`（SQL Server / Oracle）或 `LIMIT/OFFSET`（PostgreSQL / MySQL），`PageSize` clamp 上限；`IncludeTotalCount=true` 時另跑一次 `COUNT(*)` 填 `TotalCount`，否則以「多取一列」判 `HasMore`。實作時先找既有 dialect 分頁 helper（Form `GetList` 路徑），無則加最小 log 專用分頁。

**Phase 1 realignment（同 PR 一起改）**
- `GetRecordHistory` 回傳改為 `DataTable` + `PagingInfo`（標頭清單），移除「全還原」行為。
- 退場：`RecordHistoryEntry` DTO、`GetRecordHistoryResult.Changes(List<RecordHistoryEntry>)`；保留 `RecordFieldChange`（給 `GetChangeDetail`）。
- `ChangeDiffGramReader` 保留、改由 `GetChangeDetail` 呼叫。
- 同步更新 wire DTO、client connector（`GetChangeLogAsync` / `GetChangeDetailAsync` / 改版 `GetRecordHistoryAsync`）、`BoApiSurfaceTests` baseline（+2 方法）、`api-method-reference`(+zh-TW)、既有 Phase 1 測試。

**新增/異動檔案（預估）**：`LogActions`(+2)、contracts（`IGetChangeLog*`/`IGetChangeDetail*` + `ChangeDetail` 結果型別）、`Messages/AuditLog` wire、`Bee.Business/AuditLog`（args/result/BO 方法）、`IAuditLogRepository`(+GetChangeLog/GetChangeById/分頁)、`AuditLogRepository`、client、測試。

## 9. Phase 2b 設計（login / access / anomaly 清單）

沿用 2a 的 typed filter + `PagingOptions` + `LimitBuilder` 分頁基礎（repository 內抽 `WhereBuilder` + `QueryPage` 共用），四軸各一 typed 方法：

| 方法 | 表 | typed filter |
|------|----|-------------|
| `GetLoginLog` | `st_log_login` | FromUtc / ToUtc / UserId / Event(LoginEvent) |
| `GetAccessLog` | `st_log_access` | FromUtc / ToUtc / UserId / ProgId / RowKey |
| `GetApiAnomalyLog` | `st_log_anomaly_api` | FromUtc / ToUtc / UserId / Method / Kind(AnomalyKind) |
| `GetDbAnomalyLog` | `st_log_anomaly_db` | FromUtc / ToUtc / DatabaseId / Kind(AnomalyKind) |

**決策：異常 API/DB 拆兩方法（非 `GetAnomalyLog(layer)`）**：對齊 ADR-027 D6「API/DB 分表」——兩表視角/欄位不同（API 有 method + who；DB 有 database_id + command、**無 who/company**），兩個 typed 方法比單一 layer enum + union 欄位更清楚。`GetDbAnomalyLog` 因表無 company 欄，為**跨公司基礎設施檢視**（仍受 `AuditLog` Read gate）；其餘三軸依 session company 過濾。

**共用回應型別**：四方法皆回 `LogListResponse` / `LogListResult`（`DataTable? Table` + `PagingInfo? Paging`）——形狀相同、`DataTable` 自帶各表欄位（同 `GetList` 慣例），省 3 組重複 DTO。（2a 的 `GetChangeLog` 保留自有 `GetChangeLogResponse`，未回頭合併；`GetRecordHistory` 因多帶 `ProgId`/`RowKey` 亦自有型別。）

## 10. Phase 3a 設計（異常彙總：Summary + TopN）

把異常從逐筆清單（2b）提升到監控用的**彙總**檢視。將「異常監控細分」再拆一次：**3a 彙總（純標準 `GROUP BY`，低成本，現做）** vs **趨勢圖（時間桶，需 dialect helper，延後）**。

**方法集（`AuditLog` 軸）**

| 方法 | SQL | 回傳欄 | scope |
|------|-----|--------|-------|
| `GetApiAnomalySummary(from, to)` | `st_log_anomaly_api` `GROUP BY anomaly_kind` `COUNT(*)` | `anomaly_kind` / `event_count` | company-scoped |
| `GetDbAnomalySummary(from, to)` | `st_log_anomaly_db` `GROUP BY anomaly_kind` `COUNT(*)` | `anomaly_kind` / `event_count` | 跨公司（表無 company） |
| `GetTopApiMethods(from, to, topN)` | `st_log_anomaly_api` `GROUP BY method` `COUNT(*)` + `MAX(elapsed_ms)`，`ORDER BY COUNT(*) DESC` 取前 N | `method` / `event_count` / `max_elapsed_ms` | company-scoped |

- 回傳一律 `DataTable`（維度欄 + 指標欄），**無分頁**（聚合後有界；異常本就 opt-in + 門檻驅動、量體受控）。共用結果型別 `LogAggregateResult` / `LogAggregateResponse`（`DataTable? Table`）。
- WHERE 重用 2b 的 `WhereBuilder`（`log_time` 範圍 + API 的 `company_id`）；`ORDER BY COUNT(*) DESC` 用**運算式**（不靠 alias，跨方言安全）；TopN 用既有 `LimitBuilder`（clamp `1..100`）。
- 全方法沿用 `AuditLog` Read gate；純唯讀。

**分界（3a vs 延後）**：Summary/TopN 只用標準 `GROUP BY` + `COUNT/MAX`，跨方言無雷；**時間分桶趨勢**（`date_trunc` / `CONVERT(date)` / `DATE()` / `TRUNC` 各家不同）需另做 `DateBucketBuilder` dialect helper，故留待實際要畫趨勢圖時再做。DB 端 top-by-`database_id` 亦屬可選未來（資料庫數少，Summary 已足）。

## 6. 相依與約束

- 前置：稽核寫入（項 0–4，已完成）。
- 安全：查詢 SQL 一律 `DbCommandSpec` 參數化（`scanning.md`）；回傳不外洩非授權資料（權限 Q5）。
- 對齊：`bee-add-bo-method`、`bee-add-form`（若走 read-only FormSchema 路線）等既有 skill 與慣例。
- 相關：[ADR-027](../adr/adr-027-audit-trail.md)（寫入側設計）、[framework-reserved-names §1.3](../framework-reserved-names.md)、[ADR-025](../adr/adr-025-define-types-aot-xmlserializer-compat.md)（DiffGram 還原若涉 XmlSerializer / AOT）。
