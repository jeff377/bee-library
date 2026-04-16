# Bee.NET Library — 專案指引

## 專案概述

Bee.NET 是一套模組化的 .NET 企業應用程式框架，以 NuGet 套件形式發布。採用 JSON-RPC 2.0 API 模式，強調安全性、可插拔序列化與跨平台相容性。

- **版本**：4.0.1（`src/Directory.Build.props`）
- **授權**：MIT
- **主要目標框架**：`net10.0`（全部專案）

## 目錄結構

```
src/         # 核心套件（Bee.Base, Bee.Definition, Bee.Api.Core 等）
tests/       # 對應的單元測試專案
samples/     # 示範專案
```

## 常用命令

```bash
# 還原相依套件
dotnet restore

# 建置（Release）
dotnet build <project>.csproj --configuration Release --no-restore

# 執行測試（所有）
dotnet test --configuration Release --settings .runsettings

# 執行特定測試專案
dotnet test tests/<Project>.UnitTests/<Project>.UnitTests.csproj --settings .runsettings

# 封裝 NuGet
dotnet pack src/<Project>/<Project>.csproj --configuration Release --output ./nupkgs
```

## 架構分層

| 層級 | 專案 |
|------|------|
| API 層 | Bee.Api.AspNetCore, Bee.Api.Core, Bee.Api.Client |
| 商業邏輯層 | Bee.Business |
| 資料存取層 | Bee.Repository, Bee.Repository.Abstractions, Bee.Db |
| 基礎設施 | Bee.Base, Bee.Definition, Bee.ObjectCaching |

## 工作流程

### 執行前先擬計畫

任何需要事先規劃的任務（重構、新功能、架構調整等），必須：

1. 將計畫寫成 md 文件，存至 `docs/plans/` 目錄，檔名格式：`plan-<主題>.md`
2. 等待使用者確認後，才開始執行
3. 計畫執行完成後，由使用者要求時才將計畫文件移至 `docs/plans/archive/` 封存

## 架構參考

實作任何功能或模組前，請先閱讀以下文件：

- **架構總覽**：`docs/architecture-overview.md`
- **相依性全景圖**：`docs/dependency-map.md` — 11 個專案的相依關係與分層
- **開發指引**：`docs/development-cookbook.md` — 初始化流程、請求管線、ExecFunc 模式、FormSchema 驅動開發
- **開發限制**：`docs/development-constraints.md` — 初始化順序、跨層禁止事項、例外處理規則
- **架構決策紀錄**：`docs/adr/` — 重要設計決策的背景與理由
- **各專案說明**：每個 `src/` 專案目錄內的 `README.md`

重點摘要：
- **FormSchema** 為定義中樞，同時驅動 UI（FormLayout）、資料庫（DbTable）與驗證規則
- **DataSet** 為跨層 DTO，承載 Master-Detail 資料，不含邏輯
- **Business Object（BO）** 負責業務邏輯，不直接存取資料庫
- **Repository** 採雙軌策略：CRUD 由 FormSchema 驅動，報表/批次由 BO 自行實作（AnyCode）
- 架構模式：N-Tier + Clean Architecture + MVVM 混合

## 規則導入

@rules/code-style.md
@rules/testing.md
@rules/security.md
@rules/releasing.md
