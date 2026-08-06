# 計畫：業務邏輯 plugin

**狀態：📝 擬定中（決策已定案，待實作）· 2026-08-05**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| G1 | `PluginSettings` 定義型別與讀取管線（path / storage / cache / reader / overlay） | ✅ 已完成（2026-08-06） |
| G2 | `FormBusinessPlugin` 基底與掛載點的執行接線 | 📝 待做 |
| G3 | 客製 plugin 設定的 API 維護（讀 / 存 / 儲存時驗證 / 快取失效） | 📝 待做 |
| G4 | 端到端測試與雙語文件 | 📝 待做 |

> 範圍：**在套裝 BO 的既有流程上掛載客製程式碼**——不換掉整個 BO 類別，只在特定時點追加一段。
> 掛載點：`BeforeSave` / `AfterSave` / `BeforeDelete` / `AfterDelete` 四個（D2）。
> **其他 BO 方法不拆三段**（2026-08-05 裁決，見
> [BO 擴充點的交易邊界契約](plan-bo-transaction-contract.md) D4），可掛載的範圍因此已封閉。
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
也**裁決不拆**——可掛載的範圍因此封閉在上面這兩條管線內，實際開幾個點由 D2 決定。

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

### D2：四個掛載點，每個時點一個控點

| 掛載點 | 執行位置 | 拿得到什麼 | 典型用途 |
|--------|---------|-----------|---------|
| `BeforeSave` | `DoBeforeSave` 尾端——規則引擎之後、**稽核快照之前** | `SaveContext`（`DataSet` 可改） | 驗證、補值、擋單 |
| `AfterSave` | `DoAfterSave` 尾端 | `SaveContext`（`RefreshedDataSet` / `AffectedRows` 已填） | 通知、外部系統同步、開後續單據 |
| `BeforeDelete` | `DoBeforeDelete` 尾端——guard 規則之後 | `DeleteContext`（`Snapshot` 可讀） | 擋刪除 |
| `AfterDelete` | `DoAfterDelete` 尾端 | `DeleteContext`（`Snapshot` 與 `RowsAffected`） | 清理關聯、外部系統同步 |

plugin 一律跑在該段**最終實作之後**——不論那是套裝 BO 的 base 實作，還是客製 BO 的覆寫。兩種手段
因此可疊著用。命名用生命週期階段而非 BO 方法名：`Do*` 前綴屬於 BO 的可覆寫步驟，plugin 不是在
覆寫它們。

```csharp
public abstract class FormBusinessPlugin
{
    public virtual void BeforeSave(SaveContext context) { }
    public virtual void AfterSave(SaveContext context) { }
    public virtual void BeforeDelete(DeleteContext context) { }
    public virtual void AfterDelete(DeleteContext context) { }
}
```

#### 宣告粒度：設定檔只列型別，時點由類別自己 override

```xml
<Program ProgId="Order">
  <Plugin Type="Cust.A.CreditLimitPlugin, Cust.A" />
</Program>
```

**否決的替代方案**是設定檔明寫「時點 × 型別」（`<BeforeSave Type="..."/>`）＋ 每個時點一個介面。
它的設定檔一眼看得出各時點的執行順序、儲存時也能驗得更精確（型別必須實作該時點的介面），但代價
是**一個業務需求得拆成兩個類別**——「檢查信用額度（`BeforeSave`）＋ 超額時通知風控
（`AfterSave`）」是 ERP 客製的常態而非例外，拆開後兩段之間沒有共享狀態的地方。那是把框架的形狀
強加到業務邏輯上。

##### plugin 實例的生命週期＝per-operation ★這條是 A 成立的前提

**每次 `Save` / `Delete` 呼叫建構一次，該次呼叫的所有時點共用同一個實例。** 上面那個例子裡
「超了多少」就放在 instance field，由 `BeforeSave` 算、`AfterSave` 用。

若改成每個時點各建一次，跨時點的狀態就無處可放，A 相對於 B 的優勢只剩「設定檔少打幾行」——不值得
為此放棄 B 的可讀性。**這條不落實等於沒選到 A。**

連帶：plugin 不是 singleton，不需要考慮執行緒安全；也因此 instance field 是合法的設計手段，而非
要在文件勸阻的東西。

##### 代價：設定檔看不出各時點的執行順序

`Plugin1` 只 override `AfterSave`、`Plugin2` override `BeforeSave` 與 `AfterSave` 時，實際執行是：

```
BeforeSave: Plugin2
AfterSave:  Plugin1 → Plugin2
```

從 XML 完全看不出來，得翻兩個類別的原始碼。而「為什麼我的檢查沒跑」正是這類機制最常見的求助。

**中和方式見 G2**：框架反正要在載入時掃型別，順手用反射算出「各時點實際會跑哪些 plugin」——維護
工具顯示它，同一份資料也拿來過濾無效呼叫。用工具給可讀性，不用把 XML 結構改成 B（Dynamics 365
的 registration tool 走的也是這條路）。

**選用準則（要寫進文件）**：

| 需求 | 手段 | 能力 |
|------|------|------|
| 攔截或取代既有邏輯 | 繼承 BO 覆寫子方法 | 可包夾 `base.DoXxx()` 前後，也可不呼叫 base 完全取代 |
| 在既有邏輯之後追加 | plugin | 只有後置一個控點 |

#### 新增掛載點的判準（三關全過才加）

先前草案是六個（每個 `Do*` 子方法一個）。砍成四個的理由不是「想不到用途」，而是
`DoSave` / `DoDelete` 後置**與 After 行為完全相同**：都在交易外、都能改
`RefreshedDataSet`、稽核讀的是 `DoSave` 之前擷取的 diffgram，兩者皆不影響。留著只是逼使用者做
一個沒有正確答案的選擇題。

日後有人提議加掛載點時，套這三關：

1. **與相鄰掛載點可區分**——行為必須有可觀察的差異。不可區分的選項是純負成本：文件要解釋、
   使用者要猜、猜錯又沒差別。
2. **有具體用途**——必要但不充分。講不出真實需求就別開。
3. **不會把人引到危險位置**——這關會刷掉「有用途」的候選。「`DoSave` 前置」用途清楚（存檔前
   最後一刻改資料），但它落在稽核快照之後，改了資料**會寫進 DB 卻不進稽核**。位置本身是錯的，
   開了等於發邀請函。

**成本不對稱決定了預設偏少**：掛載點是公開契約，**加是非破壞性的、減是破壞性的**。客戶一旦掛了
plugin 上去，那個點就永久存在。反過來，日後真需要「規則引擎之前」，隨時可加一個 `BeforeCompute`，
既有 plugin 不受影響。反面教材是 SAP 的 Enhancement Framework——implicit enhancement point 幾乎
鋪滿每個 method 的頭尾，結果升級時沒人說得清哪些客戶程式碼會受影響。

#### 三點附帶結論

- **plugin 永遠在交易外**——交易在 repository 內部就已開閉
  （見[交易邊界契約](plan-bo-transaction-contract.md) §1）。這對「外部系統同步」這個用途有直接
  影響，見下方分工表。
- **`AfterSave` 能改的只有回給 client 的內容**。改 `context.DataSet` 沒有意義（已存檔），改
  `RefreshedDataSet` 會影響 client 收到什麼。文件要講，否則會有人在那裡改 `DataSet` 然後困惑為
  什麼沒進資料庫。
- **`BeforeSave` 是 plugin 唯一能安全改資料的位置**：在稽核快照之前，所以改動會被稽核記到；
  也在持久化之前，所以改動真的會寫進去。擋單丟例外即可，「只有後置」不損失擋單能力。

#### 外部系統同步該寫在哪

`AfterSave` / `AfterDelete` 的典型用途之一是同步資料到其他系統。因為 plugin 在交易外，**plugin
執行前行程掛掉，資料已提交而同步沒發生，且不留痕跡**。要「不漏」的標準解是 transactional
outbox：在同一個交易內寫一筆待同步記錄，背景 worker 再送——而那個寫入點在交易內，不是 plugin。

| 同步的可靠性要求 | 正確位置 |
|---|---|
| 不能漏（財務、庫存、對外承諾） | 客製 Repository 交易內登記 outbox ＋ 背景送出；plugin 頂多戳一下 worker |
| 盡力而為，或有對帳／排程兜底 | `AfterSave` / `AfterDelete` plugin 直接送 |

這條要寫進文件，否則「用 AfterSave 做同步」會在某天變成一張漏單的客訴。

### D3：plugin 例外一律往上拋

任一時點的 plugin 拋例外 → 例外往上拋，`Save` / `Delete` 回報失敗。

要給使用者看的訊息丟
[`UserMessageException`](../../src/Bee.Base/Exceptions/UserMessageException.cs)——框架既有的業務
流程中止訊號，規則引擎已在用。plugin 不需要任何新機制。

After 時點的 plugin 失敗時資料已寫入，呼叫端會看到「失敗但資料已存」——文件必須明訂
**After 時點的 plugin 自負可重試性**，發通知這類副作用應進佇列而非同步做。

**否決**：吞例外 + 記 log（客製的重要後續動作失敗時無人知道）；包進同一 transaction 回滾
（交易不上提到 BO 層，見[交易邊界契約](plan-bo-transaction-contract.md) D3）。

#### After 時點的失敗，由 plugin 自己判斷要不要中斷

「拋出即中斷」對驗證類 plugin 是對的，但對外部系統同步會變成：**對方系統維護中，使用者就存不了
單**。實務上的反應必然是把 plugin 整包 `try-catch` 吞掉——框架定了規則、大家都繞過，規則就沒了
意義。

不加機制，改在文件把責任講明：

> 外部同步類的 plugin 應自行處理失敗（記錄後不重拋，或登記重試），不要讓外部系統的可用性決定
> 使用者能不能完成作業。框架的預設是「拋出即中斷」，因為驗證類 plugin 需要它；哪些失敗該中斷
> 作業，是 plugin 作者的判斷。

這樣既保住「失敗不可靜默」（plugin 自己要記錄），又不把外部系統的可用性綁進使用者的作業流程。

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
- ~~**可寫的客製層**（G3 的前置）~~ **→ 移到 G3**。base 層的 `SavePluginSettings` 已隨管線落地，
  但**客製層仍然唯讀**（`CustomizeOnlyStorage.SavePluginSettings` 照舊丟 `NotSupportedException`）。
  在沒有呼叫端的階段先開寫入路徑與失效鏈，等於寫一段無法驗證的程式碼；G3 是它唯一的消費者，
  在那裡連同 API、儲存時驗證、cache-notify 一起做才測得出來。

> **G1 落地紀錄（2026-08-06）**：五個定義型別（`PluginSettings` / `ProgramPluginItem` /
> `ProgramPluginItemCollection` / `PluginItem` / `PluginItemCollection`）、`DefineType` 新成員、
> 三個 storage（File / CustomizeOnly / Db）、`PluginSettingsCache`、`ICacheContainer` 與
> `CacheContainerService`、`CacheDefineAccess`、`CustomizeDefineReader`、
> `CustomizeOverlay.GetPluginTypes`。
>
> 兩點與本 plan 撰寫時的預期不同：
> - **`IDefineAccess` 不加 `GetPluginSettings(customizeId)` 多載**。`ProgramSettings` 就沒有——
>   疊加由消費端自己做（讀 base、經 `ICustomizeDefineReader` 讀客製、呼叫 `CustomizeOverlay`）。
>   加一個「看起來會疊加、實際只回 base」的多載反而誤導。
> - **`bee-add-cache-object` skill 描述的「三處同步」實際只有兩處**：`CacheContainerService` 的
>   eviction 陣列與 `TryEvict` 在現行程式碼已不存在（poller 改為發布版本號、由 cache entry 依
>   `ChangeNotifyKey` 自行失效），兩個 CacheNotify 測試 stub 也不再實作 `ICacheContainer`。
>   skill 該段已過期。
>
> **公開文件暫不更新**：`definition-files-overview` 列的是使用者可用的客製型別，而 plugin 在
> G2 之前不會被執行。提前寫進去等於宣告一個還不存在的功能，留到 G4 隨執行接線一起出。

### G2 — `FormBusinessPlugin` 基底與執行接線

- `FormBusinessPlugin` 抽象基底，四個掛載點各一個虛擬空實作，plugin 只 override 需要的時點。
  設定檔只列型別，不宣告時點（D2）。
- plugin 建構走 `ActivatorUtilities.CreateInstance`，簽章比照 repository 的三參數慣例
  `(IBeeContext ctx, Guid accessToken, string progId)`——session、`DefineAccess`、`BoFactory`、
  `Services` 都從 `IBeeContext` 拿。**不把 BO 本身傳進去**：那會連 protected 成員一併曝光，
  鼓勵錯誤耦合。
- **實例生命週期 per-operation**（D2）：`Save` / `Delete` 各建構一次，該次呼叫的所有時點共用同一
  實例。實作上是在 public `Save` / `Delete` 建構一次後放進 context 或區域變數往下傳，**不是**在每
  個時點各建一次。要有測試釘住這件事——它是宣告粒度那個決策的前提，改壞了不會有編譯錯誤。
- **反射算出各時點的執行清單**：載入型別時一併判斷它 override 了哪四個方法中的哪些，得到
  「時點 → 有序 plugin 清單」。兩個用途：(1) 過濾無效呼叫，沒 override 的時點不進 plugin 迴圈；
  (2) 供維護工具顯示執行順序，中和「設定檔看不出誰在哪個時點跑」的可讀性代價（D2）。
  判斷方式是比對 `MethodInfo.DeclaringType` 是否為 `FormBusinessPlugin` 本身。
- 一個什麼時點都沒 override 的 plugin 是**設定錯誤**（掛了等於沒掛），儲存時就該擋下——見 G3 的
  儲存時驗證。
- `FormBusinessObject` 在四個位置的**最終實作之後**依序執行 plugin。注意 `DoBeforeSave` 的
  base 實作會跑規則引擎，plugin 因此看到的是已算好預設值 / 計算欄的資料。
- **修正 `DeleteContext.Snapshot` 的載入條件**：目前是
  `if (auditChange || HasBeforeDeleteRules(schema))`
  （[FormBusinessObject.cs](../../src/Bee.Business/Form/FormBusinessObject.cs)），稽核關閉且 schema
  無 BeforeDelete 規則時 `Snapshot` 為 `null`。而 `AfterDelete` 做外部系統同步時**一定需要知道刪
  掉的是什麼**（只有 `RowId` 不夠），`BeforeDelete` 擋刪除也常要讀內容。條件必須加上「該 progId
  有沒有 plugin」，否則 plugin 拿到的 context 內容取決於一個與它無關的稽核開關——某些部署正常、
  某些拿到 null，是最難查的那種差異。
- 解析與快取：plugin 型別清單依 `(customizeId, progId)` 快取，比照 BO resolver 的 reload 偵測。
  這份清單同時是上一點的判斷依據（清單非空 → 載入 Snapshot）。
- 失敗語意依 D3。

### G3 — 客製 plugin 設定的 API 維護

依 D6 落地，走 `bee-add-bo-method` 的跨層流程（contract / wire / BO / Repository / Client）。

- **開通客製層寫入**（原列於 G1，移來此處）：`CustomizeOnlyStorage.SavePluginSettings` 目前丟
  `NotSupportedException`，這裡開第一個例外；`DbDefineStorage` 的 `customize_id` 欄已可承載，
  主要是把寫入路徑與 cache-notify 接上。放在這裡是因為 G3 是它唯一的消費者，先寫沒有呼叫端的
  寫入路徑無從驗證。

- 兩個 BO 方法：讀回某 customizeId 的 plugin 設定、整份儲存。掛在 `System` 軸（與
  `SaveDefine` / `GetFormSchema` 同源），不另立 reserved progId。
- **兩者都標 `LocalOnly` + `IsLocalCall` 防禦**。不另設權限檢查。
- **儲存時驗證**每個 plugin 型別可載入、繼承 `FormBusinessPlugin`、且**至少 override 一個時點**
  （否則掛了等於沒掛，是設定錯誤）。任一不通過即拒存並指出是哪一筆。
- 儲存成功後 invalidate 該 customizeId 的 cache slot 並發 cache-notify（**多節點必要**：其他節點
  的快取不會因為本節點寫入而自動更新）。
- 稽核：設定變更寫入稽核記錄（誰、哪個 customizeId、改成什麼）。`LocalOnly` 擋掉了遠端濫用，
  但擋不掉「哪一次維護改壞了」的追查需求。

### G4 — 端到端測試與文件

- 帶 CustomizeId 的 session → API → 執行客製 plugin。
- 四個掛載點各一個順序驗證；多 plugin 依宣告順序；例外往上拋致 Save 失敗。
- **同一次 `Save` 的 `BeforeSave` 與 `AfterSave` 拿到同一個 plugin 實例**（釘住 D2 的
  per-operation 生命週期）；不同次呼叫則是不同實例。
- `BeforeSave` 改的資料**進得了稽核**（釘住「稽核快照之前」這個位置）；`AfterDelete` 拿得到
  `Snapshot`（釘住載入條件的修正，且要在**稽核關閉**的組態下測，否則測不到）。
- API 維護路徑：遠端呼叫被拒（attribute 與 `IsLocalCall` 各驗一次）、型別打錯字被拒存、
  存檔後新 plugin 立即生效。
- 雙語文件：客製化指南補 plugin 章節（D2 的選用準則、掛載點表與外部同步分工表、D3 的 After
  失敗處理指引、D7 的分界表、D6 的 `LocalOnly` 定位與多節點限制）。

---

## 4. 仍未定案

- **客製層寫入要不要一次做通用**：G3 採「PluginSettings 專屬的兩個 BO 方法」。另一種形狀是給
  `GetDefineArgs` / `SaveDefineArgs` 加 `CustomizeId` 欄位，讓既有的泛型 define 路徑同時服務兩層
  （空值＝套裝層），由 storage 決定哪些型別支援客製寫入。後者在「日後客製 Language /
  FormLayout 也要能線上維護」時一次到位，但現在就得決定客製層寫入的通用語意。專屬方法先行、
  日後有需求再抽通用，也是合理路徑。
- **組件部署模型**：所有租戶的 plugin DLL 共用 host bin，無隔離、無版本並存。維持現狀不在本案
  範圍。若日後客製由多個夥伴各自交付、需版本並存，得重新檢視——但 `AssemblyLoader` 刻意使用
  default context（避免 static 狀態分裂），改動面不小。
