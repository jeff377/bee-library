# 計畫：欄位控件唯讀模式採「去框留底線」外觀，統一套用至 BindFieldControl

**狀態：🚧 進行中（2026-06-16）**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 1 | TextEdit 族（TextBox）唯讀外觀：去四邊框、留底線（程式碼自足路徑，含 ButtonEdit 隱藏按鈕） | ✅ 已完成（2026-06-16，使用者目視確認） |
| 2 | DateEdit / DropDownEdit 唯讀：template 置換為底線顯示（隱藏按鈕、不灰化、stretch 滿版） | ✅ 已完成（2026-06-16，使用者目視確認） |
| 3 | CheckEdit 唯讀：方塊灰化（disabled）但標題文字保持正常可讀 | ✅ 已完成（2026-06-16，使用者確認） |
| 4 | GridControl 內嵌編輯器影響驗證 + sample 目視確認 | 📝 待做 |

> **實測校準（2026-06-16）**：底線色 = `#80808080` α=`0xB0`（≈69%，使用者確認的深度）；唯讀值左內縮 8px 對齊 TextBox；DateEdit/DropDownEdit 加 `HorizontalAlignment=Stretch`（比照 TextBox 預設）使寬度由容器決定、底線滿版、編輯/唯讀同寬。

> **實作方向修正（2026-06-16）**：原規劃的「`FieldEditorBinder` 打 `bee-readonly` class + library axaml Styles + host StyleInclude」**未採用**。改採**程式碼自足路徑**——理由見下方〈設計決策〉修正版。

## 背景與目標

目前 `FormView` 透過 `FormMode`（`SingleFormMode`：View / Add / Edit）切換唯讀與編輯，但唯讀／編輯的視覺差異不明顯，使用者難以一眼分辨目前處於哪種模式。

**首要設計準則（為什麼）**：唯讀模式的核心目的是讓使用者**清爽地檢視整筆資料**。一整張 View 模式表單若每個欄位都帶四邊框，視覺會被框線切割而失焦、難以閱讀。因此唯讀外觀要把框線降到最低，讓整張表單讀起來像一份乾淨的紀錄，而非一格一格的輸入框。所有視覺細節取捨（底線粗細／顏色、背景、間距對齊）都以「降低視覺雜訊、提升整體可讀性」為依歸——底線宜輕、不搶眼，只要足以界定欄位即可。

**目標**：唯讀模式下，欄位控件採「去除四邊框、只留底線」的外觀，讓整筆資料易於檢視，同時讓編輯/唯讀狀態在視覺上明確可辨。

**使用者拍板的方向**：
- **DateEdit / DropDownEdit 唯讀** → 比照 TextEdit：隱藏下拉／日曆按鈕、去四邊框留底線、**不灰化**。
- **CheckEdit** → 唯一例外，不做底線，只要有清楚可辨識的唯讀外觀即可。

## 架構現況（為何不能「一條樣式套全部」）

7 個欄位控件**無共同基底類別**，各自繼承不同 Avalonia 原生控件，靠 `IFieldEditor` 介面 + 共用的 `FieldEditorBinder`（組合）串接：

| 控件 | 原生基底 | 目前唯讀做法 |
|------|---------|------------|
| `TextEdit` / `MemoEdit` / `ButtonEdit` | `TextBox` | `IsReadOnly = true` |
| `DateEdit` / `YearMonthEdit` | `DatePicker` | `IsEnabled = false`（灰化） |
| `DropDownEdit` | `ComboBox` | `IsEnabled = false`（灰化） |
| `CheckEdit` | `CheckBox` | `IsEnabled = false`（灰化） |

且每個控件以 `StyleKeyOverride => typeof(原生型別)` 把樣式查找導回原生型別 → **無法寫 `Selector="IFieldEditor"`**，視覺必須逐原生型別定義（實際 4 份：TextBox / DatePicker / ComboBox / CheckBox）。

**兩個關鍵限制**：
1. 唯讀狀態目前用兩種不同屬性表達（`IsReadOnly` vs `IsEnabled`），無法被同一個 selector 命中。
2. `IsEnabled=false` 會灰化，與「乾淨唯讀閱讀外觀」目標相衝突。

## 設計決策（2026-06-16 修正版）

### 決策 1：唯讀判定的單一真相 = `binder.AllowsEdit(formMode)`，不另設 class

原規劃要在 `FieldEditorBinder` 打 `bee-readonly` class 當「單一咽喉點」。實作時發現**單一真相早已存在**——`FieldEditorBinder.AllowsEdit(formMode)`（綜合 formMode、layout ReadOnly、AllowEditModes），每個編輯器的 `SetControlState` 都呼叫它。因此**不需要額外的 class**：各控件在 `SetControlState` 內依 `!AllowsEdit(formMode)` 套用自己型別的視覺即可。

> **為何放棄 class + axaml**：library 目前零 axaml 基礎設施；走 axaml Styles 需 host 主動 `StyleInclude`，**漏載入會靜默失效**（樣式不生效卻無報錯）。程式碼自足路徑開箱即用、無 host 耦合、無靜默失效風險。

### 決策 2：唯讀視覺綁「有效編輯權限」而非 `IsReadOnly`

唯讀外觀必須由 `!AllowsEdit(formMode)` 驅動，**不能**綁 `TextBox.IsReadOnly`。原因：lookup 型 `ButtonEdit` 的文字框**永遠** `IsReadOnly=true`（只能經 lookup dialog 寫入），但編輯模式下它其實可編輯、該顯示完整邊框 + 搜尋鈕。綁 `IsReadOnly` 會讓 lookup 欄在編輯模式誤顯唯讀外觀。

### 決策 3：TextBox 族——程式碼 setter，沿用主題 BorderBrush（已完成）

`TextEdit.ApplyReadOnlyAppearance(bool)`：唯讀時 `BorderThickness=(0,0,0,1)`、`Background=Transparent`；編輯時 `ClearValue` 還原主題值。**底線顏色不另設，沿用主題既有 `BorderBrush`（只留底邊）**，自動適應 Light/Dark。`MemoEdit` 繼承沿用；`ButtonEdit` 改寫為唯讀時 `_button.IsVisible=false`（隱藏搜尋鈕）+ 套底線。

### 決策 4：DatePicker/ComboBox 唯讀「換 template」+ 隱藏部件契約（已完成）

唯讀時 **整個 template 換成 `ReadOnlyFieldVisual.Build` 的極簡顯示**（`TextBlock` 綁 `ReadOnlyText` + 底線 Border）。編輯模式 `ClearValue(TemplateProperty)` 還原原生 template。

- **行為改動**：不再用 `IsEnabled=false`（灰化）。唯讀設 `Focusable=false` + `IsHitTestVisible=false` 達成非互動、不灰化。可觀察契約改為 `IsHitTestVisible`（測試已同步）。
- **顯示文字**：`DateEdit.ReadOnlyText` 於 `RefreshFromSource` 以 `ValueFormat` 格式化；`DropDownEdit.ReadOnlyText` 於 `SelectionChanged` 取 `ListItem.Text`。
- **踩雷修正**（關鍵）：
  - **ComboBox** `OnApplyTemplate` 用 `NameScope.Get<Popup>("PART_Popup")`，缺則 throw → 自寫 template 必須註冊隱藏 `PART_Popup`。
  - **DatePicker** `SetSelectedDateText()/SetGrid()` **無 null 防護**解參 `PART_DayTextBlock`/`PART_MonthTextBlock`/`PART_YearTextBlock`/`PART_*Spacer`/`PART_ButtonContentGrid` → 必須全數註冊（隱藏）。
  - **Popup/Button 只「註冊不入 visual tree」**——把 `Popup` 當 Panel 子節點會凍結 layout（實測 UI hang）。`NameScope.Register` 即可滿足 `Find`/`Get`。
- **寬度**：`HorizontalAlignment=Stretch`（比照 TextBox）使底線滿版、寬度固定不隨內容變。

### 決策 5：CheckBox——方塊灰化但標題保持可讀（特例，已完成）

唯讀時 `IsEnabled=false`，讓主題把**方塊**畫成 disabled 灰（使用者要的「方塊灰階」）。但 Semi/Fluent 的 disabled 會**同時**把標題經 `ContentPresenter` 的 **Foreground 筆刷**淡化（非整體 opacity）——對閱讀不友善。

修法：`OnApplyTemplate` 取得 `PART_ContentPresenter`，把它的 `Foreground` 以**本地 binding** 釘回控件自身的正常 `Foreground`。本地優先序最高、蓋過主題 disabled 設定，故**只有方塊灰、標題維持正常可讀**。不做底線。

> 走過的彎路：先試 `ContentPresenter` opacity 還原（無效，因淡化是 Foreground 筆刷不是 opacity）；再試 instance `Style` + TemplatedParent binding（無效，selector/優先序不確定）；最終 `OnApplyTemplate` + 本地 binding 才穩定生效。

## 階段細節

### 階段 1：FieldEditorBinder 統一打 class
- `OnFormModeChanged`、`BindCore` 兩處設定 `_owner.Classes.Set("bee-readonly", !AllowsEdit(formMode))`。
- 各控件 `SetControlState` / `ApplyMetadata` 移除 `IsReadOnly = ...` / `IsEnabled = ...` 唯讀路徑（保留 `AllowsEdit` 對 write-back 的把關）。
- 此階段先不動視覺，確認 class 正確切換（可暫時用一個顯眼的 debug style 驗證）。

### 階段 2：library Styles 資源 + host 載入
- 決定資源形式：在 `src/Bee.UI.Avalonia/` 新增 `Themes/Editors.axaml`（`Styles` 根），並在 csproj 開 Avalonia XAML 編譯（`AvaloniaResource`）。
- 決定 host 載入方式：host `App.axaml` 的 `<Application.Styles>` 加 `StyleInclude Source="avares://Bee.UI.Avalonia/Themes/Editors.axaml"`。需更新兩個 sample 並在 README / scaffold 記錄。
- 用 setter 疊加而非取代既有 ControlTheme，避免覆寫 host 主題。

### 階段 3：TextBox 族底線外觀
- `Style Selector="TextBox.bee-readonly"`：`BorderThickness=0,0,0,1`、`Background=Transparent`、padding 對齊。
- 驗證 `TextEdit` / `MemoEdit` / `ButtonEdit` 三者外觀一致。

### 階段 4：DateEdit / DropDownEdit 唯讀顯示模式
- `Style Selector="DropDownEdit.bee-readonly"` / `DateEdit.bee-readonly"` 設 `Template` 為極簡唯讀顯示（TextBlock + 底線 Border）。
- 解決顯示文字來源（DropDown 取 `ListItem.Text`、Date 取格式化字串）。
- 確認按鈕消失、不可互動、不灰化、底線與 TextEdit 對齊。

### 階段 5：CheckEdit 唯讀特例
- 設計唯讀視覺提示（保留勾選可見、移除互動感）。
- 不套底線。

### 階段 6：整合驗證
- 確認 `GridControl` 內嵌編輯器（共用同一批 editor 類別）受影響範圍與外觀是否合理。
- sample（FormView 切 View/Edit）目視確認唯讀↔編輯外觀差異明確。
- 編譯通過後交付使用者本機目視自測（Avalonia 試點慣例）。

## 風險與待解

- **唯讀顯示文字來源**（階段 4）：DatePicker 無現成格式化文字屬性，需自備。
- **Focus / Tab 導覽**：template-swap 後唯讀控件的可聚焦性需明確關閉。
- **host 載入耦合**：library 首次引入 axaml 資源，需同步更新 sample / scaffold / README，否則 host 不載入則樣式不生效。
- **GridControl 連動**：in-cell 編輯器共用同類別，唯讀外觀在 grid 內可能需另調。
