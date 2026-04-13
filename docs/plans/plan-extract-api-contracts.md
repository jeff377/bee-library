# 計畫：將合約介面從 Bee.Definition 搬遷至 Bee.Api.Contracts

## 背景

目前 API 合約介面（`IXxxRequest`/`IXxxResponse`）放在 `Bee.Definition/Api/` 目錄下。以 ERP 系統的規模，未來合約介面將超過 100 組（200+ 檔案），繼續放在 `Bee.Definition` 會使該專案職責過於膨脹。

應將合約介面獨立至 `Bee.Api.Contracts` 專案，該專案**只包含純介面與共用 DTO**，不含序列化基底類別或任何實作邏輯。

## 目標架構

```
Bee.Definition              <-- 共用型別（DefineType, ParameterCollection, FormSchema 等）
    |
    +-- Bee.Api.Contracts   <-- 純介面 + 共用 DTO（ILoginRequest, PackageUpdateInfo 等）
    |       |
    |       +-- Bee.Api.Core    <-- API 型別（LoginRequest, 含 MessagePack 序列化）
    |       |       +-- Bee.Api.Client
    |       |
    |       +-- Bee.Business    <-- BO 型別（LoginArgs, 純 POCO）
```

**與舊版 Bee.Api.Contracts 的差異**：舊版包含 `BusinessArgs`/`BusinessResult` 帶 MessagePack 序列化的基底類別，導致 `Bee.Business` 耦合 API 層序列化邏輯。新版只放純介面和共用 DTO，不含任何序列化基底。

## 搬遷範圍

### 從 `Bee.Definition/Api/` 搬至 `Bee.Api.Contracts/`

**合約介面（16 檔）：**

| 檔案 | 型別 |
|------|------|
| `ILoginRequest.cs` | 介面 |
| `ILoginResponse.cs` | 介面 |
| `IPingRequest.cs` | 介面 |
| `IPingResponse.cs` | 介面 |
| `ICreateSessionRequest.cs` | 介面 |
| `ICreateSessionResponse.cs` | 介面 |
| `IGetDefineRequest.cs` | 介面 |
| `IGetDefineResponse.cs` | 介面 |
| `ISaveDefineRequest.cs` | 介面 |
| `ISaveDefineResponse.cs` | 介面 |
| `IGetCommonConfigurationRequest.cs` | 介面 |
| `IGetCommonConfigurationResponse.cs` | 介面 |
| `ICheckPackageUpdateRequest.cs` | 介面 |
| `ICheckPackageUpdateResponse.cs` | 介面 |
| `IGetPackageRequest.cs` | 介面 |
| `IGetPackageResponse.cs` | 介面 |

**共用 DTO（3 檔）：**

| 檔案 | 型別 | 說明 |
|------|------|------|
| `PackageDelivery.cs` | enum | 套件下載方式（Url/Api） |
| `PackageUpdateQuery.cs` | class（含 MessagePack） | 更新查詢項目 |
| `PackageUpdateInfo.cs` | class（含 MessagePack） | 更新資訊 |

### 新增 ExecFunc 合約介面至 `Bee.Api.Contracts/`

ExecFunc 目前沒有合約介面，API 層（`ExecFuncRequest`/`ExecFuncResponse`）與 BO 層（`ExecFuncArgs`/`ExecFuncResult`）之間靠 `ApiInputConverter` 的反射屬性複製轉換。新增合約介面後可統一走 `ApiContractRegistry` 路徑。

**新增介面（2 檔）：**

| 檔案 | 型別 | 屬性 |
|------|------|------|
| `IExecFuncRequest.cs` | 介面 | `string FuncId { get; }` |
| `IExecFuncResponse.cs` | 介面 | （空，僅作為標記合約） |

**同步修改：**
- `src/Bee.Business/ExecFuncArgs.cs`：加入 `IExecFuncRequest` 實作
- `src/Bee.Business/ExecFuncResult.cs`：加入 `IExecFuncResponse` 實作
- `src/Bee.Api.Core/ExecFuncRequest.cs`：加入 `IExecFuncRequest` 實作
- `src/Bee.Api.Core/ExecFuncResponse.cs`：加入 `IExecFuncResponse` 實作
- `src/Bee.Api.Core/ApiContractRegistry.cs` 或啟動註冊處：註冊 `IExecFuncResponse` → `ExecFuncResponse` 對應

## 實施步驟

### 步驟 1：建立 Bee.Api.Contracts 專案

新增 `src/Bee.Api.Contracts/Bee.Api.Contracts.csproj`：
- 目標框架：`netstandard2.0;net10.0`
- 引用 `Bee.Definition`（因介面使用 `DefineType`、`List<PackageUpdateQuery>` 等型別）
- 引用 `MessagePack`（因 `PackageUpdateQuery`、`PackageUpdateInfo` 帶有 MessagePack 標記）

### 步驟 2：搬移檔案

將 `src/Bee.Definition/Api/` 下全部 19 個檔案搬至 `src/Bee.Api.Contracts/`。

命名空間從 `Bee.Definition.Api` 改為 `Bee.Api.Contracts`。

### 步驟 3：更新專案引用

| 專案 | 變更 |
|------|------|
| `Bee.Api.Core.csproj` | 加入 `Bee.Api.Contracts` 引用 |
| `Bee.Business.csproj` | 加入 `Bee.Api.Contracts` 引用 |

`Bee.Api.Core` 和 `Bee.Business` 原本已引用 `Bee.Definition`，現在額外引用 `Bee.Api.Contracts`。

### 步驟 4：全域替換命名空間

將所有 `using Bee.Definition.Api` 替換為 `using Bee.Api.Contracts`。

影響範圍：
- `src/Bee.Api.Core/System/` 下約 16 個 Request/Response 檔案
- `src/Bee.Business/System/` 下約 16 個 Args/Result 檔案
- `src/Bee.Api.Core/ApiContractRegistry.cs`（若有引用）
- `tests/` 下約 3 個測試檔案

### 步驟 5：更新方案檔與 CI/CD

- `Bee.Library.slnx`：加入 `Bee.Api.Contracts` 專案
- `.github/workflows/build-ci.yml`：加入 pack 步驟
- `.github/workflows/nuget-publish.yml`：加入 build、pack 步驟與 release notes

### 步驟 6：刪除 Bee.Definition/Api/ 目錄

確認無殘留引用後，刪除 `src/Bee.Definition/Api/` 目錄。

### 步驟 7：驗證

```bash
dotnet build Bee.Library.slnx --configuration Release
dotnet test Bee.Library.slnx --configuration Release
```

### 步驟 8：更新架構文件

更新 `docs/api-bo-contract-separation.md`，將合約介面所在組件從 `Bee.Definition` 修正為 `Bee.Api.Contracts`。

## 變更摘要

| 類別 | 數量 |
|------|------|
| 新增專案 | 1（`Bee.Api.Contracts`） |
| 搬移檔案 | 19（從 `Bee.Definition/Api/` 至 `Bee.Api.Contracts/`） |
| 新增介面 | 2（`IExecFuncRequest`、`IExecFuncResponse`） |
| 修改實作類別 | 4（ExecFuncArgs/Result、ExecFuncRequest/Response 加入介面實作） |
| 修改命名空間（using） | 約 35 個檔案 |
| 修改專案引用（csproj） | 2 |
| 修改 CI/CD（yml） | 2 |
| 修改方案檔（slnx） | 1 |
| 刪除目錄 | 1（`Bee.Definition/Api/`） |

## 風險

- **風險極低**：本次只是搬移純介面檔案並變更命名空間，不涉及任何邏輯或架構變更
- **NuGet 相容性**：新版 `Bee.Definition` 將不再包含 `Bee.Definition.Api` 命名空間下的型別。若有外部消費者直接使用這些型別，需升級引用 `Bee.Api.Contracts` 套件
