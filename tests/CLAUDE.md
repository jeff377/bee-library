# 測試規範（完整）

本檔在 agent 讀取 `tests/` 下任何檔案時自動載入（巢狀 `CLAUDE.md` 為 lazy loading，
2026-08-12 由頂層 session 實測確認；**「只 Write 新檔不 Read」是否觸發尚未驗證**，
故常駐區保留「動筆前先 Read 本檔」那句保險）。骨幹與「動筆前必須知道」的五條硬約束在
`.claude/rules/testing.md`（常駐）；可貼用的程式碼樣板在 `docs/repo-ops/testing-patterns.md`。

兩邊有衝突時以本檔為準 —— 常駐那份是摘要。

---

## 本機跑測試前的環境檢查（僅本機 + docker 可用時）

> **適用範圍判定，一行搞定**：`command -v docker` —— 沒輸出就跳過整套規則直接跑測試
> （`[DbFact]` 會依 env var 未設值自動 skip）。**CI 不適用**：`build-ci.yml` 走 service
> container、env vars 由 workflow 注入，本節任何內容都不要帶進 yml。

### 為何要先檢查

`./test.sh` 對「容器不存在 → env var 不設值 → `[DbFact]` 自動 skip」**不會給明顯訊號**，
於是「按計劃 skip」與「該跑卻沒跑」看起來一樣。先檢查才能明確回報「X 個 DB 已 skip 因為
容器 Y 不在」，也才不會把 DB 連線失敗誤判成程式 bug。

### 啟動前檢查

1. **Docker daemon**：`docker ps`。失敗時**告知使用者啟動 Docker Desktop，不要自行
   `open -a Docker`**（agent 拉 GUI 工具耗時且結果不確定）。
   **例外：走 `./test.sh` 不需做這步** —— 它內建 `ensure_docker_daemon`，macOS 上會自動拉起並輪詢等待。
2. **容器存在性**：`docker ps -a --format '{{.Names}}\t{{.Status}}'` 比對
   **`test.sh` 檔頭列出的四個容器**（預設名與 `BEE_TEST_*_CONTAINER` override 都寫在那，
   本檔不複寫以免漂移）。缺任一個就告知使用者「該 DB 的測試會自動 skip」，
   **不要自行 `docker run` 創新容器**（image 版本 / port / volume / 初始 schema 都有約束，
   亂建會撞既有設定）。容器在但 stopped 不需動作，`./test.sh` 會 `docker start`。

### 測試失敗的判別順序（本機情境）

跑完 `./test.sh` 後，若看到下列例外類型，**優先懷疑容器狀態，不要直接動測試代碼**：

| 例外類型片段 | 指向 |
|-------------|------|
| `SqlException` 含 "TCP" / "network-related" / "server was not found" | SQL Server 容器 |
| `NpgsqlException` 含 "connection refused" / "Failed to connect" | PostgreSQL 容器 |
| `MySqlException` 含 "Unable to connect" / "Can't connect to server" | MySQL 容器 |
| `OracleException` 含 "ORA-12541" / "ORA-50201" / "TCP transport" | Oracle 容器 |

流程：`docker ps --filter "name=<container>" --format '{{.Status}}'` 確認容器在跑 →
在跑才考慮 schema / seed / 連線字串問題 → 不在跑就提示使用者啟動，
**禁止**為了「讓測試過」而修改測試代碼或 src code。

> CI 出現同樣例外走 `pull-request.md` 的「CI 失敗處理」，**不**套用本節（CI 不走 docker CLI）。

### 並行 flaky 的容錯空間（本機 + CI 都適用）

`./test.sh` 與 CI 在同一個 dotnet test 呼叫內並行跑多個 test 專案。
「同一個 DB 測試在 isolated 通過、在 full suite 失敗」通常是並行壓力下的連線池／容器資源爭用，
**不應直接視為 production bug**：對失敗的單一專案再跑一次，通過就是 flaky、記下不修；
連跑 2–3 次仍穩定失敗才視為真 bug。

---

## 測試撰寫模式

單一驗證用 `[Fact]`、參數化用 `[Theory]` + `[InlineData]`，一律加 `[DisplayName]`。

### 需要資料庫：`[DbFact(DatabaseType)]` / `[DbTheory(DatabaseType)]`

取代 `[Fact]` / `[Theory]`，**並指定該測試針對的資料庫類型**。兩個 attribute 定義在
`tests/Bee.Tests.Shared/`，依規則 `BEE_TEST_CONNSTR_{DBTYPE}`（uppercase 列舉值）
檢查環境變數；**未設定則自動跳過**。

| DatabaseType | 環境變數 |
|--------------|---------|
| `SQLServer` | `BEE_TEST_CONNSTR_SQLSERVER` |
| `PostgreSQL` | `BEE_TEST_CONNSTR_POSTGRESQL` |
| 未來 `MySQL` / `Oracle` | `BEE_TEST_CONNSTR_MYSQL` / `BEE_TEST_CONNSTR_ORACLE`（規則自動推導，不需新類別） |

連線 ID 命名規則 `common_{dbtype_lower}`（由 `TestDbConventions.GetDatabaseId` 產生）：
`common_sqlserver`、`common_postgresql`、…

- **本機**（`.runsettings` 設好 `BEE_TEST_CONNSTR_*`）與 **CI**（workflow 注入）皆正常執行。
- **任一 DB 未設環境變數**：該 DB 的測試自動 Skipped，不影響其他 DB。

`DbGlobalFixture` 多 DB 並存且容錯：逐一偵測 env var、驗證連線、建 schema、寫 seed；
單一 DB 失敗只跳過該 DB。

**適用**：純資料庫相依（查詢、schema、Repository/BO）。
**不適用**：純邏輯／序列化測試 —— 有 bug 應直接修復，不應跳過。

### 需要本機服務：`[LocalOnlyFact]` / `[LocalOnlyTheory]`

檢查環境變數 `CI`；**`CI=true`（GitHub Actions 預設）時自動跳過**。
**適用**：真正需要本機運行中服務的整合測試（如 API server ping）。
**不適用**：只需要 DB 的測試 —— 用 `[DbFact]`。

> **兩者目前無使用者**（2026-08-11 實測），`[DbTheory]` 同樣罕用。留著是因為
> 「需要本機服務的整合測試」這個情境仍成立。樣板檔裡的範例是**示意、不是現存程式碼**
> ——別去 grep 它。

### Per-class fixture（預設模式）

需要 DI-resolved 後端服務（`IDefineAccess` / `ISessionInfoService` /
`IBusinessObjectFactory` 等）時，透過 `IClassFixture<BeeTestFixture>` 取得 per-class
`IServiceProvider`。兩種特殊情境：

| 情境 | Fixture | 備註 |
|------|---------|------|
| 需要 per-fixture 寫檔（`SaveDefine` 系列） | `new BeeTestFixture(b => b.UseTempDefinePath())` 或自定 subclass | 把 `PathOptions.DefinePath` 切到隔離 temp 目錄 |
| 需要 `[DbFact]` 整合測試 | `IClassFixture<SharedDbFixture>` | 內建 `UseSharedDatabases()`，process-wide 一次性建 schema + seed user |

`[Collection("Initialize")]` / `GlobalFixture` / `BaseTests` / `BeeTestServices` /
`TempDefinePath` / `DefinePathInfo` / `CacheContainer` 靜態 facade **已全部移除**；
fixture 自帶 `IServiceProvider`，xUnit 預設 collection-per-class 平行恢復。

---

## 全域狀態與平行安全

xUnit 預設 collection-level parallel：**不同 test class 平行執行**，同一 collection 內串行。
任何「跨 class 共享的 static / global state」在平行下必然 race。

- **測試方法除 fixture 初始化外，禁止修改 production 的 `static`**（含靜態屬性／欄位、`AppDomain`）。
- production 必須以 static 暴露全域狀態時（如 `SysInfo.IsDebugMode`），優先**重構為可注入**
  （加接參數的重載，或抽介面走 DI）。
- 重構成本太高時，**所有碰同一個 static 的 test class 掛同一 `[Collection("...")]`**。

### 為什麼這條容易踩

本機 CPU 多、排程鬆，race 不一定觸發；CI runner 通常 2 core，平行更密集就浮現。
失敗訊息（如 `NoEncryptionEncryptor is only permitted in debug/development mode`）
看起來像 production bug，根因卻是測試互相污染。`try/finally` 還原「看起來」安全，
實際只在串行下成立。

### 串行化做法（過渡方案）

在 test 專案根目錄宣告純 marker `[CollectionDefinition("<名稱>")]`（無 fixture），
所有會修改該 static 的 test class 掛同一 `[Collection("<名稱>")]`。樣板見
`docs/repo-ops/testing-patterns.md`。

### 目前仍存在的窄序列化

多數測試已改用 fixture-scoped DI instance，race 風險自然消除。現存的 `[Collection]`
全部用於保護尚未 DI 化的 process-wide static：

| Collection | 保護對象 |
|---|---|
| `ClientInfoState` | `ClientInfo.*` |
| `SysInfoStatic` | `SysInfo.*`（`Bee.Base` 與 `Bee.Api.Core` 各自定義，跨組件必須如此） |
| `ApiClientInfoState` | `ApiClientInfo.*` |
| `ProcessWideStateCollection.Name` | `BEE_MASTER_KEY` 環境變數、`GlobalEvents`、測試 body 內建立的 DI 容器 |
| `ApiServiceOptionsState` | `ApiServiceOptions.*` |

**每個名稱都有對應的 `CollectionDefinition`，零孤兒。** 另有數個組件改以
`DisableTestParallelization` 整組序列化（`Bee.Api.Client` / `Bee.Api.Core` /
`Bee.Definition` / `Bee.ObjectCaching` / `Bee.UI.Avalonia`），那比逐類別掛 `[Collection]`
可靠 —— 讀取端會隨新測試增加，逐一補必然遺漏。

> **新增 collection 時用 `const` 而非字串字面值**（如 `ProcessWideStateCollection.Name`）：
> 打錯字的字面值會讓 xUnit 建一個沒人共用的隱式分組，**看起來有序列化、實際沒有**，
> 且不會有編譯錯。

---

## 共享 fixture 檔案隔離

`tests/Define/` 內的 XML（`SystemSettings.xml`、`DbCategorySettings.xml` 等）是
**多個測試專案共用的固定資料**，由 `TestProcessBootstrap` 啟動時讀入。任何測試
**不得寫入或修改**這些檔案 —— 一旦被改寫（含 round-trip 序列化造成的 xmlns 順序、縮排、
子節點變動），下次讀入會行為異常或 deserialize 失敗，造成連鎖錯誤。

任何 `SaveDefine` 系列呼叫（`SaveDbCategorySettings`、`SaveSystemSettings`、
`SaveTableSchema`、`SaveFormSchema`、`SaveDefine`）**或會間接觸發者**，必須切到隔離 temp：

1. **fixture-level**（推薦）：`new BeeTestFixture(b => b.UseTempDefinePath())` 或自定 subclass
   —— `PathOptions.DefinePath` 指向 `%TEMP%/bee-fixture-<guid>`，dispose 時清理。
2. **method-level**：純測試 `CacheDefineAccess` / `FileDefineStorage` 等 ctor 接
   `PathOptions` 的類別時，建 inline temp dir + `PathOptions { DefinePath = tempDir }` 傳入
   （`CacheDefineAccess(IDefineStorage, PathOptions)` 這個雙參數多載就是為此提供的）。

若需先 `GetDefine` 讀既有 fixture 再 `SaveDefine`：**先用 fixture 預設路徑 Get（從
`tests/Define`）→ 構造 temp `IDefineAccess` → Save**，避免 Get 在空 temp 讀不到資料。

完整樣板見 `docs/repo-ops/testing-patterns.md`。

---

## 常見 analyzer 退件規則

`build-ci.yml` 的 strict build 階段會直接擋 PR。三條特別容易踩：

- **S2699** —— 每個 `[Fact]`／`[Theory]` 至少一個 `Assert.*`。驗證「無例外」不可裸呼叫，
  用 `Record.Exception` / `Record.ExceptionAsync` 取回再 `Assert.Null(exception)`。
- **CA1861** —— 常數 array 不要 inline `new[] { ... }` 當引數（每次呼叫都配置），
  抽成檔案頂部的 `private static readonly string[] s_xxx = { ... }`。
- **IDE0005** —— 從別的測試檔 copy header 容易帶進不相關的 `using`，補完後逐一移除。

---

## 「本機綠、CI 紅」的反覆根因

本機環境比 CI「更完整」（有 `tests/Define` 的 DatabaseSettings、有持久 DB 容器、
可能殘留舊 seed），以下缺口**本機必定測不出來**。踩雷實例與排查過程見
`../docs/repo-ops/gotchas/test-ci-release.md`。

### 1. 會碰 DB 的測試必須用 `SharedDbFixture`

**`BeeTestFixture` 不建 schema**（只有 `SharedDbFixture` 會）。測試若會讓 BO 碰 DB
（session / 稽核 / 任何 repository 讀寫），fixture 必須是 `SharedDbFixture`，否則只有在
「別的測試類別或行程剛好先把表建好」時才會通過。**看到 `BeeTestFixture` + DB 存取就是嫌疑。**

判別捷徑：測試環境 `AuditLogOptions.Enabled` 預設 `false` 且 `tests/Define/SystemSettings.xml`
未覆寫 → 只動稽核寫入的改動在測試中不會求值，可先排除嫌疑。

**「寫入」不是唯一觸發條件 —— 讀取一樣會炸。** 測試只要拿**未植入 cache 的 token**
呼叫需驗身分的 API，server 就會 session cache miss → 走 rebuild 路徑讀 `st_session`。
辨識法：測試直接拿 `Guid.NewGuid()` 當 access token（而非
`TestSessionFactory.CreateAccessToken(fx)`，後者會把 SessionInfo 寫進 cache 因而永不觸及 DB）。

**別靠靜態 grep 判定範圍。** 觸發面比想像廣：不只 `IAccessTokenValidator`，任何
`SessionInfoService.Get(未快取 token)` 都算 —— 含 BO 內部的 `GetLangText` /
`GetCurrentCustomizeId` / 查目前公司。**用窮盡掃描，不要用推理代替執行**：drop 掉
`st_session`，再逐專案跑「`--filter` 排除所有 `SharedDbFixture` 類別」的子集 —— 建表的類別
不參與，依賴該表的測試就必定現形。完整命令與 2026-08-04 的實測結果見上述 gotchas
—— **當時此法一次掃出 4 個違規類別，先前純 grep 推理只找到 1 個。**

### 2. 一次重跑轉綠**不足以**判定 flaky

`gh run rerun --failed` 剛好轉綠是競賽條件的正常表現，不是結案依據。重跑只用來**收集證據**：
至少要看「不同 commit 的**首次**執行是否都紅」—— 都紅就當真 bug 查。

> 這條與上方「並行 flaky 的容錯空間」不衝突：那條講**同一 commit 內 isolated 通過 /
> full suite 失敗**（連跑 2–3 次判定）；這條講**跨 commit 首次執行都紅**。

### 3. seed 的冪等必須跨行程原子

`SharedDatabaseState` 的 seed 會被多個平行 test 行程對**同一實體 DB**同時執行。
以 per-table `SELECT COUNT(*)>0 then skip` 做冪等**不具跨行程原子性** —— 兩行程同時見 0
就各插一次。有 unique 業務鍵（`sys_id`）的表靠 unique 衝突讓輸家丟例外自保；
**無唯一業務鍵的表會被重複 seed**。

**正解**：整個 seed 包**單一 transaction**，gate 改判**第一張具 unique `sys_id` 的表**是否
已有列。贏家原子提交全套、輸家 rollback，其他行程因交易隔離只會看到「空」或「完整」兩態。
