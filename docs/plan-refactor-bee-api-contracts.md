# Bee.Api.Contracts 命名空間重構計畫

## 目標

將 `Bee.Api.Contracts` 所有 `.cs` 檔案的命名空間調整為與資料夾結構對應，
同時移除語意不明的 `Common/` 資料夾與非 .NET 慣例的 `Interface/` 資料夾，
並將同一檔案內的多個型別拆分為各自獨立的 `.cs` 檔案。

---

## 一、資料夾與命名空間對應

| 資料夾 | 命名空間 |
|--------|---------|
| 專案根目錄 | `Bee.Api.Contracts` |
| `System/` | `Bee.Api.Contracts.System` |

---

## 二、各資料夾異動明細

### 2.1 專案根目錄（`Bee.Api.Contracts`）

**從 `Common/` 移入（namespace 不變）：**

| 原始路徑 | 異動 |
|---------|------|
| `Common/BusinessArgs.cs` | 移至根目錄，namespace 不變 |
| `Common/BusinessResult.cs` | 移至根目錄，namespace 不變 |
| `Common/ExecFunc.cs` | **拆出**為 2 個獨立檔案，移至根目錄，namespace 不變 |

`Common/ExecFunc.cs` 拆出：

| 新檔名 | 型別 |
|--------|------|
| `ExecFuncArgs.cs` | `ExecFuncArgs` |
| `ExecFuncResult.cs` | `ExecFuncResult` |

**從 `Interface/` 移入（namespace 不變）：**

| 原始路徑 | 異動 |
|---------|------|
| `Interface/IBusinessObject.cs` | 移至根目錄，namespace 不變 |
| `Interface/IFormBusinessObject.cs` | 移至根目錄，namespace 不變 |
| `Interface/ISystemBusinessObject.cs` | 移至根目錄，namespace 不變，補 `using Bee.Api.Contracts.System;` |

> `ISystemBusinessObject` 的方法簽章參考 `CreateSessionArgs/Result`、`GetDefineArgs/Result`、`SaveDefineArgs/Result`，
> 這些型別移入 `Bee.Api.Contracts.System` 後需補上對應 using。

---

### 2.2 `System/`（`Bee.Api.Contracts.System`）

原 8 個檔案全數拆分為各自獨立 `.cs`，namespace 統一改為 `Bee.Api.Contracts.System`：

| 原始檔案 | 拆出的新檔案 | 型別 |
|---------|------------|------|
| `CheckPackageUpdate.cs` | `PackageDelivery.cs` | `PackageDelivery`（enum） |
| | `CheckPackageUpdateArgs.cs` | `CheckPackageUpdateArgs` |
| | `CheckPackageUpdateResult.cs` | `CheckPackageUpdateResult` |
| | `PackageUpdateQuery.cs` | `PackageUpdateQuery` |
| | `PackageUpdateInfo.cs` | `PackageUpdateInfo` |
| `CreateSession.cs` | `CreateSessionArgs.cs` | `CreateSessionArgs` |
| | `CreateSessionResult.cs` | `CreateSessionResult` |
| `GetCommonConfiguration.cs` | `GetCommonConfigurationArgs.cs` | `GetCommonConfigurationArgs` |
| | `GetCommonConfigurationResult.cs` | `GetCommonConfigurationResult` |
| `GetDefine.cs` | `GetDefineArgs.cs` | `GetDefineArgs` |
| | `GetDefineResult.cs` | `GetDefineResult` |
| `GetPackage.cs` | `GetPackageArgs.cs` | `GetPackageArgs` |
| | `GetPackageResult.cs` | `GetPackageResult` |
| `Login.cs` | `LoginArgs.cs` | `LoginArgs` |
| | `LoginResult.cs` | `LoginResult` |
| `Ping.cs` | `PingArgs.cs` | `PingArgs` |
| | `PingResult.cs` | `PingResult` |
| `SaveDefine.cs` | `SaveDefineArgs.cs` | `SaveDefineArgs` |
| | `SaveDefineResult.cs` | `SaveDefineResult` |

---

## 三、移除的資料夾

| 資料夾 | 原因 |
|--------|------|
| `Common/` | 語意不明，3 個檔案移至根目錄 |
| `Interface/` | 非 .NET 慣例，3 個介面移至根目錄 |

---

## 四、影響範圍

### Bee.Api.Contracts 內部

- 新建檔案（拆出）：21 個（根目錄 2 個 + `System/` 19 個）
- 移動檔案：5 個（`Common/` 2 個、`Interface/` 3 個）
- 補 `using Bee.Api.Contracts.System;`：`ISystemBusinessObject.cs`（1 個）
- 刪除原始檔案：9 個（`ExecFunc.cs` + `System/` 原 8 個）
- 移除空資料夾：`Common/`、`Interface/`

### 下游專案（需補 `using Bee.Api.Contracts.System;`）

使用 System 合約型別（`LoginArgs/Result`、`PingArgs/Result`、`CreateSessionArgs/Result` 等）的檔案：

| 專案 | 檔案 |
|------|------|
| `src/Bee.Connect` | `Connector/SystemApiConnector.cs`、`Connector/FormApiConnector.cs` |
| `src/Bee.Business` | 視使用情況確認 |
| `tests/Bee.Api.Core.UnitTests` | `ApiCoreTest.cs`、`MessagePackTests.cs`、`MessagePackContractsTests.cs` |
| `tests/Bee.Business.UnitTests` | `BusinessTest.cs` |
| `tests/Bee.Api.AspNetCore.UnitTests` | `ApiAspNetCoreTest.cs` |
| `samples/Custom.Contracts` | 視使用情況確認 |

> 僅使用 `BusinessArgs`、`BusinessResult`、`IBusinessObject` 等基底型別的檔案**不受影響**，
> 無需修改 using。

---

## 五、執行順序

1. 拆分 `System/*.cs`，每個型別獨立為一個 `.cs`，namespace 改為 `Bee.Api.Contracts.System`
2. 拆分 `Common/ExecFunc.cs` 為 `ExecFuncArgs.cs`、`ExecFuncResult.cs`，移至根目錄
3. 移動 `Common/BusinessArgs.cs`、`Common/BusinessResult.cs` 至根目錄
4. 移動 `Interface/*.cs` 至根目錄，`ISystemBusinessObject.cs` 補 `using Bee.Api.Contracts.System;`
5. 刪除原始多型別檔案及空資料夾
6. 執行 `dotnet build src/Bee.Api.Contracts/Bee.Api.Contracts.csproj` 確認套件本身無錯誤
7. 逐一更新下游專案（補 `using Bee.Api.Contracts.System;`）
8. 執行 `dotnet build` 全方案建置確認
9. 執行 `dotnet test` 確認測試全數通過
