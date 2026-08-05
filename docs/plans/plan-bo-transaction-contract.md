# 計畫：BO 擴充點的交易邊界契約

**狀態：📝 擬定中（決策已定案，待實作）· 2026-08-05**

> 範圍：把 `FormBusinessObject` 的 `Save` / `Delete` 六個可覆寫子方法的**交易邊界寫成明文契約**。
> **純文件，零程式行為變更。** 與客製化無關——覆寫子方法的客製今天就能用，這份契約讓它用得安全。
> 相關：[業務邏輯 plugin](plan-customization-plugin.md)｜[客製 BO / Repository 類別](plan-customization-business.md)

---

## 0. 一句話結論

現況早已是「只有 `Do*` 在交易中」，但這件事**只存在於實作裡，沒有寫在任何地方**。覆寫
`DoAfterSave` 的人不會知道自己在交易外、資料已經提交；在 `DoBeforeSave` 做並發檢查的人不會知道
那是個 TOCTOU 空窗。本案把它變成契約，寫進 XML doc 與公開文件。

單一階段、無前置、可立即動工。

---

## 1. 契約

```
DoBeforeSave   交易外
DoSave         交易內          ← 唯一在交易中的一段
[變更稽核]      交易外
DoAfterSave    交易外
```

`Delete` 同理（`DoBeforeDelete` / `DoDelete` / `DoAfterDelete`）。

**這是現況的明文化**——交易完全落在 repository 內部
（[`DataFormRepository.Save`](../../src/Bee.Repository/Form/DataFormRepository.cs) 的
`UpdateDataTables`、`Delete` 的 `DbBatchSpec { UseTransaction = true }`），BO 層沒有跨三段的交易。

契約的實質內容是一句話：**外部呼叫不能在交易裡**。`DoBeforeSave` 會跑運算式引擎、查 lookup、
可能呼叫其他 BO；`DoAfterSave` 會發通知、打外部系統。這些留在交易內會讓鎖持有時間被外部 I/O
綁架，是連線池耗盡與分散式死結的標準來源。

---

## 2. 決策紀錄（2026-08-05 定案）

### D1：中止流程＝丟 `UserMessageException`

機制已備，不需新增：[`UserMessageException`](../../src/Bee.Base/Exceptions/UserMessageException.cs)
就是「業務流程中止訊號」，規則引擎的 BeforeSave 規則已在用
（[`FormExpressionCalculator`](../../src/Bee.Definition/Forms/FormExpressionCalculator.cs)），
`JsonRpcExecutor` 轉為 `JsonRpcErrorCode.UserMessage`，client 端
[`ApiConnector`](../../src/Bee.Api.Client/Connectors/ApiConnector.cs) 還原成同型別。

覆寫子方法的客製、以及日後的 plugin，都直接沿用：要給使用者看的訊息丟
`UserMessageException`，其餘例外照常往上拋。

### D2：三個後果要寫進文件

1. **`DoBeforeSave` 的驗證有 TOCTOU 空窗**。檢查庫存足夠 → 另一交易扣光 → 本交易照樣寫入。
   例外機制解決的是「怎麼中止」，不解決「檢查到寫入之間資料是否被改」。**需要原子性的檢查得
   下推到 `DoSave` 內**（條件式 UPDATE、唯一索引、check constraint）；Before 的讀取只適合擋
   明顯錯誤，不能當並發防線。客製作者的直覺一定是「我在 Before 檢查過了」，所以這條必須明講。
2. **變更稽核與資料不原子**。`WriteChangeAudit` 在 `DoSave` 之後、`DoAfterSave` 之前
   （[`FormBusinessObject`](../../src/Bee.Business/Form/FormBusinessObject.cs)），所以「資料寫
   成功、稽核寫失敗」可能發生。這是既有狀況，但立約等於明文承認，文件要寫。
3. **`DoAfterSave` 失敗時資料已提交**。呼叫端看到失敗但資料在。After 的副作用必須自負可重試性，
   發通知這類動作應進佇列。

### D3：交易不上提到 BO 層

要讓「三段都在同一交易」或「稽核與資料原子」，唯一路徑是把交易邊界從 repository 上提到 BO 層：
需要 BO 層的交易 API、跨 repository 的交易協調、連線生命週期管理。**明確裁決不做**——代價遠大
於收益，記在此處以免日後被當成漏掉的缺口。

需要與主資料同交易的邏輯，寫在客製 Repository 的 `Save` 覆寫裡。

### D4：其他 BO 方法不拆三段

`GetList` / `GetData` / `GetNewData` / `GetLookup` **維持原狀**，三段式只有 `Save` / `Delete`
兩個方法有。**2026-08-05 裁決，不再重議**。

曾評估過的拆分理由與否決依據：

| 方法 | 評估 | 結論 |
|------|------|------|
| `GetList` | 方法體內有 `Authorize(Read)` 與 `CombineWithScope(ResolveScopeFilter(Read))`，客製覆寫整個 public 方法時可能漏抄而繞過授權 | 不拆——多一層間接的複雜度高於收益；客製覆寫時自行保留授權即可 |
| `GetData` | 同上，另有存取稽核 | 不拆，理由同上 |
| `GetLookup` | 已有 `GetLookupFilter()` 這個更精準的擴充點；且刻意不做 Read 授權 | 不拆 |
| `GetNewData` | 需求是設預設值，而預設值屬 FormSchema 規則引擎的職責 | 不拆——開第二條路會製造「預設值到底寫哪」的困惑 |

`SystemBusinessObject` 同樣不拆。

> 連帶結論：[業務邏輯 plugin](plan-customization-plugin.md) 的掛載點**固定為 Save / Delete 的
> 六個子方法**，不會再增加。

---

## 3. 實作

單一階段，純文件：

- `Save` / `Delete` 六個子方法的 XML doc 補上 §1 的契約與 §2 D2 的三個後果——呼叫端在 IDE
  IntelliSense 就該看到「這一段在不在交易中」，這是決定邏輯寫在哪的第一個判準。
- 公開文件（雙語）補一節「BO 擴充點與交易邊界」，含 D1 的 `UserMessageException` 用法與
  D2 第 1 點的 TOCTOU 提醒。

無程式行為變更，無回歸風險。
