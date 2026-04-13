# API/BO 合約分離架構規範

本文件定義 Bee.NET 框架中 API 合約與 BO 參數的分層架構規範，適用於所有新開發的 API 方法。

---

## 1. 目的與動機

### 現行問題

目前 `BusinessArgs` / `BusinessResult` 基底類別定義在 `Bee.Api.Contracts` 組件中，導致 `Bee.Business` 必須引用 `Bee.Api.Contracts` 才能繼承這些基底類別。這違反了 Clean Architecture 的**向內相依原則**：商業邏輯層不應依賴 API 層的任何組件。

此外，現有的 `LoginArgs`（位於 `Bee.Api.Contracts`）同時標記了 `[MessagePackObject]` 與 `[Key(n)]` 序列化屬性，但 BO 層本身不需要這些序列化能力。API 合約與 BO 參數共用同一型別，使得兩個本應獨立的關注點被耦合在一起。

### 設計目標

- **消除反向相依**：`Bee.Business` 不再引用任何 API 層組件
- **關注點分離**：API 型別負責序列化與傳輸，BO 型別負責業務邏輯參數
- **合約統一**：透過介面定義共用屬性合約，確保 API 與 BO 型別的屬性一致性
- **漸進式採用**：新開發遵循本規範，現有型別不強制重新命名

---

## 2. 架構模式

### 五層結構

| 層級 | 型別 | 所在組件 | 命名空間 | 職責 |
|------|------|----------|----------|------|
| 合約介面 | `IXxxRequest` / `IXxxResponse` | `Bee.Api.Contracts` | `Bee.Api.Contracts` | 定義屬性合約，為單一真實來源 |
| API 基底 | `ApiRequest` / `ApiResponse` | `Bee.Api.Core` | `Bee.Api.Core` | API 層基底，含 MessagePack 序列化與 `ParameterCollection` |
| API 合約 | `XxxRequest` / `XxxResponse` | `Bee.Api.Core` | `Bee.Api.Core.System` | 繼承 API 基底並實作合約介面，用於 API 傳輸 |
| BO 基底 | `BusinessArgs` / `BusinessResult` | `Bee.Business` | `Bee.Business` | BO 層基底，純 POCO，不含任何序列化標記 |
| BO 參數 | `XxxArgs` / `XxxResult` | `Bee.Business` | `Bee.Business` | 繼承 BO 基底並實作合約介面，可擴充 BO 專用屬性 |

### 組件相依圖

```
Bee.Definition                  <-- 共用型別（DefineType, ParameterCollection 等）
    |
    +-- Bee.Api.Contracts       <-- 合約介面（IXxxRequest / IXxxResponse）+ 共用 DTO
    |       |
    |       +-- Bee.Api.Core    <-- API 型別（XxxRequest / XxxResponse）
    |       |       |               繼承 ApiRequest / ApiResponse
    |       |       |               實作 IXxxRequest / IXxxResponse
    |       |       |               標記 [MessagePackObject] + [Key(n)]
    |       |       |
    |       |       +-- Bee.Api.Client  <-- 用戶端，使用 Request / Response 型別
    |       |
    |       +-- Bee.Business    <-- BO 型別（XxxArgs / XxxResult）
    |               |               繼承 BusinessArgs / BusinessResult
    |               |               實作 IXxxRequest / IXxxResponse
    |               |               純 POCO，無序列化標記
    |               |
    |               +-- IBusinessObject / ISystemBusinessObject / IFormBusinessObject
```

**關鍵隔離效果**：`Bee.Api.Core` 與 `Bee.Business` 各自依賴 `Bee.Api.Contracts`（純介面）與 `Bee.Definition`（共用型別），彼此完全脫勾。用戶端（`Bee.Api.Client`）只引用 `Bee.Api.Core`，完全不接觸 BO 參數型別。

### Executor 的角色

當 BO 方法回傳的物件是純 POCO（例如 `LoginResult`）而非已標記 `[MessagePackObject]` 的 API 型別時，Executor 透過 `ApiContractRegistry` 將回傳值從 `IXxxResponse` 對應到 `XxxResponse`，確保序列化正確進行。

```
BO 回傳 LoginResult（實作 ILoginResponse）
    --> ApiContractRegistry.ConvertForSerialization()
    --> 轉換為 LoginResponse（具備 MessagePack 標記）
    --> 序列化並回傳給用戶端
```

---

## 3. 命名規則

| 類別 | 命名模式 | 範例 | 說明 |
|------|----------|------|------|
| 合約介面（輸入） | `IXxxRequest` | `ILoginRequest` | 定義輸入屬性的唯讀合約 |
| 合約介面（輸出） | `IXxxResponse` | `ILoginResponse` | 定義輸出屬性的唯讀合約 |
| API 輸入型別 | `XxxRequest` | `LoginRequest` | 繼承 `ApiRequest`，實作 `IXxxRequest` |
| API 輸出型別 | `XxxResponse` | `LoginResponse` | 繼承 `ApiResponse`，實作 `IXxxResponse` |
| BO 輸入型別 | `XxxArgs` | `LoginArgs` | 繼承 `BusinessArgs`，實作 `IXxxRequest` |
| BO 輸出型別 | `XxxResult` | `LoginResult` | 繼承 `BusinessResult`，實作 `IXxxResponse` |

### 命名對應關係

一個 API 方法的完整型別組合：

```
ILoginRequest    -->  LoginRequest   (API)  +  LoginArgs   (BO)
ILoginResponse   -->  LoginResponse  (API)  +  LoginResult (BO)
```

---

## 4. 序列化規則

### API 層（Bee.Api.Core）

`ApiRequest` / `ApiResponse` 及其子類別標記 MessagePack 序列化屬性：

```csharp
[MessagePackObject]
[Serializable]
public class LoginRequest : ApiRequest, ILoginRequest
{
    [Key(100)]
    public string UserId { get; set; } = string.Empty;
}
```

- 類別層級標記 `[MessagePackObject]` 與 `[Serializable]`
- 每個屬性標記 `[Key(n)]`，索引從 100 開始（0 保留給基底類別的 `ParameterCollection`）
- 實作 `IObjectSerialize` 介面（由 `ApiRequest` / `ApiResponse` 基底提供）

### BO 層（Bee.Business）

`BusinessArgs` / `BusinessResult` 及其子類別為純 POCO，不帶任何序列化標記：

```csharp
public class LoginArgs : BusinessArgs, ILoginRequest
{
    public string UserId { get; set; } = string.Empty;
}
```

- 不標記 `[MessagePackObject]`、`[Key(n)]`、`[Serializable]`
- 不實作 `IObjectSerialize`
- 可自由新增 BO 專用屬性，不影響 API 合約

### 合約介面

合約介面不涉及序列化，僅定義唯讀屬性（getter only）：

```csharp
public interface ILoginRequest
{
    string UserId { get; }
    string Password { get; }
}
```

---

## 5. 程式碼範例

以 Login 為典型範例，完整展示各層型別定義與呼叫路徑。

### 5.1 合約介面（Bee.Api.Contracts/）

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

### 5.2 API 合約（Bee.Api.Core/System/）

```csharp
[MessagePackObject, Serializable]
public class LoginRequest : ApiRequest, ILoginRequest
{
    [Key(100)] public string UserId { get; set; } = string.Empty;
    [Key(101)] public string Password { get; set; } = string.Empty;
    [Key(102)] public string ClientPublicKey { get; set; } = string.Empty;
}

[MessagePackObject, Serializable]
public class LoginResponse : ApiResponse, ILoginResponse
{
    [Key(100)] public Guid AccessToken { get; set; } = Guid.Empty;
    [Key(101)] public DateTime ExpiredAt { get; set; }
    [Key(102)] public string ApiEncryptionKey { get; set; } = string.Empty;
    [Key(103)] public string UserId { get; set; } = string.Empty;
    [Key(104)] public string UserName { get; set; } = string.Empty;
}
```

### 5.3 BO 參數（Bee.Business/）

```csharp
public class LoginArgs : BusinessArgs, ILoginRequest
{
    public string UserId { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ClientPublicKey { get; set; } = string.Empty;
}

public class LoginResult : BusinessResult, ILoginResponse
{
    public Guid AccessToken { get; set; } = Guid.Empty;
    public DateTime ExpiredAt { get; set; }
    public string ApiEncryptionKey { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
}
```

### 5.4 BO 方法簽章

```csharp
public class SessionBO : SystemBusinessObject
{
    public ILoginResponse Login(ILoginRequest request)
    {
        // 業務邏輯...
        return new LoginResult
        {
            AccessToken = Guid.NewGuid(),
            ExpiredAt = DateTime.UtcNow.AddHours(8),
            UserId = request.UserId,
            UserName = "Jeff"
        };
    }
}
```

BO 方法的參數與回傳型別使用**合約介面**（`ILoginRequest` / `ILoginResponse`），不綁定具體實作。

### 5.5 呼叫路徑

**路徑一：API 呼叫（用戶端 --> Executor --> BO）**

```
用戶端發送 LoginRequest（MessagePack 序列化）
    --> Executor 反序列化為 LoginRequest（實作 ILoginRequest）
    --> BO.Login(ILoginRequest) 收到 LoginRequest
    --> BO 回傳 LoginResult（實作 ILoginResponse）
    --> ApiContractRegistry 轉換為 LoginResponse
    --> MessagePack 序列化回傳給用戶端
```

**路徑二：BO-to-BO 呼叫（伺服器端內部）**

```csharp
// 訂單 BO 呼叫 Session BO
var args = new LoginArgs
{
    UserId = "system",
    Password = "internal-token",
    ClientPublicKey = string.Empty
};

ILoginResponse result = sessionBO.Login(args);
// 不經過序列化，直接取得 LoginResult
```

---

## 6. 三種使用情境

| 情境 | 說明 | IRequest/IResponse | Request/Response | Args/Result |
|------|------|:---:|:---:|:---:|
| API 方法，BO 不需額外屬性 | 最常見情境，API 合約與 BO 參數屬性完全一致 | 需要 | 需要 | 不需要 |
| API 方法，BO 需額外屬性 | BO 間互相呼叫時需要傳遞額外的內部屬性 | 需要 | 需要 | 需要 |
| BO-only 方法（不公開至 API） | 純內部方法，不對外公開為 API | 不需要 | 不需要 | 需要 |

### 情境一：API 方法，BO 不需額外屬性

BO 方法簽章直接使用合約介面，Executor 傳入 `XxxRequest`，BO-to-BO 呼叫也傳入 `XxxRequest`（因為不需要額外屬性，不必建立 Args）。

### 情境二：API 方法，BO 需額外屬性

BO 參數型別（`XxxArgs`）在實作合約介面之外，額外定義 BO 專用屬性。BO 方法內部可透過模式比對取得額外屬性：

```csharp
public ILoginResponse Login(ILoginRequest request)
{
    if (request is LoginArgs args)
    {
        // 使用 args 上的 BO 專用屬性
    }
    // ...
}
```

### 情境三：BO-only 方法

不對外公開的內部方法，`XxxArgs` / `XxxResult` 獨立存在，不需要合約介面，也不需要對應的 Request / Response。

---

## 7. 注意事項

1. **現有型別已完成搬遷**：所有 System 型別（`LoginArgs`、`LoginResult` 等）已搬遷至 `Bee.Business.System`，對應的 API 型別位於 `Bee.Api.Core.System`。

2. **ExecFunc 已納入合約介面模式**：`ExecFunc` 現已定義 `IExecFuncRequest` / `IExecFuncResponse` 合約介面於 `Bee.Api.Contracts`，BO 層的 `ExecFuncArgs` / `ExecFuncResult` 與 API 層的 `ExecFuncRequest` / `ExecFuncResponse` 均實作對應介面。

3. **已遷移的型別**：`PackageUpdateQuery`、`PackageUpdateInfo`、`PackageDelivery` 已搬遷至 `Bee.Api.Contracts` 命名空間。

4. **BO 介面歸屬**：`IBusinessObject`、`ISystemBusinessObject`、`IFormBusinessObject` 定義在 `Bee.Business` 組件中，確保 BO 層的介面與實作同屬一個組件。

5. **用戶端（Bee.Api.Client）**：用戶端僅使用 `Request` / `Response` 型別進行 API 呼叫，不引用也不接觸 `Args` / `Result` 型別。

6. **ApiContractRegistry 註冊時機**：所有 `IXxxResponse` --> `XxxResponse` 的對應關係須在應用程式啟動時透過 `ApiContractRegistry.Register<TContract, TApi>()` 註冊，確保 Executor 能正確轉換 BO 回傳值。
