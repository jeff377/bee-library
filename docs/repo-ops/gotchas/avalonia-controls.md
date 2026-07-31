# 踩雷誌：Avalonia 控件

`tools/DefineEditor`、`src/Bee.UI.Avalonia`、`apps/Bee.Northwind` 實證過的雷（Avalonia 12 +
Semi.Avalonia）。對應硬規則見 `.claude/rules/avalonia.md`。**改控件前先掃一遍。**

## 版面與 hit-test

1. **`HorizontalAlignment=Stretch` + `MaxWidth` 會置中。** 可用空間超過 MaxWidth 時，元素在剩餘
   slot 置中、不靠左。要「填滿但封頂」且靠左，Avalonia 元素層級做不到；DocumentStyles 的解法是
   拿掉 MaxWidth 全寬填滿，寬度封頂需求改走 `.row.short` 固定 Width。
2. **`Background = null` 不參與 hit-test。** `ContentControl` / panel 沒設背景時，只有內容字面
   可點，旁邊空白區點擊直接穿透。要整區可點就設 `Brushes.Transparent`。
3. **Semi 的 ComboBox / DatePicker 預設不 stretch**（TextBox 預設 stretch）。欄位寬度會隨內容變、
   唯讀底線縮不到滿版。比照 TextBox 在 ctor 設 `HorizontalAlignment=Stretch`，寬度才由容器決定。

## 繼承內建控件

4. **`StyleKeyOverride` 是必修課。** 子類預設以自身型別查 ControlTheme，而 Semi/Fluent 只提供
   內建型別的 theme → 不覆寫就**整顆隱形**。
5. **code-behind 要存取 `x:Name` 控件 → 必須用 source-generated `InitializeComponent`。**
   ctor 只呼叫 `InitializeComponent()`、**不要自己寫**
   `private void InitializeComponent(){ AvaloniaXamlLoader.Load(this); }`。手寫版只 load XAML、
   不指派具名控件欄位 → 欄位 null → `OnLoaded`/事件存取時 NRE（exit 134 SIGABRT）。
   **不存取具名控件的 View 兩種寫法都過、不會暴雷**，所以 copy 範本時很容易誤帶手寫版。
   對齊 `samples/Avalonia.DemoCenter` / `tools/DefineEditor` 的寫法。

## 事件與資料流

6. **控件的「語意事件」對程式設值不觸發。** `TextBox.TextChanged`、
   `DatePicker.SelectedDateChanged`、`CalendarDatePicker.SelectedDateChanged` 只保證使用者互動時
   觸發，程式設值常 silent。要可靠監聽一律 hook `PropertyChanged` 比對 `TextProperty` /
   `SelectedDateProperty`（headless 單元測試也因此才測得到）。
7. **DataGrid 不觀察 `DataView` 變化。** `DataTable` 增刪列後 `DataView` 已更新，但 realized 的
   DataGrid 列不動（它不監聽 `ListChanged`）。任何列集合異動後要重設 `ItemsSource` 重 realize。
8. **ADO.NET `DataRow.BeginEdit` 不抑制 `ColumnChanged`**（且無變更的 `EndEdit` 仍發 `RowChanged`）。
   要做「暫存編輯、取消零事件」必須自行追蹤編輯中列並在事件層靜默——`FormDataObject` 的列編輯
   協定即此模式。

## DataGrid 與模板回收

9. **DataGrid 編輯管線與 popup 編輯器結構性衝突。** `CellEditingTemplate` 裡放 `ComboBox` /
   `DatePicker`，popup 一開焦點就離開 cell → 編輯模板被撕掉。解法是 **click-to-swap**
   （CellTemplate 自管置換，column 標 `IsReadOnly` 讓管線不介入，見 ADR-021）。同場加映：
   swap 進來的 ComboBox 立即 `IsDropDownOpen=true` 會被同一次點擊的後續事件關掉，要
   `Dispatcher.Post` 延後；`DatePicker` 無公開開啟 API，用 `FindDescendantOfType<Button>` 觸發
   template flyout 按鈕的 Click。
10. **`FuncDataTemplate` 算死內容 + `supportsRecycling:true` → 顯示與底層列脫鉤**（見 ADR-022）。
    cell template 若把 `Text` 在建立當下算死（`Text = FormatCell(row,...)`，非 binding），
    就**不能**開 recycling：DataGrid 跨列回收 presenter 時不重跑建立委派、只換 DataContext，
    算死的 Text 停在舊列。
    **症狀**：lookup picker 上表現為「看到某列、點下去帶回別列」——**選取與寫回其實都正確，
    錯的只有顯示層**，所以很容易往錯的方向查。**潛伏性高**：資料少/不捲動時初次每格各建一次、
    顯示是對的，多次開窗或捲動才爆。
    **修法**：算死內容的 cell 一律 `supportsRecycling:false`（`GridControl` 其他 cell 模板本就是
    false，只有純文字 cell 漏網）。改真 binding 不可行——`DataRowView` 的 string indexer 不被
    Avalonia binding 支援（ADR-020）。
11. **ComboBox + 回收模板會讓選取框空白。** `ItemTemplate` 用
    `FuncDataTemplate(supportsRecycling: true)` 時，同一個控件實例被同時發給下拉清單項與選取框
    （控件不能有兩個 parent），選完值收合後不顯示。解法：改 `DisplayMemberBinding`
    （每容器各自生成內容）。

## 唯讀外觀（FormMode 切換去框留底線）

12. **唯讀視覺要綁 `binder.AllowsEdit(formMode)`，不要綁 `TextBox.IsReadOnly`。** lookup 型
    `ButtonEdit` 的文字框永遠 `IsReadOnly=true`（只能經 dialog 寫入）但編輯模式其實可編輯，
    綁 `IsReadOnly` 會讓它在編輯模式誤顯唯讀外觀。
    TextEdit 族唯讀去四邊框：`BorderThickness=(0,0,0,1)`、`Background=Transparent`、`BorderBrush`
    設固定淺灰（`#80808080`），離開唯讀 `ClearValue` 還原。**底線色必須設本地值**才會蓋過主題的
    hover/focus setter，否則靜止時看不到底線。
13. **DatePicker / ComboBox 唯讀無法用 setter 去框，要換 template——但裸換會炸。**
    `DatePicker.OnApplyTemplate → SetSelectedDateText()/SetGrid()` **無 null 防護**，會解參
    `PART_DayTextBlock` / `PART_MonthTextBlock` / `PART_YearTextBlock` / `PART_*Spacer` /
    `PART_ButtonContentGrid`（缺則 NRE）；`ComboBox` 用 `NameScope.Get<Popup>("PART_Popup")`
    （缺則 throw）。自寫唯讀 template 必須**註冊**這些隱藏部件。
    **關鍵**：`Popup` / `Button` 只要 `scope.Register` 即可滿足 `Find`/`Get`，
    **絕不可加進 visual tree**——Popup 當 Panel 子節點會**凍結 UI**（實證 hang）。
14. **Semi/Fluent 的 `:disabled` 是用 `ContentPresenter` 的 Foreground 筆刷淡化文字、不是整體
    opacity。** 所以「CheckBox 方塊灰、標題文字保持可讀」做得到：`IsEnabled=false` 讓方塊走
    disabled 灰（`Border#NormalRectangle` 換 disabled 筆刷 + `Panel#PART_GlyphPanel` opacity 0.75），
    再於 `OnApplyTemplate` 取 `PART_ContentPresenter` 用**本地 binding** 把 Foreground 釘回控件
    正常 `Foreground`（本地優先序最高、蓋過主題 disabled setter）。
    先試 ContentPresenter opacity 還原、再試 instance Style + TemplatedParent binding **都無效**，
    只有 `OnApplyTemplate` + 本地 binding 才穩。

## 測試並行

15. **`AvaloniaPropertyRegistry` 首次填充非執行緒安全。** xUnit 並行下多個 test class 同時首觸
    同型別的 direct property（如 `DataGrid.ItemsSource`）會撞 `Dictionary.Add`。
    測試組件用 `[ModuleInitializer]` 單執行緒預熱（建構控件 + 設一次 direct property）根除。
16. **`AvaloniaPropertyDictionaryPool` 並行 race（比上一條更難纏）。** 控件 ctor parent 子控件時
    走 `SetInheritanceParent` → 共享 pool 的 `Get/Pop`，是 TOCTOU（count 檢查與 Pop 之間 race），
    並行建構控件時間歇 `InvalidOperationException: Stack empty`。
    **與上一條不同**：這是**持續性** pool 存取，「單執行緒預熱一次」化解不了；Bee 自身 static-state
    的 fixture 化解也無效（這是 Avalonia 內部）。
    **症狀**：xUnit 預設並行下同 assembly 不同控件測試**每次失敗的都不同**、約 1/3 的 run 會爆。
    **根治**：對「幾乎全是控件測試」的 assembly 加
    `[assembly: CollectionBehavior(DisableTestParallelization = true)]`
    （`Bee.UI.Avalonia.UnitTests` 已加；serial 跑 <1s，代價可忽略）。
    新增大量控件測試把並行壓力推過門檻時才會浮現（Northwind RecordView 那輪：224→236 個測試就爆）。

## UI 自動化

17. **別用 computer-use 的 `open_application` 開剛編譯的 app。** Launch Services 會把 bundle id
    解析到舊的 `publish/*.app` 產物，不是剛編譯的 `bin/{Debug,Release}/net10.0/` 執行檔——曾因此
    對舊版 app 做了多輪無效診斷。
    真要用 computer-use 驅動（如 `demo-smoke` skill）時：`dotnet <dll>` 或 apphost 裸程序**不會**被
    `request_access` 認得（無 bundleID，LaunchServices 顯示為 "Avalonia Application"，名稱比對全失敗）
    → 必須包成最小 `.app`（`Contents/MacOS/` + `Info.plist` 自訂 `CFBundleIdentifier`），`open` 後
    以 **bundle id** 呼叫 `request_access`；改 code 要重 build 再 `cp -R` 回 bundle。
    別的 app 跳前景會擋 click，用 `osascript` System Events 以 pid 設 frontmost 拉回。

    > 使用者偏好：**改動編譯通過即可交付，由他自行啟動測試**（agent 驅動 UI 太慢）。
