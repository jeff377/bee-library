# Bee.UI.Avalonia

[繁體中文](README.zh-TW.md)

Avalonia desktop control library (Windows / macOS / Linux). Renders FormSchema-driven forms with a set of native-control subclasses deeply bound to the definition layer, all backed by the `FormDataObject` view-model.

## Architecture Position

**Layer**: UI (desktop)

Belongs to the `Bee.UI.*` family: connects to the backend through the `ClientInfo` static singleton (`Bee.UI.Core`) with a per-process token model. Depends on `Bee.Api.Client` for connectors and `Bee.Definition` for schemas and layouts.

## Target Framework

Single `net10.0` TFM. Lower-bound pins: `Avalonia 12.0.0` + `Avalonia.Controls.DataGrid 12.0.0`; hosts may bring a newer `Avalonia 12.0.x` transitively. The library ships no theme — the host picks one (Semi.Avalonia, Fluent, …); every control keeps the host theme through `StyleKeyOverride`.

## Key Components

| Component | Description |
|---|---|
| `FormView` | Top-level container: list (`GridControl`) + master form (`DynamicForm`) + New / Save / Delete toolbar. Resolves `Schema` / `FormConnector` / `AccessToken` from `ClientInfo` when the host sets only `ProgId`. |
| `DynamicForm` | Renders the master sections of a `FormLayout` (one field editor per `LayoutField`) followed by the detail grids (`FormLayout.Details`); `DetailEditMode` picks the detail editing model. |
| `GridControl` | `DataGrid` subclass driven by a `LayoutGrid`; implements `IBindTableControl` / `IUIControl`. Cell rendering goes through `DataGridTemplateColumn` + `FuncDataTemplate<DataRowView>` (see ADR-020). |
| Field editors (`TextEdit` / `MemoEdit` / `ButtonEdit` / `DateEdit` / `YearMonthEdit` / `DropDownEdit` / `CheckEdit`) | One per `ControlType`; native-control subclasses implementing `IBindFieldControl` / `IUIControl`, auto-applying `FormField` metadata (MaxLength, ListItems). |
| `FormScope` | Attached inherited properties (`DataObject` / `FormMode`): set once on a container and every descendant editor with a `FieldName` binds itself. |
| `GridEditMode` | UI-layer editing model for grids: `InCell` (cell editing, ADR-021 hybrid strategy) or `EditForm` (read-only grid + popup row editing). |
| `RowEditPanel` / `RowEditDialog` | EditForm-mode editing surface built from the field editors; commits or cancels through the buffered row-edit protocol. |
| `FormDataObject` | The view-model: carries the `DataSet`, bridges ADO.NET table events into `FieldValueChanged` / dirty tracking, exposes the async CRUD round-trips and the buffered row-edit protocol (`BeginRowEdit` / `CommitRowEdit` / `CancelRowEdit`). |
| `FileEndpointStorage` | File-backed `IEndpointStorage`; persists the endpoint at `LocalApplicationData/<appName>/endpoint.txt`. |

## Usage

```csharp
// Host bootstrap — wire EndpointStorage BEFORE any UI control instantiates.
ApiClientInfo.ApiKey = "my-app";
ApiClientInfo.SupportedConnectTypes = SupportedConnectTypes.Remote;
ClientInfo.EndpointStorage = new FileEndpointStorage("MyApp");
```

```xml
<!-- Full form: one ProgId drives schema, layout, connector and data. -->
<bee:FormView ProgId="Employee" />

<!-- Hand-written form: set the ambient scope once, editors bind by FieldName. -->
<StackPanel ed:FormScope.DataObject="{Binding Data}">
    <ed:TextEdit FieldName="emp_name" />
    <ed:DateEdit FieldName="hire_date" />
</StackPanel>
```

## Design Notes

- Every control subclass overrides `StyleKeyOverride` to its native base type so the host theme keeps applying (a missing override renders the control invisible).
- `FieldValueChanged` is bridged from the ADO.NET `DataTable` events — any write path (editors, grid cells, direct `DataRow` writes) publishes; no writer raises events manually.
- DataGrid binding and editing strategies are recorded in [ADR-020](../../docs/adr/adr-020-avalonia-datagrid-binding-strategy.md) and [ADR-021](../../docs/adr/adr-021-avalonia-datagrid-editing-strategy.md).

## Samples

- [`samples/Avalonia.Demo`](../../samples/Avalonia.Demo/README.md) — full Connection → Login → CRUD flow over `FormView`.
- [`samples/Avalonia.DemoCenter`](../../samples/Avalonia.DemoCenter/README.md) — control demo center (nav tree + scenario host + theme/FormMode toolbar); includes style parity for the field editors / `GridControl` and both grid editing modes.
