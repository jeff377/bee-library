# ADR-035：業務邏輯 plugin（在既有流程上掛載，而非取代整個 BO）

## 狀態

**已採納（Accepted，2026-08-06）** —— 決策已執行。`PluginSettings` 定義型別、`FormBusinessPlugin`
基底與四個掛載點、兩層相加的疊加語意、以及客製層的第一條寫入路徑皆已落地。

本 ADR 記錄六個長效決策：**掛載而非取代**、**掛載點的封閉集合與新增判準**、**宣告粒度與
per-operation 生命週期**、**兩層相加且無移除語意**、**失敗處理的兩種不對稱**、
**與規則引擎的分界**。

## 背景

[ADR-016](adr-016-multitenant-customization-overlay.md) 的客製化覆蓋層提供了四種機制：語系、
FormLayout、客製 BO、客製 Repository。其中「改變業務行為」只有一種途徑——**繼承整個 BO 類別**
並在 `ProgramSettings` 換掉綁定（[ADR-034](adr-034-progid-type-registry.md)）。

這個粒度對常見需求過重：

- 「存檔後發一封通知」要為此接管整張單據的商業物件。
- 多個客製需求疊加時，只能寫成一個大雜燴子類——它們之間沒有任何隔離。
- 客製 BO 一旦存在，套裝 BO 日後新增的步驟能否生效，取決於子類有沒有記得呼叫 `base`。

`FormBusinessObject` 的 Save / Delete 早已各切三段可覆寫子方法：

```
Save:   DoBeforeSave → [擷取變更集] → DoSave → [寫變更稽核] → DoAfterSave
Delete: DoBeforeDelete → DoDelete → [寫刪除稽核] → DoAfterDelete
```

缺的不是擴充點，而是**一種比繼承更輕、且可逐租戶宣告的掛載方式**。

## 決策一：掛載而非取代，另立 `PluginSettings`

以新的 `PluginSettings.xml` 承載 plugin 鏈，`ProgramSettings` 維持「progId → 型別綁定」的
註冊表定位不變。

plugin 在各 `Do*` 子方法的**最終實作之後**執行——那個最終實作可能是套裝 BO 的 base，也可能是
客製 BO 的覆寫。因此**繼承與 plugin 兩種擴充手段可以疊著用**：需要接管整個流程時繼承，
只想加一段時掛 plugin。

### 為何不放進 `ProgramSettings`

`ProgramSettings` 的語意是「這個 progId **是**哪個型別」——一個 progId 一個 BO、一個 Repository，
是**擇一**關係。plugin 是「這個 progId **還要多做**哪些事」——一個 progId 多個 plugin，是**相加**
關係。兩種語意混在同一份檔案裡，覆寫規則會變成逐屬性的例外清單。

## 決策二：四個掛載點，且集合是封閉的

`BeforeSave` / `AfterSave` / `BeforeDelete` / `AfterDelete`。命名用**生命週期階段**而非 BO 方法名
（`Do*` 前綴屬於 BO 的可覆寫步驟，是另一層概念）。

### 為何不是六個

草案原為每個 `Do*` 子方法一個。砍掉 `DoSave` / `DoDelete` 兩個後置點的理由**不是「想不到用途」**，
而是它們與 `After` 行為完全相同：都在交易外、都能改 `RefreshedDataSet`，而稽核讀的是 `DoSave`
之前擷取的 diffgram、兩者皆不影響。留著只是逼使用者做一個沒有正確答案的選擇題。

讀取類方法（`GetList` / `GetData` 等）沒有三段式結構，且**裁決不拆**。可掛載的範圍因此封閉在
Save / Delete 兩條管線內。

### 新增掛載點的三關判準

1. **與相鄰點可區分** —— 行為與既有點不同，不只是位置不同。
2. **有具體用途** —— 有真實需求推動，不為對稱而開。
3. **不會把人引到危險位置** —— 這一關會刷掉「有用途」的候選。例如 `DoSave` 前置落在稽核快照
   **之後**，在那裡改資料會寫進資料庫卻不進稽核軌跡。

### 成本不對稱決定了預設值

掛載點是公開契約：**加是非破壞性的、減是破壞性的**。因此預設偏少，由真實需求推著擴充，
而不是先開好一整排等人來用。

## 決策三：設定檔只列型別，時點由類別自己 override

```xml
<PluginSettings>
  <Items>
    <ProgramPluginItem ProgId="Order">
      <Plugins>
        <PluginItem Type="Acme.Plugins.CreditLimitCheck, Acme.Plugins" />
      </Plugins>
    </ProgramPluginItem>
  </Items>
</PluginSettings>
```

`PluginItem` 的 key 是**型別名**——同一個 program 重複宣告同一型別在載入時就被拒絕，
不會靜默跑兩次。

### 否決的替代方案：設定檔明寫「時點 × 型別」

該方案的設定檔可讀性與儲存時的驗證精確度都比較好。否決它的理由是：**它會逼一個業務需求拆成
兩個類別**。而「檢查（BeforeSave）＋ 後續動作（AfterSave）」在 ERP 客製裡是常態而非例外，
拆開後兩段之間沒有共享狀態的地方。

### 因此 per-operation 生命週期是這個選擇的成立條件

plugin 實例為 **per-operation**：Save / Delete 各建構一次，該次呼叫的所有時點**共用同一實例**，
跨時點的狀態靠 instance field 傳遞。

這是本方案相對於「時點 × 型別」的**唯一實質優勢**。若改成每個時點各建一次，本方案就只剩
「設定檔少打幾行」，不值得為此放棄對方的可讀性。runner 延遲建構，但一建就建整條鏈——
per-operation 的保證是「後面的時點找到同一個物件」，只有把建構綁在**操作**而非**時點**上才成立。

### 可讀性代價以工具中和

從 XML 看不出哪個 plugin 跑在哪個時點。解法不是改設定檔結構，而是由載入時的反射
（比對 `MethodInfo.DeclaringType`）算出各時點的執行清單——一份資料兩個用途：過濾無效呼叫，
以及供維護工具顯示執行順序。Dynamics 365 的 registration tool 也是用工具而非設定檔結構解這個問題。

## 決策四：兩層相加，且不提供移除語意

套裝鏈先跑、客製鏈後跑，各自依檔案宣告順序執行。**不引入 priority 數字**（數字最後一定變成
10/20/30 的爛帳）。

這是客製化覆蓋層裡**唯一「相加」粒度的項目**——其餘四種都是擇一（語系 per key、FormLayout
整檔、BO / Repository per progId）。理由回到決策一的語意差異：binding 指名的是「這個程式就是
這個型別」，客製要換就得取代；plugin 只是多一個步驟，兩層的 plugin 並不衝突。

客製只寫自己新增的 plugin，套裝日後補的 plugin 會**自動生效**。

**代價：客製無法停用套裝宣告的 plugin。** 沒有 tombstone 語法。真要拿掉，就走繼承覆寫該子方法
——這正好回到「需要接管流程時用繼承」的分工。

兩層的 API 表面都開（`DefineType` / `IDefineAccess` / cache / path / reader 一整套），
框架只是目前**不出套裝檔**，保留日後以 plugin 組裝選配模組的可能。

## 決策五：失敗處理的兩種不對稱

### 執行期例外：一律往上拋

任一時點的 plugin 拋例外 → 例外往上拋，`Save` / `Delete` 回報失敗。要給使用者看的訊息丟
`UserMessageException`（框架既有的業務流程中止訊號，規則引擎已在用）——plugin 不需要任何新機制。

**否決**：吞例外 + 記 log（客製的重要後續動作失敗時無人知道）；包進同一 transaction 回滾
（交易不上提到 BO 層）。

但「拋出即中斷」對驗證類 plugin 是對的，對**外部系統同步**則會變成「對方系統維護中，使用者就
存不了單」——實務上必然被整包 `try-catch` 繞過，框架定了規則、大家都繞過，規則就沒了意義。
因此**不加機制，改在文件明訂責任歸屬**：

> 外部同步類的 plugin 應自行處理失敗（記錄後不重拋，或登記重試），不要讓外部系統的可用性決定
> 使用者能不能完成作業。框架的預設是「拋出即中斷」，因為驗證類 plugin 需要它；哪些失敗該中斷
> 作業，是 plugin 作者的判斷。

並須知道 plugin 在**交易外**：行程若在 plugin 執行前掛掉，就是資料已提交、同步沒發生、
且不留痕跡。不能漏的同步要在交易內登記 outbox，plugin 只適合盡力而為或有對帳兜底的場景。

### 解析失敗：一律拋，與 BO 軸相反

BO 型別載不到時**降級**到 `FormBusinessObject`（不中斷服務，但記錄 error）；
plugin 型別載不到時**直接拋**。

不對稱是刻意的：binding 指名的是「這個程式就是這個型別」，退回仍是**能跑的程式**；
plugin 是作者刻意加上的，略過等於**客製沒生效**——靜默漏掉一段信用額度檢查，比拒絕存檔更糟。

同理，`PluginSettings` 的寫入 API 在**存檔前逐一驗證**每個型別可載入、繼承 `FormBusinessPlugin`、
且至少 override 一個時點，一筆不合格整份拒存。驗證放在寫入端的用意是：編輯的人**當場**知道打錯
字，而不是幾週後某張單據存不了。什麼時點都沒 override 的 plugin 掛了等於沒掛，屬設定錯誤。

## 決策六：與規則引擎（ADR-028）的分界

| | 規則（`IFormRuleProcessor`） | plugin |
|---|---|---|
| 存放 | FormSchema 內（**不可客製**，ADR-016 永久排除） | `PluginSettings.xml`（**可客製**） |
| 形式 | 宣告式運算式 | 編譯後的型別 |
| 適用 | 欄位級預設值、計算、驗證 | 跨表 / 跨系統副作用 |
| 部署 | 改定義檔 | 交付組件 |

**「規則引擎沒接上客製層」不是待補缺口，而是刻意分工**：規則不客製，客製走 plugin。
規則存在 FormSchema 內，而 FormSchema 同時驅動資料庫結構與驗證，逐租戶分歧會讓實體 schema 裂開
（[ADR-016](adr-016-multitenant-customization-overlay.md)）。

### 附帶決策：Repository 不開 plugin

資料存取層的攔截會讓 SQL 行為變得不可追蹤。需要改就走「換掉整個 Repository」。

## 影響

### 客製層首度可寫

`PluginSettings` 是**唯一有維護 API 的客製定義**（`GetCustomizePluginSettings` /
`SaveCustomizePluginSettings`，皆為 `LocalOnly`）。客製層在此之前全面唯讀、也因此沒有快取失效
機制，兩者一併補上——檔案模式寫完即 evict 該租戶的 cache slot（維護工具會馬上讀回自己剛存的
東西，file watcher 的「最終會到」不是這裡該有的契約）。

`CustomizeOnlyStorage` 維持全面唯讀，寫入改由 `ICustomizeDefineWriter` 直接經
`CustomizeOnlyPathOptions` 落檔：兩者共用同一份路徑來源，那個類別的唯讀承諾不必為單一例外破功。

### 連帶修正：`DeleteContext.Snapshot` 的載入條件

原條件在稽核關閉且無 `BeforeDelete` 規則時不載入快照，而 `AfterDelete` 做外部同步一定需要知道
刪掉的是什麼。條件加入「該 progId 有沒有 delete 時點的 plugin」——否則 snapshot 的有無會取決於
**與 plugin 無關的稽核開關**，同一個 plugin 在一個部署正常、在另一個拿到 `null`，是最難查的
那種差異。

### 主要型別

- `src/Bee.Definition/Settings/PluginSettings/` —— `PluginSettings` / `ProgramPluginItem` /
  `PluginItem` 與兩個集合型別
- `src/Bee.Business/Form/FormBusinessPlugin.cs` —— 基底與四個虛擬空實作
- `src/Bee.Business/Form/FormPluginChain.cs` / `FormPluginRunner.cs` /
  `PluginSettingsResolver.cs` —— 解析、鏈與執行
- `src/Bee.Definition/Customization/CustomizeOverlay.cs` —— `GetPluginTypes`（唯一的相加疊加）
- `src/Bee.Definition/Storage/ICustomizeDefineWriter.cs` —— 客製層寫入路徑

### 相關文件

- [租戶客製化](../customization.zh-TW.md) —— 五種機制的決策表與 how-to
- [端到端開發指引](../development-cookbook.zh-TW.md) —— 「業務 plugin」一節
- [ADR-016](adr-016-multitenant-customization-overlay.md) —— 客製化覆蓋層
- [ADR-028](adr-028-expression-rule-engine.md) —— 運算式與規則引擎
- [ADR-034](adr-034-progid-type-registry.md) —— ProgId 型別註冊表
