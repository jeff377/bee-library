# ADR-027：資料軌跡 / 稽核日誌（六軸 `st_log_*` 設計）

## 狀態

已採納（2026-07-08）

## 背景

資料軌跡是 ERP 的核心需求——「誰登入、誰把哪個欄位從什麼改成什麼、誰看了哪筆敏感資料、哪個動作 / DB 指令出了異常」是合規稽核、責任追溯與效能調校的基礎。實作前的現況：

| 面向 | 現況 | 缺口 |
|------|------|------|
| `log` 資料庫分類 | 已存在（`DbCategoryIds.Log`），但 `<Tables />` 為空 | 框架無任何自帶稽核表 |
| 診斷日誌 | `ILogWriter` / `LogEntry` / `Tracer` / `TraceContext` | 記憶體 / UI 導向、**未持久化**、屬 observability 非業務稽核 |
| DB 異常門檻 | `DbAccessAnomalyLogOptions`（`ExecutionTimeThreshold` 等） | **定義了卻無消費者**（0 caller） |

此設計借鏡 SAP（Security Audit Log、Change Documents `CDHDR`/`CDPOS`、Table Logging `DBTABLOG`、Read Access Logging、System Log `SM21`、`SLG1`/`SM37`）與 Odoo（Chatter `mail.tracking.value`、OCA `auditlog`、`ir.logging`、`res.users.log`），並依本框架 DataSet-centric 與 FormSchema-driven 架構簡化。它橫跨 `Bee.Definition`（型別 / 設定）、`Bee.Business`（BO 埋點）、`Bee.Db`（DB 異常）、`Bee.Api.Core`（API 異常）與 `Bee.Hosting`（背景寫入 / DI），是框架對外 API surface 的結構性契約，故立此 ADR。完整設計、SAP/Odoo 出處與逐項定案見設計文件（`plan-audit-*` 系列，將封存）；本 ADR 收斂「為何如此」與拒絕的替代方案。

## 考慮過的選項

以下逐一列出關鍵決策點上「看似合理但被否決」的替代方案（採納方案見下節）。

1. **執行記錄全記每次 `JsonRpcExecutor` 呼叫**：直覺、完整覆蓋「誰做了什麼」。**否決**——讀取遠多於寫入，全記量體爆炸，且與登入（項 1）/ 異動（項 2）重複記錄。SAP（SAL 選擇性 filter、STAD 短期統計、SLG1 opt-in）與 Odoo（`ir.logging` opt-in、OCA per-rule、read 模式昂貴）**皆不全記**。改為只記「有問題的」＝**異常記錄**。

2. **異動記錄採逐欄 EAV（`st_log_change_field`，SAP `CDPOS` 式）為預設**：欄位級可 SQL 查詢 / 統計。**否決為預設**——列數 = 異動次數 × 欄數，且需自訂 diff 程式碼；換來的欄位級查詢力多數 ERP「看某單改了什麼」用不到。改採 **DataSet DiffGram 單欄**（框架原生 `GetChanges()`，一次全記 master+detail 新舊值、零自訂 diff、XML→DataSet 還原即可顯示）。EAV 降為選配的「可查詢模式」。

3. **異動記錄採 transactional outbox（強一致）**：業務 commit ⇔ log 落地。**否決**——落到實作需 per-company outbox 表、多租戶跨庫輪詢 flush、`IDataFormRepository.Save` 簽章改動、交易內寫入，成本過重。改採 **best-effort 非同步**（BO 於 commit 後走既有 `IAuditLogWriter`；漏失窗口極小，且可對 change 強制同步縮小）。零漏失需求出現時再升級 outbox（additive）。

4. **系統 / 錯誤做成 observability 稽核表（`st_log_trace`，把 `Tracer` / `ITraceWriter` 導一份）**：一次涵蓋系統事件。**否決**——`Tracer` / `TraceContext` 是**開發階段偵錯執行流程**用，不可作稽核來源；系統 / 錯誤本質是 observability（維運 / 除錯），歸 `ILogWriter` / host `ILogger`（檔案 / Seq / APM），與業務稽核分離（對齊 SAP SM21 / Odoo `ir.logging`）。軸⑤改以精準定義的**異常記錄**實現。

5. **日誌列存 `user_rowid`（正規化參照）**：省欄位、指向 `st_user`。**否決**——`log` 資料庫與 `common`/`company` **實體分離**，跨庫 join 不可行；純 rowid 需 join 才有意義。改**去正規化**存 `user_id` + `user_name`、`company_id` + `company_name`，每列**自足**。

6. **API / DB 異常合併單表（`layer` 欄區分）**：一張表、少一個型別。**否決**——兩者視角不同、記的資訊不同（API＝哪個動作 + who；DB＝哪個 `database_id` + command），且 `DbAccess` 無 session context、取不到 who。改 **API / DB 分表**（`st_log_anomaly_api` / `st_log_anomaly_db`），DB 表精簡無 who。

7. **`DbAccessFactory` 直接注入 `IAuditLogWriter`**：最直接。**否決**——形成 `IDbAccessFactory → IAuditLogWriter → AuditLogDbSink → IDbAccessFactory` 建構循環相依。改以 `Func<IAuditLogWriter?>` 延遲解析（在 `Create()` 時才取），並讓 log DB 自身的 `DbAccess` 不做異常偵測以避免遞迴。

## 決策

採「統一 `IAuditLogWriter` 寫入、六軸 `st_log_*` 表、opt-in、best-effort、去正規化自足」的整體設計。核心決策：

- **D1 — 六軸收斂**：基礎設施 ＋ 登入（①）＋ 異動（③，含安全⑥以 `is_sensitive` 旗標併入）＋ 檢視（②）＋ 異常（④/⑤）。軸⑤系統以**異常記錄**實現，非 observability 稽核表。
- **D2 — 統一寫入抽象 `IAuditLogWriter`**：`AuditEntry` 抽象基底（共通欄可覆寫的 `AddCommonColumns`）＋ 型別化子類；預設背景批次寫入（bounded channel，滿載退化同步、log DB 不可用落地檔），無 host 時同步直寫。放 `Bee.Definition.Logging`。
- **D3 — `st_log_*` 於 `log` 分類、opt-in**：`AuditLogOptions`（掛 `BackendConfiguration`）各軸獨立開關，**預設全關**，零回歸。5 張表：`st_log_login` / `st_log_change` / `st_log_access` / `st_log_anomaly_api` / `st_log_anomaly_db`。
- **D4 — 日誌獨立性**：log 列自足、查詢不 join；去正規化 who / company；`log` 可依年份分庫（`log_YYYY`，當年可寫、歷史唯讀），寫入目標未來以解析器選當年可寫 DB（現以固定 `DbCategoryIds.Log`）。
- **D5 — 異動 = DataSet DiffGram 單表**：`st_log_change.changes_xml` 存 `GetChanges()` 的 DiffGram（master+detail 新舊值）。兩條鐵則：**擷取在 `Save` 的 `AcceptChanges` 之前**、**序列化必用 `DiffGram`**（普通 `WriteXml` 丟舊值）。`Delete` 於刪除前載入記錄、存**完整 before-image**。
- **D6 — 異常 = 五類、API/DB 分表**：`Error` / `Timeout`（獨立於錯誤，屬 infra/效能訊號）/ `Slow` / `LargeAffected` / `LargeResult`。掛 `JsonRpcExecutor.ExecuteAsyncCore`（API）與 `DbAccess.Execute`（DB），實作既有 `DbAccessAnomalyLogOptions` 門檻。
- **D7 — 安全**：不記完整 SQL 與參數值（只存 `{0}` 模板）、error 訊息消毒（無堆疊、無內部路徑）；沿用 `security.md` / `scanning.md` 既有規則。

## 後果 / 影響

- **正面**：可回溯的業務資料軌跡（登入 / 異動含新舊值與 delete before-image / 檢視 / 異常）；一致的 opt-in、best-effort、去正規化自足、安全消毒設計；量體受控（檢視敏感度驅動、異常只記問題）。
- **取捨**：異動記錄以「簡潔 + 可還原顯示」換取「欄位級 SQL 查詢力」（有需要再開選配 EAV）；best-effort 有極小漏失窗口（有需要再升 transactional outbox，entry / schema 不變）。
- **相關**：系統表登記見 [framework-reserved-names §1.3](../framework-reserved-names.md)；`DbScope.Log` 路由見 [ADR-010](adr-010-logical-database-category.md)；DataForm Save 管線見 [ADR-024](adr-024-dataform-save-dataadapter.md)。
- **待辦**：per-form 稽核規則（Odoo `auditlog.rule` 式的 admin 執行期選單，涵蓋異動 + 檢視；目前全記所有表單）；`ExecuteBatch` / `UpdateDataTables` 的 DB 異常偵測（目前僅 `Execute` 主路徑）；`st_cache_notify` 既有 SQL Server 升級 idempotency bug（另案）。
