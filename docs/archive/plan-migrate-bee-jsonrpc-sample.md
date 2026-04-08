# bee-jsonrpc-sample 移植執行計畫

## Context

依 `docs/plan-migrate-projects.md` 第 3 項，將 `samples/` 下的 JSON-RPC 範例專案移出至獨立 repo `jeff377/bee-jsonrpc-sample`，讓 bee-library 只保留核心 NuGet 套件。所有相依套件已在 NuGet 發布（v3.6.2），可直接移出。

## 移植範圍

移出以下目錄：
- `samples/Custom.Contracts/`
- `samples/Custom.Business/`
- `samples/JsonRpcClient/`
- `samples/JsonRpcServer/`
- `samples/Define/`（設定檔，非專案）

## 步驟

### 1. 建立新 Repo

```bash
gh repo create jeff377/bee-jsonrpc-sample --public --clone
cd bee-jsonrpc-sample
```

### 2. 複製檔案至新 Repo

目標目錄結構：
```
bee-jsonrpc-sample/
├── .editorconfig          ← 從 bee-library 複製
├── LICENSE                 ← GitHub 建立時自動產生
├── README.md               ← 新建（中英文簡介 + 使用說明）
├── src/
│   ├── Define/             ← 原封複製
│   ├── Custom.Contracts/
│   ├── Custom.Business/
│   ├── JsonRpcClient/
│   └── JsonRpcServer/
```

### 3. 將 ProjectReference 改為 PackageReference

#### Custom.Contracts.csproj
```xml
<!-- 移除 -->
<ProjectReference Include="..\..\src\Bee.Api.Contracts\Bee.Api.Contracts.csproj" />
<!-- 改為 -->
<PackageReference Include="Bee.Api.Contracts" Version="3.6.2" />
```

#### Custom.Business.csproj
```xml
<!-- 移除 -->
<ProjectReference Include="..\..\src\Bee.Business\Bee.Business.csproj" />
<ProjectReference Include="..\..\src\Bee.Repository\Bee.Repository.csproj" />
<!-- 改為 -->
<PackageReference Include="Bee.Business" Version="3.6.2" />
<PackageReference Include="Bee.Repository" Version="3.6.2" />
```
注意：Custom.Contracts 的 ProjectReference 保留（同 repo 內部參考）。

#### JsonRpcClient.csproj
```xml
<!-- 移除 -->
<ProjectReference Include="..\..\src\Bee.UI.WinForms\Bee.UI.WinForms.csproj" />
<!-- 改為 -->
<PackageReference Include="Bee.UI.WinForms" Version="3.6.2" />
```
注意：Custom.Business 的 ProjectReference 保留（同 repo 內部參考）。

#### JsonRpcServer.csproj
```xml
<!-- 移除 -->
<ProjectReference Include="..\..\src\Bee.Api.AspNetCore\Bee.Api.AspNetCore.csproj" />
<ProjectReference Include="..\..\src\Bee.Cache\Bee.Cache.csproj" />
<!-- 改為 -->
<PackageReference Include="Bee.Api.AspNetCore" Version="3.6.2" />
<PackageReference Include="Bee.Cache" Version="3.6.2" />
```
注意：Custom.Business 的 ProjectReference 保留；`Microsoft.Data.SqlClient` PackageReference 不變；Define 資料夾的 `<None Include>` 路徑不需調整（相對路徑 `..\Define\**` 在新 repo 結構下仍然正確）。

### 4. 建立 Solution 檔

```bash
dotnet new sln -n bee-jsonrpc-sample
dotnet sln add samples/Custom.Contracts/Custom.Contracts.csproj
dotnet sln add samples/Custom.Business/Custom.Business.csproj
dotnet sln add samples/JsonRpcClient/JsonRpcClient.csproj
dotnet sln add samples/JsonRpcServer/JsonRpcServer.csproj
```

### 5. 驗證建置

```bash
dotnet restore
dotnet build --configuration Release --no-restore
```

### 6. 從 bee-library 移除

- 刪除 `samples/Custom.Contracts/` 目錄
- 刪除 `samples/Custom.Business/` 目錄
- 刪除 `samples/JsonRpcClient/` 目錄
- 刪除 `samples/JsonRpcServer/` 目錄
- 刪除 `samples/Define/` 目錄
- 清理 `samples/JsonRpcServerAspNet/` 殘留的 bin/obj 產出物（該專案已移除但遺留編譯產物）

### 7. 更新 bee-library

- **README.md / README.zh-TW.md**：將 `jsonrpc-sample` 連結更新為 `bee-jsonrpc-sample`
- **docs/plan-migrate-projects.md**：標記 bee-jsonrpc-sample 為 ✅ 已移植
- 確認 `.github/workflows/nuget-publish.yml` 無需修改（已確認不含 samples 相關設定）

## 關鍵檔案

- `samples/Custom.Contracts/Custom.Contracts.csproj`
- `samples/Custom.Business/Custom.Business.csproj`
- `samples/JsonRpcClient/JsonRpcClient.csproj`
- `samples/JsonRpcServer/JsonRpcServer.csproj`
- `README.md`（第 50 行）
- `README.zh-TW.md`（第 49 行）
- `docs/plan-migrate-projects.md`（第 16、35 行）

## 驗證方式

1. 新 repo `dotnet build --configuration Release` 通過
2. bee-library `dotnet build --configuration Release` 通過（不受影響，samples 不在 slnx 中）
3. README 連結指向正確的新 repo
