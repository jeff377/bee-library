# 計畫：DefineEditor 操作動作重新配置 — 按鈕列 → menu + context menu

**狀態：🚧 進行中（2026-06-08）**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 1 | 檔案層 commands（Save / Validate）搬到 macOS 頂端 File menu，動態綁定到 active document | 🚧 進行中 |
| 2 | 拿掉 9 個 DocumentView 的 Save / Validate 按鈕 | 📝 待做 |
| 3 | 每個 DocumentView 的 TreeView 加右鍵 context menu，依節點型別顯示適合的「新增 / 刪除」 | 📝 待做 |
| 4 | 拿掉 9 個 DocumentView 的 Add / Delete 按鈕列、整個 titlebar 工具列簡化或移除 | 📝 待做 |

## 背景

目前每個 DocumentView 的 titlebar 旁邊有 4–7 個按鈕（儲存、驗證、新增 X、新增 Y、刪除）。
按鈕數量隨 View 不同，視覺擁擠，且和 macOS / VS Code 的「menu + context menu」慣例不一致。

使用者要求：

- **檔案層級動作**（save / validate）→ 移到 application-level menu（macOS 頂端 File menu）
- **定義屬性層級動作**（新增 X、刪除）→ 改為樹狀節點的右鍵 context menu，依節點型別顯示對應項目
- **按鈕列整個拿掉**

## 現狀盤點

### 每個 DocumentView 的 commands（共 ~50 個）

| View | Save | Validate | 屬性層 commands |
|------|:----:|:--------:|----------------|
| FormSchemaDocumentView | ✓ | ✓ | AddTable, AddField, AddListItem, AddLookupMapping, AddRelationMapping, Delete |
| TableSchemaDocumentView | ✓ | ✓ | AddField, AddIndex, AddIndexField, Delete |
| FormLayoutDocumentView | ✓ | ✓ | AddSection, AddGrid, AddLayoutColumn, AddLayoutField, Delete |
| LanguageDocumentView | ✓ | ✓ | AddItem, AddEnum, AddEntry, Delete |
| PermissionModelsDocumentView | ✓ | ✓ | AddModel, AddRule, Delete |
| DatabaseSettingsDocumentView | ✓ | ✓ | AddServer, AddItem, Apply, Clear, Delete, Parse |
| DbCategorySettingsDocumentView | ✓ | ✓ | AddCategory, AddTable, Delete |
| ProgramSettingsDocumentView | ✓ | ✓ | AddCategory, AddProgram, Delete |
| SystemSettingsDocumentView | ✓ | ✓ | AddProperty, Delete |

### 共通介面

`DocumentViewModelBase` 沒有統一的 `SaveCommand` / `ValidateCommand` 抽象——每個衍生 ViewModel 各自定義同名 command。
階段 1 必須先在 base class 加抽象（或介面），才能讓 menu 透過 base 統一觸發。

## 階段 1：Save / Validate 上 File menu

### 1.1 DocumentViewModelBase 加抽象

```csharp
public abstract partial class DocumentViewModelBase : ObservableObject
{
    public abstract IRelayCommand SaveCommand { get; }
    public abstract IRelayCommand ValidateCommand { get; }
}
```

每個衍生類別把既有 `SaveCommand` / `ValidateCommand` 標 `override`（CommunityToolkit.Mvvm 從 `[RelayCommand]` 產生的屬性可改 partial override，視版本而定；若不行則手寫 wrapper）。

### 1.2 MainWindowViewModel 暴露 active 的對應 command

```csharp
public IRelayCommand? ActiveSaveCommand => ActiveDocument?.SaveCommand;
public IRelayCommand? ActiveValidateCommand => ActiveDocument?.ValidateCommand;

partial void OnActiveDocumentChanged(DocumentViewModelBase? value)
{
    // 已有的相對路徑屬性 + 新增兩個 command
    OnPropertyChanged(nameof(ActiveSaveCommand));
    OnPropertyChanged(nameof(ActiveValidateCommand));
}
```

### 1.3 App.ConfigureWindowMenu 在 File menu 加項目

```csharp
fileMenu.Menu.Add(new NativeMenuItem("Save") {
    Command = ProxySaveCommand,             // 觸發時轉發到 active document
    Gesture = new KeyGesture(Key.S, KeyModifiers.Meta),  // ⌘S
});
fileMenu.Menu.Add(new NativeMenuItem("Validate") {
    Command = ProxyValidateCommand,
    Gesture = new KeyGesture(Key.B, KeyModifiers.Meta),  // ⌘B（VS Code build 風）
});
```

App 層的 `ProxySaveCommand` 內部解析 `MainWindow.DataContext.ActiveDocument`，
有 active doc 才 Execute。`CanExecute` 跟著 active doc 切換時更新（subscribe `ActiveDocumentChanged`）。

### 1.4 預期效果

- 沒開檔時 File → Save / Validate disabled（灰）
- 開檔時 ⌘S 直接存當前 tab；⌘B 跑當前 tab 的 validation

## 階段 2：拿掉 Save / Validate 按鈕

9 個 DocumentView 的 titlebar WrapPanel 內，刪掉兩個對應的 `<Button ...SaveCommand>` / `<Button ...ValidateCommand>`。
其他 Add / Delete 按鈕暫留（階段 4 處理）。

## 階段 3：TreeView context menu

### 3.1 設計原則

每個 DocumentView 的 TreeView 節點型別不同：

- **FormSchemaDocumentView** 的節點是 `FormSchemaTreeNode`，有 `NodeKind`（Root / Table / Field / ListItem / Lookup / Relation）
- **TableSchemaDocumentView** 是 `TableSchemaTreeNode`（Root / Field / Index / IndexField）
- ...

每個節點型別應顯示的 menu items 不同，例如：

```
FormSchemaTreeNode.Root     → 新增 FormTable
FormSchemaTreeNode.Table    → 新增 FormField, 刪除
FormSchemaTreeNode.Field    → 新增 ListItem / Lookup / Relation, 刪除
FormSchemaTreeNode.ListItem → 刪除
```

### 3.2 實作方式：節點型別決定 menu

兩條路：

**A. XAML DataTemplate + Style triggers**：在 TreeView.ItemTemplate 內為每種 NodeKind 設不同 ContextMenu。
   優點：純宣告、可讀；缺點：XAML 較長。

**B. ViewModel-driven**：每個 tree node 暴露 `AvailableActions` 屬性（list of (label, command)），
   ContextMenu.ItemsSource 綁這個。
   優點：邏輯統一在 ViewModel；缺點：每個 ViewModel 要新增屬性，且 keyboard accelerator 需另設。

→ **採 A**：與既有 Avalonia code style 一致（TreeDataTemplate 已用宣告式），且每個 View 的節點型別有限，
   for loop 般枚舉清楚。

### 3.3 每個 View 的 context menu 清單

對每個 View 的每個節點型別列出 items（會在實作時細化）。先列大架構：

| View | 節點型別 | Context menu items |
|------|---------|--------------------|
| FormSchemaDocumentView | Root | 新增 FormTable |
| FormSchemaDocumentView | Table | 新增 FormField, 刪除 Table |
| FormSchemaDocumentView | Field | 新增 ListItem / Lookup / Relation, 刪除 Field |
| FormSchemaDocumentView | ListItem / Lookup / Relation | 刪除 |
| TableSchemaDocumentView | Root | 新增 FormField, 新增 Index |
| TableSchemaDocumentView | Field | 刪除 Field |
| TableSchemaDocumentView | Index | 新增 IndexField, 刪除 Index |
| TableSchemaDocumentView | IndexField | 刪除 |
| ...（其餘 7 個 View 類似）| | |

實作時逐 View 依其 `*TreeNode.NodeKind` 列舉節點型別，補完 menu 對應。

## 階段 4：拿掉 Add / Delete 按鈕列

當 context menu 替代了所有「新增 / 刪除」按鈕：

- 整個 titlebar `<WrapPanel Grid.Column="1">` 可拿掉，titlebar 只剩標題 + dirty indicator
- 視覺更乾淨；定義屬性編輯透過右鍵自然發現

特例：`DatabaseSettingsDocumentView` 的 `Apply` / `Clear` / `Parse` 不屬於樹狀節點操作，
而是 form 內按鈕。這部分**不在本次重構範圍**——它們是 form-level 動作，繼續用按鈕。

## 不在本次範圍

- About / Hide / Quit、Open Folder、Toggle Theme（已完成）
- DatabaseSettingsDocumentView form 內的 Apply / Clear / Parse 按鈕
- 多選操作（目前 TreeView 是單選）
- Keyboard shortcuts 給 context menu items（後續可加 `Gesture` 屬性）
- Windows / Linux 上的 menu（NativeMenu Application-level 在這兩個平台不顯示；後續若需要走視窗內 Menu fallback 另議）

## 風險與決策點

- **`[RelayCommand]` 是否能 override**：CommunityToolkit.Mvvm 11+ 對 `[RelayCommand]` 標的方法
  override 行為需測試。若無法 override，base class 用 `IRelayCommand` 抽象屬性，衍生類別手動實作
  getter 回傳 `[RelayCommand]` 產生的 command。實作階段 1 時驗證。
- **`CanExecute` 的更新**：active document 切換時，proxy command 需 raise `CanExecuteChanged`，
  否則 menu item 不會 enable/disable。階段 1 實作要記得處理。

## 估時與分塊提交

| 階段 | PR / commit | 約略改檔數 |
|------|------------|----------|
| 1 | 1 個 commit | App.axaml.cs + MainWindowViewModel + DocumentViewModelBase + 9 個衍生 VM |
| 2 | 1 個 commit | 9 個 axaml |
| 3 | 9 個 commit（一 View 一 commit）或 1 個大 commit | 9 個 axaml + 視情況 ViewModel |
| 4 | 1 個 commit | 9 個 axaml |

實作時建議：1 → 2 一起 commit（檔案層收尾）；3 → 4 一起 commit（屬性層收尾）。
