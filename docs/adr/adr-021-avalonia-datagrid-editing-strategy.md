# ADR-021：Avalonia DataGrid 的 in-cell 編輯策略

## 狀態

已採納（2026-06-11）

## 背景

[ADR-020](adr-020-avalonia-datagrid-binding-strategy.md) 確立 `GridControl`（繼承 `Avalonia.Controls.DataGrid`）以 `DataGridTemplateColumn` + `FuncDataTemplate<DataRowView>` 呈現 `DataTable` 列。plan-avalonia-grid-control 階段 4 為明細表加上 in-cell 編輯：`CellEditingTemplate` 依 `LayoutColumn.ControlType` 提供對應編輯控件（`TextBox` / `CheckBox` / `DatePicker` / `ComboBox`），寫回直接落 `DataRow`。

Gallery 實測結果：**文字欄（`TextEdit`）編輯正常，popup 型編輯器全數行為異常**。根因是 Avalonia `DataGrid` 編輯管線的固有設計——

- 編輯管線假設「編輯期間焦點留在 cell 內」，以焦點離開作為 commit / 結束編輯的訊號
- `ComboBox` 的下拉清單與 `DatePicker` 的選擇面板都是 **popup**：一打開，焦點即離開 cell，`DataGrid` 判定編輯結束並撕掉 `CellEditingTemplate`，popup 被連帶收掉或互動中斷
- `CheckBox` 雖無 popup，但「雙擊進編輯模式 → 再點一次才能勾選」的操作對布林欄是多餘的儀式

這不是實作 bug，而是 `DataGrid` template-column 編輯管線與 popup 型控件的結構性衝突。

## 考慮過的選項

1. **內建 bound columns**（`DataGridTextColumn` / `DataGridCheckBoxColumn`）：與編輯管線整合良好，但需要可綁定的 row 物件——`DataRowView` 正是 ADR-020 確認 Avalonia binding engine 無法解析的型別，繞道需要為每列建 wrapper VM，違背 DataSet-as-DTO 的零投影原則。
2. **攔截編輯管線事件**（`CellEditEnding` 中偵測 popup 開啟並取消結束）：Avalonia `DataGrid` 未公開足夠的掛載點判斷「焦點移到了自己 cell 的 popup 內」，hack 脆弱且版本敏感。
3. **常駐編輯器（always-on editors）**：popup 型欄位的互動控件直接放 `CellTemplate`（顯示模板），完全繞過編輯管線——沒有 edit session 可撕，popup 行為正常；點擊即生效。LOB 應用的慣用模式。
4. **列級編輯面板**：grid 全面唯讀，選列後在 grid 外以 field editor 控件組編輯。體驗最一致，但操作步驟較多，且不解決「快速逐格修改」的需求。
5. **換 grid**（官方 `TreeDataGrid`、商用 Actipro / DevExpress）：編輯模型自帶，但重構量大、theme 生態另算，屬中長期選項。

## 決策

採**混合策略（選項 3 為主）**，依 `LayoutColumn.ControlType` 分流：

| ControlType | Cell 呈現 | 編輯方式 |
|-------------|----------|---------|
| `TextEdit` / `ButtonEdit` / `Auto`（文字類） | `TextBlock` | `CellEditingTemplate`（雙擊 / F2 進入，`TextBox` 編輯）——焦點留在 cell 內，編輯管線運作正常 |
| `CheckEdit` / `DropDownEdit` / `DateEdit` / `YearMonthEdit` | **可編輯時**：互動控件常駐 `CellTemplate`；**唯讀時**：`TextBlock` 格式化文字 | 直接互動（點勾選、開下拉、開日期面板），繞過編輯管線；對應 column 標記 `IsReadOnly = true` 使管線永不介入 |

配套規則：

- 常駐編輯器的啟用狀態在模板建構時決定，`SetControlState` 切換唯讀狀態時**重新 realize 列**（`ItemsSource` 重設）讓既有 cell 反映新狀態
- 唯讀呈現（list 模式、`View` 模式、`LayoutColumn.ReadOnly`）退回 `TextBlock`，視覺與唯讀 grid 完全一致；**例外：布林欄任何狀態都呈現置中的 `CheckBox`**（唯讀時 disabled）——勾選框比 "True"/"False" 文字易讀
- 常駐編輯器以 **inline chrome** 呈現（背景 / 邊框透明、撐滿 cell 寬），靜置時與周圍文字 cell 視覺一致；代價是蓋掉主題的 hover tint（local value 優先於 theme style），屬有意取捨
- **日期欄用 `CalendarDatePicker`**（文字 + 日曆圖示，`CustomDateFormatString` 控制 `yyyy-MM-dd` / `yyyy-MM`），不用三段式 `DatePicker`——後者天生過寬，在 cell 內必截斷；`YearMonthEdit` 的日曆仍會選到「日」，只取年月寫回
- 寫回仍直接落 `DataRow`（ADR-020 的限制同樣適用顯示模板內的控件），經 `FormDataObject.MarkDirty()` 反映 dirty
- 控件的變更監聽一律 hook **property changed**（`TextProperty` / `SelectedDateProperty`），不依賴 `TextChanged` / `SelectedDateChanged` 事件——後者對程式設值不保證觸發

選項 4（列級編輯面板）不因此放棄：規劃為**進階編輯模式**另案實作（plan-avalonia-grid-row-edit-panel），與 in-cell 模式由 layout 設定選擇。

## 影響

- popup 型欄位點擊即編輯，少了「先雙擊進入編輯模式」的儀式；文字欄維持標準 DataGrid 編輯手感——兩種手感並存是本策略的有意取捨
- 可編輯明細表的 popup 型欄位每列常駐一個互動控件，列數極大時有渲染成本；明細表情境（數十列內）無感，列表模式不受影響（恆唯讀 → `TextBlock`）
- 若未來 Avalonia `DataGrid` 編輯管線支援 popup 型編輯器，或改採 `TreeDataGrid`，本策略可逐欄位回退而不影響呼叫端 API
