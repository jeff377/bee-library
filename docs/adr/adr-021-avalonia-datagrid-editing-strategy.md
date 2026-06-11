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
| `CheckEdit` | 置中 `CheckBox` 常駐（唯讀時 disabled） | 直接點勾選；繞過編輯管線 |
| `DropDownEdit` / `DateEdit` / `YearMonthEdit` | **靜置**：`TextBlock` 格式化文字（與唯讀 cell 完全相同）；**點擊**：在 `CellTemplate` 內自管置換為編輯控件 | 單擊 cell → 置換編輯器（下拉自動展開）；值寫回或編輯結束後**換回文字呈現**。置換由控件自管、不經編輯管線，popup 不會被撕掉 |

點擊置換（click-to-swap）曾評估過的替代版本是「互動控件常駐 cell」：實測樣式問題連環（ComboBox 寬度、`DatePicker` 截斷、與文字 cell 視覺不一致），且要持續以 local value 對抗主題；靜置回歸 `TextBlock` 一次解決全部視覺一致性問題，編輯器只在編輯瞬間存在。

配套規則：

- popup 型 column 標記 `IsReadOnly = true` 使 DataGrid 編輯管線永不介入；置換的生命週期：`PointerPressed` 換入 → 結束條件依控件而異，換回時**重讀 `DataRow`** 呈現已寫回的值
  - `ComboBox`：換入後 **Dispatcher 延後**自動展開（同一次點擊的後續事件會把立刻開啟的下拉再關掉）；「真的開啟過」之後的關閉才視為編輯結束
  - `DatePicker`：**僅值確認（`SelectedDate` 變更）時換回**——`LostFocus` 不可接（選輪 flyout 拿走焦點會提前撕掉編輯器）；放棄選擇的編輯器留在原地，由「下一次 inline 編輯開始」或 `EndEdit()` 收掉
  - grid 同時只允許一個 inline 編輯器（開新的先收舊的；列重 realize 時一併重置）
- 可編輯狀態在模板建構時決定，`SetControlState` 切換唯讀時**重新 realize 列**（`ItemsSource` 重設）
- 唯讀呈現（list 模式、`View` 模式、`LayoutColumn.ReadOnly`）為 `TextBlock`；**例外：布林欄任何狀態都呈現置中 `CheckBox`**（唯讀時 disabled）——勾選框比 "True"/"False" 文字易讀
- 日期編輯維持**三段式 `DatePicker`**（`DayVisible` 區分 Date / YearMonth）：編輯器只在編輯瞬間出現，寬度截斷不再是常態問題，且選輪體驗與 form 端 `DateEdit` 一致
- 寫回仍直接落 `DataRow`（ADR-020 的限制同樣適用顯示模板內的控件），經 `FormDataObject.MarkDirty()` 反映 dirty
- 控件的變更監聽一律 hook **property changed**（`TextProperty` / `SelectedDateProperty`），不依賴 `TextChanged` / `SelectedDateChanged` 事件——後者對程式設值不保證觸發

選項 4（列級編輯）已落地為 **EditForm 模式**（plan-avalonia-grid-row-edit-panel，2026-06-11）：

- 編輯模式（`GridEditMode`：`InCell` / `EditForm`）是 **UI 層屬性**（`GridControl.EditMode` / `DynamicForm.DetailEditMode`），不進共同定義層——`LayoutGrid` 跨 UI 家族共用，編輯模型屬各框架的呈現決策
- 呈現採**彈窗**（`RowEditDialog` 包 `RowEditPanel`）而非 grid 下方 inline 面板：批次作業明細列數可能很大，inline 面板有「編輯區離選列太遠」與「展開收合 + 新增列落表尾造成畫面跳動」兩個結構性問題；彈窗零版面位移、與列數無關
- 暫存語意走 `FormDataObject` 的列編輯協定（`BeginRowEdit` / `CommitRowEdit` / `CancelRowEdit`，包裝 ADO.NET `DataRow.BeginEdit` 家族）：實測釘住 **`BeginEdit` 不抑制 `ColumnChanged`、且無變更的 `EndEdit` 仍發 `RowChanged`** 兩個 ADO.NET 行為——以「編輯中列集合」讓事件橋接靜默、commit 時 diff Proposed vs Current 統一補發、無變更 commit 改走 `CancelEdit`；取消零事件、確認事件齊全
- 確認後 grid 重 realize + `ScrollIntoView` 捲回該列；`Add` 開彈窗編輯新列、取消時移除空列

## 影響

- popup 型欄位點擊即編輯，少了「先雙擊進入編輯模式」的儀式；文字欄維持標準 DataGrid 編輯手感——兩種手感並存是本策略的有意取捨
- 可編輯明細表的 popup 型欄位每列常駐一個互動控件，列數極大時有渲染成本；明細表情境（數十列內）無感，列表模式不受影響（恆唯讀 → `TextBlock`）
- 若未來 Avalonia `DataGrid` 編輯管線支援 popup 型編輯器，或改採 `TreeDataGrid`，本策略可逐欄位回退而不影響呼叫端 API
