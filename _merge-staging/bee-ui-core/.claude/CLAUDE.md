# Bee.UI.Core — 專案指引

## 專案概述

Bee.UI.Core 是 Bee.NET 用戶端橋接層套件，封裝 client/server 連線設定與登入狀態，以 NuGet 套件形式發布。本 repo 消費 `Bee.Api.Client` 提供的 API 連接器，向上對 UI 應用暴露單一進入點（`ClientInfo`）。

- **版本**：4.1.0（`src/Directory.Build.props`）
- **授權**：MIT
- **目標框架**：`net10.0`
- **核心相依**：`Bee.Api.Client` 4.1.0（隨主線 Bee.NET 版本同步升級）

## 目錄結構

```
src/Bee.UI.Core/
  ├── Common/         # 實作（ClientInfo、EndpointStorage、VersionInfo）
  └── Interface/      # 公開介面（IUIViewService、IEndpointStorage）
```

目前尚無 `tests/` 與 `docs/`；未來新增單元測試請建立 `tests/Bee.UI.Core.UnitTests/`。

## 常用命令

```bash
# 還原相依
dotnet restore Bee.UI.Core.slnx

# 建置（Release）
dotnet build src/Bee.UI.Core/Bee.UI.Core.csproj --configuration Release --no-restore

# 封裝 NuGet
dotnet pack src/Bee.UI.Core/Bee.UI.Core.csproj --configuration Release --output ./nupkgs
```

## 核心架構

| 元件 | 職責 |
|------|------|
| `ClientInfo` | 連線方式（近端/遠端）切換、`AccessToken` / `UserInfo` 狀態、`SystemApiConnector` 快取、命令列引數解析、`IDefineAccess` 提供 |
| `EndpointStorage` | 服務端點的本地 XML 儲存（`<exe>.Settings.xml`） |
| `IUIViewService` | 由 UI 層（WinForms / WPF / Avalonia 等）實作，提供連線設定視窗等服務 |
| `VersionInfo` | 用戶端版本資訊 |

### 與 Bee.NET 主線的關係

- 與 [bee-library](https://github.com/jeff377/bee-library) 維持**單向相依**：本 repo 消費 `Bee.Api.Client` 等套件，不反向被依賴
- 升級 `Bee.Api.Client` 時，依新 API 同步調整 `ClientInfo`（命名空間搬遷、靜態化、型別重命名等典型情境）— 若主線 NuGet 套件已不支援舊目標框架，此 repo 跟進升級至同樣的 `TargetFramework`
- 不在此 repo 處理 server 端職責（API 實作、DB Schema、Business Object 等）

## 工作流程

### 執行前先擬計畫

重構、套件升級、跨檔案改動，必須：

1. 將計畫寫成 md 文件，存至 `docs/plans/`，檔名格式：`plan-<主題>.md`（首次撰寫時若資料夾不存在請一併建立）
2. 等待使用者確認後才開始執行
3. **Plan 執行完畢時，立刻在文件頂部標記完成狀態**，讓後續無需回查程式碼或 commits 即可判斷狀態
4. 由使用者要求時才將計畫文件移至 `docs/archive/`（gitignored）封存

#### Plan 狀態標記格式

在文件第一行標題下一行加上狀態列：

```markdown
# 計畫：升級 Bee.Api.Client 至 4.x

**狀態：✅ 已完成（2026-05-06）**

## 背景
...
```

- 進行中可標記 `**狀態：🚧 進行中**`／`**狀態：📝 擬定中**`
- 完成日期採 ISO 格式（`YYYY-MM-DD`）

## 規則導入

### 本專案特化規則

```
# 目前無專案特化規則。
# 未來新增測試（testing.md）、SonarCloud 接入（sonarcloud.md）等
# 屬於本 repo 獨有的內容時，在此 @import。
```

### 跨專案共用規則

跨所有 Bee.NET repo 共用的規則（程式碼風格、SAST 安全防護、PR 流程、NuGet 發佈流程等）由使用者層 `~/.claude/CLAUDE.md` 統一導入，本檔不重複引用。詳見此 repo `.claude/README.md`（若存在）或團隊規則層級設計文件。
