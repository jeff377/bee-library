# Bee.NET — Samples

最小可運行的 Bee.NET demo 集合。每個 demo 聚焦單一目的，採 `ProjectReference` 直接引用 `src/` 下的 library（不走 NuGet），改 library 即時反映。

> Solution：`samples/Bee.Samples.slnx`（獨立於主 `Bee.Library.slnx`，不會拖累主 CI / build 時間）。

## Quick Start（30 秒看到 Bee.NET 跑起來）

```bash
# Terminal 1 — 啟動 JSON-RPC API host
cd samples/QuickStart.Server
dotnet run

# Terminal 2 — 連線並呼叫 Echo BO
cd samples/QuickStart.Console
dotnet run
```

> 第一次 `dotnet run` 會在 `samples/Define/Master.key` 自動產生 master key、並在 `QuickStart.Server` 的工作目錄產生 SQLite `quickstart.db`。

## Demo 清單

| 順序 | 專案 | 對應 library | 目的 |
|------|------|--------------|------|
| **P0** | [`QuickStart.Server`](QuickStart.Server/README.md) | Bee.Api.AspNetCore + Bee.Hosting + Bee.Business + Bee.Db | 啟動 JSON-RPC API host，註冊一個自訂 Echo BO，串到 SQLite |
| **P0** | [`QuickStart.Console`](QuickStart.Console/README.md) | Bee.Api.Client | 示範 Local（in-process）與 Remote（HTTP）兩種 ConnectType，做 Ping + Echo |
| P1 | `Blazor.Server.Demo` | Bee.Web.Blazor.Server | （未來）FormSchema 動態渲染（Server） |
| P1 | `Blazor.Wasm.Demo` | Bee.Web.Blazor.Wasm + Bee.Api.Client | （未來）同上但走 remote API |
| P2 | `Maui.Demo` | Bee.UI.Maui + Bee.Api.Client | （未來）同一份 FormSchema 跨平台 |

## 共用 Define

`samples/Define/` 是 demo 共用的定義檔目錄。多個 demo 都會走「從執行目錄向上找 `Define/SystemSettings.xml`」的解析策略指向這裡，確保「同一份定義驅動多個前端」。

```
Define/
├── SystemSettings.xml                       # 最小可運作的系統設定（IsDebugMode=true）
├── DbCategorySettings.xml                   # 一個 common category
├── DatabaseSettings.xml                     # SQLite local DB（quickstart.db）
├── FormSchema/
│   └── Employee.FormSchema.xml              # master-detail 示範（員工 + 員工電話）
└── TableSchema/
    └── common/
        ├── ft_employee.TableSchema.xml
        └── ft_employee_phone.TableSchema.xml
```

## 不在這版的東西

刻意排除：

- 真實 ERP 業務情境（銷售單、進貨單等）— 留給未來獨立 demo repo
- SQL Server / PostgreSQL / Oracle / MySQL — SQLite 已足夠示範
- 認證 / 授權完整流程 — Echo 走 `[ApiAccessControl(Public, Anonymous)]`，不串 OAuth/JWT
- 部署腳本（Docker / k8s）
- Sample CI 驗證 — 手動驗證即可

## 自動化建置

```bash
dotnet build samples/Bee.Samples.slnx
```

> `test.sh` 與主 `Bee.Library.slnx` 都不會跑到 samples；samples 永遠是「想試時手動跑」。
