# Plan：客製化共同前置

> 狀態：🚧 進行中（F1、F2、F3 已完成，F4 的缺口 E 已補；僅剩 F4 缺口 D）· 2026-08-01
> 定位：**三類客製的共同基礎**，不屬於任一類，但三類都被它擋住。
> 相關：[Layout 客製](plan-customization-layout.md)｜[業務邏輯客製](plan-customization-business.md)｜[語系客製](plan-customization-language.md)
> 依據：[ADR-016 多租戶客製化覆蓋層](../adr/adr-016-multitenant-customization-overlay.md)

---

## 0. 一句話結論

**ADR-016 的基礎設施 100% 蓋好了（含 66 個單元測試），但「最後一哩」的消費端接線完全沒做——
整個 `src/` 沒有任何一行讀取 `SessionInfo.CustomizeId` 餵給 overlay。三類客製在正式路徑上目前全部無效。**

這解釋了「客製化還未處理」的體感：機制存在、測試會過，但實際跑起來永遠是 base 層。
ADR-016 設計的 fail-safe（漏傳 customizeId → 退化純 base）目前是 **100% 觸發狀態**。

**本 plan 是三類客製的前置**——不先補完 §2 的 A、B 兩項，任何一類客製都不會生效。

> 2026-07-31 討論後，A1（傳遞方式）與 B1（配置來源）已定案，**F1–F4 可動工**；
> 唯「客製檔誰維護、怎麼產生」保留未決（不擋動工）。決策彙總見 §5。

> **2026-07-31 進度更正（F1 + F2 落地後）**：上述「沒有任何一行讀取 `SessionInfo.CustomizeId`」
> **已不再成立**——語系與 BO 型別解析四個消費端都已接線（§2.A），`CustomizePath` 也有 host 示範（§2.B）。
> **仍未生效的是用戶端進公司流程**
> （§2.C：至今沒有 head 走過 `EnterCompany`，所以實務上 `CustomizeId` 仍恆為空）。
> 換句話說：伺服端管道已通，但**還沒有任何部署會餵值進去**——這正是 F3 的範圍。

> **2026-08-01 再更正（F3 落地後）**：整條鏈已由端到端整合測試走通並驗證
> （`st_company.customize_id` → `EnterCompany` → `SessionInfo.CustomizeId` → 語系／BO 型別／
> Layout 三類客製生效，見 §2.F）。**尚無 head 走 `EnterCompany` 仍然為真**，
> 但那已是「能力備妥、尚無部署使用」，不再是驗證缺口。

---

## 1. 已完成的基礎設施（可直接沿用）

| 項目 | 位置 | 狀態 |
|------|------|------|
| `CustomizeId` 寫入鏈 | `st_company.customize_id` → `CompanyRepository`(`:80`) → `CompanyInfo`(`:59`) → `SessionCompanyBinder.Bind`(`:68`，由 `EnterCompany` 呼叫) → `SessionInfo.CustomizeId`(`:63`) | ✅ 完整 |
| `CustomizeId` 清除 | `ClearCompanyContext`(`Session.cs:207`)，由 `LeaveCompany`(`:161`) / `Logout`(`:188`) 共用 | ✅ |
| 客製路徑解析 | `CustomizeOnlyPathOptions`（含 path traversal 防護 `:41-56`） | ✅ |
| 客製儲存讀取 | `CustomizeOnlyStorage`（3 getter 有效，其餘 `NotSupportedException`） | ✅ |
| 客製快取容器 | `CacheContainerProvider`（per-`CustomizeId` 隔離 `:38-43`） | ✅ |
| 客製讀取器 | `CustomizeDefineReader` + `ICustomizeDefineReader`（空值短路 `:69-70`） | ✅ |
| DI 註冊 | `BeeFrameworkServiceCollectionExtensions:88-102,175-178,205-209` | ✅ reader 已注入三個消費端 |
| 測試 | 10 檔 / 約 66 測試（元件級）＋ `TenantCustomizationEndToEndTests` 9 測試（端到端，F3） | ✅ |

**客製檔路徑**（`CustomizeOnlyPathOptions:60-70`）：
```
{CustomizePath}/{customizeId}/ProgramSettings.xml
{CustomizePath}/{customizeId}/FormLayout/{layoutId}.FormLayout.xml
{CustomizePath}/{customizeId}/Language/{lang}/{ns}.Language.xml
```

**Fallback 責任分工**：override 層本身**不 fallback**（無檔回 `null`），fallback 由各消費端負責。

---

## 2. 橫向缺口

### 🟢 A. 消費端未接線（**F2 已補完，Layout 除外**）

DI 接線本身完整，reader 已注入；問題純粹是**呼叫端沒傳 `customizeId`**：

| 消費端 | 位置 | 現況呼叫 | 應為 | 影響類別 | 狀態 |
|--------|------|---------|------|---------|------|
| BO 型別解析 | `BusinessObjectFactory.cs:70` | `Resolve(progId)` | `Resolve(session.CustomizeId, progId)` | 業務邏輯 | ✅ F2 |
| BO 語系 | `BusinessObject.cs:95-105` | `GetLangText(lang, ...)` | 加 customizeId | 語系 | ✅ F2（新增 `GetCurrentCustomizeId()`） |
| 一般在地化 | `BeeStringLocalizer.cs:60,66` | base overload | 加 customizeId 管道 | 語系 | ✅ F2（3-arg ctor 委派多載；**repo 內無註冊點／消費端，僅加 API 未端到端驗證**） |
| Schema 在地化 | `FormSchemaLocalizer.cs:73,89,101,136` | base overload | 加 customizeId 管道 | 語系 | ✅ F2（`Localize` customizeId 多載） |
| FormLayout | `CacheDefineAccess.cs:112`（`GetDefine`） | `GetFormLayout(keys[0])` | ~~簽章無 customizeId，需另議~~ **不需改** | Layout | ✅ 非缺口（2026-08-01 裁決） |

> `BusinessObjectFactory` 連 `ISessionInfoService` 都已注入(`:25`)，只是沒用它讀 `CustomizeId`。
> 多數接線是「補傳一個參數」，但 Layout 那條有結構問題（見 Layout plan）。

> **2026-08-01 裁決：最後一列不是缺口。** `GetDefine` 的語意是**未經任何處理的原始定義檔**，
> 客製疊加、生成、在地化等運行階段加工一律歸 `GetFormLayout`——所以 `GetDefine` 走單參數
> 多載是正確的，不需要 customizeId。Layout 真正的結構問題在別處（API 從 schema 即時生成、
> 且兩個 UI head 根本不向 server 要 layout），見 [Layout plan](plan-customization-layout.md) §1.2 與決策 L4。

**決策 A1：customizeId 的傳遞方式 — ✅ 已定案（2026-07-31）：依消費端性質二分，來源都是 `EnterCompany` 已回傳的值**

五個消費端不是同一類東西，用同一種傳遞方式反而彆扭：

| 消費端類別 | customizeId 來源 | 作法 |
|-----------|-----------------|------|
| 伺服端、手上有 session（`BusinessObjectFactory`、BO 語系、`FormSchemaLocalizer`） | `SessionInfo.CustomizeId`（`SessionCompanyBinder` 已填） | 顯式傳參（維持 ADR-016 設計） |
| UI 端 DI adapter、無 session 概念（`BeeStringLocalizer`） | `ClientInfo.Company?.CustomizeId` | 比照它既有的 `Func<string> langProvider` 多載（`BeeStringLocalizer.cs:46`），加一個 customizeId 委派多載由 host 接 |

**不新增任何 wire 欄位**：`EnterCompanyResult.Company` 就是 `CompanyInfo`
（[`SystemBusinessObject.Session.cs:110`](../../src/Bee.Business/System/SystemBusinessObject.Session.cs)），本來就帶 `CustomizeId`；
用戶端 `ClientInfo.ApplyEnterCompanyResult` 也已把它存進 `ClientInfo.Company`
（[`ClientInfo.cs:203`](../../src/Bee.UI.Core/ClientInfo.cs)）。兩端所需的值今天都已經在手上。

> **安全界線（硬性）**：伺服端**永不**採信 client 傳回來的 customizeId 作為查找依據。
> 做成「client 每次呼叫帶 customizeId」等於讓 client 自選要讀哪一套客製檔——跨租戶讀取的直接破口。
> client 手上那份**只供 client 自己的 UI 在地化**；伺服端一律只認 `SessionInfo.CustomizeId`。

**未採用**：`ICustomizeContext`（scoped DI 隱式上下文）——與框架顯式傳參風格不一致，且非 HTTP 情境
（背景工作）的生命週期要另解；上表的二分已經避開「強迫沒有 session 概念的 UI adapter 去拿 session」這個原始痛點。

> 殘留風險：漏傳的代價是「靜默退回 base」——不會壞、但客製悄悄失效，難察覺。以 §2.F 的端到端測試作為長期防線。

**客製化維度只到公司層（兩個邊界，須寫進文件）**

`CustomizeId` 掛在 `CompanyInfo` 是對的——它與 `CompanyDatabaseId` / `NumberFormats` / `DefaultCurrency`
同屬「這家公司套哪一套規則」，基數是 many-to-one（集團多公司共用一套客製），生命週期也與 `EnterCompany`
／`ClearCompanyContext` 同進同退。但由此推得兩個邊界：

- **登入前拿不到客製**：`CustomizeId` 要進公司後才有值，所以登入畫面、公司選單、`EnterCompany` 之前的
  所有訊息與語系**永遠是 base**。若日後需要「第一畫面就換術語」，正解是**加第二層來源**（host 建
  `PathOptions` 時給部署層 default customizeId，公司層再 override），而非把 `CustomizeId` 從 `CompanyInfo` 搬走。
- **`SessionInfo.CustomizeId` 是快照不是即時值**：`SessionCompanyBinder:68` 於進公司當下複製；事後改
  `st_company.customize_id`，既有 session 不會跟著變（需重新 `EnterCompany`）。與同段的 `Roles` /
  capabilities / employeeContext 快照策略一致，可接受，但**必須寫進文件**，否則會變成「客製改了沒生效」的客訴。

### 🟢 B. `CustomizePath` 沒有任何 host 設定（**F1 已補完**）

> **前提更正（2026-07-31）**：本節原寫「無配置管道」，並把 B1-a 描述為「與 `DefinePath` 同一機制」——
> 兩者都不正確。**框架完全沒有組態綁定**：`DefinePath` 是 host 自己算好
> （`NorthwindBackend.ResolveDefinePath()` 往上找 `Define/SystemSettings.xml`），塞進
> `new PathOptions { ... }` 再傳給 `AddBeeFramework(configuration, pathOptions)`
> （[`BeeFrameworkServiceCollectionExtensions.cs:55-72`](../../src/Bee.Hosting/BeeFrameworkServiceCollectionExtensions.cs)）。
> 因此 `CustomizePath` **今天就設得了**（`init` 允許物件初始化器一併給值）。

實際缺的不是機制，是：**沒有任何 host 這樣做**（`NorthwindBackend.cs:47`、`DemoBackend.cs:43` 都只設
`DefinePath`）、**沒有文件**、**沒有預設慣例**。

**決策 B1：配置來源 — ✅ 已定案（2026-07-31）：維持 host 自建 `PathOptions`，不新增框架機制**

host 在 `new PathOptions { DefinePath = ..., CustomizePath = ... }` 時一併給值，與 `DefinePath` 真正一致。
框架端只需補**文件** + **一個 sample 示範**，F1 幾乎歸零。

**未採用**：
- B1-a（框架提供 `IConfiguration` / appsettings 綁定）——會是框架首次引入組態綁定，且 `DefinePath` 沒跟進會造成兩套風格並存。
- B1-b（放進 `SystemSettings.xml`）——該檔本身要靠 `DefinePath` 才讀得到（`SystemSettingsLoader.Load(paths)`），
  須先建 `PathOptions`、讀完設定後再重建一次，時序較繞。

### 決策 A2：定義供應與客製選用的分工 — ✅ 已定案（2026-08-01）

> **本決策推翻先前多項裁決**，是本輪最大的架構調整。原先假設「伺服端把客製疊加好、
> client 無感」，實作到 Layout 的用戶端環節時撞牆：`FormLayout` 家族的巢狀集合是 get-only，
> **JSON / MessagePack 送得出去、收不回來**（`SystemApiConnector.cs:191-203`），
> .NET 遠端用戶端會拿到「有純量欄位、沒有 sections」的 layout **且不報錯**。
> 由此重新檢視，得到下列原則。

**原則：API 只供應原始定義；套裝／客製的「選用」是一段前後端共用的邏輯，不是伺服端特權。**

| API | 定位 | 序列化 | 內容 |
|-----|------|--------|------|
| `GetDefine` | **全類型**入口，**存取權限要求高**，供工具程式使用（定義編輯器、部署腳本） | XML | 原始定義 |
| `GetFormSchema` / `GetFormLayout` / `GetLanguage` | **分型別**入口，供**一般用戶端**使用 | XML | 原始定義 |

四個方法**一律 XML、一律原始**——不生成、不疊加、不在地化。

**套裝與客製各取一次**，不合併成單一回應：connector 負責 XML → 物件的轉換，
維持既有方法合約不變，只是多一個取客製的對應方法。

**選用邏輯抽成前後端通用的取用類別，放 `Bee.Definition`**：需求端把套裝與客製兩份定義交給它，
由它決定每個 key（或整檔）取哪一邊。**伺服端現有的疊加邏輯改用同一個類別**——
`LanguageService`、`FormSchemaLocalizer`、`ProgramSettingsFormBoTypeResolver` 都不再各自實作疊加，
避免「server 算出一種、client 算出另一種」。

> **安全界線不變**：client 取客製定義時**不得指定 customizeId**——要哪一個租戶的客製，
> 一律由伺服端依 `SessionInfo.CustomizeId` 決定。共用的是**選用演算法**，不是**選擇權**。

**被本決策推翻 / 修正的先前裁決**：

| 出處 | 原裁決 | 現況 |
|------|--------|------|
| [Layout plan](plan-customization-layout.md) 決策 L4 | `GetFormLayout` = 運行階段版本（含生成、疊加、在地化） | ❌ 推翻：改為原始定義。生成與在地化移到需求端 |
| [Layout plan](plan-customization-layout.md) 決策 L5-a | 伺服端讀檔後以在地化 schema 回填 caption | ❌ 不在伺服端做；回填仍需要，但發生在組裝運行階段 layout 的那一端 |
| [語系 plan](plan-customization-language.md) 決策 G3 | 傾向 G3-a（server 疊好再回傳） | ✅ 改採 **G3-b**（回兩份，需求端以共用類別疊加） |
| 本檔 §2.A | 伺服端疊加後回傳，client 無感 | 修正：**伺服端仍需疊加能力**（BO 訊息、驗證文字都在 server 算），但改用共用類別；API 回傳則是原始定義 |
| `GetFormSchema` | server 先在地化再回傳 | ❌ 改回原始 schema，在地化交給 client |

**待辦（本決策落地前，客製對 .NET head 仍不生效）**：

1. ~~抽出共用取用類別（`Bee.Definition`），伺服端三個消費端改用它。~~
   ✅ **已完成（2026-08-01）**：`Bee.Definition.Customization.CustomizeOverlay`——純決策邏輯，
   不碰 storage / cache / session / DI。四個方法各自編碼該型別的粒度：
   `TryGetLangText`（per key）、`GetLangEnum`（整組）、`FindProgramItem`（per progId）、
   `PickFormLayout`（整檔）。`LanguageService`、`ProgramSettingsFormBoTypeResolver`、
   `CacheDefineAccess` 都改為「取得兩層 → 交給它決定」，疊加演算法全 repo 只剩一份。
2. `GetFormSchema` / `GetFormLayout` / `GetLanguage` 改回原始定義 + XML 信封。
3. connector 補「取客製定義」的對應方法（租戶由 session 決定，不收參數）。
4. UI head 改為：取原始 schema + 兩份語系 → 在地化 → 取兩份 layout 定義（缺則由 schema 生成）。
5. 回收 commit `8a418382` 中因本決策而不再需要的部分（見 Layout plan 階段表）。

### 🟢 C. 用戶端「進公司」流程從未被任何 head 走過（**F3 已補完**）

> **改寫（2026-07-31）**：本節原記為「`ClientInfo.ResetDefineCache()` 無任何呼叫端」。實測全域
> （`src/`、`samples/`、`apps/`、`tools/`）grep 後發現範圍更大——`SystemApiConnector.EnterCompanyAsync`、
> `ClientInfo.ApplyEnterCompanyResult`、`ClientInfo.ClearCompanyContext()` **三者皆無任何呼叫端**。
> Northwind 與各 sample 都是單公司、跳過 `EnterCompany`。

`ResetDefineCache` 沒人叫只是**表徵**，不是獨立缺口：整條用戶端 tenant context 流程都還沒有 head 實作過。

**影響**：
1. client 端跨租戶快取污染防護未生效（切換公司後仍可能顯示前一租戶的客製定義）。
2. **本 plan 定案的 UI 端路徑（§2.A 第二列）目前無從驗證**——要驗證得先有一個會進公司的 head，
   或走整合測試。**因此原本分開的「接上 `ResetDefineCache`」與「端到端測試」合流為單一階段 F3**（見 §4）。

**建議**：把 `ResetDefineCache()` 的責任從 host **收回框架**——直接在 `ApplyEnterCompanyResult` /
`ClearCompanyContext` 內部呼叫。現行 XML doc 寫「The host calls this … alongside `ResetDefineCache`」
（`ClientInfo.cs:148,200,213`），而事實已證明沒有 host 會記得；靠註解約束跨租戶快取污染防護不可靠。

> **✅ 前半已完成（2026-08-01，commit `5f741647`）**：`ResetDefineCache()` 現由
> `ApplyEnterCompanyResult` 與 `ClearCompanyContext` 內部呼叫，host 不需再記得。
>
> **✅ 後半（端到端驗證）已完成（2026-08-01）**：依使用者裁決採**整合測試自建 session**
> （選項 a），不改造既有 head。`TenantCustomizationEndToEndTests`
> （`tests/Bee.Api.Client.UnitTests/Customization/`）走完整條路：`st_company.customize_id`
> → `SystemApiConnector.EnterCompanyAsync` → `SessionInfo.CustomizeId` → 客製生效。
> 詳見 §2.F。
>
> 仍成立的一點：**沒有任何 head 走過 `EnterCompany`**（Northwind 與各 sample 都是單公司）。
> 這已不是驗證缺口（整合測試蓋掉了），而是「框架能力已備、尚無部署使用」——與 Layout plan
> §3 對 base 手工 layout 的結論同性質。

### 🟡 D. 客製快取沒有失效訊號

`CustomizeOnlyStorage` **未實作 `GetChangeSource`**，落到 `IDefineStorage` 的 default
（`IDefineStorage.cs:108-123`，「The default reports no signal」）。
因此 `FormLayoutCache.GetPolicy:36-38`、`LanguageResourceCache:40-42`、`ProgramSettingsCache:34-36`
在客製 container 裡拿到空 `FilePaths` + 空 `NotifyKey`。

**後果**：客製檔改動**不觸發失效**，只能等 20 分鐘 sliding expiration。
base 層有 file-watch（`FileDefineStorage.cs:232`）、DB 層有 cache-notify（`DbDefineStorage.cs:357`），
**只有客製層兩者皆無**。

> **建議**：實作 `CustomizeOnlyStorage.GetChangeSource`，比照 base 的 file-watch。

### 🟢 E. DB 儲存 + 客製的組合是壞的（**已修，2026-08-01**）

`DbDefineStorage.cs:36,201-213` 已實作完整的 DB 版 `ICustomizeDefineReader`（base 用 sentinel `customize_id = "*"`），
但 `BeeFrameworkServiceCollectionExtensions.cs:90-93` **無條件註冊檔案版** `CustomizeDefineReader`。

> **建議**：條件註冊——若 `IDefineStorage` 本身實作 `ICustomizeDefineReader` 就用它。

> **✅ 已修（2026-08-01）**：`BeeFrameworkServiceCollectionExtensions` 改為
> `sp.GetRequiredService<IDefineStorage>() as ICustomizeDefineReader ?? new CustomizeDefineReader(...)`。
> 兩個方向都有測試（storage 有實作 → 選 storage 本身；沒有 → 退回檔案版）。
>
> 順帶記下**兩種 storage 的客製實作方式刻意不同**，這不是不一致而是依差異程度分的：
> File 走**獨立類別**（`CustomizeOnlyStorage` + `CustomizeDefineReader`）——base 與客製是不同根
> 目錄、不同路徑組成，且客製只服務 3 型別且唯讀，拆開能把約束編碼進型別；
> DB 走**同一類別加方法**（`DbDefineStorage : IDefineStorage, ICustomizeDefineReader`）——base 與
> 客製是同一張 `st_define` 的不同列，只差 `customize_id`（base 用哨兵 `"*"`），同一組 SQL 兩用，
> 拆開只會複製連線與序列化。

### 🟢 F. 無端到端測試（缺口 A 從未被測出的原因）—— **F3 已補完**

66 個測試**全部是元件級**，都是測試自己手動傳 `"acme"` 字串。
**零個測試驗證「從 API 呼叫進來 → 自動套用該 session 的客製」**。

> **✅ 已補（2026-08-01）**：`tests/Bee.Api.Client.UnitTests/Customization/TenantCustomizationEndToEndTests.cs`
> ——9 個測試，**沒有任何一個手動傳 customizeId**，租戶一律由 session 決定。
>
> | 覆蓋 | 驗證內容 |
> |------|---------|
> | 語系 | 進帶 `customize_id` 的公司後，`FormDefinitionLoader.GetLocalizedSchemaAsync` 取得的 `Field.sys_name.Caption` 為客製值、`city` 仍為 base（per-key 疊加），`Schema.DisplayName` 為客製值 |
> | BO 型別解析 | 客製 `ProgramSettings` 綁定的 BO 型別被實際解析並建立（base 未註冊該 progId） |
> | Layout | 執行階段 layout 整檔採用客製定義（欄位數 2 vs base 7），且 caption 取自在地化 schema（決策 L5-a）——三層鏈在同一個斷言裡 |
> | 離開公司 | `LeaveCompanyAsync` 後重新取得即回到 base 文字 |
> | 跨租戶隔離 | 另一個 `customize_id`（無客製檔）→ schema 序列化後與純 base **逐位元一致**、BO 型別為預設 `FormBusinessObject` |
> | 回歸防護 | 未進公司的 session → 同上逐位元一致；layout 來自 base 定義檔 |
>
> **測試基礎設施的兩項改動**：
> 1. `TestProcessBootstrap` 新增 `SharedCustomizePath`（per-process 空 temp 目錄），並寫進
>    bootstrap 的 `PathOptions.CustomizePath`。**必須是 bootstrap 那一份**——近端 API 走
>    `ApiClientInfo.LocalServiceProvider`，只給 fixture 設客製根目錄的話，測試寫入的目錄
>    與 API 讀取的目錄會是兩個。
> 2. `BeeTestFixtureBuilder` 讓 fixture 的 `PathOptions` 指向同一個根目錄，兩邊一致。
>
> 根目錄預設為空，所以**其他測試行為零變化**：客製 reader 只在 `CustomizeId` 非空時才被觸及，
> 而沒有對應資料夾的 id 一律回 `null`。
>
> **反向驗證**（確認測試不是假通過）：暫時讓 `INSERT st_company` 不寫 `customize_id`，
> 3 個客製測試立刻失敗、6 個 base/回歸測試仍通過——證明整條鏈真的被走到，而非被短路。

### 🟢 其他觀察

- **無任何 TODO/FIXME 標記**（`grep "// TODO" src/` 零結果）——缺口是「靜默未接線」而非「標記待辦」。
- `CustomizeDefineReader.GetCustomizeProgramSettings:44-54` 因 `ProgramSettingsCache` 缺檔會 throw，
  改用 `File.Exists` 先探檔 → **每次呼叫都做一次同步檔案 I/O**（非 cache-only 路徑）。可列為最佳化項。

---

## 3. ADR-016 明確排除（非缺口，已裁決）

- **`FormSchema` / `TableSchema` 不可客製** — 兩者同時驅動 UI / DB schema / 驗證規則，
  逐租戶分歧會讓 DB 結構裂開。ADR 稱「永久排除，非延後」。
- **客製定義只讀不寫** — `CustomizeOnlyStorage` 所有 `SaveXxx` 全 throw，客製檔由外部工具/部署流程產生。
- **不提供 base ∪ cust 聯集列舉**。

> **⏳ 未決（跨三類）：客製檔實務上誰維護、怎麼產生？** —— 2026-07-31 討論時明確保留，未裁決。
> ADR 定「只讀不寫」，若需要在系統內編輯客製，就得重新檢視這條，並回答「編輯後如何不破壞 base 升級路徑」。
>
> 已知的低成本選項（供日後裁決時參考）：`tools/DefineEditor` 已能編輯 FormLayout / Language /
> ProgramSettings —— 正好就是 ADR-016 允許客製的三種。讓它多認一個 `{CustomizePath}/{customizeId}/`
> 根目錄，即可在**不動框架、不重開「只讀不寫」**的前提下解決客製檔的產生。

---

## 4. 建議階段

| 階段 | 範圍 | 狀態 |
|------|------|------|
| F0 | 決策定案：A1 傳遞方式、B1 配置來源 | ✅ 已定案（2026-07-31）。§3 的「客製檔誰維護」仍未決，但**不擋 F1–F4** |
| F1 | **缺口 B**：host 設定 `CustomizePath` 的文件 + 一個 sample 示範 | ✅ 已完成（2026-07-31）。`DemoBackend` 設 `CustomizePath`（`Define/` 的同層 `Customize/`）；文件見 [`definition-files-overview`](../definition-files-overview.md) §7（雙語）。依使用者決定**不入版控任何樣本客製檔**——只開路徑，客製層仍為空 |
| F2 | **缺口 A**：消費端接線，伺服端三處顯式傳參 + `BeeStringLocalizer` 委派多載（Layout 除外，見 Layout plan） | ✅ 已完成（2026-07-31）。四處全接：`FormSchemaLocalizer`、`BusinessObject.GetLangText`、`BeeStringLocalizer<T>`、`BusinessObjectFactory` |
| F3 | **缺口 C + F 合流**：會進公司的 head（或整合測試）走通 `EnterCompany` → `ApplyEnterCompanyResult` → 客製生效，並把 `ResetDefineCache` 責任收回框架 | ✅ 已完成（2026-08-01）。前半 commit `5f741647`（`ResetDefineCache` 收回框架）；後半採**整合測試自建 session**（使用者裁決選項 a），`TenantCustomizationEndToEndTests` 9 測試，語系／BO 型別／Layout／跨租戶隔離／回歸防護全覆蓋（見 §2.F） |
| F4 | **缺口 D、E**：客製快取失效訊號、DB 版 reader 條件註冊 | 🚧 進行中——**缺口 E（DB 版 reader 條件註冊）已完成**（2026-08-01，隨 Layout L1／L2 一併）；缺口 D（`CustomizeOnlyStorage.GetChangeSource` 的客製快取失效訊號）仍待做 |

> F3 為何合流：用戶端進公司流程從未被任何 head 走過（見 §2.C），所以「接上 `ResetDefineCache`」與
> 「端到端驗證客製生效」必須在同一條路徑上完成——沒有走這條路的 head，兩者都無從驗證。

> 每階段驗證：**未設 `CustomizeId` 的部署行為零變化**（回歸防護）＋**跨租戶隔離**（A 租戶客製不影響 B）。

---

## 5. 決策紀錄

| # | 問題 | 結論 |
|---|------|------|
| 1 | customizeId 傳遞方式 | ✅ **依消費端性質二分**：伺服端讀 `SessionInfo.CustomizeId` 顯式傳參，UI 端 `BeeStringLocalizer` 用委派讀 `ClientInfo.Company?.CustomizeId`。不新增 wire 欄位；伺服端永不採信 client 傳回的 customizeId（見 §2.A） |
| 2 | `CustomizePath` 配置來源 | ✅ **維持 host 自建 `PathOptions`**，不新增框架組態機制；框架只補文件與 sample（見 §2.B） |
| 3 | 客製檔誰維護、怎麼產生 | ⏳ **未決**（2026-07-31 明確保留）。不擋 F1–F4；低成本選項見 §3 |
