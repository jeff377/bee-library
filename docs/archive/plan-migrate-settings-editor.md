# BeeSettingsEditor 移植至獨立 Repo 執行步驟

## Context

BeeSettingsEditor 是一個 WinForms 設定編輯工具（`tools/BeeSettingsEditor/`），目前不在 `Bee.Library.slnx` 中，但以 ProjectReference 引用 4 個核心套件。移植目標是將它獨立至 `bee-settings-editor` repo，改用 NuGet 引用。

**前置條件**：所有相依 NuGet 套件皆已發布（v3.6.2），可直接執行移植。

---

## 執行步驟

### 1. 建立新 Repo

```bash
gh repo create jeff377/bee-settings-editor --public --clone
cd bee-settings-editor
```

### 2. 複製專案檔案

將以下檔案從 `bee-library` 複製至新 repo：

```
bee-settings-editor/
├── src/BeeSettingsEditor/            # 專案目錄（從 tools/ 改為 src/）
│   ├── BeeSettingsEditor.csproj
│   ├── Program.cs
│   ├── frmMainForm.cs / .Designer.cs / .resx
│   ├── Common/AppInfo.cs, Common.cs
│   ├── Properties/launchSettings.json
│   └── BeeSettingsEditor.ico
├── .editorconfig                     # 從 bee-library 根目錄複製
├── LICENSE.txt                       # 從 bee-library 根目錄複製
└── README.md                         # 從 tools/BeeSettingsEditor/README.md 移至根目錄
```

### 3. 將 ProjectReference 改為 PackageReference

修改 `BeeSettingsEditor.csproj`：

```xml
<!-- 移除這 4 行 -->
<ProjectReference Include="..\..\src\Bee.Business\Bee.Business.csproj" />
<ProjectReference Include="..\..\src\Bee.Cache\Bee.Cache.csproj" />
<ProjectReference Include="..\..\src\Bee.Repository\Bee.Repository.csproj" />
<ProjectReference Include="..\..\src\Bee.UI.WinForms\Bee.UI.WinForms.csproj" />

<!-- 替換為 -->
<PackageReference Include="Bee.Business" Version="3.6.2" />
<PackageReference Include="Bee.Cache" Version="3.6.2" />
<PackageReference Include="Bee.Repository" Version="3.6.2" />
<PackageReference Include="Bee.UI.WinForms" Version="3.6.2" />
```

> 注意：若重命名計畫已執行，需使用新的套件名稱。

### 4. 建立 Solution 檔案

在新 repo 根目錄建立 `BeeSettingsEditor.slnx`：

```xml
<Solution>
  <Project Path="src/BeeSettingsEditor/BeeSettingsEditor.csproj" />
</Solution>
```

### 5. 驗證建置

```bash
dotnet restore
dotnet build --configuration Release
dotnet publish src/BeeSettingsEditor/BeeSettingsEditor.csproj -c Release -r win-x64
```

### 6. 遷移 CI/CD

將 `.github/workflows/release-BeeSettingsEditor.yml` 複製至新 repo 並調整：
- 路徑引用更新（若目錄結構有變動）
- 確認 .NET SDK 版本
- 確認 publish 輸出路徑

### 7. 從 bee-library 移除

```bash
# 刪除專案目錄
rm -rf tools/BeeSettingsEditor/

# 刪除專屬 workflow
rm .github/workflows/release-BeeSettingsEditor.yml
```

### 8. 更新 bee-library

- 若 `README.md` 有提及 BeeSettingsEditor 則移除（目前未提及，無需處理）
- 在 `docs/plan-migrate-projects.md` 標記 BeeSettingsEditor 已完成
- Commit 變更

---

## 驗證方式

1. 在新 repo 執行 `dotnet build --configuration Release` 確認建置成功
2. 執行 `dotnet publish -c Release -r win-x64` 確認發布成功
3. 實際執行產出的 exe 確認功能正常
4. 推送 tag（如 `BeeSettingsEditor-v1.0.8`）確認 GitHub Actions workflow 正常運作
