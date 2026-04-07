# Bee.Business 命名空間重構計畫

## 目標

將 Bee.Business 所有 `.cs` 檔案的命名空間由統一的 `Bee.Business` 調整為與資料夾結構對應，
同時移除語意不明的 `Common/`、`Interface/`、`Attribute/` 資料夾。

---

## 一、資料夾與命名空間對應

| 資料夾 | 命名空間 |
|--------|---------|
| 專案根目錄 | `Bee.Business` |
| `BusinessObject/` | `Bee.Business.BusinessObjects` |
| `Provider/` | `Bee.Business.Provider` |
| `Validator/` | `Bee.Business.Validator` |

---

## 二、各資料夾異動明細

### 2.1 專案根目錄（`Bee.Business`）

**從 `Common/` 移入：**

| 原始路徑 | 異動 |
|---------|------|
| `Common/BusinessFunc.cs` | 移至根目錄，namespace 不變（`Bee.Business`） |

**從 `Attribute/` 移入（僅單一類別，不建立獨立命名空間）：**

| 原始路徑 | 異動 |
|---------|------|
| `Attribute/ExecFuncAccessControlAttribute.cs` | 移至根目錄，namespace 不變（`Bee.Business`） |

**刪除：**

| 原始路徑 | 原因 |
|---------|------|
| `Common/Common.cs` | 空檔案，無實質內容 |

---

### 2.2 `BusinessObject/`（`Bee.Business.BusinessObjects`）

| 檔案 | 異動 |
|------|------|
| `BusinessObject/BusinessObject.cs` | namespace 改為 `Bee.Business.BusinessObjects` |
| `BusinessObject/BusinessObjectProvider.cs` | namespace 改為 `Bee.Business.BusinessObjects` |
| `BusinessObject/FormBusinessObject.cs` | namespace 改為 `Bee.Business.BusinessObjects` |
| `BusinessObject/FormExecFuncHandler.cs` | namespace 改為 `Bee.Business.BusinessObjects` |
| `BusinessObject/SystemBusinessObject.cs` | namespace 改為 `Bee.Business.BusinessObjects` |
| `BusinessObject/SystemExecFuncHandler.cs` | namespace 改為 `Bee.Business.BusinessObjects` |

**從 `Interface/` 移入：**

| 原始路徑 | 異動 |
|---------|------|
| `Interface/IExecFuncHandler.cs` | 移至 `BusinessObject/`，namespace 改為 `Bee.Business.BusinessObjects` |

---

### 2.3 `Provider/`（`Bee.Business.Provider`）

| 檔案 | 異動 |
|------|------|
| `Provider/CacheDataSourceProvider.cs` | namespace 改為 `Bee.Business.Provider` |
| `Provider/DynamicApiEncryptionKeyProvider.cs` | namespace 改為 `Bee.Business.Provider` |
| `Provider/StaticApiEncryptionKeyProvider.cs` | namespace 改為 `Bee.Business.Provider` |

---

### 2.4 `Validator/`（`Bee.Business.Validator`）

| 檔案 | 異動 |
|------|------|
| `Validator/AccessTokenValidationProvider.cs` | namespace 改為 `Bee.Business.Validator` |

---

## 三、移除的資料夾

| 資料夾 | 原因 |
|--------|------|
| `Common/` | 語意不明；`BusinessFunc.cs` 移至根目錄，`Common.cs` 為空檔案直接刪除 |
| `Interface/` | 非 .NET 慣例；`IExecFuncHandler` 移至實作所在的 `BusinessObject/` |
| `Attribute/` | 僅單一類別，不值得獨立命名空間；`ExecFuncAccessControlAttribute` 移至根目錄 |

---

## 四、影響範圍

### Bee.Business 內部
- 修改命名空間宣告：10 個 `.cs`
- 移動檔案：`Common/BusinessFunc.cs` → 根目錄、`Attribute/ExecFuncAccessControlAttribute.cs` → 根目錄、`Interface/IExecFuncHandler.cs` → `BusinessObject/`
- 刪除檔案：`Common/Common.cs`（空檔案）
- 移除空資料夾：`Common/`、`Interface/`、`Attribute/`

### 下游專案（需補 `using`）

| 專案 | 說明 |
|------|------|
| `tests/Bee.Business.UnitTests` | 測試專案，需依實際使用的型別補上對應 `using` |
| `samples/Custom.Business` | 範例專案（2 個檔案使用 `using Bee.Business;`），需補上對應 `using` |

---

## 五、執行順序

1. 修改 Bee.Business 內部所有命名空間宣告與檔案位置
2. 執行 `dotnet build src/Bee.Business/Bee.Business.csproj --configuration Release` 確認專案本身無錯誤
3. 逐一更新下游專案的 `using` 陳述式（`tests/Bee.Business.UnitTests`、`samples/Custom.Business`）
4. 執行 `dotnet build --configuration Release` 全方案建置確認
5. 執行 `dotnet test --configuration Release` 確認測試全數通過
