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
2. **每次建立或修改 plan 文件後，回覆中必須附上該 plan 的連結**（markdown 相對連結，如 `[plan-xxx.md](docs/plans/plan-xxx.md)`），讓使用者可在對話中直接點開 plan 內容、不需自行翻找
3. 等待使用者確認後，才開始執行
4. **Plan 執行完畢時，立刻在文件頂部標記完成狀態**（見下方格式），讓後續不需回查程式碼或 commits 即可判斷狀態
5. 由使用者要求時才將計畫文件移至 `docs/archive/` 封存（此目錄已 gitignored，不上 GitHub；亦可放置任何需封存的舊文件，不限 plan）

#### Plan 狀態標記格式

在文件第一行標題（`# <標題>`）下一行直接加上**單行狀態列**：

```markdown
# 計畫：移除 Newtonsoft.Json，遷移至 System.Text.Json

**狀態：✅ 已完成（2026-04-17）**

## 背景
...
```

- 進行中或尚未開始的 plan 可省略狀態列，或標記 `**狀態：🚧 進行中**`／`**狀態：📝 擬定中**`
- 完成日期採 ISO 格式（`YYYY-MM-DD`），對應實作落地當天
- 已封存至 `docs/archive/` 的 plan 必有完成狀態列
- 狀態列**只放單行**：`emoji + 短標籤（+ 完成日期）`，不寫成完整段落、不堆細節

#### 多階段 plan：狀態列下加階段表格

當 plan 拆成 ≥ 2 個獨立可交付的階段（Phase / PR / Milestone）時，**在單行狀態列下方再加一個階段表格**，欄位為 `階段 / 範圍 / 狀態`。狀態列負責「整體一句話判斷（封存掃讀）」，階段表格負責「細部進度掃讀」，兩者並存、不二擇一：

```markdown
# 計畫：網站前端整合 JSON-RPC 後端

**狀態：🚧 進行中（2026-05-23）**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 1a | xxx | ✅ 已完成（2026-05-22） |
| 1b | xxx | 🚧 進行中 |
| 2  | xxx | 📝 待做 |

## 背景
...
```

- 整體狀態列與階段表格的對應規則：全部 ✅ → 整體 ✅；任一 🚧 或部分 ✅ → 整體 🚧；全部 📝 → 整體 📝。
- 階段表格欄位的狀態 emoji 沿用單行狀態列同一套（`✅ / 🚧 / 📝`）。
- 階段表格的列順序對應實作順序；表格之外不要再用 prose 寫「目前在 Phase X」這種重複資訊。
- 表格內每階段一行，「範圍」要可獨立交付，不要把細節 step 全列進去（那屬於該階段的內文段落）。
- 單一階段或一次性 plan **不需要表格**，只留單行狀態列。

## 架構參考

實作任何功能或模組前，請先閱讀以下文件：

- **架構總覽**：`docs/architecture-overview.md`
- **相依性全景圖**：`docs/dependency-map.md` — 17 個專案的相依關係與分層
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

跨專案共用規則（`code-style`、`scanning`、`pull-request`、`releasing`）由使用者層 `~/.claude/CLAUDE.md` 統一載入，本檔僅引用本專案特化規則：

@rules/testing.md
@rules/security.md
@rules/sonarcloud.md
@rules/maui.md
@rules/avalonia.md
