# 執行計畫：API 合約與 BO 參數分層實作

## Context

目前 `BusinessArgs`/`BusinessResult` 放在 `Bee.Api.Contracts`，導致 `Bee.Business` 必須引用 API 層。此次實作建立介面合約 + 組件隔離 + 基底類別拆分的基礎架構，並搬遷 BO 介面至正確位置。

**設計決策**（已確認）：
1. 新 `BusinessArgs`/`BusinessResult` 為純 POCO，不實作 `IObjectSerialize`
2. Executor 負責 `IXxxResponse` → `XxxResponse` 的映射序列化
3. 合約介面放在 `Bee.Definition.Api` 命名空間
4. `IBusinessObject`/`ISystemBusinessObject`/`IFormBusinessObject` 移至 `Bee.Business`

## 目標架構

```
Bee.Definition                ← IXxxRequest / IXxxResponse（合約介面）
    ├── Bee.Api.Core          ← ApiRequest / ApiResponse（基底，含序列化）
    │                            XxxRequest / XxxResponse（API 合約型別）
    │                            Executor 回應映射機制
    ├── Bee.Api.Client        ← 呼叫端，使用 XxxRequest / XxxResponse（API 層型別）
    │                            SystemApiConnector / FormApiConnector
    └── Bee.Business          ← IBusinessObject / ISystemBusinessObject / IFormBusinessObject
                                 BusinessArgs / BusinessResult（新基底，純 POCO）
                                 XxxArgs / XxxResult（BO 參數）
```

**Bee.Api.Client 定位**：呼叫端只接觸 API 層型別（`Request`/`Response`），不使用 BO 層型別（`Args`/`Result`）。現行 `SystemApiConnector` 直接建構 `LoginArgs` 等 BO 型別，需改為使用 `LoginRequest` 等 API 型別。

## 執行步驟

### 步驟 1：在 Bee.Definition 建立合約介面基礎

**新增檔案**：
- `src/Bee.Definition/Api/ILoginRequest.cs`
- `src/Bee.Definition/Api/ILoginResponse.cs`

以 Login 為範例建立合約介面，介面只定義唯讀屬性（`{ get; }`），不含序列化標記。

```csharp
namespace Bee.Definition.Api
{
    public interface ILoginRequest
    {
        string UserId { get; }
        string Password { get; }
        string ClientPublicKey { get; }
    }
}
```

**複雜度**：低。純新增檔案，不影響現有程式碼。

---

### 步驟 2：在 Bee.Api.Core 建立 API 基底類別

**新增檔案**：
- `src/Bee.Api.Core/ApiRequest.cs`
- `src/Bee.Api.Core/ApiResponse.cs`

這兩個類別承接現行 `BusinessArgs`/`BusinessResult` 的序列化基礎建設（`IObjectSerialize`、`[Key(0)] ParameterCollection`、`SerializeState`）。

```csharp
[Serializable]
public abstract class ApiRequest : IObjectSerialize
{
    [Key(0)]
    public ParameterCollection Parameters { get; set; }
    // ... IObjectSerialize 實作（與現行 BusinessArgs 相同）
}
```

**關鍵參考**：`src/Bee.Api.Contracts/BusinessArgs.cs`（複製序列化邏輯）

**複雜度**：低。結構與現行 `BusinessArgs`/`BusinessResult` 幾乎相同。

---

### 步驟 3：在 Bee.Business 建立新的 BO 基底類別

**新增檔案**：
- `src/Bee.Business/BusinessArgs.cs`（新，純 POCO）
- `src/Bee.Business/BusinessResult.cs`（新，純 POCO）

```csharp
namespace Bee.Business
{
    public abstract class BusinessArgs
    {
        // 純 POCO，無 IObjectSerialize、無 [Key]
        // 可保留 ParameterCollection 為普通屬性（視需要）
    }
}
```

**注意**：命名空間為 `Bee.Business`，與現行 `Bee.Api.Contracts.BusinessArgs` 共存。現有程式碼的 `using Bee.Api.Contracts;` 不受影響。新開發的 BO 參數使用 `Bee.Business.BusinessArgs`。

**複雜度**：低。

---

### 步驟 4：搬遷 BO 介面至 Bee.Business

**搬遷檔案**：
- `src/Bee.Api.Contracts/IBusinessObject.cs` → `src/Bee.Business/IBusinessObject.cs`
- `src/Bee.Api.Contracts/ISystemBusinessObject.cs` → `src/Bee.Business/ISystemBusinessObject.cs`
- `src/Bee.Api.Contracts/IFormBusinessObject.cs` → `src/Bee.Business/IFormBusinessObject.cs`

**命名空間變更**：`Bee.Api.Contracts` → `Bee.Business`

**影響檔案**（需更新 using）：
- `src/Bee.Business/BusinessObjects/BusinessObject.cs` — 移除 `using Bee.Api.Contracts;`（介面已在同組件）
- `src/Bee.Business/BusinessObjects/SystemBusinessObject.cs` — 同上
- `src/Bee.Business/BusinessObjects/FormBusinessObject.cs` — 同上
- `src/Bee.Business/BusinessObjects/BusinessObjectProvider.cs` — 同上

**不受影響**：
- `Bee.Api.Core`（JsonRpcExecutor 使用反射，不引用這些介面）
- `Bee.Api.Client`（不引用這些介面）

**複雜度**：低-中。搬遷簡單，但需確認沒有遺漏的引用。

---

### 步驟 5：建立 Executor 回應映射機制

**修改檔案**：
- `src/Bee.Api.Core/JsonRpc/ApiPayloadConverter.cs`（或新增映射類別）

**設計**：在 `TransformTo()` 中，當 `payload.Value` 的 runtime type 沒有 `[MessagePackObject]` 標記時，透過介面映射至對應的 API 型別再序列化。

可能實作方式：
- 新增 `IApiResponseMapper` 或 `ApiContractRegistry`，註冊 `IXxxResponse → XxxResponse` 的映射
- 映射邏輯：透過介面屬性逐一複製至 API 型別實例

**關鍵參考**：
- `src/Bee.Api.Core/JsonRpc/ApiPayloadConverter.cs`（`TransformTo` 方法，line 22-49）
- `src/Bee.Api.Core/MessagePack/MessagePackHelper.cs`

**複雜度**：**中-高**。這是整個計畫中最複雜的部分，需要：
1. 設計型別註冊機制
2. 實作屬性映射邏輯
3. 確保效能可接受
4. 處理 ParameterCollection 等複雜屬性的映射

---

### 步驟 6：以 Login 為範例建立完整 API 合約型別

**新增檔案**：
- `src/Bee.Api.Core/System/LoginRequest.cs`（`ApiRequest, ILoginRequest`，有 MessagePack）
- `src/Bee.Api.Core/System/LoginResponse.cs`（`ApiResponse, ILoginResponse`，有 MessagePack）

**注意**：此步驟為「新型別示範」，不修改現行 `LoginArgs`/`LoginResult`。現有 API 方法繼續使用舊型別，新的 Request/Response 型別為後續新 API 開發使用。

**複雜度**：低。

---

### 步驟 6.5：更新 Bee.Api.Client（呼叫端適配）

**背景**：`Bee.Api.Client` 是呼叫端，目前直接建構 BO 層型別（`LoginArgs`、`PingArgs` 等），這在新架構中不合理。

**修改檔案**：
- `src/Bee.Api.Client/Connectors/SystemApiConnector.cs` — 將 `LoginArgs` → `LoginRequest`，`LoginResult` → `LoginResponse`，以此類推
- `src/Bee.Api.Client/Connectors/FormApiConnector.cs` — ExecFunc 排除，暫不修改

**具體變更**（以 Login 為例）：
```csharp
// 舊：建構 BO 型別
var args = new LoginArgs { UserId = userId, Password = password };
var result = await ExecuteAsync<LoginResult>("System", "Login", args);

// 新：建構 API 型別
var request = new LoginRequest { UserId = userId, Password = password };
var response = await ExecuteAsync<LoginResponse>("System", "Login", request);
```

**影響範圍**：
- `SystemApiConnector` 中所有 System 方法（Login, Ping, CreateSession, GetDefine, SaveDefine, GetCommonConfiguration, CheckPackageUpdate, GetPackage）
- 需同步建立對應的 8 對 Request/Response 型別（步驟 6 只建了 Login，此處需補齊其餘 7 對）

**額外新增檔案**（`src/Bee.Api.Core/System/`）：
- `PingRequest.cs` / `PingResponse.cs`
- `CreateSessionRequest.cs` / `CreateSessionResponse.cs`
- `GetDefineRequest.cs` / `GetDefineResponse.cs`
- `SaveDefineRequest.cs` / `SaveDefineResponse.cs`
- `GetCommonConfigurationRequest.cs` / `GetCommonConfigurationResponse.cs`
- `CheckPackageUpdateRequest.cs` / `CheckPackageUpdateResponse.cs`
- `GetPackageRequest.cs` / `GetPackageResponse.cs`

**對應介面**（`src/Bee.Definition/Api/`）：同步新增 7 對 IXxxRequest/IXxxResponse 介面。

**複雜度**：中。型別數量多（8 對 = 16 個檔案），但每個都是簡單的屬性複製。

**注意**：此步驟將 `Bee.Api.Client` 的 `using Bee.Api.Contracts.System;` 改為 `using Bee.Api.Core.System;`。

---

### 步驟 7：更新專案引用

**修改檔案**：
- `src/Bee.Api.Core/Bee.Api.Core.csproj` — 確認已引用 `Bee.Definition`（目前透過 `Bee.Api.Contracts` 間接引用）
- `src/Bee.Business/Bee.Business.csproj` — `Bee.Api.Contracts` 引用暫時保留（現有型別仍在使用），未來重構時移除

**複雜度**：低。

---

### 步驟 8：撰寫規範文件

**新增檔案**：`docs/api-bo-contract-separation.md`

依計畫大綱撰寫 7 個章節的規範文件（繁體中文）。

---

### 步驟 9：新增單元測試

**新增/修改檔案**：
- `tests/Bee.Definition.UnitTests/` — 合約介面測試（如有需要）
- `tests/Bee.Api.Core.UnitTests/` — ApiRequest/ApiResponse 序列化測試、回應映射測試
- `tests/Bee.Business.UnitTests/` — 新 BusinessArgs/BusinessResult 測試

**重點測試**：
- Executor 的 `IXxxResponse` → `XxxResponse` 映射正確性
- 新 `ApiRequest`/`ApiResponse` 的 MessagePack 序列化相容性
- 確保現有測試全部通過（搬遷 BO 介面後）

---

## 複雜度總評

| 步驟 | 複雜度 | 風險 |
|------|--------|------|
| 1. 合約介面（8 對） | 低 | 無 |
| 2. API 基底 | 低 | 低（複製現有邏輯） |
| 3. BO 基底 | 低 | 無 |
| 4. 搬遷 BO 介面 | 低-中 | 中（需確認所有引用） |
| 5. Executor 映射 | **中-高** | **高（核心機制，需仔細設計）** |
| 6. API 合約型別（8 對） | 低 | 低 |
| 6.5. Client 適配 | **中** | **中（影響 SystemApiConnector 所有方法）** |
| 7. 專案引用 | 低 | 低 |
| 8. 規範文件 | 中 | 無 |
| 9. 單元測試 | 中 | 低 |

**新增檔案估計**：~40 個（8 對介面 + 8 對 API 型別 + 基底類別 + 映射機制 + 文件 + 測試）
**修改檔案估計**：~8 個（BO 介面搬遷 + Client Connector + 專案引用）

**整體可行性**：可行。兩個風險點：
1. 步驟 5（Executor 映射）— 核心機制，建議先實作原型
2. 步驟 6.5（Client 適配）— 影響面廣，但每個變更本身簡單

## 可行性風險

1. **Executor 映射效能**：每次 API 回應都需要屬性複製，對高頻 API 可能有效能影響。可透過快取映射委派（Expression Tree 或 compiled delegate）緩解。
2. **向後相容**：新舊型別共存期間，需確保現行 `BusinessArgs` → `LoginArgs` 路徑完全不受影響。
3. **MessagePack Key 衝突**：新 `ApiRequest` 的 `[Key(0)]` 需與現行 `BusinessArgs` 保持一致，確保序列化相容。
4. **Client 端破壞性變更**：`SystemApiConnector` 的公開方法回傳型別從 `LoginResult` 改為 `LoginResponse`，所有呼叫端都需更新。這是 **breaking change**，需在版本號反映。

## 驗證方式

- [ ] `dotnet build` 全專案建置成功
- [ ] `dotnet test` 全部測試通過（含既有測試）
- [ ] Login 範例的完整路徑驗證（API 呼叫 → 反序列化 → BO 執行 → 回應序列化）
- [ ] BO 介面搬遷後，所有 BO 實作正常運作
- [ ] Client 端驗證：`SystemApiConnector` 使用新 Request/Response 型別，序列化/反序列化正常
