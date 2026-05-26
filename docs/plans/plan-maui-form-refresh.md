# 計畫：Maui FormPage `New` / `Load` 點擊後 form 不重繪

**狀態：✅ 已完成（2026-05-26）**

## 背景

Maui.Demo 登入後 grid 正常顯示 3 筆 seed Employee。點「New」按鈕後:

- 預期：表單欄位清空、顯示 server 回傳的新 row 預設值（含 server-issued `sys_rowid`），Save/Delete 按鈕亮起。
- 實際：**完全無視覺變化**，無錯誤訊息。

Blazor.Server.Demo / Blazor.Wasm.Demo 的「New」正常（已被使用者驗證能新增 E004 Jeff）。問題僅在 MAUI 端。

## Root cause

[src/Bee.UI.Maui/Controls/DynamicForm.cs:38–42](src/Bee.UI.Maui/Controls/DynamicForm.cs:38)：

```csharp
public static readonly BindableProperty DataObjectProperty = BindableProperty.Create(
    nameof(DataObject),
    typeof(FormDataObject),
    typeof(DynamicForm),
    propertyChanged: (b, _, _) => ((DynamicForm)b).Rebuild());
```

MAUI 的 `BindableProperty` **只在「新值與舊值 reference 不同」時才觸發 `propertyChanged`**（reference equality）。

`FormPage.RefreshFormView()` 在每次操作完做：

```csharp
private void RefreshFormView()
{
    // Reassign DataObject so the DynamicForm rebuilds its inputs against
    // the (possibly replaced) DataSet returned by the connector.
    _form.DataObject = _dataObject;
}
```

但 `_dataObject` 從 `AttachDataObject()` 建立後**永遠是同一個 instance**，`NewAsync` / `LoadAsync` / `SaveAsync` / `DeleteAsync` 只是 mutate 它內部的 `DataSet` 欄位，從不換 instance。因此：

- `_form.DataObject = _dataObject` → 新舊值 reference 相同 → `propertyChanged` 不開火 → `Rebuild()` 不被呼叫 → form 不重繪

Grid 沒踩到是因為 `_grid.Rows = response.Table` 每次 server 都回傳新建的 `DataTable`，reference 必然不同。

Blazor 沒踩到是因為 Razor 在 event handler 跑完會自動 `StateHasChanged()` 觸發整棵 component 重新 diff render，不需要靠 BindableProperty 重繪。

## 修法方向

`DynamicForm` 開放一個 `public Refresh()` 方法（呼叫既有 private `Rebuild()`），`FormPage.RefreshFormView()` 直接叫它，不再倚賴 BindableProperty 自動觸發。

理由：
- 最小變動。`Rebuild()` 既有 logic 完全沿用
- 不動 `DataObjectProperty` 的 binding 行為（破壞性風險低）
- 不需要把 `FormDataObject` 改成 INotifyPropertyChanged（重構成本不成比例）
- 不需要重建 `_dataObject` instance（會打亂同樣綁定它的內部欄位 ViewModel）

## 修改範圍

### 1. [src/Bee.UI.Maui/Controls/DynamicForm.cs](src/Bee.UI.Maui/Controls/DynamicForm.cs)

- `private void Rebuild()` 旁加 `public void Refresh()` 入口（單純 forward 到 `Rebuild`）
- XML doc 解釋使用情境（DataObject 內部 mutate 後手動觸發重繪）

### 2. [src/Bee.UI.Maui/Controls/FormPage.cs:417–422](src/Bee.UI.Maui/Controls/FormPage.cs:417)

- `RefreshFormView()` 改為呼叫 `_form.Refresh()`，移除「reassign 同 instance」的 hack（並更新註解說明為何用 Refresh）

## 不做的事

- 不改 `DynamicForm.DataObjectProperty` 的 binding 機制
- 不重構 `FormDataObject` 成 observable / INotifyPropertyChanged（範圍過大）
- 不動 `DynamicGrid`（已正常）
- 不動 Blazor 版（不踩此 bug）

## 測試策略

### 既有測試

- `tests/Bee.UI.Maui.UnitTests/` 跑 `./test.sh` 全綠

### 手動 smoke（必須親自跑）

`samples/Maui.Demo`:

1. Login `demo`/`demo` → grid 顯示 E001/E002/E003
2. 點「**New**」→ 表單欄位清空、Save/Delete 按鈕亮起（**回歸目標**）
3. 填 Employee ID / Name / Hire Date → 點「Save」→ grid 多出新 row
4. 點 grid 列 → 表單載入該 row（**順帶確認 LoadAsync 也被同一個 fix 覆蓋**）
5. 點「Delete」→ row 從 grid 移除、表單清空

### 不增加 unit test 的理由

DynamicForm 的 BindableProperty 行為與 MAUI runtime 緊耦合，pure xunit 無法重現 propertyChanged 觸發語意；手動 smoke 是這層 UI 行為的可靠驗證方式。

## 執行步驟

1. 加 `DynamicForm.Refresh()`
2. 改 `FormPage.RefreshFormView()`
3. `dotnet build -c Release` 通過
4. `./test.sh` 全綠
5. 起 `samples/Maui.Demo`（Debug），依上述手動 smoke 5 步驟跑
6. 確認 OK 後 commit 到 main + push
7. Plan 標示 ✅ 完成
