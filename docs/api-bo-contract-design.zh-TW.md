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

定義 API 方法輸入與輸出的屬性合約，只包含唯讀屬性，不含任何序列化標記。介面依軸分入 `Bee.Api.Contracts.System` / `.Form` / `.AuditLog`；root 的 `Bee.Api.Contracts` 命名空間只保留跨軸共用型別（`IExecFuncRequest` / `IExecFuncResponse`）。

```csharp
namespace Bee.Api.Contracts.System
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

### API 合約型別（Bee.Api.Core.Messages.System）

繼承 `ApiRequest` / `ApiResponse`，實作合約介面，標記 MessagePack 序列化屬性。用戶端透過這些型別發送請求與接收回應。

合約型別採**屬性名為鍵**（`[MessagePackObject(keyAsPropertyName: true)]`）：成員以屬性名作為 wire 鍵，與 JSON 合約一致，並消除脆弱的整數鍵編號協調。**不要**加 `[Key(int)]`；排除成員用 `[IgnoreMember]`，僅在 wire 名需與屬性名不同時用 `[Key("name")]`。見 [ADR-030](adr/adr-030-messagepack-name-based-keys.md)。

```csharp
[MessagePackObject(keyAsPropertyName: true)]
public class LoginRequest : ApiRequest, ILoginRequest
{
    public string UserId { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ClientPublicKey { get; set; } = string.Empty;
}

[MessagePackObject(keyAsPropertyName: true)]
public class LoginResponse : ApiResponse, ILoginResponse
{
    public Guid AccessToken { get; set; } = Guid.Empty;
    public DateTime ExpiredAt { get; set; }
    public string ApiEncryptionKey { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
}
```

> **例外 —— `[Union]` 多型階層**（如 `FilterNode`）維持整數 `[Key]` + `[Union]`，因 `[Union]` 與 `keyAsPropertyName` 不相容。

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
**BO 方法簽章：** 參數與回傳型別使用具體 `XxxArgs` / `XxxResult` 型別

```csharp
public LoginResult Login(LoginArgs args)
{
    // Executor 傳入 LoginArgs，BO-to-BO 呼叫也直接傳入 LoginArgs
    return new LoginResult
    {
        AccessToken = Guid.NewGuid(),
        UserId = args.UserId
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

// BO 方法直接以具體 args 型別為參數，額外屬性可直接取用
public LoginResult Login(LoginArgs args)
{
    bool isAutoLogin = args.IsAutoLogin;
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

| 層級 | `[MessagePackObject]` | `[Key(n)]` | `IObjectSerialize` |
|------|:---:|:---:|:---:|
| 合約介面 | 否 | 否 | 否 |
| API 型別 | **是** | **是**（從 100 起） | 是（基底提供） |
| BO 型別 | 否 | 否 | 否 |

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

BO 方法的參數與回傳型別使用**具體 `XxxArgs` / `XxxResult` 型別**，BO 介面宣告（`ISystemBusinessObject`、`IFormBusinessObject`）也一樣是具體型別：

```csharp
// BO 方法（及其介面宣告）皆使用具體型別
public LoginResult Login(LoginArgs args) { ... }
```

合約介面（`ILoginRequest` / `ILoginResponse` 等）仍然存在，且由 `XxxArgs` / `XxxResult`
型別（以及 API 的 `XxxRequest` / `XxxResponse` 型別）**實作** —— 例如
`LoginArgs : BusinessArgs, ILoginRequest`。它們提供共用屬性合約與跨層獨立性，但
**不用於方法簽章**；簽章綁定具體型別。

### 回應映射

當 BO 方法回傳純 POCO（如 `LoginResult`），框架的 `ApiOutputConverter` 會透過**命名慣例**自動對應至 API 型別（`LoginResponse`），**不需要任何註冊**。

對應規則：

```
{Action}Result  ──反射搜尋 Bee.Api.Core 組件──▶  {Action}Response
```

例如 `PingResult` 會自動對應到 `PingResponse`。反射結果以 BO 型別為 key 快取，每個型別只解析一次。

> 此命名慣例為**強制規範**：凡不符合 `{Action}Result` / `{Action}Response` 命名的 BO 回傳型別都無法自動轉換。背景請參閱 [ADR-007](adr/adr-007-convention-based-type-resolution.md)。

### ExecFunc 模式

`ExecFunc` 使用 `ParameterCollection` 作為通用參數機制，不適用本文所述的 Request/Response 分層模式，維持既有做法。

---

## 新增 API 方法的步驟

以新增 `GetOrder` 方法為例：

1. **定義合約介面**（`src/Bee.Api.Contracts/`）
   - `IGetOrderRequest.cs` — 輸入屬性
   - `IGetOrderResponse.cs` — 輸出屬性

2. **建立 API 合約型別**（`src/Bee.Api.Core/Messages/System/` 或 `Messages/Form/` 等對應模組目錄；namespace 為 `Bee.Api.Core.Messages.<Module>`）
   - `GetOrderRequest.cs` — 繼承 `ApiRequest`，實作 `IGetOrderRequest`，標記 MessagePack
   - `GetOrderResponse.cs` — 繼承 `ApiResponse`，實作 `IGetOrderResponse`，標記 MessagePack

3. **實作 BO 方法**
   - 方法簽章使用具體 `GetOrderArgs` / `GetOrderResult` 型別
   - 必須遵守 `{Action}Args` / `{Action}Result` 命名慣例，`ApiOutputConverter` 才能自動將 `GetOrderResult` 對應至 `GetOrderResponse`
   - args / result 型別實作對應合約介面（`GetOrderArgs : BusinessArgs, IGetOrderRequest` 等），提供跨層屬性共用

4. **更新用戶端 Connector**（若需要）
   - 在 Connector 中新增對應方法，使用 `GetOrderRequest` / `GetOrderResponse`

> 不需要任何手動註冊，回應映射由命名慣例自動推導（詳見 [ADR-007](adr/adr-007-convention-based-type-resolution.md)）。

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
