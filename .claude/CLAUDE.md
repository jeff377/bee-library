# Bee.NET Library — 專案指引

## 專案概述

Bee.NET 是一套模組化的 .NET 企業應用程式框架，以 NuGet 套件形式發布。採用 JSON-RPC 2.0 API 模式，強調安全性、可插拔序列化與跨平台相容性。

- **版本**：4.13.0（`src/Directory.Build.props`）
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
# 使用 ./test.sh：偵測本機 SQL Server / PostgreSQL / MySQL / Oracle container
# （預設 sql2025、pgvector-db、mysql8、oracle23ai），存在則自動啟動再跑 dotnet test。
# 未啟動的容器對應的 [DbFact(DatabaseType.X)] 測試會依 .runsettings 中各
# BEE_TEST_CONNSTR_{DBTYPE} 是否可連線自動 skip。
# 容器名稱可用 BEE_TEST_SQL_CONTAINER / BEE_TEST_PG_CONTAINER /
# BEE_TEST_MYSQL_CONTAINER / BEE_TEST_ORACLE_CONTAINER 環境變數 override。
./test.sh

# 執行特定測試專案
./test.sh tests/<Project>.UnitTests/<Project>.UnitTests.csproj

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
2. **每次建立或修改 plan 文件後，回覆中必須附上該 plan 的連結**（markdown 相對連結），讓使用者可在對話中直接點開、不需自行翻找
3. 等待使用者確認後，才開始執行
4. **Plan 執行完畢時，立刻在文件頂部標記完成狀態**
5. 由使用者要求時才將計畫文件移至 `docs/archive/` 封存（此目錄已 gitignored）

> 狀態列格式、多階段 plan 的階段表格、封存細節 → 見 `plan-write` skill。

## 架構參考

實作任何功能或模組前，先讀 `docs/README.md` —— 公開文件的入口索引（架構總覽、開發指引與限制、
資料庫、設計概念，皆雙語、分類列表），再依索引開對應文件。設計決策的背景見 `docs/adr/`；
進行中 / 已完成的規劃見 `docs/plans/`；各套件細節見各 `src/` 專案的 `README.md`。

核心心智模型（實作時的定錨，細節見上述文件）：
- **FormSchema** 為定義中樞，同時驅動 UI（FormLayout）、資料庫（DbTable）與驗證規則
- **DataSet** 為跨層 DTO，承載 Master-Detail 資料，不含邏輯
- **Business Object（BO）** 負責業務邏輯，不直接存取資料庫
- **Repository** 採雙軌策略：CRUD 由 FormSchema 驅動，報表/批次由 BO 自行實作（AnyCode）
- 架構模式：N-Tier + Clean Architecture + MVVM 混合

## 規則導入

跨專案共用規則（`code-style`、`scanning`、`pull-request`、`releasing`）由使用者層 `~/.claude/CLAUDE.md` 統一載入，本檔僅引用本專案特化規則：

@rules/testing.md
@rules/security.md
@rules/sonarcloud.md
@rules/maui.md
@rules/avalonia.md
