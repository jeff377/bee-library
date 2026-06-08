# 計畫：DefineEditor 多語系（zh-TW / en）支援

**狀態：✅ 已完成（2026-06-08）**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 1 | i18n infrastructure：`LocalizationService` + 自訂 markup extension + resx 雙語資源 | ✅ 已完成（c5ba472d） |
| 2 | View menu 加 Language 切換、寫入 user-config 並 binding 動態更新 | ✅ 已完成（c5ba472d） |
| 3 | axaml UI 字串遷移至 resx key（9 DocumentView + MainWindow + Dialogs） | ✅ 已完成（5ebcae4f） |
| 4 | ViewModel UI 字串（StatusText / Dialog 訊息）遷移 | ✅ 已完成（5ebcae4f） |
| 5 | Validator messages、tree node detail、placeholder defaults | ✅ 已完成（本 commit；不放 resx，採英文 inline——dev-tool 慣例） |

## 背景

DefineEditor 目前所有 UI 字串為繁體中文。要讓給「所有開發者」用，需支援英文。
直接全英文化會丟失對繁中使用者的可讀性，**雙語系 + 執行時可切換**是合理做法。

## 設計

### 為何不選「全英文化」單一語系

- 既有繁中文字已穩定使用、文件文化一致；切英文會失去對使用者的友善
- 雙語架構同樣的 infrastructure 成本（要動所有字串），但保留兩種選擇
- 未來若要再加日 / 簡中等其他語系，infrastructure 已就緒

### 為何不選「.NET ResourceManager 預設機制」

`ResourceManager` 標準但太重，且 Avalonia 沒有對 `.resx` 的內建 markup extension；
通常仍需自訂 markup extension 或 `IStringLocalizer` adapter，cost 跟自寫差不多。

### 採取的方案

**自訂 markup extension `{Loc StringKey}` + `LocalizationService` singleton + 雙 resx**：

1. **`Resources/Strings.resx`**（**英文，預設**）與 **`Resources/Strings.zh-TW.resx`**（繁中翻譯）—
   每個 UI 字串對應一個 key，例如 `Menu_File`、`Action_Save`、`Confirm_DeleteTitle`。
   .NET ResourceManager 找不到對應 culture 的條目時自動 fallback 到 neutral，所以英文
   永遠是兜底；想看中文必須明確切到 zh-TW。
2. **`Services/LocalizationService.cs`** singleton，`INotifyPropertyChanged`，暴露 `string this[key]`
   indexer；切換語系時 raise `PropertyChanged(Indexer)` 讓所有 binding 自動更新
3. **`Markup/LocExtension.cs`** 自訂 markup extension，回傳 binding 到 LocalizationService indexer
4. **持久化**：使用者選擇寫入 `~/Library/Application Support/Bee.DefineEditor/settings.json`
   （Mac）/ `%APPDATA%/Bee.DefineEditor/settings.json`（Win）/ `~/.config/bee-define-editor/settings.json`（Linux）

### 範例 markup 使用

```xml
<!-- 既有 -->
<TextBlock Text="儲存"/>

<!-- 改為 -->
<TextBlock Text="{loc:Loc Action_Save}"/>
```

```csharp
// ViewModel 內
StatusText = LocalizationService.Current["Status_NodeDeleted"];
```

### View menu 切換結構

```
View
├── Toggle Theme
├── ─────
└── Language
    ├── English       (☑ 預設)
    └── 繁體中文
```

點選後立即生效，所有 UI binding 同步更新（不需重啟）。首次啟動預設 English；
使用者切換後選擇寫入 user-config，下次啟動讀回。

## 範圍盤點

### 要處理的 UI 字串（~250 條）

| 類別 | 檔案 | 約略字串數 |
|------|------|-----------|
| Main shell | MainWindow.axaml, App.axaml.cs | ~15 |
| Menu (macOS) | App.axaml.cs | ~10 |
| 9 DocumentView axaml | Views/*DocumentView.axaml | ~80 |
| 11 ViewModel StatusText / dialog | ViewModels/*.cs | ~60 |
| Dialogs | AboutDialog.axaml, ConfirmationDialog.axaml | ~15 |
| Validators messages | Services/*Validator.cs | ~70 |

### 不在範圍

- **code comment**：保留中文，內部開發者讀寫一致即可
- **plan / docs**：規範要求 zh-TW
- **commit message**：保留 zh-TW
- **`DocumentStyles.axaml` 內 ToolTip / Header**：少量 demo-like strings，混入 axaml 一起做
- **`tests/Define/`** 內 fixture：定義檔本身的內容，不是 UI

## 階段細節

### 階段 1：infrastructure

新增：
- `Resources/Strings.resx`（中文預設，含所有 key）
- `Resources/Strings.en.resx`（英文翻譯）
- `Services/LocalizationService.cs`（singleton + PropertyChanged）
- `Markup/LocExtension.cs`（markup extension）
- `Services/UserSettings.cs`（讀寫 user-config 的 Language 偏好）

`Bee.DefineEditor.csproj` 加 `<EmbeddedResource>` 對 `Strings.resx` 與 `Strings.en.resx`。

### 階段 2：Language 切換

`App.axaml.cs` 啟動時讀 `UserSettings.Language`（不存在則用 `"en"`）並設定
`LocalizationService.Culture`；View menu 加 Language sub-menu 兩項
（English / 繁體中文），每項 Click 改 LocalizationService.Culture + 寫入 UserSettings。

> 不採「依 OS culture 自動偵測」— 預設一律英文，讓「給所有開發者使用」這個目標
> 在 first-run 就成立；中文使用者再手動切換即可。

### 階段 3：axaml 字串遷移

對每個 axaml：把所有硬編碼中文 `Text="..."` / `Header="..."` / `ToolTip.Tip="..."` /
`PlaceholderText="..."` 改為 `{loc:Loc Xxx}` markup。同步把字串塞進 resx 雙份。

實作時用 grep + Edit 一個 view 一輪。

### 階段 4：ViewModel 字串遷移

把 ViewModel 內 `StatusText = "..."` / dialog message / validator message
改為 `LocalizationService.Current["..."]` 或 `string.Format(...)`（含參數的情況）。

## 風險

- **資源 key 命名一致性**：建議 prefix（`Action_` / `Status_` / `Confirm_` / `Validation_`）
  避免後期撞名。實作前先列 key 清單。
- **語系切換時 StatusText 不會自動更新**：已記憶體的 status text 是當下值的 snapshot；
  切換語系後仍是舊語言，直到下次操作。可接受（user 切完繼續用 app 就會看到新語）；
  若要嚴格的話可在 LocalizationService.CultureChanged 時呼叫各 VM 的 RefreshStatusText。
- **Validator message 是大宗**：可能跨 70+ 條，工作量集中在這。先做 axaml + StatusText，
  Validator 留 phase 4 最後。

## 不在本次範圍

- 其他語系（日 / 簡中等）— infrastructure 支援即可，翻譯延後
- README 雙語化（單獨議題）
- Bee.NET 其他套件 i18n
