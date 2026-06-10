# 計畫：DefineEditor tab 體驗修正（dirty 標記 / 右鍵選單 / Save All）

**狀態：✅ 已完成（2026-06-10）**

## 背景

tools/DefineEditor 三項使用體驗缺口：

1. **屬性變更未標記 dirty**：tab 模板已有 dirty 圓點（`MainWindow.axaml` 綁 `IsDirty`），
   新增/刪除節點的命令也會設 `IsDirty = true`；但右側屬性面板的
   TextBox / ComboBox / CheckBox 是直接 TwoWay 綁到 Bee.Definition POCO
   （無 `INotifyPropertyChanged`），修改屬性值不會觸發任何通知，tab 不會出現已變更圖示。
2. **Tab 無右鍵選單**：只有 hover 關閉按鈕，缺 Close / Close Others。
3. **無批次儲存**：File menu 的 Save 只儲存 active document，多個 tab 編輯中時需逐一切換儲存。

## 修正項目

### 1. 屬性面板變更 → tab dirty 標記

POCO 無法發通知，改由 **view 層攔截使用者輸入事件**：

- 新增 `Behaviors/DirtyTrackingBehavior.cs`：attached property（如 `DirtyTracking.IsEnabled`），
  掛在各 document view 右側屬性面板的容器（hosting `SelectedEditorContext` 的 `ContentControl`）上。
- 以 `AddHandler(..., handledEventsToo: true)` 訂閱 bubbled 事件：
  - `TextBox.TextChangedEvent`
  - `SelectingItemsControl.SelectionChangedEvent`（ComboBox）
  - `ToggleButton.IsCheckedChangedEvent`（CheckBox）
- 事件觸發時往上找 `DocumentViewModelBase` DataContext，設 `IsDirty = true`。
- **抑制非使用者觸發的事件**：切換樹節點時 DataTemplate 重建、binding 初始賦值也會
  fire TextChanged / SelectionChanged。做法：監聽 `ContentControl.Content` 變更時設
  suppress 旗標，`Dispatcher.UIThread.Post(..., DispatcherPriority.Background)` 後解除
  （binding 初始化都在更高 priority 完成）。
- 套用範圍：FormSchema / FormLayout / TableSchema / SystemSettings / DbCategorySettings /
  PermissionModels / ProgramSettings / DatabaseSettings 各 document view 的屬性面板容器。
  （Language 的 DataGrid 與 DatabaseSettings 的 editor wrapper 已自行設 `IsDirty`，
  行為重疊無害，仍統一掛上以涵蓋未來新增欄位。）

### 2. Tab 右鍵選單：仿 VS Code 的 5 個 close 動作

- `MainWindow.axaml` tab item 模板的 `Grid` 加 `ContextMenu`，項目與 VS Code 一致：
  - **Close**：綁既有 `CloseDocumentCommand`（CommandParameter = 該 tab 的 doc）
  - **Close Others**：新增 `CloseOtherDocumentsCommand` — 關閉其他所有 tab
  - **Close to the Right**：新增 `CloseDocumentsToTheRightCommand` — 關閉該 tab 右側所有 tab
  - **Close Saved**：新增 `CloseSavedDocumentsCommand` — 關閉所有非 dirty 的 tab
  - **Close All**：新增 `CloseAllDocumentsCommand` — 關閉全部 tab
- 共用一個私有 helper（接受「要關閉的 doc 集合」），統一處理移除、`Dispose()`、
  `ActiveDocument` 重指派。
- 新增字串：`TabMenu_Close` / `TabMenu_CloseOthers` / `TabMenu_CloseRight` /
  `TabMenu_CloseSaved` / `TabMenu_CloseAll`（`Strings.resx` + `Strings.zh-TW.resx`）。

### 3. File menu「Save All」批次儲存

- `MainWindowViewModel` 新增：
  - `SaveAllAsync()`：依序對每個 `IsDirty && FileSaveCommand != null` 的 doc
    `await ((IAsyncRelayCommand)FileSaveCommand).ExecuteAsync(null)`
    （沿用各 doc 既有 Save 流程，含驗證錯誤時的確認對話框）；完成後
    `StatusText` 顯示儲存份數。
  - `HasDirtyDocuments` 屬性：OpenDocuments 增減時訂閱/退訂各 doc 的
    `PropertyChanged(IsDirty)` 並更新，供 menu CanExecute 用。
- `App.axaml.cs`：
  - 新增 `SaveAllCommand`，`CanExecute = vm.HasDirtyDocuments`；
    在既有 `vm.PropertyChanged` hook 加 `HasDirtyDocuments` 分支做
    `NotifyCanExecuteChanged`。
  - File menu 在 Save 之後加「Save All」項，gesture `Cmd+Option+S`（VS Code 慣例）。
- 新增字串：`MenuItem_SaveAll`（en + zh-TW）。

### 4. 關閉 dirty tab 時提示未儲存（追加項目）

- `ConfirmationDialog` 擴充三鍵模式（儲存 / 不儲存 / 取消），新增
  `ConfirmCloseResult` 列舉與 `ShowUnsavedAsync`；視窗被直接關閉視同取消。
- `MainWindowViewModel.PrepareCloseAsync(docs)`：要關的分頁含 dirty 時跳提示——
  單一檔顯示檔名、多檔顯示數量與清單；選「儲存」逐一走各編輯器既有 Save 流程
  （驗證錯誤確認框若被取消，該檔保持 dirty，**整個關閉動作中止**以免遺失）；
  headless（無 MainWindow）時跳過提示直接關，與其他對話框慣例一致。
- 五個 close 動作（按鈕 X、Close、Close Others、Close to the Right、Close All）
  全部先過 `PrepareCloseAsync`；Close Saved 只關非 dirty 分頁，天然不觸發。
- 新增字串：`Confirm_CloseUnsavedTitle` / `Confirm_CloseUnsavedMessage` /
  `Confirm_CloseUnsavedMessageMulti` / `Action_Save` / `Action_DontSave`。

## 不做的事

- 不引入 POCO 層的 change tracking（INPC 改造影響 Bee.Definition 整個套件，超出範圍）。
- 切換方案（OpenSolution）與關閉視窗時的未儲存提示不在本次範圍（可另開需求）。

## 驗證

1. `dotnet build tools/DefineEditor --configuration Release` 通過（TreatWarningsAsErrors）。
2. `Smoke.cs` 增補 VM 層可驗證的部分：`CloseOtherDocuments` 行為、
   `SaveAllAsync` 後所有 doc `IsDirty == false`。
3. UI 行為（dirty 圓點即時出現、右鍵選單、menu gesture）依慣例由使用者自測。
