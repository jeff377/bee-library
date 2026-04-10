# Bee.UI.WinForms 移植計畫 — 移至獨立 Repo

## Context

依據 `docs/plans/plan-migrate-projects.md` 第 6 順位（最後一個）。此專案為 WinForms UI 元件庫，含 `.Designer.cs` 和 `.resx` 設計工具檔案，需在 **Windows + Visual Studio** 環境執行移植。

## 前置條件

- **Bee.UI.Core 必須先發布 NuGet 套件**（目前已移植至 bee-ui-core repo，但尚未打 tag 發布）
- 待 Bee.UI.Core 完成命名空間調整並發布 NuGet 後，才能開始此專案的移植

## 現況

- **專案路徑**：`src/Bee.UI.WinForms/`
- **目標框架**：`net10.0-windows`
- **ProjectReference**：`Bee.UI.Core`（尚未發布 NuGet）
- **不在 slnx 中**，不在 CI workflow 中，無對應測試專案
- **檔案清單**（14 個 .cs + 1 個 .resx = 15 個檔案）：
  - `BaseForm/BaseForm.cs`
  - `Common/ApplicationExceptionHandler.cs`、`UIFunc.cs`、`UIInfo.cs`、`UIViewService.cs`
  - `Controls/BeePropertyGrid.cs`
  - `Controls/TreeView/BeeTreeView.cs`、`BeeTreeViewBuilder.cs`
  - `Editor/CollectionEditor.cs`
  - `Event/ObjectTreeNodeCreatedEvent.cs`、`ObjectTreeNodeCreatingEvent.cs`
  - `Forms/ApiConnectForm.cs`、`ApiConnectForm.Designer.cs`、`ApiConnectForm.resx`
  - `Interface/ICollectionEditorNotify.cs`
  - `Tracing/FormTraceWriter.cs`、`ITraceDisplayForm.cs`

## 相依分析

源碼中使用的 Bee.NET namespace：

| Namespace | 對應 NuGet 套件 |
|-----------|----------------|
| `Bee.Base`、`Bee.Base.Attributes`、`Bee.Base.Collections`、`Bee.Base.Tracing`、`Bee.Base.Serialization` | `Bee.Base`（傳遞依賴） |
| `Bee.Definition` | `Bee.Definition`（傳遞依賴） |
| `Bee.Api.Client` | `Bee.Api.Client`（傳遞依賴） |
| `Bee.UI.Core` | `Bee.UI.Core`（直接依賴，**需先發布**） |

csproj 只需直接引用 `Bee.UI.Core`，其餘透過傳遞依賴自動引入。

## 執行步驟

### 步驟 1：確認前置條件

- 確認 `Bee.UI.Core` 已發布至 NuGet（記錄實際版本號）

### 步驟 2：建立新 Repo 目錄結構

```
bee-ui-winforms/
├── .editorconfig            ← 從 bee-library 複製
├── .gitignore               ← .NET gitignore
├── Directory.Build.props    ← 根目錄，套件 metadata + CI 設定
├── LICENSE.txt              ← 從 bee-library 複製
├── bee.png                  ← 從 bee-library 複製
├── README.md                ← 新建
├── Bee.UI.WinForms.slnx     ← 新建 solution
├── src/
│   └── Bee.UI.WinForms/
│       ├── Bee.UI.WinForms.csproj
│       ├── BaseForm/
│       ├── Common/
│       ├── Controls/
│       │   └── TreeView/
│       ├── Editor/
│       ├── Event/
│       ├── Forms/           ← 含 .Designer.cs 和 .resx
│       ├── Interface/
│       └── Tracing/
└── .github/
    └── workflows/
        ├── build-ci.yml
        └── nuget-publish.yml
```

### 步驟 3：修改 csproj — ProjectReference 改為 PackageReference

```xml
<!-- 原本 -->
<ProjectReference Include="..\Bee.UI.Core\Bee.UI.Core.csproj" />

<!-- 改為（版本號依實際發布版本填入） -->
<PackageReference Include="Bee.UI.Core" Version="x.x.x" />
```

### 步驟 4：建立根目錄 Directory.Build.props

- 合併 bee-library 的套件 metadata + CI 最佳化設定
- `RepositoryUrl` / `PackageProjectUrl` 改為 `https://github.com/jeff377/bee-ui-winforms`
- 資源檔路徑調整為相對於根目錄

### 步驟 5：建立 GitHub Actions workflows

- **build-ci.yml**：觸發 push/PR to main，Runner 使用 `windows-latest`（WinForms 需要 Windows）
- **nuget-publish.yml**：觸發 push tag `v*`，Runner 使用 `windows-latest`

> 注意：`net10.0-windows` + `UseWindowsForms` 需要 Windows SDK，CI runner 必須使用 `windows-latest`。

### 步驟 6：驗證建置（Windows + Visual Studio 環境）

```bash
dotnet restore
dotnet build src/Bee.UI.WinForms/Bee.UI.WinForms.csproj --configuration Release
dotnet pack src/Bee.UI.WinForms/Bee.UI.WinForms.csproj --configuration Release --output ./nupkgs
```

在 Visual Studio 中開啟 `ApiConnectForm.cs`，確認 WinForms 設計工具可正常載入。

### 步驟 7：建立 GitHub Repo 並推送

```bash
git init
git add .
git commit -m "Initial commit: migrate Bee.UI.WinForms from bee-library"
gh repo create jeff377/bee-ui-winforms --public --source=. --push
```

### 步驟 8：從 bee-library 刪除已移出的檔案

- 刪除 `src/Bee.UI.WinForms/` 目錄
- 更新 `docs/plans/plan-migrate-projects.md` 標記為已完成
- 所有專案移植完成後，將 `plan-migrate-projects.md` 移至 `docs/plans/archive/`

## 注意事項

- 含 WinForms 設計工具檔案（`.Designer.cs`、`.resx`），需用 Visual Studio 確認設計工具正常運作
- CI runner 必須使用 `windows-latest`
- 此為移植計畫最後一個專案，完成後 bee-library 的 `src/` 僅保留 slnx 中的核心 NuGet 套件
- 目前不打 tag、不發布 NuGet，待確認命名空間調整需求後再處理
