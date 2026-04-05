# Bee.Repository.Abstractions 命名空間重構計畫

## 目標

將 `Bee.Repository.Abstractions` 所有 `.cs` 檔案的命名空間由統一的 `Bee.Repository.Abstractions` 調整為與資料夾結構對應，並消除語意不明的 `Info/` 單一類別資料夾。

---

## 一、資料夾與命名空間對應

| 資料夾 | 命名空間 |
|--------|---------|
| 專案根目錄 | `Bee.Repository.Abstractions` |
| `Form/` | `Bee.Repository.Abstractions.Form` |
| `Provider/` | `Bee.Repository.Abstractions.Provider` |
| `System/` | `Bee.Repository.Abstractions.System` |

---

## 二、各資料夾異動明細

### 2.1 `Info/` → 移入根目錄（僅單一類別，不建立獨立命名空間）

| 原始路徑 | 異動 |
|---------|------|
| `Info/RepositoryInfo.cs` | 移至根目錄，namespace 不變（`Bee.Repository.Abstractions`） |

移入根目錄後刪除空資料夾 `Info/`。

### 2.2 `Form/`（`Bee.Repository.Abstractions.Form`）

| 檔案 | 異動 |
|------|------|
| `Form/IDataFormRepository.cs` | namespace 改為 `Bee.Repository.Abstractions.Form` |
| `Form/IReportFormRepository.cs` | namespace 改為 `Bee.Repository.Abstractions.Form` |

### 2.3 `Provider/`（`Bee.Repository.Abstractions.Provider`）

| 檔案 | 異動 |
|------|------|
| `Provider/IFormRepositoryProvider.cs` | namespace 改為 `Bee.Repository.Abstractions.Provider` |
| `Provider/ISystemRepositoryProvider.cs` | namespace 改為 `Bee.Repository.Abstractions.Provider` |

> `IFormRepositoryProvider` 回傳型別 `IDataFormRepository`、`IReportFormRepository` 需加 `using Bee.Repository.Abstractions.Form;`
> `ISystemRepositoryProvider` 屬性型別 `IDatabaseRepository`、`ISessionRepository` 需加 `using Bee.Repository.Abstractions.System;`

### 2.4 `System/`（`Bee.Repository.Abstractions.System`）

| 檔案 | 異動 |
|------|------|
| `System/IDatabaseRepository.cs` | namespace 改為 `Bee.Repository.Abstractions.System` |
| `System/ISessionRepository.cs` | namespace 改為 `Bee.Repository.Abstractions.System` |

---

## 三、移除的資料夾

| 資料夾 | 原因 |
|--------|------|
| `Info/` | 僅單一類別，不值得獨立命名空間，`RepositoryInfo` 移至根目錄 |

---

## 四、影響範圍

### Bee.Repository.Abstractions 內部
- 修改命名空間宣告：6 個 `.cs`
- 移動檔案：`Info/RepositoryInfo.cs` → 根目錄
- `Provider/` 兩個介面需補 `using` 參照子命名空間型別

### 下游專案（需補 `using`）

| 檔案 | 需補的 using |
|------|------------|
| `Bee.Repository/System/DatabaseRepository.cs` | `Bee.Repository.Abstractions.System` |
| `Bee.Repository/System/SessionRepository.cs` | `Bee.Repository.Abstractions.System` |
| `Bee.Repository/Provider/FormRepositoryProvider.cs` | `Bee.Repository.Abstractions.Form`, `Bee.Repository.Abstractions.Provider` |
| `Bee.Repository/Provider/SystemRepositoryProvider.cs` | `Bee.Repository.Abstractions.System`, `Bee.Repository.Abstractions.Provider` |
| `Bee.Repository/Form/DataFormRepository.cs` | `Bee.Repository.Abstractions.Form` |
| `Bee.Repository/Form/ReportFormRepository.cs` | `Bee.Repository.Abstractions.Form` |
| `Bee.Business/BusinessObject/SystemBusinessObject.cs` | 依使用型別補對應 using |
| `Bee.Business/BusinessObject/SystemExecFuncHandler.cs` | 依使用型別補對應 using |
| `Bee.Business/Provider/CacheDataSourceProvider.cs` | 依使用型別補對應 using |
| `samples/JsonRpcServer/Extensions/BackendExtensions.cs` | 依使用型別補對應 using |
| `tools/BeeDbUpgrade/Common/AppInfo.cs` | 依使用型別補對應 using |
| `tools/BeeSettingsEditor/Common/AppInfo.cs` | 依使用型別補對應 using |

---

## 五、執行順序

1. 修改 `Bee.Repository.Abstractions` 內部（namespace 調整、`RepositoryInfo.cs` 移至根目錄）
2. `dotnet build src/Bee.Repository.Abstractions/Bee.Repository.Abstractions.csproj` — 確認套件本身無錯誤
3. 逐一更新下游 `Bee.Repository`、`Bee.Business`、samples、tools 的 `using`
4. `dotnet build` 全方案建置確認
5. `dotnet test` 確認測試全數通過
