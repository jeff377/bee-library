# 計畫：SQLitePCLRaw 2.x → 3.x 升級（解除 NU1903 抑制）

**狀態：✅ 已完成（2026-06-19）**

> 結果：三處直接引用 `Microsoft.Data.Sqlite` 的專案 pin `SQLitePCLRaw.bundle_e_sqlite3 3.0.3`，覆寫 transitive 2.1.10；兩處 `NuGetAuditSuppress` 已移除。全套件 restore 0 NU1903，`./test.sh` 全綠（SQLite in-memory round-trip + 其他 DB 容器測試全過），確認 MDS 9.0.4 與 SQLitePCLRaw core 3.0.3 runtime 相容、SQLite 行為無回歸。

## 背景

2026-06-19 GitHub 發布安全告警 **GHSA-2m69-gcr7-jv3q**（HIGH）：`SQLitePCLRaw.lib.e_sqlite3`（內嵌的 SQLite native 引擎）有已知漏洞，`vulnerableVersionRange: <= 2.1.11`、**`firstPatchedVersion: null`**（2.1.x 沒有修補版）。

該套件由 `Microsoft.Data.Sqlite 9.0.4` 透過 `SQLitePCLRaw.bundle_e_sqlite3 2.1.10` 間接帶入，命中三處直接引用 `Microsoft.Data.Sqlite` 的專案：

- `tests/Bee.Tests.Shared`（→ 所有 `*.UnitTests` 經 project ref）
- `samples/Bee.Samples.Shared`
- `apps/Bee.Northwind/Bee.Northwind.Server`

NuGet audit + `TreatWarningsAsErrors` 下，`dotnet restore` 直接失敗，`build-ci.yml` 在 main 紅燈。

## 已採取的暫時措施（已 commit）

在 `tests/Directory.Build.props` 與根 `Directory.Build.props` 加 `NuGetAuditSuppress GHSA-2m69-gcr7-jv3q` 解鎖 CI（維持 2.1.10 / MDS 9.0.4 不動）。本計畫追蹤真正的版本升級以解除抑制。

## 待評估的升級路徑

唯一非漏洞版本是 native `lib.e_sqlite3 3.50.3`（SQLitePCLRaw 將 native 版號對齊 SQLite 版本）；managed `bundle_e_sqlite3` 最新為 `3.0.3`。即需 **SQLitePCLRaw 2.x → 3.x 主版本升級**。

選項：
1. **pin `SQLitePCLRaw.bundle_e_sqlite3 3.0.3`**（覆寫 transitive）—— 驗證 `Microsoft.Data.Sqlite 9.0.4` 與 SQLitePCLRaw 3.x（core API）相容。
2. **升級 `Microsoft.Data.Sqlite`** 至本身依賴修補版 SQLitePCLRaw 3.x 的版本（若有）。

## 驗收條件（升級後必過）

- `dotnet restore` 無 NU1903，移除兩處 `NuGetAuditSuppress`。
- 跨 DB 測試全綠，**特別是 SQLite 路徑**：`[DbFact]` SQLite 測試、`Bee.Northwind` SQLite 種子/查詢。
- 確認 SQLite 行為無回歸——重點 **GUID 大小寫 / TEXT 儲存**（見 [[sqlite-guid-text-case-sensitivity]]，本 repo 已踩過 master-detail 子列孤兒問題）。
- 本機 `./test.sh` + CI `build-ci.yml` 通過。

## 備註

- 漏洞為內嵌 SQLite 引擎的 CVE；本 repo SQLite 僅用於測試與 Northwind sample/app，非主要 production DB（SQL Server / PostgreSQL / MySQL / Oracle）。
- 升級屬獨立任務，與 Avalonia.DemoCenter 無關（後者觸發 CI 時才連帶曝光此既有相依問題）。
