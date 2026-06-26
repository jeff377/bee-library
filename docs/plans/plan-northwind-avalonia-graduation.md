# 計畫：Bee.Northwind 畢業 — 同步至獨立 repo bee-northwind-avalonia

**狀態：🚧 進行中（2026-06-26）**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 0 | 框架發佈 **4.12.0**（畢業前置：把 v4.11.0 後的響應式 + AOT 修正推上 NuGet） | 🚧 進行中 |
| 1 | 同步 source 至 `bee-northwind-avalonia`（補 iOS/Android、ProjectReference→NuGet 4.12.0、slnx、README） | 📝 待做 |
| 2 | 獨立 repo build + 驗證（純 NuGet 4.12.0，四 head） | 📝 待做 |
| 3 | commit + push 至 `bee-northwind-avalonia` | 📝 待做 |

## 背景

`apps/Bee.Northwind`（bee-library 內）已完成 **Desktop / Browser / iOS / Android 四個 Avalonia head**，共用 `Bee.Northwind.UI`。依 [[northwind-graduation-keep-source]] 的決策，畢業作法是**複製到獨立 repo `bee-northwind-avalonia`、bee-library 內保留不刪**（續作 dogfooding）。

目標 repo `https://github.com/jeff377/bee-northwind-avalonia` 已存在，但為 **2026-06-24 舊快照**：
- 只有 **Server / UI / Desktop / Browser** 四個專案（**無 iOS / Android**）。
- 以 NuGet **`Bee.* 4.11.0`** PackageReference 引用框架（畢業 repo 已採 NuGet、非 ProjectReference）。

本計畫把它**升級同步至目前 bee-library 的完整四 head 狀態**。

## 關鍵前置：為何必須先發 4.12.0

`v4.11.0` tag 之後，`src/` 累積了 **9 個 commit**（全在 `Bee.UI.Avalonia` 與 `Bee.Definition`），這些**尚未發佈到 NuGet**：

- Avalonia 響應式（`FormView`/`ListView` 依寬度重排、`ListView` 卡片化、`RowEditDialog` overlay、EditForm 可調整大小、表單垂直捲動、`GridControl` lookup 圖示）。
- **`Bee.Definition` 的 AOT reflection XmlSerializer 相容修正**（`4f4ebebc`，單一 `Add` + 無參數建構子，見 ADR-025）—— **這是 iOS/Android 載入 XML 定義不崩的關鍵**。

> 已發佈的 NuGet `4.11.0` **不含**上述變更。若畢業 repo 引用 `4.11.0`：
> - **iOS / Android 會在載入任何 XML 定義時崩潰**（ADR-025 修掉的那個 bug 重現）。
> - Desktop / Browser 缺響應式佈局。
>
> 因此**畢業 repo 能用的最低 NuGet 版本是 4.12.0**，必須先發佈。

## 範圍外（明確不做）

- **不刪除** bee-library 的 `apps/Bee.Northwind`（[[northwind-graduation-keep-source]]：保留續作 dogfooding，要刪另案）。
- 獨立 repo 的 **CI workflow（build-ci）** 不在本期；列為日後選用 follow-up。
- iOS/Android 的 **Release / trim-safe / 上架簽章**：同 bee-library，Debug-first，另案。

---

## 階段 0：框架發佈 4.12.0（前置）

依 `~/.claude/rules/releasing.md` 流程：

1. **整理 CHANGELOG**：`/changelog-draft` 從 `v4.11.0` 至 HEAD 統整雙語大綱（範圍即上述 9 個 Avalonia/Definition commit + sonar-fix）。使用者 review 後 commit。
2. 確認 `build-ci.yml` 在 main 上通過。
3. 更新 `src/Directory.Build.props` 的 `<Version>` / `<AssemblyVersion>` / `<FileVersion>` → `4.12.0`。
4. commit & push to main。
5. 推送 tag `v4.12.0`（`git tag v4.12.0 && git push origin v4.12.0`）。
6. 確認 GitHub Actions `nuget-publish.yml` 成功；驗證 nuget.org 上 `Bee.UI.Avalonia` / `Bee.Hosting` / `Bee.Db` / `Bee.Business` / `Bee.Api.AspNetCore`（及其相依）皆有 `4.12.0`。

**完成準則**：nuget.org 出現全套 `Bee.* 4.12.0`，可被外部專案還原。

## 階段 1：同步 source 至 bee-northwind-avalonia

1. 在工作目錄外 clone 目標 repo：`git clone https://github.com/jeff377/bee-northwind-avalonia`。
2. 以 bee-library `apps/Bee.Northwind/` 目前內容**覆蓋**目標 repo 的對應專案，並**新增** iOS / Android 兩個 head。需同步的內容：

   | 來源（bee-library `apps/Bee.Northwind/`） | 目標 repo | 動作 |
   |------|------|------|
   | `Bee.Northwind.UI/`（含 MainView BackRequested / safe area、FormsView TryHandleBack、FormWorkspace TryGoBack 等） | 同名 | 覆蓋 |
   | `Bee.Northwind.Desktop/` `Bee.Northwind.Browser/` `Bee.Northwind.Server/` | 同名 | 覆蓋 |
   | `Bee.Northwind.iOS/` `Bee.Northwind.Android/` | （新） | **新增** |
   | `Define/`、`README.md`、`README.zh-TW.md`、`.smoke.yaml` | 同名 | 覆蓋 |

3. **ProjectReference → PackageReference 轉換**（這是獨立 repo 與 bee-library 的唯一結構差異）：

   | 專案 | bee-library 內引用 | 獨立 repo 改為 |
   |------|------|------|
   | `Bee.Northwind.Server` | ProjectReference `src/Bee.Api.AspNetCore`、`Bee.Business`、`Bee.Db`、`Bee.Hosting` | PackageReference `4.12.0` |
   | `Bee.Northwind.UI` | ProjectReference `src/Bee.UI.Avalonia` | PackageReference `4.12.0` |
   | `Desktop` / `Browser` / `iOS` / `Android` | ProjectReference `Bee.Northwind.UI`（內部） | **不變**（維持內部 ProjectReference） |

   - 既有 Server / UI 已是 `4.11.0` PackageReference → 升 `4.12.0`。
   - 新增的 iOS / Android head 只內部引用 `Bee.Northwind.UI`，**不直接碰 src**，無需轉換。
   - 全 repo grep 確認無殘留 `..\..\..\src\` 路徑。

4. **slnx**：在目標 repo `Bee.Northwind.slnx` 的 `/UI/` 資料夾補 iOS / Android 兩列（對齊 bee-library slnx 結構）。
5. **.vscode**：目標 repo 既有 06-24 的 launch/task（Desktop/Browser）。iOS/Android 的 launch 設定列為選用補充，不阻擋本階段。

**完成準則**：目標 repo 工作目錄含完整四 head + Server + UI + Define，所有框架引用為 NuGet `4.12.0`，無 `src/` 相對路徑殘留。

## 階段 2：獨立 repo build + 驗證

於 clone 的目標 repo 內（純 NuGet，不依賴 bee-library）：

1. `dotnet restore`（確認能從 nuget.org 取得 `Bee.* 4.12.0`）。
2. Build 各 head：
   - Desktop：`dotnet build Bee.Northwind.Desktop -c Debug`
   - Browser：`dotnet build Bee.Northwind.Browser -c Debug`（需 `wasm-tools`）
   - iOS：`dotnet build Bee.Northwind.iOS -f net10.0-ios -c Debug`（需 `ios` workload + Xcode）
   - Android：`dotnet build Bee.Northwind.Android -f net10.0-android -c Debug`（需 Android SDK + JDK17，本機已備）
   - Server：`dotnet build Bee.Northwind.Server -c Debug`
3. 冒煙（最低）：Server + Desktop 起得來、連線→登入→清單真實資料（沿用 bee-library 已驗證行為；只是改用 NuGet 套件）。iOS/Android 視需要在模擬器各跑一次（工具鏈已就緒）。

**完成準則**：四 head + Server 在獨立 repo 以 NuGet `4.12.0` build 通過；Desktop 端到端冒煙通過。

## 階段 3：commit + push 至 bee-northwind-avalonia

1. 在目標 repo commit（訊息：`feat: 同步至框架 4.12.0 + 新增 Avalonia iOS / Android head`，繁中 body 摘要四 head 完成）。
2. push 到 `bee-northwind-avalonia` 的 `main`。
3. 確認 GitHub 上反映完整四 head。

**完成準則**：`bee-northwind-avalonia` main 為最新四 head + NuGet `4.12.0`，與 bee-library `apps/Bee.Northwind` 內容一致（差異僅 ProjectReference vs PackageReference）。

## 完成後

- 本檔頂部標記 ✅ 與日期，階段表格逐列更新。
- `bee-northwind-avalonia` 成為對外可獨立 clone + `dotnet build`（純 NuGet）的四 head 範例 repo。
- bee-library `apps/Bee.Northwind` 保留不動（dogfooding）。
- 視需要更新 [[northwind-graduation-keep-source]] 記憶（畢業已落地、保留來源）。
