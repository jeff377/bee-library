# Bee.UI.Avalonia：UI 架構定位與控件規範

本檔在 agent 觸及 `src/Bee.UI.Avalonia/` 下任何檔案時自動載入（巢狀 `CLAUDE.md` 為 lazy loading）。

**套件版本相容性那條留在 `.claude/rules/avalonia.md`（常駐）** —— 它的適用面是
四個 Avalonia 頭的**任一個 csproj**（本專案、`tools/DefineEditor`、`samples/Avalonia.*`、
`apps/Bee.Northwind`），不是只有這裡。

行動 head（`net10.0-ios` / `net10.0-android`）的 trim / AOT 雷見
`.claude/rules/apple-mobile-trim.md`。

## 預設主題：Semi.Avalonia

開發 Avalonia UI **一律預設套 Semi.Avalonia**（MIT，使用者決策 2026-06-10），不用 FluentTheme、
不自建配色字典。入口 `<StyleInclude Source="avares://Semi.Avalonia/Index.axaml"/>`（v12 在 assembly 根）；
深淺走 `RequestedThemeVariant`；語意色用 Semi token（`SemiColorText0-3` / `SemiColorBackground0-4` /
`SemiColorBorder` / `SemiColorWarning` / `SemiColorDanger`）；主行為按鈕 `Classes="Primary"` +
`Theme="{DynamicResource SolidButton}"`。參考實作：`tools/DefineEditor`。

## UI 架構定位

本專案是框架 UI 架構（**繼承原生控件的 field editor + 組合式 GridControl +
FormView/ListView View 層 + lookup 開窗機制**）的**參考設計**。

- **設計新控件預設走「繼承原生控件並改寫」**，別建議「vanilla 控件 + 外部 binding/behavior」的
  反方向 —— 控件本身要懂 `FormField`/`FormSchema` 的 metadata（`MaxLength` / `ListItems` /
  `ReadOnly` / relation→lookup），schema 驅動行為要**內建進子類**。
- **評估結構／命名決策時，不要以「和 Blazor 對齊」為由反對** —— Avalonia 是領先的參考實作，
  刻意可以歧異；它的決策反而是 Blazor.Server 日後要跟進的範本。
- UI 家族已收斂為 **Avalonia + Blazor.Server 雙軌**（`Bee.UI.Maui` / `Bee.Web.Blazor.Wasm` 已移除），
  **「移植到其他 UI 家族」只剩 Blazor.Server 一個對象**。
- **不要為了消除 Avalonia 與 Blazor.Server 的重複（如 `FormDataObject`）而讓 Blazor 依賴
  `Bee.UI.Core`** —— `docs/dependency-map.md` 明文以「是否消費 `Bee.UI.Core` 抽象」判別
  `Bee.UI.*` family，讓 Blazor 依賴它會牴觸判別基礎本身。已採做法是雙向註解。

## 控件行為的驗收基準

`samples/Avalonia.DemoCenter` 是本專案控件的展示中心兼**對齊基準**：移植控件到其他
UI head 時，以其每個案例的行為（綁定、唯讀、必填、FormMode、AllowEditModes、Layout、Grid、
Master-Detail）為驗收基準。控件外觀變更先在此目視驗證再回推其他平台。

## 踩過的雷

實證雷（Stretch+MaxWidth 置中、控件語意事件對程式設值不觸發、DataGrid 編輯管線與 popup
編輯器衝突、`StyleKeyOverride` 必修、模板回收造成顯示脫鉤、唯讀外觀 template 的隱藏部件…）
見 `../../docs/repo-ops/gotchas/avalonia-controls.md`。**改本專案控件前先讀。**

> 使用者偏好：**改動編譯通過即可交付，由他自行啟動測試**（agent 驅動 UI 太慢）。
