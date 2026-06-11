# 計畫：GridControl 內建圖示工具列與 AllowEdit 屬性（組合式改版）

**狀態：✅ 已完成（2026-06-11）**

> 本計畫為 [plan-layout-form-mode-editability.md](plan-layout-form-mode-editability.md) 的**前置作業**。
> 工具列與 FormMode 無直接相關，先獨立完成；formMode 計畫之後只需在
> `SetControlState` 的映射加入 `AllowEditModes` 條件。

## 背景

目前 `GridControl : DataGrid`，明細工具列由 `DynamicForm.BuildDetailToolbar` 外掛（文字按鈕 Add/Edit/Delete），有三個問題：

1. 工具列不屬於 grid 本身——`FormView` 等其他 host 用 GridControl 拿不到工具列；RowEditDialog 的 Add/Edit 流程也卡在 DynamicForm 內。
2. 工具列不隨 grid 的編輯狀態啟停——View 模式下按鈕仍可按。
3. grid 的可編輯狀態散落在 `SetControlState` 的合成式內，host 無法以單一屬性控制（列表表單沒有 FormMode，無從透過 `SetControlState` 控制）。

確認後的設計方向（使用者拍板）：

- **工具列內建於 GridControl**，以圖示按鈕呈現，**grid 處於編輯狀態才顯示**（`IsVisible`，非 disabled）。
- **host 對 grid 的編輯控制收斂為單一屬性 `AllowEdit`**：`SetControlState(formMode)` 只負責 `AllowEdit = formMode != View`；工具列顯示、in-cell 編輯、double-tap 行為全部由 GridControl 內部依 `AllowEdit` 自理。
- **不依賴 ambient FormMode 存在**：只有單筆資料的基底表單有 FormMode；列表表單（`FormView`）直接用屬性控制，list-mode 綁定（無 `FormDataObject`）一律不可編輯。

## 設計

### 結構：DataGrid 子類 → 組合式控件

DataGrid 子類別無法在自身上方內嵌工具列，改為：

```
GridControl : ContentControl, IBindTableControl, IUIControl
└── DockPanel
    ├── 工具列（Dock=Top，StackPanel horizontal，圖示按鈕）
    └── DataGrid（內部實例，公開為 InnerGrid）
```

- `StyleKeyOverride => typeof(ContentControl)`（子類 theme lookup 雷，沿用既有 WARNING 註解模式）。
- 既有 cell template / click-to-swap 邏輯（ADR-020/021）原樣改掛內部 DataGrid，不動行為。
- **Breaking change**：`GridControl` 不再是 `DataGrid`。對外 API 保留並轉發：
  `Bind` ×2、`Unbind`、`TableName`、`EditMode`、`DataTable`、`Layout`、`RowSelected`、
  `AddRow`、`DeleteSelectedRow`、`EndEdit`、`RefreshRows`、`SetControlState`、`SelectedItem`。
  其餘 DataGrid 成員（`Columns`、`ItemsSource`、`IsReadOnly`…）經 `InnerGrid` 取用。
  記入下次發版 CHANGELOG。

### `AllowEdit` styled property

```csharp
public static readonly StyledProperty<bool> AllowEditProperty =
    AvaloniaProperty.Register<GridControl, bool>(nameof(AllowEdit), false);
```

- `SetControlState(formMode)` → `AllowEdit = formMode != SingleFormMode.View;`（`IUIControl` 契約維持，formMode 計畫再加 `AllowEditModes` gate）。
- `AllowEdit` / `EditMode` 變更、`Bind`、`RefreshFromDataObject` 都收斂到 `UpdateControlState()` 重算內部有效狀態：

```
canEdit        = AllowEdit && DataObject 已綁定 && _layout 不為 null
toolbarVisible = canEdit && AllowActions != None
inCellEdit     = canEdit && EditMode == InCell  && AllowActions.Edit
editFormFlow   = canEdit && EditMode == EditForm && AllowActions.Edit（Edit 圖示 + double-tap）
addAction      = toolbarVisible && AllowActions.Add
deleteAction   = toolbarVisible && AllowActions.Delete
```

- in-cell `IsReadOnly` 變更才重建 rows（沿用既有 guard）；工具列顯示變更不重建。
- ambient `FormScope.FormModeProperty` 預設 `Edit`，detail 綁定在無表單 scope 下（如 gallery）`AllowEdit` 為 true——與現行「ambient 預設可編輯」行為一致。

### 工具列圖示

- 自繪簡單幾何 path（plus / pencil / trash，24 grid），`PathIcon` + 小型按鈕，附 `ToolTip`；不依賴應用 theme 的 icon 資源（與 ButtonEdit 內嵌 geometry 同一手法）。
- `Geometry.Parse` 需要 platform services，延後到 `OnAttachedToVisualTree`（unit test 無 platform 的既有雷）。

### RowEditDialog 流程移入 GridControl

- `DynamicForm.AddDetailRowAsync` / `EditSelectedRowAsync` / `RefreshAndFocusRow` 移入 GridControl：
  - Add（InCell）：`AddRow()`；Add（EditForm）：`AddRow()` + `RowEditDialog`，取消則移除空列。
  - Edit（EditForm）：選取列開 `RowEditDialog`。
  - Delete：`EndEdit()` + `DeleteSelectedRow()`。
  - double-tap：EditForm 且 effective 可編輯時開編輯視窗。
- `DataObject` 由 `GridControlBinder` 取得、dialog parent 用自身。

### DynamicForm / FormView / gallery 同步

- `DynamicForm.BuildDetailSection` 簡化為 caption + GridControl；`BuildDetailToolbar` 與列編輯方法刪除。
- `FormView`：用法不變（`Bind(list)` / `DataTable` / `RowSelected` / `IsVisible` 都保留）。
- gallery：grid 區段沿用；in-cell 示範 grid 將出現內建工具列（行為升級，README 文字若有描述一併更新）。

## 測試

### GridControlTests 更新

- 型別測試：`ContentControl` 子類、`StyleKeyOverride == typeof(ContentControl)`、`InnerGrid` 為 `DataGrid`。
- 既有 `Columns` / `ItemsSource` / `IsReadOnly` / `SelectionMode` 斷言改經 `InnerGrid`；`SelectedItem` 走轉發屬性。
- 私有方法 reflection（`BuildCellEditor` 等）不變（仍在 GridControl 上）。

### 新增測試

- `SetControlState(View)` → `AllowEdit == false`；`SetControlState(Edit/Add)` → `true`。
- detail 綁定 + `AllowEdit = true` → 工具列顯示；`AllowEdit = false` → 隱藏且 `InnerGrid.IsReadOnly`。
- list-mode 綁定（無 DataObject）→ `AllowEdit = true` 仍不顯示工具列、不可編輯。
- 工具列按鈕依 `AllowActions` 顯示（`All` 三顆、`Add|Delete` 兩顆、InCell 模式無 Edit 鈕）。

### DynamicFormTests 更新

- 明細區不再有外掛工具列（detailStack 子項：caption + grid）；工具列相關斷言改對 GridControl 內部。

## 執行順序

1. GridControl 組合式改版（結構 + 轉發 + `AllowEdit` + `UpdateControlState`）。
2. 內建工具列（圖示、顯示邏輯、Add/Edit/Delete 行為 + RowEditDialog 流程內移）。
3. DynamicForm 簡化、FormView / gallery 同步。
4. 測試更新與新增 → Release build 0 警告 + 全測試通過 → commit main。
