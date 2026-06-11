# 計畫：GridControl 列級編輯表單（EditForm 編輯模式，彈窗呈現）

**狀態：✅ 已完成（2026-06-11）**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 1 | UI 層：`GridEditMode` enum + `GridControl.EditMode` / `DynamicForm.DetailEditMode` 屬性 | ✅ 已完成（2026-06-11） |
| 2 | Field editor 列綁定泛化（binder row-scope + `FormDataObject` 列存取） | ✅ 已完成（2026-06-11） |
| 3 | `RowEditPanel` 編輯面 + 彈窗包裝（暫存編輯 + 確認/取消） | ✅ 已完成（2026-06-11） |
| 4 | `GridControl` / `DynamicForm` 整合（EditMode 分流） | ✅ 已完成（2026-06-11） |
| 5 | Gallery 比對區 + 收尾 | ✅ 已完成（2026-06-11） |

## 背景

[ADR-021](../adr/adr-021-avalonia-datagrid-editing-strategy.md) 確立 in-cell 編輯（文字走編輯管線、popup 型 click-to-swap）。對欄位多、需要完整輸入體驗或「可取消」語意的明細表，**列級編輯面板**是更合適的模式：grid 維持唯讀，選列後在 grid 下方以 field editor 控件組編輯整列，確認才落實、取消完整還原。

已確認的設計決策：

1. **編輯模式是 UI 層屬性，不進定義層**（使用者決策）：`LayoutGrid` 是跨 UI 家族的共同定義，編輯模型屬各框架的呈現決策——`GridEditMode` enum（`InCell` / `EditForm`）定義在 `Bee.UI.Avalonia`，由 `GridControl.EditMode` 承載、宿主統一指定；`EditForm` 命名對齊 ERP 慣例（DevExpress EditForm 模式）
2. **彈窗呈現**（原 inline 面板方案經評估放棄）：批次作業的明細列數可能很大，inline 面板在長 grid 下有「編輯區離選列太遠」與「展開/收合 + 新增列落表尾造成畫面上下跳動」兩個結構性問題；modal 彈窗零版面位移、與列數無關，且與暫存語意天然契合。**編輯面與呈現分離**：核心是 `RowEditPanel`（UserControl，可不開視窗單測），`DynamicForm` 以 `Window.ShowDialog` 包裝；未來少列數場景要 inline 呈現時編輯面可重用
3. **暫存語意用 ADO.NET 原生 `DataRow.BeginEdit` / `EndEdit` / `CancelEdit`**，並以 **`FormDataObject` 的列編輯協定 API** 包裝（`BeginRowEdit` / `CommitRowEdit` / `CancelRowEdit`）：`BeginEdit` 期間 ADO.NET 暫停事件、`EndEdit` 只發 `RowChanged` 不逐欄補發——`CommitRowEdit` 在 `EndEdit` **前** diff Proposed vs Current 取得本次 session 實際變更的欄位，落實後逐欄補發 `FieldValueChanged`；取消路徑零事件、完整還原。session 知識收在 ViewModel，橋接層維持無腦轉發、不加啟發式

事件橋接（plan-avalonia-dataobject-event-bridge ✅）是本計畫的地基：`EndEdit` 落實時由橋接統一標 dirty / 發事件，面板不需要手動通知。

## 階段 1：UI 層 `GridEditMode`

`src/Bee.UI.Avalonia/Controls/Editors/`（不動 `Bee.Definition`）：

- 新增 `GridEditMode.cs`：`enum GridEditMode { InCell, EditForm }`
- `GridControl` 新增 `EditMode` StyledProperty（預設 `InCell`），axaml / 程式碼皆可設
- `DynamicForm` 新增 `DetailEditMode` StyledProperty（預設 `InCell`），套用到其產生的所有明細 grid——宿主一處設定即可統一明細編輯模型
- 測試：屬性註冊、預設值、`DynamicForm` 明細 grid 繼承設定

## 階段 2：Field editor 列綁定泛化

讓 field editors 能綁「指定 `DataRow`」而非僅 master row：

1. **[FormDataObject.cs](../../src/Bee.UI.Avalonia/DataObjects/FormDataObject.cs) 列存取 + 列編輯協定**：
   - `GetField(DataRow row, string fieldName)` / `SetField(DataRow row, string fieldName, string? value)` —— 重用既有 `FormatForBinding` / `ConvertToColumnValue` 與 compare-first 防護；master 版內部改呼列版（master row 為特例）
   - `BeginRowEdit(row)` / `CommitRowEdit(row)` / `CancelRowEdit(row)` —— Commit 在 `EndEdit` 前 diff Proposed vs Current（`row[col, DataRowVersion.Proposed]` vs `Current`），落實後逐欄補發 `FieldValueChanged`（橋接在 `BeginEdit` 期間收不到 `ColumnChanged`、`EndEdit` 只有 `RowChanged`）；Cancel = `CancelEdit` 零事件
2. **[FieldEditorBinder.cs](../../src/Bee.UI.Avalonia/Controls/Editors/FieldEditorBinder.cs) row-scope**：
   - 新增 `BindRow(FormDataObject, LayoutFieldBase field, DataRow row)` 路徑：`GetValue` / `WriteBack` 走列版 helpers；`FormField` metadata 以 `(row.Table.TableName, fieldName)` 解析（`GetFormField` 表名多載已存在）
   - 事件過濾：row 綁定時比對 `e.Row == 目標列`（master 綁定維持表名過濾）
   - metadata 參數型別由 `LayoutField` 放寬為 `LayoutFieldBase`（`LayoutColumn` 同樣繼承它，`ReadOnly` 等屬性在基底）
3. **`IFieldEditor` 增列多載**：`void Bind(FormDataObject dataObject, LayoutFieldBase field, DataRow row)`，七個控件各轉呼 binder（每個 +3 行）
4. **ADO.NET 行為釘住（測試）**：`BeginEdit` 期間 proposed 寫入是否觸發 `ColumnChanged`（文件不明確）——若不觸發（預期），`CommitRowEdit` 的補發即為唯一事件來源；若觸發，`CommitRowEdit` 改為不補發（事件已逐筆出過）並以測試固定行為。兩種情況協定 API 的對外語意不變

## 階段 3：`RowEditPanel` 編輯面 + 彈窗包裝

新增 `src/Bee.UI.Avalonia/Controls/Editors/RowEditPanel.cs`（`UserControl` 組合式，編輯面核心）：

- `Bind(FormDataObject dataObject, LayoutGrid layout, DataRow row)`：
  - `dataObject.BeginRowEdit(row)` → 依 `layout.Columns`（Visible）以 `FieldEditorFactory` 產生 caption + editor 的格線（仿 `DynamicForm.BuildFieldGrid` 的簡化版，兩欄排列），每個 editor 走階段 2 的列綁定
- 「確認」→ 各 editor `Unbind` → `dataObject.CommitRowEdit(row)`（落實 + 逐欄補發事件 + dirty）→ raise `EditCommitted` 事件
- 「取消」→ 各 editor `Unbind` → `dataObject.CancelRowEdit(row)` → raise `EditCancelled`
- `Unbind()` 公開（未確認的編輯一律 `CancelEdit`）
- 測試：BeginRowEdit 啟動、編輯後取消完整還原且零事件、確認落實 + dirty + 逐欄 FieldValueChanged 補發（只含本次變更欄位）、重複 Bind 自動取消前一筆

彈窗包裝 `RowEditDialog`（static helper 或輕量 `Window` 子類）：

- `ShowAsync(Visual host, FormDataObject, LayoutGrid, DataRow)`：建 `Window`（Title = layout.Caption、`SizeToContent`、`WindowStartupLocation.CenterOwner`）裝 `RowEditPanel`，owner 以 `TopLevel.GetTopLevel(host) as Window` 解析（解析不到時退回非 modal `Show`，嵌入式宿主不炸）
- `EditCommitted` / `EditCancelled` → 關窗，回傳是否確認

## 階段 4：`GridControl` / `DynamicForm` 整合

1. **`GridControl.SetControlState`**：`EditMode == EditForm` 時恆唯讀（in-cell 不啟用），與 list 模式同呈現
2. **`DynamicForm.BuildDetailSection`** 依 `DetailEditMode` 分流：
   - `InCell`：現行為不變
   - `EditForm`：grid 唯讀；**雙擊列**（`DataGrid.DoubleTapped` + `SelectedItem`）或工具列 `Edit` 鈕開彈窗編輯選取列；確認後**重 realize grid 列**（realized 的 TextBlock cell 不會自動反映 `EndEdit` 後的值）並 `ScrollIntoView` 回到該列
   - 工具列照 `AllowActions`：`Add` → `AddRow` + 開彈窗編輯新列（取消時連 row 一併移除，避免留空列）；`Delete` → 刪選取列；`Edit` 鈕僅 `EditForm` 模式出現
3. 注意：`RowSelected` 事件目前只帶 `sys_rowid` Guid（明細列不一定有）——取列一律用 `SelectedItem`（`DataRowView.Row`）直取，不依賴 rowid

## 階段 5：Gallery 比對區 + 收尾

- Gallery 新增 EditForm 區：同一份 Phones 明細以 `GridControl.EditMode = EditForm` 呈現（唯讀 grid + 雙擊開彈窗），與 in-cell 區對照；驗證確認/取消/新增列（取消移除空列）流程
- ADR-021 補記 EditForm 模式落地（決策段的「選項 4 另案」更新為已實作，含 inline 面板 → 彈窗的取捨）

## 不在範圍

- 面板內欄位驗證規則（必填 / 格式）——驗證體系另案
- Blazor / MAUI 家族鏡像
- in-cell 模式行為變更

## 相容性

定義層零變更（`GridEditMode` 只存在於 `Bee.UI.Avalonia`），layout XML 完全不受影響；`IFieldEditor` 新增方法對外部實作者是破壞性，但該介面本 session 新增、未隨版發布。

## 驗證

1. `dotnet build src/Bee.UI.Avalonia/Bee.UI.Avalonia.csproj --configuration Release` + `Bee.UI.Avalonia.UnitTests`
2. Gallery 跑 EditForm 區：雙擊開彈窗、編輯後取消還原、確認落實（grid 顯示新值 + dirty + 捲回該列）、新增列（取消移除空列）、AllowActions 遮蔽
3. push main 後 `build-ci.yml` 全套驗證

## 工作流

桌面環境本機可驗證 → 依階段直接 commit `main`，每階段本機 build + test 通過後 push。
