# 計畫文件索引

`docs/plans/` 下所有 plan 的清單，供人工快速掃讀目前有哪些計畫、進行到哪。

> **plan 是階段性工作文件，不是參考資料**：它記錄的是當時的打算，完成後未必等於現行行為。
> 公開文件一律不得連結進本目錄（見 [rules/public-docs.md](../../.claude/rules/public-docs.md)）。

**維護約定**：新增 plan、變更 plan 狀態、或將 plan 移入 `archive/` 時，一併更新本檔。

## 進行中 / 待動工

| 計畫 | 狀態 | 範圍 | 備註 |
|------|------|------|------|
| [列級租戶隔離（`sys_company_id`）](plan-row-level-tenancy.md) | 📝 擬定中（2026-07-30） | 試用公司共用 company 資料庫，以公司編號在列的層級區隔；與既有的資料庫級隔離正交並存 | **D1–D5 全數定案，可動工**。分 4 階段，階段 1（`st_session` 公司欄位化）獨立可先交付 |
| [API Key 存放機制與預設驗證強化](plan-api-key-store.md) | 📝 擬定中（2026-07-27） | `X-Api-Key` 的存放位置與預設驗證行為 | 尚未確認 |
| [客製化共同前置](plan-customization-foundation.md) | 📝 擬定中（討論稿，2026-07-25） | 三類客製的共同基礎（缺口 A、B） | **另外三份客製 plan 的前置**，未補則其餘三份無法生效 |
| [Layout 客製化](plan-customization-layout.md) | 📝 擬定中（討論稿，2026-07-25） | `FormLayout` 的租戶客製：版面重排、欄位隱藏、區塊調整 | 前置：共同前置 |
| [業務邏輯客製化](plan-customization-business.md) | 📝 擬定中（討論稿，2026-07-25） | BO 的租戶客製：單據行為、驗證規則、流程差異 | 前置：共同前置 |
| [語系客製化](plan-customization-language.md) | 📝 擬定中（討論稿，2026-07-25） | 語系資源的租戶客製：欄位標題、表單名稱、訊息、選項文字 | 前置：共同前置 |

## 已封存（`archive/`）

已完成並封存的 plan，作為維護者的團隊記憶；依完成日期新到舊排列。

| 計畫 | 完成日 | 主題 |
|------|--------|------|
| [SessionInfo 持久化與重建](archive/plan-session-persistence.md) | 2026-07-30 | `st_session` 種子、四個寫入點、快取失效後重建 |
| [Database 快取改經 `ICacheDataSourceProvider` 自載](archive/plan-cache-createinstance-db-loading.md) | 2026-07-29 | 三個 DB 快取的自載接縫 |
| [框架體檢與分級重構](archive/plan-framework-review-2026-07-28.md) | 2026-07-28 | 九面向唯讀審查與 P0–P4 重構計畫 |
| [UI 專案收斂為 Avalonia + Blazor.Server 雙軌](archive/plan-ui-consolidation.md) | 2026-07-28 | 移除 `Bee.UI.Maui` 與 `Bee.Web.Blazor.Wasm` |
| [`FieldDbType.Time` 純時刻型別](archive/plan-time-semantics.md) | 2026-07-27 | 時刻語意與 wire 貫通 |
| [DateTime 時區處理機制](archive/plan-datetime-timezone.md) | 2026-07-26 | UTC 存放、connector 雙向轉換、`st_user.time_zone` |
| [日曆日語意的顯式標記](archive/plan-date-semantics.md) | 2026-07-25 | `FieldDbType.Date` 貫通 wire、`DateOnly` |
| [Bee.Definition 職責拆分](archive/plan-bee-definition-split.md) | 2026-07-24 | Storage IO / Security 實作外移 |
| [快取失效模型統一](archive/plan-cache-invalidation-model.md) | 2026-07-24 | 檔案相依 + DB 相依皆進 `CacheItemPolicy` |
| [DataSet 欄名全小寫](archive/plan-dataset-lowercase-columns.md) | 2026-07-24 | 定義 / 資料 / UI 三者一致（ADR-029） |
| [ERP 資料軌跡 / 日誌功能（母計畫）](archive/plan-audit-trail.md) | 2026-07-24 | log 資料庫分類，統括下列子項 |
| [Avalonia 行動端 Release AOT / Trim 修正](archive/plan-mobile-release-trim-safe.md) | 2026-07-24 | `ILLink.Descriptors.xml` 內嵌 |
| [plan 工作流可攜化](archive/plan-workflow-portability.md) | 2026-07-24 | 抽 skill → `plan-workflow` plugin |
| [Bee.Api.Contracts 命名空間按 BO 軸對齊](archive/plan-contracts-namespace-align.md) | 2026-07-23 | 合約介面命名空間 |
| [MessagePack 合約改採 property-name key](archive/plan-messagepack-name-based-keys.md) | 2026-07-22 | `keyAsPropertyName`（ADR-030，wire breaking） |
| [API 合約三棲序列化單元測試](archive/plan-api-contract-serialization-tests.md) | 2026-07-22 | MessagePack + JSON round-trip |
| [自訂運算式與規則引擎](archive/plan-expression-rule-engine.md) | 2026-07-09 | 後端 + Avalonia 前端即時運算 |
| [稽核日誌查詢](archive/plan-audit-log-query.md) | 2026-07-08 | `st_log_*` 讀取側，`AuditLog` 軸 10 個方法 |
| [異常記錄（項 4）](archive/plan-audit-4-anomaly.md) | 2026-07-08 | `st_log_anomaly` |
| [檢視記錄（項 3）](archive/plan-audit-3-access.md) | 2026-07-07 | `st_log_access` |
| [異動記錄（項 2）](archive/plan-audit-2-change.md) | 2026-07-05 | `st_log_change` |
| [登入記錄（項 1）](archive/plan-audit-1-login.md) | 2026-07-05 | `st_log_login` |
| [日誌基礎設施（項 0）](archive/plan-audit-0-foundation.md) | 2026-07-05 | 稽核寫入側基礎 |
| [前端權限 Capability](archive/plan-permission-capability.md) | 2026-07-03 | element 細粒度降級 |
| [SQL Server `datetime` → `datetime2`](archive/plan-sqlserver-datetime2.md) | 2026-07-03 | 精度與最小值遷移 |
| [數值處理核心](archive/plan-numeric-core.md) | 2026-07-01 | `NumberKind` + 公司位數 + round-then-sum |
| [多幣別數值](archive/plan-numeric-multicurrency.md) | 2026-07-01 | `CurrencySettings` + CUKY + 現金捨入 |
| [多計量單位數值](archive/plan-numeric-uom.md) | 2026-07-01 | `UnitSettings` + UNIT 綁定 |
| [發佈 4.12.1 並同步 Northwind 複本](archive/plan-release-4.12.1-and-sync-copy.md) | 2026-06-27 | 行動端 trim descriptor |
| [Bee.Northwind 畢業至獨立 repo](archive/plan-northwind-avalonia-graduation.md) | 2026-06-26 | 同步至 `bee-northwind-avalonia` |
| [Bee.Northwind 新增 Android head](archive/plan-northwind-android.md) | 2026-06-26 | Avalonia `net10.0-android` |
| [Bee.Northwind 新增 iOS head](archive/plan-northwind-ios.md) | 2026-06-26 | Avalonia `net10.0-ios` |
| [bee-northwind-avalonia 重新同步 + WASM head](archive/plan-northwind-avalonia-resync.md) | 2026-06-24 | Browser backend |
| [新增 Avalonia UI sample](archive/plan-avalonia-sample.md) | 2026-06-09 | 鏡像 Maui.Demo |
| [定義檔方案維護工具](archive/plan-define-editor.md) | 2026-06-07 | Avalonia 桌面程式 DefineEditor |

### 設計總覽（不直接執行）

| 文件 | 性質 |
|------|------|
| [ERP 數值處理設計總覽](archive/plan-numeric-formatting.md) | 📐 決策紀錄（2026-06-21）——保存完整設計與 SAP / Odoo 研究，執行拆為上述三個 numeric plan |
