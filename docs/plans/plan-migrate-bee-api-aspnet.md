# Bee.Api.AspNet 移植計畫 — 移至獨立 Repo

## Context

依據 `docs/plans/plan-migrate-projects.md` 第 4 順位。此專案為 .NET Framework 4.8 的 ASP.NET HttpModule，需在 **Windows + Visual Studio** 環境執行移植。

## 現況

- **專案路徑**：`src/Bee.Api.AspNet/`
- **目標框架**：`net48`（.NET Framework 4.8）
- **ProjectReference**：`Bee.Api.Core`（已發布 NuGet 4.0.1）
- **PackageReference**：`Microsoft.CSharp` 4.7.0、`System.Data.DataSetExtensions` 4.5.0
- **GAC 參考**：`System.Web`
- **不在 slnx 中**，不在 CI workflow 中，無對應測試專案
- **檔案清單**（1 個 .cs）：
  - `HttpModules/ApiServiceModule.cs`

## 相依分析

`ApiServiceModule.cs` 使用的 namespace：
- `Bee.Api.Core`、`Bee.Api.Core.Authorization`、`Bee.Api.Core.JsonRpc`
- `Bee.Base`、`Bee.Base.Serialization`
- `System`、`System.IO`、`System.Web`

移植後改用 NuGet 套件 `Bee.Api.Core`（4.0.1），其傳遞依賴會自動引入 `Bee.Base` 等套件。

## 執行步驟

### 步驟 1：建立新 Repo 目錄結構

```
bee-api-aspnet/
├── .editorconfig            ← 從 bee-library 複製
├── .gitignore               ← .NET gitignore
├── Directory.Build.props    ← 根目錄，套件 metadata + CI 設定
├── LICENSE.txt              ← 從 bee-library 複製
├── bee.png                  ← 從 bee-library 複製
├── README.md                ← 新建
├── Bee.Api.AspNet.slnx      ← 新建 solution
├── src/
│   └── Bee.Api.AspNet/
│       ├── Bee.Api.AspNet.csproj
│       └── HttpModules/
│           └── ApiServiceModule.cs
└── .github/
    └── workflows/
        ├── build-ci.yml
        └── nuget-publish.yml
```

### 步驟 2：修改 csproj — ProjectReference 改為 PackageReference

```xml
<!-- 原本 -->
<ProjectReference Include="..\Bee.Api.Core\Bee.Api.Core.csproj" />

<!-- 改為 -->
<PackageReference Include="Bee.Api.Core" Version="4.0.1" />
```

### 步驟 3：建立根目錄 Directory.Build.props

- 合併 bee-library 的套件 metadata + CI 最佳化設定
- `RepositoryUrl` / `PackageProjectUrl` 改為 `https://github.com/jeff377/bee-api-aspnet`
- 資源檔路徑調整為相對於根目錄

### 步驟 4：建立 GitHub Actions workflows

- **build-ci.yml**：觸發 push/PR to main，Runner 使用 `windows-latest`（net48 需要 Windows）
- **nuget-publish.yml**：觸發 push tag `v*`，Runner 使用 `windows-latest`

> 注意：net48 專案需要 Windows 環境建置，CI runner 必須使用 `windows-latest`。

### 步驟 5：驗證建置（Windows 環境）

```bash
dotnet restore
dotnet build src/Bee.Api.AspNet/Bee.Api.AspNet.csproj --configuration Release
dotnet pack src/Bee.Api.AspNet/Bee.Api.AspNet.csproj --configuration Release --output ./nupkgs
```

### 步驟 6：建立 GitHub Repo 並推送

```bash
git init
git add .
git commit -m "Initial commit: migrate Bee.Api.AspNet from bee-library"
gh repo create jeff377/bee-api-aspnet --public --source=. --push
```

### 步驟 7：從 bee-library 刪除已移出的檔案

- 刪除 `src/Bee.Api.AspNet/` 目錄
- 更新 `docs/plans/plan-migrate-projects.md` 標記為已完成

## 注意事項

- 此專案依賴 `System.Web`（ASP.NET Classic），僅能在 net48 上運行
- CI runner 必須使用 `windows-latest`
- 目前不打 tag、不發布 NuGet，待確認命名空間調整需求後再處理
