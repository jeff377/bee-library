# 封存計畫索引

已完成並封存的 plan，作為維護者的團隊記憶；依完成日新到舊排列。
進行中 / 待動工者見 [../README.md](../README.md)。

> **封存的 plan 只是歷史紀錄**：它記載當時的打算與決策脈絡，**不保證等於現行行為**——
> 後續版本可能已經改掉。要查現況請看原始碼、`docs/adr/` 或 `docs/` 下的公開文件。
> 公開文件一律不得連結進本目錄（見 [rules/public-docs.md](../../../.claude/rules/public-docs.md)）。

**維護約定**：將 plan 移入本目錄時，一併在此登錄一列，並自上層索引移除。

| 計畫 | 完成日 | 說明 |
|------|--------|------|
| [docs 根目錄文件重編排](plan-docs-reorganization.md) | 2026-07-31 | 索引改旅程分層，新增 getting-started 與定義檔全景（雙語） |
| [開發流程強化](plan-dev-workflow-hardening.md) | 2026-07-31 | commit 前驗證 hook；`plan-workflow` → `dev-workflow` plugin 改名 |
| [Bee.Analyzers — 框架慣例編譯期化](plan-bee-analyzers.md) | 2026-07-30 | 22 條 Roslyn 規則（定義檔跨檔一致性 / 序列化 / C# 慣例） |
| [修復 Bee.Northwind 登入中斷](plan-northwind-session-tables.md) | 2026-07-30 | common 表資料驅動 + 啟動 fail-fast + debug 例外透傳 |
| [SessionInfo 持久化與重建](plan-session-persistence.md) | 2026-07-30 | `st_session` 種子、四個寫入點、快取失效後重建 |
| [Database 快取改經 `ICacheDataSourceProvider` 自載](plan-cache-createinstance-db-loading.md) | 2026-07-29 | 三個 DB 快取的自載接縫 |
| [框架體檢與分級重構](plan-framework-review-2026-07-28.md) | 2026-07-28 | 九面向唯讀審查與 P0–P4 重構計畫 |
| [UI 專案收斂為 Avalonia + Blazor.Server 雙軌](plan-ui-consolidation.md) | 2026-07-28 | 移除 `Bee.UI.Maui` 與 `Bee.Web.Blazor.Wasm` |
| [`FieldDbType.Time` 純時刻型別](plan-time-semantics.md) | 2026-07-27 | 時刻語意與 wire 貫通 |
| [DateTime 時區處理機制](plan-datetime-timezone.md) | 2026-07-26 | UTC 存放、connector 雙向轉換、`st_user.time_zone` |
| [日曆日語意的顯式標記](plan-date-semantics.md) | 2026-07-25 | `FieldDbType.Date` 貫通 wire、`DateOnly` |
| [Bee.Definition 職責拆分](plan-bee-definition-split.md) | 2026-07-24 | Storage IO / Security 實作外移 |
| [快取失效模型統一](plan-cache-invalidation-model.md) | 2026-07-24 | 檔案相依 + DB 相依皆進 `CacheItemPolicy` |
| [DataSet 欄名全小寫](plan-dataset-lowercase-columns.md) | 2026-07-24 | 定義 / 資料 / UI 三者一致（ADR-029） |
| [ERP 資料軌跡 / 日誌功能（母計畫）](plan-audit-trail.md) | 2026-07-24 | log 資料庫分類，統括下列項 0–4 與查詢側 |
| [Avalonia 行動端 Release AOT / Trim 修正](plan-mobile-release-trim-safe.md) | 2026-07-24 | `ILLink.Descriptors.xml` 內嵌 |
| [plan 工作流可攜化](plan-workflow-portability.md) | 2026-07-24 | 抽 skill → `plan-workflow` plugin |
| [Bee.Api.Contracts 命名空間按 BO 軸對齊](plan-contracts-namespace-align.md) | 2026-07-23 | 合約介面命名空間 |
| [MessagePack 合約改採 property-name key](plan-messagepack-name-based-keys.md) | 2026-07-22 | `keyAsPropertyName`（ADR-030，wire breaking） |
| [API 合約三棲序列化單元測試](plan-api-contract-serialization-tests.md) | 2026-07-22 | MessagePack + JSON round-trip |
| [自訂運算式與規則引擎](plan-expression-rule-engine.md) | 2026-07-09 | 後端 + Avalonia 前端即時運算 |
| [稽核日誌查詢](plan-audit-log-query.md) | 2026-07-08 | `st_log_*` 讀取側，`AuditLog` 軸 10 個方法 |
| [異常記錄（項 4）](plan-audit-4-anomaly.md) | 2026-07-08 | `st_log_anomaly` |
| [檢視記錄（項 3）](plan-audit-3-access.md) | 2026-07-07 | `st_log_access` |
| [異動記錄（項 2）](plan-audit-2-change.md) | 2026-07-05 | `st_log_change` |
| [登入記錄（項 1）](plan-audit-1-login.md) | 2026-07-05 | `st_log_login` |
| [日誌基礎設施（項 0）](plan-audit-0-foundation.md) | 2026-07-05 | 稽核寫入側基礎 |
| [前端權限 Capability](plan-permission-capability.md) | 2026-07-03 | element 細粒度降級 |
| [SQL Server `datetime` → `datetime2`](plan-sqlserver-datetime2.md) | 2026-07-03 | 精度與最小值遷移 |
| [數值處理核心](plan-numeric-core.md) | 2026-07-01 | `NumberKind` + 公司位數 + round-then-sum |
| [多幣別數值](plan-numeric-multicurrency.md) | 2026-07-01 | `CurrencySettings` + CUKY + 現金捨入 |
| [多計量單位數值](plan-numeric-uom.md) | 2026-07-01 | `UnitSettings` + UNIT 綁定 |
| [發佈 4.12.1 並同步 Northwind 複本](plan-release-4.12.1-and-sync-copy.md) | 2026-06-27 | 行動端 trim descriptor |
| [Bee.Northwind 畢業至獨立 repo](plan-northwind-avalonia-graduation.md) | 2026-06-26 | 同步至 `bee-northwind-avalonia` |
| [Bee.Northwind 新增 Android head](plan-northwind-android.md) | 2026-06-26 | Avalonia `net10.0-android` |
| [Bee.Northwind 新增 iOS head](plan-northwind-ios.md) | 2026-06-26 | Avalonia `net10.0-ios` |
| [bee-northwind-avalonia 重新同步 + WASM head](plan-northwind-avalonia-resync.md) | 2026-06-24 | Browser backend |
| [ERP 數值處理設計總覽](plan-numeric-formatting.md) | 📐 2026-06-21 | 決策紀錄，不直接執行；保存完整設計與 SAP / Odoo 研究，執行拆為上述三個 numeric plan |
| [新增 Avalonia UI sample](plan-avalonia-sample.md) | 2026-06-09 | 鏡像 Maui.Demo |
| [定義檔方案維護工具](plan-define-editor.md) | 2026-06-07 | Avalonia 桌面程式 DefineEditor |
