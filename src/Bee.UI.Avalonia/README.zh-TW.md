# Bee.UI.Avalonia

[English](README.md)

Avalonia 桌面控制項套件（Windows / macOS / Linux）。以一組深度綁定定義層的原生控件子類渲染 FormSchema 驅動表單，資料中樞為 `FormDataObject` view-model。

## 架構定位

**層級**：UI（桌面）

屬於 `Bee.UI.*` 家族：透過 `ClientInfo` 靜態單例（`Bee.UI.Core`）連接後端，採 per-process token 模型。依賴 `Bee.Api.Client`（連接器）與 `Bee.Definition`（schema 與 layout）。

## 目標框架

單一 `net10.0` TFM。下限版本：`Avalonia 12.0.0` + `Avalonia.Controls.DataGrid 12.0.0`；host 可透過 transitive 帶更新的 `Avalonia 12.0.x`。本套件不內建主題——由 host 自選（Semi.Avalonia、Fluent…），所有控件透過 `StyleKeyOverride` 沿用 host 主題。

## 主要元件

| 元件 | 說明 |
|---|---|
| `FormView` | 頂層容器：列表（`GridControl`）+ 主檔表單（`DynamicForm`）+ New / Save / Delete 工具列。host 只設 `ProgId` 時自動向 `ClientInfo` 解析 `Schema` / `FormConnector` / `AccessToken`。 |
| `DynamicForm` | 渲染 `FormLayout` 的主檔 sections（每個 `LayoutField` 一個 field editor），其後渲染明細（`FormLayout.Details`）；`DetailEditMode` 決定明細編輯模型。 |
| `GridControl` | 繼承 `DataGrid`、由 `LayoutGrid` 驅動；實作 `IBindTableControl` / `IUIControl`。cell 顯示走 `DataGridTemplateColumn` + `FuncDataTemplate<DataRowView>`（見 ADR-020）。 |
| Field editors（`TextEdit` / `MemoEdit` / `ButtonEdit` / `DateEdit` / `YearMonthEdit` / `DropDownEdit` / `CheckEdit`） | 與 `ControlType` 一一對應；繼承原生控件並實作 `IBindFieldControl` / `IUIControl`，自動套用 `FormField` metadata（MaxLength、ListItems）。 |
| `FormScope` | 可繼承的 attached properties（`DataObject` / `FormMode`）：容器設一次，子孫編輯器憑 `FieldName` 自動綁定。 |
| `GridEditMode` | grid 的 UI 層編輯模型：`InCell`（逐格編輯，ADR-021 混合策略）或 `EditForm`（唯讀 grid + 彈窗整列編輯）。 |
| `RowEditPanel` / `RowEditDialog` | EditForm 模式的編輯面，由 field editors 組成；經暫存列編輯協定確認或取消。 |
| `FormDataObject` | view-model：承載 `DataSet`、把 ADO.NET 表事件橋接為 `FieldValueChanged` 與 dirty 追蹤，提供非同步 CRUD 與暫存列編輯協定（`BeginRowEdit` / `CommitRowEdit` / `CancelRowEdit`）。 |
| `FileEndpointStorage` | 檔案後端 `IEndpointStorage`；endpoint 落在 `LocalApplicationData/<appName>/endpoint.txt`。 |

## 使用方式

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

## 設計備註

- 所有控件子類都覆寫 `StyleKeyOverride` 指向原生基底，host 主題才會持續生效（漏覆寫控件會隱形）。
- `FieldValueChanged` 由 ADO.NET `DataTable` 事件橋接——任何寫入路徑（編輯器、grid cell、直接寫 `DataRow`）都會發布，寫入者不需手動引發。
- DataGrid 的綁定與編輯策略記錄於 [ADR-020](../../docs/adr/adr-020-avalonia-datagrid-binding-strategy.md) 與 [ADR-021](../../docs/adr/adr-021-avalonia-datagrid-editing-strategy.md)。

## 範例

- [`samples/Avalonia.Demo`](../../samples/Avalonia.Demo/README.zh-TW.md) —— `FormView` 的完整 Connection → Login → CRUD 流程。
- [`samples/Avalonia.Editors.Gallery`](../../samples/Avalonia.Editors.Gallery/README.md) —— field editor / `GridControl` 的樣式比對 gallery，含兩種 grid 編輯模式。
