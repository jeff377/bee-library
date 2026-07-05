# Bee.UI.Maui

[繁體中文](README.zh-TW.md)

Cross-platform .NET MAUI control library (iOS / Android / Mac Catalyst / Windows). Renders FormSchema-driven forms with code-built `ContentView` controls that mirror the Blazor component family, all backed by the `FormDataObject` view-model.

## Architecture Position

**Layer**: UI (mobile / cross-platform)

Belongs to the `Bee.UI.*` family: connects to the backend through the `ClientInfo` static singleton (`Bee.UI.Core`) with a per-process token model. Depends on `Bee.UI.Core`, which references `Bee.Api.Client` for connectors; `Bee.Definition` (schemas and layouts) then flows in transitively through `Bee.Api.Client → Bee.Api.Core → Bee.Definition`.

## Target Framework

Default single `net10.0` TFM — the library compiles against the `Microsoft.Maui.Controls` ref assemblies (PackageReference `10.0.20`), so a plain .NET SDK builds it with no MAUI workload, Android SDK or Xcode (CI-friendly).

Platform TFMs are opt-in via `-p:BeeUiMauiFullPlatforms=true` when the local environment has the matching workload + SDK:

| Host OS | Added TFMs |
|---|---|
| any | `net10.0-android` |
| macOS | `net10.0-ios`, `net10.0-maccatalyst` |
| Windows | `net10.0-windows10.0.19041.0` |

`UseMaui` / `SingleProject` are only set under the opt-in — enabling `UseMaui` on the plain `net10.0` build would pull in maui-tizen workload validation (NETSDK1147) and break workload-less CI runners.

## Key Components

| Component | Description |
|---|---|
| `FormPage` | Top-level container: list (`DynamicGrid`) + master form (`DynamicForm`) + New / Save / Delete toolbar over a shared `FormDataObject`. When the host sets only `ProgId`, resolves `Schema` / `FormConnector` / `AccessToken` from `ClientInfo`; the `ResolveSystemConnector` / `ResolveFormConnector` / `ResolveAccessToken` hooks are `virtual` for hosts that bypass the static singleton. |
| `DynamicForm` | Renders the master sections of a `FormLayout`, dispatching each `LayoutField` to the MAUI control matching its `ControlType` (`CheckBox` / `DatePicker` / `Editor` / `Picker` / `Entry`). Detail grids (`FormLayout.Details`) are not rendered yet. |
| `DynamicGrid` | Presentation-only list grid over a `DataTable` driven by a `LayoutGrid`; raises `RowSelected` with the row's `sys_rowid` Guid. The host owns the `FormApiConnector.GetListAsync` call and pushes the result in via `Rows`. |
| `FormDataObject` | The view-model: holds the `DataSet` (master row + detail tables) derived from `FormSchema`, exposes the string-based `GetField` / `SetField` binding surface with type coercion and dirty tracking, plus the async CRUD round-trips (`LoadAsync` / `NewAsync` / `SaveAsync` / `DeleteAsync`). |
| `MauiPreferenceEndpointStorage` | `IEndpointStorage` backed by MAUI `Preferences.Default` (NSUserDefaults / SharedPreferences / registry). Required on sandboxed hosts where the default file-based storage cannot write into the read-only `.app` bundle. |

## Usage

```csharp
// MauiProgram.CreateMauiApp — wire sandbox-friendly storage BEFORE ClientInfo.Initialize.
ClientInfo.EndpointStorage = new MauiPreferenceEndpointStorage();
```

```xml
<!-- Full form: one ProgId drives schema, layout, connector and data. -->
<bee:FormPage ProgId="Employee" />
```

## Design Notes

- The control set mirrors the Blazor components (`FormPage` / `DynamicForm` / `DynamicGrid`) for cross-family parity. The layout properties are named `FormLayout` / `ListLayout` instead of the Blazor-side `Layout` because `VisualElement` already exposes a public `Layout(Rect)` method.
- MAUI `BindableProperty` only fires `propertyChanged` on reference changes, while `FormDataObject` mutates its `DataSet` in place across New / Load / Save / Delete — `FormPage` therefore drives `DynamicForm.Refresh()` explicitly after each round-trip.
- Sandbox storage rules, csproj pitfalls and the Apple Release-mode trimming decision tree (Mono linker strips the `XmlSerializer` reflection fallback) are recorded in [.claude/rules/maui.md](../../.claude/rules/maui.md); the practical consequence — run samples with `-c Debug` on Apple platforms — is explained in the [Maui.Demo README](../../samples/Maui.Demo/README.md).

## Samples

- [`samples/Maui.Demo`](../../samples/Maui.Demo/README.md) — full Connect → Login → CRUD flow over `FormPage`, against the JSON-RPC backend hosted by `samples/QuickStart.Server`.
