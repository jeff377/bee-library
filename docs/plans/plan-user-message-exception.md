# 計畫：新增 `UserMessageException` 統一處理使用者訊息傳遞

**狀態：✅ 已完成（2026-05-20）**

## 背景

目前 BO 層要把「顯示給使用者的訊息」往外拋給 Client，做法是借用 BCL 既有例外型別：

```csharp
// src/Bee.Business/System/SystemBusinessObject.cs:142
throw new ArgumentException("CompanyId is required.", nameof(args));

// src/Bee.Business/System/SystemBusinessObject.cs:149
throw new InvalidOperationException("Company access denied.");

// src/Bee.Business/System/SystemBusinessObject.cs:176
throw new UnauthorizedAccessException("Session not found or has expired.");
```

這些訊息能傳到 Client，靠的是 [`JsonRpcExecutor.IsUserFacingException`](../../src/Bee.Api.Core/JsonRpc/JsonRpcExecutor.cs#L190) 的型別白名單 —— 列在白名單裡的例外，`Message` 原樣回 Client；否則一律遮成 `"Internal server error"` 避免內部細節洩漏。

問題在於：

1. **語意錯位**：`InvalidOperationException` 在 BCL 的本意是「物件狀態錯，呼叫不該發生」，`ArgumentException` 是「呼叫端傳錯參數」—— 拿來表達「業務規則違反，請告知使用者」是語意借用，看 code 的人不容易分辨「這個 throw 是 bug 還是設計上要給使用者的訊息」。
2. **白名單模糊**：BCL 例外既出現在「真正的程式錯誤」也出現在「user-facing 訊息」，將來想把這兩類在 logging / 監控上分流時無從下手。
3. **未來擴充無載點**：要加錯誤代碼（i18n key、前端錯誤分流）、改 HTTP status code、加結構化 log level，目前在 BCL 例外上做不到。

## 範圍

**本次涵蓋**：
- 新增 `Bee.Base/Exceptions/` 資料夾（對應 namespace `Bee.Base.Exceptions`），並把既有 `ExceptionExtensions.cs` 一併搬入，讓例外相關型別集中
- 新增 `UserMessageException`（`Bee.Base.Exceptions`）
- 納入 `JsonRpcExecutor.IsUserFacingException` 白名單
- 新增 `JsonRpcErrorCode.UserMessage = -32099`（協定層分類，詳見 §D8）
- 修 `JsonRpcExecutor` 抓例外時依型別寫對應 code（順手修掉現況 hardcoded `-1`，對齊既有 `JsonRpcErrorCode` enum）
- 修 Client 端 `ApiConnector.FinalizeResponse` 依 code 重建例外（`code == UserMessage` → `throw new UserMessageException(message)`，訊息純度不再被前綴污染）
- 對應單元測試（含 Client 端 round-trip）
- 文件更新（`development-constraints.md` / 中文版、CHANGELOG）

**本次不做**（未來另案）：
- **不**加 `UserMessageException.Code` 屬性 —— 詳見 §D3：i18n 機制本身是獨立大議題（含 locale 來源、resource lookup、key 命名等 8 項決策），本次保留向後相容擴充路徑即可
- **不**啟用 `JsonRpcError.Data` —— 已預留為 i18n payload 載點，本次不寫入；詳見 §未來 i18n 落地路徑
- **不**加 `Severity` / 訊息類別 —— 詳見 §D6：拋例外語意上就是錯誤，與訊息類別正交
- **不**批次替換現有 BO 的 `InvalidOperationException` / `ArgumentException` —— 新程式碼用新型別，舊程式碼遇到時順手改即可，避免一次性大改造成 review 風險
- **不**動 `JsonRpcException` —— 它仍負責 API 框架自身的協定錯誤（HTTP status / JSON-RPC error code），與 user-facing message 是不同職責
- **不**立即縮減白名單 —— 詳見 §D7：這是漸進過渡的起點，本次只新增白名單項目，BCL 例外暫保留

## 設計決策

### D1：型別放在 `Bee.Base.Exceptions`（新建資料夾，同時搬遷 `ExceptionExtensions.cs`）

`Bee.Base` 是所有專案（含 `Bee.Business`、`Bee.Api.Core`、`Bee.Api.Client`）的共同相依，新例外放這裡：

- BO 層可 throw
- API server 層（`JsonRpcExecutor`）可在白名單判斷
- Client 端 `ApiConnector` 未來重建例外時也可用同一型別

**資料夾與 namespace 一致性**：依 `code-style.md` 規則，資料夾必須對映 namespace。新建 `src/Bee.Base/Exceptions/` 對應 `namespace Bee.Base.Exceptions`。

**順手搬遷 `ExceptionExtensions.cs`**：既有 [`ExceptionExtensions.cs`](../../src/Bee.Base/ExceptionExtensions.cs) 目前直接放在 `Bee.Base/` 根目錄、namespace 為 `Bee.Base`。本次一併搬入 `Bee.Base/Exceptions/`，namespace 改為 `Bee.Base.Exceptions`，讓例外相關型別集中、未來再加 exception 不必再考慮位置。

**搬遷影響面（4 個檔案需新增 `using Bee.Base.Exceptions;`）**：
- `src/Bee.Business/ExecFuncHandlerExtensions.cs`
- `src/Bee.Api.Core/JsonRpc/JsonRpcExecutor.cs`
- `src/Bee.Api.AspNetCore/Controllers/ApiServiceController.cs`
- `tests/Bee.Base.UnitTests/ExceptionExtensionsTests.cs`

範圍可控，全部僅補 using，無語意變動。

### D2：繼承 `Exception` 而非 `InvalidOperationException` 等 BCL 子類

```csharp
public class UserMessageException : Exception { ... }
```

**Why**：語意獨立。若繼承 `InvalidOperationException`，現有白名單因 `is InvalidOperationException` 仍然攔得到，但 catch 順序會混亂（catch BCL 父類會誤捕新型別）。獨立繼承 `Exception` 讓「使用者訊息」自成一類。

### D3：本次只實作 `Message` + `InnerException`，不加 `Code` / 其他屬性

```csharp
public class UserMessageException : Exception
{
    public UserMessageException(string message) : base(message) { }
    public UserMessageException(string message, Exception inner) : base(message, inner) { }
}
```

**為何本次不加 `Code`**（即使技術上有 `JsonRpcError.Data` 可承載）：

`Code` 屬性的真正用途是 **i18n key**（例如 `"validation.field_required"`），讓 Client 端依使用者 locale 重新 localize。但「能傳輸 Code」只是 i18n 的最末端，i18n 機制本身是個獨立大議題，需要先決定：

1. **i18n key 命名規則**（`Module.Submodule.Action.Reason`？要 framework-wide 統一）
2. **Resource bundle 機制**（.resx / JSON / YAML？放哪個專案？怎麼分模組？）
3. **Locale 來源**：server-side 從 `Session.UserId` 查？client-side 自己決定？兩者不一致誰贏？
4. **誰負責 localize**：server 端 localize（訊息已是目標語言）vs client 端 localize（server 只給 key + args）—— 取捨重大
5. **訊息模板插值**（`"Field {0} is required"` + args 怎麼傳）
6. **Fallback 策略**（key 找不到時的行為）
7. **Resource 維護流程**（誰寫、翻譯流程、版本控制）
8. **支援的 locale 列表**（哪個是 default？）

把 `UserMessageException.Code` 屬性塞進本次 plan，等於本次變成「i18n 啟動工程 + 附帶 UserMessageException」—— 主題位移。

本次保持單純：**只引入型別、白名單、協定層 code 分流**，i18n 機制完整留待獨立 plan。`Code` 屬性是**向後相容**變更（加 nullable 屬性不會破壞既有 client），未來啟動 i18n 時可安心擴充。

**為何其他屬性都不加**（YAGNI）：
- `Details`（補充說明）：實務上 `Message` 已足夠
- `LogLevel`：本框架 logging 機制（[`Bee.Base.Tracing`](../../src/Bee.Base/Tracing/Tracer.cs)）非 `Microsoft.Extensions.Logging`，現在塞會混淆
- `WithData` fluent API：訊息模板插值是 i18n 進階場景

> 未來需要更多屬性 / 行為時，**有兩條路**：(1) 直接在本類加 nullable 屬性（向後相容）；(2) 繼承出子類別（如 `NotFoundUserMessageException`、`ValidationUserMessageException`）。設計上保留兩條路都通。

### D4：不做階層、不引入 marker interface

ABP 有 `IUserFriendlyException` / `IBusinessException` 兩個 marker interface 讓使用者自訂例外能被框架辨識；本次**不採用**。

**Why**：
- `JsonRpcExecutor` 是框架內部唯一攔截點，沒有「使用者自訂例外要被框架識別」的情境
- 引入 interface 增加抽象成本，目前用單一具體型別即可
- 未來真的需要分階層（如 `NotFoundException` 對應 HTTP 404），再抽 `UserMessageException` 為基底、加 interface

### D5：命名選用 `UserMessageException` 而非 `UserFriendlyException`

- `UserMessageException` 描述「載荷是訊息」，更精準
- `UserFriendlyException`（ABP 命名）語意較含糊（什麼叫 friendly？）
- 不採 `BusinessException` / `DomainException`：這兩個名稱在 DDD 圈有其他預期語意（領域規則、聚合不變式），與本框架不完全契合

### D6：不加 `Severity` / 訊息類別 —— 拋例外就是錯誤

考慮過是否在 `UserMessageException` 加 `Severity` 屬性（Info / Warning / Error），結論是**不加**。

**理由 1：例外語意上等於「流程中斷」，與訊息類別正交**

`throw` 之後呼叫端後續邏輯不執行 —— 這在語意上等於「操作失敗、無法完成」。UI 上的訊息類別則是另一個維度：

| 訊息類別（UI 視角） | 對應的程式行為 |
|---|---|
| 提示「已成功儲存」 | 流程**完成**，正常 return（可帶訊息） |
| 警告「庫存不足，是否繼續？」 | 流程**未完成**，需 user interaction → 兩段式呼叫 / result type |
| 警告「資料已建立，但某項預設值被自動套用」 | 流程**完成**，正常 return（result 帶 warnings 陣列） |
| 錯誤「欄位不能為空」 | 流程**中斷** → 例外 |

只有最後一種該走例外。其他三種若走例外，會出現「成功路徑被 try-catch 接住」的反模式。

**理由 2：業界佐證**

- ABP `UserFriendlyException` 沒有 severity 屬性
- ABP `BusinessException` 有 `LogLevel`，但作用是決定**伺服器端 log 等級**，不是 UI 呈現
- ASP.NET Core `ProblemDetails` 也沒有 severity 概念

**理由 3：避免 UI 耦合滲透到 server**

`Severity` 屬性 = server 替 UI 決定呈現樣式。當 UI 改版（Toast vs Modal vs 紅黃綠 banner）時，server 端例外型別要跟著動，是不健康的依賴方向。讓 server 只回傳事實層（「操作失敗，原因 X」），Client 自行決定呈現，才是乾淨分層。

**真有需求時的替代路徑**

未來若前端要根據錯誤類型顯示不同樣式：

- **子類別**：`ValidationFailureException` / `NotFoundException` / `PermissionDeniedException` 繼承 `UserMessageException`，Client 依型別決定樣式
- **`Code` 屬性**：前端 code → icon / 樣式對映（與 §D3 一致：與 i18n 一起整體設計時再加）

兩條路都是向後相容變更，本次不預先設計。

### D7：白名單長期策略 —— 漸進過渡，最終縮減

當前 `IsUserFacingException` 白名單同時涵蓋 `UserMessageException` 與 BCL 例外（`ArgumentException` / `InvalidOperationException` / `UnauthorizedAccessException` / `NotSupportedException` / `FormatException`）。長期目標是讓 `UserMessageException` 成為**唯一** user-facing 通道，把 BCL 例外踢回它們在 BCL 的本意，避免兩類訊息混在同一白名單裡無法在 logging / 監控分流。

**過渡步驟**（不在本次完成）：

1. （本次）引入 `UserMessageException` 並納入白名單，文件標示為「預設選項」
2. 新程式碼一律用 `UserMessageException`；現有 BO 改動時順手把 BCL 例外換掉
3. 視覆寫進度，逐步把 BCL 例外從白名單踢出（每踢一個會把對應 BCL 例外原樣訊息改為遮蔽，需先確認該型別在現存 BO 已無 user-facing 用途）
4. 終態：白名單只剩 `UserMessageException` 與 `JsonRpcException`（前者承載業務訊息、後者承載 API 協定錯誤）

**為何不一次完成**：批次替換 BO 層 throw 點需逐一檢視「這個 throw 是真的程式錯誤還是要給使用者看」，誤判風險高；分階段、由實際接觸 BO 的 PR 自然推進，比較安全。

**何時觸發白名單縮減**：實際上，當某個 BCL 例外（例如 `FormatException`）在 prod BO 已 0 處 user-facing 使用時，即可考慮從白名單移除。這需要獨立 plan 評估，不在本次範圍。

### D8：協定層 code 分流 —— 業務訊息與系統錯誤拆開傳

**問題現況**：[`JsonRpcExecutor.cs:105`](../../src/Bee.Api.Core/JsonRpc/JsonRpcExecutor.cs#L105) 抓 BO 例外時 hardcoded `new JsonRpcError(-1, message)`，完全沒對齊既有的 [`JsonRpcErrorCode`](../../src/Bee.Api.Core/JsonRpc/JsonRpcErrorCode.cs) enum（已有 `InternalError = -32000`、`Unauthorized = -32001`、`CompanyNotEntered = -32002`、`CompanyAccessDenied = -32003`）。結果：

- 業務訊息（白名單命中）與系統錯誤（白名單外）**共用 `code = -1`**，Client 無法區分
- `JsonRpcErrorCode` enum 形同虛設

**解決方案**：

```csharp
// JsonRpcErrorCode.cs 新增
UserMessage = -32099,  // 通用業務訊息容器

// JsonRpcExecutor.cs ExecuteAsyncCore catch 區塊
catch (Exception ex)
{
    var rootEx = ex.Unwrap();
    var (code, message) = MapException(rootEx);
    response.Error = new JsonRpcError((int)code, message);
    Tracer.End(ctx, TraceStatus.Error, rootEx.Message);
}

private static (JsonRpcErrorCode code, string message) MapException(Exception ex)
{
    if (ex is UserMessageException)
        return (JsonRpcErrorCode.UserMessage, ex.Message);
    if (IsUserFacingException(ex))
        return (JsonRpcErrorCode.UserMessage, ex.Message);  // 過渡期：白名單 BCL 例外暫歸 UserMessage
    return (JsonRpcErrorCode.InternalError, "Internal server error");
}
```

**為何選 `-32099`**：

| 數值 | 名稱 | 性質 |
|------|------|------|
| -32000 | InternalError | 系統錯誤 |
| -32001 | Unauthorized | 認證 |
| -32002 | CompanyNotEntered | 業務狀態（具體分類） |
| -32003 | CompanyAccessDenied | 業務權限（具體分類） |
| -32099 | **UserMessage**（新加） | **通用業務訊息容器** |

`-32099` 在語意上像 "catch-all 業務訊息"，視覺上與系統錯誤拉開距離，並預留中間給未來特定業務 code（如 `-32004`、`-32005`…）。

**對應 Client 端改造**（`ApiConnector.FinalizeResponse`）：

```csharp
if (response.Error != null)
{
    var code = (JsonRpcErrorCode)response.Error.Code;
    if (code == JsonRpcErrorCode.UserMessage)
        throw new UserMessageException(response.Error.Message);  // 純訊息、純型別
    throw new InvalidOperationException(
        $"API error: {response.Error.Code} - {response.Error.Message}");
}
```

效果：
- Client 端 `catch UserMessageException` 即可精準接住業務訊息
- 訊息純度恢復（不再被 `"API error: -1 - "` 前綴污染）
- 系統錯誤仍走 `InvalidOperationException` 包裝（保留呼叫端原本的 catch 邏輯）

**為何協定層 code 不能用來傳 i18n key**：見 §未來 i18n 落地路徑。`JsonRpcErrorCode` 是**封閉、有限、框架擁有**的 enum；i18n key 是**開放、無限、業務擁有**的字串。兩者是不同維度，必須分通道。

## 未來 i18n 落地路徑

本次 plan **不啟動 i18n 機制**，但設計上預留擴充路徑，未來啟動時不必破壞既有 client。

### 未來的傳輸形狀

```json
{
  "jsonrpc": "2.0",
  "id": "...",
  "error": {
    "code": -32099,                              // 協定層分類（本次落地）
    "message": "欄位 email 不能為空",              // server 端 fallback localized 訊息（本次落地）
    "data": {                                    // ← i18n payload 進這裡（本次預留、不寫入）
      "messageKey": "validation.field_required",
      "args": { "fieldName": "email" }
    }
  }
}
```

### 兩個維度、兩個通道

| 維度 | 用途 | 性質 | JSON 欄位 | 擁有者 |
|------|------|------|-----------|--------|
| **協定層分類** | 「沒成功的類別」 | 封閉、有限 | `code` | 框架（`JsonRpcErrorCode`） |
| **業務層 i18n key** | 「具體是哪個訊息」 | 開放、無限 | `data.messageKey` | 業務層 |

若把 i18n key 塞進 `JsonRpcErrorCode` enum，會導致：
- 業務細節滲透到框架層（層級反轉）
- enum 無限膨脹（每加一個業務訊息都要改框架）
- 跨業務 repo 無法共用框架

### 未來實作時的接點

i18n 落地時，需同步改：

| 改動點 | 內容 |
|--------|------|
| `UserMessageException` | 加 `Code` (string?) 與 `Args` (IReadOnlyDictionary<string, object?>?) nullable 屬性 |
| `JsonRpcExecutor.MapException` | 把 `Code` / `Args` 寫進 `JsonRpcError.Data` |
| `ApiConnector.FinalizeResponse` | 從 `Data` 解析 → 重建 `UserMessageException` 的完整資訊 |
| 新增 i18n 機制本體 | locale 來源、resource bundle、key 命名、模板插值、fallback、誰負責 localize ……（見 §D3 列出的 8 項決策） |

本次 plan 已為前三項預備好擴充點（向後相容），落地時只需新增屬性與寫入邏輯，不破壞既有 client。

## 影響的程式

### 新增

| 路徑 | 內容 |
|------|------|
| `src/Bee.Base/Exceptions/UserMessageException.cs` | 例外型別本體（`namespace Bee.Base.Exceptions`） |
| `tests/Bee.Base.UnitTests/Exceptions/UserMessageExceptionTests.cs` | ctor 重載、`Message`、`InnerException` 驗證 |
| `tests/Bee.Api.Core.UnitTests/JsonRpc/JsonRpcExecutorUserMessageExceptionTests.cs` | BO 拋 `UserMessageException` → `Response.Error.Message` 為原訊息；非白名單例外仍被遮成 `"Internal server error"`（迴歸） |

### 搬遷（檔案搬位 + namespace 變更）

| 路徑變動 | namespace 變動 |
|----------|----------------|
| `src/Bee.Base/ExceptionExtensions.cs` → `src/Bee.Base/Exceptions/ExceptionExtensions.cs` | `Bee.Base` → `Bee.Base.Exceptions` |

連帶需補 `using Bee.Base.Exceptions;` 的呼叫端（共 4 個檔案）：

| 路徑 | 用途 |
|------|------|
| `src/Bee.Business/ExecFuncHandlerExtensions.cs` | `ex.Unwrap()` 呼叫 |
| `src/Bee.Api.Core/JsonRpc/JsonRpcExecutor.cs` | `ex.Unwrap()` 呼叫（同時加白名單條件） |
| `src/Bee.Api.AspNetCore/Controllers/ApiServiceController.cs` | `ex.Unwrap()` 呼叫 |
| `tests/Bee.Base.UnitTests/ExceptionExtensionsTests.cs` | 測試 `ExceptionExtensions.Unwrap` |

### 修改

| 路徑 | 改動 |
|------|------|
| `src/Bee.Api.Core/JsonRpc/JsonRpcErrorCode.cs` | 新增 `UserMessage = -32099`（見 §D8） |
| `src/Bee.Api.Core/JsonRpc/JsonRpcExecutor.cs` | (1) 白名單加 `ex is UserMessageException`；(2) `catch` 區塊抽 `MapException` helper，依例外型別寫對應 code（見 §D8），順手修掉 hardcoded `-1`；(3) 補 `using Bee.Base.Exceptions;`（搬遷連帶） |
| `src/Bee.Api.Client/Connectors/ApiConnector.cs` `FinalizeResponse` | 依 `JsonRpcError.Code` 判斷：`UserMessage` 拋 `UserMessageException(message)`、其他維持 `InvalidOperationException` 包裝（見 §D8） |
| `docs/development-constraints.zh-TW.md` | 「例外處理規則」段：白名單清單加入 `UserMessageException`；新增「使用時機」表與「演進方向」說明（見下方文件草案） |
| `docs/development-constraints.md` | 同步英文版 |

**CHANGELOG 不在本次處理**：CHANGELOG 採發佈套件新版本時統整大綱的策略，不在每個 plan 完成時逐項追加，避免條目過於雜亂。本次的協定層異動會在下一次發佈前的 changelog 整理（`/changelog-draft` skill）中以大綱形式收錄。

### 不動

- BO 層既有 `throw new InvalidOperationException(...)` / `throw new ArgumentException(...)` —— 留給未來修改該檔案時順手換（漸進過渡，見 §D7）
- `JsonRpcException` —— 它仍負責 API 框架自身的協定錯誤（HTTP status / JSON-RPC error code），與 user-facing message 是不同職責
- `JsonRpcError.Data` —— 已預留為 i18n payload 載點，本次不寫入（見 §未來 i18n 落地路徑）
- 其他白名單例外（`UnauthorizedAccessException`、`FormatException`、`NotSupportedException`）—— 本次保留，長期會視 BO 替換進度逐步縮減（見 §D7）

## 文件草案：`development-constraints.zh-TW.md`「例外處理規則」段

更新後內容（節錄）：

```markdown
## 例外處理規則

### Client 可見的例外類型

`JsonRpcExecutor` 僅將以下例外類型原樣回傳給 Client：

- `UserMessageException`（**預設選項**，新增）
- `UnauthorizedAccessException`
- `ArgumentException`（含 `ArgumentNullException`、`ArgumentOutOfRangeException`）
- `InvalidOperationException`
- `NotSupportedException`
- `FormatException`
- `JsonRpcException`

其他所有例外在正式環境一律轉為 `"Internal server error"`，開發環境（`IsDevelopment`）會回傳完整錯誤訊息。

### 使用時機

| 例外型別 | 使用情境 |
|----------|----------|
| `UserMessageException` | **預設選項**：任何要顯示給使用者看的訊息（業務規則違反、驗證失敗、流程中斷） |
| `ArgumentException` | API contract 違反 —— 呼叫端傳錯參數（null、格式錯、超出範圍）。**注意**：白名單暫保留，新程式碼請優先用 `UserMessageException` |
| `InvalidOperationException` | 物件狀態錯誤、操作時機不對。**注意**：白名單暫保留，新程式碼請優先用 `UserMessageException` |
| `UnauthorizedAccessException` | 認證／授權失敗 |
| `NotSupportedException` | 功能未實作或不支援當前情境 |
| `FormatException` | 字串／資料格式無法解析 |
| `JsonRpcException` | API 框架自身的協定錯誤（HTTP status / JSON-RPC error code） |

### 演進方向

長期目標是讓 `UserMessageException` 成為 user-facing 訊息的**唯一**通道，把 BCL 例外回歸 BCL 本意（呼叫錯誤、狀態錯誤、程式 bug）。白名單目前保留 BCL 例外是為了**漸進過渡**：

- **新程式碼**：一律用 `UserMessageException` 拋送使用者訊息
- **舊程式碼**：遇到時順手把 `InvalidOperationException("xxx")`／`ArgumentException("xxx")` 改成 `UserMessageException("xxx")`
- **白名單縮減**：當某個 BCL 例外在 prod BO 已 0 處 user-facing 使用時，由獨立 plan 評估從白名單移除
- **終態**：白名單只剩 `UserMessageException` 與 `JsonRpcException`

### 擴充方式

- 需要更多屬性（如 `Code`、`Details`、結構化資料）：直接在 `UserMessageException` 加 nullable 屬性（向後相容）
- 需要分類錯誤（如 `NotFoundException` 對應 HTTP 404）：繼承 `UserMessageException` 拆子類別

### 設計意圖

- 防止內部實作細節洩漏給 Client
- 為業務訊息建立獨立通道，與「真程式錯誤」在型別上明確區隔，方便未來 logging／監控分流
```

## 測試計畫

### `UserMessageExceptionTests.cs`

| 測試方法 | 驗證 |
|----------|------|
| `Ctor_WithMessage_SetsMessage` | 單參數 ctor 設定 `Message`、`InnerException` 為 null |
| `Ctor_WithMessageAndInner_SetsBoth` | `Message` 與 `InnerException` 都正確設定 |
| `Throw_CanBeCaughtAsException` | 可被 `catch (Exception)` 接住（驗證繼承關係） |
| `Throw_NotCaughtAsBclException` | **不**被 `catch (InvalidOperationException)` / `catch (ArgumentException)` 接住（驗證 D2 的型別獨立性） |

### `JsonRpcExecutorUserMessageExceptionTests.cs`

| 測試方法 | 驗證 |
|----------|------|
| `Execute_BoThrowsUserMessageException_ReturnsUserMessageCode` | BO 拋 `UserMessageException("test message")` → `Response.Error.Code == (int)JsonRpcErrorCode.UserMessage`、`Response.Error.Message == "test message"` |
| `Execute_BoThrowsBclWhitelistException_ReturnsUserMessageCode` | 過渡期：BO 拋 `InvalidOperationException("test")` → 仍歸 `UserMessage` code（白名單命中） |
| `Execute_BoThrowsUnknownException_ReturnsInternalErrorCode` | 迴歸：BO 拋 `Exception` → `Code == InternalError`、`Message == "Internal server error"` |

### `ApiConnectorFinalizeResponseTests.cs`（新增）

| 測試方法 | 驗證 |
|----------|------|
| `FinalizeResponse_UserMessageCode_ThrowsUserMessageException` | `Response.Error = { Code = -32099, Message = "test" }` → 拋 `UserMessageException`，且 `ex.Message == "test"`（純訊息、無前綴） |
| `FinalizeResponse_InternalErrorCode_ThrowsInvalidOperationException` | `Response.Error = { Code = -32000, Message = "Internal server error" }` → 仍拋 `InvalidOperationException`，訊息含 `"API error: ..."` 前綴（迴歸） |
| `FinalizeResponse_NoError_ReturnsValue` | `Response.Error == null` → 正常回傳 result value |

> Server 端測試需要能模擬 BO 拋例外的設置；如該專案已有類似 fixture（如 mock business object），沿用；否則新增最小可行的 test BO。實作時若發現現有 fixture 結構不適合，可改為純單元測試 `MapException` helper（透過 `InternalsVisibleTo` 或拆獨立靜態類），於實作階段決定。
>
> Client 端測試直接組裝 `JsonRpcResponse` 模擬不同 `Error.Code` 情境，不需要真的網路呼叫。

## 風險與緩解

| 風險 | 緩解 |
|------|------|
| 白名單加新型別後，是否影響既有測試？ | 既有測試針對既有型別不應受影響；CI 全跑驗證 |
| BO 開發者繼續用 `InvalidOperationException` 不改 | 屬可接受 —— 兩者都能傳訊息，新型別只是更佳實踐，文件提示後逐步演化（見 §D7） |
| **JsonRpcExecutor 從 hardcoded `-1` 改為依型別寫 code，可能影響 Client 既有 catch 邏輯** | 既有 Client 若依 `code == -1` 判斷錯誤型別會壞；但目前 `ApiConnector` 不檢查 code（僅檢查 `Error != null`），影響面有限。CI 全跑驗證；CHANGELOG 標 breaking change |
| **`ApiConnector.FinalizeResponse` 改為「`UserMessage` code 拋 `UserMessageException`、其他維持 `InvalidOperationException`」—— Client 端 catch 行為變更** | 既有 Client 若 `catch (InvalidOperationException)` 接所有錯誤，會漏接 `UserMessageException`；但 `UserMessageException : Exception`，`catch (Exception)` 仍接得到。CHANGELOG 標 breaking change，並建議 Client 改 `catch (UserMessageException)` + `catch (Exception)` 分流 |
| **`JsonRpcErrorCode.UserMessage = -32099` 數值不可逆** | 一旦發佈套件，既有 Client 會固定解析此值；本次定案後不得修改。版本控制由 NuGet 套件版本機制保障 |
| `JsonRpcError.Data` 預留為 i18n 載點，未來啟動 i18n 時可能發現此設計不足 | `Data` 是 `object?`，結構上完全開放；未來 i18n payload 形狀可在 i18n plan 階段重新設計，不受本次限制 |

## 變更規模

- **新增**：
  - `UserMessageException` production class（< 20 行，2 個 ctor）
  - `JsonRpcErrorCode.UserMessage = -32099`（1 行 enum 值）
  - 3 個測試檔（`UserMessageExceptionTests`、`JsonRpcExecutorUserMessageExceptionTests`、`ApiConnectorFinalizeResponseTests`）
- **搬遷**：`ExceptionExtensions.cs`（檔案位置 + namespace），連帶 4 個檔案補 `using`
- **修改**：
  - `JsonRpcExecutor.cs`：白名單條件 + 抽 `MapException` helper 取代 hardcoded `-1`
  - `ApiConnector.FinalizeResponse`：依 code 重建例外
  - 2 個文件（development-constraints x 2；CHANGELOG 留待發佈時統整）
- **預估 commit 數**：2–3 個建議拆法：
  1. **C1**：搬遷 `ExceptionExtensions.cs` 至 `Bee.Base.Exceptions` namespace（4 個檔案補 using）
  2. **C2**：新增 `UserMessageException` + `JsonRpcErrorCode.UserMessage` + `JsonRpcExecutor.MapException`（含順手修 hardcoded -1）+ 白名單條件 + server 端測試
  3. **C3**：改 `ApiConnector.FinalizeResponse` + Client 端 round-trip 測試 + 文件更新
- **不可逆 protocol 變更**：`JsonRpcErrorCode.UserMessage = -32099` 一旦發佈，未來不能改數值（既有 Client 會解析錯誤）—— 本次選定後即定案


