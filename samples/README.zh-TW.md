# Bee.NET — Samples

[English](README.md) | **繁體中文**

最小可運行的 Bee.NET demo 集合。每個 demo 聚焦單一目的,採 `ProjectReference` 直接引用 `src/` 下的 library(不走 NuGet),改 library 即時反映。

> Solution:[`samples/Bee.Samples.slnx`](Bee.Samples.slnx)(獨立於主 `Bee.Library.slnx`,不會拖累主 CI / build 時間)。

## 30 秒看到 Bee 跑起來

```bash
# Terminal 1 — 啟動 JSON-RPC API host
cd samples/QuickStart.Server
dotnet run                          # listen on http://localhost:5050

# Terminal 2 — 連線並呼叫 Echo BO
cd samples/QuickStart.Console
dotnet run
```

預期 Terminal 2 印出 `response : echo: hello from QuickStart.Console`。

要看 Blazor 元件實際渲染 `FormSchema`、走 Login + Employee CRUD:

```bash
# 方案 A — Blazor Server(in-process LocalApiProvider,無 HTTP round-trip)
cd samples/Blazor.Server.Demo
dotnet run                          # → http://localhost:5055

# 方案 B — Blazor Wasm(同元件、改走瀏覽器端 + HTTP /api)
cd samples/Blazor.Wasm.Demo.Host
dotnet run                          # → http://localhost:5060
```

兩邊都用 **`demo / demo`** 登入,登入後渲染同一份 `Employee` FormSchema。

## 我該從哪個 demo 看起

| 想了解 | 看這個 |
|--------|--------|
| 如何起一個 Bee 後端、註冊自訂 BO、暴露 JSON-RPC API | [`QuickStart.Server`](QuickStart.Server/README.zh-TW.md) |
| 如何用 `Bee.Api.Client` 從第三方端連 Bee(Remote 模式) | [`QuickStart.Console`](QuickStart.Console/README.zh-TW.md) |
| 如何在 Blazor 內用 `Bee.Web.Blazor.Server` 元件(Local 派遣,效能最佳) | [`Blazor.Server.Demo`](Blazor.Server.Demo/README.zh-TW.md) |
| 如何在 Blazor 內用 `Bee.Web.Blazor.Wasm` 元件(瀏覽器端跑 .NET,必走 HTTP) | [`Blazor.Wasm.Demo`](Blazor.Wasm.Demo/README.zh-TW.md) + [`.Host`](Blazor.Wasm.Demo.Host/README.zh-TW.md) |
| 同一份 `FormSchema` 在原生 App 上如何渲染 | [`Maui.Demo`](Maui.Demo/README.zh-TW.md) |
| 如何用純 JavaScript 從瀏覽器呼叫 Bee（前端無 .NET，走 Plain wire format） | [`Web.Js.Demo`](Web.Js.Demo/README.zh-TW.md) |
| Login、AccessToken、Encrypted payload 的客戶端 fallback 機制 | `Blazor.Server.Demo` 或 `Maui.Demo`(任一) |

## Demo 清單

| 專案 | 角色 | 預設 port | 啟動指令 | 對應 library |
|------|------|-----------|----------|--------------|
| [`QuickStart.Server`](QuickStart.Server/README.zh-TW.md) | API host | `5050` | `dotnet run` | Bee.Api.AspNetCore + Bee.Hosting + Bee.Business + Bee.Db |
| [`QuickStart.Console`](QuickStart.Console/README.zh-TW.md) | API client | — | `dotnet run` | Bee.Api.Client |
| [`Blazor.Server.Demo`](Blazor.Server.Demo/README.zh-TW.md) | 全端 Blazor Server | `5055` | `dotnet run` | Bee.Web.Blazor.Server + Bee.Samples.Shared |
| [`Blazor.Wasm.Demo`](Blazor.Wasm.Demo/README.zh-TW.md) | 瀏覽器端 Wasm 元件 | — | (由 `.Host` 一起跑) | Bee.Web.Blazor.Wasm |
| [`Blazor.Wasm.Demo.Host`](Blazor.Wasm.Demo.Host/README.zh-TW.md) | Wasm 靜態檔 + API host | `5060` | `dotnet run` | Bee.Api.AspNetCore + Bee.Web.Blazor.Wasm |
| [`Maui.Demo`](Maui.Demo/README.zh-TW.md) | 原生 App 客戶端 | —(連 5050) | `dotnet build -t:Run -c Debug -f net10.0-maccatalyst` | Bee.UI.Maui + Bee.Api.Client |
| [`Web.Js.Demo`](Web.Js.Demo/README.zh-TW.md) | 純 JS 瀏覽器客戶端 | —(連 5050) | `open index.html` | (無 .NET — vanilla HTML/JS) |
| [`Bee.Samples.Shared`](Bee.Samples.Shared/) | 共用後端 wiring | — | (被引用) | Bee.Business + Bee.Db + Bee.Hosting + Bee.Api.Client |

### Demo 之間的依賴

```
QuickStart.Console ──HTTP──▶ QuickStart.Server
                              (亦為 Maui.Demo 預設後端)

Maui.Demo          ──HTTP──▶ QuickStart.Server  ← 需先啟動
Web.Js.Demo        ──HTTP──▶ QuickStart.Server  ← 需先啟動（已開 CORS）

Blazor.Wasm.Demo   ◀────靜態檔──── Blazor.Wasm.Demo.Host
                                  (host 內含 Bee 後端與 /api endpoint,一起跑)

Blazor.Server.Demo                ← 不需另起 server,前後端同 process
```

## 共用帳號

Blazor / MAUI demo 的 Login 一律走 `demo / demo`:

| 欄位 | 值 |
|------|-----|
| User ID | `demo` |
| Password | `demo` |
| 顯示名稱 | `Demo User` |

由 [`DemoAuthenticatingSystemBusinessObject`](Bee.Samples.Shared/DemoAuthenticatingSystemBusinessObject.cs) 寫死比對,不查 `st_user`,因此**完全不需要 seed 任何系統資料表**。

`QuickStart.Server` / `QuickStart.Console` 的 `Echo.Echo` 標 `[ApiAccessControl(Public, Anonymous)]`,**不需要登入**。

## 共用 Define

[`samples/Define/`](Define/) 是所有 demo 共用的定義檔目錄。各 host 用「從執行目錄向上找 `Define/SystemSettings.xml`」的策略指向這裡(見 [`DemoBackend.ResolveDefinePath`](Bee.Samples.Shared/DemoBackend.cs)),確保「同一份定義驅動多個前端」。

```
Define/
├── SystemSettings.xml                       # 系統設定(IsDebugMode=true)
├── DbCategorySettings.xml                   # 一個 common category
├── DatabaseSettings.xml                     # SQLite local DB(quickstart.db)
├── FormSchema/
│   └── Employee.FormSchema.xml              # master-detail 示範(員工 + 員工電話)
├── TableSchema/
│   └── common/
│       ├── ft_employee.TableSchema.xml
│       └── ft_employee_phone.TableSchema.xml
└── Master.key                               # ⚠ 首次執行自動生成、被 .gitignore
```

## 首次執行自動生成的檔案

下列檔案**不會**進 git,是執行時產物。clone 下來首次 `dotnet run` 會自動建立:

| 檔案 | 由誰建立 | 內容 | gitignore 規則 |
|------|----------|------|----------------|
| `samples/Define/Master.key` | `AddBeeFramework(autoCreateMasterKey: true)` | 機器專屬主金鑰,加密所有 payload 用 | `samples/Define/Master.key` |
| `samples/<Host>/quickstart.db` | [`DemoSchemaSeeder`](Bee.Samples.Shared/DemoSchemaSeeder.cs) | SQLite,含 `ft_employee` + `ft_employee_phone` 兩張表與 3 筆 demo 資料(Alice / Bob / Carol) | `/samples/**/*.db` |

> 三個 host(`QuickStart.Server` / `Blazor.Server.Demo` / `Blazor.Wasm.Demo.Host`)**各有自己的 `quickstart.db`**,不會互相干擾。同一個 host 重跑會沿用既有資料(schema 建立與 seed 都是 idempotent)。

要重置:直接刪 `samples/<Host>/quickstart.db` 重跑即可。要重置 master key:刪 `samples/Define/Master.key` 並一併刪所有 `quickstart.db`(舊資料是用舊 key 加密的;保留舊 db 會解不開)。

## Local vs Remote 派遣模式

Bee 的 `Bee.Api.Client` 對呼叫端有**一致的 API 表面**,差異只在底層 provider:

| 模式 | 路徑 | 用於 | 範例 demo |
|------|------|------|-----------|
| **Local** | client → `LocalApiProvider` → `JsonRpcExecutor` → BO(同 process) | Blazor Server、in-process 工具、跨 BO 直接呼叫 | `Blazor.Server.Demo` |
| **Remote** | client → `RemoteApiProvider` → HTTP POST → `ApiServiceController` → `JsonRpcExecutor` → BO | Blazor Wasm、Console、MAUI、跨機器 | `QuickStart.Console`、`Blazor.Wasm.Demo`、`Maui.Demo` |

切換只是 `AddBeeBlazor` / `ApiClientInfo` 一行設定:

```csharp
// Local
builder.Services.AddBeeBlazor(o => o.UseLocalProvider());

// Remote
builder.Services.AddBeeBlazor(o => o.UseRemoteProvider("http://host:5060/api"));
```

## 建置全部 samples

```bash
dotnet build samples/Bee.Samples.slnx
```

> `./test.sh` 與主 `Bee.Library.slnx` 都**不會**跑到 samples;samples 永遠是「想試時手動跑」。

## 常見問題

**Q: Port 5050/5055/5060 被佔用怎麼辦?**
改 `samples/<Host>/Properties/launchSettings.json` 的 `applicationUrl`。記得連帶調整:依賴它的 client(`QuickStart.Console` 的 `--endpoint` 旗標、`Maui.Demo` 的 endpoint 輸入欄位、`MauiProgram.DefaultEndpoint`)。

**Q: 出現 `Could not locate 'Define/SystemSettings.xml' walking up from ...`?**
請從 bee-library checkout 目錄內執行 `dotnet run`,不要把 binary 拷貝到 repo 外。`DemoBackend` 是用「從 `AppContext.BaseDirectory` 向上找」的策略,跳出 repo 後找不到 `Define/`。

**Q: MAUI demo 為什麼必須跑 Debug?**
Apple 平台 Release-mode Mono linker 會砍 `System.Xml.Serialization` 反射 fallback,導致 `FormSchema` 反序列化失敗。詳見 [`Maui.Demo/README.zh-TW.md`](Maui.Demo/README.zh-TW.md#啟動-demo)。

**Q: 三個 host 同時跑會不會打架?**
不會。三個 host port 不同(5050 / 5055 / 5060),各自有獨立的 `quickstart.db`,共用 `samples/Define/` 但只讀不寫。三個都跑 + Console + Maui 一起測試完全可行。

**Q: 改了 `src/` 下的 library,要怎麼反映到 demo?**
重跑即可,`ProjectReference` 會自動 rebuild。不需要 `dotnet pack` / 也不需要清快取。

## 刻意不做

- 真實 ERP 業務情境(銷售單、進貨單等)— 留給未來獨立 demo repo
- SQL Server / PostgreSQL / Oracle / MySQL — SQLite 已足夠示範
- 認證 / 授權完整流程(OAuth、JWT、實際 `st_user` 表)— 用 hard-coded `demo/demo` 帶過
- 部署腳本(Docker / k8s / TestFlight / Microsoft Store)
- Sample CI 驗證 — 手動跑為主,僅 `Maui.Demo/.smoke.yaml` 可由 `demo-smoke` skill 一鍵冒煙
