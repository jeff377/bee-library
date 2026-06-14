# ADR-022：Avalonia DataGrid 清單儲存格不啟用模板回收

## 狀態

已採納（2026-06-14）

## 背景

[ADR-020](adr-020-avalonia-datagrid-binding-strategy.md) 確立 `GridControl`（繼承 `Avalonia.Controls.DataGrid`）的儲存格策略：用 `DataGridTemplateColumn` + `FuncDataTemplate<DataRowView>`，在 template 內以 code 顯式取值（`row.Row[fieldName]`）而非 Avalonia binding，因為 Avalonia binding engine 不會 dispatch 到 `DataRowView` 的 `ICustomTypeDescriptor` string indexer。

ADR-020 當時的範例（含實作）對**唯讀清單純文字 cell**使用 `supportsRecycling: true`：

```csharp
templateColumn.CellTemplate = new FuncDataTemplate<DataRowView>(
    (row, _) => new TextBlock { Text = FormatCell(row, fieldName, ...) },
    supportsRecycling: true);   // ← 與「Text 算死、非 binding」相沖
```

關鍵衝突：這個 `Text` 是在 template **建立當下**用當時的 `row` 算出的**固定字串**，**不是**綁定到 cell DataContext 的值。

Avalonia `DataGrid` 為效能維持一個 presenter 池，捲動／重新實體化時**把同一個 cell 視覺重複用到不同列**，只替換 `DataContext`。當 `supportsRecycling: true`，回收重用時**不會重跑** `FuncDataTemplate` 的建立委派 —— 它預期內容是 binding、會自行跟著 DataContext 更新。

兩者相撞的後果：presenter 被回收到另一列時，底層 `DataRowView` 換了，但算死的 `Text` 停在**舊列**的字。於是**畫面顯示的文字與底層實際的列脫鉤**。

此 bug 在 lookup picker 上最明顯：使用者看到某格顯示「SALES」便點它，框架取的是該視覺列**底層真正的** `DataRowView`（`SelectedItem.Row` 一直正確），結果帶回的是**別列**的資料 —— 即「開窗畫面顯示資料與取回實際資料不對應」。

> 為何潛伏未爆：資料少、照順序、初次實體化時每格各建一次（尚未觸發池重用）時顯示正確；一旦回收重用（多次開窗、捲動、重綁）才錯位。由 Bee.Northwind demo 階段 3 的 Employee→Department lookup 仔細測試逼出。

## 決策

**`GridControl` 的清單純文字 cell 模板改用 `supportsRecycling: false`。**

```csharp
templateColumn.CellTemplate = new FuncDataTemplate<DataRowView>(
    (row, _) => new TextBlock { Text = FormatCell(row, fieldName, ...) },
    supportsRecycling: false);   // 每列各建一個 cell，文字永遠對應該列
```

這同時是**回歸一致**：`GridControl` 內其他所有 cell 模板（lookup 顯示 cell、互動 cell〔ComboBox/DatePicker〕、`CellEditingTemplate`）**本來就都是 `false`**，只有這個唯讀清單純文字 cell 是 `true`，是漏網之魚。

## 後果

### 正面

- **顯示與底層列恆一致**：每列拿到自己新建、`Text` 當場以該列算出的 cell；presenter 不跨列重用 → 不再有「看到 A、取回 B」
- **修在框架、全面受惠**：所有用到 `GridControl` 清單顯示之處（`ListView` 清單、lookup picker、master-detail 明細 grid）一次修好
- **與控件內其他模板策略一致**：全部 cell 模板皆「fresh per row」

### 負面

- **放棄回收帶來的視覺重用**：每列各配置一個 `TextBlock`。對本框架的清單／picker（資料量小、單頁為主）成本可忽略；大資料量清單若日後成為瓶頸，正解是改走「真正的 binding cell」（見下）而非重新開回收

### 中性

- **另一條正解：改用 binding 而非算死字串**，如此 `supportsRecycling: true` 也安全。但本控件其餘 cell 皆採「fresh per row（false）」策略，且 ADR-020 已說明為何不走 Avalonia binding（`DataRowView` string indexer 不被 binding engine 支援）；改 `false` 與既有策略最一致、改動最小、風險最低
- **不影響選取／寫回正確性**：選取一直是物件參考（`SelectedItem.Row`）、寫回（`ApplyLookupSelection`）一直以欄位名存取，兩者本來就正確；本 ADR 只修**顯示層**讓使用者看到的文字對應正確的列

## 相關連結

- [ADR-020：Avalonia DataGrid 對 DataTable 列的綁定策略](adr-020-avalonia-datagrid-binding-strategy.md) — 本 ADR 修正其範例中的 `supportsRecycling: true`
- [ADR-021：Avalonia DataGrid in-cell / EditForm 編輯策略](adr-021-avalonia-datagrid-editing-strategy.md)
- `src/Bee.UI.Avalonia/Controls/Editors/GridControl.cs` — `BuildColumn` 清單純文字 cell 模板
- 逼出此問題的 plan：`docs/plans/plan-bee-northwind-demo.md` 階段 3（Employee→Department lookup）

## 不在範圍

- **大資料量清單的虛擬化／回收效能**：若日後需要，改走真正的 binding cell（需先解決 `DataRowView` indexer 的 binding 支援，屬 Avalonia upstream 議題），不在本 ADR
