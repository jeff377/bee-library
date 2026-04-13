# 計畫：API 合約與 BO 參數分層規範

## Context

目前 Bee.NET 的 JSON-RPC 框架中，API 方法合約（如 `LoginArgs`/`LoginResult`）同時作為 BO 方法的參數型別。當 BO 之間互相呼叫時（例如訂單 BO 轉採購 BO），可能需要額外的內部屬性，但這些屬性不屬於 API 合約、也不應讓外部呼叫者使用。

此外，現有的 `BusinessArgs` / `BusinessResult` 基底類別放在 `Bee.Api.Contracts`，導致 `Bee.Business` 必須引用 API 層的組件，形成不合理的相依方向。

為解決此問題，採用**介面合約 + 組件隔離 + 基底類別拆分**模式：

- **合約介面**：`IXxxRequest` / `IXxxResponse` → 放在 `Bee.Definition`
- **API 基底與合約**：`ApiRequest` / `ApiResponse`（基底）、`XxxRequest` / `XxxResponse`（實作介面，含 MessagePack 序列化）→ 放在 `Bee.Api.Core`
- **BO 基底與參數**：`BusinessArgs` / `BusinessResult`（基底）、`XxxArgs` / `XxxResult`（實作介面，純 POCO，可擴充）→ 放在 `Bee.Business`

組件相依關係：
```
Bee.Definition          ← IXxxRequest / IXxxResponse（合約介面）
    ├── Bee.Api.Core    ← ApiRequest / ApiResponse（API 基底，含序列化）
    │                      XxxRequest : ApiRequest, IXxxRequest
    │                      XxxResponse : ApiResponse, IXxxResponse
    └── Bee.Business    ← BusinessArgs / BusinessResult（BO 基底，不含序列化）
                           XxxArgs : BusinessArgs, IXxxRequest
                           XxxResult : BusinessResult, IXxxResponse
```

`Bee.Api.Core` 與 `Bee.Business` 各自只依賴 `Bee.Definition`，彼此完全脫勾。前端只引用 `Bee.Api.Core`，完全看不到 BO 參數。

## 產出項目

### 新增規範文件

建立 `docs/api-bo-contract-separation.md`（繁體中文撰寫）。

### 文件大綱

1. **目的與動機**
   - 現行設計的限制：API 合約與 BO 參數共用同一型別
   - BO 間呼叫需額外屬性的實際場景（訂單轉採購等）
   - `BusinessArgs` / `BusinessResult` 放在 API 層導致不合理的相依方向

2. **架構模式：介面合約 + 組件隔離**

   四層結構與組件歸屬：

   | 層級 | 型別 | 所在組件 | 職責 |
   |------|------|----------|------|
   | 合約介面 | `IXxxRequest` / `IXxxResponse` | `Bee.Definition` | 定義屬性合約，為單一來源 |
   | API 基底 | `ApiRequest` / `ApiResponse` | `Bee.Api.Core` | API 層基底類別，含 MessagePack 序列化 |
   | API 合約 | `XxxRequest` / `XxxResponse` | `Bee.Api.Core` | 繼承 API 基底，實作合約介面 |
   | BO 基底 | `BusinessArgs` / `BusinessResult` | `Bee.Business` | BO 層基底類別，不含序列化 |
   | BO 參數 | `XxxArgs` / `XxxResult` | `Bee.Business` | 繼承 BO 基底，實作合約介面，可擴充 BO 專用屬性 |

   組件相依圖與隔離效果說明。

3. **命名規則**
   - 合約介面：`IXxxRequest` / `IXxxResponse`（如 `ILoginRequest` / `ILoginResponse`）
   - API 基底：`ApiRequest` / `ApiResponse`
   - API 合約：`XxxRequest` / `XxxResponse`（如 `LoginRequest` / `LoginResponse`）
   - BO 基底：`BusinessArgs` / `BusinessResult`
   - BO 參數：`XxxArgs` / `XxxResult`（如 `LoginArgs` / `LoginResult`）

4. **序列化規則**
   - `ApiRequest` / `ApiResponse` 及其子類別標記 `[MessagePackObject]` 與 `[Key(n)]`
   - `BusinessArgs` / `BusinessResult` 及其子類別為純 POCO，不需序列化標記
   - 合約介面不涉及序列化

5. **程式碼範例**
   - 以 Login 為例完整展示：介面 → API 合約 → BO 參數
   - BO 方法簽章使用 `ILoginRequest` / `ILoginResponse` 作為參數與回傳型別
   - API 路徑：Executor 反序列化為 `LoginRequest` → BO 收到 `ILoginRequest`
   - BO-to-BO 路徑：`new LoginArgs { ... }` → BO 收到 `ILoginRequest`
   - BO 需要額外屬性時：`args is LoginArgs loginArgs` 模式比對

6. **三種使用情境**

   | 情境 | IRequest / IResponse | Request/Response | Args/Result |
   |------|:---:|:---:|:---:|
   | API 方法，BO 不需額外屬性 | ✅ | ✅ | ❌ |
   | API 方法，BO 需額外屬性 | ✅ | ✅ | ✅ |
   | BO-only 方法（不公開至 API） | ❌ | ❌ | ✅ |

   第三種情境不需介面與 Request，Args 獨立存在即可。

7. **注意事項**
   - 現有型別的重新命名與搬遷屬於後續重構範圍，本文件僅定義新開發的規範
   - ExecFunc 模式（ParameterCollection）不受此規範影響，維持現行做法

### 參考檔案

| 檔案 | 用途 |
|------|------|
| `docs/architecture-overview.md` / `.zh-TW.md` | 現有架構文件，確認一致性 |
| `src/Bee.Api.Contracts/BusinessArgs.cs` | 現有基底類別（未來搬至 Bee.Business） |
| `src/Bee.Api.Contracts/System/LoginArgs.cs` | 現有範例型別 |
| `src/Bee.Api.Core/JsonRpc/JsonRpcExecutor.cs` | 反序列化邏輯 |

## 驗證方式

- [ ] 範例程式碼邏輯正確（不實際建置）
- [ ] 與 `architecture-overview.md` / `.zh-TW.md` 無矛盾
