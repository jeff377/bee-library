# 封存計畫索引

已完成並封存的 plan，作為維護者的團隊記憶；依完成日新到舊排列。
進行中 / 待動工者見 [../README.md](../README.md)。

> **封存的 plan 只是歷史紀錄**：它記載當時的打算與決策脈絡，**不保證等於現行行為**——
> 後續版本可能已經改掉。要查現況請看原始碼、`docs/adr/` 或 `docs/` 下的公開文件。
> 公開文件一律不得連結進本目錄（見 [rules/public-docs.md](../../../.claude/rules/public-docs.md)）。

**維護約定**：將 plan 移入本目錄時，一併在此登錄一列，並自上層索引移除。

> **保留期限**：封存滿一個月的 plan 會被清除，git 歷史仍可追溯。
> 2026-08-20 首次清理，移除 20 份 2026-07-20 以前完成者。
> 2026-09-06 第二次清理，移除 28 份 2026-08-06 以前完成者。

| 計畫 | 完成日 | 說明 |
|------|--------|------|
| [業務 plugin 設定檔標記時點](plan-plugin-stage-declaration.md) | 2026-09-05 | 重啟 [adr-035](../../adr/adr-035-business-logic-plugin.md) 決策三：`PluginSettings.xml` 的每一筆繫結一個時點（`Stage="BeforeSave"`），直接看 XML 就知道哪些時點有外掛。舊格式（只列型別、時點由類別 override）直接拒；`FormPluginStage` 下移改名為 `Bee.Definition.Settings.PluginStage`；反射降為驗證器。代價是放棄 ADR 自稱「唯一實質優勢」的 per-operation 跨時點狀態共享。破壞性變更，隨 4.29.0 發佈 |
| [框架全面體檢（2026-09-04）](plan-framework-review.md) | 2026-09-04 | 十一面向唯讀體檢（基準 v4.27.0）。P0–P4 條目全數處理（71 項已修，`T-2` / `CON-6` 查證後撤回），每項修正都經負向驗證或真環境實測。**五處刻意開著並各自記明理由**（`Z-13` / `Z-15` / `A-5`+`A-3` / `DEP-1` / `T-6`）。過程中查出報告本身數處有誤，逐筆更正也是產出的一部分。方法論新得一條：**plan 完成不等於文件完成** |
| [T-8：71 筆「`[Fact]` 卻需要資料庫」的分類](plan-db-dependent-tests.md) | 2026-09-04 | 承接體檢的 T-7 / T-8。窮盡掃描（不帶 `--settings`）得 71 個 case，逐筆問「這個測試的主題需要資料庫嗎」：A 類 38 個 case 拆掉相依（每個環境都跑得起來，淨賺），B 類 33 個依建議維持現狀 |
| [TypeScript 連接器與 payload codec 逐請求協商](plan-typescript-connector.md) | 2026-09-03 | body codec 改由每個請求在 payload 信封宣告、伺服端以同一 codec 回應，未宣告即 MessagePack（相容性常數）；`ApiPayloadOptions.Serializer` 移除。另交付跨語言黃金樣本 `wire-fixtures/` 與合約 `wire-contracts/`，下游 [bee-connector-js](https://github.com/jeff377/bee-connector-js) 同步之。決策見 [adr-044](../../adr/adr-044-payload-codec-negotiation.md) |
| [錯誤契約對映收斂為單一登錄](plan-error-contract-mapping.md) | 2026-09-02 | 伺服端拋出的例外型別 ↔ JSON-RPC 錯誤碼原本兩端各寫一份，已漂掉一個分支（`ReplayRejected` 呼叫端沒接）。抽出單一對映登錄，兩端從同一份宣告消費，並補迴歸測試鎖住；順手修正公開文件的錯誤碼表 |
| [API 重放攻擊防護](plan-api-anti-replay.md) | 2026-09-01 | wire frame 承載 timestamp + 伺服端時窗檢查，加上 per-session 序號滑動視窗（零 DB）；`ApiAccessControlAttribute` 長出第三維度，重放事件納入 anomaly log。七項決策與三項明確不納入見 [adr-042](../../adr/adr-042-api-replay-protection.md) |
| [WASM head 的中文字型缺失](plan-wasm-cjk-font.md) | 2026-09-01 | Northwind Browser head 中文標籤全變豆腐方塊（`.WithInterFont()` 不含 CJK 字形）。**推翻內嵌字型路線**——即使子集化，相對 61 MB 基數的淨增量仍不划算；改為把 WASM head 的 UI 語系固定為英文（一行 culture 釘選），其餘 head 的 `WithInterFont()` 呼叫點不動 |
| [Northwind 登入後進入公司](plan-northwind-enter-company.md) | 2026-08-28 | seed `st_company` / `st_user_company` 與三張角色表、刪掉兩個捷徑類別，登入成功後自動 `EnterCompanyAsync`（不做公司選單 UI）；獨立 repo `bee-northwind-avalonia` 同步 |
| [per-form 稽核規則](plan-per-form-audit-rule.md) | 2026-08-26 | 稽核開關由全域降到「每張表單 × 每家公司」：`st_audit_rule` 表 + per-company 快取 + 框架內建維護表單與 cache-notify 失效鏈，結案 [adr-027](../../adr/adr-027-audit-trail.md)〈待辦〉第一條、補齊 [adr-040](../../adr/adr-040-audit-trail-taxonomy.md) 決策四未實作的兩條。決策見 [adr-041](../../adr/adr-041-per-form-audit-rule.md) |
| [異常日誌寫入介面自稽核軌跡拆出](plan-anomaly-writer-split.md) | 2026-08-24 | 依 [adr-040](../../adr/adr-040-audit-trail-taxonomy.md) 決策二的分界（稽核 vs observability）把寫入介面拆成兩個：`AnomalyEntry` 基底 + `IAnomalyLogWriter`。破壞性變更刻意全押在同一階段——純加法那半的中間狀態沒有價值 |
| [CI 依條件決定跑哪些資料庫測試](plan-ci-db-scope.md) | 2026-08-21 | 日常 push 只起 SQL Server + SQLite（精簡模式，跳過 Sonar），四種資料庫全跑需 commit message 帶 `[all-db]` 或手動 dispatch。實測 479 → **195 秒**（省 59%）——四種資料庫的測試合計只花 77 秒，四個容器起來卻要 104 秒，成本在容器啟動而非測試本身。**明確不採用**依變更路徑自動判定：判定規則會與實際相依關係漂移，且誤判方向是「該跑卻沒跑」的靜默失效 |
| [FormLayout 收回設計階段，移除執行階段自動推導](plan-formlayout-design-time-only.md) | 2026-08-20 | 版面改為設計階段產出並存檔，執行階段不再由 `FormSchema` 推導；移除 `FormSchema.GetFormLayout`、產生器轉公開 API、`FormView.Layout` 與 DefineEditor 產生入口。隨 4.23.0 發佈，決策見 [ADR-039](../../adr/adr-039-formlayout-design-time-only.md) |
| [定義編輯器：把宣告式 metadata 接回來](plan-definition-editor.md) | 2026-08-17（已拆分） | **本 plan 未執行即被拆為兩份獨立 plan**——TreeView 吃 `TreeNodeAttribute`、PropertyGrid 吃 `PropertyDescriptor`，兩者無共用程式碼，拆開可並行。後續進度見上層索引的 `plan-tree-view-builder.md` 與 `plan-property-grid-control.md` |
| [Northwind 案例走進客製覆蓋層](plan-northwind-customize-layer.md) | 2026-08-18 | 案例（`apps/Bee.Northwind`）首次接上客製覆蓋層：租戶客製語系資源改寫訂單表單的客戶欄標題，另加一份整份取代的 Order `FormLayout` 示範疊加粒度。**框架端不動**——`CustomizeOverlay` / `CustomizeDefineReader` 機制本就完好，缺的只有案例接線。獨立 repo `bee-northwind-avalonia` 之後另一輪同步 |
| [框架全面體檢（2026-08-07）](plan-framework-review-2026-08-07.md) | 2026-08-16（過期封存） | 十一面向唯讀體檢（基準 v4.17.0）。P0 / P3 全數落地，P1 / P2 部分完成；**未結的 P1 / P2 / P4 項目隨基準版本推進而過期，不再由本 plan 追蹤**，現象是否仍成立須重新確認。續輪見同目錄的 2026-08-11 體檢；移交的 D-3 / D-5 由 [plan-property-grid-control.md](../plan-property-grid-control.md) 與 [plan-tree-view-builder.md](../plan-tree-view-builder.md) 承接 |
| [XML doc 漂移全 repo 盤點與修正](plan-xmldoc-drift-audit.md) | 2026-08-15 | 對 `src/**/*.cs` 的 991 檔／26,263 行 `///` 做四類全掃，修掉 10 筆 A 級實質錯誤（其中 8 筆是清點數字漂掉）與 1 筆過期敘述。落地兩道閘門：`check-xmldoc-refs.sh` 掃 `<c>` 懸空識別字，`code-style.md` 加上「散文提到自家型別一律用 `<see cref>`」與「不寫程式碼構件的清點數字」 |
| [Bee.Northwind 同步至 bee-northwind-avalonia（框架 4.21.0）](plan-northwind-sync-4.21.0.md) | 2026-08-13 | 五階段全數落地：檔案同步、ProjectReference → PackageReference 4.21.0、README 雙語逐段 port、獨立 repo 五個 head build + 端到端冒煙（iOS 同日補驗）、commit 推上 `bee-northwind-avalonia` |
| [skill 與 rule 的分層歸位](plan-skills-cleanup.md) | 2026-08-12 | 起於 repo 層 skill 精簡，執行中擴為三層歸位：repo skill（綁 bee-library 的慣例）／plugin skill（跨 repo 的開發流程）／常駐 rule（不知道要查也會違反的硬規則）。收斂 bootstrap 複寫為單一來源，並跨到 `~/.claude/` 與 `claude-plugins` repo |
| [設定檔健檢（2026-08-12）](plan-config-audit-2026-08-12.md) | 2026-08-12 | 常駐設定語料（`CLAUDE.md`／`rules/`／skills／memory）首次基線健檢，六階段全數落地。修掉三處失效的 skill／plugin 指路與一處跨檔矛盾的 AOT 結論；建立「設定檔不寫清點數字」判準並清除既有者；推導內容與程式碼樣板搬入 `docs/repo-ops/`；`bee-jsonrpc-backend` skill 從使用者層搬入版控。常駐 −7.4%。**下次健檢的三項改進已記在文末** |
| [未使用型別盤點與清理（2026-08-12）](plan-unused-type-cleanup.md) | 2026-08-12 | Roslyn 符號級全掃（47 專案／995 型別／8038 成員）確認無死型別；真正的問題是三處「宣告了但沒接上」——`NumberFormatApplier` 從不執行、兩個公司錯誤碼從未拋出。另移除 `ApiContractRegistry`（被 `ApiOutputConverter` 取代）與 `ILogBusinessObject`，並補上 BO 側契約配對閘門 |
| [框架全面體檢（2026-08-11）](plan-framework-review-2026-08-11.md) | 2026-08-12 | 十一面向唯讀體檢（基準 v4.19.0）；修正後十一面向平均 8.09 → 9.13，另記 5 條刻意遞延與 2 筆待查 flaky |
| [解除 Bee.Definition 對 DynamicExpresso 的傳遞相依](plan-definition-expressions-decoupling.md) | 2026-08-11 | 抽象三型別下沉 `Bee.Base`，拿掉對 `Bee.Expressions` 的 ProjectReference；相依閘門由人眼掃描變成可執行檢查（ADR-038） |
| [Northwind 補齊 common / log 資料庫分類](plan-northwind-common-log-scopes.md) | 2026-08-11 | 兩個分類正式登錄、seeder 旁路建表退役、開啟稽核；框架補 `st_user` 預設認證，案例登入改走它 |
| [修復行動端（iOS）AOT 下的 MessagePack wire 路徑](plan-mobile-aot-wire.md) | 2026-08-10 | wire 型別全面脫離 contractless 改顯式註冊、`object` 成員改判別式封套；`DynamicCodeSupport=false` 納入回歸閘門（ADR-037） |
| [解除 Bee.Definition 對 MessagePack 的相依](plan-definition-messagepack-decoupling.md) | 2026-08-09 | 定義層移除全部標註與 `PackageReference`，wire 綁定改由 `Bee.Api.Core` 的手寫 formatter 承擔（ADR-036） |
| [盤點 `IObjectSerialize*` 家族並移除孤兒介面](plan-serialization-interface-cleanup.md) | 2026-08-09 | 移除 `IObjectSerializeProcess` 與隨之失效的 `SerializeFormat`；`IObjectSerializeEmpty` 盤點後推翻原訂移除，改補設計意圖 XML doc |
