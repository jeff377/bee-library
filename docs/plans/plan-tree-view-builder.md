# TreeViewBuilder：用 `[TreeNode]` 標註驅動結構樹

**狀態：📝 擬定中（2026-08-17）**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 1 | `src/Bee.Base/Tree/` 建樹核心 + 循環防護 + 測試 | 📝 擬定中 |
| 2 | `TreeViewBuilder`（Avalonia）+ 補無參數 `[TreeNode]` 的標籤標註 | 📝 擬定中 |
| 3 | 命令 provider 介面 + DefineEditor 右鍵選單遷移 | 📝 擬定中 |
| 4 | 拖曳 handler 介面 + 實作（目前**完全無**拖曳能力） | 📝 擬定中 |
| 5 | 在地化實作（節點標籤接上語言來源） | 📝 擬定中 |

定義類別上有 `[TreeNode]` / `[TreeNodeIgnore]` 標註，描述物件如何呈現為結構樹節點，
但**沒有任何 builder 消費它們**。本 plan 把它們接回實際的 TreeView。

> 2026-08-17 量測：`[TreeNode]` 64（其中無參數 23、帶參數 41）、`[TreeNodeIgnore]` 5。
> **此為當時快照，不是需要維護的數字** —— 判斷規模用，勿據以推導任何行為。

來源：2026-08-07 框架體檢的 D-3 / D-5（該體檢已過期封存，本 plan 的排程不綁定它）。
PropertyGrid 那半由 [plan-property-grid-control.md](plan-property-grid-control.md) 承接——兩者
**無共用程式碼**（本 plan 吃 `TreeNodeAttribute`，那份吃 `PropertyDescriptor`），可並行推進。

---

## 背景：為什麼會斷線

歷史工作模式是 **TreeView（結構）+ PropertyGrid（屬性）** 雙控件，兩邊都由 metadata 驅動，
手寫量接近零；結構異動走每定義型別專屬的右鍵選單類別與拖曳類別；最後以 XML 持久化。

移植 Avalonia 時 TreeView **有**內建控件，所以結構那半的標註留著、但沒有 builder ——
`tools/DefineEditor` 改為每個文件型別各自手寫 `DataTemplate` 與節點模型
（`Models/DefineNode.cs`、`Models/FormSchemaNodeBuilder.cs`、`Models/SettingsTreeNode.cs`），
與標註完全脫節。

`TreeNodeAttribute.GetDisplayText` 用 `TypeDescriptor` 而非一般反射，是因為它本就是
`System.ComponentModel` 那一套的一部分，`[TypeConverter]` 要靠它才生效。

---

## 分層

| 層 | 位置 | 職責 |
|----|------|------|
| 通用建樹 | `src/Bee.Base/Tree/` | `TreeNodeAttribute` 驅動 → UI 無關的 `ObjectTreeNode` |
| 命令 / 拖曳抽象 | `src/Bee.Base/Tree/` | `ITreeNodeCommandProvider`、`ITreeNodeDragDropHandler`（UI 無關） |
| Avalonia 控件 | `src/Bee.UI.Avalonia/Controls/` | `TreeViewBuilder` |
| 各型別 provider 實作 | 消費端（現為 DefineEditor） | 每定義型別一個選單類別 / 拖曳類別 |
| 未來其他 head | 如 `Bee.UI.DevExpress` | 各寫薄 builder |

**TreeView 需要 UI 無關的中介模型**（`ObjectTreeNode`），因為各 UI 家族的節點模型不同；
這與 PropertyGrid 相反——那邊 DevExpress / WinForms 原生吃 `TypeDescriptor`，不需中介層。

**每型別的 provider 實作先留在消費端**，不預先上收到框架。依 `code-style.md`
「不為假設的未來建類」——介面定好，第二個 head 出現時再上收，成本低。

### 控件歸屬與相依（已決定，2026-08-17）

`TreeViewBuilder` **放 `src/Bee.UI.Avalonia/Controls/`**，不另立組件。

`Bee.UI.Avalonia` 的 ProjectReference 是 `Bee.UI.Core` + `Bee.Api.Client` + `Bee.Definition`
+ `Bee.Expressions`（→ DynamicExpresso）。DefineEditor 目前只參照 `Bee.Definition` +
`Bee.Base`，階段 2/3 起會接受整條相依鏈。**這兩條主要相依都是 DefineEditor 自己要用的，
不是附帶成本**：

- **`Bee.Api.Client`** —— DefineEditor 規劃支援**近端／遠端雙模**，經 Connector 存取定義。
  框架端已備妥：`ConnectType` 的 `Local` / `Remote`、
  [`ClientDefineAccess`](../../src/Bee.Api.Client/ClientDefineAccess.cs) 已有全套
  `GetXxxAsync` / `SaveXxxAsync`（FormSchema / FormLayout / TableSchema / Language / 各 Settings），
  走 `SystemApiConnector`。屆時現行的檔案 IO 成為 Local 分支。
  **這是獨立於本 plan 的工項**（牽動存取層、連線設定 UI、存檔語意與 dirty tracking），
  應另立 plan；本節只記錄「此相依是刻意的」。
- **`Bee.Expressions`** —— 定義本身帶運算式，編輯器應在**設計期**驗證其正確性。
  帶運算式的屬性有四個：`FormField.ValueExpression`、`FormField.DefaultValueExpression`、
  `FormRule.When`、`FormRule.Condition`。接縫是
  `IExpressionEvaluator.GetReferencedVariables(expression)`：**parse-only、不求值、不需資料**，
  parse 失敗擲 `ExpressionEvaluationException`，成功則回傳引用的識別字清單。
  兩項檢查：(1) 語法是否 parse 得過；(2) 引用的識別字是否都對得上該 `FormSchema` 宣告的
  `FieldName` —— **必須 Ordinal 比對**，因為 DynamicExpresso 識別字區分大小寫
  （見 `rules/serialization.md`）。**回傳型別驗不到**，那需要真的 `Evaluate` 一次，
  投入層級不同，留待需要時再做。

另有**版本線**問題：`tools/DefineEditor` 是 Avalonia 12.0.4 / Semi.Avalonia 12.0.3，
`Bee.UI.Avalonia` 是 Avalonia 12.0.0 且不引 Semi。NuGet min-version 語意下 restore 過得去，
但 `rules/avalonia.md` 明講「restore 過不代表主題相容」。**階段 2 動工前先把兩邊版本線對齊
並實際 runtime 驗證主題不跑版**，不要靠 restore 綠燈當證據。

---

## 階段 1：`src/Bee.Base/Tree/` 建樹核心

輸出 UI 無關的 `ObjectTreeNode`（節點文字 + 對應物件 + 子節點），由 `TreeNodeAttribute` 驅動。

### 循環防護：不可只靠標註

`[TreeNodeIgnore]` **全部**標在反向導航屬性上：`CollectionItem.Collection`、
`KeyCollectionItem.Collection`、`FormRule.Schema`、`FormField.Table`、
`FormSchema.MasterTable`（2026-08-17 核對，共 5 處）。

**未標但需防護**（`object?`，可指回任何東西）：

| 型別 | 屬性 |
|---|---|
| `CollectionBase` | `Owner`、`Tag` |
| `KeyCollectionBase` | `Owner`、`Tag` |
| `CollectionItem` | `Tag` |
| `KeyCollectionItem` | `Tag` |

→ builder **必須自帶 visited set + `MaxDepth`**，標註只是額外剪枝。

### 驗收

- [ ] 以 `FormLayout` / `FormSchema` 建樹，節點層級與下方「標註模型」一致
- [ ] `CollectionFolder = true` 產生資料夾節點、`false` 直接掛子節點
- [ ] 人為建構含循環參照的物件圖（`Tag` 指回祖先），建樹不無限遞迴
- [ ] 超過 `MaxDepth` 時停止而非拋例外
- [ ] `dotnet build -c Release --no-incremental` 綠燈
- [ ] 新增公開型別已進 `PublicAPI.Unshipped.txt`

---

## 階段 2：`TreeViewBuilder` + 補標籤標註

Avalonia 端把 `ObjectTreeNode` 綁上內建 `TreeView`，取代 DefineEditor 現有的
11 個 `TreeDataTemplate`（2026-08-17 量測）與各自的節點模型。

**補標籤標註與 builder 同階段**的理由：補 `[TreeNode]` 標籤要有 TreeView 才驗得出效果，
單獨成階段等於盲改。改標註 → 立刻在畫面上看到，是同一輪工作。

### 23 個無參數 `[TreeNode]` 目前產不出可讀標籤

`GetDisplayText` 的 fallback 鏈是 `DisplayFormat` → `IDisplayName` → `ToString()`。實測：
零個標註用 `(displayFormat, propertyName)` 建構子、`CollectionItem` 無 `DisplayName` 也未覆寫
`ToString()`、全 repo 無型別實作 `IDisplayName`。故節點目前會顯示型別全名。

**機制已完備、不需改 attribute**：`GetDisplayText` 對 `PropertyName` 有做 `Split(",")`，
故 `[TreeNode("{0} — {1}", "FieldName,Caption")]` 今天就能運作。
標籤欄位可對照 `../../tools/DefineEditor/Models/FormSchemaNodeDisplay.cs`——它是實際跑過的答案。

### 在地化接縫（形狀在此定，實作留階段 5）

節點標籤的翻譯接縫比照 PropertyGrid 採 `Func<string, string>? LabelTranslator`，
**形狀必須在本階段定死**——控件一旦進 `PublicAPI.Shipped.txt`，階段 5 再加參數就是
破壞性變更。不吃 `IStringLocalizer`，避免 `Bee.UI.Avalonia` 為此長出新相依。

### 踩雷誌適用條款（`docs/repo-ops/gotchas/avalonia-controls.md`）

- **#4**：若對內建 `TreeView` / `TreeViewItem` 建子類，`StyleKeyOverride` 是必修課，
  不覆寫會**整顆隱形**
- **#15/16**：`Bee.UI.Avalonia.UnitTests` 已有 `DisableTestParallelization`，新增控件測試不需另外處理

樣式一律用 Semi token，不自建配色（`rules/avalonia.md`）。

### 驗收

- [ ] DefineEditor 的 `FormSchema` / `FormLayout` 樹以 builder 產出，畫面與現況等價
- [ ] 無參數 `[TreeNode]` 的型別不再顯示型別全名
- [ ] `LabelTranslator` 未設時原樣顯示
- [ ] `dotnet build -c Release --no-incremental` 綠燈

---

## 階段 3：命令 provider

`ITreeNodeCommandProvider`（UI 無關）+ DefineEditor 每定義型別一個實作，
取代現有的右鍵選單寫法。介面形狀待階段 2 落地後再定——需要先知道節點模型的實際樣貌。

---

## 階段 4：拖曳 handler

`ITreeNodeDragDropHandler`（UI 無關）+ Avalonia 端接 `DragDrop` / `AllowDrop` / `DragOver`。

> **目前全 repo 對這三者零命中**——這是新增能力，不是遷移。範圍與驗收待階段 3 後再定。

---

## 階段 5：在地化實作

決定 `LabelTranslator` 接哪個語言來源（DefineEditor 的 resx vs 框架 `LanguageResource`），
並在消費端注入。控件端**不需改動**——接縫在階段 2 已定死。

---

## 已驗證的現況

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

### 相依現況

- `Bee.UI.Avalonia` 已 ProjectReference `Bee.Definition`，加控件無新增相依
- `tools/DefineEditor/Bee.DefineEditor.csproj` 目前只參照 `Bee.Definition` + `Bee.Base`，
  **未**參照 `Bee.UI.Avalonia`；階段 2/3 若要 DefineEditor 使用框架控件需先加此參照
- 拖曳：`DragDrop` / `AllowDrop` / `DragOver` 全 repo **零命中**
