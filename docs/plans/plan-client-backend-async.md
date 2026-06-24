# 計畫：前端↔後端 async 存取（解耦 ClientDefineAccess、消除 SyncExecutor）

**狀態：✅ 已完成（2026-06-24）**

| 面向 | 範圍 | 狀態 |
|------|------|------|
| A | 連線初始化 async 統一（`ClientInfo.Initialize`/`SetEndpoint`/`InitializeConnect` + `ApiConnectValidator` 去 SyncExecutor + host 啟動 async 接點） | ✅ 已完成（2026-06-24） |
| B | `ClientDefineAccess` 從 `IDefineAccess` 解耦 → client 端 async 型別化 define 快取（read+save）；`IDefineAccess` 維持 server-side sync 不動 | ✅ 已完成（2026-06-24） |
| C | A/B 完成後整個移除 `SyncExecutor`（剩餘只有 `SystemApiConnector` 3 個便利包裝，已有 async 原生） | ✅ 已完成（2026-06-24） |

## 目標

**前端存取後端一律 async。** sync-over-async 橋（`SyncExecutor.Run(...GetAwaiter().GetResult())`）在 browser-wasm 單執行緒 runtime 直接拋 `PlatformNotSupportedException: Cannot wait on monitors`，桌面雖能運作但本就是反模式。統一 async 後 WASM 前端可行、桌面同享非阻塞、`SyncExecutor` 退場。

> 緣起：`plan-northwind-web.md`（Northwind Avalonia WASM head）spike 連續踩到三個同根 SyncExecutor 雷，見 [[wasm-client-async-init]]。連線初始化已加 async sibling（commit d26d7d1a），define 載入採窄修（VM 直走 connector）。本計畫收斂為框架正解。

## 關鍵掃描結論（重塑了原 plan 範圍）

開工前 spike 掃描推翻了原 plan「讓 `IDefineAccess` 全套 async」的前提：

1. **`SyncExecutor` 很收斂 —— 只有 10 處，全在 client 端**：`ApiConnectValidator`(2)、`SystemApiConnector`(3，已有 async 原生)、`ClientDefineAccess`(2)、`ClientInfo`(2)。
2. **`IDefineAccess` 是 server-side 同步抽象**：被 Bee.Db / Bee.Repository / Bee.Business 共 30+ 檔消費，全走 `LocalDefineAccess`（真同步、不碰 SyncExecutor、無 WASM 問題）。把整個介面轉 async 會把 churn 灌進整個後端，對 WASM 目標零貢獻。
3. **client 端 `ClientInfo.DefineAccess`（→ `ClientDefineAccess`）沒有任何活的呼叫端**：全 repo 唯一提及處是 `FormsViewModel.cs:22` 的**註解**，內容正是「避開它，因為 SyncExecutor 會 block 單執行緒」。所有 client UI（Avalonia `FormView`/`ListView`/`LookupDialog`、MAUI `FormPage`、Northwind WASM）早就直接走 `connector.GetDefineAsync<T>(DefineType, keys[])`。
4. 原 Phase B2 點名的兩個「棘手 resolver」(`ProgramSettingsFormBoTypeResolver` / `ScopeResolver`) 在 `Bee.Business`，只被 server 端專案引用，**不在 WASM 路徑上** → **整段 B2 與原決策 #4 移除**。

→ 正解不是「給 `IDefineAccess` 加 async」，而是**承認 `ClientDefineAccess` 是條 IDefineAccess sync 契約綁出來的死橋，把它從介面解耦、轉成 client 端 async 型別化 define 快取**。`IDefineAccess` 維持純 server-side sync，概念分離乾淨。

## 面向 A：連線初始化 async 統一

**現況**：async sibling 已存在但與 sync 版並存——
- ✅ 已有：`ClientInfo.InitializeAsync(string)` / `SetEndpointAsync` / `ApiConnectValidator.ValidateAsync` / `ValidateRemoteAsync`（commit d26d7d1a）。
- ⚠️ 仍用 SyncExecutor：
  - `ClientInfo.SetEndpoint`（`ClientInfo.cs:178`）、`InitializeConnect`（`:216`）。
  - `ApiConnectValidator.ValidateRemote`（`ApiConnectValidator.cs:157` reachable + `:161` ping）。

**待做**：
- **A1** — host 啟動入口**直接 async 化**（不留 sync 版、不做 sibling，因全方案無呼叫端）：
  - `ClientInfo.Initialize(IUIViewService, SupportedConnectTypes)`（`:231`）→ `InitializeAsync(IUIViewService, ...) : Task<bool>`，sync 版直接移除。
  - `InitializeConnect`（`:208`，private）→ `InitializeConnectAsync : Task<bool>`，內部 `await SystemApiConnector.InitializeAsync()`（去 SyncExecutor）。
  - `IUIViewService.ShowApiConnect()`（`src/Bee.UI.Core/IUIViewService.cs:12`）→ `ShowApiConnectAsync : Task<bool>`（連線設定 fallback view）。
  - **註**：`IUIViewService` 全方案**零實作端**（僅介面定義 + ClientInfo 消費），整條 host-啟動 fallback 路徑目前休眠；A1 為「休眠 API 直接轉 async」，無下游實作端波及。
- **A2** — sync 入口的實際呼叫端遷移（僅 2 處，皆 `Initialize(string)` 多載 + 桌面 `Task.Run` 反模式）：
  - `samples/Avalonia.Demo/ViewModels/ConnectionViewModel.cs:87`：`await Task.Run(() => ClientInfo.Initialize(endpoint))` → `await ClientInfo.InitializeAsync(endpoint)`（拿掉 `Task.Run`）。
  - `samples/Maui.Demo/Pages/ConnectionPage.cs:100`：同樣。
  - （`apps/Bee.Northwind/.../ConnectionViewModel.cs:85` 已用 `InitializeAsync` ✅）
- **A3** — sync `Initialize`/`SetEndpoint`/`InitializeConnect` + `ApiConnectValidator.ValidateRemote` 標 `[Obsolete]` 後移除（不能保留 —— 它們是 SyncExecutor 的唯一殘留呼叫端）。
- **A4 — 跨專案 doc-cref sweep（必須與 A3 原子落地）**：移除 sync 方法會讓引用它的 `<see cref>` 觸發 CS1574，`TreatWarningsAsErrors=true` 下直接 build 失敗。需同步更新的 doc-comment（跨 4 專案 ~7 處）：
  - `src/Bee.UI.Avalonia/Storage/FileEndpointStorage.cs:13-14`
  - `src/Bee.UI.Maui/Storage/MauiPreferenceEndpointStorage.cs:15`
  - `apps/Bee.Northwind/Bee.Northwind.Browser/Storage/BrowserLocalStorageEndpointStorage.cs:16`
  - `apps/Bee.Northwind/Bee.Northwind.UI/ViewModels/ConnectionViewModel.cs:10,62`（已遷 async 但 cref 仍指 sync）
  - `samples/Avalonia.Demo/ViewModels/ConnectionViewModel.cs:10,64`
  - `samples/Maui.Demo/Pages/ConnectionPage.cs:8`

> **原子性**：A2+A3+A4 必須同一批落地。`TreatWarningsAsErrors` 下任何殘留的 sync 呼叫端或 stale cref 都會 build 壞，無法分批 merge。

## 面向 B：ClientDefineAccess 解耦為 client async 型別化快取

**設計**（取代原「IDefineAccess 全套 async」）：

`ClientDefineAccess` **不再實作 `IDefineAccess`**，改為 client 端獨立具體類別 —— 以 async 型別化方法包裝 `SystemApiConnector.GetDefineAsync` + 保留快取：

- 解決兩個「窄修繞過」犧牲掉的東西：
  - **直覺型別化 API**：`GetFormSchemaAsync(progId)` 取代生硬的 `GetDefineAsync<FormSchema>(DefineType.FormSchema, [progId])`（那個 `keys[]` 正是不直覺處）。
  - **client 端快取**：保留既有 `List` cache + 租戶切換 `ClearCache()`；窄修直走 connector 每次重抓、快取已失效。
- `IDefineAccess` 自此純 server-side sync 抽象，完全不動。

**Phase 拆解**：

### Phase B1：ClientDefineAccess 解耦 + async 化
- `src/Bee.Api.Client/ClientDefineAccess.cs`（命名空間 `Bee.Api.Client` root）：移除 `: IDefineAccess`；read 端轉 `GetFormSchemaAsync` / `GetFormLayoutAsync(customizeId, layoutId)` / `GetProgramSettingsAsync` / `GetLanguageAsync` / `GetTableSchemaAsync` / `GetSystemSettingsAsync` / `GetDatabaseSettingsAsync` / `GetDbCategorySettingsAsync` / `GetPermissionModelsAsync`，內部 `await Connector.GetDefineAsync<T>(...)`。
- **read + save 都做**：`SaveDefineAsync` / `SaveFormSchemaAsync` / … 對應寫端（供 client 端 DefineEditor 等存檔場景）。
- async 快取並發去重：以 `Dictionary<string, Task<object>>` 快取「in-flight task」而非結果，多個 await 同時 miss 同一 key 共用同一條 task（WASM 單執行緒風險低，仍以此為正確語意）。`ClearCache()` 保留。

### Phase B2：ClientInfo.DefineAccess 型別轉具體
- `src/Bee.UI.Core/ClientInfo.cs`：`DefineAccess` property 型別 `IDefineAccess` → 具體 `ClientDefineAccess`（`:115`）；`ClearCache` 連動處（`:135`）改直呼，不再 `as ClientDefineAccess`。

### Phase B3：回收 client UI 窄修呼叫端
本次一併把「直走 connector」改回走 `ClientInfo.DefineAccess.GetXAsync`，恢復快取 + 可讀性：
- **無 seam（直接走 ClientInfo）**：`Controls/Editors/LookupDialog.cs`（FormSchema）、`apps/.../FormsViewModel.cs`（ProgramSettings，連 `:22` 「避開」註解一併更新）→ 直接改走 `ClientInfo.DefineAccess.GetXAsync`。
- **有 schema 注入 seam 的控件**：`FormView` / `ListView` / `FormPage` 原本走 `protected virtual ResolveSystemConnector()` —— 該 seam 既是 production connector hook **也是測試的 schema 注入點**（`TestFormView` / `FormPageTestProbe` 覆寫它讓測試不碰 static `ClientInfo`）。
  - **決策**：把 `ResolveSystemConnector()` 重構為 `protected virtual Task<FormSchema?> ResolveSchemaAsync(string progId)`，預設 `=> ClientInfo.DefineAccess.GetFormSchemaAsync(progId)`。production 走快取；測試覆寫此 hook 回 stub schema、維持隔離。三個 hook（`ResolveSchemaAsync` / `ResolveFormConnector` / `ResolveAccessToken`）對稱。
  - 連帶更新 3 個控件測試（`FormViewTests` / `ListViewTests` / `FormPageClientInfoTests`）的 override 目標。

## 面向 C：移除 SyncExecutor

A3 + B1 完成後，`SyncExecutor` 唯一殘留呼叫端只剩 `SystemApiConnector` 的 3 個便利包裝（`GetFormSchema`/`GetDepartmentTree`/`GetFormLayout`，皆已有 async 原生）。將呼叫端導向 async 原生後，刪除這 3 個 sync 包裝與 `src/Bee.Api.Client/SyncExecutor.cs`。

## 測試
- `ClientDefineAccess` async 方法單元測試（快取命中 / miss / 並發去重 / `ClearCache` 後重抓）。
- `ClientInfo` async 啟動路徑（`InitializeAsync` guard / reachability，鏡像既有 sync 用例）。
- WASM 冒煙（Northwind Web：connect → login → 開表單 → CRUD）。
- 桌面 head（Avalonia）回歸：async 啟動後仍能正常連線 / 載表單。

## 已定案決策

1. **IDefineAccess 去留**：維持 server-side sync，**不加 async**。client 的 async 需求由解耦後的 `ClientDefineAccess` 承接。
2. **ClientDefineAccess**：從 `IDefineAccess` 解耦為 client 端獨立類別；async 表面 **read + save 都做**。命名由舊 `RemoteDefineAccess` 改為 `ClientDefineAccess`（transport 中立 —— `SystemApiConnector` 本就同時支援近端/遠端，`Remote` 前綴不精確）。
3. **ClientInfo.DefineAccess 型別**：具體 `ClientDefineAccess`。
4. **窄修呼叫端**：本次一併回收成走 `ClientInfo.DefineAccess.GetXAsync`。
5. **B2（BO/resolver 轉 async）**：移除 —— server-side、走 sync-safe LocalDefineAccess，零效益 churn。
6. **SyncExecutor**：A/B 後整個移除。

## 風險 / 待實作釐清

- **桌面 head async 啟動接點**（A2）：Avalonia `Program.Main` / App lifetime 如何承接 async `Initialize` —— `OnFrameworkInitializationCompleted` 走 async 流程的接法需於 A2 落地時確認，避免 fire-and-forget 吞例外。
- **`IUIViewService.ShowApiConnect()`**：連線設定 fallback view 目前 sync；若連線探測 async 化，此處可能需 async sibling。

## 不在本計畫
- Northwind Web 其餘階段（dialog overlay / trimming / docs）—— 屬 `plan-northwind-web.md`。
- MessagePack 在 WASM 的 source-gen（Northwind 用 JSON payload 即可）。
