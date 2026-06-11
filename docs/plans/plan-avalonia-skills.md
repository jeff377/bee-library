# 計畫：新增 Avalonia 相關 skill（i18n + macOS bundle）

**狀態：✅ 已完成（2026-06-10）**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 1 | `avalonia-i18n` skill — runtime localization 標準做法 + 雷區 | ✅ 已完成（2026-06-10） |
| 2 | `avalonia-macos-bundle` skill — publish.sh + .app 打包 + icon + Gatekeeper | ✅ 已完成（2026-06-10） |

## 背景

第一次完整跑 Avalonia 12 開發（DefineEditor）過程中踩了多個雷，希望沉澱為跨專案可重用的 skill。

## Skill 1：`avalonia-i18n`

### 觸發描述（trigger description）

「Avalonia app 要支援多語系（live switching, runtime culture change）— 含 axaml 內字串、ViewModel StatusText / dialog 訊息、macOS 系統 menu。當使用者要『加入多語系』、『i18n』、『localization』、『支援中英文切換』、『resx』之類需求時使用。涵蓋 IObservable.ToBinding() pattern（DataTemplate 內 binding 唯一可靠的方式）、VM stored-state 雷（StatusText 不會自動翻譯）、macOS NSMenu top-level header 凍結限制。」

### Skill 內容大綱

1. **架構決策**
   - `LocalizationService` singleton + `ResourceManager`
   - 英文作 neutral default（`Strings.resx`），各 culture 用 `Strings.{lang}.resx`
   - `UserSettings` 持久化（`ApplicationData/<App>/settings.json`）
2. **`{loc:Loc Key}` markup extension 正解**
   - 返回 `IObservable<string>.ToBinding()`（needs `using Avalonia;` + `using Avalonia.Reactive;`）
   - **錯路備忘**（避免重踩）：`INotifyPropertyChanged + Item[]`、indexer binding、自訂 holder + named property binding 都不夠
3. **ViewModel stored state 雷**
   - `StatusText = L(key)` 是 snapshot；culture change 不會自動翻譯
   - 解法：VM ctor 訂閱 `CultureChanged`，記 `_lastDefaultHint`，只在 status 未被覆蓋時 reset
4. **macOS NativeMenu**
   - App-level vs Window-level 雙路設計（about/hide/quit 折進 app menu；File / View 變 top-level）
   - leaf items 用 per-item `CultureChanged` subscribe + 重設 `Header`
   - **頂層 (sub-menu-bearing) NativeMenuItem 的 Header 不可 dynamic update**；重 SetMenu 會 crash
   - 接受「頂層英文」trade-off
5. **`Validators / tree detail / placeholder` 處理策略**
   - dev-tool 慣例：英文 inline，不放 resx（避免 resx 表膨脹）

### 範例參考

- [Jeek.Avalonia.Localization](https://github.com/tifish/Jeek.Avalonia.Localization)
- 本專案 commit：`c5ba472d` (infrastructure)、`88708f68` (live switch fix)、`e873ad3b` (native menu)

## Skill 2：`avalonia-macos-bundle`

### 觸發描述

「Avalonia app 要發佈成 macOS `.app` bundle 讓使用者雙擊開啟（含 single-file + 圖示 + Gatekeeper 解除指引）。當使用者要『打包 macOS app』、『.app bundle』、『publish to Mac』、『讓 Mac 使用者下載執行』、『macOS app icon』之類需求時使用。涵蓋 publish.sh `--app-bundle` 設計、Info.plist 結構、`Bee.Base.AssemblyLoader` 之類 IL3002 single-file 雷、icon 用 Swift + iconutil 從 emoji / 幾何渲染、macOS Gatekeeper xattr quarantine 解除。」

### Skill 內容大綱

1. **publish.sh 設計**
   - 4-RID 矩陣（osx-arm64 / osx-x64 / win-x64 / linux-x64）
   - 三個正交旗標：`--self-contained` / `--single-file` / `--app-bundle`
   - 預設 framework-dependent（要求 .NET runtime）
2. **`.app` 目錄結構**
   - `Contents/MacOS/` — 主執行檔 + native dylib
   - `Contents/Resources/AppIcon.icns`
   - `Contents/Info.plist` — CFBundleExecutable / Identifier / Version / Icon / LSMinimumSystemVersion / NSHighResolutionCapable
3. **icon 生成（macOS 內建工具）**
   - Swift script 用 AppKit NSBitmapImageRep 渲染多尺寸 PNG
   - `iconutil -c icns` 打 .icns
   - 範例：emoji rendering、幾何圖塊（squircle 22.5% radius、honeycomb）
4. **single-file / trim 雷**
   - `PublishSingleFile=true` 對 `Module.Name`（`Bee.Base.AssemblyLoader`）等 single-file-unsafe API 報 IL3002 strict build 失敗
     - fix：用 `Assembly.GetName().Name` 取代
   - `PublishTrimmed=true` 對 XmlSerializer 反射不友善 — 保持 off
   - native dylib 不能 bundle（.NET 規格限制）
5. **Gatekeeper / quarantine**
   - 跨機器傳輸 .app 後 `xattr -d com.apple.quarantine .app` 解除
   - 或 Finder 右鍵 → 打開
6. **發佈策略給「一般開發者」**
   - framework-dependent + single-file + .app bundle 是甜蜜點（~30 MB）
   - 雙 RID（arm64 + x64）各一份；不做 universal binary（複雜）

### 範例參考

- 本專案 [tools/DefineEditor/publish.sh](../../tools/DefineEditor/publish.sh)
- 本專案 [tools/DefineEditor/scripts/build-icon.swift](../../tools/DefineEditor/scripts/build-icon.swift)
- 本專案 commits：`c9e2e923` (IL3002 + `--single-file`)、`67213d95` (`.app` bundle)、`402deace` (icon)

## 實作方式

兩個 skill 都用 `anthropic-skills:skill-creator` 建立，落地在 `~/.claude/skills/` 全域可用（跨 repo）。
