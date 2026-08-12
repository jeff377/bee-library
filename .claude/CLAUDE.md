# Bee.NET Library — 專案指引

## 專案概述

Bee.NET 是一套模組化的 .NET 企業應用程式框架，以 NuGet 套件形式發布。採用 JSON-RPC 2.0 API 模式，強調安全性、可插拔序列化與跨平台相容性。

- **版本**：見 repo 根的 `Version.props`（`src/` 與 `tools/` 共用）
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
dotnet restore
dotnet build <project>.csproj --configuration Release --no-restore
./test.sh                                    # 全部測試（自動啟動本機 DB 容器）
./test.sh tests/<Project>.UnitTests/<Project>.UnitTests.csproj
./check-public-docs.sh                       # 公開文件不得引用 docs/plans/
dotnet pack src/<Project>/<Project>.csproj --configuration Release --output ./nupkgs
```

`./test.sh` 的容器偵測 / 自動 skip / env var override 細節見 `tests/CLAUDE.md`
（動 `tests/` 時自動載入）。

## 架構分層

| 層級 | 專案 |
|------|------|
| API 層 | Bee.Api.AspNetCore, Bee.Api.Core, Bee.Api.Client |
| 商業邏輯層 | Bee.Business |
| 資料存取層 | Bee.Repository, Bee.Repository.Abstractions, Bee.Db |
| 基礎設施 | Bee.Base, Bee.Definition, Bee.ObjectCaching |

> ⚠️ **`Bee.Base` 與 `Bee.Definition` 是最底層的兩個組件**：`Bee.Base` 是所有專案的相依、
> `Bee.Definition` 的直接下游遍及各層。**除非必要，不得再為這兩個專案加入套件參考**——
> 加在這裡的任何相依會沿相依鏈傳染給每一個消費者。判準、正解與閘門見
> `rules/dependency-boundary.md`。

## 工作流程

### 執行前先擬計畫

任何需要事先規劃的任務（重構、新功能、架構調整等），必須：

1. 將計畫寫成 md 文件，存至 `docs/plans/` 目錄，檔名格式：`plan-<主題>.md`
2. **每次建立或修改 plan 文件後，回覆中必須附上該 plan 的連結**（markdown 相對連結），讓使用者可在對話中直接點開、不需自行翻找
3. 等待使用者確認後，才開始執行
4. **Plan 執行完畢時，立刻在文件頂部標記完成狀態**
5. 由使用者要求時才將計畫文件移至 `docs/plans/archive/` 封存（此目錄**入版控**，作為維護者的團隊記憶）
   - 例外：含未修安全弱點清單的 review 類 plan 改放 `docs/internal/`（gitignored），避免公開 repo 附上現成攻擊面盤點
   - **公開文件一律不得連結或引用 plan**（含封存 plan）—— plan 是階段性文件、舊版未必正確，詳見 `rules/public-docs.md`

> 狀態列格式、多階段 plan 的階段表格、**plan 內的連結慣例**（封存後仍有效的相對路徑寫法）、
> 封存細節 → 見 `/dev-workflow:plan-write` skill（由 `jeff377-plugins` marketplace 的
> `dev-workflow` plugin 提供，已於 `.claude/settings.json` 宣告啟用）。
> 該 plugin 早期名為 `plan-workflow`，**已改名**；cache 裡殘留的舊目錄不再生效，別照舊名指路。

## 架構參考

實作任何功能或模組前，先讀 `docs/README.md` —— 公開文件的入口索引（架構總覽、開發指引與限制、
資料庫、設計概念，皆雙語、分類列表），再依索引開對應文件。設計決策的背景見 `docs/adr/`；
進行中 / 已完成的規劃見 `docs/plans/`（階段性文件，舊 plan 未必符合現行行為，勿當規格）；
各套件細節見各 `src/` 專案的 `README.md`。

**踩雷誌 `docs/repo-ops/gotchas/`**（維護者視角，非公開文件）記錄實際踩過、下次很可能再踩的雷，
含症狀、根因與正解。硬規則已收進 `rules/`（常駐），gotchas 是**按需查閱**的脈絡。
動到下列範圍前先讀對應那份：**資料庫 / provider dialect**、**序列化與運算式引擎**、
**Avalonia 控件**、**測試 / CI / 發佈**、**Northwind 各 head**。索引見
`docs/repo-ops/gotchas/README.md`。

核心心智模型（實作時的定錨，細節見上述文件）：
- **FormSchema** 為定義中樞，同時驅動 UI（FormLayout）、資料庫（DbTable）與驗證規則
- **DataSet** 為跨層 DTO，承載 Master-Detail 資料，不含邏輯
- **Business Object（BO）** 負責業務邏輯，不直接存取資料庫
- **Repository** 採雙軌策略：CRUD 由 FormSchema 驅動，報表/批次由 BO 自行實作（AnyCode）
- 架構模式：N-Tier + Clean Architecture + MVVM 混合

## 規則導入

跨專案共用規則（`code-style`、`scanning`、`pull-request`、`releasing`）由使用者層 `~/.claude/CLAUDE.md` 統一載入，本檔僅引用本專案特化規則：

@rules/public-docs.md
@rules/dependency-boundary.md
@rules/database.md
@rules/definition.md
@rules/serialization.md
@rules/testing.md
@rules/security.md
@rules/sonarcloud.md
@rules/commit-verification.md
@rules/apple-mobile-trim.md
@rules/avalonia.md
