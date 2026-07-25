# Plan：客製化共同前置（討論稿）

> 狀態：📝 擬定中（討論用，尚未動工）· 2026-07-25
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

---

## 1. 已完成的基礎設施（可直接沿用）

| 項目 | 位置 | 狀態 |
|------|------|------|
| `CustomizeId` 寫入鏈 | `st_company.customize_id` → `CompanyRepository`(`:46,80`) → `CompanyInfo`(`:59`) → `EnterCompany`(`SystemBusinessObject.Session.cs:120`) → `SessionInfo.CustomizeId`(`:63`) | ✅ 完整 |
| `CustomizeId` 清除 | `ClearCompanyContext`(`Session.cs:207`)，由 `LeaveCompany`(`:161`) / `Logout`(`:188`) 共用 | ✅ |
| 客製路徑解析 | `CustomizeOnlyPathOptions`（含 path traversal 防護 `:41-56`） | ✅ |
| 客製儲存讀取 | `CustomizeOnlyStorage`（3 getter 有效，其餘 `NotSupportedException`） | ✅ |
| 客製快取容器 | `CacheContainerProvider`（per-`CustomizeId` 隔離 `:38-43`） | ✅ |
| 客製讀取器 | `CustomizeDefineReader` + `ICustomizeDefineReader`（空值短路 `:69-70`） | ✅ |
| DI 註冊 | `BeeFrameworkServiceCollectionExtensions:88-102,175-178,205-209` | ✅ reader 已注入三個消費端 |
| 測試 | 10 檔 / 約 66 測試 | ✅ 元件級完整 |

**客製檔路徑**（`CustomizeOnlyPathOptions:60-70`）：
```
{CustomizePath}/{customizeId}/ProgramSettings.xml
{CustomizePath}/{customizeId}/FormLayout/{layoutId}.FormLayout.xml
{CustomizePath}/{customizeId}/Language/{lang}/{ns}.Language.xml
```

**Fallback 責任分工**：override 層本身**不 fallback**（無檔回 `null`），fallback 由各消費端負責。

---

## 2. 橫向缺口

### 🔴 A. 消費端未接線（最嚴重，三類全被擋）

DI 接線本身完整，reader 已注入；問題純粹是**呼叫端沒傳 `customizeId`**：

| 消費端 | 位置 | 現況呼叫 | 應為 | 影響類別 |
|--------|------|---------|------|---------|
| BO 型別解析 | `BusinessObjectFactory.cs:70` | `Resolve(progId)` | `Resolve(session.CustomizeId, progId)` | 業務邏輯 |
| BO 語系 | `BusinessObject.cs:95-105` | `GetLangText(lang, ...)` | 加 customizeId | 語系 |
| 一般在地化 | `BeeStringLocalizer.cs:60,66` | base overload | 加 customizeId 管道 | 語系 |
| Schema 在地化 | `FormSchemaLocalizer.cs:73,89,101,136` | base overload | 加 customizeId 管道 | 語系 |
| FormLayout | `CacheDefineAccess.cs:112`（`GetDefine`） | `GetFormLayout(keys[0])` | 簽章無 customizeId，需另議 | Layout |

> `BusinessObjectFactory` 連 `ISessionInfoService` 都已注入(`:25`)，只是沒用它讀 `CustomizeId`。
> 多數接線是「補傳一個參數」，但 Layout 那條有結構問題（見 Layout plan）。

**決策 A1：customizeId 的傳遞方式**
- **選項 A1-a（建議）**：**維持 ADR-016 的顯式傳參**。消費端各自從 `ISessionInfoService` 取 `CustomizeId` 後顯式傳入。
  優點：符合現有設計、fail-safe 明確、無隱式狀態。缺點：每個消費端都要改、日後新增消費端容易再漏。
- **選項 A1-b**：由 DI 提供 `ICustomizeContext`（scoped），消費端注入後自動取得。
  優點：新增消費端不易漏。缺點：引入隱式上下文，與框架現有「顯式傳參」風格不一致，且 scoped 生命週期在非 HTTP 情境（背景工作）需另外處理。

> 待討論：漏傳的代價是「靜默退回 base」——不會壞、但客製悄悄失效，難察覺。
> 若選 A1-a，建議搭配 §2.F 的端到端測試作為防線。

### 🔴 B. `CustomizePath` 無配置管道（客製層目前在所有部署中都是關閉的）

- `PathOptions.CustomizePath` 是 `init`-only（`PathOptions.cs:24`）
- **沒有** `IConfiguration` binding、**沒有** appsettings key、**沒有** `SystemSettings.xml` 欄位
- 現有 host 都只設 `DefinePath`：`NorthwindBackend.cs:47`、`DemoBackend.cs:43`

**決策 B1：配置來源**
- **選項 B1-a（建議）**：走 `IConfiguration` / appsettings（與 `DefinePath` 同一機制），host 啟動時綁定。
- **選項 B1-b**：放進 `SystemSettings.xml`（定義檔驅動，與框架其他設定一致）。
- 兩者可並存（appsettings 優先）。

> 待討論：客製檔的**部署位置**實務上長怎樣？與 `DefinePath` 同一層、還是獨立掛載（如共用磁碟 / 容器 volume）？
> 這會影響預設值該怎麼給。

### 🔴 C. `ClientInfo.ResetDefineCache()` 無任何呼叫端

`ClientInfo.cs:147-150` 只被兩個測試呼叫。ADR「取捨」段指定「切換公司後呼叫」，
`ClientInfo.cs:192` 註解也寫「calls this after `SystemApiConnector.EnterCompanyAsync`」——**但實際流程沒呼叫**。

**影響**：client 端跨租戶快取污染防護未生效。切換公司後仍可能顯示前一租戶的客製定義。

> **建議**：接上 `EnterCompany` / `LeaveCompany` 流程。這是安全性/正確性問題，不是最佳化。

### 🟡 D. 客製快取沒有失效訊號

`CustomizeOnlyStorage` **未實作 `GetChangeSource`**，落到 `IDefineStorage` 的 default
（`IDefineStorage.cs:108-123`，「The default reports no signal」）。
因此 `FormLayoutCache.GetPolicy:36-38`、`LanguageResourceCache:40-42`、`ProgramSettingsCache:34-36`
在客製 container 裡拿到空 `FilePaths` + 空 `NotifyKey`。

**後果**：客製檔改動**不觸發失效**，只能等 20 分鐘 sliding expiration。
base 層有 file-watch（`FileDefineStorage.cs:232`）、DB 層有 cache-notify（`DbDefineStorage.cs:357`），
**只有客製層兩者皆無**。

> **建議**：實作 `CustomizeOnlyStorage.GetChangeSource`，比照 base 的 file-watch。

### 🟡 E. DB 儲存 + 客製的組合是壞的

`DbDefineStorage.cs:36,201-213` 已實作完整的 DB 版 `ICustomizeDefineReader`（base 用 sentinel `customize_id = "*"`），
但 `BeeFrameworkServiceCollectionExtensions.cs:90-93` **無條件註冊檔案版** `CustomizeDefineReader`。

> **建議**：條件註冊——若 `IDefineStorage` 本身實作 `ICustomizeDefineReader` 就用它。

### 🟡 F. 無端到端測試（缺口 A 從未被測出的原因）

66 個測試**全部是元件級**，都是測試自己手動傳 `"acme"` 字串。
**零個測試驗證「從 API 呼叫進來 → 自動套用該 session 的客製」**。

> **建議**：補整合測試——建立帶 `CustomizeId` 的 session，走真實 API 路徑，驗證三類客製都生效。
> 這同時是缺口 A 的長期防線。

### 🟢 其他觀察

- **無任何 TODO/FIXME 標記**（`grep "// TODO" src/` 零結果）——缺口是「靜默未接線」而非「標記待辦」。
- `CustomizeDefineReader.GetCustomizeProgramSettings:44-54` 因 `ProgramSettingsCache` 缺檔會 throw，
  改用 `File.Exists` 先探檔 → **每次呼叫都做一次同步檔案 I/O**（非 cache-only 路徑）。可列為最佳化項。
- ADR-016 引用的 `docs/plans/plan-multitenant-customization.md` **已不存在**（封存後刪除），連結是死的。

---

## 3. ADR-016 明確排除（非缺口，已裁決）

- **`FormSchema` / `TableSchema` 不可客製** — 兩者同時驅動 UI / DB schema / 驗證規則，
  逐租戶分歧會讓 DB 結構裂開。ADR 稱「永久排除，非延後」。
- **客製定義只讀不寫** — `CustomizeOnlyStorage` 所有 `SaveXxx` 全 throw，客製檔由外部工具/部署流程產生。
- **不提供 base ∪ cust 聯集列舉**。

> 待討論（跨三類）：**客製檔實務上誰維護、怎麼產生？**
> ADR 定「只讀不寫」，但若你們需要在系統內編輯客製，就得重新檢視這條，並回答
> 「編輯後如何不破壞 base 升級路徑」。

---

## 4. 建議階段

| 階段 | 範圍 | 說明 |
|------|------|------|
| F0 | 決策定案（A1 傳遞方式、B1 配置來源） | 三類 plan 的共同前提 |
| F1 | **缺口 B**：`CustomizePath` 配置管道 | 先讓客製層「能被開啟」 |
| F2 | **缺口 A**：消費端接線（Layout 除外，見 Layout plan） | 讓客製真的生效 |
| F3 | **缺口 C**：`ClientInfo.ResetDefineCache()` 接上 EnterCompany/LeaveCompany | 跨租戶污染防護 |
| F4 | **缺口 F**：端到端整合測試 | 缺口 A 的長期防線 |
| F5 | **缺口 D、E**：客製快取失效訊號、DB 版 reader 條件註冊 | 完備性 |

> 每階段驗證：**未設 `CustomizeId` 的部署行為零變化**（回歸防護）＋**跨租戶隔離**（A 租戶客製不影響 B）。

---

## 5. 給 review 的提問

1. **customizeId 傳遞**走顯式傳參（A1-a，維持 ADR 設計）還是 scoped context（A1-b）？
2. **`CustomizePath` 實務部署位置**在哪？與 `DefinePath` 同層或獨立掛載？影響配置預設值。
3. **客製檔誰維護、怎麼產生**？是否需要框架提供編輯/寫入能力（目前 `SaveXxx` 全 throw）？
