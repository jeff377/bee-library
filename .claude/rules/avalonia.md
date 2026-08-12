# Avalonia 規範（骨幹）

> 預設主題（Semi.Avalonia）、UI 架構定位、控件驗收基準、控件踩雷指路
> → `src/Bee.UI.Avalonia/CLAUDE.md`（觸及該專案時自動載入）。
> 行動 head（`net10.0-ios` / `net10.0-android`）的 trim / AOT 雷 → `rules/apple-mobile-trim.md`。

本檔只留**跨四個 Avalonia 頭**都適用的那條：`src/Bee.UI.Avalonia`、`tools/DefineEditor`、
`samples/Avalonia.*`、`apps/Bee.Northwind`。

## 升級 Avalonia / UI 套件前，必先評估相關套件相容性

**相容性凌駕「升到最新」**。升級任一 Avalonia / UI 套件（Avalonia 核心族、
`Avalonia.Controls.DataGrid`、`Semi.Avalonia`、`Semi.Avalonia.DataGrid`、任何第三方主題 /
控件庫）前，先評估**所有相關 UI 套件**的相容性：

1. **版本可用性** —— 每個相關套件是否都有對應目標版。第三方主題（如 Semi）常慢半拍，
   Avalonia 核心與周邊套件**本來就非同步釋出**，不強求同版號。
2. **runtime 相容** —— 主題 / 控件在新核心上是否跑版、樣式錯位。
   **build 過 ≠ 相容**：全專案 0 error 也證明不了 Semi 主題套在新核心上不出問題，
   此層只有執行期看得出來。

### 升級原則

- **不立「同版號」硬規則** —— 非同步釋出下做不到；依賴為 NuGet min-version 語意，
  混版可 restore，但 restore 過不代表主題相容。
- **不無腦升核心到最新** —— Semi 慢半拍時，寧可整組停在 **`Semi.Avalonia` 能支援的版本線**
  求穩，不讓 Avalonia 核心超前 Semi。
- **整組一起升的時機** —— 等所有相關套件都出對應版、且能實際 runtime 驗證主題不跑版後，
  再整組一起升。
- 升級前先掃出全 repo 引用點，確保無遺漏（跨 `src/`、`tools/`、`samples/`、`apps/`）：

```bash
grep -rn "Include=\"\(Avalonia\|Semi\)" --include="*.csproj" .
```

> 2026-07-09 曾把核心全升 12.1.0（8 個 csproj build 0 error），仍在收尾時**喊停退回** ——
> 理由是 Semi.Avalonia 停在 12.0.3，主題樣式針對舊核心建置，視覺風險不願冒。
> 這是「相容性凌駕升最新」的實際案例。
