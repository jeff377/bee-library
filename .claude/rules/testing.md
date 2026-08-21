# 測試規範（骨幹）

> **要動 `tests/` 下任何檔案前，先 Read `tests/CLAUDE.md`** —— 完整規範在那裡，
> 觸及該目錄時會自動載入，但**新建檔案不一定觸發**，所以請主動讀一次。
> 可貼用的程式碼樣板另見 `docs/repo-ops/testing-patterns.md`。
>
> 本檔只留「動筆前必須知道、晚載入就來不及」的部分。

- **xUnit** v2.9.3 + **coverlet**；全域 `<Using Include="Xunit" />` 已設定。
- 每個 `src/<Module>` 對應 `tests/<Module>.UnitTests`，共用工具在 `tests/Bee.Tests.Shared/`。
- 方法命名 `<方法名稱>_<情境>_<預期結果>`（`ValidateToken_ExpiredToken_ReturnsFalse`），
  一律加 `[DisplayName]` 中文描述。
- 每個測試只驗證**一個行為**；加密／雜湊等安全邏輯**必須**有測試；新增公開 API 同步補測試。

## 五條選錯就會付代價的決定

這幾條是在**寫第一行之前**就決定的，所以留在常駐區：

1. **會碰 DB 就用 `SharedDbFixture`，不是 `BeeTestFixture`。**
   後者**不建 schema**，只有在「別的測試類別剛好先把表建好」時才會通過 ——
   這是「本機綠、CI 紅」的頭號成因。**讀取也算碰 DB**（未植入 cache 的 token
   會讓 server 走 rebuild 路徑讀 `st_session`）。
2. **需要資料庫用 `[DbFact(DatabaseType.X)]` / `[DbTheory]`，不要用 `[Fact]`。**
   它依 `BEE_TEST_CONNSTR_{DBTYPE}` 未設值自動跳過；用 `[Fact]` 則會在缺容器的環境紅掉。
   純邏輯／序列化測試**不適用**——那種有 bug 該直接修，不該跳過。
3. **禁止修改 production 的 `static`**（fixture 初始化除外）。xUnit 不同 test class 平行執行，
   碰同一個 static 必然 race；`try/finally` 還原只在串行下成立。無法避免時所有相關 class
   掛同一 `[Collection]`，且**用 `const` 不要用字串字面值**（打錯字會建出沒人共用的隱式分組，
   看起來有序列化實際沒有，且不會編譯錯）。
4. **不得寫入 `tests/Define/`** —— 那是多專案共用的固定資料，被 round-trip 改寫就會連鎖壞掉。
   任何 `SaveDefine` 系列呼叫必須先切到隔離 temp 目錄。
5. **容器不在跑時，禁止為了「讓測試過」而改測試碼或 src。** 先確認容器狀態再判斷。

## 跑測試

`./test.sh`（會偵測並啟動本機 DB 容器）或 `./test.sh tests/<Proj>/<Proj>.csproj`。
本機環境檢查、例外類型 → 容器的對照表、flaky 判定流程都在 `tests/CLAUDE.md`。

## CI 的資料庫範圍：push 前先問使用者

`build-ci.yml` 預設只跑 **SQL Server + SQLite**（精簡模式，約 3.5 分鐘，且**不跑 SonarCloud**）。
四種資料庫全跑需**明確指定**：commit message（PR 則為 PR 標題）帶 `[all-db]`，
或手動 `workflow_dispatch` 選 `db_scope=all`。

**準備 commit / push 到 `main` 前，一律先問使用者本次要不要跑完整模式。**
不要自行決定 —— 該不該全跑取決於使用者對這次改動的風險判斷，不是從 diff 看得出來的。

需要主動**建議**全跑的訊號：改動觸及 `src/Bee.Db/Providers/**`、`src/Bee.Repository/**`、
`SchemaSyntax` / `DbTypeMapper` / `NormalizeDbType`，或任何 SQL 產生邏輯。
**發版前必須跑一次完整模式。**

漏跑的補救成本很低（手動 dispatch 重跑一次即可），所以這條是條文而非 hook 強制 ——
`PreToolUse` hook 只能 allow/deny、無法互動提問，要強制就得每次 push 都擋一輪，不值得。

> 精簡模式跳過 Sonar 的連帶影響：`/sonar-fix`、`/ci-watch` 要看的是**完整模式**的 run，
> 不是每次 push 的 run。
