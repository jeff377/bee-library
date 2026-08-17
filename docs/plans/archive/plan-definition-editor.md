# 定義編輯器：把宣告式 metadata 接回來

**狀態：✅ 已拆分（2026-08-17）**

> **本 plan 已被兩份獨立 plan 取代，內容不再維護。**
> TreeView 與 PropertyGrid 兩個控件互不相依（前者吃 `TreeNodeAttribute`、
> 後者吃 `PropertyDescriptor`，無共用程式碼），拆開後可並行推進：
>
> - `plan-property-grid-control.md`
> - `plan-tree-view-builder.md`
>
> 共用背景與決策（分層、控件歸屬、相依、版本線）已各自複寫進兩份 plan，
> **以那兩份為準**。本文件僅作為拆分前的歷史紀錄保留。

定義類別上有大量宣告式編輯器 metadata（`[TreeNode]` / `[Description]` / `[Category]` /
`[Browsable]` / `[TypeConverter]` / `[DefaultValue]`），**絕大多數純編輯器用途、零消費端**。
本 plan 把它們接回實際的編輯器。

> 2026-08-17 量測：`[TreeNode]` 64、`[Description]` 311、`[Category]` 114、`[Browsable]` 68、
> `[TypeConverter]` 13、`[DefaultValue]` 144，合計 714。**此為當時快照，不是需要維護的數字** ——
> 判斷規模用，勿據以推導任何行為。

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
且與這些標註完全脫節——改 `[Category]` 不影響畫面，加新屬性要手工補 TextBox。

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
`TypeDescriptor`，設 `SelectedObject` 即可——那些標註在那些 head 上是零工作量。
因此 PropertyGrid **不需要** UI 無關的中介模型，只有 TreeView 需要（各家節點模型不同）。

**每型別的 provider 實作先留在消費端**，不預先上收到框架。依 `code-style.md`
「不為假設的未來建類」——介面定好，第二個 head 出現時再上收，成本低。

### 控件歸屬與相依（已決定，2026-08-17）

`PropertyGridControl` 與 `TreeViewBuilder` **放 `src/Bee.UI.Avalonia`**，不另立組件。

`Bee.UI.Avalonia` 的 ProjectReference 是 `Bee.UI.Core` + `Bee.Api.Client` + `Bee.Definition`
+ `Bee.Expressions`（→ DynamicExpresso）。DefineEditor 目前只參照 `Bee.Definition` +
`Bee.Base`，階段 3/4 起會接受整條相依鏈。**這兩條主要相依都是 DefineEditor 自己要用的，
不是附帶成本**：

- **`Bee.Api.Client`** —— DefineEditor 規劃支援**近端／遠端雙模**，經 Connector 存取定義。
  框架端已備妥：`ConnectType` 的 `Local` / `Remote`、
  [`ClientDefineAccess`](../../../src/Bee.Api.Client/ClientDefineAccess.cs) 已有全套
  `GetXxxAsync` / `SaveXxxAsync`（FormSchema / FormLayout / TableSchema / Language / 各 Settings），
  走 `SystemApiConnector`。屆時現行的檔案 IO 成為 Local 分支。
  **這是獨立於本 plan 的工項**（牽動存取層、連線設定 UI、存檔語意與 dirty tracking），
  應另立 plan；本節只記錄「此相依是刻意的」。
- **`Bee.Expressions`** —— 定義本身帶運算式，編輯器應在**設計期**驗證其正確性。
  帶運算式的屬性有四個：`FormField.ValueExpression`、`FormField.DefaultValueExpression`、
  `FormRule.When`、`FormRule.Condition`。
  接縫是 `IExpressionEvaluator.GetReferencedVariables(expression)`：**parse-only、不求值、
  不需資料**，parse 失敗擲 `ExpressionEvaluationException`，成功則回傳引用的識別字清單。
  兩項檢查：(1) 語法是否 parse 得過；(2) 引用的識別字是否都對得上該 `FormSchema` 宣告的
  `FieldName` —— **必須 Ordinal 比對**，因為 DynamicExpresso 識別字區分大小寫
  （見 `rules/serialization.md`）。此檢查能把現在只會在客戶端執行期浮現的
  `UnknownIdentifierException` / `FormLiveComputation.IsDegraded` 提前到編輯當下。
  **回傳型別驗不到**（`When` / `Condition` 應為 `bool`、`ValueExpression` 應為欄位型別）——
  那需要真的 `Evaluate` 一次，投入層級不同，留待需要時再做。

另有**版本線**問題：`tools/DefineEditor` 是 Avalonia 12.0.4 / Semi.Avalonia 12.0.3，
`Bee.UI.Avalonia` 是 Avalonia 12.0.0 且不引 Semi。NuGet min-version 語意下 restore 過得去，
但 `rules/avalonia.md` 明講「restore 過不代表主題相容」。**階段 3 動工前先把兩邊版本線對齊
並實際 runtime 驗證主題不跑版**，不要靠 restore 綠燈當證據。

---

## 階段

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 1 | **`PropertyGridControl`（Avalonia）** | 📝 擬定中 |
| 2 | `Bee.Base/Tree/` 建樹核心 + 循環防護 + 測試 | 📝 擬定中 |
| 3 | `TreeViewBuilder`（Avalonia）+ 同時補無參數 `[TreeNode]` 的標籤標註 | 📝 擬定中 |
| 4 | 命令 provider 介面 + DefineEditor 右鍵選單遷移 | 📝 擬定中 |
| 5 | 拖曳 handler 介面 + 實作（目前**完全無**拖曳能力） | 📝 擬定中 |
| 6 | 在地化實作（`LabelTranslator` 接上語言來源） | 📝 擬定中 |

> 階段 1 先行的理由：PropertyGrid 是四個部件裡唯一「沒有它就沒有替代路徑」的一個，
> 且可獨立驗證（拿任一定義物件當 `SelectedObject` 就看得出成效），不相依於建樹核心。
>
> 標籤標註併入階段 3 的理由：補 `[TreeNode]` 標籤要有 TreeView 才驗得出效果，
> 單獨成階段等於盲改。改標註 → 立刻在畫面上看到，是同一輪工作。

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
    public Func<string, string>? LabelTranslator { get; set; }  // 在地化接縫，見下
    public event EventHandler<PropertyValueChangedEventArgs>? PropertyValueChanged;
}
```

複合控件（非繼承內建控件），比照 `GridControl : ContentControl`，內部視覺樹以程式碼建構。

### 與既有 `Controls/Editors/` 的關係：自建，不動 `IFieldEditor`（已決定，2026-08-17）

`src/Bee.UI.Avalonia/Controls/Editors/` 已有一整套 field editor（`IFieldEditor`、
`FieldEditorFactory`、`TextEdit` / `CheckEdit` / `NumericEdit` / `DateEdit` / `DropDownEdit`
/ `MemoEdit` / `TimeEdit`）與 `RowEditPanel`（label + 編輯器成列版面），下面「編輯器選擇」
那張表看起來像它的重寫版。**不重用，理由是驅動來源不同**：既有那套由 `LayoutField` /
`ControlType` / `FormDataObject`（`DataRow`）驅動，PropertyGrid 由 `PropertyDescriptor` /
CLR 物件驅動；要共用就得擴充 `IFieldEditor`（已發佈的公開表面）或加一組平行 bind 多載，
牽動既有 FormView 路徑。

PropertyGrid 的消費端是 DefineEditor（維護者工具），**外觀與終端表單一致不是需求**，
為此改動已發佈介面代價不對等。因此 `PropertyGridControl` 內部直接建原生控件，
只把「提交時機 + stretch 設定」抽成 **internal** 小工具（對應 gotcha #3 / #6）。

> 這代表 gotcha #3 / #6 要在本控件重新處理一次，不是免費繼承——寫的時候別漏。

### 在地化接縫（形狀在階段 1 定，實作留階段 6）

`[Description]` / `[Category]` 是編譯期常數字串，要走 `LanguageResource` 需要 key 對映。
目前是兩套並存：DefineEditor 有自己的 `../../../tools/DefineEditor/Services/LocalizationService.cs`
（resx + `LocExtension`），框架有 `../../../src/Bee.Definition/Language/LanguageResource.cs`。
**接哪一套留到階段 6 決定**，但**接縫形狀必須在階段 1 定死**——控件一旦進
`PublicAPI.Shipped.txt`，階段 6 再加參數就是破壞性變更。

採 `Func<string, string>? LabelTranslator`：階段 1 不設即原樣顯示，階段 6 由消費端注入。
不吃 `IStringLocalizer`，避免 `Bee.UI.Avalonia` 為此長出新相依。

### metadata 對映

| 來源 | 行為 |
|------|------|
| `TypeDescriptor.GetProperties(obj)` | 屬性列舉（**非**一般反射，`[TypeConverter]` 才生效） |
| `[Browsable(false)]` | 不顯示 |
| `[Category("...")]` | 分組標頭；無標註歸入 `Misc` |
| `[Description("...")]` | 底部說明列（隨選取屬性更新） |
| `PropertyDescriptor.IsReadOnly` | 該列唯讀 |
| `PropertyDescriptor.ShouldSerializeValue` | 值 ≠ 預設值時標粗體，**但僅限 `CanResetValue` 為 true 的屬性**（見下） |
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

### 粗體規則不能只看 `ShouldSerializeValue`

`PropertyDescriptor.ShouldSerializeValue` 在「既無 `ShouldSerializeXxx` 方法、也無
`[DefaultValue]`」時**恆回 `true`**，那些屬性會永遠粗體、等於此功能失效。
`[DefaultValue]` 覆蓋率差異很大：`FormField` 幾乎全覆蓋（24 個標註 / 23 個屬性），
但 `SystemSettings` 只有 1 / 8 —— 直接照用會讓它有 7 個屬性恆粗體。

→ 條件為 **`CanResetValue(obj) && ShouldSerializeValue(obj)`**；`CanResetValue` 為 false
的屬性一律不套粗體判定（也對應「右鍵重設」在該列應 disabled）。

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
- [ ] 有 `[DefaultValue]` 且現值 ≠ 預設值時標粗體；右鍵可重設。以 `SystemSettings` 驗證
      「無 `[DefaultValue]` 的屬性不得恆粗體」
- [ ] enum 屬性顯示為 ComboBox 且可寫回
- [ ] `IsReadOnly = true` 時全部編輯器唯讀
- [ ] `LabelTranslator` 未設時原樣顯示；設一個大寫轉換函式可看到分組標頭與屬性名改變
- [ ] `dotnet build -c Release --no-incremental` 綠燈
- [ ] 新增公開型別已進 `PublicAPI.Unshipped.txt`
- [ ] 單元測試：屬性列舉、分組、Browsable 過濾、值寫回、重設、`LabelTranslator` 套用

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

`[TreeNodeIgnore]` **全部**標在反向導航屬性上：`CollectionItem.Collection`、
`KeyCollectionItem.Collection`、`FormRule.Schema`、`FormField.Table`、
`FormSchema.MasterTable`（2026-08-17 核對，共 5 處）。

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
  **未**參照 `Bee.UI.Avalonia`；階段 3/4 若要 DefineEditor 使用框架控件需先加此參照
- 拖曳：`DragDrop` / `AllowDrop` / `DragOver` 全 repo **零命中**
