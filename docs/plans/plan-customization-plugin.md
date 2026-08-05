# 計畫：業務邏輯 plugin

**狀態：📝 擬定中（決策已定案，待實作）· 2026-08-05**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| G1 | `PluginSettings` 定義型別與讀取管線（path / storage / cache / reader / overlay） | 📝 待做 |
| G2 | `FormBusinessPlugin` 基底與掛載點的執行接線 | 📝 待做 |
| G3 | 客製 plugin 設定的 API 維護（讀 / 存 / 儲存時驗證 / 快取失效） | 📝 待做 |
| G4 | 端到端測試與雙語文件 | 📝 待做 |

> 範圍：**在套裝 BO 的既有流程上掛載客製程式碼**——不換掉整個 BO 類別，只在特定時點追加一段。
> 掛載點：`Save` / `Delete` 的六個 `Do*` 子方法。**其他 BO 方法不拆三段**（2026-08-05 裁決，
> 見 [BO 擴充點的交易邊界契約](plan-bo-transaction-contract.md) D4），所以掛載點集合已固定。
> 相關：[客製 BO / Repository 類別](plan-customization-business.md)｜[客製化共同前置](plan-customization-foundation.md)｜[ADR-016](../adr/adr-016-multitenant-customization-overlay.md)｜[ProgId 型別註冊表](plan-progid-type-registry.md)

---

## 0. 一句話結論

目前想在存檔前後加一段檢查，唯一途徑是**繼承整個 BO 類別**並在 `ProgramSettings` 換掉綁定——
對「存檔後發一封通知」這種需求代價過重，多個客製需求疊加時也只能寫成一個大雜燴子類。

本案以新的 `PluginSettings.xml` 承載輕量擴充。而它要能由本機維護工具經 API 維護（D6），使它成為
**第一個可寫的客製定義**——客製層至今是唯讀的、也沒有快取失效機制，這兩件事得一併補上。

---

## 1. 現況

### 1.1 缺口：擴充只有「繼承整個 BO」一種

[`FormBusinessObject`](../../src/Bee.Business/Form/FormBusinessObject.cs) 的 Save / Delete 各切
三段可覆寫子方法：

```
Save:   DoBeforeSave → [擷取變更集] → DoSave → [寫變更稽核] → DoAfterSave
Delete: DoBeforeDelete → DoDelete → [寫刪除稽核] → DoAfterDelete
```

要在其中任一段加東西，只能繼承整個 BO 類別。讀取類方法（`GetList` / `GetData` 等）沒有三段式，
也**裁決不拆**——本案的掛載點因此就是上面這六個。

### 1.2 客製層目前全面唯讀

[`CustomizeOnlyStorage`](../../src/Bee.Definition/Storage/CustomizeOnlyStorage.cs) 除了四個 getter
之外全部丟 `NotSupportedException`，訊息就是 `The customization-override layer is read-only.`。
也因為從來不會變，**客製層沒有快取失效機制**。

---

## 2. 決策紀錄（2026-08-05 定案）

### D1：plugin 另立設定檔，型別綁定留在 `ProgramSettings`

`ProgramSettings` 維持「progId → 型別綁定」的註冊表定位（[ProgId 型別註冊表](plan-progid-type-registry.md)
階段 1 的收斂成果），plugin 鏈另立 `PluginSettings.xml`：

```xml
<PluginSettings>
  <Program ProgId="Order">
    <Plugin Type="Cust.A.CreditLimitPlugin, Cust.A" />
    <Plugin Type="Cust.A.OrderNotifyPlugin, Cust.A" />
  </Program>
</PluginSettings>
```

三個理由：

1. **cardinality 不同**：註冊表是 progId → 1 個型別；plugin 是 progId → N 個型別且有序，塞不進
   `ProgramItem` 的字串屬性。
2. **覆寫語意不同**：型別綁定是取代（含
   [客製 BO / Repository 類別](plan-customization-business.md) 的欄位級繼承）；plugin 是疊加。
   同一個檔內兩種粒度會讓 overlay 規則說不清。
3. **`ProgramSettings.xml` 會被框架整檔改寫**：
   [`ReservedProgIdRegistrationService.Register`](../../src/Bee.Hosting/Registry/ReservedProgIdRegistrationService.cs)
   在缺 reserved progId 時重建整份檔案再存回，程式碼裡已有一段
   `WARNING: every ProgramItem property must be copied here`。把手寫且**有序**的 plugin 鏈放進
   一個會被 wholesale rewrite 的檔案，等於再開一個「漏抄就靜默消失」的坑。

**否決**：型別綁定也搬新檔（推翻剛收斂的註冊表定位）；在 `ProgramItem` 下加 `<Plugins>` 子元素
（撞上理由 2 與 3）。

### D2：plugin 掛在 `Do*` 子方法，一律後置

掛載點＝BO 的可覆寫子方法。plugin 跑在該子方法**最終實作之後**——不論那是套裝 BO 的 base 實作，
還是客製 BO 的覆寫。兩種手段因此可疊著用。

**掛載點固定為六個**：`DoBeforeSave` / `DoSave` / `DoAfterSave` / `DoBeforeDelete` / `DoDelete` /
`DoAfterDelete`。讀取類方法不拆三段（[交易邊界契約](plan-bo-transaction-contract.md) D4），
所以這個集合不會再增加。

**選用準則（要寫進文件）**：

| 需求 | 手段 | 能力 |
|------|------|------|
| 攔截或取代既有邏輯 | 繼承 BO 覆寫子方法 | 可包夾 `base.DoXxx()` 前後，也可不呼叫 base 完全取代 |
| 在既有邏輯之後追加 | plugin | 只有後置一個控點 |

三點附帶結論：

- **`DoSave` 後置與 `DoAfterSave` 只差變更稽核的寫入位置**：`DoSave` 後置在稽核**之前**，
  `DoAfterSave` 在**之後**。兩個都開，但這個差異必須寫清楚，否則沒人分得出該掛哪個。
  注意 `DoSave` 的 plugin **不在交易內**——交易在 repository 內部就已提交
  （見[交易邊界契約](plan-bo-transaction-contract.md) §1）。**plugin 永遠在交易外。**
- **`DoSave` 後置的 plugin 能改 `context.RefreshedDataSet` 與 `AffectedRows`**（`SaveContext`
  是可變的），等於能左右回給 client 的內容。是能力也是風險，文件要標明。
- **`DoBeforeSave` 後置仍可阻擋存檔**——丟例外即可。「只有後置」不損失擋單能力。

### D3：plugin 例外一律往上拋

任一時點的 plugin 拋例外 → 例外往上拋，`Save` / `Delete` 回報失敗。

要給使用者看的訊息丟
[`UserMessageException`](../../src/Bee.Base/Exceptions/UserMessageException.cs)——框架既有的業務
流程中止訊號，規則引擎已在用。plugin 不需要任何新機制。

After 時點的 plugin 失敗時資料已寫入，呼叫端會看到「失敗但資料已存」——文件必須明訂
**After 時點的 plugin 自負可重試性**，發通知這類副作用應進佇列而非同步做。

**否決**：吞例外 + 記 log（客製的重要後續動作失敗時無人知道）；包進同一 transaction 回滾
（交易不上提到 BO 層，見[交易邊界契約](plan-bo-transaction-contract.md) D3）。

### D4：兩層都開，套裝層目前不出檔

API 表面與現有四種客製型別對稱（`DefineType` / `IDefineAccess` / cache / path / reader 一整套
都加），框架只是**不出套裝檔**——保留日後套裝以 plugin 組裝選配模組的可能。

因為兩層都開，**疊加語意現在就要定死**，不能留待日後撞上：

- **套裝先、客製後**，各自依檔案宣告順序執行。不引入 priority 數字（數字最後一定變成
  10/20/30 的爛帳）。
- 客製只寫自己新增的 plugin；套裝日後補的 plugin 會自動生效。
- **不提供移除語意**——客製無法停用套裝宣告的 plugin。真要拿掉就走繼承覆寫該子方法。

### D5：Repository 不開 plugin

資料存取層的攔截會讓 SQL 行為變得不可追蹤。需要改就走「換掉整個 Repository」
（[客製 BO / Repository 類別](plan-customization-business.md)）。

### D6：客製 plugin 設定要能由本機維護工具經 API 維護

它把 `PluginSettings` 從「部署時放檔案」變成「執行期可寫的定義資料」，客製層現行的假設幾乎
全部要重新檢視（§1.2）。

#### 授權：`LocalOnly`，不另設權限

存取方法標 `ApiProtectionLevel.LocalOnly`，比照
[`SystemBO.SaveDefine`](../../src/Bee.Business/System/SystemBusinessObject.Define.cs)：

```csharp
[ApiAccessControl(ApiProtectionLevel.LocalOnly, ApiAccessRequirement.Authenticated)]
public virtual SaveCustomizePluginSettingsResult SaveCustomizePluginSettings(...)
{
    // Defence in depth：ApiAccessValidator 只跑在 JSON-RPC 派發路徑上，直接建構 BO 的
    // 呼叫端（in-process 主機、自訂 dispatcher、子類）不會經過它。
    if (!IsLocalCall)
        throw new NotSupportedException("...is restricted to local calls.");
    ...
}
```

這比任何 token / 權限檢查都嚴——遠端呼叫根本到不了方法本體，所以**不需要**再綁部署層或公司層
權限。**attribute 與 `IsLocalCall` 兩道都要寫**，理由見 `SaveDefine` 的註解：attribute 只擋遠端
API 流量，`IsLocalCall` 擋其餘所有進入方式。

「用戶端自行維護」指的是**本機維護工具**（DefineEditor 這類）透過
[`LocalApiProvider`](../../src/Bee.Api.Client/Providers/LocalApiProvider.cs)（`IsLocalCall = true`）
走同一組 API 介面，**不是**遠端 client。這與既有的定義維護模型一致——定義寫入是部署時操作，
不是應用操作。

#### 儲存時驗證（把錯誤從執行期提前到設定期）

存檔前逐一驗證每個 plugin 型別：可載入、且繼承 `FormBusinessPlugin`。任一不通過即拒存並回報
是哪一筆。這是最好的驗證位置——設定的人當場知道打錯字，而不是等某張單據存檔時才炸。

連帶影響 D3：執行期「解析不到型別」退化為罕見路徑（組件事後被移除才會發生），但仍維持 throw。

#### 儲存後端：兩種都支援，多節點限制寫進文件

[`DbDefineStorage`](../../src/Bee.Db/Storage/DbDefineStorage.cs) 同時實作 `IDefineStorage` 與
`ICustomizeDefineReader`，已有 `customize_id` 欄（base 以 `"*"` 為哨兵值）——**客製層的可寫模型
在 DB backend 上等於已經備好**，且多節點天然共享。

檔案模式（`CustomizeOnlyStorage`）則要新開寫入路徑，並帶兩個限制：

1. **多節點部署下寫檔只寫到執行維護工具的那個節點**，除非 `CustomizePath` 指向共享儲存。
2. **唯讀容器**部署不可寫，此時寫入應以清楚的訊息失敗。

`LocalOnly` 讓這兩點的殺傷力小得多——維護工具本來就跑在伺服器上、頻率極低——所以兩種 backend
都支援，把限制寫進文件即可。

#### 併發

整份 `PluginSettings` 覆寫，last-write-wins。多人同時編輯同一租戶的 plugin 設定會互相蓋掉。
本案不做樂觀鎖——維護頻率極低、操作者人數少——但文件要明講這個行為。

### D7：與規則引擎（ADR-028）的分界

必須一併寫進文件，否則「這該寫規則還是 plugin」會長期困擾：

| | 規則（`IFormRuleProcessor`） | plugin |
|---|---|---|
| 存放 | FormSchema 內（**不可客製**，ADR-016 永久排除） | `PluginSettings.xml`（可客製） |
| 形式 | 宣告式運算式 | 編譯後的型別 |
| 適用 | 欄位級預設值、計算、驗證 | 跨表 / 跨系統副作用 |
| 部署 | 改定義檔 | 交付組件 |

「規則引擎沒接上客製層」因此**不是待補缺口，而是刻意分工**：規則不客製，客製走 plugin。

---

## 3. 階段

### G1 — `PluginSettings` 定義型別與讀取管線

- `PluginSettings` / `ProgramPluginItem`（或等價命名）+ 集合型別，依
  [rules/definition.md](../../.claude/rules/definition.md) 繼承 `KeyCollectionBase<T>`。
  命名需通過 CA1724 與跨 UI 撞名檢查。
- `DefineType` 新增成員（**加在尾端**，wire 值不可重排）。
- `PathOptions.GetPluginSettingsFilePath()` + `CustomizeOnlyPathOptions` 覆寫。
- `IDefineStorage` / `FileDefineStorage` / `CustomizeOnlyStorage` 對應成員。
- cache slot：走 `bee-add-cache-object` 的三處同步（`ICacheContainer` /
  `CacheContainerService` / 兩個 CacheNotify 測試 stub）。
- `IDefineAccess.GetPluginSettings()` 與 `ICustomizeDefineReader.GetCustomizePluginSettings()`。
  後者比照 `GetCustomizeProgramSettings` 先探檔案存在性再進 cache。
- `CustomizeOverlay` 加疊加方法（D4：套裝在前、客製在後，回傳合併後的有序清單）。
- **可寫的客製層**（G3 的前置）：客製 slot 的寫入介面與快取失效鏈。`CustomizeOnlyStorage` 目前
  全面唯讀，此處要開第一個例外；`DbDefineStorage` 的 `customize_id` 欄已可承載，主要是把寫入
  路徑與 cache-notify 接上。

### G2 — `FormBusinessPlugin` 基底與執行接線

- `FormBusinessPlugin` 抽象基底，每個掛載點一個虛擬空實作，plugin 只 override 需要的時點。
  設定檔只列型別，不宣告時點。
- plugin 建構走 `ActivatorUtilities.CreateInstance`，與 repository 一致，可注入 host 服務。
- `FormBusinessObject` 在各子方法的**最終實作之後**依序執行 plugin。注意 `DoBeforeSave` 的
  base 實作會跑規則引擎，plugin 因此看到的是已算好預設值 / 計算欄的資料。
- 解析與快取：plugin 型別清單依 `(customizeId, progId)` 快取，比照 BO resolver 的 reload 偵測。
- 失敗語意依 D3。

### G3 — 客製 plugin 設定的 API 維護

依 D6 落地，走 `bee-add-bo-method` 的跨層流程（contract / wire / BO / Repository / Client）。

- 兩個 BO 方法：讀回某 customizeId 的 plugin 設定、整份儲存。掛在 `System` 軸（與
  `SaveDefine` / `GetFormSchema` 同源），不另立 reserved progId。
- **兩者都標 `LocalOnly` + `IsLocalCall` 防禦**。不另設權限檢查。
- **儲存時驗證**每個 plugin 型別可載入且繼承 `FormBusinessPlugin`，不通過即拒存並指出是哪一筆。
- 儲存成功後 invalidate 該 customizeId 的 cache slot 並發 cache-notify（**多節點必要**：其他節點
  的快取不會因為本節點寫入而自動更新）。
- 稽核：設定變更寫入稽核記錄（誰、哪個 customizeId、改成什麼）。`LocalOnly` 擋掉了遠端濫用，
  但擋不掉「哪一次維護改壞了」的追查需求。

### G4 — 端到端測試與文件

- 帶 CustomizeId 的 session → API → 執行客製 plugin。
- 各時點各一個順序驗證；多 plugin 依宣告順序；例外往上拋致 Save 失敗。
- API 維護路徑：遠端呼叫被拒（attribute 與 `IsLocalCall` 各驗一次）、型別打錯字被拒存、
  存檔後新 plugin 立即生效。
- 雙語文件：客製化指南補 plugin 章節（D2 選用準則、D3 的 After 時點警告、D7 的分界表、
  D6 的 `LocalOnly` 定位與多節點限制）。

---

## 4. 仍未定案

- **plugin 宣告粒度**採「設定檔只列型別、一個類別可 override 多個時點」。若要改成設定檔明寫
  「時點 × 型別」，動工前提出即可，影響僅止於 XML 結構與解析。
- **客製層寫入要不要一次做通用**：G3 採「PluginSettings 專屬的兩個 BO 方法」。另一種形狀是給
  `GetDefineArgs` / `SaveDefineArgs` 加 `CustomizeId` 欄位，讓既有的泛型 define 路徑同時服務兩層
  （空值＝套裝層），由 storage 決定哪些型別支援客製寫入。後者在「日後客製 Language /
  FormLayout 也要能線上維護」時一次到位，但現在就得決定客製層寫入的通用語意。專屬方法先行、
  日後有需求再抽通用，也是合理路徑。
- **組件部署模型**：所有租戶的 plugin DLL 共用 host bin，無隔離、無版本並存。維持現狀不在本案
  範圍。若日後客製由多個夥伴各自交付、需版本並存，得重新檢視——但 `AssemblyLoader` 刻意使用
  default context（避免 static 狀態分裂），改動面不小。
