# Bee.NET 專案移植計畫 — 將非 slnx 專案移出至獨立 Repo

## Context

Bee.Library.slnx 中只保留核心 NuGet 套件，不在 slnx 中的 7 個專案（UI 層、Legacy ASP.NET、工具、範例）需移出至獨立 repo，以支援 VS Code 開發及各專案獨立演進。

## 新 Repo 規劃

| # | 新 Repo | 移出專案 | 說明 |
|---|---------|---------|------|
| 1 | bee-ui-core | `src/Bee.UI.Core` | 前端介面核心，未來支援多種 UI 框架 |
| 2 | bee-ui-winforms | `src/Bee.UI.WinForms` | WinForms UI 元件 |
| 3 | bee-api-aspnet | `src/Bee.Api.AspNet` | .NET Framework 網站服務 |
| 4 | bee-settings-editor | `tools/BeeSettingsEditor` | 設定編輯工具 |
| 5 | bee-db-upgrade | `tools/BeeDbUpgrade` | 資料庫升級工具 |
| 6 | bee-jsonrpc-sample | `samples/JsonRpcClient` + `samples/JsonRpcServerAspNet` | Sample 專案，未來升級至 .NET 10 |

## 相依關係（移植後改用 NuGet 引用）

```
bee-ui-core         → NuGet: Bee.Connect (即將改名 Bee.Api.Client)
bee-ui-winforms     → NuGet: Bee.UI.Core
bee-api-aspnet      → NuGet: Bee.Api.Core
bee-settings-editor → NuGet: Bee.Business, Bee.Cache, Bee.Repository, Bee.UI.WinForms
bee-db-upgrade      → NuGet: Bee.Business, Bee.Cache, Bee.Repository, Bee.UI.WinForms
bee-jsonrpc-sample  → NuGet: Bee.UI.WinForms, Bee.Api.AspNet, Bee.Api.Core, Bee.Base, Bee.Business, Bee.Cache, Bee.Db, Bee.Define
```

## 移植順序

依相依性由底層往上（下游專案需等上游先發布 NuGet）：

1. **Bee.Api.AspNet** — 僅依賴已在 NuGet 的 Bee.Api.Core，可最先移出
2. **Bee.UI.Core** — 僅依賴已在 NuGet 的 Bee.Connect，可同步移出
3. **Bee.UI.WinForms** — 等 Bee.UI.Core 發布 NuGet 後移出
4. **BeeSettingsEditor** — 等 Bee.UI.WinForms 發布 NuGet 後移出
5. **BeeDbUpgrade** — 同上，可與 BeeSettingsEditor 同步
6. **bee-jsonrpc-sample** — 等 Bee.UI.WinForms + Bee.Api.AspNet 都發布後移出

## 每個專案的移植步驟

以 `Bee.UI.Core` 為例：

### 1. 建立新 Repo
```bash
# 在 GitHub 建立 bee-ui-core repo
gh repo create jeff377/bee-ui-core --public --clone
```

### 2. 複製專案檔案
- 複製 `src/Bee.UI.Core/` 至新 repo 的 `src/Bee.UI.Core/`
- 複製必要的基礎設施檔案：`.editorconfig`、`Directory.Build.props`、`LICENSE.txt`、`README.md`

### 3. 將 ProjectReference 改為 PackageReference
```xml
<!-- 原本 -->
<ProjectReference Include="..\Bee.Connect\Bee.Connect.csproj" />

<!-- 改為 -->
<PackageReference Include="Bee.Connect" Version="3.6.2" />
```

### 4. 建立新的 .slnx（或 .sln）
- 建立對應的 solution 檔案

### 5. 驗證建置
```bash
dotnet restore
dotnet build --configuration Release
```

### 6. 從 bee-library 移除
- 刪除 `src/Bee.UI.Core/` 目錄
- 若 slnx 中有引用則移除（此案例不在 slnx 中，無需處理）

### 7. 更新 bee-library
- 更新 `.github/workflows/nuget-publish.yml`（移除已不存在的專案）
- 更新 README.md（移除已移出的專案說明，或加上指向新 repo 的連結）

## 注意事項

### 與重命名計畫的協調
此移植計畫與 `docs/plan-rename-projects.md` 的重命名計畫相關：
- 移植時 ProjectReference 改為 PackageReference 需使用**新的套件名稱**
- 建議先完成 bee-library 的重命名並發布新版 NuGet，再執行移植
- 執行順序：重命名 → 發布 NuGet → 移植專案

### NuGet 版本
- 移植後的專案引用 NuGet 套件版本需與 bee-library 的發布版本一致
- 目前版本為 3.6.2（定義於 `src/Directory.Build.props`）

### bee-jsonrpc-sample 特殊處理
- `JsonRpcServerAspNet` 目前為 net4.8 + packages.config 格式，未來需升級至 .NET 10
- 移植時可先保持現狀，後續在新 repo 中進行升級
