# 計畫：讓 framework repository 的多 provider 測試真的打到該 provider

**狀態：✅ 已完成（2026-09-08）**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 1 | 測試側新增 provider-scoped router，讓 Common/Log scope 的 repository 測試可指定實體資料庫 | ✅ 已完成（2026-09-08） |
| 2 | 改寫 A 類（宣告 X 只打 SQL Server）與 B 類（空轉通過）的 repository 測試 | ✅ 已完成（2026-09-08） |
| 3 | 改正 BO 層 21 支宣告錯誤的 `[DbFact(DatabaseType.SQLite)]` | ✅ 已完成（2026-09-08） |
| 4 | 以暫時停用 `ApplyOracleBindByName` 驗證 Oracle 那軸會紅在該紅的地方 | ✅ 已完成（2026-09-08） |

## 背景

`RepositoryDatabaseRouter.Resolve` 對 `DbScope.Common` / `DbScope.Log` 回傳固定的
`"common"` / `"log"`。測試 fixture 在 `SharedDatabaseState.EnsureFallbackCommonDatabaseItem`
把 `"common"` 註冊成 **SQL Server**，各 provider 的實體資料庫則另以
`TestDbConventions.GetDatabaseId` 註冊為 `common_sqlserver` / `common_oracle` / …。

於是所有透過 `DbScope.Common` 取得連線的 framework repository
（`SessionRepository`、`UserRepository`、`CompanyRepository`、`UserCompanyRepository`、
`ApiKeyRepository`、`DatabaseRepository`）上的
`[DbFact(DatabaseType.Oracle)]` / `[DbFact(DatabaseType.PostgreSQL)]` / `[DbFact(DatabaseType.MySQL)]`
**實際執行的都是 SQL Server**，attribute 只剩「env var 有設才跑」的閘門作用。

2026-09-08 修掉的 Oracle 參數綁定 bug（`OracleCommand.BindByName` 預設 `false`，
佔位符非遞增順序時綁錯欄位，commit `c53bc0e3`）就藏在 `SessionRepository.UpdateSession`。
那條路徑在 Oracle 上等於從未被驗證過，而測試報表看起來是有涵蓋的 —— 這比沒有測試更糟。

## 盤點方法與結果

**用執行判定，不用 grep 推理。** 在 `DbConnectionManagerService.GetConnectionInfo`
（`DbAccess` 取連線的唯一咽喉）插入暫時性探針，記錄 `databaseId` + `DatabaseType`
+ 由 stack trace 反查的測試方法名，跑完 6 個會碰 DB 的測試專案
（Repository / Business / Db / Hosting / Api.Client / Api.Core，共 3,344 筆全綠），
再把「`[DbFact(X)]` 宣告的 X」與「實際碰到的 `DatabaseType`」對帳。探針事後移除。

73 個宣告 `[DbFact]` 的 test class 中，20 個有問題，分四類：

### A 類 — 宣告 X、只打到 SQL Server（50 支）

| Test class | 支數 | 主體 |
|---|---|---|
| `ApiKeyRepositoryTests` | 9 | `ApiKeyRepository`（Common scope） |
| `CompanyRepositoryTests`（Enabled / NotFound） | 8 | `CompanyRepository.GetById` |
| `UserCompanyRepositoryTests`（Granted / NotGranted） | 8 | `UserCompanyRepository.HasAccess` |
| `UserRepositoryTests` | 4 | `UserRepository.GetRowIdBySysId` |
| `SystemBusinessObjectApiKeyLifecycleTests` | 9 | BO 層 |
| `SystemBusinessObjectApiKeyTests` | 5 | BO 層 |
| `SystemBusinessObjectDeploymentAuditTests` | 5 | BO 層 |
| `SystemBusinessObjectDeploymentAdminTests` | 2 | BO 層 |

`UserRepositoryTests` 的 `private void RunRoundTrip(DatabaseType _)`（參數直接丟棄）
是最明顯的徵狀。後四組（BO 層 21 支）更糟一層：宣告 `SQLite` 卻實跑 SQL Server ——
閘門看的是 `BEE_TEST_CONNSTR_SQLITE`、跑的卻是 SQL Server，兩邊都錯。

### B 類 — arrange 打對 provider、act 讀 SQL Server ⇒ 空轉通過（8 支）

`CompanyRepositoryTests.GetById_Disabled_*` 與
`UserCompanyRepositoryTests.HasAccess_GrantedDisabled_*`（各 4 支非 SQLServer）。
它們用 `_fx.NewDbAccess(TestDbConventions.GetDatabaseId(dbType))` 把 disabled company
寫進 `common_oracle`，再由 `CreateRepo()`（走 router → `common` → SQL Server）讀，
斷言 `null` / `false`。**把 `enabled` 過濾整條拿掉，這些測試照樣綠。**

### C 類 — MIX 但合法，不改

`FormBusinessObject*`、`AuditRuleFormTests`、`CacheNotifyPollerTests`：主體走
company scope，由 `CompanyInfo.CompanyDatabaseId` 正確路由到 `company_oracle` /
`common_mysql`；順帶碰到的 `common` 只是 session / user 查詢。

### D 類 — 宣告了 DB 卻從不連線，本次不動

`BuildSelectTests` 7 支（純 SQL 產生）、`TenantCustomizationEndToEndTests` 4 支、
`SystemBusinessObjectExtraTests.ExecFunc_TestConnection_ValidDatabaseItem_Succeeds` 1 支。
覆蓋沒有少，只是 attribute 語意不實；改成 `[Fact]` 屬於另一個判斷，留待後續。

### 另記：`SessionRepositoryTests` 8 支全部宣告 `SQLServer`

也就是 `c53bc0e3` 修掉的 `UpdateSession` bind-by-name bug，在 Oracle 上**連名義覆蓋都沒有**
—— 比「名義上有、實際打 SQL Server」還空一階。本計畫要一併補上。

## 修法決策

考慮過兩條路：

- **只動測試側**：`TestRepositoryContext.Create` 已有 `router` 參數，新增一個把
  Common/Log 解析成 `{category}_{dbtype}` 的 router 即可。零 `src/` 改動、零公開 API 變更。
- **動 `src/`**：為各 framework repository 開 public `(ctx, progId, databaseId)` 建構式
  （`RepositoryBase` 的 protected 版已存在，`AuditLogRepository` / `DataFormRepository`
  已有 public 前例）。純新增、二進位相容，但公開 API 表面 +6 型別。

**採第一條。** 這些測試要驗的是「repository 產生的 SQL 在該 provider 上跑不跑得起來」，
而 router 的職責是 scope → databaseId，兩者正交；`RepositoryDatabaseRouterTests` 本來就
專門覆蓋正式 router 的行為。為了測試而擴張公開 API 表面，換到的只是同一件事的另一種寫法。

BO 層 21 支不擴成四家 provider：它們測的是 common scope 的 BO 行為，一家 provider 足夠。
只把 `[DbFact(DatabaseType.SQLite)]` 改成 `SQLServer`，讓閘門與實跑對齊、不再說謊。

## 執行步驟

### 階段 1 — 測試側 provider-scoped router

在 `tests/Bee.Tests.Shared/` 新增 `ProviderScopedRouter`（或掛在
`TestRepositoryContext` 上的工廠方法），把 `DbScope.Common` / `DbScope.Log` 解析成
`TestDbConventions.GetDatabaseId(dbType, "common" | "log")`，`DbScope.Company` 沿用
既有 `FixedRouter` 的行為。

### 階段 2 — 改寫 A / B 類 repository 測試

四個 test class 的 `CreateRepo()` 改為 `CreateRepo(DatabaseType dbType)`，
`RunXxx(DatabaseType _)` 的參數不再丟棄。B 類的 arrange 與 act 因而落在同一個資料庫。
`SessionRepositoryTests` 補上其餘四家 provider 的 `[DbFact]`。

### 階段 3 — 改正 BO 層 attribute

21 支 `[DbFact(DatabaseType.SQLite)]` → `[DbFact(DatabaseType.SQLServer)]`，
`[DisplayName]` 若提到 SQLite 一併改。

### 階段 4 — 驗證會紅在該紅的地方

暫時 revert `src/Bee.Db/DbCommandSpec.cs` 的 `ApplyOracleBindByName`，確認
`SessionRepositoryTests` 的 Oracle 那支會紅；還原後轉綠。這一步的用意是證明
新的覆蓋不是另一種形式的空轉。

## 前置與注意事項

- 四個 DB 容器要在跑（`./test.sh` 那組），連線字串在 `.runsettings`。
- **Oracle 環境**：`tools/Bee.LoadTests` 與單元測試共用同一個 `testuser` schema，
  跑過 `prepare --provider Oracle` 之後單元測試 fixture 會整組失敗於
  `Change narrows a column`。復原步驟見 [../repo-ops/gotchas/database.md](../repo-ops/gotchas/database.md)。
- 測試規範見 [../../tests/CLAUDE.md](../../tests/CLAUDE.md)。
- 本計畫的成因背景見 [plan-paging-and-oracle-getlist.md](plan-paging-and-oracle-getlist.md)
  的「仍未處理（不在本次範圍）」。
- 改動觸及跨 provider 的測試矩陣，push 前應建議跑完整模式 CI（`[all-db]`）。

## 執行結果

### 改對之後浮出的兩個框架缺陷

覆蓋一旦真的落到各 provider，兩個缺陷當場現形——兩者都是「只在 SQL Server 上跑過」才活得下來的。

**1. Common scope repository 依 `DbCategoryIds.Common` 而非自身 `DatabaseId` 決定 SQL 方言**

`UserRepository` / `CompanyRepository` / `UserCompanyRepository` / `ApiKeyRepository`
以 `GetConnectionInfo(DbCategoryIds.Common).DatabaseType` 取方言去 `QuoteIdentifier`，
指令卻對 `DatabaseId` 執行。兩者在正式部署恆等（框架強制 common 的 `Id == CategoryId == "common"`），
所以這不是既有部署的行為缺陷；但方言來源是寫死的字面值而不是這個 repository 自己的目標，
測試一把 scope 指到別的引擎就整批 `syntax error at or near "["`。
已改為一律取自 `DatabaseId`——`DataFormRepository` 與 `AuditLogRepository` 本來就是這樣寫的。
`AuditRuleRepository.NotifyRulesChanged` 維持 `DbCategoryIds.Common` 不動：它寫的是
cache-notify 表，本來就在 common，而該 repository 是 null scope、沒有自己的 `DatabaseId`。

**2. SQLite 上日期欄被 `is DateTime` 判掉，金鑰到期時間憑空消失**

`ApiKeyRepository.GetEnabledById` / `GetList` 以 `expiredAt is DateTime dt ? dt : null` 讀
`expired_at`。SQLite 沒有日期型別、驅動在臨機查詢下交回 `string`，於是有到期時間的金鑰讀成
`ExpiredAt = null`，`ApiKeyInfo.IsExpired` 永遠為 false——**已過期的金鑰照樣通行，且無聲**。
改走 `ValueUtilities.CDateTime(object?)`。這與 `c53bc0e3` 修掉的 Oracle `RAW(16)` / `is Guid`
是同一個形狀：驅動交回的 CLR 型別不是宣告型別，而裸 `is T` 把「型別不符」與「值不存在」
壓成同一個答案。兩則都記進 [../repo-ops/gotchas/database.md](../repo-ops/gotchas/database.md)。

### 階段 4 的驗證

暫時讓 `DbCommandSpec.ApplyOracleBindByName` 早退，跑 `SessionRepositoryTests`：

```
失敗 SessionRepositoryTests.UpdateSession_OverwritesCompanyId_Oracle
  ORA-00932: 表示式 (:1) 為 TIMESTAMP 資料類型, 與預期的資料類型 BINARY 不相容
```

**只有這一支紅**——其餘 39 支的佔位符順序是遞增的，位置綁定與名稱綁定同義。
還原後 40/40 綠。新的覆蓋確實落在缺陷所在，而不是另一種形式的空轉。

### 測試數變化

`Bee.Repository.UnitTests` 187 → 219（+32，主要來自 `SessionRepositoryTests` 8 → 40）。
BO 層 21 支的閘門由 `SQLite` 改為 `SQLServer`，數量不變。

### 未處理

- D 類 12 支（宣告了 DB 卻從不連線）本次不動，見上方盤點。
- BO 層那 21 支維持單一 provider；要不要擴成四家，另案依風險判斷。
- `Bee.Api.AspNetCore.UnitTests` 有 2 支在**本機**失敗（`ExecFunc_Hello_ReturnsNotNull`、
  `ApiKeyGateControllerTests.Post_NoValidatorRegistered_UsesPresenceCheck`，
  皆為預期 `ContentResult` 實得 `ObjectResult`）。以 `git stash` 在乾淨的 `1c60bcf1` 上重跑
  仍紅，與本計畫無關；但**完整模式 CI 上這個組件 33/33 全綠**（run 34183769320），
  所以它是 macOS 本機環境特有的失敗，不是 main 壞了。另案查明環境差異。

### CI 驗證

完整模式 run 34183769320 全綠（`Resolve database scope` → 四家 DB + SonarCloud + Mobile AOT gate）。
`Bee.Repository.UnitTests` 在 CI 上同樣是 **219 筆、零 skip**——新增的 Oracle / PostgreSQL / MySQL
三軸確實在 CI 上執行了，不是被 env var 閘門跳過。
