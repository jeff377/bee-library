# ADR-016：多租戶客製化覆蓋層（雙層唯讀疊加）

## 狀態

已採納（2026-05-31）

## 背景

Bee.NET 的租戶概念原本只到**資料庫層**（[ADR-012](adr-012-session-company-context.md) 的 `SessionInfo.CompanyId` + `EnterCompany`/`LeaveCompany`，row-level 以 `sys_company_rowid` 隔離），**定義檔則全系統共用**——所有 `GetXxxFilePath()` 都從單一 `PathOptions.DefinePath` 根目錄衍生。

多租戶部署下，不同租戶需要不同的客製化：

- **Language**：同一 key 不同租戶顯示不同文字 / enum。
- **FormLayout**：不同租戶不同畫面排版（重排 / 隱藏既有欄位）。
- **Custom BO（ProgramSettings）**：不同租戶綁不同 `FormBusinessObject` 子類。

需要在 base 套裝定義之上疊加一層**租戶專屬的唯讀客製覆蓋**，由一個「客製化代碼」（`CustomizeId`）驅動。

## 核心不變式（凌駕一切實作便利）

**伺服端的任何快取資料——無論套裝（base）或客製（cust）——初始化後執行階段一律唯讀，絕不異動。**

- 套裝快取為全 session 共用的單一實例：異動它會污染**所有**租戶與 session。
- 客製快取為「同一 `CustomizeId` 全 session 共用」的實例：異動它會污染**該租戶**的其他 session。
- 多租戶第一鐵則：**任一客製代碼的資料不得影響其他租戶**。

此不變式是後續所有選型的根因——正因為不能異動快取，才否決「合併成單一物件」。

## 決策

引入 **per-`CustomizeId` 的唯讀客製覆蓋層**，與 base 套裝層**雙層獨立、各自唯讀**，疊加只發生在消費端查找當下。

### 核心要點

1. **雙層唯讀疊加，永不合併物件**

   - Base 層＝現有全部快取，process-wide 單一份、唯讀、零異動。
   - Override 層＝per-`CustomizeId` 獨立快取，backing 換成 `CustomizeOnlyStorage`：**嚴格只讀** `{CustomizePath}/{customizeId}/...`、無對應檔即回 `null`（不 fallback、不混入 base）。以 `CachePrefix=customizeId` 物理隔離。
   - 疊加在消費端、以查找粒度進行，**永不產生合併物件、永不 mutate base**。

2. **三類客製，各自的疊加粒度**

   | 類型 | 疊加粒度 | 查找語意 |
   |------|---------|---------|
   | **Language** | key 級 | cust resource 含該 key → 用 cust 值；否則 base 值（enum 同理） |
   | **ProgramSettings** | progId 級 | cust settings 命中該 progId → 用之；否則 base |
   | **FormLayout** | 整檔擇一 | cust 檔存在 → 回 cust 物件；否則 base 物件 |

   `FormSchema` / `TableSchema` / `SystemSettings` / `DatabaseSettings` / `DbCategorySettings` **永遠走 `DefinePath`，不進客製分支**。

3. **`CustomizeId` 為獨立代碼，非等同 `CompanyId`**

   - 多個 Company 可共用同一套客製（集團共用、標準 / 客製版分離）；Company → `CustomizeId` 多對一。
   - 載體沿用 [ADR-012](adr-012-session-company-context.md) 模式：**`CompanyInfo` 存對照、`SessionInfo` 存當前值**。`CustomizeId` 自 `st_company.customize_id` 欄由 `CompanyRepository` 載入，`EnterCompany` 寫入 `SessionInfo.CustomizeId`、`LeaveCompany` / `Logout` 清空（與 `CompanyId` 同步）。

4. **`CustomizeId` 顯式傳參（非 ambient）**

   - 消費端（`LanguageService` / `ProgramSettingsFormBoTypeResolver` / `LocalDefineAccess`）為 stateless 單例，由持有 `AccessToken` 的呼叫端自 `SessionInfo.CustomizeId` 解析後**顯式傳入**。
   - 新疊加方法以 **default interface method** 加在 `ILanguageService` / `IFormBoTypeResolver` / `IDefineAccess`，預設委派 base、零漣漪到既有實作。

5. **短路即向後相容**

   - 每個疊加點第一步是 `string.IsNullOrEmpty(customizeId)`；為空（單租戶、登入前、未 `EnterCompany`、或 `CustomizePath` 未設）即直接走 base、**完全不進客製層**（不呼叫 reader / provider、不探檔）。
   - `ICustomizeDefineReader` 內部再做一次同樣 guard 作為第二道防線。
   - 結果：未啟用客製時全鏈路與現狀**逐位元一致**。

## 理由

### 為何否決 merge（合併成單一物件）

合併 base + cust 成新物件，會誘發兩種違反核心不變式的路徑：「讀 base→就地改寫」污染全租戶共用快取，或需配置額外的合併快取（又一份要維護的可變狀態）。改採「雙層唯讀、查找時擇一」後，base 快取自始至終零異動。

### 為何 `CustomizeId` 顯式傳參而非 ambient（AsyncLocal）

| 機制 | 最壞失效模式 | 安全性 |
|------|------------|--------|
| **顯式傳參** | 某呼叫端漏傳 → 退化純 base（**客製沒套到**） | ✅ fail-safe |
| Ambient AsyncLocal | request 結束漏 reset / thread 重用 → **沿用上一 request 的 `CustomizeId`** | ❌ fail-dangerous = 跨租戶外溢 |

安全關鍵功能應選 fail-safe：顯式傳參失效僅「客製沒生效」（安全降級）；ambient 失效正是計畫最在意的跨租戶污染。且顯式傳參對齊框架既有「caller 顯式傳 lang」的 stateless 設計、不引入隱式全域狀態。

### 為何排除 `FormSchema` 客製

`FormSchema` 是定義中樞，同時驅動 UI / DB schema / 驗證規則，逐租戶分歧會讓 DB 結構裂開。`FormLayout` 客製（只能重排 / 隱藏既有欄位，欄位集仍由共用 `FormSchema` 鎖定）已能滿足「不同租戶不同畫面」，約束反而強化「`FormSchema` 為中樞」。

### 為何 override 快取用 `CachePrefix=customizeId`

沿用既有 `CacheContainerService` 的 `CachePrefix` 機制即可達成 per-租戶物理隔離，無需新增快取基礎設施；且 base 套裝層的 cache 類別與 `FileDefineStorage` **一行不改**。

## 替代方案（已評估後不採納）

1. **合併 base + cust 成單一物件**
   - 拒絕原因：誘發改寫 base 快取、污染全租戶（見上）。

2. **Ambient AsyncLocal 傳遞 `CustomizeId`**
   - 拒絕原因：fail-dangerous，跨租戶外溢；引入隱式全域狀態，與框架 stateless 設計相悖。

3. **客製化 `FormSchema`**
   - 拒絕原因：定義中樞逐租戶分歧會裂開 DB schema / 驗證規則。永久排除，非延後。

4. **`CustomizeId` 等同 `CompanyId`**
   - 拒絕原因：無法表達「多公司共用一套客製」（集團共用、標準 / 客製版分離）。

5. **override container 共用 base 的 storage 並 fallback**
   - 拒絕原因：override 快取會混入 base 內容，破壞「override 層只放純客製內容」的清晰語意，也讓無檔情境難以乾淨回 null。

## 結果

### 覆蓋層架構

```text
消費端（持 AccessToken）
   │ 自 SessionInfo.CustomizeId 解析 customizeId，顯式傳入
   ↓
LanguageService / ProgramSettingsFormBoTypeResolver / LocalDefineAccess
   │ customizeId 空 → 短路純 base
   │ 非空 ↓
ICustomizeDefineReader.GetCustomizeXxx(customizeId, ...)
   ↓
ICacheContainerProvider.For(customizeId)   → per-customizeId 唯讀 container（CachePrefix=customizeId）
   ↓
CustomizeOnlyStorage（嚴格只讀 {CustomizePath}/{customizeId}/...，無檔→null）
```

### 目錄結構

```text
{DefinePath}/FormSchema/{progId}.FormSchema.xml          ← 全租戶共用、不客製
{DefinePath}/Language/{lang}/{ns}.Language.xml           ← 標準版
{DefinePath}/FormLayout/{layoutId}.FormLayout.xml
{DefinePath}/ProgramSettings.xml
{CustomizePath}/{customizeId}/Language/{lang}/{ns}.Language.xml   ← 客製差異（僅 3 類）
{CustomizePath}/{customizeId}/FormLayout/{layoutId}.FormLayout.xml
{CustomizePath}/{customizeId}/ProgramSettings.xml
```

### 對外 API 變更

| 範圍 | 變更 |
|------|------|
| `PathOptions` | 加 `CustomizePath`；`GetProgramSettings/FormLayout/Language FilePath` 改 `virtual` |
| `CustomizeOnlyPathOptions` / `CustomizeOnlyStorage` | **新增**（`Bee.Definition`）：只服務三類、無檔回 null、含 path traversal 防護 |
| `ICustomizeDefineReader` | **新增**（`Bee.Definition.Storage`）：三個 `GetCustomizeXxx(customizeId, ...)` |
| `ICacheContainerProvider` / `CacheContainerProvider` / `CustomizeDefineReader` | **新增**（`Bee.ObjectCaching`） |
| `ILanguageService` / `IFormBoTypeResolver` / `IDefineAccess` | 加 `customizeId`-aware default interface method |
| `CompanyInfo` / `SessionInfo` | 加 `CustomizeId` 欄位 |
| `RemoteDefineAccess` / `ClientInfo` | 加 `ClearCache()` / `ResetDefineCache()`（切換租戶清 client 快取） |
| `st_company` | 加 `customize_id` 欄 |

## 取捨

### Client 端快取需在切換租戶時 flush

客製疊加在 server 端（依 session `CustomizeId`）完成，client 取得的已是疊加後結果。但 `RemoteDefineAccess` 本地快取以 progId / layoutId / namespace 為鍵，同一連線經 `EnterCompany` 切換公司（`CustomizeId` 變動）時會回前一租戶的疊加結果。對策：`RemoteDefineAccess.ClearCache()` + `ClientInfo.ResetDefineCache()`，切換公司後呼叫。

### Oracle `''=NULL` 與 `customize_id` 的 nullability

`customize_id` 標準版常態為空。Oracle 把 `''` 視為 `NULL`，使「String NOT NULL 且常態為空」在 Oracle fresh CREATE 下無法成立。處理見 [plan-oracle-string-nullability](../plans/plan-oracle-string-nullability.md)：Oracle dialect 對 String 欄一律建 nullable，讀取端 `ValueUtilities.CStr(null)→""` 正規化，上層 C# 永遠看到空字串、不見 null；其餘方言維持 `NOT NULL` + `DEFAULT ''`。

### 「列舉全部 program」場景不給聯集

progId 級查找只解「給定 progId 取其一」，不直接給 base ∪ cust 聯集。若選單建構需要聯集，消費端當下建構暫時 view（不快取、不 mutate 任一層）。BO 型別解析不需聯集，故框架內不實作。

### 客製定義只讀不寫

框架只負責**讀**客製檔；客製檔由外部工具 / 部署流程產生。`SaveXxx` 不走客製寫入路徑。寫入留待後續計畫（需考量寫入路徑、cache 失效、與唯讀不變式的互動）。

## 影響範圍

| 範圍 | 影響 |
|------|------|
| `src/Bee.Definition` | 新增 `CustomizeOnlyPathOptions` / `CustomizeOnlyStorage` / `ICustomizeDefineReader`；`PathOptions` 加 `CustomizePath`；`ILanguageService` / `IDefineAccess` 加多載；`CompanyInfo` / `SessionInfo` 加 `CustomizeId` |
| `src/Bee.ObjectCaching` | 新增 `ICacheContainerProvider` / `CacheContainerProvider` / `CustomizeDefineReader`；`LocalDefineAccess` 加 overlay 多載 |
| `src/Bee.Business` | `ProgramSettingsFormBoTypeResolver` overlay（type cache 改 `(customizeId, progId)` 複合鍵）；`IFormBoTypeResolver` 加多載；`SystemBusinessObject` EnterCompany / LeaveCompany / Logout 設 / 清 `CustomizeId` |
| `src/Bee.Repository` | `CompanyRepository.GetById` 載入 `customize_id` |
| `src/Bee.Hosting` | DI 註冊 provider / reader、三消費端注入 reader |
| `src/Bee.Api.Client` / `src/Bee.UI.Core` | `RemoteDefineAccess.ClearCache()` / `ClientInfo.ResetDefineCache()` |
| 測試 | Override 層、消費端疊加、跨租戶隔離、短路、向後相容、EnterCompany→CustomizeId、client 切換清快取 |

## 相關文件

- [ADR-012：Session 公司情境模型](adr-012-session-company-context.md) — `CustomizeId` 載體沿用其 `CompanyInfo` / `SessionInfo` 模式
- [ADR-009：快取實作](adr-009-cache-implementation.md) — override 層重用其 `CachePrefix` 隔離機制
- [計畫：多租戶客製化](../plans/plan-multitenant-customization.md) — 五階段實作細節
- [計畫：Oracle VARCHAR2 string nullability 修正](../plans/plan-oracle-string-nullability.md) — `customize_id` 在 Oracle 的 nullability 處理
