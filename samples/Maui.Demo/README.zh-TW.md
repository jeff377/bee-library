# Maui.Demo

[English](README.md) | **繁體中文**

Bee.NET 範例專案：MAUI 用戶端，連 `samples/QuickStart.Server` 的 JSON-RPC API，渲染共用 `Define/FormSchema/Employee.FormSchema.xml`。對應 [plan-samples-structure.md](../../docs/plans/plan-samples-structure.md) P2 與 [plan-maui-integration.md](../../docs/plans/plan-maui-integration.md) Phase 2。

## 它要證明什麼

- 同一份 `FormSchema` 在 Blazor Server / Blazor Wasm / MAUI 三種前端都能渲染（與 `Blazor.Server.Demo` / `Blazor.Wasm.Demo` 共用 `samples/Define/`）
- `Bee.UI.Maui.Controls.FormPage` 在「host 只給 `ProgId`」的情況下，會自動向 `Bee.UI.Core.ClientInfo` 取 connector / schema / access token（即 Phase 1d 行為）
- MAUI app 走 `ConnectType.Remote` 經 HTTP 打 JSON-RPC，與 Blazor.Wasm 走同一條 wire path

## 前置條件（macOS）

1. **maui workloads**：

   ```
   sudo dotnet workload install maui
   ```

   裝完 `dotnet workload list` 應該會多出 `maui-ios`、`maui-maccatalyst`、`maui-tizen`（及保留的 `maui-android`）。

2. **Xcode**：Mac Catalyst 編譯需要 Xcode（不只 command-line tools）。`xcode-select -p` 應指向 `/Applications/Xcode.app/Contents/Developer`。

3. **後端**：另一個 terminal 跑

   ```
   cd samples/QuickStart.Server
   dotnet run
   ```

   server 預設聽 `http://localhost:5050`。`QuickStart.Server` 已經透過 `Bee.Samples.Shared.DemoBackend` 接 `DemoAuthenticatingSystemBusinessObject`，第一次啟動會自動建 SQLite + 種 3 筆 Employee 資料。

## 啟動 demo

```
cd samples/Maui.Demo
dotnet build -t:Run -c Debug -f net10.0-maccatalyst
```

**請用 `-c Debug` 跑**。Apple 平台的 Release-mode Mono linker 會把 `System.Xml.Serialization` 的反射 fallback (Sgen) 砍掉，導致 `Bee.Api.Client` 反序列化 `FormSchema` 時拋出「`XmlSerializeErrorDetails, 2, 2`」。Debug 不做 trimming，可以直接跑通。日後若要 ship 為真正 release（App Store / TestFlight），需要：

- 加 `Microsoft.XmlSerializer.Generator` 預先產出 Sgen 組件
- 或在 `Bee.Definition` 加 `DynamicallyAccessedMembers` 註記
- 或加 `TrimmerRootDescriptor` 把 `System.Xml.Serialization` 入口保留住

預設只跑 Mac Catalyst（`TargetFrameworks` 預設只含 `net10.0-maccatalyst`）。要編 iOS / Android 等其他平台，加 `-p:MauiDemoFullPlatforms=true`，並先裝對應 workload。

## 預期畫面

1. **Connect 頁**：endpoint 預設 `http://localhost:5050/api`。按 **Connect** 會呼叫 `ClientInfo.Initialize(endpoint)`，由 `ApiConnectValidator` 做 HTTP reachability + `system.ping`。成功跳到 Login。
2. **Login 頁**：兩格 entry，預設帶入 `demo` / `demo`（對應 `Bee.Samples.Shared.DemoCredentials`）。按 **Sign in** 呼叫 `SystemApiConnector.LoginAsync`，token 透過 `ClientInfo.ApplyLoginResult` 存起來。
3. **Employee 頁**：`<FormPage ProgId="Employee" />`。MAUI 端不傳 `Schema` / `FormConnector`，由 Phase 1d 的 fallback 從 `ClientInfo` 取得：
   - `ClientInfo.SystemApiConnector.GetDefineAsync<FormSchema>(DefineType.FormSchema, ["Employee"])`
   - `ClientInfo.CreateFormApiConnector("Employee")`
   - `ClientInfo.AccessToken`

   渲染後：上方 `DynamicGrid` 列出三筆種子資料（Alice / Bob / Carol），點任一列 → 下方 `DynamicForm` 顯示明細；New / Save / Delete 走 BO 回 server。

## 對應 library 元件

| Demo 行為 | library 元件 |
|-----------|--------------|
| Connect → endpoint 驗證 | [src/Bee.UI.Core/ClientInfo.cs](../../src/Bee.UI.Core/ClientInfo.cs) + [src/Bee.Api.Client/ApiConnectValidator.cs](../../src/Bee.Api.Client/ApiConnectValidator.cs) |
| Login → token | [src/Bee.Api.Client/Connectors/SystemApiConnector.cs](../../src/Bee.Api.Client/Connectors/SystemApiConnector.cs)（LoginAsync） |
| Employee 表單渲染 | [src/Bee.UI.Maui/Controls/FormPage.cs](../../src/Bee.UI.Maui/Controls/FormPage.cs) → DynamicForm / DynamicGrid |
| FormSchema fallback | Phase 1d ResolveSystemConnector / ResolveFormConnector / ResolveAccessToken |
| CRUD wire path | FormApiConnector → RemoteApiProvider → QuickStart.Server `ApiController` → DemoBusinessObjectFactory → FormBusinessObject |

## 與 Blazor demo 的關係

`QuickStart.Server` 已經是 `Bee.Samples.Shared.DemoBackend` 的 host，與 `Blazor.Wasm.Demo.Host` 是兩個獨立啟動的 host 但共用同一 `DemoBackend` 程式碼。`Blazor.Server.Demo` 走 in-process Local provider，不需要 `QuickStart.Server`。

Maui.Demo 與 Blazor.Wasm.Demo 結構對稱（都走 `ConnectType.Remote` 打 server），程式碼差別只在 UI family（MAUI ContentPage vs Razor component）。

## 不做的事

- 多 detail 表 / 自訂 widget 擴充
- 真正的部署（codesign、TestFlight、Microsoft Store）— 第一版只跑本機
- 自動化 sample CI 驗證 — 純手動跑
- Apple Release-mode trimming 完整解（要再投入 `Microsoft.XmlSerializer.Generator` 或 `DynamicallyAccessedMembers` 註記）— 留待 demo 真正要 ship 時再處理
