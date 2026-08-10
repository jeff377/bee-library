# 未來工作構想

尚未啟動、也還沒寫成 plan 的方向。**這裡只記「為什麼要做、要等什麼、啟動時第一步是什麼」**；
真正啟動時依 `plan-workflow:plan-write` 寫 `docs/plans/plan-<主題>.md` 交使用者 review 後才執行。

## 對外開發者 skill 包（Claude Code plugin）

**目標**：做一組給**使用 Bee.NET 框架的外部開發者**的 skill 包，讓他們快速上手。

**關鍵區別——這不是「把現有 skills 分享出去」**：

- `.claude/skills/bee-*`（`bee-app-scaffold` / `bee-add-form` / `bee-add-bo-method` 等）是
  **在 bee-library repo 內開發用**的內部視角：引用 `src/Bee.*`、`apps/Bee.Northwind` 等內部路徑。
  **直接 ship 給外部消費者不了**（原因是內部路徑與內部視角，不是版控與否——
  `.claude/skills/` 與 `.claude/commands/` 自 2026-07-23 起已入版控）。
- 開發者包是**消費端視角**（裝了 `Bee.*` NuGet 的人）：只引用公開 API surface + 公開文件 +
  公開範例；必須**版控、發佈、隨版本維護**；打包成 Claude Code **plugin**。

**散佈與錨點**：做成一個 plugin（如 `bee-dotnet`），**錨定在畢業後的公開
`bee-northwind-avalonia` repo** 當活範例（非內部 `apps/`）。

**要等什麼**：**自然接在 Northwind 畢業之後**——畢業才有公開參考實作可指。現在不必動。

**預計內容**（現有知識的消費端改寫 + 補上手路徑）：`bee-quickstart`（裝 NuGet → 最小 app → 跑起來）、
`bee-app-scaffold`（PackageReference 版）、`bee-add-form`、`bee-add-bo-method`、
`bee-formschema-reference`（lookup / 明細 / 下拉 / 唯讀 / scope 完整慣例）、
`bee-concepts`（FormSchema 中樞 / DataSet DTO / BO / Repository 雙軌 / common-company scope）、
序列化、快取。

**啟動時第一步**：寫 `docs/plans/plan-bee-developer-skills.md`（plugin 結構、各 skill 的消費端改寫、
散佈/維護機制、與發版綁定）。消費端最易錯的觀念是 DB scope，見 `.claude/rules/database.md`。

## 部署期作業的工具程式

> 原記為「API 金鑰管理工具程式」。2026-08-01 部署層管理員階段 1 落地後多了第二個消費者，
> 範圍隨之從「金鑰管理」擴為「所有 `LocalOnly` 的部署期作業」——這改變了下方落點的權衡。

**目標**：讓部署端不必寫程式就能執行部署期作業。

**為什麼需要**：`LocalOnly` 的方法只能在主機、行程內呼叫。能力都已交付，
但缺一個「在主機上跑一下就好」的入口，否則部署端得自己在 host 行程內寫呼叫程式碼。

**目前的消費者**：

| 作業 | 方法 | 現況 |
|------|------|------|
| 發放 API 金鑰 | `SystemBO.CreateApiKey` | 已交付，缺入口。2026-08-03 起遠端也走得通（須為部署層管理員），但**尚無管理員的部署仍只有本機這條路**——bootstrap 依舊需要入口 |
| 指派部署層管理員 | `SystemBO.SetDeploymentAdmin` | 已交付，缺入口。**且它是該欄唯一的寫入口**——沒有工具就只能自己寫程式，或手動 `UPDATE st_user` |
| 停用 / 列出金鑰 | 尚無 | 屬 API Key plan 的階段 3 |

`SetDeploymentAdmin` 這一列尤其尷尬：它是**首位管理員的唯一產生路徑**（設定檔 bootstrap 帳號
已否決為永久後門），新部署接上框架後的第一件事就會撞到它。

**現況限制**：框架目前**只有發放有程式路徑**。`IApiKeyRepository` 只有 `GetEnabledById` /
`GetGateState` / `Exists` / `Insert`，停用只能直接下 `UPDATE st_api_key`——而直接改 DB 不會 bump
`st_cache_notify`，其他行程最壞要等 `ApiKeyCache.AbsoluteMinutes`（60 分）才失效。
停用 / 列出的 API 屬 API Key plan 的階段 3，本工具屆時才能做全套。

**兩個候選落點**：

| 落點 | 取捨 |
|------|------|
| `dotnet bee apikey ...` / `dotnet bee admin ...` | CLI 天然是部署期工具、可進腳本。但 `tools/Bee.Cli` 目前只引用 `Bee.Base` / `Bee.Definition` / `Bee.Expressions`，要接 DB 得把 `Bee.Business` 與 repository 一起拉進來——**這是本項最主要的決策**，會讓 CLI 從「定義檔工具」變成「需要連得上資料庫的維運工具」 |
| DefineEditor 加一個分頁 | 已是本機 Avalonia 工具、已有 DI 宿主。但它的定位是編輯定義檔，而金鑰與管理員旗標都在 DB 不在定義檔 |

**第二個消費者如何改變權衡**：CLI 那一格的成本（把 `Bee.Business` 與 repository 拉進
`tools/Bee.Cli`）是**一次性**的，接上之後每個新的部署期作業都只是多一個子命令。原本為單一功能
付這筆相依成本顯得重，現在有兩個消費者、且 API Key plan 階段 3 還會再加三個，攤提就合理得多。
反過來說，DefineEditor 那一格的「定位不符」問題只會隨消費者增加而放大——它的分頁會逐漸變成
一個與定義檔無關的維運面板。

**要等什麼**：現有兩個消費者都不等任何東西（能力已交付），純粹是還沒排；
停用 / 列出要等 API Key plan 的階段 3——該階段已於 2026-08-03 解除受阻（遠端管理表單所需的
授權路徑已就緒），本工具屆時才能做全套。

**啟動時第一步**：先決上表的落點與相依取捨，再寫 plan。

## 租戶層的宣告式邏輯（線該畫在哪裡）

**目標**：讓**異動成本與需求大小相稱**——簡單的邏輯異動只需改定義檔，複雜的才走組件部署。
不是「租戶層都不該更版」，而是這條線目前被迫畫在極端位置。

**現況**：這條線在**套裝層已經存在**——宣告式運算式規則（FormSchema 內）處理簡單的，
BO 程式碼處理複雜的，兩者各安其位。**租戶層只有「程式碼」那一半**：

| | 簡單邏輯 | 複雜邏輯 |
|---|---------|---------|
| **套裝層** | 運算式規則（改定義檔） | BO / plugin（組件） |
| **租戶層** | ❌ **缺這一半** | ✅ 客製 BO / Repository / plugin |

因此租戶要改一條折扣公式（理應最簡單那類）與要串接外部 ERP（理應最複雜那類），
**付一樣的代價**：改 `.cs`、重編譯、部署到共用的 host `bin`。

**為何這在多租戶下特別痛**：組件部署的成本不對稱。單租戶時只是停機一次；多租戶下
租戶 A 的一行公式改動，要讓 B、C、D 一起承擔部署風險——協調全體維護視窗、回歸測試所有租戶、
任一租戶出事就整批 rollback。且所有租戶的客製組件都在同一個 `bin`，綁在同一次編譯裡。

**核心設計問題（啟動時要先回答）**：

1. **線該畫在哪裡** —— 哪些異動屬「定義檔側」、哪些屬「組件側」。
   套裝層的既有分界（`IFormRuleProcessor` 的運算式 vs BO 程式碼）是起點，但租戶層的需求分布未必相同。
2. **租戶層的「定義檔側」用什麼承載** —— 有個現成的死結：套裝層的宣告式規則存在 `FormSchema` 內，
   而 `FormSchema` 依 [ADR-016](../adr/adr-016-multitenant-customization-overlay.md) **永久不可客製**
   （它同時驅動資料庫結構，逐租戶分歧會讓實體 schema 裂開）。因此租戶專屬的規則需要**另一個落點**
   ——可能是 `PluginSettings` 的延伸、也可能是新的定義型別。

**要等什麼**：不等任何東西。`PluginSettings`（[ADR-035](../adr/adr-035-business-logic-plugin.md)）
已證明「客製定義可寫 + 有維護 API + 快取失效鏈」這條路走得通，是本項的現成基礎設施。

**啟動時第一步**：先蒐集實際的租戶客製案例，按「若有宣告式規則能否解決」分類——
用真實分布決定第 1 題的線畫在哪，而不是憑想像設計語法。

## 行動端 AOT：MessagePack wire 路徑的 reflection-only 失敗

**已追查完畢並修復（2026-08-10）：是真實缺陷，不是模擬假象。**
修復記於 [ADR-037](../adr/adr-037-wire-explicit-registration.md)，
執行過程見 [plan-mobile-aot-wire.md](../plans/plan-mobile-aot-wire.md)。
**剩餘工作只有一項：真 Apple runtime（iOS 模擬器 / Mac Catalyst）的形式驗證**
——已驗證的兩個環境是 CoreCLR 關開關與 NativeAOT，都不是 Mono full-AOT。

以下留結論摘要供索引。

- **iOS head 的 wire 目前不通**：adr-036 移除全部 `[MessagePackObject]` 標註後，
  wire 型別改由 contractless 承載，而 **contractless 沒有 reflection fallback**
  （MessagePack 的 fallback 只涵蓋有標註的合約型別，NativeAOT 對照實驗證實）。
- **「模擬」就是 iOS SDK 自己設的開關**：`Microsoft.iOS.Sdk` 對 iOS / tvOS / MacCatalyst
  的每一種組態預設 `DynamicCodeSupport=false`，SDK 再映射成同一個
  `RuntimeHostConfigurationOption`。**Android 沒有這一條，驗不到這半。**
- **`InvalidProgramException` 確實是模擬特有的症狀**，但那只表示症狀失真——
  同一批案例在 NativeAOT（真無動態碼）上照樣失敗。例外種類不可當診斷依據。
- **adr-036 放大了缺陷而非縮小**：同一口徑下 37（v4.18.0）→ 185（v4.19.0）。
- 既有缺陷另有一處早於 adr-036：typeless 通道對
  `Decimal` / `Guid` / `DateTime` / `DateOnly` / `Byte[]` 不可用。

重現只需一個命令列屬性，不需改 csproj：

```bash
dotnet test tests/Bee.Api.Core.UnitTests/Bee.Api.Core.UnitTests.csproj -c Release --settings .runsettings -p:DynamicCodeSupport=false
```

判讀與重現法的完整規範已收進 `.claude/rules/apple-mobile-trim.md`
與 `.claude/rules/serialization.md`。
