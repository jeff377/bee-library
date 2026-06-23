# 計畫：框架級 async IDefineAccess（remote 定義存取去 SyncExecutor）

**狀態：📝 擬定中（2026-06-23）**

## 背景

`IDefineAccess` 整個介面是**同步**的（21 個 `GetX` / `SaveX`）。remote 實作 `RemoteDefineAccess` 用 `SyncExecutor.Run(Task.Run(...).GetAwaiter().GetResult())` 橋接 async connector 呼叫 —— `SyncExecutor` 的 XML 註解本就寫「Intended only for bridging synchronous interfaces (e.g. IDefineAccess)」。

這個同步橋在 **browser-wasm 單執行緒 runtime 本質不可行**：阻塞唯一執行緒等 task，task 完成又需同一執行緒 pump event loop → `PlatformNotSupportedException: Cannot wait on monitors on this runtime`。

於 `plan-northwind-web.md`（Northwind Avalonia WASM head）spike 中發現：連線後 `FormsViewModel` 同步呼叫 `ClientInfo.DefineAccess.GetProgramSettings()` 即崩。當時採**窄修**（VM 直接走 `connector.GetDefineAsync`，繞過 `RemoteDefineAccess`），足以讓 Northwind Web 跑通，但**框架層的 sync IDefineAccess 仍對所有 WASM 前端是地雷**。本計畫處理框架層正解。

> 相關：[[wasm-client-async-init]] 已為「連線路徑」做過同類 async sibling（`ClientInfo.InitializeAsync` / `ApiConnectValidator.ValidateAsync`），本計畫是把同一手法推廣到 define 存取。

## 目標

為 `IDefineAccess` 加 **async 全套**（不破壞既有 sync API，桌面 / Server 維持 sync），讓 remote + WASM 前端能 async-all-the-way 載入定義。

## 範圍（spike 已掃，~11 檔，中等規模）

### Phase 1：基礎層（純擴展，無 breaking）
- `src/Bee.Definition/Storage/IDefineAccess.cs` — 加 async 簽章（`GetDefineAsync` / `GetFormSchemaAsync` / `GetFormLayoutAsync` / `GetProgramSettingsAsync` / `GetLanguageAsync` / `SaveDefineAsync` 等）。
- `src/Bee.Api.Client/DefineAccess/RemoteDefineAccess.cs` — async 實作，直接 `await Connector.GetDefineAsync<T>(...)`（connector 的 `*Async` 皆已存在），保留既有快取語意（async cache）。
- `src/Bee.ObjectCaching/LocalDefineAccess.cs` — async 實作以 `Task.FromResult(...)` 包既有同步邏輯（local 真同步、不受 WASM 影響）。

### Phase 2：清理無用 sync 包裝
- `src/Bee.Api.Client/Connectors/SystemApiConnector.cs` — `GetFormSchema` / `GetFormLayout` / `GetDepartmentTree` 的 `SyncExecutor` 同步包裝（已有 async 原生）標 `[Obsolete]` 或移除。
- `src/Bee.UI.Core/ClientInfo.cs` — `SetEndpoint` / `InitializeConnect` 的 `SyncExecutor` 已有 async sibling（`SetEndpointAsync` / `InitializeAsync`），評估是否再清理。

### Phase 3：BO 層轉 async（呼叫鏈）
- `src/Bee.Business/Form/FormBusinessObject.cs`（6 處 `DefineAccess.GetFormSchema`，public 方法多已 async，直接 await async 版）。
- `src/Bee.Business/System/SystemBusinessObject.cs`（2 處，方法已 async）。
- `src/Bee.Business/ProgramSettingsFormBoTypeResolver.cs`、`src/Bee.Business/Permission/ScopeResolver.cs`（無 async 上游，需加 async 路徑或注入 async provider —— 較棘手）。

### Phase 4：UI 消費點
- 各前端 UI 改用 async 版（Avalonia `FormView` / Blazor WASM `FormPage` 多已直接走 `connector.GetDefineAsync`，受影響面比想像小）。
- Northwind 窄修（`FormsViewModel`）可在框架 async IDefineAccess 完成後評估是否回收為走 `IDefineAccess` async。

### Phase 5：測試
- `RemoteDefineAccess` async 方法單元測試（鏡像 sync 版）。
- BO async 呼叫鏈整合測試。

## 待決策（開工前確認）

1. **async 命名**：`GetXAsync` 全套 vs 只加最常用的幾個（FormSchema / FormLayout / ProgramSettings / Language）？
2. **sync API 去留**：sync `IDefineAccess` 保留給桌面 / Server（其 `SyncExecutor` 在多執行緒環境可用），或長期廢棄統一 async？
3. **快取**：async `RemoteDefineAccess` 的 cache 並發語意（多個 await 同時 miss 同一 key 時的去重）。
4. **`ProgramSettingsFormBoTypeResolver` / `ScopeResolver`**：這兩處無 async 上游，是注入 async provider 還是讓 caller 預載 → 影響介面設計。

## 不在本計畫

- Northwind Web 其餘階段（dialog overlay / trimming / docs）—— 屬 `plan-northwind-web.md`。
- MessagePack 在 WASM 的 source-gen（目前 Northwind 用 JSON payload 也能跑；MessagePack WASM 是另一獨立議題）。
