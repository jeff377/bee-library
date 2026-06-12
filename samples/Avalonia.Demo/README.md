# Avalonia.Demo

**English** | [繁體中文](README.zh-TW.md)

A Bee.NET sample: an Avalonia desktop client that talks to the JSON-RPC API hosted by `samples/QuickStart.Server`, rendering the shared `Define/FormSchema/` definitions (Employee / Department / Project). Structurally symmetric to `samples/Maui.Demo` but targets desktop Avalonia (Windows / macOS / Linux) instead of MAUI's mobile-first matrix, and additionally demonstrates the **lookup picker flow** (relation fields opening a search-and-pick dialog).

## What it proves

- The same `FormSchema` renders across **four** UI families now — Blazor Server, Blazor Wasm, MAUI, and Avalonia — all sharing `samples/Define/`
- When the host passes only `ProgId`, `Bee.UI.Avalonia.Controls.FormView` automatically resolves connector / schema / access token through `Bee.UI.Core.ClientInfo` (mirroring the MAUI `FormPage` fallback)
- An Avalonia desktop app over `ConnectType.Remote` walks the same JSON-RPC wire path as Blazor.Wasm / Maui.Demo
- The Avalonia DataGrid `DataRowView` indexer binding is a viable presentation path for `DataTable`-backed list views (no `ITypedList` schema introspection required)

## Prerequisites

1. **.NET 10 SDK** — installed system-wide. No Avalonia workload required (Avalonia is a regular NuGet library; no MAUI / Xcode dependency).

2. **Backend**: in another terminal run

   ```sh
   cd samples/QuickStart.Server
   dotnet run
   ```

   The server listens on `http://localhost:5050` by default. `QuickStart.Server` is already wired through `Bee.Samples.Shared.DemoBackend` with `DemoAuthenticatingSystemBusinessObject`; the first run auto-creates the SQLite file and seeds 3 Employee rows.

## Launch the demo

```sh
cd samples/Avalonia.Demo
dotnet run --configuration Debug
```

**Run with `-c Debug`** for first-cut development. Release builds work the same on desktop (Avalonia doesn't share the MAUI Apple-platform trim issue — it runs on the regular .NET runtime, not Mono linker), but a future `dotnet publish -p:PublishTrimmed=true` would re-introduce the `System.Xml.Serialization` reflection-fallback hazard. That fix is out of scope for this sample.

## What you'll see

1. **Connection view**: endpoint defaults to `http://localhost:5050/api`. Clicking **Connect** calls `ClientInfo.Initialize(endpoint)`, which runs an HTTP reachability check plus `system.ping` via `ApiConnectValidator`. On success the main window swaps to Login.
2. **Login view**: two text fields pre-filled with `demo` / `demo` (matches the QuickStart seed). Clicking **Sign in** calls `SystemApiConnector.LoginAsync`; the token is stored through `ClientInfo.ApplyLoginResult` and the window advances to the Forms view.
3. **Forms view**: a tab strip hosting one `<bee:FormView ProgId="..." />` per demo form. Each view passes neither `Schema` nor `FormConnector`; the fallback pulls them from `ClientInfo` (`GetDefineAsync<FormSchema>` / `CreateFormApiConnector` / `AccessToken`):
   - **Employee** — the original master-detail form; the seeded rows (Alice / Bob / Carol) list at the top, selecting a row drives the `DynamicForm`, and **New** / **Save** / **Delete** round-trip through the BO.
   - **Department** — a plain master form that doubles as the **lookup source**; its schema declares `LookupFields="sys_id,sys_name"` (the same set the server would default to).
   - **Project** — the **lookup demo**: the master *Owner Department* field renders as a `ButtonEdit` whose icon opens the Department picker (server-side search via `GetLookup`), and the *Member* column of the Project Members detail grid opens the Employee picker on cell click (in-cell lookup). Selections write the row id plus the mapped `ref_*` display fields back through `FormDataObject`; Delete/Backspace on the master lookup field clears it.

### Lookup walkthrough

1. On the **Department** tab create a department or two (e.g. `D001 / Engineering`).
2. Switch to **Project**, click **New**, then click the magnifier on *Owner Department* — search, double-click a row (or select + OK), and watch the department name appear.
3. In **Project Members** add a row and click the *Member* cell — the Employee picker opens; pick one of the seeded employees.
4. **Save**, reselect the project from the list: the `ref_*` columns now come from the server-side relation JOIN (the single source of truth).

## What this maps to in the library

| Demo behaviour | Library component |
|----------------|-------------------|
| Connect → endpoint validation | [src/Bee.UI.Core/ClientInfo.cs](../../src/Bee.UI.Core/ClientInfo.cs) + [src/Bee.Api.Client/ApiConnectValidator.cs](../../src/Bee.Api.Client/ApiConnectValidator.cs) |
| Endpoint persistence | [src/Bee.UI.Avalonia/Storage/FileEndpointStorage.cs](../../src/Bee.UI.Avalonia/Storage/FileEndpointStorage.cs) |
| Login → token | [src/Bee.Api.Client/Connectors/SystemApiConnector.cs](../../src/Bee.Api.Client/Connectors/SystemApiConnector.cs) (LoginAsync) |
| Employee form rendering | [src/Bee.UI.Avalonia/Controls/FormView.cs](../../src/Bee.UI.Avalonia/Controls/FormView.cs) → DynamicForm / DynamicGrid |
| FormSchema fallback | `FormView` ResolveSystemConnector / ResolveFormConnector / ResolveAccessToken |
| CRUD wire path | FormApiConnector → RemoteApiProvider → QuickStart.Server `ApiController` → DemoBusinessObjectFactory → FormBusinessObject |

## Relationship to the other demos

`QuickStart.Server` is the same `Bee.Samples.Shared.DemoBackend` host that backs `Maui.Demo`. Both clients hit the same SQLite seed, so you can run the Avalonia demo and the MAUI demo side-by-side and watch a Save on one immediately surface in the other after the next list reload.

| Sample | UI family | Backend | Wire |
|--------|-----------|---------|------|
| Blazor.Server.Demo | Razor + Blazor Server | in-process Local | none |
| Blazor.Wasm.Demo | Razor + Blazor Wasm | Blazor.Wasm.Demo.Host | JSON-RPC over HTTP |
| Maui.Demo | MAUI Shell + ContentPage | QuickStart.Server | JSON-RPC over HTTP |
| **Avalonia.Demo** | **Avalonia Window + UserControl + MVVM** | **QuickStart.Server** | **JSON-RPC over HTTP** |

## MVVM conventions

The sample uses `CommunityToolkit.Mvvm` source generators (matches the convention used by `tools/DefineEditor`). Key idioms:

- `ViewModelBase : ObservableObject` — marker for `ViewLocator` routing
- `[ObservableProperty]` on private fields — auto-generates `INotifyPropertyChanged` properties
- `[RelayCommand]` on async methods — auto-generates `IAsyncRelayCommand`
- `[NotifyPropertyChangedFor(nameof(IsNotBusy))]` — re-raises change for derived properties
- Navigation is plain `Action` callbacks passed to child VM constructors (rather than CLR events) — the navigation graph is explicit at `MainWindowViewModel` construction time and there's nothing to unsubscribe.

## Deliberately out of scope

- Multiple detail tables / custom widget extensions
- Real deployment (codesign, MSIX, AppImage) — local-only for the first cut
- Automated sample CI validation — purely manual runs
- `PublishTrimmed` / AOT publish — would need `Microsoft.XmlSerializer.Generator` or `DynamicallyAccessedMembers` work on `Bee.Definition` types
- Cell-level edits inside `DynamicGrid` — edits happen through `DynamicForm`, the grid is read-only
