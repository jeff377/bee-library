# 計畫：DefineEditor tree node 統一 + FormSchemaDocumentViewModel 繼承 base

**狀態：🚧 進行中（2026-06-08）**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 1 | C14：合併 `FormSchemaTreeNode` → `SettingsTreeNode`；`FormSchemaNodeKind` enum 改字串常數 | ✅ 已完成（2026-06-08） |
| 2 | C15：`FormSchemaDocumentViewModel` 改繼承 `SingletonDocumentViewModelBase` | 🚧 進行中 |

## 背景

DefineEditor review 已先後完成 A（5 處 validator bug、smoke 假成功、CultureChanged 記憶體洩漏、emoji TabIcon）、B（死碼與雜訊清理）、C12/C13/C16/C17（重複收斂）。剩下 C14/C15 兩條規模較大、相互耦合的重構：

1. **`FormSchemaTreeNode` ≈ `SettingsTreeNode` 同構**：兩 class 的欄位、方法簽章幾乎一致，差別只在 `Kind` 一邊是 `FormSchemaNodeKind` enum、一邊是 string，以及 refresh 一邊靠 `FormSchemaNodeDisplay.For()` 直接 dispatch、一邊靠 `Refresher` delegate。
2. **`FormSchemaDocumentViewModel` 整套 Save / Validate / Delete / FilePath / StatusText 與 `SingletonDocumentViewModelBase` 重複實作**：它沒繼承 base 是因為 tree node 型別不同（FormSchemaTreeNode vs SettingsTreeNode）。一旦 C14 完成、兩個 tree node 合一，C15 就能讓 FormSchemaVM 也吃 base 的 plumbing。

合併後預期再砍 ~150 行重複，且未來 tree-based 編輯器一律收斂於同一條骨架。

## 既定決策

| 決策點 | 選定方案 | 理由 |
|--------|---------|------|
| Tree node 收斂方向 | 把 `FormSchemaTreeNode` 併入 `SettingsTreeNode`（保留後者名稱） | `SettingsTreeNode` 已有 8 個 caller、`Refresher` delegate 模式更彈性，逆向會動更多檔 |
| `FormSchemaNodeKind` enum 處置 | 改為字串常數 class `FormSchemaKinds`（位於 `Models/`） | 對齊 8 個 singleton VM 既有的 `KindRoot / KindCategory / ...` 字串常數慣例；避免 `Models → ViewModels` 反向依賴 |
| FormSchemaVM 繼承關係 | `: SingletonDocumentViewModelBase`（不是 `: DocumentViewModelBase`） | base 已涵蓋 Save / Validate / Delete / FilePath / SelectedTreeNode / SelectedKindCanDelete plumbing；FormSchemaVM 只剩自家 Add 命令與 SelectedKindIsX flags 要留 |
| `Title` / `DocumentKey` | 走 base ctor 計算 | base ctor `(filePath, "FormSchema", schema.ProgId)` 會算成 `"FormSchema — Employee"`，與現狀一致 |
| FormSchema 的 `Add` 命令分流 | 留在 FormSchemaVM 為 `[RelayCommand]`，base 只統一 Delete | Singleton 編輯器的 Add 是 root/category/leaf 三層；FormSchema 多了 RelationMapping/LookupMapping/ListItem 三條額外路徑，不合適泛化 |

## Phase 1：tree node 合併（C14）

### 1.1 新增 `Models/FormSchemaKinds.cs`

把現有 `FormSchemaNodeKind` enum 的 8 個值改為字串常數：

```csharp
internal static class FormSchemaKinds
{
    public const string Schema = "Schema";
    public const string Table = "Table";
    public const string Field = "Field";
    public const string RelationGroup = "RelationGroup";
    public const string LookupGroup = "LookupGroup";
    public const string Mapping = "Mapping";
    public const string ListItemsGroup = "ListItemsGroup";
    public const string ListItem = "ListItem";
}
```

### 1.2 改寫 `FormSchemaNodeDisplay`

簽章從 `For(FormSchemaNodeKind, object?)` 改成 `For(string, object?)`。Switch 內部用 `FormSchemaKinds.X` 做 constant pattern。其餘 Schema / Table / Field / ... 私有 helper 不動。

### 1.3 改寫 `FormSchemaNodeBuilder`

- 回傳型別從 `FormSchemaTreeNode` 改 `SettingsTreeNode`
- `Make()` 改為呼叫 `SettingsTreeNode.Create(icon, kind, payload, Refresh, isExpanded)`
- 新增私有 `Refresh(SettingsTreeNode node)` 一行：把 `FormSchemaNodeDisplay.For(node.Kind, node.Payload)` 結果寫回 `Header / Detail`
- 8 個 `BuildXxx` 方法的 `FormSchemaNodeKind.X` 全部改 `FormSchemaKinds.X`

### 1.4 改寫 `FormSchemaDocumentViewModel`

- `_selectedTreeNode` 型別 `FormSchemaTreeNode?` → `SettingsTreeNode?`
- `Roots` collection 型別跟進
- `SelectedKindIsX` 比較 `FormSchemaNodeKind.X` → `FormSchemaKinds.X`
- `SelectedKindCanDelete` 比較字串
- Delete switch 內所有 `FormSchemaNodeKind.X` → `FormSchemaKinds.X`
- 私有 `FindAncestor` helper 簽章跟進

### 1.5 改寫 `Views/FormSchemaDocumentView.axaml`

兩處 `x:DataType="models:FormSchemaTreeNode"` 改 `models:SettingsTreeNode`。

### 1.6 移除 `Models/FormSchemaNodeKind.cs` 與 `Models/FormSchemaTreeNode.cs`

兩檔皆 dead 後刪除。

### 1.7 Phase 1 驗收

- `dotnet build` 0 警告 0 錯誤
- `dotnet run -- --smoke tests/Define/FormSchema/Employee.FormSchema.xml` 全綠（FormSchema phase 必通過，否則代表 tree dispatch 壞掉）

## Phase 2：FormSchemaVM 繼承 base（C15）

### 2.1 改繼承

```csharp
public sealed partial class FormSchemaDocumentViewModel : SingletonDocumentViewModelBase
```

Ctor 改為：

```csharp
private FormSchemaDocumentViewModel(string filePath, FormSchema schema, SolutionContext solution)
    : base(filePath, "FormSchema", schema.ProgId)
{
    Schema = schema;
    Solution = solution;
    Roots.Add(FormSchemaNodeBuilder.BuildSchema(schema));
    SelectedTreeNode = Roots[0];
}
```

### 2.2 刪除已被 base 涵蓋的成員

- `public override string Title { get; }`（base 已算）
- `public override string DocumentKey => FilePath;`（base 已給）
- `public string FilePath { get; }`（base 已有）
- `public ObservableCollection<SettingsTreeNode> Roots { get; }`（base 已有）
- `public ObservableCollection<ValidationIssue> Issues { get; }`（base 已有）
- `_selectedTreeNode` 與其 `[ObservableProperty]` + `[NotifyXxx]` attributes（base 已有）
- `Save` / `Validate` `[RelayCommand]`（base 已有）
- `FileSaveCommand / FileValidateCommand` override（base 已 forward 到 SaveCommand/ValidateCommand）
- `SelectedKindCanDelete`（base 已算為 `SelectedTreeNode != null && GetDeleteAction(SelectedTreeNode) != null`）
- 整段 `Delete` `[RelayCommand]`（base 已有；改 override `GetDeleteAction`）

### 2.3 改成 override

```csharp
protected override object RootObject => Schema;

protected override IReadOnlyList<ValidationIssue> PerformValidation()
    => FormSchemaValidator.Validate(Schema, Solution);

protected override Action? GetDeleteAction(SettingsTreeNode node) => node.Kind switch
{
    FormSchemaKinds.Table when node.Payload is FormTable t
        && node.Parent?.Payload is FormSchema s => () => s.Tables!.Remove(t),
    FormSchemaKinds.Field when node.Payload is FormField f
        && node.Parent?.Payload is FormTable t => () => t.Fields!.Remove(f),
    FormSchemaKinds.Mapping when node.Payload is FieldMapping m
        && node.Parent is { Kind: FormSchemaKinds.RelationGroup, Payload: FormField rf }
        => () => rf.RelationFieldMappings!.Remove(m),
    FormSchemaKinds.Mapping when node.Payload is FieldMapping m
        && node.Parent is { Kind: FormSchemaKinds.LookupGroup, Payload: FormField lf }
        => () => lf.LookupFieldMappings!.Remove(m),
    FormSchemaKinds.ListItem when node.Payload is ListItem i
        && node.Parent is { Kind: FormSchemaKinds.ListItemsGroup, Payload: FormField pf }
        => () => pf.ListItems!.Remove(i),
    _ => null,
};

protected override void OnSelectedTreeNodeRefreshDerivedProperties(SettingsTreeNode? value)
{
    OnPropertyChanged(nameof(SelectedKindIsSchema));
    OnPropertyChanged(nameof(SelectedKindIsTable));
    OnPropertyChanged(nameof(SelectedKindIsField));
    OnPropertyChanged(nameof(SelectedKindIsRelationGroup));
    OnPropertyChanged(nameof(SelectedKindIsLookupGroup));
    OnPropertyChanged(nameof(SelectedKindIsListItemsGroup));
    // base 已會自動更新 SelectedKindCanDelete 與 DeleteCommand can-execute；
    // 5 條 AddXxxCommand 的 can-execute 仍須手動觸發。
    AddTableCommand.NotifyCanExecuteChanged();
    AddFieldCommand.NotifyCanExecuteChanged();
    AddRelationMappingCommand.NotifyCanExecuteChanged();
    AddLookupMappingCommand.NotifyCanExecuteChanged();
    AddListItemCommand.NotifyCanExecuteChanged();
}
```

### 2.4 保留的成員

- `Schema`、`Solution`（FormSchema 專屬狀態）
- `TabIcon` override
- `SelectedEditorContext` override（含 MappingGroupEditor 包裝）
- 6 個 `SelectedKindIsX` 唯讀屬性（context-menu 顯示控制；base 沒有對應）
- 5 個 `[RelayCommand]`：`AddTable / AddField / AddRelationMapping / AddLookupMapping / AddListItem`
- 兩個 private static helper：`FindAncestor`、`EnsureGroup`

### 2.5 axaml 驗收點

- `FormSchemaDocumentView.axaml` 對 SelectedKindIsX 與 AddTableCommand 等的 binding **不需改**（屬性名相同）。
- 確認 `IsDirty` 在 tab 上的紅點仍亮（base 的 IsDirty 推送）。
- 確認 SelectedKindCanDelete → context menu「刪除」項顯示邏輯與舊版一致。

### 2.6 Phase 2 驗收

- `dotnet build` 0 警告 0 錯誤
- `--smoke` 全綠（特別關注 `[smoke:formschema]` — 它做 Add+Save round-trip，會驗到 Add 命令與 base 的 Save 串接）
- 手動 GUI 開 `tests/Define/FormSchema/Employee.FormSchema.xml`：點 Schema/Table/Field 三層 → 右鍵 context menu 顯示對的「新增/刪除」項；切 RelationGroup 看 MappingGroupEditor 載出

## 風險與緩解

| 風險 | 緩解 |
|------|------|
| `[NotifyCanExecuteChangedFor]` 在 base 的 `_selectedTreeNode` 只能列 base 自己的命令（DeleteCommand），FormSchema 的 5 條 Add 命令 can-execute 不會自動更新 | 在 `OnSelectedTreeNodeRefreshDerivedProperties` override 內手動 `.NotifyCanExecuteChanged()`（已在 2.3 列出） |
| FormSchema 原 Delete 命令裡的 switch 分流到 5 個 case；用 `GetDeleteAction` pattern 後若漏 case，UI 會把刪除選項顯示但點下去 no-op | 拆完先跑 smoke + 手動 GUI 點各層級確認；smoke 已涵蓋 Table/Field round-trip |
| Tree node 合一後，FormSchemaNodeDisplay 的 string constant pattern matching 若 `FormSchemaKinds` 不是 `public`，外部組件無法引用 → 不是問題（單組件，`internal` 就夠） | 設為 `internal` |
| Phase 1 + Phase 2 改動量大，merge conflict 風險 | 分兩個 commit；Phase 1 完成 + smoke 通過後立即 commit，再做 Phase 2 |

## 不在範圍內

- C18（`DatabaseServerEditor` / `DatabaseItemEditor` 屬性 proxy）— 屬「樣板代碼但功能正確」，暫不動
- C19（8 份 `OnSelectedTreeNodeRefreshDerivedProperties` override）— CommunityToolkit.Mvvm 局限，無清爽解
- D 段細節（如 `using System.X` 系列冗餘 using 清理）— 風險低但收益小，獨立批次
