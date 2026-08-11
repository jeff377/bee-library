# 架構決策紀錄（ADR）索引

← 回到 [文件索引](../README.zh-TW.md)

ADR 記錄**決策當下的脈絡與理由**，是理解「為何這樣設計」的主要來源。

> ADR 不隨實作演進改寫。決策被推翻時標記為「已取代」並指向新的 ADR；
> 實作細節偏離但決策仍成立時，於文末加〈實作演進〉段落說明，原文保留。

| # | 決策 | 狀態 |
|---|------|------|
| [001](adr-001-dataset-as-dto.md) | 使用 DataSet 作為跨層 DTO | ✅ 已採納 |
| [002](adr-002-newtonsoft-json.md) | JSON 序列化函式庫的選擇與遷移 | 🔁 已取代 |
| [003](adr-003-static-service-locator.md) | 使用靜態 Service Locator 而非依賴注入 | 🔁 已取代 |
| [004](adr-004-messagepack-payload.md) | 使用 MessagePack 作為 API Payload 序列化格式 | ✅ 已採納 |
| [005](adr-005-formschema-driven.md) | FormSchema 定義驅動架構 | ✅ 已採納 |
| [006](adr-006-dual-target-framework.md) | 雙目標框架策略（netstandard2.0 + net10.0） | 🔁 已取代 |
| [007](adr-007-convention-based-type-resolution.md) | 以命名慣例自動推導 API 型別 | ✅ 已採納 |
| [008](adr-008-bee-db-namespace-layout.md) | Bee.Db 命名空間佈局——語法層與模型層分離 | ✅ 已採納 |
| [009](adr-009-cache-implementation.md) | Bee.ObjectCaching 採用 Microsoft.Extensions.Caching.Memory + IChangeToken | ✅ 已採納 |
| [010](adr-010-logical-database-category.md) | 邏輯資料庫分類（DbCategory）解耦資料庫部署彈性 | ✅ 已採納 |
| [011](adr-011-di-replaces-service-locator.md) | 採用 DI 取代靜態 Service Locator | ✅ 已採納 |
| [012](adr-012-session-company-context.md) | Session 公司情境模型（兩階段 session lifecycle） | ✅ 已採納 |
| [013](adr-013-frontend-api-connection-strategy.md) | 前端 API 連線策略 — `Bee.UI.*` 與 `Bee.Web.*` 兩條 family 分流 | ✅ 已採納 |
| [014](adr-014-jsonrpc-plain-public-default.md) | JSON-RPC `Plain` 開放策略 — `Public` 為預設保護等級，HTTPS 為信任界線 | ✅ 已採納 |
| [015](adr-015-master-key-environment-default.md) | `MasterKeySource` 預設改為 `Environment` — 對齊 12-factor「config in env」 | ✅ 已採納 |
| [016](adr-016-multitenant-customization-overlay.md) | 多租戶客製化覆蓋層（雙層唯讀疊加） | ✅ 已採納 |
| [017](adr-017-db-cache-invalidation.md) | 資料庫快取相依/失效機制（通知表 + 輪詢 + 慣例分派） | ✅ 已採納 |
| [018](adr-018-db-define-storage.md) | 定義儲存於資料庫（`st_define` 單表 XML blob） | ✅ 已採納 |
| [019](adr-019-permission-authorization-model.md) | 權限授權模型（兩層 enforcement + record scope） | ✅ 已採納 |
| [020](adr-020-avalonia-datagrid-binding-strategy.md) | Avalonia DataGrid 對 DataTable 列的綁定策略 | ✅ 已採納 |
| [021](adr-021-avalonia-datagrid-editing-strategy.md) | Avalonia DataGrid 的 in-cell 編輯策略 | ✅ 已採納 |
| [022](adr-022-avalonia-datagrid-cell-recycling.md) | Avalonia DataGrid 清單儲存格不啟用模板回收 | ✅ 已採納 |
| [023](adr-023-lookup-relation-mechanism.md) | 定義驅動的 lookup 關連機制 | ✅ 已採納 |
| [024](adr-024-dataform-save-dataadapter.md) | DataForm 持久化改走 DataTable 級 DataAdapter | ✅ 已採納 |
| [025](adr-025-define-types-aot-xmlserializer-compat.md) | 定義型別相容 AOT reflection XmlSerializer（單一 Add + 無參數建構子） | ✅ 已採納 |
| [026](adr-026-numeric-semantics-rounding.md) | 數值語意、公司/貨幣/單位位數與 round-then-sum | ✅ 已採納 |
| [027](adr-027-audit-trail.md) | 資料軌跡 / 稽核日誌（六軸 `st_log_*` 設計） | ✅ 已採納 |
| [028](adr-028-expression-rule-engine.md) | 自訂運算式與規則引擎（減少 BO 手寫程式碼） | ✅ 已採納 |
| [029](adr-029-lowercase-field-names.md) | 欄位名稱一律小寫（定義 / 資料 / UI 三層一致） | ✅ 已採納 |
| [030](adr-030-messagepack-name-based-keys.md) | MessagePack 合約改採 property-name key（keyAsPropertyName） | ✅ 已採納 |
| [031](adr-031-calendar-day-column-semantics.md) | 日曆日欄位語意以顯式標記承載，不改 CLR 型別 | ✅ 已採納 |
| [032](adr-032-datetime-timezone.md) | DateTime 以 UTC 為單一時區來源，Connector 為唯一轉換點 | ✅ 已採納 |
| [033](adr-033-time-of-day-semantics.md) | 時刻語意（`FieldDbType.Time`）以定寬字串承載 | ✅ 已採納 |
| [034](adr-034-progid-type-registry.md) | ProgramSettings 作為全框架型別註冊表（選單分離、Repository 以 progId 綁定） | ✅ 已採納 |
| [035](adr-035-business-logic-plugin.md) | 業務邏輯 plugin（在既有流程上掛載、兩層相加、與規則引擎分界） | ✅ 已採納 |
| [036](adr-036-wire-serialization-externalized.md) | 傳輸序列化外置至 API 層，定義層不再承載 MessagePack | ✅ 已採納 |
| [037](adr-037-wire-explicit-registration.md) | wire 型別一律顯式註冊 formatter，`object` 值改用判別式封套 | ✅ 已採納 |
| [038](adr-038-definition-dependency-boundary.md) | 定義層相依邊界：運算式抽象下沉至 `Bee.Base`，判準以閘門固化 | ✅ 已採納 |
