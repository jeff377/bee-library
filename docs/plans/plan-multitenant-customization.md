# 計畫：多租戶客製化（客製化代碼驅動定義檔 fallback）

**狀態：🚧 進行中（2026-05-29）**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 1 | Override 層：`CustomizeOnlyStorage`（嚴格只讀、無檔回 null）+ per-custCode `ICacheContainer` provider + `ICustomizeDefineReader` | ✅ 已完成（2026-05-29） |
| 2 | 消費端疊加：`LanguageService`（key）、`ProgramSettingsFormBoTypeResolver`（progId）、`LocalDefineAccess.GetFormLayout`（整檔擇一） | ✅ 已完成（2026-05-29） |
| 3 | 身分傳遞：`CompanyInfo.CustomizeId` + `SessionInfo.CustomizeId` + EnterCompany 解析 | ✅ 已完成（2026-05-29） |
| 4 | 接線：DI 註冊 `ICustomizeDefineReader` / provider；`PathOptions.CustomizePath`；向後相容預設 | 📝 待做 |
| 5 | 客戶端 / 測試：`RemoteDefineAccess` 切換清快取、各層 round-trip + 疊加 / 隔離測試 | 📝 待做 |

## 背景

多租戶部署下，不同租戶需要不同的客製化（language resource、FormLayout、custom BO）。目前框架的租戶概念只到**資料庫層**（`CompanyInfo.CompanyDatabaseId` + `EnterCompany`/`LeaveCompany`，row-level 以 `sys_company_rowid` 隔離），**定義檔則全系統共用**——所有 `GetXxxFilePath()` 都從單一 `PathOptions.DefinePath` 根目錄衍生（`src/Bee.Definition/PathOptions.cs`）。

本計畫由一個「客製化代碼」（CustomizeId）驅動，在 base 套裝定義之上疊加一層**租戶專屬的唯讀客製覆蓋**。

## 核心不變式（最高優先，凌駕一切實作便利）

**伺服端的任何快取資料——無論套裝（base）或客製（cust）——初始化後在執行階段一律唯讀，絕不異動**（對齊記憶 [[definition-immutability]]）。

- 套裝快取為全 session 共用的單一實例：異動它會污染**所有**租戶與 session。
- 客製快取為「同一 custCode 全 session 共用」的實例：異動它會污染**該租戶**的其他 session。
- 多租戶第一鐵則：**任一客製代碼的資料不得影響其他租戶**——靠 (a) 兩層皆唯讀、(b) 永不把 base + cust 合併成新物件、(c) override 快取以 `CachePrefix=custCode` 物理隔離，三者共同保證。
- 任何消費端若需要「依當下情境變動定義」（per-session / per-request 改寫），**必須先 `Clone()` 再改**，且禁用 `XmlCodec.Serialize(cached)` 當深拷貝（見記憶）。本計畫的疊加全程只讀不寫，故不需 clone；clone 僅在未來確有變動需求的消費端適用。

> 此不變式同時是設計選型的根因：正因為不能異動快取，才**否決 merge**（合併會產生「讀 base→改寫」的誘惑或需配置額外合併快取），改採「雙層唯讀、查找時擇一」。

## 已確認的設計決策

| 決策 | 選擇 | 理由 |
|------|------|------|
| 客製化代碼定位 | **獨立代碼**，非等同 CompanyId | 多個 Company 可共用同一套客製（集團共用、標準版/客製版分離）。Company → CustomizeId 多對一 |
| Override 模型 | **雙層唯讀疊加**（不合併成單一物件、不異動 base） | base 套裝快取維持單一唯讀；客製為另一份獨立快取（嚴格只讀 `CustomizePath`，無檔回 null）。疊加發生在**消費端查找粒度**（key / progId / 整檔擇一），永不把兩份併成一個物件、永不 mutate base（對齊記憶 [[definition-immutability]]：cache 共享不可 mutate） |
| 代碼載體 | **CompanyInfo 存對照，SessionInfo 存當前值** | 對照是公司推導屬性（與 `CompanyDatabaseId` 同生命週期）；執行期 decorator 一律從 `ISessionInfoService` 讀當前值 |
| 客製範圍 | Language / FormLayout / Custom BO(ProgramSettings) | **FormSchema 排除**——它是定義中樞，同時驅動 UI / DB schema / 驗證規則，逐租戶分歧會讓 DB 結構裂開。FormLayout 只能重排/隱藏既有欄位（欄位集仍由共用 FormSchema 鎖定），約束反而強化「FormSchema 為中樞」 |
| 目錄結構 | **獨立根目錄** `CustomizePath` | 標準版保持乾淨；每租戶可獨立備份/部署/權限 |

### 覆蓋機制（三類各不同，皆唯讀疊加）

| 類型 | 疊加粒度 | 客製檔內容 | 查找語意（不產生合併物件） |
|------|---------|-----------|--------------------------|
| **Language** | key 級 | 只放被覆寫/新增的 key | 查 cust resource：`cust.Items.Contains(key)` → 回 cust 值；否則回 base resource 的值。Enum 同理 |
| **ProgramSettings** | progId 級 | 只放被覆寫/新增的 progId item | 查 cust settings：`cust.FindItem(progId)` 命中 → 用之；否則 base.FindItem(progId) |
| **FormLayout** | 整檔擇一 | 整份 layout | cust 檔存在 → 回 cust 物件；否則回 base 物件（兩者皆為各自快取的唯讀實例） |

關鍵：**沒有任何一類把 base 與 cust 合併成新物件**。Language / ProgramSettings 在消費端逐 key / progId 擇一；FormLayout 在存取端整檔擇一。base 套裝快取自始至終唯讀、零異動。

> **為何 FormLayout 不做 key 級疊加**：UI 版面是 section / grid / 欄位位置的整體結構，欄位間有相對排列與容器歸屬，無法以「key 疊加」乾淨表達局部差異。整檔取代語意清楚：租戶要客製某 layout 就提供完整一份。

## 目錄結構

```
{DefinePath}/FormSchema/{progId}.FormSchema.xml          ← 定義中樞，全租戶共用、不客製
{DefinePath}/TableSchema/...                             ← 同上（DB schema，不客製）
{DefinePath}/Language/{lang}/{ns}.Language.xml           ← 標準版（pristine）
{DefinePath}/FormLayout/{layoutId}.FormLayout.xml
{DefinePath}/ProgramSettings.xml
...
{CustomizePath}/{custCode}/Language/{lang}/{ns}.Language.xml   ← 客製差異（僅 3 類）
{CustomizePath}/{custCode}/FormLayout/{layoutId}.FormLayout.xml
{CustomizePath}/{custCode}/ProgramSettings.xml
```

解析規則（皆為唯讀疊加，base 不被異動）：

- **FormLayout（整檔擇一）**：cust 檔 `{CustomizePath}/{custCode}/FormLayout/{layoutId}.FormLayout.xml` 存在則回 cust 物件；否則回 base 物件。
- **Language / ProgramSettings（查找疊加）**：base 與 cust **各自獨立快取為唯讀實例**；消費端先查 cust（嚴格只讀 `CustomizePath`，無檔＝null）、命中就用，否則查 base。**不讀成單一合併物件**。

custCode 為空（登入前、未進公司、或該租戶無客製）→ **直接略過整個客製層存取**（不查 reader / provider、不探檔），三類都走純 base。

FormSchema / TableSchema / SystemSettings / DatabaseSettings / DbCategorySettings **永遠走 `DefinePath`，不進客製分支**。

## 架構設計

兩層獨立、各自唯讀的快取，疊加只發生在查找當下：

- **Base 套裝層**＝現有的全部快取，process-wide 單一份、唯讀、零異動（就是今天的 `ICacheContainer` 行為）。FormSchema / TableSchema / 各 Settings / 以及 Language·ProgramSettings·FormLayout 的「標準版」都在這層。
- **Customize-override 層**＝per-custCode 的獨立快取，**嚴格只讀** `{CustomizePath}/{custCode}/...`、無對應檔即回 `null`（不 fallback、不混入 base）。只服務 Language / ProgramSettings / FormLayout 三類。
- **疊加**在消費端、以查找粒度進行，**永不產生合併物件、永不 mutate base**。

custCode 的傳遞機制：**Option A — 顯式傳參（已定案，2026-05-29）**。框架無 ambient 當前 session 機制，且 overlay 服務（`LanguageService` / `ProgramSettingsFormBoTypeResolver` / `LocalDefineAccess`）為 stateless 單例。改採**持有 `AccessToken` 的呼叫端**（BO 層等）自 `ISessionInfoService.Get(AccessToken).CustomizeId` 解析 custCode 後，透過 additive overload 顯式傳入。否決 ambient AsyncLocal：其最壞失效模式是「沿用上一 request 的 custCode」＝跨租戶外溢（本計畫第一鐵則禁止）；顯式傳參的最壞失效僅「客製沒套到」＝ fail-safe 降級。新 overload 以 default interface method 加在 `ILanguageService` / `IFormBoTypeResolver` / `IDefineAccess`，預設委派 base、零漣漪到既有實作。base 呼叫端逐一改傳真實 custCode 留待後續階段。

### 短路規則（無客製代碼直接略過客製層）

**每個疊加點的第一步都是 `string.IsNullOrEmpty(custCode)` 檢查；為空即直接回 base，完全不進客製層**——不呼叫 `ICustomizeDefineReader`、不取 provider、不探任何客製檔。意義有二：

- **正確性**：無代碼的請求（單租戶、登入前、未 EnterCompany）與現狀逐位元一致。
- **效能**：常見路徑零客製開銷，不為「可能沒有的客製檔」付出 provider 查找 / 檔案 probe / 負快取成本。

`ICustomizeDefineReader` 內部亦再做一次同樣的 guard（custCode 空 / `CustomizePath` 未設 → 回 null），作為第二道防線；但正常情況短路在消費端就已發生，reader 根本不會被呼叫。

### Override 層的載體

per-custCode `ICacheContainer`（重用既有 `CachePrefix`，見 `CacheContainerService` ctor）做租戶隔離，但其 backing storage 換成 **`CustomizeOnlyStorage`**：只讀 `{CustomizePath}/{custCode}/...`，檔不存在回 null（**不** fallback 到 base）。如此 override 快取裡放的就是「純客製內容」。對外以 `ICustomizeDefineReader` 暴露：

```csharp
LanguageResource? GetCustomizeLanguage(string custCode, string lang, string ns);   // 無檔→null
ProgramSettings?  GetCustomizeProgramSettings(string custCode);                     // 無檔→null
FormLayout?       GetCustomizeFormLayout(string custCode, string layoutId);         // 無檔→null
```

介面置於 `Bee.Definition`（與 `IDefineAccess` 同層），實作於 `Bee.ObjectCaching`（持 `ICacheContainerProvider`）。`IDefineAccess` **不動**——base 讀取維持原樣。

### 三類的疊加點

| 類型 | 疊加發生處 | 邏輯 |
|------|-----------|------|
| **Language** | `LanguageService.TryGetLangText` / `LookupEnum`（`Bee.Definition`） | 先 `reader.GetCustomizeLanguage(custCode, lang, ns)`，`cust?.Items.Contains(key)` → cust 值；否則沿用現有 `_defineAccess.GetLanguage` 的 base 值 |
| **ProgramSettings** | `ProgramSettingsFormBoTypeResolver.Resolve`（`Bee.Business`） | 先 `reader.GetCustomizeProgramSettings(custCode)?.FindItem(progId)`，命中→用之；否則 base `FindItem`。type cache 鍵改為 `(custCode, progId)` |
| **FormLayout** | `LocalDefineAccess.GetFormLayout`（整檔擇一） | `reader.GetCustomizeFormLayout(custCode, layoutId)` 非 null → 回之；否則回 base `GetFormLayout`。整檔擇一不違反「不合併」 |

> `LanguageService` / `ProgramSettingsFormBoTypeResolver` 改為注入 `ICustomizeDefineReader?` + `ISessionInfoService`（**可選**，null＝無客製能力，退化為純 base，向後相容）。

### 既有 cache 類別零修改

base 套裝層完全沿用今天的 cache 類別與 `FileDefineStorage`，**一行不改**。新增的只有 override 層（`CustomizeOnlyStorage` + per-custCode container + `ICustomizeDefineReader`）。比起原 merge 方案需動 `LanguageResourceCache` / `ProgramSettingsCache` 的 loader，本設計反而 cache 改動為零。

### 已知取捨

- **「列舉全部 program」場景**（如選單建構需要 base ∪ cust 的聯集）：本設計的 progId 級查找只解「給定 progId 取其一」，不直接給聯集。若有此需求，在消費端**當下建構暫時 view**（base 清單覆寫 cust 清單，產生新的 transient list，不快取、不 mutate 任一層）。BO 型別解析不需聯集，故 Phase 內不實作；列為消費端按需處理。

## 階段拆解

### 階段 1：Override 層（嚴格只讀 + per-custCode 隔離 + reader 介面）

**檔案**：`src/Bee.Definition/PathOptions.cs`（改）、新增 `CustomizeOnlyPathOptions`、`CustomizeOnlyStorage`、`ICustomizeDefineReader`（`Bee.Definition`）、`CustomizeDefineReader` + `ICacheContainerProvider` / `CacheContainerProvider`（`Bee.ObjectCaching`）

- `PathOptions`：新增 `CustomizePath { get; init; }` 根目錄屬性（標準版時為空）。
- `CustomizeOnlyPathOptions : PathOptions`：
  - ctor 接 `(string customizePath, string custCode)`。
  - **僅** override `GetLanguageFilePath` / `GetFormLayoutFilePath` / `GetProgramSettingsFilePath`，一律回 `{CustomizePath}/{custCode}/...`（**不** fallback 到 base）。其餘路徑方法不應被呼叫（override 層只服務這三類）。
  - 路徑安全（`scanning.md`）：custCode 驗證不含 `..` / 路徑分隔符，`Path.GetFullPath` 正規化後確認落在 `{CustomizePath}/{custCode}` 內。
- `CustomizeOnlyStorage : IDefineStorage`：包 `CustomizeOnlyPathOptions`；`GetLanguage` / `GetFormLayout` / `GetProgramSettings` **檔不存在回 null**（不丟例外、不 fallback）。其餘方法 `throw NotSupportedException`（override 層不該被問）。
- `ICacheContainerProvider.For(custCode)`：lazy 建 per-custCode override container（`CachePrefix=custCode`、backing = `CustomizeOnlyStorage`），`ConcurrentDictionary` + `GetOrAdd` thread-safe。`For("")` 不適用於 override 層（空 custCode 直接不查 override）。
- `ICustomizeDefineReader` / `CustomizeDefineReader`：對外三個 `GetCustomizeXxx(custCode, ...)`，內部走 provider 的 override container 取值（命中即唯讀實例，無檔回 null）。

> **已知邊界**：客製檔 runtime 新增時，override container 對該 key 已負快取 null（change-monitor 監看的是客製檔路徑，新增檔案會觸發失效→下次取得新值）。base 層不受影響。部署若有疑慮可主動 flush 該 custCode container。

### 階段 2：消費端疊加

**檔案**：`src/Bee.Definition/Language/LanguageService.cs`、`src/Bee.Business/ProgramSettingsFormBoTypeResolver.cs`、`src/Bee.ObjectCaching/LocalDefineAccess.cs`

每個疊加點都以 **`custCode` 空 → 直接走 base、不進客製層** 為第一步（見「短路規則」）。

- `LanguageService`：ctor 增注入 `ICustomizeDefineReader?` + `ISessionInfoService?`（可選，null＝純 base、向後相容）。`TryGetLangText` / `LookupEnum`：取 custCode，**空則直接走現有 base 查找**；非空才查 `reader.GetCustomizeLanguage(custCode, lang, ns)`，cust 命中（`Items.Contains(subKey)` / `GetEnum`）→ 回 cust，否則 base。
- `ProgramSettingsFormBoTypeResolver`：ctor 增注入 `ICustomizeDefineReader?` + `ISessionInfoService?`。`Resolve`：custCode **空則直接 base `FindItem`**；非空才查 `reader.GetCustomizeProgramSettings(custCode)?.FindItem(progId)`，命中→用之，否則 base。type cache 鍵由 `progId` 改 `(custCode, progId)` 複合鍵（custCode 空為其中一個正常鍵值）；reset 判斷對 base + cust 兩個 instance ref。
- `LocalDefineAccess.GetFormLayout`：custCode **空則直接回原 base `_cache.FormLayout.Get(layoutId)`**；非空才查 `reader.GetCustomizeFormLayout(custCode, layoutId)`，非 null → 回之，否則 base。整檔擇一，不合併。

### 階段 3：身分傳遞（CustomizeId 自 `st_company` 表載入）

**檔案**：`CompanyInfo.cs`、`SessionInfo.cs`、`CompanyRepository.cs`、`st_company.TableSchema.xml`、`EnterCompany*`

CustomizeId 來源已定案：**`st_company` 表新增 `customize_id` 欄位**，由 `CompanyRepository.GetById` 隨 `CompanyInfo` 一起載入（與 `company_database_id` 同模式）。

- **`st_company.TableSchema.xml`**（`tests/Define/TableSchema/common/`，FormSchema 驅動建表的 schema 源）：新增 `customize_id` 欄位（字串、nullable / 預設空＝標準版）。
- **`CompanyRepository.GetById`**（`src/Bee.Repository/System/CompanyRepository.cs`）：SELECT 加 `customize_id` 欄、映射 `CustomizeId = ValueUtilities.CStr(row["customize_id"])`。沿用既有 `{0}` 參數化，null/缺值→空字串。
- **`CompanyInfo`**：新增 `[Key(3)] CustomizeId { get; set; } = string.Empty;`（與 `CompanyDatabaseId` 並列；MessagePack key 接續）。
- **`SessionInfo`**：新增 `CustomizeId { get; set; }`（執行期當前值，預設空）。
- **`EnterCompany`**（`src/Bee.Business/System/EnterCompanyArgs|Result.cs` + executor）：解析 `CompanyInfo`（→`ICompanyInfoService.Get`→`CompanyRepository`）後，將 `CompanyInfo.CustomizeId` 寫入 `SessionInfo.CustomizeId`，並持久化 session（沿現有 session 寫回路徑）。
- **`LeaveCompany` / `Logout`**：清空 `SessionInfo.CustomizeId`（與 `CompanyId` 同步清）。

### 階段 4：接線（DI + 向後相容）

**檔案**：`src/Bee.Hosting/BeeFrameworkServiceCollectionExtensions.cs`

- 註冊 `ICacheContainerProvider`（Singleton）、`ICustomizeDefineReader`（Singleton）。
- `LanguageService` / `ProgramSettingsFormBoTypeResolver` / `LocalDefineAccess` 的 ctor 注入新增依賴（`ICustomizeDefineReader` / `ISessionInfoService`）。
- `PathOptions` 增 `CustomizePath`（host 啟動設定）；**未設＝關閉客製**：`CustomizeDefineReader` 在 `CustomizePath` 空時對所有 `GetCustomizeXxx` 直接回 null，全鏈路退化為純 base、與現狀逐位元一致。
- 向後相容：`ICustomizeDefineReader` 在消費端為**可選依賴**，未註冊時消費端走純 base 路徑。

### 階段 5：客戶端與測試

- `RemoteDefineAccess`（`src/Bee.Api.Client`）：客製疊加在 server 端（依 session custCode）完成，client 取得的已是疊加後結果。client 端本地字典快取以 progId / layoutId 為鍵——單一連線單一 custCode 通常安全，但 **EnterCompany 切換公司導致 custCode 變動時須 flush client 快取**。加對應清快取點。
- 測試（對稱於 `tests/<Module>.UnitTests`）：
  - `CustomizeOnlyStorage`：客製檔存在→回該檔內容、不存在→**回 null（不 fallback base）**；非三類方法 → `NotSupportedException`；path traversal 防護。
  - `ICustomizeDefineReader` + provider：同 custCode 回同一 container、不同 custCode 隔離；`CustomizePath` 空 → 全回 null。
  - **疊加正確性**：
    - Language：cust 有 key → 回 cust 值；cust 無 key → 回 base 值；cust resource 不存在 → 全回 base。
    - ProgramSettings：cust 有 progId → 解析到客製 BO；cust 無 → base BO；type cache 以 `(custCode, progId)` 隔離。
    - FormLayout：cust 檔存在 → 回 cust；否則 base。
  - **快取不被異動 + 跨租戶非干擾（核心守門）**：
    - 租戶 A 走完整條疊加查找後，base 層（`For("")`）的 Language / ProgramSettings / FormLayout 實例內容不變。
    - 租戶 A 的客製查找不改變租戶 B 的查找結果（A、B 各自客製不同 key/progId/layout，交叉驗證互不影響）。
    - 連續多次查找回傳的快取實例 reference 穩定（證明沒有每次重建／就地改寫）。
  - EnterCompany → SessionInfo.CustomizeId 正確設定；LeaveCompany 清空。
  - **短路**：custCode 空時三類消費端完全不觸及客製層（以 spy / mock `ICustomizeDefineReader` 驗證其 `GetCustomizeXxx` **零呼叫**），結果等同純 base。
  - 向後相容：無 `ICustomizeDefineReader` 注入時，三類消費端行為與現狀一致。

## 已定案決策（原開放問題）

1. **客製定義的 Save：本期不做**。客製檔由外部工具 / 部署流程產生，框架只負責**讀**。`SaveLanguage` / `SaveFormLayout` / `SaveProgramSettings` 在 custCode 非空時不走客製寫入路徑（維持只寫 base 或不支援，見「不在本計畫範圍」）。
2. **`CompanyInfo.CustomizeId` 來源：`st_company` 表的 `customize_id` 欄位**，由 `CompanyRepository.GetById` 載入（已併入階段 3）。

## 不在本計畫範圍

- **客製定義的 Save（本期不做）**——只讀不寫；客製檔由外部工具 / 部署流程產生。框架端寫入留待後續計畫（屆時需考量寫入路徑、cache 失效、與唯讀不變式的互動）。
- **FormSchema 客製化（永久排除，非本期延後）**——定義中樞，逐租戶分歧會讓 DB schema / 驗證規則裂開。FormLayout 客製已能滿足「不同租戶不同畫面」，且受共用 FormSchema 的欄位集鎖定。
- TableSchema / SystemSettings / DatabaseSettings / DbCategorySettings 客製化（同屬 schema/系統層，永久排除）。
- 客製化檔案的「編輯/上傳/部署」管理介面（本計畫只處理讀取解析鏈路）。
- 客製檔 runtime 新增的 hot-reload（見階段 1 已知邊界）。

## 關鍵風險

1. **異動任何共用快取（base 或 cust）**（最高，見上方「核心不變式」）——本設計以「雙層唯讀疊加、永不合併物件、只讀不寫」從根本避免；階段 5 的「快取不被異動」測試守門。任何實作若試圖把 cust 疊回 base、或就地改寫任一層快取物件，即為違規。
2. **客製資料跨租戶外溢**——override 層以 `CachePrefix=custCode` 物理隔離；ProgramSettings type cache 改 `(custCode, progId)` 複合鍵；階段 5 跨租戶非干擾測試守門。
3. **向後相容**——`CustomizePath` 未設 / `ICustomizeDefineReader` 未注入時，全鏈路退化為純 base，與現狀逐位元一致。
4. **path traversal**——custCode 來自身分鏈雖相對可信，仍須在 `CustomizeOnlyPathOptions` 做正規化驗證（`scanning.md`）。
