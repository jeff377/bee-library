# 定義編輯器：把宣告式 metadata 接回來

**狀態：📝 擬定中（2026-08-07）**

> 本 plan **不在 2026-08-07 框架體檢的處理範圍內**，待體檢項目處理完畢後另行排程。

定義類別上有 **723 處宣告式編輯器 metadata**（`[TreeNode]` 71 + `[Description]` 312 +
`[Category]` 115 + `[Browsable]` 68 + `[TypeConverter]` 13 + `[DefaultValue]` 144），
其中 **579 處純編輯器用途、零消費端**。本 plan 把它們接回實際的編輯器。

來源：[plan-framework-review-2026-08-07.md](plan-framework-review-2026-08-07.md) 的 D-3 / D-5。
**該體檢不處理本議題，僅標記移交本 plan。**

---

## 背景：為什麼會斷線

歷史工作模式是 **TreeView（結構）+ PropertyGrid（屬性）** 雙控件，兩邊都由 metadata 驅動，
手寫量接近零；結構異動走每定義型別專屬的右鍵選單類別與拖曳類別；最後以 XML 持久化。

移植 Avalonia 時：
- TreeView **有**內建控件 → 結構那半的標註留著，但沒有 builder
- PropertyGrid **無**內建控件 → 屬性那半整個改為手寫面板

代價是 `tools/DefineEditor` 的 **57 個 DataTemplate + 141 個手寫欄位繫結**
（109 TextBox / 16 ComboBox / 14 CheckBox / 2 NumericUpDown），約 1,800 行 view XAML，
且與 579 處標註完全脫節——改 `[Category]` 不影響畫面，加新屬性要手工補 TextBox。

`TreeNodeAttribute.GetDisplayText` 用 `TypeDescriptor` 而非一般反射，也是因為它本就是
`System.ComponentModel`（PropertyGrid）那一套的一部分，13 處 `[TypeConverter]` 要靠它才生效。

---

## 分層

| 層 | 位置 | 職責 |
|----|------|------|
| 通用建樹 | `src/Bee.Base/Tree/` | `TreeNodeAttribute` 驅動 → UI 無關的 `ObjectTreeNode` |
| 命令 / 拖曳抽象 | `src/Bee.Base/Tree/` | `ITreeNodeCommandProvider`、`ITreeNodeDragDropHandler`（UI 無關） |
| Avalonia 控件 | `src/Bee.UI.Avalonia/Controls/` | `TreeViewBuilder`、`PropertyGridControl` |
| 各型別 provider 實作 | 消費端（現為 DefineEditor） | 每定義型別一個選單類別 / 拖曳類別 |
| 未來其他 head | 如 `Bee.UI.DevExpress` | 各寫薄 builder |

**PropertyGrid 只有 Avalonia 需要自建。** DevExpress 與 WinForms 的 PropertyGrid 原生吃
`TypeDescriptor`，設 `SelectedObject` 即可——那 579 處標註在那些 head 上是零工作量。
因此 PropertyGrid **不需要** UI 無關的中介模型，只有 TreeView 需要（各家節點模型不同）。

**每型別的 provider 實作先留在消費端**，不預先上收到框架。依 `code-style.md`
「不為假設的未來建類」——介面定好，第二個 head 出現時再上收，成本低。

---

## 階段

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 1 | **`PropertyGridControl`（Avalonia）** | 📝 擬定中 |
| 2 | `Bee.Base/Tree/` 建樹核心 + 循環防護 + 測試 | 📝 擬定中 |
| 3 | 補 23 個無參數 `[TreeNode]` 的標籤標註 | 📝 擬定中 |
| 4 | `TreeViewBuilder`（Avalonia），消除 11 處重複 XAML | 📝 擬定中 |
| 5 | 命令 provider 介面 + DefineEditor 右鍵選單遷移 | 📝 擬定中 |
| 6 | 拖曳 handler 介面 + 實作（目前**完全無**拖曳能力） | 📝 擬定中 |
| 7 | 在地化 hook（標籤走 `LanguageResource`） | 📝 擬定中 |

> 階段 1 先行的理由：PropertyGrid 是四個部件裡唯一「沒有它就沒有替代路徑」的一個，
> 且可獨立驗證（拿任一定義物件當 `SelectedObject` 就看得出成效），不相依於建樹核心。

---

## 階段 1：`PropertyGridControl`

### 位置與命名

`src/Bee.UI.Avalonia/Controls/PropertyGridControl.cs`

命名沿用 `GridControl` 前例加 `Control` 後綴——避開 WinForms `System.Windows.Forms.PropertyGrid`
與 DevExpress `PropertyGridControl` 撞名（`code-style.md` 的「跨 UI 消費型別避開 UI 框架型別名」）。

### 公開表面

```csharp
public class PropertyGridControl : ContentControl
{
    public object? SelectedObject { get; set; }   // 要編輯的物件
    public bool ShowDescription { get; set; }     // 底部說明列，預設 true
    public bool ShowCategories { get; set; }      // 依 [Category] 分組，預設 true
    public bool IsReadOnly { get; set; }          // 整體唯讀
    public event EventHandler<PropertyValueChangedEventArgs>? PropertyValueChanged;
}
```

複合控件（非繼承內建控件），比照 `GridControl : ContentControl`，內部視覺樹以程式碼建構。

### metadata 對映

| 來源 | 行為 |
|------|------|
| `TypeDescriptor.GetProperties(obj)` | 屬性列舉（**非**一般反射，`[TypeConverter]` 才生效） |
| `[Browsable(false)]` | 不顯示 |
| `[Category("...")]` | 分組標頭；無標註歸入 `Misc` |
| `[Description("...")]` | 底部說明列（隨選取屬性更新） |
| `PropertyDescriptor.IsReadOnly` | 該列唯讀 |
| `PropertyDescriptor.ShouldSerializeValue` | 值 ≠ `[DefaultValue]` 時標粗體 |
| `PropertyDescriptor.CanResetValue` / `ResetValue` | 右鍵「重設為預設值」 |
| `TypeConverter.GetStandardValues` | 有標準值 → ComboBox |

### 編輯器選擇

| 屬性型別 | 控件 |
|---|---|
| `bool` | `CheckBox` |
| `enum` | `ComboBox`（`Enum.GetValues`） |
| 數值（int/long/short/byte/decimal/double/float） | `NumericUpDown` |
| `DateTime` / `DateOnly` | `DatePicker` |
| 有 `GetStandardValues` 的 `TypeConverter` | `ComboBox` |
| 其餘 | `TextBox`（經 `TypeConverter` 做字串往返） |

### 踩雷誌適用條款（`docs/repo-ops/gotchas/avalonia-controls.md`）

- **#2**：說明列 `Border` 與可點區域必須設背景，`Background = null` 不參與 hit-test
- **#3**：Semi 的 `ComboBox` / `DatePicker` 預設不 stretch → 建立時設 `HorizontalAlignment = Stretch`
- **#6**：寫回值不依賴 `TextChanged` 這類語意事件（程式設值不觸發）；`TextBox` 於 `LostFocus`
  提交（與 `TextEdit` 一致），`CheckBox` / `ComboBox` / `NumericUpDown` 監聽 `PropertyChanged`
  比對對應的 `AvaloniaProperty`
- **#4**：本控件不繼承內建控件，故不需 `StyleKeyOverride`；但內部若對內建控件建子類則需要
- **#15/16**：`Bee.UI.Avalonia.UnitTests` 已有 `DisableTestParallelization`，新增控件測試不需另外處理

樣式一律用 Semi token（`SemiColorText0-3` / `SemiColorBackground0-4` / `SemiColorBorder`），
不自建配色（`rules/avalonia.md`）。

### 實作參考

遇到問題時可參考 [Avalonia.PropertyGrid](https://github.com/bodong1987/Avalonia.PropertyGrid)
（bodong1987）的作法，但**以目前編輯定義的需求為主**，不照搬其完整功能面。
該專案是獨立實作，本 plan 不引入其套件相依。

### 不做的事（範圍外）

- **不遷移 DefineEditor 的既有面板**。階段 1 只交付控件本身 + DemoCenter 展示；
  遷移是後續獨立工項，且預期只能吃掉簡單型別那部分——`MappingGroupEditor` 這類
  複合/跨欄位驗證面板仍需保留客製。
- 不做巢狀物件展開（`ExpandableObjectConverter`）——留待實際需要時再加。

### 驗收

- [ ] 以 `FormField` / `DbField` / `SystemSettings` 當 `SelectedObject`，屬性依 `[Category]` 正確分組
- [ ] `[Browsable(false)]` 屬性不出現
- [ ] 選取屬性時說明列顯示 `[Description]` 內容
- [ ] 值 ≠ `[DefaultValue]` 時標粗體；右鍵可重設
- [ ] enum 屬性顯示為 ComboBox 且可寫回
- [ ] `IsReadOnly = true` 時全部編輯器唯讀
- [ ] `dotnet build -c Release --no-incremental` 綠燈
- [ ] 新增公開型別已進 `PublicAPI.Unshipped.txt`
- [ ] 單元測試：屬性列舉、分組、Browsable 過濾、值寫回、重設

---

## 已驗證的現況（供後續階段參考）

### `[TreeNode]` 標註模型是連貫的

```
FormLayout                              [TreeNode]
 ├─ LayoutSectionCollection             [TreeNode("Sections", false)]
 │   └─ LayoutSection                   [TreeNode]
 │       └─ LayoutFieldCollection       [TreeNode("Fields", false)]
 │           └─ LayoutField             [TreeNode]
 └─ LayoutGridCollection                [TreeNode("Details", false)]
     └─ LayoutGrid                      [TreeNode]
         └─ LayoutColumnCollection      [TreeNode("Columns", false)]
             └─ LayoutColumn            [TreeNode]
```

`CollectionFolder` 的 true/false 是刻意區分：`FormFieldCollection` = `true`（`FormTable` 下
同時有 Fields 與 Rules，需資料夾區分）、`LayoutFieldCollection` = `false`（`LayoutSection`
只有 Fields，直接掛）。

### 循環防護已由標註處理，但有兩處缺口

7 個 `[TreeNodeIgnore]` **全部**標在反向導航屬性上（`CollectionItem.Collection`、
`KeyCollectionItem.Collection`、`MessagePackCollectionItem.Collection`、
`MessagePackKeyCollectionItem.Collection`、`FormRule.Schema`、`FormField.Table`、
`FormSchema.MasterTable`）。

**未標但需防護**：`CollectionBase.Owner`、`Tag`（`object?`，可指回任何東西）。
階段 2 的 builder 需自帶 visited set + `MaxDepth`，不可只靠標註。

### 23 個無參數 `[TreeNode]` 目前產不出可讀標籤

`GetDisplayText` 的 fallback 鏈是 `DisplayFormat` → `IDisplayName` → `ToString()`。實測：
零個標註用 `(displayFormat, propertyName)` 建構子、`CollectionItem` 無 `DisplayName` 也未覆寫
`ToString()`、全 repo 無型別實作 `IDisplayName`。故節點目前會顯示型別全名。

機制已完備：`GetDisplayText` 對 `PropertyName` 有做 `Split(",")`，
故 `[TreeNode("{0} — {1}", "FieldName,Caption")]` 今天就能運作，不需改 attribute。
標籤欄位可對照 `tools/DefineEditor/Models/FormSchemaNodeDisplay.cs`——它是實際跑過的答案。

### 相依現況

- `Bee.UI.Avalonia` 已 ProjectReference `Bee.Definition`，加控件無新增相依
- `tools/DefineEditor/Bee.DefineEditor.csproj` 目前只參照 `Bee.Definition` + `Bee.Base`，
  **未**參照 `Bee.UI.Avalonia`；階段 4/5 若要 DefineEditor 使用框架控件需先加此參照
- 拖曳：`DragDrop` / `AllowDrop` / `DragOver` 全 repo **零命中**
