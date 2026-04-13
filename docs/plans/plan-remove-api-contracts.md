# 計畫：消除 Bee.Api.Contracts 專案

## 背景

`Bee.Business`（商業邏輯層）目前引用 `Bee.Api.Contracts`（API 層）來取得 `BusinessArgs`/`BusinessResult` 基底類別和所有 `XxxArgs`/`XxxResult` 型別。這違反了 Clean Architecture 的向內相依原則。

根據 `docs/api-bo-contract-separation.md` 設計的五層架構，合約介面（`IXxxRequest`/`IXxxResponse`）已建立於 `Bee.Definition/Api/`，API 型別（`XxxRequest`/`XxxResponse`）已建立於 `Bee.Api.Core/System/`，純 POCO 基底（`BusinessArgs`/`BusinessResult`）已建立於 `Bee.Business/`。剩下的工作就是在 `Bee.Business` 中建立 BO 型別、切換所有引用、並移除 `Bee.Api.Contracts`。

## 目標架構

```
Bee.Definition          <-- 合約介面 + 共用型別
    |
    +-- Bee.Api.Core    <-- API 型別（含序列化）+ ExecFuncRequest/Response
    |       +-- Bee.Api.Client
    |
    +-- Bee.Business    <-- BO 型別（純 POCO）+ ExecFuncArgs/Result
```

`Bee.Api.Contracts` 整個專案移除。

---

## 實施步驟

### 階段 1：補齊 BO 層基礎

1. **修改 `src/Bee.Business/BusinessArgs.cs`**：加入 `ParameterCollection Parameters { get; set; }` 屬性（不帶 MessagePack 標記），與舊版 `Bee.Api.Contracts.BusinessArgs` 功能對齊
2. **修改 `src/Bee.Business/BusinessResult.cs`**：同上
3. **新增 `src/Bee.Business/ExecFuncArgs.cs`**：繼承 `BusinessArgs`，含 `FuncId` 屬性，純 POCO
4. **新增 `src/Bee.Business/ExecFuncResult.cs`**：繼承 `BusinessResult`，空類別

### 階段 2：建立 System BO 型別

在 `src/Bee.Business/System/` 目錄下新增 16 個檔案（8 組），每個繼承 `BusinessArgs`/`BusinessResult` 並實作合約介面：

- LoginArgs / LoginResult
- CreateSessionArgs / CreateSessionResult
- PingArgs / PingResult
- GetDefineArgs / GetDefineResult
- SaveDefineArgs / SaveDefineResult
- GetCommonConfigurationArgs / GetCommonConfigurationResult
- CheckPackageUpdateArgs / CheckPackageUpdateResult
- GetPackageArgs / GetPackageResult

### 階段 3：建立 ExecFunc API 型別

- **新增 `src/Bee.Api.Core/ExecFuncRequest.cs`**：繼承 `ApiRequest`，`[MessagePackObject]`，`[Key(100)] FuncId`
- **新增 `src/Bee.Api.Core/ExecFuncResponse.cs`**：繼承 `ApiResponse`，`[MessagePackObject]`

### 階段 4：更新 BO 介面與實作

修改 `Bee.Business` 中所有引用 `Bee.Api.Contracts` 的檔案，改用 `Bee.Business` / `Bee.Business.System` / `Bee.Definition.Api` 的型別：

- `IBusinessObject.cs` — ExecFunc 改用 `Bee.Business` 版本
- `ISystemBusinessObject.cs` — 方法簽章改用合約介面（`ICreateSessionRequest` 等）
- `BusinessObject.cs`、`SystemBusinessObject.cs`、`FormBusinessObject.cs`
- `BusinessFunc.cs`、`SystemExecFuncHandler.cs`、`FormExecFuncHandler.cs`

### 階段 5：Executor 輸入參數轉換

- **新增 `src/Bee.Api.Core/ApiInputConverter.cs`**：通用屬性複製轉換器，當 API 層反序列化出的 `XxxRequest` 型別與 BO 方法參數型別不匹配時，自動複製屬性建立目標型別實例
- **修改 `src/Bee.Api.Core/JsonRpc/JsonRpcExecutor.cs`**：在 `method.Invoke` 前加入參數型別檢查與轉換（第 142 行附近）

### 階段 6：更新 Bee.Api.Client

- `Connectors/SystemApiConnector.cs` — `ExecFuncArgs`/`ExecFuncResult` 改為 `ExecFuncRequest`/`ExecFuncResponse`
- `Connectors/FormApiConnector.cs` — 同上

### 階段 7：更新專案引用與白名單

- `src/Bee.Business/Bee.Business.csproj` — 移除 `Bee.Api.Contracts` 引用，加入 `Bee.Definition` 直接引用
- `src/Bee.Api.Core/Bee.Api.Core.csproj` — 移除 `Bee.Api.Contracts` 引用，加入 `Bee.Definition` 直接引用
- `src/Bee.Base/SysInfo.cs` — 白名單中 `"Bee.Api.Contracts"` 替換為 `"Bee.Api.Core"`, `"Bee.Business"`

### 階段 8：更新測試

更新所有引用 `Bee.Api.Contracts` 的測試檔案的 using 和型別參考。

### 階段 9：移除 Bee.Api.Contracts

- 從 `Bee.Library.slnx` 移除 `Bee.Api.Contracts` 和 `Bee.Api.Contracts.UnitTests`
- 刪除 `src/Bee.Api.Contracts/` 目錄
- 刪除 `tests/Bee.Api.Contracts.UnitTests/` 目錄

### 階段 10：驗證

```bash
dotnet build Bee.Library.slnx --configuration Release
dotnet test Bee.Library.slnx --configuration Release
```

確認 `Bee.Business.csproj` 不再引用任何 `Bee.Api.*` 專案。

---

## 關鍵檔案

| 檔案 | 操作 |
|------|------|
| `src/Bee.Business/BusinessArgs.cs` | 修改（加入 ParameterCollection） |
| `src/Bee.Business/BusinessResult.cs` | 修改（同上） |
| `src/Bee.Business/ExecFuncArgs.cs` | 新增 |
| `src/Bee.Business/ExecFuncResult.cs` | 新增 |
| `src/Bee.Business/System/*.cs` | 新增（16 檔） |
| `src/Bee.Api.Core/ExecFuncRequest.cs` | 新增 |
| `src/Bee.Api.Core/ExecFuncResponse.cs` | 新增 |
| `src/Bee.Api.Core/ApiInputConverter.cs` | 新增 |
| `src/Bee.Api.Core/JsonRpc/JsonRpcExecutor.cs` | 修改 |
| `src/Bee.Business/IBusinessObject.cs` | 修改 |
| `src/Bee.Business/ISystemBusinessObject.cs` | 修改 |
| `src/Bee.Business/BusinessObjects/SystemBusinessObject.cs` | 修改（最大變更） |
| `src/Bee.Api.Client/Connectors/SystemApiConnector.cs` | 修改 |
| `src/Bee.Base/SysInfo.cs` | 修改（白名單） |
| `src/Bee.Api.Contracts/` | 刪除 |

## 風險

1. **JsonRpcExecutor 反射呼叫**：API 層反序列化出 `LoginRequest`，但 BO 方法參數現在是 `ILoginRequest`。需要 `ApiInputConverter` 正確轉換，包含 `ParameterCollection` 的傳遞
2. **ExecFuncResult 輸出轉換**：ExecFunc 不走合約介面模式，需擴充 `ApiContractRegistry` 或在 `ApiPayloadConverter` 中特殊處理 ExecFunc 型別的轉換
3. **型別白名單**：反序列化時 `TypeName` 格式為 `"Bee.Api.Contracts.System.LoginArgs, Bee.Api.Contracts"`。移除後用戶端發送的仍是舊型別名稱 — 但實際上用戶端使用的是 `Bee.Api.Core.System.LoginRequest`，不受影響
