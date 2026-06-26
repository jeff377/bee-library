# 計畫：發佈框架 4.12.1（行動端 trim descriptor）並同步 bee-northwind-avalonia 複本

**狀態：✅ 已完成（2026-06-27）**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| A | 發佈框架 **4.12.1** 到 NuGet（內含 `ILLink.Descriptors.xml`） | ✅ 已完成（2026-06-27，nuget-publish success、NuGet.org 已索引） |
| B | 複本 `bee-northwind-avalonia` bump 至 4.12.1 + 同步 README + 驗證行動端 trim | ✅ 已完成（2026-06-27，複本 commit `243fde9`） |

> 階段 B 依賴階段 A 的套件**已發佈且 NuGet.org 已索引**（通常數分鐘）；兩階段不可並行。

---

## 背景

`src/Bee.Definition/ILLink.Descriptors.xml`（行動端 Release trim/AOT 序列化半 A 的修正，見 `plan-mobile-release-trim-safe.md`）在 **v4.12.0 之後**才進 main，尚未隨套件發佈。複本 `bee-northwind-avalonia` 以 NuGet `PackageReference @4.12.0` 消費框架，因此**目前仍拿不到 descriptor**。本案：

1. 發佈含 descriptor 的新框架版本（**4.12.1**）。
2. 複本 bump 到 4.12.1（transitive 帶入 `Bee.Definition` 4.12.1 的 descriptor），並把今天的 README 修正一併同步過去。

### 版本號決策（建議 4.12.1 / patch）

`v4.12.0..HEAD` 的 **唯一 src 變更＝descriptor**（`Bee.Definition.csproj` + `ILLink.Descriptors.xml`，+50 行），其餘全是 docs。屬**加性、無 API 表面變動**的相容性改善 → 依 SemVer 為 **patch**：

- `<Version>` `4.12.0` → **`4.12.1`**
- `<AssemblyVersion>` / `<FileVersion>` `4.12.0.0` → **`4.12.1.0`**

> 若使用者認為「行動端可 Release 打包」屬足以標記的功能，可改 minor（4.13.0）。預設採 patch。

### 複本端事實（盤點結果）

- 複本參考 5 個套件 @4.12.0：`Bee.Api.AspNetCore`、`Bee.Business`、`Bee.Db`、`Bee.Hosting`、`Bee.UI.Avalonia`。
  - `Bee.Definition` 是這些的 **transitive 相依**；同版本發佈下 4.12.1 套件相依 4.12.1 的 `Bee.Definition`，故 **bump 這 5 個即連帶帶入含 descriptor 的 `Bee.Definition` 4.12.1**，不需新增顯式 `Bee.Definition` 參考。
  - descriptor 以 **embedded resource** 內嵌於 `Bee.Definition.dll`，隨組件進 trim 閉包、trimmer 自動套用，transitive 即生效。
- 複本 README 與來源已不同步（今天的「行動端 Release trim」段、表單數 9→8、移除終章預告句與失效 plan 連結尚未過去）。
- **來源 README 有複本會失效的內部連結**，同步時須改寫：
  - `README.md:5` / `README.zh-TW.md` 對應行：`[Bee.NET](../../README.md)` — 框架 README，複本無此相對路徑。
  - `README.md:63`：`[...](../../docs/plans/plan-mobile-release-trim-safe.md)` — 複本無 `docs/plans/`。
  - 其餘 app 內相對連結（`Bee.Northwind.Browser/README.md`、`Bee.Northwind.Server/BusinessObjects/...`）在複本結構相同、可直接保留。

---

## 階段 A：發佈框架 4.12.1

依 `releasing.md` 流程：

- [x] **CHANGELOG**：`[4.12.1]` 雙語精簡條目 + `docs/changelogs/4.12.1.md`/`.zh-TW.md` 明細（連 ADR-025）。
- [x] 確認 `build-ci.yml` 在 main 通過（version-bump commit `afa6e490` run 28253305553 success）。
- [x] 更新 `src/Directory.Build.props` → 4.12.1 / 4.12.1.0。
- [x] commit & push main（`afa6e490`）。
- [x] 打 tag `v4.12.1` 推送 → `nuget-publish.yml` run 28253687747 success。
- [x] NuGet.org 已索引 4.12.1；`Bee.Definition` 4.12.1 dll 確認內含 `ILLink.Descriptors.xml`。

> **發佈不可逆**：套件一經發佈無法刪除，只能 unlist。tag 與版本號須一致（`v4.12.1` → `4.12.1`）。

## 階段 B：同步複本 bee-northwind-avalonia

> 前置：階段 A 的 4.12.1 已可從 NuGet.org 還原。

- [x] **bump 套件版本**：複本 5 個 `Bee.*` `PackageReference` 4.12.0 → 4.12.1。
- [x] **同步 README**（中英）：行動端段、表單數 9→8、移除終章預告句；plan 連結在地化為 bee-library GitHub 絕對網址（複本既有慣例，已無 `../../` 連結）。
- [x] `dotnet restore` + `dotnet build`（Server + Desktop）確認 4.12.1 解析、編譯 0 錯誤。
- [x] **驗證 descriptor 到位（payoff）**：直接檢查還原的 `Bee.Definition` 4.12.1 dll（211,968B 全量）內含 `ILLink.Descriptors.xml` / `preserve="all"` / `Bee.Base.Collections` ✓。
      > full-trim build 驗法因 CLI `-p:TrimMode=full` 傳播到 `Bee.Northwind.UI`（`TreatWarningsAsErrors=true`）觸發其 `ViewLocator` IL2057（`Type.GetType` 反射）而中止——**與 descriptor 無關**，是 flag 傳播 + UI full-trim 的產物。改用 dll 內嵌資源檢查直接證實，更可靠。
- [x] 複本 commit + push（`243fde9`）。

> **附記（非本案範圍）**：`Bee.Northwind.UI/ViewLocator.cs` 的 `Type.GetType` 在「app 組件 full-trim」下有 IL2057；行動 head 出貨走 Debug 或預設 Release trim（SdkOnly，不裁 app 組件）不會踩。若未來要 app 全裁，ViewLocator 需補 trim 註記。

---

## 範圍與非範圍

- **範圍**：發佈 4.12.1；複本 bump + README 同步 + trim 到位驗證。
- **非範圍**：
  - 行動端實機 / 簽章 / App Store（另案，見 `plan-mobile-release-trim-safe.md`）。
  - 複本的 heads / Define / UI 程式碼改動——本次無，只有套件版本與 README。
  - minor/major 版本語意調整（除非使用者覆寫版本決策）。

## 完成定義（DoD）

- NuGet.org 上 `Bee.*` 4.12.1（含 `Bee.Definition` 內嵌 descriptor）已發佈、可還原。
- 複本 `bee-northwind-avalonia` 參考 4.12.1、建置通過、README 與來源一致（連結已在地化）。
- 複本行動端 Release full-trim 下 `Bee.Definition` metadata 保留（descriptor 經 NuGet 生效）。
- `plan-mobile-release-trim-safe.md` 的「複本 sync（release-gated）」項可打勾。
