# 計畫：客製 BO 與 Repository 類別

**狀態：✅ 已完成（B1、B2 全數落地）· 2026-08-05**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| B1 | `ProgramItem` 欄位級繼承：客製只寫要改的屬性，空值沿用套裝 | ✅ 已完成（2026-08-05） |
| B2 | BO 型別解析失敗的可觀測性：降級但記錄（訊息帶 customizeId） | ✅ 已完成（2026-08-05） |

> 範圍：**以租戶客製的類別整個換掉套裝的 BO / Repository**——`ProgramSettings` 的 progId 綁定。
> 前置：[客製化共同前置](plan-customization-foundation.md)（缺口 A、B 已於 F1／F2 補完，本案無阻塞）
> 相關：[業務邏輯 plugin](plan-customization-plugin.md)（輕量擴充的另一條路）｜[BO 擴充點的交易邊界契約](plan-bo-transaction-contract.md)｜[Layout 客製](plan-customization-layout.md)｜[語系客製](plan-customization-language.md)｜[ADR-016](../adr/adr-016-multitenant-customization-overlay.md)｜[ProgId 型別註冊表](plan-progid-type-registry.md)

---

## 0. 一句話結論

**「換掉整個 BO / Repository」這條路已經完整可用**——接線、快取、fail-fast 都在 2026-08-04 的
註冊表重構中落地。剩下兩個缺口，都不大但都會靜默出錯：

1. **覆寫粒度太粗**——客製只想換 BO，卻會連帶把套裝的專屬 Repository 打掉，且無聲。
2. **一般 progId 的 BO 型別解析失敗完全無訊號**——打錯字只是悄悄退化成通用 CRUD。

兩項合起來的改動不大，可獨立出貨、獨立回歸，**不依賴任何其他 plan**。

---

## 1. 現況（2026-08-05 覆核）

> 本節依實際程式碼寫成。2026-07-25 初版的「未接線」「五種失敗全部靜默吞掉」等描述已被
> [ProgId 型別註冊表](plan-progid-type-registry.md) 的落地推翻，不再適用。

### 1.1 覆寫語意：per-progId 擇一，且兩軸都已接線

[`CustomizeOverlay.FindProgramItem`](../../src/Bee.Definition/Customization/CustomizeOverlay.cs)：

```
客製 ProgramSettings 有該 progId → 整個 ProgramItem 取代套裝的同 progId
客製沒有                        → 用套裝的
兩邊都沒有                      → null（BO 降級 FormBusinessObject、Repository 用 DataFormRepository）
```

兩個消費端都已從 `SessionInfo.CustomizeId` 取值，不再是空字串：

- [`BusinessObjectFactory.GetCustomizeId`](../../src/Bee.Business/BusinessObjectFactory.cs)
- [`RepositoryFactory.FindProgramItem`](../../src/Bee.Repository/Factories/RepositoryFactory.cs)

### 1.2 失敗語意：三種行為並存

| 對象 | 型別載不到 / 不相容 | 依據 |
|------|-------------------|------|
| reserved progId 的 BusinessObject | **throw** | 降級成 `FormBusinessObject` 會讓症狀變成 JSON-RPC method not found，誤導診斷方向 |
| 任何 progId 的 Repository | **throw** | 資料存取沒有無害的降級模式；跑框架自己的 SQL 等於繞過作者刻意換掉的邏輯 |
| 一般 progId 的 BusinessObject | **靜默降級** | 一個 progId 設錯只該讓那張單據退化成通用 CRUD，不該讓系統停擺 |

第三列是 B2 要補的：行為保留，但要留下訊號。

### 1.3 ★缺口：`ProgramItem` 現在有兩個綁定，whole-item 覆寫變成陷阱

[`ProgramItem`](../../src/Bee.Definition/Settings/ProgramSettings/ProgramItem.cs) 現在承載
`DisplayName` + `BusinessObject` + `Repository`。整個 item 取代的語意下：

```xml
<!-- 套裝 -->
<ProgramItem ProgId="Order" DisplayName="訂單"
             BusinessObject="Erp.OrderBo, Erp" Repository="Erp.OrderRepository, Erp" />

<!-- 客製：只想換 BO -->
<ProgramItem ProgId="Order" BusinessObject="Cust.A.OrderBo, Cust.A" />
```

結果 `Repository` 變空 → 整張單據的資料存取從 `Erp.OrderRepository` **無聲**掉回通用
`DataFormRepository`（空字串是合法的「用預設」，只有型別載不到才 throw）。反向（只寫
Repository）則 BO 掉回 `FormBusinessObject`，同樣無聲。`DisplayName` 也一併變空。

客製作者的直覺必然是「只寫要改的那個」，踩中率高，而症狀（客製 BO 生效了，但 SQL 行為變成
通用的）極難聯想到根因。

### 1.4 現有測試

[`ProgramSettingsBoTypeResolverCustomizeTests`](../../tests/Bee.Business.UnitTests/ProgramSettingsBoTypeResolverCustomizeTests.cs)
（per-progId 擇一、cache 隔離、base 缺檔）與
[`CustomizeOverlayTests`](../../tests/Bee.Definition.UnitTests/Customization/CustomizeOverlayTests.cs)
皆為手動傳 customizeId 的元件級測試。B1 會改變 `FindProgramItem` 的行為，這兩處需同步調整。

---

## 2. 決策紀錄（2026-08-05 定案）

### D1：`ProgramItem` 內採欄位級繼承，空值沿用套裝

`CustomizeOverlay.FindProgramItem` 從「擇一回傳」改為「合成回傳」：

```
cust 有、base 無 → 直接回 cust（不合成）
cust 無、base 有 → 直接回 base（不合成）
兩邊都有         → new 一個 ProgramItem，逐屬性：cust 非空取 cust，否則取 base
```

- **必須 new，不可 mutate**——兩層都是 process-wide cache 實例，改任一邊會跨 session 污染
  （見 [development-constraints.md](../development-constraints.md) 的 Definition Data
  Immutability After Init）。
- 判空用 `StringUtilities.IsEmpty`（含純空白），與 `RepositoryFactory.ResolveFormRepositoryType`
  現有判定一致。
- **「刻意退回框架通用」以顯式型別名表達**：BO 寫
  `Bee.Business.Form.FormBusinessObject, Bee.Business`、Repository 寫
  `Bee.Repository.Form.DataFormRepository, Bee.Repository`。後者剛好命中
  `CreateFormRepositoryCore` 既有的 `type == typeof(DataFormRepository)` 特判，走 `new` 而非
  `ActivatorUtilities`，與空值路徑等價。
- **維護風險**：日後 `ProgramItem` 再加屬性、忘了加進合成邏輯 → 該屬性靜默退回 whole-item
  語意，症狀與本次要修的一模一樣。防護見 §3 B1 的反射測試。

**否決**：維持 whole-item 取代（僅補警告 log 或只寫文件）——踩中率太高，且症狀離根因太遠。

### D2：一般 progId 的 BO 解析失敗＝降級 + 記錄

保留降級到 `FormBusinessObject` 的行為（不中斷服務），但寫一筆 error log。

- `Bee.Business` 目前無 logging 依賴（`ILogger` 只出現在 `Bee.Hosting` /
  `Bee.Api.AspNetCore`），需加 `Microsoft.Extensions.Logging.Abstractions`。
- 走 optional ctor 參數 `ILogger<ProgramSettingsBoTypeResolver>? logger = null`，與現有的
  `ICustomizeDefineReader?` 同風格——null 即不記錄，便利建構子與既有測試不受影響。
- **訊息必須帶 customizeId 與來源層**，否則多租戶下分不出是哪一層的錯字。`ResolveCore` 目前
  是 static 且不知道 item 來自哪層，需一併調整。
- 只記兩種情況：型別載不到、型別不繼承 `BusinessObject`。「item 為空 / progId 未註冊」是正常
  路徑，不記。
- **type cache 會讓同一組 `(customizeId, progId)` 只記一次**（只有 cache miss 才進
  `ResolveCore`）。不洗版是好事，但要在 XML doc 寫明「不會每次呼叫重複，定義重載後才會再
  出現」，否則維運會誤判成問題自己好了。

**否決**：改 fail-fast（一個租戶的錯字會讓該單據完全不能用）；啟動時驗證所有客製型別（需能
列舉所有 customizeId，且客製檔新增時不會重驗）。

---

## 3. 階段

### B1 — `ProgramItem` 欄位級繼承（✅ 已完成 2026-08-05）

- [`CustomizeOverlay.FindProgramItem`](../../src/Bee.Definition/Customization/CustomizeOverlay.cs)
  改為 D1 的合成語意；兩層都有時 `new` 一個 `ProgramItem`，只有一層宣告時直接回該層實例。
- 反射防護測試落在 `CustomizeOverlayTests`：列舉 `ProgramItem` 所有 public 可寫字串屬性
  （排除 `ProgId` / `Key`），逐一驗證「客製有值取客製、其餘取套裝」。日後加屬性沒補就紅。
- 既有 4 個 `FindProgramItem` 測試只斷言 `BusinessObject`，合成語意下結果不變，無需調整；
  另加 4 個新案例（只寫 BO、只寫 Repository、不 mutate 兩層、單層直接回實例）。
- 文件：[definition-files-overview](../definition-files-overview.md) 雙語的粒度表由「整筆取代」
  改為「progId 級 → 屬性級」，並補「顯式指名框架型別」的退回寫法。

回歸防護：未設 CustomizeId 時解析結果與現況一致；客製 item 完整重述所有屬性時行為亦與現況一致。

### B2 — BO 解析失敗可觀測性（✅ 已完成 2026-08-05）

- `Bee.Business` 加 `Microsoft.Extensions.Logging.Abstractions` 10.0.0（對齊 `Bee.Hosting`）。
- [`ProgramSettingsBoTypeResolver`](../../src/Bee.Business/ProgramSettingsBoTypeResolver.cs) 新增
  **三參數建構子**承載 optional logger。改為新多載而非在既有二參數建構子加預設參數——後者雖然
  source 相容，但屬二進位破壞性變更。DI 以 `sp.GetService<ILogger<...>>()` 注入，無 logging 的
  host 照常運作。
- 合成後的 item 不再帶「來源層」，故 `DescribeOrigin` 直接查客製副本判斷 `BusinessObject` 由哪層
  宣告，訊息寫成 `declared by customization 'acme'` / `declared by the base registry`。
- XML doc 寫明「因 type cache，同一組 (customizeId, progId) 只記一次，重載才會再出現」。
- 測試：4 個（載不到 + 客製來源、不繼承 `BusinessObject` + 套裝來源、成功不記、重複呼叫只記一次）。

### 平行路徑與連帶修正

- `FindProgramItem` 的兩個消費端（`ProgramSettingsBoTypeResolver`、`RepositoryFactory`）共用同一個
  overlay，改動自動生效——這正是把疊加邏輯集中在 `CustomizeOverlay` 的用意。
- 兩處 XML doc 明說「整筆取代 / 從不合併」，已隨語意更新：resolver 的類別 remarks 與
  `RepositoryFactory.FindProgramItem`。
- [ADR-016](../adr/adr-016-multitenant-customization-overlay.md) 的粒度表加註 2026-08-05 修訂：
  定案時 `ProgramItem` 只有 `BusinessObject` 一個綁定，整筆取代與屬性級繼承無差別；
  `Repository` 加入後才分歧。修訂不影響「不 merge 成單一物件」的核心決策——合成結果是查找當下
  產生的新實例，兩層快取物件都不被異動。

---

## 4. 仍未定案

- **組件部署模型**：所有租戶的客製 BO / Repository DLL 共用 host bin，無隔離、無版本並存。
  維持現狀不在本案範圍。若日後客製由多個夥伴各自交付、需版本並存，得重新檢視——但
  `AssemblyLoader` 刻意使用 default context（避免 static 狀態分裂），改動面不小。
