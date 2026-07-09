# Avalonia 規範

本檔記 bee-library 內 Avalonia 相關專案（`src/Bee.UI.Avalonia`、`tools/DefineEditor`、`samples/Avalonia.*`、`apps/Bee.Northwind` 四頭）的硬性規則與已知雷區。

> Avalonia 行動 head（`net10.0-ios` / `net10.0-android`）的 trim / AOT 序列化雷與解法，與 MAUI 共用機制，見 `rules/maui.md`「Apple Release-mode trim 決策樹」。

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
