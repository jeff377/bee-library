# ADR-020：Avalonia DataGrid 對 DataTable 列的綁定策略

## 狀態

已採納（2026-06-09）

## 背景

[ADR-001](adr-001-dataset-as-dto.md) 確立 `DataSet` / `DataTable` 為框架的跨層 DTO，server 端 BO 回傳 `DataTable`，client 端直接 render 而不投影到 typed POCO。`Bee.UI.Maui.Controls.DynamicGrid` 以「Grid + Label + TapGestureRecognizer 逐 cell 手刻」的方式跑通了這條資料流。

`Bee.UI.Avalonia` 在 Phase 3 加入時，自然想沿用 Avalonia 內建的 `Avalonia.Controls.DataGrid`：理應比 MAUI 端的手刻 Grid 提供更完整的 selection / scroll / column sizing 等基礎能力。WPF 對應的慣用做法是：

```csharp
new DataGridTextColumn
{
    Header = column.Caption,
    Binding = new Binding($"[{column.FieldName}]") { Mode = BindingMode.OneWay },
}
```

`DataGrid.ItemsSource = DataTable.DefaultView`，每列為 `DataRowView`，binding path `[FieldName]` 透過 `DataRowView` 的 string-key indexer 拿值。WPF 的 binding engine 會 dispatch 到 `ICustomTypeDescriptor`，從 `DataRowView` 取得每欄的 `PropertyDescriptor` 然後讀值，cell 順利顯示。

在 Avalonia 12 上實測這個做法的結果是：

- **DataGrid 正確 iterate 出 row（列數正確）**
- **每一格 cell 都是空字串**

追查發現 Avalonia 12 的 binding engine（透過 `ExpressionObserver` 解析路徑）只認以下兩種資料來源：

1. CLR 屬性（透過 reflection 直接取得 PropertyInfo）
2. typed indexer — 也就是 `IList<T>` / `IReadOnlyList<T>` 的整數鍵 indexer，或宣告為 `this[T key]` 且 `T` 為 binding 期可推導之型別的 indexer

`DataRowView.this[string columnName]` 屬於前述兩者之外的第三類：它是 PropertyDescriptor-based 的 string-key indexer，需要走 `ICustomTypeDescriptor` 取得欄位描述後再讀值。**Avalonia 的 binding engine 不會做這個 dispatch**，所以路徑 `[FieldName]` 解析失敗、cell 收到 `BindingNotification`，最終 render 出空字串。

WPF / MAUI 走相同的綁定字面語法卻能跑通，是因為 WPF binding engine 對 `ICustomTypeDescriptor` 有內建 awareness；這是兩個 framework binding 引擎實作層面的差異，不是「Avalonia 哪裡設錯」可以救的。

## 決策

**`Bee.UI.Avalonia.Controls.DynamicGrid` 不使用 `DataGridTextColumn` + `Binding "[FieldName]"`，改用 `DataGridTemplateColumn` + `FuncDataTemplate<DataRowView>`，在 cell template 內以 code 顯式呼叫 `row.Row[fieldName]` 取值。**

```csharp
private static DataGridTemplateColumn BuildColumn(LayoutColumn column)
{
    var fieldName = column.FieldName;
    var displayFormat = column.DisplayFormat;
    var numberFormat = column.NumberFormat;

    return new DataGridTemplateColumn
    {
        Header = column.Caption,
        CellTemplate = new FuncDataTemplate<DataRowView>(
            (row, _) => new TextBlock
            {
                Text = FormatCell(row, fieldName, displayFormat, numberFormat),
                Margin = new Thickness(8, 4),
            },
            supportsRecycling: true),
    };
}
```

`row.Row` 取得底層 `DataRow`，再走 `DataRow.this[string]`（這是純 ADO.NET，反射可達）— Avalonia binding engine 不會碰到，因為它的角色被 `FuncDataTemplate` 縮成「給定 item，回傳一個 control」。

連帶把欄位格式化（`DisplayFormat` / `NumberFormat` / `DateTime` ISO 8601 / `IFormattable` invariant culture）邏輯統一封裝在 `FormatCell` 靜態方法內，與 `Bee.UI.Maui.Controls.DynamicGrid.FormatCell` 行為對稱。

## 後果

### 正面

- **不需投影到 typed POCO**：DataTable 仍是端到端唯一的列資料表達，與 [ADR-001](adr-001-dataset-as-dto.md) 一致
- **`Bee.UI.Avalonia.DynamicGrid` 與 `Bee.UI.Maui.DynamicGrid` 行為對齊**：兩端都是 code-based formatting，差別只在 host control（Avalonia `DataGrid` vs MAUI `Grid` + `Label`）
- **Avalonia binding engine 的「不會 dispatch 到 ICustomTypeDescriptor」這個事實只需在這一個 adapter 處理**：框架其他地方仍可正常使用 Avalonia binding（綁 CLR 屬性、ViewModel、`IList` 等）
- **避免之後讀者重蹈覆轍**：本 ADR + `DynamicGrid.cs` 的 `<remarks>` 註解明確標示「不要改回 `Binding "[FieldName]"`」

### 負面

- **失去 cell-level binding 的 `OneWayToSource` / `TwoWay` 模式**：`FuncDataTemplate` 內的 `TextBlock` 不會自動寫回 `DataRowView`。對本 `DynamicGrid` 不是問題 — 它本來就 `IsReadOnly = true`，cell-level 編輯由 master 區的 `DynamicForm` 走事件驅動（`TextChanged` / `IsCheckedChanged` / `SelectionChanged`），這個 idiom 在 Avalonia / MAUI / Blazor 三條 family 一致
- **每個 cell template 自己 format**：`DisplayFormat` / `NumberFormat` 處理邏輯集中在 `FormatCell` 靜態方法（行內 5 行 switch），可控；但不再受惠於 Avalonia column-level `IValueConverter` 的 framework 級重用
- **`DataGrid.AutoGenerateColumns` 仍維持 `false`**：原本就因為要對應 `LayoutGrid.Columns` 而手動產欄，本 ADR 不改變這個現狀；但意味著若日後 Avalonia 推出更聰明的 schema 自動推導，我們仍是 opt-out

### 中性

- **Avalonia `Binding "[X]"` 仍可用於其他資料形狀**：`IList<T>`（整數鍵）、`IReadOnlyDictionary<string,T>`（字串鍵但 typed）、自訂宣告 `this[T] { get; }` 的物件都仍走 binding engine，**不要把本 ADR 推廣為「Avalonia 的 indexer binding 都不能用」**；本 ADR 只限縮在「`DataRowView` 的 PropertyDescriptor-based string indexer」這一個情境
- **未來 Avalonia upstream 若補 `ICustomTypeDescriptor` 支援**，可重新評估走回 `DataGridTextColumn` + `Binding "[FieldName]"`，但無此需求前不主動回頭

## 相關連結

- [ADR-001：使用 DataSet 作為跨層 DTO](adr-001-dataset-as-dto.md) — 為何 DataTable 是 client 端直接 render 的單位
- [ADR-013：前端 API 連線策略](adr-013-frontend-api-connection-strategy.md) — `Bee.UI.Avalonia` 為 `Bee.UI.*` family 的一員
- `src/Bee.UI.Avalonia/Controls/DynamicGrid.cs` — 實作 + 詳細 `<remarks>` 註解
- `docs/development-cookbook.md` §「Avalonia desktop (Bee.UI.Avalonia)」 — 從使用者角度說明 binding 策略
- 觀察到問題的 plan：`docs/archive/plan-avalonia-sample.md` §「端到端冒煙過程中追修的問題」#2

## 不在範圍

- **Cell-level 編輯**：目前 `DynamicGrid` 為 read-only；若日後需要 inline 編輯，可在那時再評估「自己寫 two-way binding 機制」或「投影到 ViewModel POCO」哪個成本較低
- **Avalonia CompiledBinding 對 `DataRowView` 的支援**：是 Avalonia upstream 議題，不在 Bee.NET 這層處理
- **將 `FormatCell` 抽到 `Bee.UI.Core` 與 `Bee.UI.Maui.DynamicGrid` 共用**：行為對稱但載體型別不同（Avalonia `DataRowView` 用 `row.Row[name]`、MAUI 直接吃 `DataRow`），抽共用需要先抽 helper signature，與本 ADR 的決策正交；本 ADR 範圍不處理

## 後記（2026-06-11）

本 ADR 的實作位置已由 `DynamicGrid`（`UserControl` 包裝，現已移除）遷移為 `GridControl`（`src/Bee.UI.Avalonia/Controls/GridControl.cs`；最初直接繼承 `DataGrid`，後重構為 `ContentControl` 組合式、內部 `DataGrid` 以 `InnerGrid` 公開）；`DataGridTemplateColumn` + `FuncDataTemplate<DataRowView>` + code-fetch 的綁定策略不變。in-cell / EditForm 編輯策略的後續決策見 [ADR-021](adr-021-avalonia-datagrid-editing-strategy.md)。

## 後記（2026-06-14）：清單 cell 的 `supportsRecycling` 修正

上方「決策」範例對唯讀清單純文字 cell 用了 `supportsRecycling: true` —— 這與「`Text` 算死、非 binding」相沖：DataGrid 跨列回收 presenter 時不重跑建立委派，導致顯示文字與底層列脫鉤（lookup picker 上表現為「看到某列、帶回別列」）。已改為 `supportsRecycling: false`，詳見 [ADR-022](adr-022-avalonia-datagrid-cell-recycling.md)。
