# 計畫：錯誤契約對映的單一來源

**狀態：✅ 已完成（2026-09-02）**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 1 | 迴歸測試鎖住兩端對映、補上已漂掉的 `ReplayRejected` 呼叫端分支、修正公開文件的錯誤碼表 | ✅ 已完成（2026-09-02） |
| 2 | 抽出單一對映登錄（`型別 ↔ 錯誤碼`），伺服端與呼叫端從同一份宣告消費 | ✅ 已完成（2026-09-02） |

## 題目

使用者提問：

> `CompanyNotEnteredException`、`CompanyAccessDeniedException` 這類白名單例外，是否直接繼承
> `UserMessageException` 即可，這樣會比較容易判斷，未來也不用寫一堆 `if`。

**結論先講：不建議繼承，而且它一個 `if` 都省不掉**（理由見 §3）。但提問指出的痛點是真的，
只是位置不在繼承，而在「同一組對映在兩端各寫一次、沒有任何機制保證同步」——
而**這件事已經發生了三次**（見 §2.5）。本計畫處理的是後者。

---

## 1. 現況盤點（2026-09-02 實地讀過現行程式碼）

### 1.1 例外型別：五個，不是四個

| 型別 | 所在組件 | 基底 |
|------|---------|------|
| `UserMessageException` | `Bee.Base` | `Exception` |
| `CompanyNotEnteredException` | `Bee.Base` | `Exception` |
| `CompanyAccessDeniedException` | `Bee.Base` | `Exception` |
| `ForbiddenException` | `Bee.Base` | `Exception` |
| `ReplayRejectedException` | **`Bee.Api.Core`** | `Exception`（且 `sealed`） |

`ReplayRejectedException` 是 ADR-042 重放防護第三階段（commit `e09a104b`）加進來的，
**不在 `Bee.Base/Exceptions/`**，位置與其他四個不同。它是本題最有價值的證物（§2.5）。

### 1.2 型別的 doc 契約（逐字引述）

`UserMessageException` 的 `<remarks>`：

> Conceptually this is a "business flow interruption signal" rather than a genuine program error:
> control flow is aborted because the operation cannot be completed, and **the message is meant to
> reach the user as-is**.

`CompanyNotEnteredException` 的 `<remarks>`：

> This is a recoverable protocol state, not a business message: **it must not be shown to the user
> verbatim.**

`CompanyAccessDeniedException` 的 `<remarks>` 與建構子 `<param name="message">`：

> The three causes are deliberately merged into one exception carrying one message, so that error
> text cannot be used to enumerate valid company identifiers.
>
> Keep it identical for every cause — a message that distinguishes "no such company" from "not
> granted" reopens the enumeration channel this type exists to close.

`ForbiddenException` 的 `<summary>` / `<remarks>`：

> Thrown when an authenticated caller lacks permission for a specific action on a permission model
> — the layer-1 model+action authorization check. […] the client reconstructs it from that code so
> callers can `catch (ForbiddenException)` and **degrade the UI accordingly**.

`ReplayRejectedException` 的 `<remarks>`：

> This is a distinct type rather than a reuse of `UnauthorizedAccessException` so that the caller
> can tell **"retrying will not help"** apart from "the credential was rejected".

**四份 doc 的共通點不是「訊息能不能顯示」，是「呼叫端該做什麼」**：導向選公司、退回選公司、
降級 UI、別再重試。這正是它們各自要一個獨立錯誤碼的原因，也是 §3 判定的依據。

### 1.3 伺服端：`src/Bee.Api.Core/JsonRpc/JsonRpcExecutor.cs`

`MapException` 現在是**四**條具名 `if`（計畫題目寫三條，`ReplayRejected` 是後加的）：

```csharp
if (ex is CompanyNotEnteredException)  return (CompanyNotEntered, ex.Message);
if (ex is CompanyAccessDeniedException) return (CompanyAccessDenied, ex.Message);
if (ex is ForbiddenException)          return (PermissionDenied, ex.Message);
if (ex is ReplayRejectedException)     return (ReplayRejected, ex.Message);
if (IsUserFacingException(ex))         return (UserMessage, ex.Message);
return (InternalError, SysInfo.IsDebugMode ? ex.Message : "Internal server error");
```

`IsUserFacingException` 是七個型別的白名單：`UserMessageException`、`UnauthorizedAccessException`、
`ArgumentException` 家族、`InvalidOperationException`、`NotSupportedException`、`FormatException`、
`JsonRpcException`（框架自有，非 BCL）。其 doc 自陳後六個是過渡路徑、會逐步移除。

> **關鍵事實：那四個具名型別完全不在 `IsUserFacingException` 的名單裡。**
> 它們靠排在前面的具名 `if` 攔截，白名單根本沒看過它們。§3.3 整個結論建立在這一點上。

### 1.4 呼叫端：`src/Bee.Api.Client/Connectors/ApiConnector.cs`

`FinalizeResponse` 是四個 `if` 比對 `response.Error.Code`（`UserMessage` / `PermissionDenied` /
`CompanyAccessDenied` / `CompanyNotEntered`）各重建對應型別，落不到的走
`InvalidOperationException($"API error: {code} - {message}")`。該方法 remarks 明寫它與
`MapException` **互為反函數**。

### 1.5 錯誤碼：`src/Bee.Api.Core/JsonRpc/JsonRpcErrorCode.cs`

十一個成員（題目寫十個，`ReplayRejected = -32005` 是後加的）。實地清點產生者：

| 碼 | 名稱 | `src/` 產生者 | 呼叫端有無重建分支 |
|----|------|--------------|------------------|
| -32700 | `ParseError` | 1 | ✗（走通用分支，預期如此） |
| -32600 | `InvalidRequest` | 8 | ✗（同上） |
| -32601 | `MethodNotFound` | **0** | ✗ |
| -32602 | `InvalidParams` | **0** | ✗ |
| -32000 | `InternalError` | 2 | ✗（預期如此） |
| -32001 | `Unauthorized` | **0** | ✗ |
| -32002 | `CompanyNotEntered` | 3 | ✓ |
| -32003 | `CompanyAccessDenied` | 4 | ✓ |
| -32004 | `PermissionDenied` | 3 | ✓ |
| -32005 | `ReplayRejected` | 2 | **✗ ← 漂移** |
| -32099 | `UserMessage` | 4 | ✓ |

### 1.6 三筆已經發生的漂移（不是預測，是量到的）

這三筆是本計畫的**實證基礎**。它們全都編譯得過、測試也沒紅。

**漂移一：`ReplayRejected` 只有伺服端那一半。**
`ApiPayloadFrame` 與 `JsonRpcExecutor` 共四處丟 `ReplayRejectedException`，伺服端映成 -32005，
但 `FinalizeResponse` 沒有對應分支 → 呼叫端拿到的是
`InvalidOperationException("API error: -32005 - …")`。該型別 doc 承諾的
「caller can tell 'retrying will not help' apart from …」**在呼叫端拿不到**，
`catch (ReplayRejectedException)` 永遠不會進去。
這正是 Day 18 文章預言的失敗樣態，而它在文章發佈前就已經發生了。

**漂移二：`Unauthorized`（-32001）零產生者，且公開文件寫的是錯的。**
認證失敗實際走 `ApiAuthorizationValidator` 回 `JsonRpcErrorCode.InvalidRequest`（-32600）
＋ HTTP 401（`ApiServiceController:66`）。但
[`docs/jsonrpc-frontend-integration.zh-TW.md`](../jsonrpc-frontend-integration.zh-TW.md)
的錯誤碼表寫著「`-32001` `Unauthorized` Token 缺、無效、過期 → 重新登入」。
照文件寫 client 的人會去接一個永遠不會出現的碼。

**漂移三：公開文件的錯誤碼表沒有 -32005。**
同一張表列了 -32700 到 -32099 共十個碼，`ReplayRejected` 加進列舉時沒有跟上（雙語兩份皆是）。

> 三筆的共通形狀：**沒有任何機制會發現**。編譯器不看，測試不驗，CI 不擋。
> 這與 `single-source.md` 判定的「結構問題，不是紀律問題」是同一件事。

---

## 2. 直接繼承的可行性判定

### 2.1 先回答前置問題：`CompanyNotEnteredException` 那句 doc 是對的嗎？

**是對的，而且應該保留。**

判準是「呼叫端拿到它該做什麼」。這個型別代表的是 **session 少了公司 context**——
使用者可能連公司都還沒選、或 `LeaveCompany` 剛清掉。此時正確的 UX 是**把使用者帶去選公司**，
不是彈一個
`"Company scope is required but the session has no company context."` 這種對使用者無意義的字串。
`CompanyNotEntered` 對應 HTTP 409 Conflict 語意，本質是**協定狀態**，不是業務訊息。

`CompanyAccessDeniedException` 的訊息倒是**可以**顯示（它刻意去識別化成一句固定文字），
但它的 doc 同樣要求「route the user back to company selection」——**要的是導流，不是彈訊息**。
`ForbiddenException` 也一樣，要的是「degrade the UI」。

所以：**要顯示訊息只是這四個型別語意的一小塊，甚至不是重點那一塊。**
把它們掛到一個以「訊息可原樣顯示」為全部語意的基底之下，是把最不重要的那一維當成了分類軸。

### 2.2 繼承實際省下什麼：**零個 `if`**

逐處檢查：

| 位置 | 方向 | 繼承後 |
|------|------|-------|
| `MapException` 四條具名 `if` | 型別 → 碼 | **一條都不能少**。每個型別要**不同**的碼，`is 基底` 分不出來 |
| `IsUserFacingException` 七型白名單 | 型別 → 布林 | **一個 `is` 都不減**。那四個型別**本來就不在名單裡**（§1.3） |
| `FinalizeResponse` 四個 `if` | **碼 → 型別** | **一個都不能少**。`int` 不可能靠 `is` 解析成子類 |

題目假設「至少能讓 `IsUserFacingException` 少列幾個」——那是誤解，那份名單從來沒列過它們。
**淨效果是 `if` 數量不變。**

### 2.3 繼承會多出什麼代價

**代價 A：把型別保證換成順序約定（靜默失效）。**
`MapException` 現在給得出正確的碼，靠的是具名 `if` 排在 `IsUserFacingException` 之前。
一旦變成子類，任何人把順序調換、或「順手合併」那幾條具名分支，
**每一種公司／權限錯誤都會靜默變成 -32099**，編譯得過、多數測試看不出來。
現在由型別系統保證的東西，換成由「別動這個順序」的註解保證。

**代價 B：對外部使用者是 source-breaking，而且是編譯錯誤。**
[`docs/development-constraints.zh-TW.md`](../development-constraints.zh-TW.md) 現在教的 catch 順序是：

```csharp
catch (UserMessageException ex) { ShowMessage(ex.Message); }
catch (Exception ex)            { LogError(ex); }
```

任何依此寫、再往後補一條 `catch (CompanyNotEnteredException)` 的應用程式，
繼承之後那條子類 clause 變成 unreachable → **CS0160 編譯失敗**。
沒補那條的應用程式更糟：**不會編譯錯，但會開始把「must not be shown verbatim」的訊息
直接彈給使用者**——正好是那句 doc 要防的事。這兩種後果都落在框架的外部消費者身上。

**代價 C：二進位破壞性變更。**
改公開型別的基底類別會動到 public API surface，四個型別都在
`src/Bee.Base/PublicAPI.Shipped.txt`（第 112–124 行）。而 `Bee.Base` 是**所有專案的相依**
（見 [`.claude/rules/dependency-boundary.md`](../../.claude/rules/dependency-boundary.md)）。
需處理 `PublicAPI` 檔、判版號、依 `releasing.md` 與 `commit-verification.md` 明寫相容性判定。

### 2.4 判定

**不採行。** 它一個 `if` 都省不掉（§2.2），卻同時買下契約衝突（§2.1）、順序脆弱性（代價 A）、
外部 source-breaking（代價 B）與二進位破壞（代價 C）。

---

## 3. 替代方案比較

| # | 方案 | 解到什麼 | 代價 | 評估 |
|---|------|---------|------|------|
| A | **直接繼承 `UserMessageException`** | 什麼都沒解（§2.2） | 契約衝突、順序脆弱、CS0160、二進位破壞 | ❌ 不採 |
| B | **標記介面 `IUserFacingException`**（只承諾「訊息可原樣送出」） | 讓 `IsUserFacingException` 的**框架自有**型別可用 `is` 一次收攏；避開了 A 的契約衝突與 catch 破壞 | 對六個 BCL 型別**無效**（不能替 BCL 型別加介面），名單仍得留一半；`MapException`／`FinalizeResponse` 的 `if` 依舊一個不減；`Bee.Base` 多一個公開型別 | ⚠️ 可搭配，但單獨做解不到痛點 |
| C | **共用對映登錄**（一份 `型別 ↔ 碼` 宣告，伺服端取 `Type → code`、呼叫端取 `code → Func<string, Exception>`） | **直接解掉痛點**：新增一種錯誤只改一處；漂移一那種缺半邊變成宣告時就看得見 | 多一個型別與一份靜態表；`ReplayRejectedException` 在 `Bee.Api.Core`、其餘在 `Bee.Base`，登錄表得放在同時看得到兩者的層（`Bee.Api.Core`，呼叫端已相依，可行）；`UserMessage` 是多對一（七個型別 → 一個碼），登錄表必須容得下這條不對稱 | ✅ 建議（階段 2） |
| D | **維持現狀，只逐步收掉白名單六個 BCL 型別** | 讓「訊息可否顯示」回歸型別語意 | **與本題無關**：收完 `MapException` 的四條具名 `if` 與 `FinalizeResponse` 的四個 `if` 一條不減；且 `src/` 有 450+ 處 BCL `throw`（多為基礎設施 guard clause），要逐一判斷哪些會走到執行器，工作量遠大於本題 | ⏸ 另案，不在本計畫 |
| E | **迴歸測試鎖住兩端對映** | 讓漂移**當場紅**，而不是靜默 | 只防漂移、不減 `if`；需要一份「刻意不重建的碼」白名單（-32700／-32600／-32000 本來就該落通用分支），這份白名單本身要維護 | ✅ 建議（階段 1），且**單獨就有價值** |

**D 的前提檢查**：題目問「六個過渡型別收掉之後 `if` 還剩幾個？」——
答案是**一個都沒少**，因為那六個型別在 `IsUserFacingException` 裡面，
而痛點的 `if` 在 `MapException` 與 `FinalizeResponse`。收白名單是另一件對的事，但不解這題。

---

## 4. 建議方案

**採 E → C 兩階段，不採 A。**

順序刻意如此：E 便宜、單獨成立、且會**立刻抓出漂移一**；C 的正確性靠 E 驗證。
先做 C 而沒有 E，等於把一個沒有守衛的機制換成另一個沒有守衛的機制。

### 階段 1：鎖住現況（不改對映語意）

1. **新增 `ErrorContractDriftTests`**（形狀參考
   [`tests/Bee.Api.Core.UnitTests/WireContractDriftTests.cs`](../../tests/Bee.Api.Core.UnitTests/WireContractDriftTests.cs)）：
   - 對 `JsonRpcErrorCode` 每個成員斷言「有伺服端產生者」或「在刻意不重建的白名單上」，二者必居其一。
   - 對每個「有具名重建分支」的碼，斷言 `MapException` 反過來也給得出同一個碼（round-trip）。
   - 比照 `WireContractDriftTests` 的做法，**加一條反萎縮斷言**（釘住具體成員），
     否則列舉改名時兩條檢查會一起變成恆真。
2. **補上 `ReplayRejected` 的呼叫端分支**（修漂移一）。
   `Bee.Api.Client` 已 `ProjectReference` → `Bee.Api.Core`，型別看得到，不需搬家。
3. **修正公開文件錯誤碼表**（修漂移二、三），雙語兩份同步：
   補 `-32005`；`-32001` 那列改成與實際一致（或標明目前無產生者）。
4. 決定 `MethodNotFound` / `InvalidParams` / `Unauthorized` 三個零產生者成員的去留——
   **這是設計決定，需使用者裁示**，本計畫先記著不動。

### 階段 2：單一對映登錄

在 `Bee.Api.Core` 新增一份登錄（暫名 `JsonRpcErrorContract`），承載三欄：
`錯誤碼` / `例外型別（可為 null，表示只出不進）` / `重建工廠`。
`MapException` 與 `FinalizeResponse` 都改成查這一份，具名 `if` 收斂成查表。

**必須在這一階段一併處理的不對稱**：`UserMessage` 是**多對一**——伺服端七個型別映到同一個碼，
呼叫端只能重建成 `UserMessageException`。登錄表的形狀要能表達「多個型別 → 一個碼」與
「一個碼 → 一個型別」，不能假設雙射。這是階段 2 唯一有設計難度的地方。

階段 1 的測試在階段 2 之後**不刪**：它守的是「兩端一致」這個性質，
換實作之後性質不變，測試恰好變成該實作的驗收。

---

## 5. 若採行：受影響範圍

### 5.1 檔案

**階段 1**

| 檔案 | 動作 |
|------|------|
| `tests/Bee.Api.Core.UnitTests/ErrorContractDriftTests.cs` | 新增 |
| [`src/Bee.Api.Client/Connectors/ApiConnector.cs`](../../src/Bee.Api.Client/Connectors/ApiConnector.cs) | 補 `ReplayRejected` 分支＋更新 `FinalizeResponse` 的 remarks |
| `tests/Bee.Api.Client.UnitTests/Connectors/ApiConnectorFinalizeResponseTests.cs` | 補一則 `ReplayRejected` 測試 |
| [`docs/jsonrpc-frontend-integration.md`](../jsonrpc-frontend-integration.md) ／ `.zh-TW.md` | 錯誤碼表修正（雙語同步） |

**階段 2**

| 檔案 | 動作 |
|------|------|
| `src/Bee.Api.Core/JsonRpc/JsonRpcErrorContract.cs` | 新增 |
| [`src/Bee.Api.Core/JsonRpc/JsonRpcExecutor.cs`](../../src/Bee.Api.Core/JsonRpc/JsonRpcExecutor.cs) | `MapException` 改查表 |
| `src/Bee.Api.Client/Connectors/ApiConnector.cs` | `FinalizeResponse` 改查表 |
| `src/Bee.Api.Core/PublicAPI.Unshipped.txt` | 登錄型別若為 public 則需申報 |

**兩階段都不動 `src/Bee.Base/Exceptions/` 的任何型別**——這是不採 A 的直接結果，
`Bee.Base` 的 public API surface 因此完全不變。

### 5.2 閘門

| 閘門 | 本計畫的關係 |
|------|------------|
| `BEE9001`（`src/Directory.Build.targets`） | **不觸發**：階段 1、2 都不動 `Bee.Base` / `Bee.Definition` 的參考 |
| `DefinitionDependencyGateTests` | 同上，不受影響 |
| `WireContractDriftTests` | **需注意**：登錄型別若遞移可達 wire 訊息型別，得補 formatter 註冊。目前設計不上 wire，預期不觸發，但階段 2 動筆後要跑一次確認 |
| clean Release build ＋ `TreatWarningsAsErrors` | 一律適用（`PreToolUse` hook 於 commit 前強制） |
| `./check-public-docs.sh` | 階段 1 改公開文件後必跑 |
| CI 資料庫範圍 | 本計畫**不觸及** `Bee.Db` / `Bee.Repository` / SQL 產生邏輯，精簡模式即可；push 前仍依 `rules/testing.md` 問使用者 |

### 5.3 測試補法

- 新測試都是純邏輯，用 `[Fact]` 不用 `[DbFact]`（`rules/testing.md` 第 2 條：純邏輯測試不該跳過）。
- 方法命名 `<方法名>_<情境>_<預期結果>`＋中文 `[DisplayName]`。
- **不得**為了讓 `ErrorContractDriftTests` 轉綠而把零產生者的碼塞進白名單——
  那正是這條測試要防的事。紅了先判「這個碼該不該存在」。

### 5.4 要不要立 ADR

**階段 1 不需要**（修漂移＋加守衛，沒有新決策）。
**階段 2 需要**，理由有二：一是「錯誤契約以單一登錄表達、兩端消費」是長效決策，
外部讀者需要理解「為何這樣設計」；二是本計畫**否決了一個看起來很自然的替代方案（繼承）**，
而否決的理由（§2.1 四份 doc 的共通點是「呼叫端該做什麼」、§2.2 淨省零個 `if`）
不記下來，下一個人會再問一次。

依 `rules/public-docs.md`，該 ADR **不得引用本 plan**——結論要寫進 ADR 本身。

---

## 6. 附記

### 6.1 與連載文章的關係

[`docs/blogs/ithome-2026-ironman/day-18-error-contract.md`](../blogs/ithome-2026-ironman/day-18-error-contract.md)
（2026-09-03 發）描述的正是現行的四個 `if` 與「兩端的對映是一組必須同步、而沒有任何機制保證同步的清單」。
本計畫階段 2 落地後，那段描述會變成**歷史**。

**不修改該文**（已定稿／即將發佈）。另有兩點值得記著：

- 該文寫的是 `MapException` **三**條具名 `if`，現行已是四條（`ReplayRejected` 後加）。
  文章本身就已落後半步，這反而佐證了本計畫要處理的問題。
- 該文預言的失敗樣態——「伺服端多回一個碼，呼叫端沒跟上，那個碼會安靜落到通用分支」——
  在文章發佈前就已經真實發生（§1.6 漂移一）。

### 6.2 明確排除在本計畫之外

- **收斂 `IsUserFacingException` 的六個 BCL 型別**（方案 D）：對的方向，但與本題正交，另案。
- **零產生者錯誤碼的去留**：`MethodNotFound` / `InvalidParams` / `Unauthorized` 三個。
  這是設計決定（要補產生者，還是移除成員），需使用者裁示，階段 1 只負責讓它們被**看見**。
