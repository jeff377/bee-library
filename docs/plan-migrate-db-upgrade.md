# BeeDbUpgrade 移植至獨立 Repo 執行步驟

## Context

BeeDbUpgrade 是一個 WinForms 資料庫升級工具（`tools/BeeDbUpgrade/`），目前不在 `Bee.Library.slnx` 中，但以 ProjectReference 引用 4 個核心套件。移植目標是將它獨立至 `bee-db-upgrade` repo，改用 NuGet 引用。

**前置條件**：所有相依 NuGet 套件皆已發布（v3.6.2），可直接執行移植。

---

## 執行步驟

### 1. 建立新 Repo

```bash
gh repo create jeff377/bee-db-upgrade --public --clone
cd bee-db-upgrade
```

### 2. 複製專案檔案

將以下檔案從 `bee-library` 複製至新 repo：

```
bee-db-upgrade/
├── src/BeeDbUpgrade/            # 專案目錄（從 tools/ 改為 src/）
│   ├── BeeDbUpgrade.csproj
│   ├── Program.cs
│   ├── frmMainForm.cs / .Designer.cs / .resx
│   ├── Common/AppInfo.cs
│   ├── Properties/launchSettings.json
│   ├── Properties/PublishProfiles/FolderProfile.pubxml
│   └── BeeDbUpgrade.ico
├── .editorconfig                 # 從 bee-library 根目錄複製
├── LICENSE.txt                   # 從 bee-library 根目錄複製
└── README.md                     # 新建
```

### 3. 將 ProjectReference 改為 PackageReference

修改 `BeeDbUpgrade.csproj`：

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

### 4. 修正 csproj 中 BeeSettingsEditor 殘留

`RemoveExtraArtifacts` target 的 Exclude 目前錯誤引用 `BeeSettingsEditor`，需修正為 `BeeDbUpgrade`：

```xml
<!-- 修正前 -->
<XmlFilesToDelete Include="$(PublishDir)**\*.xml" Exclude="$(PublishDir)BeeSettingsEditor.xml" />
<PdbFilesToDelete Include="$(PublishDir)**\*.pdb" Exclude="$(PublishDir)BeeSettingsEditor.pdb" />

<!-- 修正後 -->
<XmlFilesToDelete Include="$(PublishDir)**\*.xml" Exclude="$(PublishDir)BeeDbUpgrade.xml" />
<PdbFilesToDelete Include="$(PublishDir)**\*.pdb" Exclude="$(PublishDir)BeeDbUpgrade.pdb" />
```

### 5. 建立 Solution 檔案

在新 repo 根目錄建立 `BeeDbUpgrade.slnx`：

```xml
<Solution>
  <Project Path="src/BeeDbUpgrade/BeeDbUpgrade.csproj" />
</Solution>
```

### 6. 驗證建置

```bash
dotnet restore
dotnet build --configuration Release
dotnet publish src/BeeDbUpgrade/BeeDbUpgrade.csproj -c Release -r win-x64
```

### 7. 遷移 CI/CD

將 `.github/workflows/release-BeeDbUpgrade.yml` 複製至新 repo 並調整：
- .NET SDK 版本更新為 `10.0.x`（目前 workflow 使用 8.0.x，但 csproj 已升至 net10.0-windows）
- 路徑引用更新（若目錄結構有變動）
- 確認 publish 輸出路徑

### 8. 從 bee-library 移除

```bash
# 刪除專案目錄
rm -rf tools/BeeDbUpgrade/

# 刪除專屬 workflow
rm .github/workflows/release-BeeDbUpgrade.yml
```

同時清理 `.gitignore` 中的例外規則（移除此行）：
```
!tools/BeeDbUpgrade/Properties/PublishProfiles/FolderProfile.pubxml
```

### 9. 更新 bee-library

- 在 `docs/plan-migrate-projects.md` 標記 BeeDbUpgrade 為 ✅ 已移植
- Commit 變更

---

## 驗證方式

1. 在新 repo 執行 `dotnet build --configuration Release` 確認建置成功
2. 執行 `dotnet publish -c Release -r win-x64` 確認發布成功
3. 實際執行產出的 exe 確認功能正常
4. 推送 tag（如 `BeeDbUpgrade-v1.0.0`）確認 GitHub Actions workflow 正常運作
