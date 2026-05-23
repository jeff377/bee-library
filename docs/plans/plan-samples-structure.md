# 計畫：samples/ 目錄結構與最小 Demo 清單

**狀態：✅ 已完成（2026-05-23）**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| P0 | `QuickStart.Server` + `QuickStart.Console` + 共用 `Define/` + 主 README Quick Start | ✅ 已完成（2026-05-23） |
| P1 | `Blazor.Server.Demo` + `Blazor.Wasm.Demo` + `Blazor.Wasm.Demo.Host` + `Bee.Samples.Shared` | ✅ 已完成（2026-05-23） |
| P2 | `Maui.Demo` | ✅ 已完成（2026-05-23） |

## 背景

Bee.NET 目前有 16 個 src 套件、涵蓋 API（AspNetCore / Client）、UI（Blazor Server / Blazor Wasm / MAUI）、Hosting、Repository、Db 等多層。`CLAUDE.md` 已在目錄結構中預留 `samples/` 路徑，`.gitignore` 也已預留 `/samples/Define/DatabaseSettings.xml`，但 repo 內目前並無實際範例專案。

需求面：
- 對外（GitHub 訪客）：看到 README 後想立即跑得起來一個能展示框架特色的最小 demo
- 對內（library 開發者）：library 改 API 時，能透過 demo 立即驗證「真實消費端」是否還能運作
- 不影響日常開發節奏：samples 不該每次 `dotnet build` 主 solution 時都被拉進去

## 設計原則

1. **每個 demo 聚焦單一目的**，不堆 ERP 完整情境（完整情境留給未來的獨立 repo）
2. **不加入 `Bee.Library.slnx`**，改用獨立的 `Bee.Samples.slnx`，避免拖累一般開發/CI build 時間
3. **採 ProjectReference**（不走 NuGet）— 改 library 即時反映，PR 改 breaking API 必須同步修 demo（強制讓 demo 與 library 同步演進）
4. **資料庫一律先用 SQLite**，免裝 container、跨平台、可放 seed 資料
5. **共用 Define 檔放 `samples/Define/`**（FormSchema / TableSchema / SystemSettings），多個 demo 共用同一份定義以示範「同一份 FormSchema 渲染到多個前端」
6. **每個 demo 一份 `README.md`**，列：跑前置條件 → 啟動指令 → 預期畫面 → 對應到哪個 library 功能

## 目錄結構提案

```
samples/
├── Bee.Samples.slnx                  # 獨立 solution，不掛進 Bee.Library.slnx
├── README.md                          # samples 總覽（demo 列表 + 跑法）
├── Define/                            # 共用定義檔（多 demo 共用）
│   ├── SystemSettings.xml
│   ├── DbCategorySettings.xml
│   ├── FormSchema/
│   │   └── Employee.xml              # master-detail 示範用
│   └── TableSchema/
│       └── common/
│           └── Employee.xml
├── QuickStart.Server/                 # Demo 1：JSON-RPC API host（必備）
│   ├── QuickStart.Server.csproj      # 引用 Bee.Api.AspNetCore + Bee.Hosting
│   ├── Program.cs
│   ├── appsettings.json
│   └── README.md
├── QuickStart.Console/                # Demo 2：Bee.Api.Client 消費端（必備）
│   ├── QuickStart.Console.csproj     # 引用 Bee.Api.Client
│   ├── Program.cs                    # 示範 Ping / Login / ExecFunc
│   └── README.md
├── Blazor.Server.Demo/                # Demo 3：Blazor Server FormSchema 動態表單
│   ├── Blazor.Server.Demo.csproj     # 引用 Bee.Web.Blazor.Server
│   ├── Components/
│   ├── Program.cs
│   └── README.md
├── Blazor.Wasm.Demo/                  # Demo 4：Blazor Wasm + remote API
│   ├── Blazor.Wasm.Demo.csproj       # 引用 Bee.Web.Blazor.Wasm + Bee.Api.Client
│   └── README.md
└── Maui.Demo/                         # Demo 5：MAUI 同一份 FormSchema 渲染
    ├── Maui.Demo.csproj              # 引用 Bee.UI.Maui + Bee.Api.Client
    └── README.md
```

## Demo 清單與優先順序

| 順序 | 專案 | 對應 library | 目的 | 第一版範圍 |
|------|------|--------------|------|-----------|
| **P0** | `QuickStart.Server` | Bee.Api.AspNetCore, Bee.Hosting, Bee.Business | 啟動 JSON-RPC API host | 載入 SystemSettings、註冊一個 Echo BO、可被 Ping |
| **P0** | `QuickStart.Console` | Bee.Api.Client | 示範客戶端連線 | Local + Remote 兩種 ConnectType、Ping、Login、呼叫 Echo BO |
| **P1** | `Blazor.Server.Demo` | Bee.Web.Blazor.Server | FormSchema 動態渲染（Server） | 載入 Employee FormSchema、master-detail 編輯、Save 走 BO |
| **P1** | `Blazor.Wasm.Demo` | Bee.Web.Blazor.Wasm + Bee.Api.Client | 同上但走 remote API | 同一份 FormSchema、Wasm 透過 JSON-RPC 呼叫 Server |
| **P2** | `Maui.Demo` | Bee.UI.Maui + Bee.Api.Client | 同一份 FormSchema 跨平台 | 桌面/手機渲染同一份 Employee 表單 |

**P0 是 README 要先指向的「30 秒跑起來」入口**；P1 是「FormSchema 多前端渲染」的核心賣點；P2 是「真跨平台」的延伸示範。

## 共用 Define 設計

`samples/Define/` 內放：
- `SystemSettings.xml` — 最小可運作的系統設定（指向 SQLite、demo 用 IsDebugMode = true）
- `DbCategorySettings.xml` — 一個 `common` category，連 SQLite
- `FormSchema/Employee.xml` — 一張員工表（master）+ 一張員工眷屬表（detail），展示 master-detail
- `TableSchema/common/Employee.xml` — 對應的表結構

多個 demo 都指向 `samples/Define/`，確保「同一份定義驅動多個前端」這個賣點具象化。

## 不放進這版的東西

刻意排除，避免第一版 demo 太重：
- 真實 ERP 業務情境（銷售單、進貨單等）— 留給未來獨立 `bee-demo` repo
- SQL Server / PostgreSQL / Oracle / MySQL demo — SQLite 已足夠展示功能
- 認證/授權完整流程 — Login 只示範 API，不串 OAuth/JWT 等外部 IdP
- 部署腳本（Docker / k8s） — 屬於獨立 demo repo 的工作
- 自動化 sample CI 驗證 — 第一版手動驗證即可，等 sample 成熟再考慮加入 CI

## CI 與 build 整合

- `Bee.Library.slnx` 維持不變，**不**把 samples 加進主 solution
- `samples/Bee.Samples.slnx` 獨立，本機需要時 `dotnet build samples/Bee.Samples.slnx` 跑
- `test.sh` 不跑 samples
- `.gitignore`：保留現有 `/samples/Define/DatabaseSettings.xml`；視需要再加 `samples/**/bin/`、`samples/**/obj/`（已被根層級規則覆蓋，多半不需要）

## README 整合

主 `README.md` 加一段 **Quick Start**：

```
要 30 秒看到 Bee.NET 跑起來？

cd samples/QuickStart.Server
dotnet run
# 另一個 terminal
cd samples/QuickStart.Console
dotnet run
```

並在「Architecture」段落結尾連結到 `samples/README.md` 列出完整 demo 清單。

## 執行步驟

按優先序逐步落地，每一步是獨立的 PR：

1. **PR 1**：建立 `samples/` 骨架 + `Bee.Samples.slnx` + `samples/README.md` + 共用 `Define/` 種子檔
2. **PR 2**：P0 `QuickStart.Server` + `QuickStart.Console`（含 README）
3. **PR 3**：P1 `Blazor.Server.Demo`（含 README、共用 Employee FormSchema）
4. **PR 4**：P1 `Blazor.Wasm.Demo`（含 README、連 QuickStart.Server）
5. **PR 5**：P2 `Maui.Demo`（含 README）
6. **PR 6**：主 `README.md` 加 Quick Start 段落、連結 samples 總覽

每個 PR 都能獨立 merge、獨立跑，不需等到全部完成。

## 待確認

請使用者確認以下決策後再開工：

1. **Solution 策略**：採「獨立 `Bee.Samples.slnx`」還是「掛進主 `Bee.Library.slnx`」？
2. **資料庫**：第一版 demo 是否同意只用 SQLite？或要不要直接做 PostgreSQL/SQL Server 版本？
3. **Demo 範圍**：P0 + P1 是否符合預期？P2 (MAUI) 要不要這版就做？
4. **共用 FormSchema**：Employee（master-detail）合適嗎？還是想換成更貼近你心目中的場景（例：簡易訂單 SalesOrder + SalesOrderItem）？
5. **未來獨立 demo repo**：要先把名稱占下來嗎（例：`bee-library-demo`）？
