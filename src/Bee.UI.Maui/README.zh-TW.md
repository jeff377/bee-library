# Bee.UI.Maui

[English](README.md)

跨平台 .NET MAUI 控制項套件（iOS / Android / Mac Catalyst / Windows）。以一組鏡像 Blazor 元件家族的程式建構 `ContentView` 控件渲染 FormSchema 驅動表單，資料中樞為 `FormDataObject` view-model。

## 架構定位

**層級**：UI（行動 / 跨平台）

屬於 `Bee.UI.*` 家族：透過 `ClientInfo` 靜態單例（`Bee.UI.Core`）連接後端，採 per-process token 模型。依賴 `Bee.UI.Core`，並由其參照 `Bee.Api.Client`（連接器）；`Bee.Definition`（schema 與 layout）則經 `Bee.Api.Client → Bee.Api.Core → Bee.Definition` 遞移帶入。

## 目標框架

預設單一 `net10.0` TFM——套件僅以 `Microsoft.Maui.Controls` ref assemblies（PackageReference `10.0.20`）編譯，純 .NET SDK 即可建置，不需 MAUI workload、Android SDK 或 Xcode（CI 友善）。

平台 TFM 採 opt-in：本機已裝對應 workload + SDK 時，以 `-p:BeeUiMauiFullPlatforms=true` 加入：

| Host OS | 加入的 TFM |
|---|---|
| 任意 | `net10.0-android` |
| macOS | `net10.0-ios`、`net10.0-maccatalyst` |
| Windows | `net10.0-windows10.0.19041.0` |

`UseMaui` / `SingleProject` 只在 opt-in 時設定——在純 `net10.0` 建置上開 `UseMaui` 會無條件觸發 maui-tizen workload 驗證（NETSDK1147），讓沒有任何 MAUI workload 的 CI runner 直接失敗。

## 主要元件

| 元件 | 說明 |
|---|---|
| `FormPage` | 頂層容器：列表（`DynamicGrid`）+ 主檔表單（`DynamicForm`）+ New / Save / Delete 工具列，共用同一個 `FormDataObject`。host 只設 `ProgId` 時自動向 `ClientInfo` 解析 `Schema` / `FormConnector` / `AccessToken`；`ResolveSystemConnector` / `ResolveFormConnector` / `ResolveAccessToken` 為 `virtual` hook，供繞過靜態單例的 host 覆寫。 |
| `DynamicForm` | 渲染 `FormLayout` 的主檔 sections，依 `ControlType` 把每個 `LayoutField` 派發到對應 MAUI 控件（`CheckBox` / `DatePicker` / `Editor` / `Picker` / `Entry`）。明細（`FormLayout.Details`）尚未渲染。 |
| `DynamicGrid` | 純展示用列表 grid：由 `LayoutGrid` 驅動渲染 `DataTable`，點選列時以該列的 `sys_rowid` Guid 引發 `RowSelected`。`FormApiConnector.GetListAsync` 由 host 負責呼叫，結果經 `Rows` 傳入。 |
| `FormDataObject` | view-model：承載由 `FormSchema` 推導的 `DataSet`（主檔列 + 明細表），提供字串型 `GetField` / `SetField` 綁定介面（含型別轉換與 dirty 追蹤），以及非同步 CRUD（`LoadAsync` / `NewAsync` / `SaveAsync` / `DeleteAsync`）。 |
| `MauiPreferenceEndpointStorage` | 以 MAUI `Preferences.Default` 為後端的 `IEndpointStorage`（NSUserDefaults / SharedPreferences / registry）。sandboxed host 必備——預設檔案型 storage 寫不進唯讀的 `.app` bundle。 |

## 使用方式

```csharp
// MauiProgram.CreateMauiApp — wire sandbox-friendly storage BEFORE ClientInfo.Initialize.
ClientInfo.EndpointStorage = new MauiPreferenceEndpointStorage();
```

```xml
<!-- Full form: one ProgId drives schema, layout, connector and data. -->
<bee:FormPage ProgId="Employee" />
```

## 設計備註

- 控件組鏡像 Blazor 元件（`FormPage` / `DynamicForm` / `DynamicGrid`）以維持跨家族對稱。layout 屬性命名為 `FormLayout` / `ListLayout` 而非 Blazor 端的 `Layout`，因為 `VisualElement` 已有公開的 `Layout(Rect)` 方法。
- MAUI `BindableProperty` 只在參考變更時引發 `propertyChanged`，而 `FormDataObject` 在 New / Load / Save / Delete 之間是原地改寫 `DataSet`——因此 `FormPage` 在每次 round-trip 後明確驅動 `DynamicForm.Refresh()`。
- Sandbox storage 規則、csproj 雷區與 Apple Release-mode trimming 決策樹（Mono linker 砍掉 `XmlSerializer` 反射 fallback）記錄於 [.claude/rules/maui.md](../../.claude/rules/maui.md)；實務結論——Apple 平台 sample 一律跑 `-c Debug`——見 [Maui.Demo README](../../samples/Maui.Demo/README.zh-TW.md)。

## 範例

- [`samples/Maui.Demo`](../../samples/Maui.Demo/README.zh-TW.md) —— `FormPage` 的完整 Connect → Login → CRUD 流程，對接 `samples/QuickStart.Server` 提供的 JSON-RPC 後端。
