# Bee.Repository 命名空間重構計畫

## 目標

將 `Bee.Repository` 所有 `.cs` 檔案的命名空間由統一的 `Bee.Repository` 調整為與資料夾結構對應的子命名空間，遵循與 `Bee.Base`、`Bee.Db`、`Bee.Repository.Abstractions` 相同的重構模式。

---

## 一、資料夾與命名空間對應

| 資料夾 | 現有命名空間 | 重構後命名空間 |
|--------|------------|--------------|
| `Form/` | `Bee.Repository` | `Bee.Repository.Form` |
| `Provider/` | `Bee.Repository` | `Bee.Repository.Provider` |
| `System/` | `Bee.Repository` | `Bee.Repository.System` |

---

## 二、各資料夾異動明細

### 2.1 `Form/`（`Bee.Repository.Form`）

| 檔案 | 異動 |
|------|------|
| `Form/DataFormRepository.cs` | namespace 改為 `Bee.Repository.Form` |
| `Form/ReportFormRepository.cs` | namespace 改為 `Bee.Repository.Form` |

### 2.2 `Provider/`（`Bee.Repository.Provider`）

| 檔案 | 異動 |
|------|------|
| `Provider/FormRepositoryProvider.cs` | namespace 改為 `Bee.Repository.Provider` |
| `Provider/SystemRepositoryProvider.cs` | namespace 改為 `Bee.Repository.Provider` |

### 2.3 `System/`（`Bee.Repository.System`）

| 檔案 | 異動 |
|------|------|
| `System/DatabaseRepository.cs` | namespace 改為 `Bee.Repository.System` |
| `System/SessionRepository.cs` | namespace 改為 `Bee.Repository.System` |

---

## 三、影響範圍

### Bee.Repository 內部

- 修改命名空間宣告：6 個 `.cs` 檔案
- 無檔案搬移、無資料夾改名
- 無新建檔案

### 下游專案（需補 `using`）

調查結果：下游專案（`Bee.Business`、`samples/JsonRpcServer`、`tools/`）均引用的是 `Bee.Repository.Abstractions`，**不直接引用** `Bee.Repository` 命名空間。

唯一直接引用 `Bee.Repository` 的是：

| 檔案 | 需新增的 using |
|------|--------------|
| `tests/Bee.Repository.UnitTests/SessionRepositoryTests.cs` | `using Bee.Repository.System;`（使用 `SessionRepository`） |

> `GlobalCollection.cs` 使用 `Bee.Repository.UnitTests` 命名空間，不受影響。

---

## 四、執行順序

1. 修改 `Bee.Repository` 內部 6 個檔案的命名空間宣告
2. 執行 `dotnet build src/Bee.Repository/Bee.Repository.csproj` 確認專案本身無錯誤
3. 更新 `tests/Bee.Repository.UnitTests/SessionRepositoryTests.cs` 的 `using`
4. 執行 `dotnet build` 全方案建置確認
5. 執行 `dotnet test tests/Bee.Repository.UnitTests/Bee.Repository.UnitTests.csproj` 確認測試通過
