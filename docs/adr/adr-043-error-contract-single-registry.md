# ADR-043：錯誤契約以單一登錄表達，兩端從同一份宣告消費

## 狀態

**已採納（Accepted，2026-09-02）**

## 背景

一次 API 呼叫失敗時，伺服端把例外映成 JSON-RPC 錯誤碼，呼叫端再把錯誤碼映回例外，
好讓呼叫者能 `catch` 一個型別而不是比對整數。兩個方向互為反函數，
卻分別以一串手寫的 `if` 實作在**兩個組件**裡（`Bee.Api.Core` 的 `JsonRpcExecutor.MapException`
與 `Bee.Api.Client` 的 `ApiConnector.FinalizeResponse`），**沒有任何機制保證兩者同步**。

編譯器不看這件事，測試也測不到：伺服端多認一個碼而呼叫端沒跟上，那個碼會安靜地落到
呼叫端的通用分支，於是該例外型別自己的文件所承諾的 `catch` 永遠不會進去，
而程式編譯得過、既有測試也全綠。

**這不是假想的風險，它已經發生。** [ADR-042](adr-042-api-replay-protection.md) 引入
`ReplayRejectedException` 與錯誤碼 `-32005` 時，伺服端四處丟、也映了碼，呼叫端卻整整少了一條
重建分支。該型別的文件寫著它存在的理由是「讓呼叫端能分辨『重試沒有用』與『憑證被拒』」——
而呼叫端拿到的是 `InvalidOperationException`，這個能力從未真正存在過。同一次盤點另外查出
三筆公開文件的漂移（錯誤碼表缺 `-32005`、`-32001` 的描述與實際不符、
開發限制文件誤稱只有一個碼會被重建）。

四筆缺陷共用同一個形狀：**必須一起改、而沒有任何東西會提醒你一起改**。
依 `single-source` 的判準，這是結構問題而不是紀律問題，靠「記得一起改」不會成立。

## 決策

### 一、對映關係只宣告一次，兩端消費同一份

新增 [`JsonRpcErrorContract`](../../src/Bee.Api.Core/JsonRpc/JsonRpcErrorContract.cs)，
以一張有序表宣告「哪個例外型別走哪個錯誤碼、該碼由誰重建」。伺服端經 `TryGetCode` 查出站碼，
呼叫端經 `TryRebuild` 查回站型別。

**新增一種錯誤現在是一處編輯，而不是兩個組件各一處。** 呼叫端因此完全不需要認識任何具體例外
型別——`ApiConnector` 連 `Bee.Base.Exceptions` 的 `using` 都不再需要。

登錄表放在 `Bee.Api.Core`：`ReplayRejectedException` 本來就在該組件，其餘例外型別在 `Bee.Base`
（其下游），而 `Bee.Api.Client` 相依 `Bee.Api.Core`——這是唯一同時看得到所有參與者的位置。

只有 `TryRebuild` 是公開 API。出站方向目前只有執行器一個消費者，且它與登錄表同組件，
因此維持 `internal`；日後要放寬是加法、不是破壞性變更。

### 二、否決「讓這些例外直接繼承 `UserMessageException`」

提案是把 `CompanyNotEnteredException` 這類型別改為繼承 `UserMessageException`，
以為這樣「比較容易判斷、未來不用寫一堆 `if`」。**實測後否決，理由不只一條，
但最關鍵的是它一個 `if` 都省不掉**：

- `MapException` 的具名分支一條都不能少——每個型別要**不同**的碼，`is 基底` 分不出來。
- 白名單判斷也一個都不減——那四個型別**本來就不在白名單裡**，它們靠排在前面的具名分支攔截。
- 呼叫端是 `int → 型別`，繼承在這個方向上完全用不上：一個整數沒辦法靠 `is` 解析成子類。

代價則是實在的。四份型別文件的共通點不是「訊息能不能顯示」，而是**「呼叫端該做什麼」**
（導向選公司、退回選公司、降級 UI、別再重試）；顯示訊息只是其中一小塊，
拿它當分類軸是選錯了維度。`CompanyNotEnteredException` 的文件更明寫
「it must not be shown to the user verbatim」，與 `UserMessageException` 的
「the message is meant to reach the user as-is」直接相反。

其中對外部使用者傷害最大的一項：框架文件現在教的 catch 順序以
`catch (UserMessageException)` 打頭，繼承之後任何在其後補一條子類 `catch` 的應用程式
會直接 **CS0160 編譯失敗**；沒補的則不會編譯錯，但會開始把上述「不得原樣顯示」的訊息
彈給使用者。

**把這一段記在這裡，是因為它是個看起來很自然的想法。** 不寫下來，下一個人會再問一次，
而「省不掉任何 `if`」這件事只有實際讀過那三處程式碼才看得出來。

### 三、順序敏感性不假裝消除，而是變成可驗證的不變式

型別比對必須用可指派（assignable）而非精確比對，否則
`ArgumentNullException` 無法歸到 `ArgumentException` 之下。可指派比對本質上有序：
基底型別若排在自己的子類之前，會把子類那一列吃掉，使其永遠匹配不到。

因此登錄表**不宣稱自己順序無關**，而是宣告一條不變式——
**衍生型別必須排在其所有基底型別之前**——並由 `ErrorContractDriftTests` 逐對驗證，
違反時指名是哪兩列互相遮蔽。

這一點值得展開：舊實作的順序同樣關鍵（具名分支必須排在白名單之前），
但那是靠註解與記憶維持的。差別不在於是否有順序，而在於**順序錯了會不會有人發現**。

### 四、fallback 不進登錄表

兩件事刻意留在原地：伺服端落到 `InternalError` 時要不要透露原訊息，
是一條讀 `SysInfo.IsDebugMode` 的**資訊揭露政策**；呼叫端通用分支的
`"API error: {code} - {message}"` 是一個**訊息格式**。兩者都不是「型別對應碼」，
放進登錄表只會讓它承載不屬於它的東西。

## 後果

- 新增錯誤型別由兩處編輯降為一處，且漏掉的那一半會被測試指名。
- 錯誤契約成為可檢視的資料，而非散落在兩個組件的控制流程。
- 公開 API 表面增加一個型別與一個方法（`JsonRpcErrorContract.TryRebuild`）。
- 登錄表是**有序**的，這是本決策未消除的複雜度；代價由不變式測試承擔。
- `JsonRpcErrorCode` 有三個成員（`MethodNotFound`、`InvalidParams`、`Unauthorized`）
  **全 repo 零產生者**——`Unauthorized` 尤其容易誤導，認證失敗實際回的是
  `InvalidRequest` 加 HTTP 401。本決策不處理它們的去留，但測試現在強制每個成員都要被歸類，
  因此它們不再是隱形的。
- 收斂 `UserMessage` 那六個過渡期 BCL 例外仍是待辦。它與本決策正交：
  收完之後兩端的分支數不變，因為那六個型別從來就不在造成分歧的那一段裡。
