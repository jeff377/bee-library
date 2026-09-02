# 未來工作構想

尚未啟動、也還沒寫成 plan 的方向。**這裡只記「為什麼要做、要等什麼、啟動時第一步是什麼」**；
真正啟動時依 `dev-workflow:plan-write` 寫 `docs/plans/plan-<主題>.md` 交使用者 review 後才執行。

## `sys_date` 系統欄位：替「單據日期」這個角色命名

**構想（2026-08-13 使用者於鐵人賽 Day 3 校稿時提出）**：新增 `sys_date` 系統欄位表示單據日期，
`FormBusinessObject.GetNewData` 認得它，預設填入使用者時區的今天。

⚠️ **動手前先知道：「填今天」現在就已經在做，而且範圍更寬。**
`FormRowDefaults.DefaultForDbType` 對 **每一個** `FieldDbType.Date` 欄位都回
`FrameworkClock.Today(timeZoneId)`（`DataFormRepository.GetNewData` → `FormRowDefaults.Apply`）。
所以「有 `sys_date` 就填今天」在效果上不新增任何行為。

**真正的價值不在預設值，在替一個角色命名** —— 與 `sys_id`（業務代碼）、`sys_name`（顯示欄）
同一個家族。一旦框架認得哪一欄是單據日期，能用它的就不只預設值：期間查詢的預設區間、
報表區間、關帳檢查、稽核歸屬哪一天。

**它同時暴露一個真問題**：`FormRowDefaults` 的 XML doc 自陳目的是 NOT NULL 填充
（"never reaches the database with a NULL"），而「單據日期預設今天」是**語意**預設。
兩件事現在共用同一條規則，症狀在 `apps/Bee.Northwind` 上看得到：`order_date` 填今天是對的，
**`hire_date` 填今天只是不讓它是 NULL**。

**要先答的四個問題**：

1. `sys_*` 是框架保留名（`docs/framework-reserved-names.zh-TW.md`），新增一個等於擴充保留字表，
   既有應用可能已用此欄名。
2. **要真正分開語意，就得讓沒標 `sys_date` 的 `Date` 欄位不再自動填今天 —— 那是破壞性行為變更**，
   要走版號與 CHANGELOG。不改的話 `sys_date` 只是別名，什麼都沒解決。
3. 一張單只有一個日期嗎？預計出貨日、到期日承載不了，還是得回到逐欄宣告那條路。
4. `FormField.DefaultValueExpression` 已出貨且白名單含 `Today()`，**同一件事宣告也表達得出來**，
   而且表達得了 `sys_date` 表達不了的（哪一欄要今天、哪一欄要月底）。

**分岔判準**（用專案自己那條「制式化的收斂、不一樣的留給應用」）：
「新增單據時日期預設今天」是制式的 → 該收斂；「哪一欄是那個日期」逐家不同 → 該宣告。
**兩條路各對一半，真正要決定的是「框架需不需要認得單據日期這個角色」。**

**要等什麼**：**等 2026 iThome 鐵人賽發文結束**。若框架加了 `sys_date` 且案例跟著改
（`order_date` → `sys_date`），連載至少四篇要重驗：Day 3 §2 的系統欄位表（現列五個，
且該節寫「這幾個名字不能改」）、Day 3 §5、Day 27 的案例段（兩個 `Date` 欄位正是它的素材）、
Day 29 的對帳表。**而鐵人賽發文後只有當日可改。**

**啟動時第一步**：先答上面第 2 題（破壞性變更的範圍），再決定要不要寫 plan。

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
| `dotnet bee apikey ...` / `dotnet bee admin ...` | CLI 天然是部署期工具、可進腳本。但 `tools/Bee.Cli` 目前只宣告 `Bee.Definition`（傳遞閉包為 `Bee.Definition` + `Bee.Base`；ADR-038 後已不含 `Bee.Expressions`），要接 DB 得把 `Bee.Business` 與 repository 一起拉進來——**這是本項最主要的決策**，會讓 CLI 從「定義檔工具」變成「需要連得上資料庫的維運工具」 |
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
執行過程見 [plan-mobile-aot-wire.md](../plans/archive/plan-mobile-aot-wire.md)。
已在五個環境驗證通過（閘門、NativeAOT、Mac Catalyst Release、iOS 模擬器 Release、
iOS 裝置 full-AOT 編譯）；**僅餘 iOS 實機執行期未測**，屬低風險形式缺口。

以下留結論摘要供索引。

- **iOS head 的 wire 曾整條不通**：adr-036 移除全部 `[MessagePackObject]` 標註後，
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

## per-form 稽核規則：讓管理員挑哪些表單要記錄

異動記錄與檢視記錄目前都是**全記所有表單**。實務上不是每張表單都值得記——
稽核價值集中在少數高敏感／高爭議的單據，其餘只是量體。

構想是一份**執行期**規則（不是編譯期旗標、也不是改程式）：管理員指定哪些 ProgId
要做異動／檢視記錄，可再細到 per-操作。對齊 Odoo 的 `auditlog.rule`——管理員在畫面上
挑 model，不動一行程式碼。SAP 那邊的對照是 Change Documents 的開發期旗標與 RAL 的
管理員設定，兩者一硬一軟，Odoo 的做法更接近這裡要的。

**預設必須維持全記**，否則既有部署升版後會靜默少記——稽核少記比多記危險得多。

分類軸與各項的定位見 [ADR-040](../adr/adr-040-audit-trail-taxonomy.md)。

## 匯率主檔與自動帶值：多幣別唯二的缺口

**構想（2026-09-01 使用者追問多幣別設計時盤點出來）**：補上**公司層的匯率主檔**，
以及**建單時按單據日期自動帶出匯率**。換算本身不缺——見下節。

### 前提：單據自帶匯率欄位（決定了其餘所有判斷）

多幣別單據的 ERP 慣例是**把當時的匯率直接存在單據主檔欄位上，以單據上的匯率為準**。
這個前提決定了整節的形狀：

- **匯率主檔的角色是「預設值來源」，不是「計算依據」**。建單時帶出 → 寫入單據欄位 →
  之後所有計算以單據上那個值為準。匯率表事後修改，已開單據不受影響。
- **手動覆寫天然成立**——合約約定匯率、預售鎖匯，使用者直接改單據欄位即可。
- **事後查帳看單據自己就夠**，不需回查匯率表歷史。

由此再推一層，決定匯率主檔的**正確性要求與失敗行為**：

- **取不到匯率不該阻擋建單** —— 它是參考值不是憑證資料。查無當日匯率時應留空或給 0
  讓使用者自行輸入，而非擲例外或擋下存檔。
- **取值策略可以是「不晚於單據日期的最近一筆」**，不必要求當日精確命中 ——
  假日、未維護日在實務上必然存在。
- **稽核要求相對低** —— 它不是憑證級資料；真正需要留痕的是單據上那個值，而那本來就
  隨單據走稽核軌跡。

### 換算能力已經具備（**曾被誤判為「整個不存在」**）

盤點初期判為「換算那半整個不存在」，**這個判斷高估了缺口**。單據自帶匯率欄位之後，
本幣換算用**純定義**即可成立，不需要任何新框架能力：

```
sys_exchange_rate   NumberKind="ExchangeRate"                       ← Preserve，不捨入
amt_doc             NumberKind="Amount"                             ← 走 schema.CurrencyField
amt_local           NumberKind="Amount" CurrencyField="sys_local_currency"
                    ValueExpression="amt_doc * sys_exchange_rate"
```

鏈上每一環都已實作：

- `ResolveRefCode` 讓**每個欄位各自指定 `CurrencyField`**，取不到才退回 `schema.CurrencyField`
  （`src/Bee.Definition/Forms/FormExpressionCalculator.cs`），代碼值取自該列的變數表 ——
  原幣欄與本幣欄因此**各自**解析到不同幣別的位數。
- 匯率欄是 `Preserve`（`NumberKindProfile.GetRoundingPolicy`），不會被捨到 5 位再拿去乘，
  乘法吃完整精度（符合 [ADR-026](../adr/adr-026-numeric-semantics-rounding.md) D4）。
- 乘積走 `RoundByKind` 捨到**本幣**位數後 round-then-sum。

因此 **ADR-026 D2 的「原幣/本幣各依自己的貨幣鍵欄獨立 round-then-sum」不是規劃，
是現在就成立的行為**。（靜態推導，每一環都對過實作；尚未寫測試實跑。）

**連帶更正：`NumberKind.ExchangeRate` 不是孤立標籤。** 盤點初期以「框架有沒有自己拿它去乘」
為準，判它零消費者近乎死碼 —— **判準錯了**。它的角色本來就是「單據上那個匯率欄位」的
數值語意宣告（5 位、不捨入、位數不隨公司變），而乘法本就該由 app 在 FormSchema 上宣告，
這正是 FormSchema 驅動的一貫策略。該標籤現在就可用。

### 真正的缺口

| 層 | 內容 | 狀態 |
|----|------|------|
| 系統層 | 幣別代碼、最小單位 | ✅ 已有 |
| 公司層 | 本位幣、可用幣別、現金捨入 | ✅ 已有 |
| **公司層資料** | **匯率主檔（帶日期）** | ❌ 缺 |
| 單據層 | 匯率欄位、本幣金額計算欄 | ✅ 已可宣告 |
| **框架** | **建單時按單據日期自動帶出匯率** | ❌ 缺 |

自動帶值有現成接縫：`FormBusinessObject.GetNewData` 或 `DefaultValueExpression` ——
與本檔 `sys_date` 那節是同一個機制，兩者宜一併考慮（單據日期正是取匯率的依據）。

**另有一處待釐清**：`CompanyInfo.DefaultCurrency` 的 XML doc 自稱
*default (local/home) currency*，但實際只當位數解析的 fallback 用 —— **它是預設幣別，
不是本位幣**。名稱與措辭暗示了一個框架其實沒有的概念，補換算時應一併釐清。

### 第一題：匯率放哪一層 —— 資料層，不是定義層

這題一度判錯（初次建議「跟 `CurrencySettings` 同層、走 `IDefineStorage` 與 `GetDefine` 通道」），
由使用者以租賃雲端情境指正。錯因是**把 SAP 的 client 層直接類比成本框架的系統層**：
SAP 一個 client = 一個企業集團（公司代碼共用財務政策，要出合併報表本就該同一組匯率），
本框架一個部署 = **多個彼此無關的企業**。兩者不是同一個層級。

即使採 SAP 那種不綁本幣的絕對報價模型（TCURR 存幣別對幣別，與公司無關），
SaaS 下仍然不能共用，理由有三：**取價來源不同**（台銀／日銀／ECB，同日同幣對數字不同）、
**法定匯率規定不同**（各國稅法指定交易日即期／月平均／期末）、
**匯率類型與財務政策不同**。Odoo 則更直接 —— `res.currency.rate` 存「1 單位公司本幣 =
rate 單位外幣」，`company_id` 是結構性必要維度（Odoo 自述該欄位當初就是從 `res.currency`
搬到 `res.currency.rate` 的，因為「幣別是全世界通用的定義，匯率則因公司與時間而異」）。

因此匯率應比照 `src/Bee.ObjectCaching/Database/` 既有那一族（`DepartmentTreeCache`、
`CompanyRolePermissionsCache`、`CompanyAuditRulesCache`），走 `ICacheDataSourceProvider`
+ company 資料庫 + cache-notify 失效，而非 `DefineType` + `IDefineAccess`。

**`CustomizeId` 不是替代解**：定義檔確實有 per-tenant 疊加（`CustomizeOverlay`），
但那是「這間公司用哪一套**客製定義版本**」—— 多間公司可共用同一個 `CustomizeId`，
且定義檔低頻、隨版本走。匯率是每日變動的業務資料，用它承載等於把資料塞進定義層。

**判準（可回頭檢查其他分層）**：**這個資料是「客觀事實」還是「商業判斷」？**
客觀事實（ISO 4217 幣別代碼與小數位、計量單位）→ 系統層定義檔；
商業判斷（匯率、現金捨入政策、可用幣別）→ 公司層資料。
框架既有分層**已經符合這條** —— `CompanyInfo.CashRounding` 與 `AllowedCurrencies` 在公司層、
`CurrencySettings` 在系統層。**`CurrencySettings` 留系統層是正確的，不受本項影響。**

### 第二題：匯率主檔的維度

| 維度 | 必要性 | 理由 |
|------|-------|------|
| 公司 | ✅ 第一版必要 | 見第一題 |
| **生效日期** | ✅ **第一版必要** | 建單時要按**單據日期**取值（補單／倒填日期不能給當前匯率）；期末評價要用期末匯率 |
| 幣別對（或幣別） | ✅ 第一版必要 | 見第三題 |
| 匯率類型（即期／記帳／月平均） | ⚠️ 可延後 | Odoo 就沒有；但 key 要預留，事後加維度是破壞性變更 |

> **日期維度的理由曾被寫錯**：初版寫成「事後查帳要能重現」。單據自帶匯率之後，
> 重現靠單據自己，不需回查匯率表。日期維度改由「建單時按單據日期取值」與「期末評價」撐著。

### 第三題：資料模型

| 模型 | 代表 | 特徵 |
|------|------|------|
| 相對本幣 | Odoo `res.currency.rate` | 筆數少；跨第三幣別靠本幣三角換算 |
| 幣別對絕對報價 | SAP `TCURR`（來源幣＋目標幣＋匯率類型＋生效日） | 可直接報價；筆數多 |

**傾向後者**：三角換算會**多一次捨入**（USD→TWD→JPY 的中間值要不要捨、捨到幾位），
直接報價沒有這個中間值。此誤差比匯率表大一些的儲存成本嚴重得多。

### 已知限制與待審項

- **集團合併報表沒有共用基準**：匯率完全 per company 後，同集團兩間公司各自維護，
  合併時對不起來（SAP 放 client 層正為此）。本框架目前沒有「集團」這一層。
  這是**已知限制**，真的需要時是**加一層**，不是把匯率搬回系統層。
- **`NumberKind.ExchangeRate` 固定 5 位，是否夠用取決於報價方向**：若單據一律存
  「1 外幣 = n 本幣」的正向報價（TWD/USD = 31.50000），5 位對絕大多數幣對夠用；
  只有 IDR、VND 這類極端幣別才需要 SAP TCURF 那種因子機制
  （ADR-026 尾段自列的未做項）。**反向報價（JPY→USD = 0.0067…）才會立刻不夠。**
- **`CurrencyItem.Name` 沒有多語系**：現為單一字串，而顯示名需要（US Dollar／美元／米ドル）。
  框架有 LanguageResource 機制，此處未接上。
- **`CurrencyItem.Symbol` 的顯示位置未表達**：前綴（`$1,234.56`）vs 後綴（`1 234,56 €`）
  是**地區慣例**、跟使用者語系走，不跟幣別走。Odoo 有 `position` 欄位但同樣掛在幣別上，
  其實也不完全對。
- **系統層的「最小單位」並沒有比「位數」多表達什麼**：現金捨入已分出去給
  `CompanyCashRounding` 之後，系統層 `CurrencyItem.Rounding` 依 ISO 4217 只可能是
  `1 / 0.1 / 0.01 / 0.001`；`CurrencySettings.DecimalsFromRounding` 就是在數 10 的次方
  （塞 0.05 只會被算成 2 位）。**這不用改**（與 Odoo 一致、便於未來對接），
  但別誤以為它承載了現金捨入那層語意。

**要等什麼**：等實際的多幣別換算需求（目前無 app 在用）。基礎設施已齊備 ——
company-scope 資料庫相依快取那一族是現成 pattern，`bee-add-cache-object` skill
已涵蓋其完整跨檔流程。

**啟動時第一步**：先寫一個 throw-away test 實跑上面那條純定義換算鏈（確認靜態推導成立），
再定資料模型（第三題），然後依 `bee-add-cache-object` 開匯率快取物件。
捨入政策本身另有 `docs/plans/plan-rounding-mode.md`。
