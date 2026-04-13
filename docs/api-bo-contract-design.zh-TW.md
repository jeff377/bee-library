# API 合約與 BO 參數設計原則

[English](api-bo-contract-design.md)

本文件說明 Bee.NET 框架中 API 合約（Request / Response）與 BO 參數（Args / Result）的設計架構與使用方式，供開發人員在擴充 API 方法或撰寫 BO 邏輯時參考。

---

## 核心觀念

框架將 API 傳輸層與 BO 業務邏輯層的參數型別分開管理，透過**合約介面**統一定義屬性，確保兩層各自獨立、關注點分離。

```
合約介面（ILoginRequest / ILoginResponse）   ← 定義共用屬性，唯一真實來源
     │
     ├── API 型別（LoginRequest / LoginResponse）  ← 含序列化標記，用於 API 傳輸
     │
     └── BO 型別（LoginArgs / LoginResult）        ← 純 POCO，用於業務邏輯
```

**為什麼要分層？**

- 用戶端（`Bee.Api.Client`）只接觸 API 型別，不需知道 BO 實作細節
- BO 層不依賴 API 組件，可獨立測試與演進
- BO 可在合約之外新增內部專用屬性，不影響 API 合約

---

## 型別總覽

### 合約介面（Bee.Api.Contracts）

定義 API 方法輸入與輸出的屬性合約，只包含唯讀屬性，不含任何序列化標記。

```csharp
namespace Bee.Api.Contracts
{
    public interface ILoginRequest
    {
        string UserId { get; }
        string Password { get; }
        string ClientPublicKey { get; }
    }

    public interface ILoginResponse
    {
        Guid AccessToken { get; }
        DateTime ExpiredAt { get; }
        string ApiEncryptionKey { get; }
        string UserId { get; }
        string UserName { get; }
    }
}
```

### API 合約型別（Bee.Api.Core.System）

繼承 `ApiRequest` / `ApiResponse`，實作合約介面，標記 MessagePack 序列化屬性。用戶端透過這些型別發送請求與接收回應。

```csharp
[MessagePackObject]
[Serializable]
public class LoginRequest : ApiRequest, ILoginRequest
{
    [Key(100)] public string UserId { get; set; } = string.Empty;
    [Key(101)] public string Password { get; set; } = string.Empty;
    [Key(102)] public string ClientPublicKey { get; set; } = string.Empty;
}

[MessagePackObject]
[Serializable]
public class LoginResponse : ApiResponse, ILoginResponse
{
    [Key(100)] public Guid AccessToken { get; set; } = Guid.Empty;
    [Key(101)] public DateTime ExpiredAt { get; set; }
    [Key(102)] public string ApiEncryptionKey { get; set; } = string.Empty;
    [Key(103)] public string UserId { get; set; } = string.Empty;
    [Key(104)] public string UserName { get; set; } = string.Empty;
}
```

### BO 參數型別（Bee.Business）

繼承 `BusinessArgs` / `BusinessResult`，實作合約介面，純 POCO。可在合約屬性之外新增 BO 專用屬性。

```csharp
public class LoginArgs : BusinessArgs, ILoginRequest
{
    public string UserId { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ClientPublicKey { get; set; } = string.Empty;

    // BO 專用屬性（不在合約介面中）
    public bool IsAutoLogin { get; set; }
}
```

---

## 命名規則

| 用途 | 命名模式 | 範例 | 所在組件 |
|------|----------|------|----------|
| 合約介面（輸入） | `IXxxRequest` | `ILoginRequest` | Bee.Api.Contracts |
| 合約介面（輸出） | `IXxxResponse` | `ILoginResponse` | Bee.Api.Contracts |
| API 輸入 | `XxxRequest` | `LoginRequest` | Bee.Api.Core |
| API 輸出 | `XxxResponse` | `LoginResponse` | Bee.Api.Core |
| BO 輸入 | `XxxArgs` | `LoginArgs` | Bee.Business |
| BO 輸出 | `XxxResult` | `LoginResult` | Bee.Business |

---

## 三種使用情境

### 情境一：API 方法，BO 不需額外屬性（最常見）

API 與 BO 的參數屬性完全相同，不需要建立 Args / Result 型別。

**需要建立的型別：** 合約介面 + API 合約型別
**BO 方法簽章：** 參數與回傳型別使用合約介面

```csharp
public ILoginResponse Login(ILoginRequest request)
{
    // Executor 傳入 LoginRequest，BO-to-BO 呼叫也可直接傳入 LoginRequest
    return new LoginResponse
    {
        AccessToken = Guid.NewGuid(),
        UserId = request.UserId
    };
}
```

### 情境二：API 方法，BO 需要額外屬性

BO 間互相呼叫時需要傳遞 API 不可見的內部屬性。

**需要建立的型別：** 合約介面 + API 合約型別 + BO 參數型別

```csharp
// BO 參數帶有額外屬性
public class LoginArgs : BusinessArgs, ILoginRequest
{
    public string UserId { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ClientPublicKey { get; set; } = string.Empty;
    public bool IsAutoLogin { get; set; }  // BO 專用
}

// BO 方法透過模式比對取得額外屬性
public ILoginResponse Login(ILoginRequest request)
{
    bool isAutoLogin = request is LoginArgs args && args.IsAutoLogin;
    // ...
}
```

### 情境三：BO 內部方法（不公開至 API）

僅在 BO 內部使用的方法，不對外公開為 JSON-RPC API。

**需要建立的型別：** 僅 BO 參數型別（不需要合約介面，不需要 API 型別）

```csharp
public class RecalcArgs : BusinessArgs
{
    public string OrderId { get; set; }
    public bool ForceRecalc { get; set; }
}
```

---

## 序列化規則

| 層級 | `[MessagePackObject]` | `[Key(n)]` | `[Serializable]` | `IObjectSerialize` |
|------|:---:|:---:|:---:|:---:|
| 合約介面 | 否 | 否 | 否 | 否 |
| API 型別 | **是** | **是**（從 100 起） | **是** | 是（基底提供） |
| BO 型別 | 否 | 否 | 否 | 否 |

- `[Key(0)]` 保留給基底類別的 `ParameterCollection` 屬性
- 自訂屬性的 Key 從 100 開始，避免與基底衝突

---

## 用戶端開發

用戶端透過 `SystemApiConnector` 呼叫 API 時，一律使用 `Request` / `Response` 型別：

```csharp
var connector = new SystemApiConnector(endpoint, accessToken);

// 使用 API 型別，不使用 BO 型別
var response = await connector.LoginAsync("admin", "password");
Console.WriteLine(response.AccessToken);
```

用戶端**不應引用**也**不需引用** `BusinessArgs`、`BusinessResult` 或任何 `XxxArgs` / `XxxResult` 型別。

---

## BO 開發

### 方法簽章

BO 方法的參數與回傳型別使用**合約介面**，不綁定具體實作類別：

```csharp
// 正確：使用合約介面
public ILoginResponse Login(ILoginRequest request) { ... }

// 避免：綁定具體型別
public LoginResult Login(LoginArgs args) { ... }
```

### 回應映射

當 BO 方法回傳純 POCO（如 `LoginResult`），框架的 `ApiContractRegistry` 會自動將其轉換為對應的 API 型別（`LoginResponse`）進行序列化。此映射需在應用程式啟動時註冊：

```csharp
// 在啟動時註冊
ApiContractRegistry.Register<ILoginResponse, LoginResponse>();
```

### ExecFunc 模式

`ExecFunc` 使用 `ParameterCollection` 作為通用參數機制，不適用本文所述的 Request/Response 分層模式，維持既有做法。

---

## 新增 API 方法的步驟

以新增 `GetOrder` 方法為例：

1. **定義合約介面**（`src/Bee.Api.Contracts/`）
   - `IGetOrderRequest.cs` — 輸入屬性
   - `IGetOrderResponse.cs` — 輸出屬性

2. **建立 API 合約型別**（`src/Bee.Api.Core/System/` 或對應模組目錄）
   - `GetOrderRequest.cs` — 繼承 `ApiRequest`，實作 `IGetOrderRequest`，標記 MessagePack
   - `GetOrderResponse.cs` — 繼承 `ApiResponse`，實作 `IGetOrderResponse`，標記 MessagePack

3. **實作 BO 方法**
   - 方法簽章使用 `IGetOrderRequest` / `IGetOrderResponse`
   - 若需要 BO 專用屬性，另建 `GetOrderArgs` / `GetOrderResult`

4. **註冊回應映射**（若 BO 回傳純 POCO）
   - `ApiContractRegistry.Register<IGetOrderResponse, GetOrderResponse>()`

5. **更新用戶端 Connector**（若需要）
   - 在 Connector 中新增對應方法，使用 `GetOrderRequest` / `GetOrderResponse`

---

## 組件相依方向

```
Bee.Api.Contracts           ← 合約介面（API 與 BO 共用）
    │
    ├── Bee.Api.Core        ← API 型別（含序列化）
    │       │
    │       └── Bee.Api.Client  ← 用戶端（只用 Request / Response）
    │
    └── Bee.Business        ← BO 型別（純 POCO）+ BO 介面
```

**原則**：箭頭方向為相依方向。`Bee.Api.Core` 與 `Bee.Business` 彼此不相依，各自只依賴 `Bee.Api.Contracts`。
