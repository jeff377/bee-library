# Bee.NET 專案重命名計畫

## Context

Bee.NET 採用 Definition-Driven Architecture，目前 src/ 中有 4 個套件命名不夠精準，需要調整。範圍僅限 slnx 中 src/ 的 NuGet 套件專案，不含 samples、tests、tools。

## 命名調整清單

| 現名 | 新名 | 理由 |
|------|------|------|
| `Bee.Base` | `Bee.Core` | "Base" 過於泛化，`Core` 符合 .NET 慣例，精準表達框架核心基礎設施 |
| `Bee.Define` | `Bee.Definition` | "Define" 是動詞，套件名應為名詞；與 Definition-Driven Architecture 直接對應 |
| `Bee.Cache` | `Bee.ObjectCaching` | "Cache" 易誤認為通用快取框架；實際快取的是業務物件（定義資料、組織/權限、SessionInfo），ObjectCaching 明確表達應用層物件快取 |
| `Bee.Connect` | `Bee.Api.Client` | 與 `Bee.Api.*` 家族對稱（Contracts/Core/AspNetCore/Client），清楚表達 API 客戶端角色 |

## 不調整的專案

`Bee.Api.Contracts`、`Bee.Api.Core`、`Bee.Api.AspNetCore`、`Bee.Repository.Abstractions`、`Bee.Repository`、`Bee.Db`、`Bee.Business` — 命名均已精準。

## 每個專案的重命名步驟

以 `Bee.Base` → `Bee.Core` 為例，每個專案需要：

1. **重命名目錄**：`src/Bee.Base/` → `src/Bee.Core/`
2. **重命名 .csproj 檔案**：`Bee.Base.csproj` → `Bee.Core.csproj`
3. **更新 .csproj 內容**：若有明確指定 `RootNamespace`、`AssemblyName`、`PackageId` 則同步更新
4. **更新 slnx**：修改 `Bee.Library.slnx` 中的專案路徑
5. **更新所有 ProjectReference**：其他 src/ 專案中引用此專案的 `<ProjectReference>` 路徑
6. **更新 namespace**：所有 `.cs` 檔案中的 `namespace Bee.Base` → `namespace Bee.Core`
7. **更新 using**：所有 src/ 專案中的 `using Bee.Base` → `using Bee.Core`
8. **更新對應測試專案**：
   - 重命名 `tests/Bee.Base.UnitTests/` → `tests/Bee.Core.UnitTests/`
   - 重命名 .csproj 檔案
   - 更新 slnx 中測試專案路徑
   - 更新測試專案中的 ProjectReference 和 using
9. **更新文件**：README.md、CLAUDE.md、`docs/` 中的相關引用

### 特別注意：`Bee.Connect` → `Bee.Api.Client`

此專案移入 `Bee.Api.*` 家族，目錄結構變為 `src/Bee.Api.Client/`，namespace 從 `Bee.Connect` 變為 `Bee.Api.Client`。

## 執行順序

依據相依性由底層往上層調整，避免中間狀態的建置錯誤：

1. `Bee.Base` → `Bee.Core`（最底層，被所有其他專案引用）
2. `Bee.Define` → `Bee.Definition`（依賴 Bee.Core）
3. `Bee.Cache` → `Bee.ObjectCaching`（依賴 Bee.Core + Bee.Definition）
4. `Bee.Connect` → `Bee.Api.Client`（依賴 Bee.Api.Core）

## 更新 CI/CD Workflow

檔案：`.github/workflows/nuget-publish.yml`

1. **移除不在 slnx 中的專案**（Build、Pack、Release body 三處）：
   - `Bee.Api.AspNet`
   - `Bee.UI.Core`
   - `Bee.UI.WinForms`

2. **更新重命名的專案路徑**（Build、Pack 兩處）：
   - `src/Bee.Base/Bee.Base.csproj` → `src/Bee.Core/Bee.Core.csproj`
   - `src/Bee.Define/Bee.Define.csproj` → `src/Bee.Definition/Bee.Definition.csproj`
   - `src/Bee.Cache/Bee.Cache.csproj` → `src/Bee.ObjectCaching/Bee.ObjectCaching.csproj`
   - `src/Bee.Connect/Bee.Connect.csproj` → `src/Bee.Api.Client/Bee.Api.Client.csproj`

3. **更新 Release body** 套件名稱清單，反映新名稱並移除已移出的專案

## 驗證方式

每個專案重命名完成後：
```bash
dotnet build Bee.Library.slnx --configuration Release
dotnet test --configuration Release
```

全部完成後確認：
- 所有專案建置成功（零警告，因為 TreatWarningsAsErrors=true）
- 所有測試通過
- namespace 和 using 無殘留舊名
- `.github/workflows/nuget-publish.yml` 中只包含 slnx 中的專案
