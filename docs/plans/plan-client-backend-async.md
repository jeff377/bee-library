# 計畫：統一前端↔後端為 async 存取（消除 SyncExecutor 同步橋）

**狀態：📝 擬定中（2026-06-23）**

| 面向 | 範圍 | 狀態 |
|------|------|------|
| A | 連線初始化 async 統一（sync `Initialize`/`SetEndpoint`/`InitializeConnect` 仍在） | 📝 待做（async sibling 已存在，未統一） |
| B | `IDefineAccess` async 全套（remote 去 SyncExecutor） | 📝 待做 |
| C | 原則：消除「SyncExecutor 橋接 sync 介面」用法，前端存取後端一律 async | 📝 待做 |

## 目標

**前端存取後端用統一的 async 方法。** 目前 client→backend 存取是 sync / async 混雜：多數 sync 介面（連線初始化、`IDefineAccess`）靠 `SyncExecutor.Run(Task.Run(...).GetAwaiter().GetResult())` 橋接底層 async connector。這個同步橋：

- 在 **browser-wasm 單執行緒 runtime 直接拋** `PlatformNotSupportedException: Cannot wait on monitors`（阻塞唯一執行緒等 task，task 完成又需該執行緒 pump → deadlock）。
- 在桌面 / Server 雖能運作，但 sync-over-async 本就是反模式（執行緒佔用、潛在死結）。

統一為 async-all-the-way 後：WASM 前端可行、桌面同享非阻塞、`SyncExecutor` 可逐步退場。

> 緣起：`plan-northwind-web.md`（Northwind Avalonia WASM head）spike 連續踩到三個同根 SyncExecutor 雷（連線 ping/init、登入後 define 載入），見 [[wasm-client-async-init]]。連線初始化當時已加 async sibling（commit d26d7d1a），define 載入採窄修（VM 直走 connector）。本計畫把兩者收斂為「統一 async 存取」的框架正解。

## 面向 A：連線初始化 async 統一

**現況**：async sibling 已存在但與 sync 版並存——
- ✅ 已有：`ClientInfo.InitializeAsync(string)` / `SetEndpointAsync` / `ApiConnectValidator.ValidateAsync`（commit d26d7d1a）。
- ⚠️ 仍在且仍用 SyncExecutor：`ClientInfo.Initialize(string)` / `SetEndpoint` / `InitializeConnect`（`src/Bee.UI.Core/ClientInfo.cs`），`ApiConnectValidator.Validate`（`src/Bee.Api.Client/ApiConnectValidator.cs` 內 `IsEndpointReachableAsync` + `PingAsync` 兩處 SyncExecutor）。

**待做**：
- UI host 啟動流程（`ClientInfo.Initialize(IUIViewService, ...)` → `InitializeConnect`）改走 async，或提供 `InitializeAsync(IUIViewService, ...)`。
- 決定 sync `Initialize`/`SetEndpoint` 去留（見待決策）。
- `SystemApiConnector` 內 `GetFormSchema`/`GetFormLayout`/`GetDepartmentTree` 的 SyncExecutor 同步包裝（已有 async 原生）標 `[Obsolete]` 或移除。

## 面向 B：IDefineAccess async 全套

`IDefineAccess`（`src/Bee.Definition/Storage/IDefineAccess.cs`）整個介面同步（21 個 `GetX`/`SaveX`）；`RemoteDefineAccess`（`src/Bee.Api.Client/DefineAccess/RemoteDefineAccess.cs`）以 `SyncExecutor` 橋接 `Connector.GetDefineAsync`/`SaveDefineAsync`（async 原生皆已存在）。

**範圍（spike 已掃，~11 檔，中等規模）**：

### Phase B1：基礎層（純擴展，無 breaking）
- `IDefineAccess.cs` — 加 async 簽章（`GetDefineAsync` / `GetFormSchemaAsync` / `GetFormLayoutAsync` / `GetProgramSettingsAsync` / `GetLanguageAsync` / `SaveDefineAsync` 等）。
- `RemoteDefineAccess.cs` — async 實作 `await Connector.GetDefineAsync<T>(...)`，保留快取語意（async cache + 並發去重）。
- `src/Bee.ObjectCaching/LocalDefineAccess.cs` — async 以 `Task.FromResult(...)` 包既有同步邏輯（local 真同步、不受 WASM 影響）。

### Phase B2：BO 層轉 async（呼叫鏈）
- `src/Bee.Business/Form/FormBusinessObject.cs`（6 處 `DefineAccess.GetFormSchema`，public 方法多已 async）。
- `src/Bee.Business/System/SystemBusinessObject.cs`（2 處，方法已 async）。
- `src/Bee.Business/ProgramSettingsFormBoTypeResolver.cs`、`src/Bee.Business/Permission/ScopeResolver.cs`（**無 async 上游，較棘手** —— 注入 async provider 或讓 caller 預載）。

### Phase B3：UI 消費點
- Avalonia `FormView` / Blazor WASM `FormPage` 多已直接 `connector.GetDefineAsync`，受影響面小。
- Northwind 窄修（`FormsViewModel` 直走 connector）可評估是否回收為走 `IDefineAccess` async。

## 面向 C：原則 —— 消除 SyncExecutor 同步橋

`SyncExecutor` 的設計用途即「橋接同步介面（如 IDefineAccess）over async connector」。本計畫的終態是讓**前端存取後端不再需要這個橋**：所有 client→backend 路徑 async-all-the-way。`SyncExecutor` 完成 A/B 後評估是否整個移除（或僅保留給非前端、確定多執行緒安全的場景）。

## 測試
- `RemoteDefineAccess` / `ClientInfo` async 方法單元測試（鏡像既有 sync 版 guard/reachability 用例，2b 已示範）。
- BO async 呼叫鏈整合測試。
- WASM 冒煙（Northwind Web：connect → login → 開表單 → CRUD）。

## 待決策（開工前確認）

1. **sync API 去留**：sync `Initialize`/`SetEndpoint`/`IDefineAccess.GetX` 保留給桌面 / Server（其 SyncExecutor 在多執行緒可用），或長期廢棄、統一 async-only？「統一方法」目標傾向後者，但需評估既有呼叫端遷移成本。
2. **async 命名與粒度**：`GetXAsync` 全套 vs 只加最常用幾個（FormSchema / FormLayout / ProgramSettings / Language）。
3. **async 快取並發**：`RemoteDefineAccess` cache 在多個 await 同時 miss 同一 key 時的去重語意。
4. **`ProgramSettingsFormBoTypeResolver` / `ScopeResolver`**：無 async 上游 —— 注入 async provider vs caller 預載，影響介面設計。
5. **UI host 啟動 async 化**：桌面 head 的 `Program.Main` / lifetime 如何承接 async `Initialize`（Avalonia 啟動流程與 async 的接點）。

## 不在本計畫
- Northwind Web 其餘階段（dialog overlay / trimming / docs）—— 屬 `plan-northwind-web.md`。
- MessagePack 在 WASM 的 source-gen（Northwind 用 JSON payload 即可；MessagePack WASM 為另一獨立議題）。
