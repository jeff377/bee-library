# Bee.Api.AspNetCore 命名空間重構計畫

## 目標

將 `Bee.Api.AspNetCore` 所有 `.cs` 檔案的命名空間由統一的 `Bee.Api.AspNetCore`
調整為與資料夾結構對應，模式與 `plan-refactor-bee-base.md` 一致。

---

## 一、資料夾與命名空間對應

| 資料夾 | 目前命名空間 | 目標命名空間 |
|--------|------------|------------|
| `Controllers/` | `Bee.Api.AspNetCore` | `Bee.Api.AspNetCore.Controllers` |

---

## 二、各檔案異動明細

### 主專案：`src/Bee.Api.AspNetCore`

| 檔案 | 異動 |
|------|------|
| `Controllers/ApiServiceController.cs` | namespace 改為 `Bee.Api.AspNetCore.Controllers` |

---

## 三、下游影響

### 3.1 `samples/JsonRpcServer/Controllers/ApiServiceController.cs`

目前使用完整限定名稱繼承基底類別：

```csharp
// 修改前
public class ApiServiceController : Bee.Api.AspNetCore.ApiServiceController

// 修改後
public class ApiServiceController : Bee.Api.AspNetCore.Controllers.ApiServiceController
```

### 3.2 `tests/Bee.Api.AspNetCore.UnitTests/ApiAspNetCoreTest.cs`

目前使用部分限定名稱繼承：

```csharp
// 修改前（部分限定名稱）
public class ApiServiceController : AspNetCore.ApiServiceController { }

// 修改後（加 using，改用短名稱）
using Bee.Api.AspNetCore.Controllers;
...
public class ApiServiceController : Controllers.ApiServiceController { }
```

> 或直接改為完整限定名稱：`Bee.Api.AspNetCore.Controllers.ApiServiceController`

---

## 四、影響範圍總覽

| 類型 | 數量 |
|------|------|
| 修改命名空間宣告 | 1 個 `.cs` |
| 下游需更新的檔案 | 2 個 `.cs` |

---

## 五、執行順序

1. 修改 `Controllers/ApiServiceController.cs` 的命名空間為 `Bee.Api.AspNetCore.Controllers`
2. 執行 `dotnet build src/Bee.Api.AspNetCore/Bee.Api.AspNetCore.csproj` 確認主專案無誤
3. 更新 `samples/JsonRpcServer/Controllers/ApiServiceController.cs` 的完整限定名稱
4. 更新 `tests/Bee.Api.AspNetCore.UnitTests/ApiAspNetCoreTest.cs` 的繼承參考
5. 執行 `dotnet build` 全方案建置確認
6. 執行 `dotnet test tests/Bee.Api.AspNetCore.UnitTests/Bee.Api.AspNetCore.UnitTests.csproj` 確認測試通過
