# Plan：業務邏輯客製化（討論稿）

> 狀態：📝 擬定中（討論用，尚未動工）· 2026-07-25
> 範圍：**Business Object 的租戶客製**——單據行為、驗證規則、流程差異。
> 前置：[客製化共同前置](plan-customization-foundation.md)（缺口 A、B 未補則本案無法生效）
> 相關：[Layout 客製](plan-customization-layout.md)｜[語系客製](plan-customization-language.md)｜[ADR-016](../adr/adr-016-multitenant-customization-overlay.md)

---

## 0. 一句話結論

機制方向正確（per-progId 換 BO 類別）、疊加邏輯已實作，但**未接線**；
且擴充模型只有「**整個換掉 BO 類別**」一種，無 hook / 無組合能力，
失敗還會**靜默降級**——客製 BO 打錯字不會報錯，只是悄悄失效。

---

## 1. 現況

### 1.1 已實作的 overlay（但呼叫端永遠傳空字串）

`ProgramSettingsFormBoTypeResolver.cs`：
- 複合鍵 cache `customizeId + "\0" + progId`（`:94-96`）
- `ResolveCore`（`:134-135`）：`FindItem(cust) ?? FindItem(base)` — **per-progId 擇一**

**未接線**：`BusinessObjectFactory.CreateFormBusinessObject`（`BusinessObjectFactory.cs:70`）
呼叫**單參數** `_resolver.Resolve(progId)` → 內部委派 `Resolve("", progId)`（`:64`）
→ **customizeId 永遠是空字串**。

> `BusinessObjectFactory` 已注入 `ISessionInfoService`（`:25`），只是沒用它讀 `CustomizeId`。
> 這條接線是三類裡最單純的——補一個參數即可。

### 1.2 客製方式：換掉 BO 類別字串

客製 `ProgramSettings.xml` 的 `ProgramItem.BusinessObject`（`ProgramItem.cs:69`）指向不同型別。

**組件載入**（`AssemblyLoader.cs:132-137` → `LoadAssembly:57-88`）：
1. 掃 `AppDomain.CurrentDomain.GetAssemblies()`
2. `Assembly.Load(new AssemblyName(simpleName))`
3. fallback `AssemblyLoadContext.Default.LoadFromAssemblyPath(...)` → **host 的 bin 目錄**

**無 plugin 目錄、無 per-tenant 組件隔離、無版本並存。**
註解（`:64-67`）明確說明**刻意**使用 default context，以免 static 狀態分裂。

### 1.3 靜默降級 ★風險點

`ProgramSettingsFormBoTypeResolver.cs:148-165`：**五種失敗情況全部吞掉**，
一律降級為 `typeof(FormBusinessObject)`。

> 客製 BO 型別名打錯、組件沒部署、型別不相容——**都不會報錯**，
> 只會靜默失去客製。多租戶部署下極難診斷。

### 1.4 現有擴充點（編譯期繼承）

`FormBusinessObject` 可 override：
`DoBeforeSave`(`:263`)、`DoSave`(`:274`)、`DoAfterSave`(`:286`)、
`DoBeforeDelete`(`:332`)、`DoDelete`(`:343`)、`DoAfterDelete`(`:353`)、`GetLookupFilter`(`:135`)

**這些是編譯期繼承，不是 per-tenant 註冊**——多個客製需求疊加時無法組合，只能寫成一個子類。

### 1.5 規則引擎未接上客製層

`architecture-overview:338` 描述的 LowCode「Events, conditions, and rules to extend BO」
（ADR-028 `IFormRuleProcessor`）**沒有接上客製層**——
規則存在 **FormSchema** 裡，而 FormSchema **明確不可客製**（ADR-016 永久排除）。

### 1.6 測試

`tests/Bee.Business.UnitTests/ProgramSettingsFormBoTypeResolverCustomizeTests.cs`（6 測試）：
per-progId 擇一、cache 隔離(`:75`)、base 缺檔(`:121`)。**皆為手動傳 customizeId 的元件級測試。**

---

## 2. 設計決策

### 決策 B1：客製 BO 的擴充模型 ★核心

- **選項 B1-a（建議，第一階段）**：**沿用「整個換掉 BO 類別」**，先接線讓它生效。
  客製 BO 繼承 base BO 後 override（§1.4 的擴充點）。
  優點：零新機制、立即可用、與現有設計一致。
  缺點：多個客製需求疊加時只能寫成一個「大雜燴」子類；不同來源的客製無法組合。

- **選項 B1-b**：引入 **hook / 擴充點註冊**——per-tenant 註冊多個 handler，依序執行。
  優點：客製可組合、彼此不覆蓋、來源可分離。
  缺點：需設計 hook 生命週期、執行順序、失敗語意（一個 handler 失敗要不要中止？）；是新的架構決策，需 ADR。

- **選項 B1-c**：接上 ADR-028 規則引擎作為 LowCode 客製途徑。
  **障礙**：規則存在 FormSchema，而 FormSchema 不可客製 → 需先決定「客製規則存哪」
  （獨立的客製規則檔？ProgramSettings 擴充？），否則此路不通。

> **選擇取決於**：實務上是否常有「多個客製需求疊加在同一張單據」（見 §4 提問 1）。
> 若是，B1-a 會很快撞牆；若客製多由單一單位交付，B1-a 長期可行。

### 決策 B2：失敗可觀測性

> **建議（不論 B1 選什麼都該做）**：客製 BO 解析失敗應**可觀測**——至少寫 log / 稽核事件，
> 而非五種情況全部靜默吞掉。可保留「降級不中斷服務」的行為，但必須留下訊號。

> 待討論：要「降級 + 記錄」還是「啟動時驗證 + 快速失敗」？
> 後者（部署時就驗證所有客製 BO 型別可解析）在多租戶下更安全，但需要列舉所有 customizeId。

### 決策 B3：組件部署模型

目前所有租戶的客製 BO DLL 共用 host bin，無隔離、無版本並存。

- **選項 B3-a（建議）**：**維持共用 bin**，不在本次範圍。
  多數 ERP 部署是單一供應商交付，共用 bin 可接受。
- **選項 B3-b**：引入 `AssemblyLoadContext` per-tenant 隔離。
  **衝突**：與現有刻意使用 default context 的設計相斥（`AssemblyLoader.cs:64-67` 註解），
  會讓 static 狀態分裂。需重新檢視框架的 static 使用。

> 待確認：客製 BO 由誰開發？若由不同單位/夥伴各自交付、需版本並存，B3-a 會不夠。

---

## 3. 建議階段

| 階段 | 範圍 | 前置 |
|------|------|------|
| B0 | 決策定案（B1 擴充模型、B2 可觀測性、B3 部署模型） | — |
| B1 | 接線：`BusinessObjectFactory.cs:70` 傳入 `session.CustomizeId` | foundation F1、F2 |
| B2 | 失敗可觀測性：解析失敗寫 log / 稽核 | — |
| B3 | 端到端測試：帶 CustomizeId 的 session → API → 解析到客製 BO | foundation F4 |
| B4（選配） | hook / 擴充點註冊機制（若選 B1-b） | 需新 ADR |
| B5（選配） | 客製規則存放位置（若選 B1-c，解 ADR-028 與 FormSchema 不可客製的衝突） | 需新 ADR |

> 回歸防護：**未設 CustomizeId 時，BO 解析結果與現況一致**。

---

## 4. 給 review 的提問

1. **實務上是否常有「多個客製需求疊加在同一張單據」？**
   （例：A 客戶要改存檔驗證、同時又要改預設值來源）
   → 決定 B1 是否需要 hook 機制，還是「換掉整個 BO」就夠。
2. **客製 BO 由誰開發、怎麼部署？** 單一供應商 vs 多夥伴各自交付？
   → 決定 B3 是否需要組件隔離。
3. **客製 BO 解析失敗的期望行為？** 靜默降級（現況）／降級但記錄／啟動時快速失敗？
4. **LowCode 規則客製有需求嗎？** 若有，需先解決「規則存在不可客製的 FormSchema 裡」的衝突。
