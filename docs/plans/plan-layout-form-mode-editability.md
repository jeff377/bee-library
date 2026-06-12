# 計畫：Layout 定義層支援依表單模式（FormMode）的可編輯設定

**狀態：📝 擬定中**

> **前置作業**：
> 1. [plan-avalonia-gridcontrol-toolbar.md](plan-avalonia-gridcontrol-toolbar.md) ——
>    GridControl 內建圖示工具列 + `AllowEdit` 屬性（✅ 已完成）。
>    完成後本計畫的階段 3 只剩 `SetControlState` 映射加入 `AllowEditModes` 條件。
> 2. [plan-avalonia-single-form-base.md](plan-avalonia-single-form-base.md) ——
>    資料表單基底類別 `SingleFormBase`（FormMode 擁有者與 ambient 廣播）。
>    完成後本計畫的測試可走「表單切模式 → 廣播 → 子樹控件切換」的真實管線。

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 1 | Bee.Definition：`FormEditModes` enum + `LayoutField` / `LayoutGrid` 新增 `AllowEditModes` + 測試 | 📝 待做 |
| 2 | Bee.UI.Avalonia 欄位編輯器：`FieldEditorBinder.AllowsEdit` 合成 + 各編輯器 `SetControlState` + 測試 | 📝 待做 |
| 3 | Bee.UI.Avalonia GridControl：`SetControlState` 納入 `AllowEditModes` + 測試 | 📝 待做 |

## 背景

資料表單有三種模式（`SingleFormMode`：View 瀏覽 / Add 新增 / Edit 修改）。**只有單筆資料的基底表單才有 FormMode 屬性**（透過 `FormScope.FormModeProperty` 廣播），列表等其他表單沒有此屬性。表單切換模式時透過 `IUIControl.SetControlState(formMode)` 通知所有控制項切換為合適狀態。

目前的缺口：

1. **欄位層**：`LayoutFieldBase.ReadOnly` 只有單一 bool，是「任何模式都唯讀」的語意。無法表達「新增模式允許編輯、修改模式鎖定」（典型如單號、關鍵代碼欄）。各編輯器的 `SetControlState` 只有兩段邏輯：View → 唯讀；Add/Edit → 可編輯（除非 `ReadOnly`），Add 與 Edit 之間沒有差異化依據。
2. **Grid 層**：`LayoutGrid.AllowActions`（Add/Edit/Delete flags）是 mode-invariant 的「允許哪些動作」，無法表達「哪些表單模式下允許編輯」。
3. **Grid 的控制方式**：明細工具列目前由 `DynamicForm.BuildDetailToolbar` 外掛（文字按鈕），且不隨表單模式啟停。依確認後的設計方向，工具列應**內建於 GridControl**（圖示按鈕、編輯狀態才顯示），表單模式切換對 grid 的影響收斂為**設定 GridControl 的單一屬性**，其餘行為由 GridControl 內部自理。

## 設計

### 1. 新 enum：`FormEditModes`（Bee.Definition.Layouts）

```csharp
/// <summary>
/// Form modes in which editing is allowed.
/// </summary>
[Flags]
public enum FormEditModes
{
    /// <summary>Editing is not allowed in any mode.</summary>
    None = 0,
    /// <summary>Editing is allowed in Add mode.</summary>
    Add = 1,
    /// <summary>Editing is allowed in Edit mode.</summary>
    Edit = 2,
    /// <summary>Editing is allowed in both Add and Edit modes.</summary>
    All = Add | Edit
}
```

- 不含 View——瀏覽模式永遠不可編輯，是不變式而非設定項。
- `[Flags]` + 複數結尾符合 S2342。
- 搭配擴充方法 `FormEditModesExtensions`：

```csharp
/// <summary>
/// Determines whether editing is allowed in the specified form mode.
/// </summary>
public static bool Allows(this FormEditModes modes, SingleFormMode formMode)
    => formMode switch
    {
        SingleFormMode.Add => modes.HasFlag(FormEditModes.Add),
        SingleFormMode.Edit => modes.HasFlag(FormEditModes.Edit),
        _ => false,   // View is never editable.
    };
```

### 2. `LayoutField.AllowEditModes`

```csharp
[Category(PropertyCategories.Appearance)]
[XmlAttribute]
[Description("Form modes in which this field is editable.")]
[DefaultValue(FormEditModes.All)]
public FormEditModes AllowEditModes { get; set; } = FormEditModes.All;
```

- 放在 `LayoutField`（master 欄位），**不**放 `LayoutFieldBase`——grid 欄位（`LayoutColumn`）的可編輯性由 `LayoutGrid.AllowEditModes` 整體控制，欄位級維持既有 `ReadOnly`。
- 與既有 `ReadOnly` 的關係：**AND 合成**，`ReadOnly = true` 為任何模式強制唯讀（保留、不棄用）。最終可編輯判定：

  ```
  editable = formMode != View && !ReadOnly && AllowEditModes.Allows(formMode)
  ```

### 3. `LayoutGrid.AllowEditModes`

```csharp
[XmlAttribute]
[Description("Form modes in which this grid allows editing actions.")]
[DefaultValue(FormEditModes.All)]
public FormEditModes AllowEditModes { get; set; } = FormEditModes.All;
```

- 與 `AllowActions` 正交：`AllowActions` 決定「允許哪些動作」（Add/Edit/Delete row），`AllowEditModes` 決定「哪些表單模式下允許編輯」。

### 序列化相容性

- `FormLayout` 走 XML 持久化（`IObjectSerializeFile`）。`XmlSerializer` 對 `[Flags]` enum 以空白分隔序列化（如 `AllowEditModes="Add"`），`[DefaultValue(All)]` 讓預設值不落檔——既有 layout XML 完全不受影響，舊檔反序列化取得預設 `All`，行為與現狀一致。

## Avalonia 端：欄位編輯器（階段 2）

### `FieldEditorBinder`

新增單一合成入口，取代各編輯器自行組合 `formMode` 與 `IsLayoutReadOnly`：

```csharp
/// <summary>
/// Determines whether the bound field is editable in the specified form mode,
/// combining the mode, the layout read-only flag and the field's AllowEditModes.
/// </summary>
public bool AllowsEdit(SingleFormMode formMode)
    => formMode != SingleFormMode.View
        && !IsLayoutReadOnly
        && (LayoutField is not LayoutField field || field.AllowEditModes.Allows(formMode));
```

- 列綁定（`LayoutColumn`，RowEditPanel/Dialog 用）沒有 `AllowEditModes` → 視為 `All`，行為不變。

### 各編輯器 `SetControlState`

| 編輯器 | 改後 |
|--------|------|
| TextEdit（含 MemoEdit / ButtonEdit 繼承） | `IsReadOnly = !_binder.AllowsEdit(formMode);` |
| DateEdit / DropDownEdit / CheckEdit | `IsEnabled = _binder.AllowsEdit(formMode);` |

- `ApplyMetadata` 維持現狀（mode-invariant 的 layout `ReadOnly`）；`BindCore` 末尾本就會以 ambient 模式補一次 `SetControlState`，新規則在綁定當下即生效。
- ButtonEdit 的 lookup 按鈕已 hook `IsReadOnlyProperty`，自動跟隨，無需改動。

## Avalonia 端：GridControl（階段 3）

前置 plan 完成後，GridControl 已具備 `AllowEdit` 屬性與內建工具列，本階段只剩：

- `SetControlState(formMode)` 的映射由 `AllowEdit = formMode != View` 改為：

  ```csharp
  AllowEdit = formMode != SingleFormMode.View
      && (_layout?.AllowEditModes.Allows(formMode) ?? false);
  ```

- 工具列顯示、in-cell 編輯、double-tap 等行為不需再動——全部跟隨 `AllowEdit`。

### 不在本次範圍

- **Bee.UI.Maui**：MAUI 端 `DynamicForm` 目前僅以 `ReadOnly` 渲染、未實作 `SetControlState` 管線，後續另案同步。
- **LayoutColumn 級的 per-mode 設定**：grid 欄位維持 `ReadOnly`，per-mode 控制收在 grid 層。
- **棄用 `ReadOnly`**：保留並與新屬性 AND 合成，不做 breaking change。

## 測試

### Bee.Definition.UnitTests（階段 1）

- `LayoutFieldTests` / `LayoutGridTests`：預設值斷言補 `AllowEditModes == All`。
- `FormEditModesExtensionsTests`（新檔）：`Allows` 真值表（View 永遠 false；Add/Edit 對應 flag；None 全 false；All 對 Add/Edit true）。
- XML round-trip：非預設值（如 `Add`）序列化後可還原；預設 `All` 不落 XML（沿用 `DefinitionSerializationTests` 慣例）。

### Bee.UI.Avalonia.UnitTests（階段 2）

- `TextEditTests`：綁定 `AllowEditModes = Add` 的 `LayoutField` 後，`SetControlState(Add)` 可編輯、`SetControlState(Edit)` 唯讀、`SetControlState(View)` 唯讀。
- ButtonEdit：同情境下內嵌按鈕 `IsEnabled` 跟隨。
- DropDownEdit / DateEdit / CheckEdit：同情境斷言 `IsEnabled`。

### Bee.UI.Avalonia.UnitTests（階段 3）

- `GridControlTests`：
  - `AllowEditModes = Add` 時 `SetControlState(Edit)` → `AllowEdit == false`；`SetControlState(Add)` → `AllowEdit == true`。
  - `AllowEditModes = None` 時任何模式 `AllowEdit == false`。

## 執行順序

0. 前置：完成 [plan-avalonia-gridcontrol-toolbar.md](plan-avalonia-gridcontrol-toolbar.md)。
1. 階段 1：`FormEditModes` + 擴充方法 → `LayoutField` / `LayoutGrid` 屬性 → Definition 測試 → build + test。
2. 階段 2：`FieldEditorBinder.AllowsEdit` → 各編輯器 `SetControlState` → 編輯器測試 → build + test。
3. 階段 3：`GridControl.SetControlState` 納入 `AllowEditModes` → 測試 → build + test。
4. 每階段獨立 commit；全套通過後 push main。
