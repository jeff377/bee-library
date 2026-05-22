# ADR-013：前端 API 連線策略 — `Bee.UI.*` 與 `Bee.Web.*` 兩條 family 分流

## 狀態

已採納（2026-05-22）

## 背景

Bee.NET 在 v4.4 階段同時擁有三類前端 host:

| 前端類型 | 代表套件 | 部署 / 執行環境 |
|---------|---------|----------------|
| **桌面端** | `Bee.UI.Core`(共通)、`Bee.UI.Maui`(未來)、`Bee.UI.WinForms`(未來,獨立 repo) | iOS / Android / macOS / Windows / 桌面 OS native |
| **Blazor Server** | `Bee.Web.Blazor.Server` | ASP.NET Core server-rendered,with SignalR circuit |
| **Blazor WASM** | `Bee.Web.Blazor.Wasm` | Browser sandbox(WebAssembly) |

這三類前端**對「如何取得 / 持久化 API 連線狀態」的需求結構性不同**:

| 維度 | 桌面端 | Blazor Server | Blazor WASM |
|------|--------|---------------|-------------|
| 連線資訊存放 | 本機檔案(`{ExeName}.Settings.xml`) | Server 端 DI scope / circuit state | Browser 端記憶體 / localStorage |
| Endpoint 設定流程 | 啟動時讀檔 → 不可達則彈 dialog 讓使用者輸入 | 由宿主 startup 注入或讀 appsettings | 由宿主 startup 注入或讀 JS interop |
| Token 管理 | static singleton(`ClientInfo.AccessToken`) | DI-scoped(per circuit) | DI-scoped(per app instance) |
| UI 互動需求 | 需要對話框服務(`IUIViewService.ShowApiConnect()`) | 走 Razor component 流程,無 dialog 抽象 | 同 Server |
| 連線方式 | Local 或 Remote(可雙模式) | Local(in-process)或 Remote(HTTP) | **只能 Remote(HTTP)** —— Browser 無法載入後端組件 |

如果強要**單一連線抽象**涵蓋三類前端,會出現結構性矛盾:

1. **桌面端需要的 `IUIViewService.ShowApiConnect()` dialog 抽象,在 Blazor 環境無對應**(Razor component 模型完全不同),抽象會變空殼或語意錯置
2. **`ClientInfo` 用 static singleton 維持狀態 fits 桌面端**(一個 process = 一個使用者),但對 **Blazor Server 完全錯誤**(同 process 內多個 user circuit 共享 static 會 cross-user data leak)
3. **桌面端的檔案 IO 持久化**(`{ExeName}.Settings.xml`)在 Browser WASM 沙箱內**根本不可用**

歷史上 v4.3 之前 `Bee.UI.Core` 設計時只想到桌面端,`ClientInfo` 自然走 static singleton。
v4.4 加入 Blazor RCL 時若強行讓 Blazor 走 `Bee.UI.Core`,就會踩到上述問題。

## 決策

採**兩條 family 分流**:

### Family A:`Bee.UI.*`(消費 `Bee.UI.Core` 抽象)

- **消費對象**:`ClientInfo` static singleton、`IEndpointStorage`、`IUIViewService`、`VersionInfo`
- **適用前端**:桌面端 / native UI(MAUI、WinForms、WPF、Avalonia 等)
- **連線模型**:
  - `ClientInfo.Initialize(uiService, supportedConnectTypes)` 在 App 啟動時呼叫
  - `ClientInfo.SetEndpoint(endpoint)` 設定 endpoint(Local 路徑或 Remote URL),內部走 `SyncExecutor.Run(() => SystemApiConnector.InitializeAsync())`
  - `ClientInfo.ApplyLoginResult(loginResponse)` 套用登入結果
  - 透過 `ClientInfo.SystemApiConnector` / `ClientInfo.CreateFormApiConnector(progId)` 取得 connector
  - 持久化由 `IEndpointStorage`(預設實作:檔案);UI 對話流程由 `IUIViewService` 提供
- **目前成員**:
  - `Bee.UI.Core`(共通)
  - `Bee.UI.Maui`(Phase 0 placeholder,Phase 1 加實際控制項時擴 multi-target)
  - 未來:`Bee.UI.WinForms`、`Bee.UI.Wpf`、`Bee.UI.Avalonia` 等同理

### Family B:`Bee.Web.*`(獨立 family,**不**消費 `Bee.UI.Core`)

- **不消費 `Bee.UI.Core`**:Blazor 環境無檔案 IO / dialog service 概念,共通抽象無對應
- **適用前端**:Blazor Server、Blazor WASM,以及未來其他 Web framework(`Bee.Web.React.*` 等)
- **連線模型**:
  - 透過宿主 `IServiceCollection.AddBeeFramework(...)` 或自訂 DI 設定 `IApiProvider`
  - `LocalApiProvider`(in-process,Blazor Server 可選)或 `RemoteApiProvider`(HTTP,WASM 強制)
  - `SystemApiConnector` / `FormApiConnector` 由 DI scope 注入到 Razor component
  - 狀態管理由 component / `CascadingValue` / Razor scoped service 處理
  - **WASM 嚴禁相依任何後端組件**(Repository / Business / Hosting 等),由相依鏈強制
- **目前成員**:`Bee.Web.Blazor.Server`、`Bee.Web.Blazor.Wasm`

### Family 判別準則(新加套件時依此判斷)

> **是否消費 `Bee.UI.Core` 抽象(`ClientInfo` / `IEndpointStorage` / `IUIViewService` 等)?**
>
> - **消費** → 歸 `Bee.UI.*` family
> - **不消費,有自己的狀態管理 / dialog 模型** → 走獨立 family prefix(如 `Bee.Web.*`)

這個準則是「**現實對應**」而非「**理想分類**」:`Bee.UI.Core` 抽象是為桌面端設計,
Web / Blazor 環境結構性不同,**不該勉強套用**。

## 後果

### 正面

- **桌面端 vs Web 端各自簡潔**:沒有「兩邊都要委屈」的共通抽象
- **WASM 安全性自動保護**:`Bee.Web.*` 不依賴 `Bee.UI.Core` 連帶不依賴任何 server-only 組件
- **Family 判別準則明確**:未來加新前端套件時不需再開一輪辯論
- **演進獨立**:`Bee.UI.Core` 可以為桌面端優化(如 async 化 `ClientInfo`),不影響 Blazor;反之亦然

### 負面

- **看似「重複」**:兩條 family 都各自有 `SystemApiConnector` 包裝、connection state 管理,
  讀者第一眼會問「為何不共用?」。本 ADR 即為回答此問題的文件
- **跨 family 共用組件成本**:若未來真有「兩 family 都需要」的共通邏輯,
  需要往更下層放(如 `Bee.Api.Client` 已是兩 family 共用的最低層)
- **新 family 的命名負擔**:若未來出現「既不是 native UI 又不是 Web」的前端
  (如 CLI tool、background worker UI),要再決定 prefix(可能走 `Bee.Console.*` 等)

### 中性

- **`Bee.Api.Client` 是兩 family 共用的最低層**:不在分流之內,維持為純通訊 / 序列化 / 加密層,
  兩 family 都消費它(Blazor 直接消費;`Bee.UI.Core` 包裝後給桌面端消費)

## 相關連結

- 依賴關係視覺化:`docs/dependency-map.md`
- 各前端的實際操作範例:`docs/development-cookbook.md` §「Frontend API Connection Patterns」
- 桌面端 connection 流程的 sync→async 演進:`docs/plans/plan-deprecate-sync-api.md`
- `Bee.UI.Maui` Phase 0 placeholder 設計:`docs/plans/plan-add-bee-ui-maui.md`
- 後端 DI 取代靜態 Service Locator(影響 Blazor host 註冊方式):[ADR-011](adr-011-di-replaces-service-locator.md)

## 不在範圍

- **未來「同時提供 ClientInfo + DI」的混合模式**:目前未需要,實際 use case 出現再評估
- **`Bee.UI.Core` 本身的 static state DI 化**:屬於桌面端 family 內的重構,不影響 Web family,留後續獨立決策
- **Blazor Hybrid(MAUI 內嵌 Blazor)**:可能需要橫跨兩 family,屆時開新 ADR 評估
