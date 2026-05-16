# 計畫：新增 `UserMessageException` 統一處理使用者訊息傳遞

**狀態：📝 擬定中**

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
- 新增 `UserMessageException`（`Bee.Base`），帶可選 `Code` 屬性
- 納入 `JsonRpcExecutor.IsUserFacingException` 白名單
- 對應單元測試
- 文件更新（`development-constraints.md` / 中文版、CHANGELOG）

**本次不做**（未來另案）：
- **不**批次替換現有 BO 的 `InvalidOperationException` / `ArgumentException` —— 新程式碼用新型別，舊程式碼遇到時順手改即可，避免一次性大改造成 review 風險
- **不**動 `JsonRpcException` —— 它仍負責 API 框架自身的協定錯誤（HTTP status / JSON-RPC error code），與 user-facing message 是不同職責
- **不**改 `JsonRpcExecutor` 既有的「白名單遮蔽」策略 —— 只是新增一個白名單項目
- **不**改 Client 端 `ApiConnector.FinalizeResponse` —— 目前統一包成 `InvalidOperationException`，要讓 Client 精準 `catch UserMessageException` 需要在 `JsonRpcError` 帶型別資訊，屬獨立議題
- **不**做 i18n 機制本身 —— `Code` 屬性只是預留載點，實際的 resource lookup / locale 對應留待後續需要時再設計

## 設計決策

### D1：型別放在 `Bee.Base`

`Bee.Base` 是所有專案（含 `Bee.Business`、`Bee.Api.Core`、`Bee.Api.Client`）的共同相依，新例外放這裡：

- BO 層可 throw
- API server 層（`JsonRpcExecutor`）可在白名單判斷
- Client 端 `ApiConnector` 未來重建例外時也可用同一型別

**Why**：與 [`ExceptionExtensions.Unwrap`](../../src/Bee.Base/ExceptionExtensions.cs#L20) 同位置，例外相關擴充集中於 `Bee.Base/Exceptions/`（新增此目錄）。

### D2：繼承 `Exception` 而非 `InvalidOperationException` 等 BCL 子類

```csharp
public class UserMessageException : Exception { ... }
```

**Why**：語意獨立。若繼承 `InvalidOperationException`，現有白名單因 `is InvalidOperationException` 仍然攔得到，但 catch 順序會混亂（catch BCL 父類會誤捕新型別）。獨立繼承 `Exception` 讓「使用者訊息」自成一類。

### D3：加 `Code` 屬性、不加其他

```csharp
public class UserMessageException : Exception
{
    public UserMessageException(string message) : base(message) { }
    public UserMessageException(string message, string? code) : base(message) => Code = code;
    public UserMessageException(string message, Exception inner) : base(message, inner) { }

    public string? Code { get; }
}
```

**為何加 `Code`**：
- 成本極低（一個 nullable 字串屬性、一個 ctor 重載）
- 未來做 i18n / 前端錯誤分流時不用改 ctor 簽章（破壞性變更成本高）
- 對齊 [ABP `BusinessException`](https://github.com/abpframework/abp/blob/dev/framework/src/Volo.Abp.Core/Volo/Abp/BusinessException.cs) 的 `Code` 概念，業界辨識度高

**為何其他屬性都不加**（YAGNI）：
- `Details`（補充說明）：實務上 `Message` 已足夠，未來真有需求再加
- `LogLevel`：本框架 logging 機制（[`Bee.Base.Tracing`](../../src/Bee.Base/Tracing/Tracer.cs)）非 `Microsoft.Extensions.Logging`，現在塞會混淆；等真的接入結構化 logging 再說
- `WithData` fluent API：訊息模板插值是 i18n 進階場景，本次不做

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

## 影響的程式

### 新增

| 路徑 | 內容 |
|------|------|
| `src/Bee.Base/Exceptions/UserMessageException.cs` | 例外型別本體 |
| `tests/Bee.Base.UnitTests/Exceptions/UserMessageExceptionTests.cs` | ctor 重載、`Message`、`Code`、`InnerException` 驗證 |
| `tests/Bee.Api.Core.UnitTests/JsonRpc/JsonRpcExecutorUserMessageExceptionTests.cs` | BO 拋 `UserMessageException` → `Response.Error.Message` 為原訊息；非白名單例外仍被遮成 `"Internal server error"`（迴歸） |

### 修改

| 路徑 | 改動 |
|------|------|
| `src/Bee.Api.Core/JsonRpc/JsonRpcExecutor.cs` `IsUserFacingException`（第 190 行附近）| 白名單加 `ex is UserMessageException`；新增 `using Bee.Base.Exceptions;` |
| `docs/development-constraints.zh-TW.md` | 「例外處理規則」段：白名單清單加入 `UserMessageException`；新增「使用時機」表（見下方文件草案）；註明擴充方式 |
| `docs/development-constraints.md` | 同步英文版 |
| `CHANGELOG.zh-TW.md` | Unreleased 區記錄：新增 `UserMessageException`、納入白名單 |
| `CHANGELOG.md` | 同步英文版 |

### 不動

- BO 層既有 `throw new InvalidOperationException(...)` / `throw new ArgumentException(...)` —— 留給未來修改該檔案時順手換
- `JsonRpcException`、`Bee.Api.Client.ApiConnector`、`JsonRpcError` —— 屬 future work
- 其他白名單例外（`UnauthorizedAccessException`、`FormatException`、`NotSupportedException`）—— 仍保留，各自有合理使用情境

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
| `ArgumentException` | API contract 違反 —— 呼叫端傳錯參數（null、格式錯、超出範圍） |
| `InvalidOperationException` | 物件狀態錯誤、操作時機不對（例如未登入就操作） |
| `UnauthorizedAccessException` | 認證／授權失敗 |
| `NotSupportedException` | 功能未實作或不支援當前情境 |
| `FormatException` | 字串／資料格式無法解析 |
| `JsonRpcException` | API 框架自身的協定錯誤（HTTP status / JSON-RPC error code） |

### 擴充方式

- 需要更多屬性（如 `Details`、結構化資料）：直接在 `UserMessageException` 加 nullable 屬性
- 需要分類錯誤（如 `NotFoundException` 對應 HTTP 404）：繼承 `UserMessageException` 拆子類別

### 設計意圖

- 防止內部實作細節洩漏給 Client
- `UserMessageException` 帶 `Code` 屬性，預留 i18n / 前端錯誤分流的擴充點
```

## 測試計畫

### `UserMessageExceptionTests.cs`

| 測試方法 | 驗證 |
|----------|------|
| `Ctor_WithMessage_SetsMessage` | 單參數 ctor 設定 `Message`，`Code` 為 null、`InnerException` 為 null |
| `Ctor_WithMessageAndCode_SetsBoth` | `Message` 與 `Code` 都正確設定 |
| `Ctor_WithMessageAndNullCode_AllowsNull` | `Code` 顯式傳 null 不拋例外 |
| `Ctor_WithMessageAndInner_SetsInnerException` | `InnerException` 正確設定 |
| `Throw_CanBeCaughtAsException` | 可被 `catch (Exception)` 接住（驗證繼承關係） |

### `JsonRpcExecutorUserMessageExceptionTests.cs`

| 測試方法 | 驗證 |
|----------|------|
| `Execute_BoThrowsUserMessageException_ReturnsOriginalMessage` | BO 方法拋 `UserMessageException("test message")` → `Response.Error.Message == "test message"` |
| `Execute_BoThrowsUnknownException_ReturnsInternalServerError` | 迴歸：BO 拋 `Exception` 仍被遮成 `"Internal server error"` |

> 第二個測試專案的測試需要能模擬 BO 拋例外的設置；如該專案已有類似 fixture（如 mock business object），沿用；否則新增最小可行的 test BO。實作時若發現現有 fixture 結構不適合，可改為純單元測試 `IsUserFacingException` 私有方法（透過 `InternalsVisibleTo` 或拆 helper），於實作階段決定。

## 風險與緩解

| 風險 | 緩解 |
|------|------|
| 白名單加新型別後，是否影響既有測試？ | 既有測試針對既有型別不應受影響；CI 全跑驗證 |
| BO 開發者繼續用 `InvalidOperationException` 不改 | 屬可接受 —— 兩者都能傳訊息，新型別只是更佳實踐，文件提示後逐步演化 |
| Client 端 `ApiConnector.FinalizeResponse` 一律包成 `InvalidOperationException`，新型別在 Client 端「看起來」沒差 | 屬已知限制，列為 future work；本次重點是 server 端語意清晰 |

## 變更規模

- 新增：1 個 production class（< 30 行）、2 個測試檔
- 修改：1 個白名單條件、4 個文件
- 預估 commit 數：1–2 個

## 交付分支

- 開發分支：`claude/error-message-propagation-8Bhj3`
- 最終推送：`main`（依使用者指示）
