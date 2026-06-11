# 計畫：Avalonia GridControl 控件 — LayoutGrid 對應與明細表深度綁定

**狀態：🚧 進行中（2026-06-11）**



## Context

field editor 控件組已完成（plan-avalonia-field-editors，4 階段全 ✅），Grid 控件化是該 plan 明列的「另案」。現況：

- [DynamicGrid.cs](../../src/Bee.UI.Avalonia/Controls/DynamicGrid.cs) 是唯讀呈現用 `UserControl`（內包 `DataGrid`），只有 `FormView` 使用；欄位取值走 [ADR-020](../adr/adr-020-avalonia-datagrid-binding-strategy.md) 的 `FuncDataTemplate<DataRowView>` 策略
- 定義層 [IBindTableControl](../../src/Bee.Definition/Layouts/IBindTableControl.cs)（`TableName` / `DataTable` / `EndEdit()`）無任何實作
- [DynamicForm.cs](../../src/Bee.UI.Avalonia/Controls/DynamicForm.cs) 只渲染 master sections，`FormLayout.Details` 明細 grid 是已知缺口
- `LayoutGrid` 定義含 `TableName`、`AllowActions`（Add/Edit/Delete flags）、`Columns`（`LayoutColumn`：FieldName / Caption / ControlType / Width / 格式）

使用者已確認的設計決策：

1. **命名 `GridControl`**，繼承 `DataGrid` + `StyleKeyOverride`，放 `Controls/Editors/` 與 field editor 家族同居
2. **先唯讀、後編輯**，同一 plan 內分階段交付
3. **淘汰 `DynamicGrid`**（破壞性變更）：`FormView` 直接改用 `GridControl`，刪除 `DynamicGrid` 與其測試。僅限 Avalonia 家族——Blazor / MAUI 的同名元件不動
4. **支援 `FormScope` ambient 綁定**：設 `TableName` 即從 ambient `FormDataObject` 取明細表，與 field editor 體驗一致

## 階段表

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 1 | `GridControl` 唯讀核心（顯式綁定 / 欄位生成 / RowSelected） | ✅ 已完成（2026-06-11） |
| 2 | `FormScope` ambient + `DynamicForm` Details 渲染 + Gallery 比對區 | ✅ 已完成（2026-06-11） |
| 3 | `FormView` 遷移 + 淘汰 `DynamicGrid` | 📝 待做 |
| 4 | In-cell 編輯 + `AllowActions` 增刪列 + `EndEdit` | 📝 待做 |

## 階段 1：`GridControl` 唯讀核心

新增 `src/Bee.UI.Avalonia/Controls/Editors/GridControl.cs`：

- `public class GridControl : DataGrid, IBindTableControl, IUIControl`，`StyleKeyOverride => typeof(DataGrid)`
- 建構預設：`IsReadOnly = true`、`AutoGenerateColumns = false`、`SelectionMode = Single`、可調欄寬
- **顯式綁定兩條路**（對應兩種資料來源）：
  - `Bind(FormDataObject dataObject, LayoutGrid layout)` — 明細表：依 `layout.TableName` 從 `DataSet.Tables` 解析 `DataTable`
  - `Bind(LayoutGrid layout, DataTable rows)` — 列表模式：`GetListAsync` 的結果不屬於 `FormDataObject`，由呼叫端直接給表
- **欄位生成**沿用 ADR-020 策略：`DataGridTemplateColumn` + `FuncDataTemplate<DataRowView>`；`BuildColumn` / `FormatCell` / `Width` 處理邏輯自 `DynamicGrid` 原樣搬移（含 per-column closure capture 註解）
- `RowSelected` 事件（`sys_rowid` → `Guid`）與 `TryGetRowId` 搬移
- `IBindTableControl` 實作：`TableName` get/set（StyledProperty）、`DataTable` get/set（set 觸發重建列）、`EndEdit()` 唯讀階段 no-op（XML `<remarks>` 註明階段 4 補實作）
- `SetControlState`：唯讀階段一律維持 `IsReadOnly = true`（編輯能力到階段 4 才開）
- **空資料呈現空表頭**：不再內建 empty placeholder（`DynamicGrid` 的 `EmptyText` 行為由 host 自行疊加）
- 測試 `tests/Bee.UI.Avalonia.UnitTests/Controls/Editors/GridControlTests.cs`：把 `DynamicGridTests` 場景改寫到新 API（欄位生成 / Visible 過濾 / Width / 格式化 / RowSelected / StyleKeyOverride / 兩種 Bind）

## 階段 2：`FormScope` ambient + `DynamicForm` Details

- `TableName` 設定 + attach 到 logical tree 時，從 `FormScope.DataObjectProperty` 取 ambient `FormDataObject`，解析 `DataSet.Tables[TableName]` 自動綁定；`DataSetReplaced` → 重解析 + 刷新；detach 解除訂閱（防洩漏，與 `FieldEditorBinder` 同策略）
- Binder 採**獨立的 `GridControlBinder`**（不硬塞 `FieldEditorBinder`）：欄位 binder 與表 binder 的職責不同（解析 DataTable vs 讀寫 field value），共用的只有 ambient/attach 狀態機模式
- `DynamicForm` 渲染 `FormLayout.Details`：master sections 之下，每個 `LayoutGrid` 一段 caption + 綁定好的 `GridControl`（補上既有缺口；layout 來源 `FormLayout.Details`，資料來源 `DataObject` 的明細表）
- Gallery 新增 GridControl 比對區（原生 `DataGrid` vs `GridControl`，含空表與帶資料兩態）；`samples/Avalonia.Editors.Gallery` csproj 需加 `Avalonia.Controls.DataGrid` 12.0.x + `Semi.Avalonia.DataGrid` 12.0.3（theme StyleInclude 進 App.axaml）

## 階段 3：`FormView` 遷移 + 淘汰 `DynamicGrid`

- [FormView.cs](../../src/Bee.UI.Avalonia/Controls/FormView.cs) 的 `_grid` 改 `GridControl`：`_grid.ListLayout = Schema.GetListLayout()` + `_grid.Rows = response.Table` 改為 `_grid.Bind(Schema.GetListLayout(), response.Table)`；`RowSelected` 接線不變；空資料提示（原 `EmptyText`）改由 `FormView` 以 `TextBlock` 切換自行處理
- 刪除 `DynamicGrid.cs` 與 `DynamicGridTests.cs`（場景已由 `GridControlTests` 覆蓋）
- 檢查 `docs/development-cookbook.md` 等文件對 Avalonia `DynamicGrid` 的提及並更新

## 階段 4：In-cell 編輯 + `AllowActions`（最後交付）

- `CellEditingTemplate` 依 `LayoutColumn.ControlType` 提供編輯控件（文字 / 勾選 / 日期 / 下拉用輕量原生控件 + 手動 two-way 寫回 `DataRow`——ADR-020 的 binding 限制同樣適用於編輯模板）
- `LayoutGrid.AllowActions` flags → 增列 / 刪列操作（grid 上方工具列；`Add` 不允許時隱藏）
- `EndEdit()` 實作：commit 編輯中的列（`DataGrid.CommitEdit` + `DataRowView.EndEdit`）
- dirty 追蹤：明細變更以 `DataRow.RowState` 判定；評估把 `FormDataObject.IsDirty` 擴為「master 旗標 || `DataSet.HasChanges()`」
- `SetControlState`：`View` → `IsReadOnly = true`；`Add` / `Edit` → 依 `AllowActions` 決定編輯能力
- **風險註記**：Avalonia `DataGrid` 編輯 API 與 `DataRowView` 組合雷多；實作中若確認不可行，退回「列級編輯對話框」方案並補 ADR 記錄取捨

## 不在本次範圍

- Blazor / MAUI 家族的 `DynamicGrid` 重構（待 Avalonia 模式驗證後另案鏡像）
- `ButtonEdit` lookup 開窗（前案遺留，另案）
- Master-Detail 的明細列「展開編輯子表單」模式

## 驗證

1. `dotnet build src/Bee.UI.Avalonia/Bee.UI.Avalonia.csproj --configuration Release`
2. `./test.sh tests/Bee.UI.Avalonia.UnitTests/Bee.UI.Avalonia.UnitTests.csproj`（純邏輯，無 DB 依賴）
3. Gallery 跑起來看 GridControl 區：原生 vs 繼承樣式一致（Light/Dark）、空表與帶資料兩態
4. `samples/Avalonia.Demo` Debug 跑 `FormView` 冒煙：列表渲染、選列載入主檔、Save/Delete 後列表刷新（階段 3 後）；階段 4 後加驗明細編輯
5. push main 後 `build-ci.yml` 全套驗證

## 工作流

桌面環境本機可驗證 → 依階段直接 commit `main`，每階段本機 build + test 通過後 push（與前案相同）。
