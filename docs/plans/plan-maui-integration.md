# MAUI 端 FormSchema 驅動 UI 整合

**狀態：🚧 進行中（2026-05-23）**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 1a | `Bee.UI.Maui.Controls.DynamicForm`（code-based ContentView，BindableProperty 雙向 binding） | ✅ 已完成 |
| 1b | `Bee.UI.Maui.DataObjects.FormDataObject` 接 BO：`LoadAsync` / `SaveAsync` / `DeleteAsync` / `NewAsync` | ✅ 已完成（2026-05-23） |
| 1c | `DynamicGrid`（master 列表）+ `FormPage`（整合頁，code-based） | ✅ 已完成（2026-05-23） |
| 1d | `FormPage` 接 `Bee.UI.Core.ClientInfo`（取得 connector / accessToken），驗證 Local↔Remote 切換 | ✅ 已完成（2026-05-23） |
| 2 | `samples/Maui.Demo`（對應 [plan-samples-structure.md](plan-samples-structure.md) P2） | 📝 待做 |

## 背景

`src/Bee.UI.Maui` 目前只有 Phase 1a 的 `DynamicForm`（[Controls/DynamicForm.cs:19](../../src/Bee.UI.Maui/Controls/DynamicForm.cs)）與 Phase 1a 的 `FormDataObject`（[DataObjects/FormDataObject.cs](../../src/Bee.UI.Maui/DataObjects/FormDataObject.cs)，四個 async 方法仍 throw `NotImplementedException`）。

[plan-blazor-web-integration.md](plan-blazor-web-integration.md) 的 §架構圖把 `bee.ui.core` 列為「獨立 repo」，但實際 `src/Bee.UI.Maui` 與 `src/Bee.UI.Core` 已存在於本 repo。本 plan 採「留在 bee-library 內」的方向把缺的實作補齊，**未來** MAUI 生態擴張到值得獨立 repo 時再另開 plan 處理拆分。

## 與 Blazor Web plan 的關係

兩條 plan 各自獨立推進：

- Web（[plan-blazor-web-integration.md](plan-blazor-web-integration.md)）：`Bee.Web.Blazor.Server` / `Bee.Web.Blazor.Wasm`，Razor 元件 + `AddBeeBlazor` DI extension。
- MAUI（本 plan）：`Bee.UI.Maui`，code-based MAUI ContentView，**不再做 `AddBeeMaui` DI extension** — `Bee.UI.Core.ClientInfo` 已提供 `CreateFormApiConnector(progId)` / `SystemApiConnector` 兩個 factory（[ClientInfo.cs:85,105](../../src/Bee.UI.Core/ClientInfo.cs)）以及 Local↔Remote 切換邏輯，MAUI 端的 `FormPage` 直接呼叫 `ClientInfo` 取連線。

對應參照表（同樣 schema/contract，UI 與 connector 來源不同）：

| 關注點 | Blazor Web | MAUI |
|--------|-----------|------|
| 渲染 | Razor `[Parameter]` + `RenderFragment` | MAUI `BindableProperty` + 程式碼建 `VerticalStackLayout` 等 |
| Connector 取得 | `BeeApiConnectorFactory`（DI scoped） | `ClientInfo.CreateFormApiConnector(progId)`（static） |
| AccessToken | `[CascadingParameter] Guid AccessToken` | `ClientInfo.AccessToken`（Login 後設） |
| Local↔Remote | 部署架構鎖死（Server=Local / Wasm=Remote） | 執行期切換（`ApiClientInfo.ConnectType` + `IEndpointStorage`） |
| 共用層 | `Bee.Api.Client`、`Bee.Definition` | 同左 + `Bee.UI.Core` |

## 階段細部

### Phase 1b：FormDataObject 接 BO

把 Web 端 [src/Bee.Web.Blazor.Server/DataObjects/FormDataObject.cs](../../src/Bee.Web.Blazor.Server/DataObjects/FormDataObject.cs) 的 Phase 1b 行為複製到 MAUI 端：

- `LoadAsync(Guid rowId)` 改簽章接 Guid，呼叫 `connector.GetDataAsync(rowId)`，server 回的 DataSet 取代本地。
- `SaveAsync()` 呼叫 `connector.SaveAsync(DataSet)`，refreshed DataSet 取代本地，重置 `IsDirty`。
- `DeleteAsync()` 取 master row 的 `sys_rowid`，呼叫 `connector.DeleteAsync(rowId)`，本地 reset 至空白 DataSet。
- `NewAsync()` 呼叫 `connector.GetNewDataAsync()`，server skeleton 取代本地。
- `DataSet` / `IsLoading` 改可變狀態。
- 補單元測試（用 fake `FormApiConnector` subclass override 四方法，模式與 Web 端 [tests/Bee.Web.Blazor.Server.UnitTests/DataObjects/FormDataObjectTests.cs](../../tests/Bee.Web.Blazor.Server.UnitTests/DataObjects/FormDataObjectTests.cs) 對齊）。

### Phase 1c：DynamicGrid + FormPage（code-based）

- `Bee.UI.Maui.Controls.DynamicGrid`：用 `Grid` 或 `CollectionView` 渲染 `LayoutGrid` + `DataTable`；BindableProperty：`Layout`、`Rows`；事件：`RowSelected(Guid rowId)`（取自 `sys_rowid`）。
- `Bee.UI.Maui.Controls.FormPage`：垂直 layout：toolbar（New / Save / Delete）+ DynamicGrid + DynamicForm；`OnHandlerChanged` 或 `OnBindingContextChanged` 取 schema 與 connector，建 `FormDataObject`，掛 grid `RowSelected` → `LoadAsync`。
- 與 Web 對應元件 [src/Bee.Web.Blazor.Server/Components/DynamicGrid.razor.cs](../../src/Bee.Web.Blazor.Server/Components/DynamicGrid.razor.cs)、[FormPage.razor.cs](../../src/Bee.Web.Blazor.Server/Components/FormPage.razor.cs) 行為對齊。
- MAUI XAML 不必引入；維持 code-based 風格與既有 `DynamicForm` 一致。

### Phase 1d：對接 ClientInfo

- `FormPage.InitializeAsync` 在 `Schema` / `FormConnector` 為 null 且 `ProgId` 已設定時，向 `Bee.UI.Core.ClientInfo` 取得 schema 與 connector：
  - Schema：`ClientInfo.SystemApiConnector.GetDefineAsync<FormSchema>(DefineType.FormSchema, [ProgId])`
  - Connector：`ClientInfo.CreateFormApiConnector(ProgId)`
  - AccessToken：fallback 到 `ClientInfo.AccessToken`（host 若已設定則保留 host 值）
- Resolution 透過 `protected virtual` hook（`ResolveSystemConnector` / `ResolveFormConnector` / `ResolveAccessToken`）對接，方便測試與 host 客製覆寫，**不引入 DI / Factory 介面**。
- 內部加上 `_isInitializing` 重入鎖：fallback 寫回 `Schema` / `FormConnector` 會再觸發 `OnInputsChanged`，需要避免遞迴。
- 共用測試 helper：[`tests/Bee.Tests.Shared/ClientInfoTestScope.cs`](../../tests/Bee.Tests.Shared/ClientInfoTestScope.cs) — try/finally 還原 `ApiClientInfo.{ConnectType, Endpoint, SupportedConnectTypes, ApiKey, ApiEncryptionKey, LocalServiceProvider}` + `ClientInfo.{AccessToken, SystemApiConnector cache, IDefineAccess cache, UserInfo, AllowGenerateSettings, EndpointStorage, UIViewService}`。所有觸及 `ClientInfo` 的 test class 統一 `[Collection("ClientInfo")]` 避免平行 race。
- Local / Remote 驗證：`Default_Resolve_DelegatesToClientInfo` 用 `[Theory]` 跑兩種 `ConnectType`，斷言預設 hook 回傳的 connector 內部 `Provider` 是 `LocalApiProvider` / `RemoteApiProvider`。

### Phase 2：samples/Maui.Demo

對齊 [plan-samples-structure.md](plan-samples-structure.md) §P2：

- `samples/Maui.Demo` 引用 `Bee.UI.Maui` + `Bee.Api.Client`。
- 預設連 `samples/QuickStart.Server`（與 Console demo 共享後端）。
- 至少示範一頁 `<bee:FormPage ProgId="Employee" />` 渲染共用 `Define/FormSchema/Employee.FormSchema.xml`。
- 桌面平台優先（macOS / Windows）；行動平台 opt-in。

## 不在本 plan 範圍

- 把 `src/Bee.UI.Maui` 搬到獨立 repo `bee.ui.core`（未來 MAUI 生態擴張到值得拆分時再開 plan）。
- 通用 MAUI Login UI（XAML 登入頁設計）— 留給 demo / app 各自實作，本 plan 只確保 `ClientInfo` 接面可用。
- XAML 版本的 `DynamicForm` / `DynamicGrid`（目前 code-based 已足；XAML 版本可待後續視需求決定）。
- 多 detail 表頁 / 自訂 widget 擴充 — 待 Phase 1 完整跑通後再評估。
