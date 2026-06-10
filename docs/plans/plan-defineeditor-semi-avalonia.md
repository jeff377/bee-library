# 計畫：DefineEditor 全面改用 Semi.Avalonia theme（保留深淺兩種變體）

**狀態：✅ 已完成（2026-06-10）**

## 背景

`tools/DefineEditor` 目前的視覺層由三部分組成：

1. 官方 `FluentTheme`（`Avalonia.Themes.Fluent` 12.0.4）作為基底
2. `App.axaml` 內自製的 VS Code Dark+/Light+ 風 `ThemeDictionaries`（約 28 個 brush key × 2 變體）
3. `Styles/DocumentStyles.axaml`（318 行）— 混合「配色」與「版面」的 style classes（`tool`、`row`、`label`、`h2`、`muted`、`surface-sunken`、`titlebar`、`statusbar` 等），被 12 個 View 共用（`DynamicResource` 引用約 80 處、classes 引用 300+ 處）

維護自訂配色（每加一個 UI 元素要同時調 Dark/Light 兩套 brush）是目前的主要負擔。決議改採 **Semi.Avalonia**（已支援 Avalonia 12，NuGet `Semi.Avalonia` 12.0.x），配色全交給 theme，自訂樣式只保留純版面部分，讓後續開發專注在程式邏輯。

**需求確認**：深淺兩種變體都要保留，現有的 Dark/Light 切換功能（`App.axaml.cs:294` 的 toggle）不可退化。

## 改動範圍

### 1. csproj — 套件替換

- 移除 `Avalonia.Themes.Fluent`
- 加入 `Semi.Avalonia`（取執行當下 12.x 最新版，目前為 12.0.1）
- 其餘（`Avalonia.Fonts.Inter`、`CommunityToolkit.Mvvm` 等）不動

### 2. App.axaml — theme 入口與自訂 brush 移除

- `<FluentTheme />` → `<StyleInclude Source="avares://Semi.Avalonia/Themes/Index.axaml" />`
- **整段刪除** `ResourceDictionary.ThemeDictionaries`（Dark/Light 兩套自訂 brush）
- 保留 `IconKeyToGeometryConverter` resource 與 `ViewLocator`
- `RequestedThemeVariant="Dark"` 預設值保留；Semi 原生支援 Light/Dark variant，`App.axaml.cs` 的 toggle 邏輯**零改動**即可運作

### 3. DocumentStyles.axaml — 瘦身為純版面 + 圖示庫

- **保留**：全部 `StreamGeometry` 圖示資源（MDI path data）、版面類 setter（`Padding`、`Margin`、`Width`、`FontSize`、`CornerRadius`、`MinHeight`、Dock 配置）
- **移除**：所有 `Foreground` / `Background` / `BorderBrush` 的自訂 brush setter，讓控件繼承 Semi 預設
- **按鈕對應**：
  - `Button.tool` → 只留尺寸/間距 setter，外觀走 Semi 預設按鈕
  - `Button.tool.primary` → 改用 Semi 內建 `Classes="Primary"` + Solid 樣式（執行時依 Semi 12 實際 API 確認寫法）
  - `Button.tool.danger` → 同上改 `Classes="Danger"`
- **語意色改 binding Semi semantic token**（名稱以 Semi 12 實際資源為準）：
  - `ErrorForeground` → `SemiColorDanger`
  - `WarningForeground` / `DirtyIndicator` → `SemiColorWarning`
  - `MutedForeground` / `PropertyLabelForeground` → `SemiColorText2`
  - `SurfaceBackground` / `SunkenBackground` / `TitleBarBackground` / `StatusBarBackground` / `TreeBackground` → `SemiColorBg0`–`SemiColorBg2` 系列
  - `SplitterBrush` / `TitleBarBorder` → `SemiColorBorder`
- `Window` / `UserControl` 的 Inter 字型 setter 保留，背景/前景 setter 移除

### 4. Views（12 個 .axaml）— 清理零散引用

約 80 處 `DynamicResource` 引用逐一處理，原則：

- 能刪就刪（讓控件繼承 Semi 預設）— 預期為多數
- 確實需要語意色的（error/warning/dirty 指示、splitter）改 Semi token
- `Classes="..."` 引用全部不動（class 名不變，只是定義瘦身）

涉及檔案：`MainWindow`、`AboutDialog`、`ConfirmationDialog` + 9 個 document view。

### 5. 不動的部分

- `App.axaml.cs` theme toggle、`IconKeyToGeometryConverter`（icon 資源仍可解析）
- ViewModels / Models / Services 全部零改動
- `publish.sh`、`Program.cs`（`WithInterFont` 保留）

## 驗證

1. `dotnet build tools/DefineEditor/Bee.DefineEditor.csproj --configuration Release` 通過
2. 本機啟動 app，Dark/Light 各跑一輪：
   - 9 種 document view 逐一開啟，檢查屬性面板（`row`/`label` 版面）、工具列按鈕（primary/danger 配色）、TreeView、TabControl、status bar
   - 切換 theme toggle 確認兩變體即時切換、無殘留舊配色或對比不良的角落
3. 殘留檢查：`grep` 確認舊 brush key（`SurfaceBackground`、`ToolButtonBackground` 等）在 repo 內 0 引用

## 風險與備註

- Semi 的視覺語言（明亮、圓潤）與現在 VS Code 風差異大，屬預期內的取捨
- Semi token 命名以執行當下安裝版本的實際資源為準，計畫中列的名稱若有出入，以「語意對應」原則就近選用
- `tools/` 路徑不觸發 build-ci.yml，正確性以本機 build + 手動冒煙為準
- 單一交付，不分階段；完成後直接 commit main（本機可驗證環境）
