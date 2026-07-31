# Avalonia 規範

本檔記 bee-library 內 Avalonia 相關專案（`src/Bee.UI.Avalonia`、`tools/DefineEditor`、`samples/Avalonia.*`、`apps/Bee.Northwind` 四頭）的硬性規則與已知雷區。

> Avalonia 行動 head（`net10.0-ios` / `net10.0-android`）的 trim / AOT 序列化雷與解法，見 `rules/apple-mobile-trim.md`。

## 套件版本與相容性

### 升級 Avalonia / UI 套件前，必先評估相關套件相容性

**相容性凌駕「升到最新」**。升級任一 Avalonia / UI 套件（Avalonia 核心族、`Avalonia.Controls.DataGrid`、`Semi.Avalonia`、`Semi.Avalonia.DataGrid`、任何第三方主題 / 控件庫）前，先評估**所有相關 UI 套件**的相容性：

1. **版本可用性** — 每個相關套件是否都有對應目標版。第三方主題（如 Semi）常慢半拍，Avalonia 核心與周邊套件**本來就非同步釋出**，不強求同版號。
2. **runtime 相容** — 主題 / 控件在新核心上是否跑版、樣式錯位。**build 過 ≠ 相容**：全專案 0 error 也證明不了 Semi 主題套在新核心上不出問題，此層只有執行期看得出來。

### 升級原則

- **不立「同版號」硬規則** — 非同步釋出下做不到（核心可能已到 12.1.0，`Avalonia.Controls.DataGrid` / `Semi.Avalonia` 只到 12.0.x）；依賴為 NuGet min-version 語意，混版可 restore，但 restore 過不代表主題相容。
- **不無腦升核心到最新** — Semi 慢半拍時，寧可整組停在 **`Semi.Avalonia` 能支援的版本線**求穩，不讓 Avalonia 核心超前 Semi。
- **整組一起升的時機** — 等所有相關套件都出對應版、且能實際 runtime 驗證主題不跑版後，再整組一起升。
- 升級前先掃出全 repo 引用點，確保無遺漏（跨 `src/`、`tools/`、`samples/`、`apps/` 多個 csproj）：

```bash
grep -rn "Include=\"\(Avalonia\|Semi\)" --include="*.csproj" .
```

## 預設主題

開發 Avalonia UI **一律預設套 Semi.Avalonia**（MIT，使用者決策 2026-06-10），不用 FluentTheme、
不自建配色字典。入口 `<StyleInclude Source="avares://Semi.Avalonia/Index.axaml"/>`（v12 在 assembly 根）；
深淺走 `RequestedThemeVariant`；語意色用 Semi token（`SemiColorText0-3` / `SemiColorBackground0-4` /
`SemiColorBorder` / `SemiColorWarning` / `SemiColorDanger`）；主行為按鈕 `Classes="Primary"` +
`Theme="{DynamicResource SolidButton}"`。參考實作：`tools/DefineEditor`。

> 2026-07-09 曾把核心全升 12.1.0（8 個 csproj build 0 error），仍在收尾時**喊停退回**——理由是
> Semi.Avalonia 停在 12.0.3，主題樣式針對舊核心建置，視覺風險不願冒。這是上節「相容性凌駕升最新」
> 的實際案例。

## UI 架構定位

`Bee.UI.Avalonia` 是框架 UI 架構（**繼承原生控件的 field editor + 組合式 GridControl +
FormView/ListView View 層 + lookup 開窗機制**）的**參考設計**。

- **設計新控件預設走「繼承原生控件並改寫」**，別建議「vanilla 控件 + 外部 binding/behavior」的
  反方向——控件本身要懂 `FormField`/`FormSchema` 的 metadata（`MaxLength` / `ListItems` /
  `ReadOnly` / relation→lookup），schema 驅動行為要**內建進子類**。
- **評估 Avalonia 的結構／命名決策時，不要以「和 Blazor 對齊」為由反對**——Avalonia 是領先的
  參考實作，刻意可以歧異；它的決策反而是 Blazor.Server 日後要跟進的範本。
- UI 家族已收斂為 **Avalonia + Blazor.Server 雙軌**（`Bee.UI.Maui` / `Bee.Web.Blazor.Wasm` 已移除），
  **「移植到其他 UI 家族」只剩 Blazor.Server 一個對象**。
- **不要為了消除 Avalonia 與 Blazor.Server 的重複（如 `FormDataObject`）而讓 Blazor 依賴
  `Bee.UI.Core`** —— `docs/dependency-map.md` 明文以「是否消費 `Bee.UI.Core` 抽象」判別
  `Bee.UI.*` family，讓 Blazor 依賴它會牴觸判別基礎本身。已採做法是雙向註解。

## 控件行為的驗收基準

`samples/Avalonia.DemoCenter` 是 `Bee.UI.Avalonia` 控件的展示中心兼**對齊基準**：移植控件到其他
UI head 時，以其每個案例的行為（綁定、唯讀、必填、FormMode、AllowEditModes、Layout、Grid、
Master-Detail）為驗收基準。控件外觀變更先在此目視驗證再回推其他平台。

## 踩過的雷

18 條實證雷（Stretch+MaxWidth 置中、控件語意事件對程式設值不觸發、DataGrid 編輯管線與 popup
編輯器衝突、`StyleKeyOverride` 必修、模板回收造成顯示脫鉤、唯讀外觀 template 的隱藏部件…）
見 `docs/repo-ops/gotchas/avalonia-controls.md`。**改 `Bee.UI.Avalonia` 控件前先讀。**

> 使用者偏好：**改動編譯通過即可交付，由他自行啟動測試**（agent 驅動 UI 太慢）。
