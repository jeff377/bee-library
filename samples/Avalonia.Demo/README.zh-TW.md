# Avalonia.Demo

[English](README.md) | **繁體中文**

Bee.NET 範例專案：Avalonia 桌面用戶端，連 `samples/QuickStart.Server` 的 JSON-RPC API，渲染共用的 `Define/FormSchema/` 定義（Employee / Department / Project）。結構上對應 `samples/Maui.Demo`，但走桌面 Avalonia（Windows / macOS / Linux）而非 MAUI 的行動裝置矩陣，並額外展示 **lookup 開窗流程**（relation 欄位開啟搜尋選取對話框）。

## 它要證明什麼

- 同一份 `FormSchema` 在 **四種** UI family 都能渲染 — Blazor Server / Blazor Wasm / MAUI / Avalonia — 都共用 `samples/Define/`
- `Bee.UI.Avalonia.Controls.FormView` 在「host 只給 `ProgId`」的情況下，會自動向 `Bee.UI.Core.ClientInfo` 取 connector / schema / access token（鏡像 MAUI `FormPage` 的 fallback）
- Avalonia 桌面 app 走 `ConnectType.Remote` 經 HTTP 打 JSON-RPC，與 Blazor.Wasm / Maui.Demo 走同一條 wire path
- Avalonia DataGrid 的 `DataRowView` indexer binding 對 `DataTable` 驅動的列表呈現是可行路徑（不需要 `ITypedList` schema introspection）

## 前置條件

1. **.NET 10 SDK** — 系統層安裝即可。**不需** Avalonia workload（Avalonia 是純 NuGet library，沒有 MAUI / Xcode 的相依問題）。

2. **後端**：另一個 terminal 跑

   ```sh
   cd samples/QuickStart.Server
   dotnet run
   ```

   server 預設聽 `http://localhost:5050`。`QuickStart.Server` 已經透過 `Bee.Samples.Shared.DemoBackend` 接 `DemoAuthenticatingSystemBusinessObject`，第一次啟動會自動建 SQLite + 種 3 筆 Employee 資料。

## 啟動 demo

```sh
cd samples/Avalonia.Demo
dotnet run --configuration Debug
```

開發階段請用 **`-c Debug`**。桌面 Avalonia 的 Release 也可以跑（不像 MAUI Apple 平台有 Mono linker 砍 `System.Xml.Serialization` 反射的問題，Avalonia 走完整 .NET runtime），但如果未來執行 `dotnet publish -p:PublishTrimmed=true` 還是會踩到一樣的雷。trim 的解法不在此 sample 範圍。

## 預期畫面

1. **Connection 頁**：endpoint 預設 `http://localhost:5050/api`。按 **Connect** 會呼叫 `ClientInfo.Initialize(endpoint)`，由 `ApiConnectValidator` 做 HTTP reachability + `system.ping`。成功後主視窗切到 Login。
2. **Login 頁**：兩格 textbox，預設帶入 `demo` / `demo`（對應 QuickStart seed）。按 **Sign in** 呼叫 `SystemApiConnector.LoginAsync`，token 透過 `ClientInfo.ApplyLoginResult` 存起來，視窗前進到 Forms 頁。
3. **Forms 頁**：tab 列，每個 tab 各放一個 `<bee:FormView ProgId="..." />`。各表單不傳 `Schema` / `FormConnector`，由 fallback 從 `ClientInfo` 取得（`GetDefineAsync<FormSchema>` / `CreateFormApiConnector` / `AccessToken`）：
   - **Employee** — 原本的 master-detail 表單；三筆種子資料（Alice / Bob / Carol）列在上方，點任一列驅動下方 `DynamicForm`，**New** / **Save** / **Delete** 走 BO 回 server。
   - **Department** — 純主檔表單，同時是 **lookup 來源**；schema 顯式宣告 `LookupFields="sys_id,sys_name"`（與 server 預設集相同，宣告是為了示範語法）。
   - **Project** — **lookup 展示**：主表「Owner Department」欄位呈現為 `ButtonEdit`，按 icon 開部門選取窗（搜尋走 server 端 `GetLookup`）；Project Members 明細的「Member」欄點 cell 即開員工選取窗（InCell lookup）。選取後 rowid + 映射的 `ref_*` 顯示欄位經 `FormDataObject` 寫回；主表 lookup 欄按 Delete/Backspace 可清空。

### Lookup 操作流程

1. 在 **Department** tab 先建一兩個部門（如 `D001 / Engineering`）。
2. 切到 **Project**，按 **New**，點「Owner Department」的放大鏡 —— 搜尋、雙擊列（或選取 + OK），「D001 Engineering」（編號+名稱）即帶出。
3. 在 **Project Members** 新增一列、點「Member」cell —— 員工選取窗開啟，挑一筆種子員工。
4. **Save** 後從清單重選該專案：`ref_*` 欄位此時來自 server 端 relation JOIN（單一真相來源）。

## 對應 library 元件

| Demo 行為 | library 元件 |
|-----------|--------------|
| Connect → endpoint 驗證 | [src/Bee.UI.Core/ClientInfo.cs](../../src/Bee.UI.Core/ClientInfo.cs) + [src/Bee.Api.Client/ApiConnectValidator.cs](../../src/Bee.Api.Client/ApiConnectValidator.cs) |
| Endpoint 持久化 | [src/Bee.UI.Avalonia/Storage/FileEndpointStorage.cs](../../src/Bee.UI.Avalonia/Storage/FileEndpointStorage.cs) |
| Login → token | [src/Bee.Api.Client/Connectors/SystemApiConnector.cs](../../src/Bee.Api.Client/Connectors/SystemApiConnector.cs)（LoginAsync） |
| Employee 表單渲染 | [src/Bee.UI.Avalonia/Controls/FormView.cs](../../src/Bee.UI.Avalonia/Controls/FormView.cs) → DynamicForm / DynamicGrid |
| FormSchema fallback | `FormView` 的 ResolveSystemConnector / ResolveFormConnector / ResolveAccessToken |
| CRUD wire path | FormApiConnector → RemoteApiProvider → QuickStart.Server `ApiController` → DemoBusinessObjectFactory → FormBusinessObject |

## 與其他 demo 的關係

`QuickStart.Server` 就是 `Maui.Demo` 用的同一個 `Bee.Samples.Shared.DemoBackend` host。兩個 client 打同一份 SQLite seed，你可以同時跑 Avalonia demo + MAUI demo，在其中一邊做 Save，另一邊下次重新拉列表就會看到。

| Sample | UI family | 後端 | Wire |
|--------|-----------|------|------|
| Blazor.Server.Demo | Razor + Blazor Server | in-process Local | 無 |
| Blazor.Wasm.Demo | Razor + Blazor Wasm | Blazor.Wasm.Demo.Host | JSON-RPC over HTTP |
| Maui.Demo | MAUI Shell + ContentPage | QuickStart.Server | JSON-RPC over HTTP |
| **Avalonia.Demo** | **Avalonia Window + UserControl + MVVM** | **QuickStart.Server** | **JSON-RPC over HTTP** |

## MVVM 慣例

Sample 用 `CommunityToolkit.Mvvm` 的 source generator（與 `tools/DefineEditor` 同套）。主要 idiom：

- `ViewModelBase : ObservableObject` — 給 `ViewLocator` routing 用的 marker
- `[ObservableProperty]` 標 private field — 自動生對應的 `INotifyPropertyChanged` 屬性
- `[RelayCommand]` 標 async method — 自動生 `IAsyncRelayCommand`
- `[NotifyPropertyChangedFor(nameof(IsNotBusy))]` — 衍生屬性連動觸發
- 頁面切換用 plain `Action` callback 串到子 VM 的 ctor（而不是 CLR event）— 整個導航圖在 `MainWindowViewModel` 建構時就明確，不需 unsubscribe

## 不做的事

- 多個 detail 表 / 自訂 widget 擴充
- 真正的部署（codesign、MSIX、AppImage）— 第一版只跑本機
- 自動化 sample CI 驗證 — 純手動跑
- `PublishTrimmed` / AOT publish — 需要先在 `Bee.Definition` 補 `Microsoft.XmlSerializer.Generator` 或 `DynamicallyAccessedMembers`
- `DynamicGrid` 內 cell-level 編輯 — 編輯走 `DynamicForm`，grid 純呈現
