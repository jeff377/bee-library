# 測試規範

## 測試框架

- **xUnit** v2.9.3
- **coverlet** 進行覆蓋率收集
- 全域 `<Using Include="Xunit" />` 已設定，無需逐一 using

## 測試專案對應

每個 `src/<Module>` 對應 `tests/<Module>.UnitTests`，結構對稱：
```
src/Bee.Base/           → tests/Bee.Base.UnitTests/
src/Bee.Definition/     → tests/Bee.Definition.UnitTests/
src/Bee.Api.Core/       → tests/Bee.Api.Core.UnitTests/
```

共用測試工具放在 `tests/Bee.Tests.Shared/`。

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
2. **容器存在性**：`docker ps -a --format '{{.Names}}\t{{.Status}}'` 比對下表。
   缺任一個就告知使用者「該 DB 的測試會自動 skip」，**不要自行 `docker run` 創新容器**
   （image 版本 / port / volume / 初始 schema 都有約束，亂建會撞既有設定）。
   容器在但 stopped 不需動作，`./test.sh` 會 `docker start`。

   | 容器名 | DB 類型 | env var |
   |--------|--------|---------|
   | `sql2025` | SQL Server | `BEE_TEST_CONNSTR_SQLSERVER` |
   | `pgvector-db` | PostgreSQL | `BEE_TEST_CONNSTR_POSTGRESQL` |
   | `mysql8` | MySQL | `BEE_TEST_CONNSTR_MYSQL` |
   | `oracle23ai` | Oracle | `BEE_TEST_CONNSTR_ORACLE` |

### 測試失敗的判別順序（本機情境）

跑完 `./test.sh` 後，若看到下列例外類型，**優先懷疑容器狀態，不要直接動測試代碼**：

| 例外類型片段 | 對應容器 |
|-------------|---------|
| `SqlException` 含 "TCP" / "network-related" / "server was not found" | `sql2025` |
| `NpgsqlException` 含 "connection refused" / "Failed to connect" | `pgvector-db` |
| `MySqlException` 含 "Unable to connect" / "Can't connect to server" | `mysql8` |
| `OracleException` 含 "ORA-12541" / "ORA-50201" / "TCP transport" | `oracle23ai` |

判別流程：

1. `docker ps --filter "name=<container>" --format '{{.Status}}'` 確認容器確實在跑
2. 容器在跑 → 才考慮是 schema / seed / 連線字串問題
3. 容器不在跑 → 提示使用者啟動，**禁止**為了「讓測試過」而修改測試代碼或 src code

> CI 環境若出現同樣的例外，走 `pull-request.md` 的「CI 失敗處理」流程，**不**套用本節（CI 不走 docker CLI，容器由 workflow yml 管理）。

### 並行 flaky 的容錯空間（本機 + CI 都適用）

`./test.sh` 與 CI `build-ci.yml` 在同一個 dotnet test 呼叫內並行跑多個 test 專案。觀察到「同一個 DB 測試在 isolated 跑通過、在 full suite 跑失敗」通常是並行壓力下的連線池 / 容器資源爭用，**不應直接視為 production bug**。

判別流程：

1. 對失敗的單一 test 專案再跑一次（`dotnet test tests/<Project>/<Project>.csproj`）
2. 通過 → flaky，記下不修
3. 連跑 2-3 次仍穩定失敗 → 才視為真 bug 並 debug

## 測試撰寫模式

> **可貼用的程式碼樣板見 `docs/repo-ops/testing-patterns.md`**（按需讀，不常駐）。
> 本節只留「哪種情境用哪個 attribute / fixture」的判準。

單一驗證用 `[Fact]`、參數化用 `[Theory]` + `[InlineData]`，一律加 `[DisplayName]`
提供中文描述。

### 需要資料庫的測試：`[DbFact(DatabaseType)]` / `[DbTheory(DatabaseType)]`

需要連接資料庫的測試使用 `[DbFact(DatabaseType.X)]` 或 `[DbTheory(DatabaseType.X)]` 取代 `[Fact]` / `[Theory]`，**並指定該測試針對的資料庫類型**。
兩個 attribute 定義在 `tests/Bee.Tests.Shared/`，會依規則 `BEE_TEST_CONNSTR_{DBTYPE}`（uppercase 列舉值）檢查對應環境變數；**未設定則自動跳過**。

| DatabaseType | 環境變數 |
|--------------|---------|
| `SQLServer` | `BEE_TEST_CONNSTR_SQLSERVER` |
| `PostgreSQL` | `BEE_TEST_CONNSTR_POSTGRESQL` |
| 未來 `MySQL` / `Oracle` | `BEE_TEST_CONNSTR_MYSQL` / `BEE_TEST_CONNSTR_ORACLE`（規則自動推導，不需新類別） |

連線 ID 命名規則 `common_{dbtype_lower}`（由 `TestDbConventions.GetDatabaseId` 產生）：
- `common_sqlserver`、`common_postgresql`、…

- **本機（`.runsettings` 設好對應 `BEE_TEST_CONNSTR_*`）**：對應 DB 的測試正常執行
- **CI（`build-ci.yml` 啟動對應 service container 並注入 `BEE_TEST_CONNSTR_*`）**：正常執行
- **任一 DB 未設環境變數**：該 DB 的測試自動 Skipped，不影響其他 DB

`DbGlobalFixture` 多 DB 並存且容錯：對每個 `DatabaseType` 偵測對應 env var、驗證連線、建立 schema、寫入 seed；單一 DB 失敗只跳過該 DB，不阻擋其他 DB。

**適用場景**：純資料庫相依的測試（查詢、schema、Repository/BO 相關）。
**不適用**：純邏輯 / 序列化測試 — 這類測試有 bug 應直接修復，不應跳過。

### 需要本機基礎設施的測試：`[LocalOnlyFact]` / `[LocalOnlyTheory]`

需要本機特定基礎設施（例如本機跑著的 API server、專屬資料、或無法在 CI 自動備妥的環境）的測試，使用 `[LocalOnlyFact]` / `[LocalOnlyTheory]`。

定義在 `tests/Bee.Tests.Shared/`，會檢查環境變數 `CI`；**當 `CI=true`（GitHub Actions 預設）時自動跳過**。

> **兩者目前無使用者**（2026-08-11 實測），`[DbTheory]` 同樣罕用。留著是因為
> 「需要本機服務的整合測試」這個情境仍成立。樣板檔裡的範例是**示意、不是現存程式碼**
> ——別去 grep 它。

**適用場景**：真正需要「本機運行中服務」的整合測試（如需要 API server 回應的 ping 測試）。
**不適用**：只需要 DB 的測試 — 請使用 `[DbFact]` / `[DbTheory]`。

### Per-class fixture（Phase 5 後預設模式）

需要 DI-resolved 後端服務（`IDefineAccess` / `ISessionInfoService` / `IBusinessObjectFactory` 等）的測試，
透過 `IClassFixture<BeeTestFixture>` 取得 per-class `IServiceProvider`。

兩種特殊情境：

| 情境 | Fixture | 備註 |
|------|---------|------|
| 需要 per-fixture 寫檔（`SaveDefine` 系列） | `new BeeTestFixture(b => b.UseTempDefinePath())` 或自定 subclass | `b.UseTempDefinePath()` 把 fixture `PathOptions.DefinePath` 切到隔離 temp 目錄 |
| 需要 `[DbFact]` 整合測試（SQL Server / Postgres / SQLite / MySQL / Oracle） | `IClassFixture<SharedDbFixture>` | 內建 `UseSharedDatabases()`，process-wide 一次性建 schema + seed user |

Phase 5（PR 5.1–5.8）結束後 `[Collection("Initialize")]` / `GlobalFixture` / `BaseTests` /
`BeeTestServices` / `TempDefinePath` / `DefinePathInfo` / `CacheContainer` 靜態 facade 已全部移除；
測試用 fixture 自帶 `IServiceProvider`，xUnit 預設 collection-per-class 平行恢復。

## 命名規則

方法名稱格式：`<方法名稱>_<情境>_<預期結果>`
- `ValidateToken_ExpiredToken_ReturnsFalse`
- `Encrypt_ValidInput_ReturnsNonEmptyBytes`
- `CreateSession_DuplicateUser_ThrowsException`

## 測試原則

- 每個測試只驗證**一個行為**
- 測試不依賴外部服務（純單元測試）
- 加密、雜湊等安全相關邏輯**必須**有對應測試
- 新增公開 API 時同步新增對應測試
- 使用 `[DisplayName]` 提供清楚的中文描述

## 全域狀態與平行安全

xUnit 預設 collection-level parallel：**不同 test class 平行執行**，同一 collection 內串行。任何「跨 class 共享的 static / global state」在平行執行下必然 race。

### 核心原則

- **測試方法除 fixture 初始化外，禁止直接修改 production 的 `static` 變數**（含靜態屬性、靜態欄位、`AppDomain` 等全域狀態）
- 若 production code 必須以 static 暴露全域狀態（如 `SysInfo.IsDebugMode`），優先**重構為可注入**：
  - 加重載方法接收參數（如 `CreateEncryptor(string name, bool isDebugMode)`）
  - 或抽介面以 DI 提供（如 `IDebugModeProvider`）
- 重構成本太高、暫時無法避免時，**所有會碰同一個 static 的測試 class 必須加入同一 `[Collection("...")]`**，讓 xUnit 串行執行

### 為什麼這條容易踩

- 本機 CPU 多、排程鬆，race 不一定觸發；CI runner 通常 2 core，平行更密集，問題就浮現
- 失敗訊息（如 `NoEncryptionEncryptor is only permitted in debug/development mode`）看起來像 production bug，但根因是測試之間互相污染
- try/finally 還原 static 值「看起來」安全，實際上只在串行執行下成立

### 串行化做法（過渡方案）

在 test 專案根目錄宣告一個純 marker `[CollectionDefinition("<名稱>")]`（無 fixture），
所有會修改該 static 的 test class 都掛同一個 `[Collection("<名稱>")]`。
樣板見 `docs/repo-ops/testing-patterns.md`。

### 目前仍存在的窄序列化

多數測試已改以 fixture-scoped DI instance 取代 process-wide static，race 風險自然消除。現存的 `[Collection]` 序列化全部用於保護尚未 DI 化的 process-wide static：

| Collection | 保護對象 |
|---|---|
| `ClientInfoState` | `ClientInfo.*` |
| `SysInfoStatic` | `SysInfo.*`（`Bee.Base` 與 `Bee.Api.Core` 各自定義，跨組件必須如此） |
| `ApiClientInfoState` | `ApiClientInfo.*` |
| `ProcessWideStateCollection.Name` | `BEE_MASTER_KEY` 環境變數、`GlobalEvents`、測試 body 內建立的 DI 容器 |
| `ApiServiceOptionsState` | `ApiServiceOptions.*` |

**每個名稱都有對應的 `CollectionDefinition`，零孤兒。** 另有數個組件改以
`DisableTestParallelization` 整組序列化（`Bee.Api.Client` / `Bee.Api.Core` /
`Bee.Definition` / `Bee.ObjectCaching` / `Bee.UI.Avalonia`），那比逐類別掛 `[Collection]` 可靠——
讀取端會隨新測試增加，逐一補必然遺漏。

> **新增 collection 時用 `const` 而非字串字面值**（如 `ProcessWideStateCollection.Name`）：
> 打錯字的字面值會讓 xUnit 建一個沒人共用的隱式分組，**看起來有序列化、實際沒有**，
> 且不會有編譯錯。

## 共享 fixture 檔案隔離

`tests/Define/` 內的 XML 檔案（`SystemSettings.xml`、`DbCategorySettings.xml` 等）是**多個測試專案共用的固定資料**，由 `TestProcessBootstrap` 啟動時讀入、提供 schema / settings 種子。任何測試**不得寫入或修改**這些檔案——一旦被改寫（包括 round-trip 序列化造成的 xmlns 順序、縮排、子節點變動），下次測試讀入時會行為異常或 deserialize 失敗，造成連鎖測試錯誤。

### 規則

任何呼叫 `SaveDefine` 系列方法（`SaveDbCategorySettings`、`SaveSystemSettings`、`SaveTableSchema`、`SaveFormSchema`、`SaveDefine` 等）**或會間接觸發其呼叫的測試**，必須透過下列之一切到隔離的暫存資料夾：

1. **fixture-level**（推薦）：`new BeeTestFixture(b => b.UseTempDefinePath())` 或自定 fixture subclass —
   `PathOptions.DefinePath` 指向 `%TEMP%/bee-fixture-<guid>`，dispose 時清理。
2. **method-level**：純測試 `CacheDefineAccess` / `FileDefineStorage` 等 ctor 接 `PathOptions` 的類別時，
   可直接建立 inline temp dir + `PathOptions { DefinePath = tempDir }`，傳入 ctor
   （`CacheDefineAccess(IDefineStorage, PathOptions)` 這個雙參數多載就是為此提供的，
   內部自建 `CacheContainerService`）。

若測試需要先 `GetDefine` 讀取既有 fixture 再 `SaveDefine`：**先用 fixture 預設路徑 Get（從 `tests/Define`）→ 構造 temp `IDefineAccess` → Save**，避免 Get 在空 temp 內讀不到資料。

兩種做法的完整樣板見 `docs/repo-ops/testing-patterns.md`。

## 常見 analyzer 退件規則

`build-ci.yml` 有 strict build 階段會直接擋 PR；以下三條是撰寫測試檔時特別容易踩的，列出以減少 PR churn。

### S2699 — 每個 `[Fact]`／`[Theory]` 必須至少一個 `Assert.*`

驗證「無例外」的測試不可裸呼叫，需用 `Record.Exception` / `Record.ExceptionAsync` 明確斷言：

```csharp
// ❌ 無 assert，S2699 觸發
[Fact]
public async Task PingAsync_LocalConnector_Succeeds()
{
    var connector = new SystemApiConnector(Guid.NewGuid());
    await connector.PingAsync();
}

// ✅ Record.ExceptionAsync + Assert.Null
[Fact]
public async Task PingAsync_LocalConnector_Succeeds()
{
    var connector = new SystemApiConnector(Guid.NewGuid());
    var exception = await Record.ExceptionAsync(() => connector.PingAsync());
    Assert.Null(exception);
}
```

### CA1861 — 常數 array 改用 `static readonly` 欄位

`new[] { ... }` 作為 method 引數傳入時每次呼叫會配置新 array，應抽成檔案頂部的 `static readonly`：

```csharp
// ❌ inline new[]，CA1861 觸發
var result = access.GetDefine(DefineType.FormSchema, new[] { "Employee" });

// ✅ static readonly 欄位
private static readonly string[] s_employeeKey = { "Employee" };

var result = access.GetDefine(DefineType.FormSchema, s_employeeKey);
```

### IDE0005 — 不留未使用的 `using`

從別的測試檔 copy header 時容易帶進不相關的 using，補完測試後逐一檢查並移除。

## 「本機綠、CI 紅」的反覆根因

本機環境比 CI「更完整」（有 `tests/Define` 的 DatabaseSettings、有持久 DB 容器、可能殘留舊 seed），
以下缺口**本機必定測不出來**。踩雷實例與排查過程見
`docs/repo-ops/gotchas/test-ci-release.md`。

### 1. 會碰 DB 的測試必須用 `SharedDbFixture`

**`BeeTestFixture` 不建 schema**（只有 `SharedDbFixture` 會）。測試若會讓 BO 碰 DB
（session / 稽核 / 任何 repository 讀寫），fixture 必須是 `SharedDbFixture`，否則只有在
「別的測試類別或行程剛好先把表建好」時才會通過。**看到 `BeeTestFixture` + DB 存取就是嫌疑。**

判別捷徑：測試環境 `AuditLogOptions.Enabled` 預設 `false` 且 `tests/Define/SystemSettings.xml`
未覆寫 → 只動稽核寫入的改動在測試中不會求值，可先排除嫌疑。

**「寫入」不是唯一觸發條件——讀取一樣會炸。** 測試只要拿**未植入 cache 的 token**
呼叫需驗身分的 API，server 就會 session cache miss → 走 rebuild 路徑讀 `st_session`。
辨識法：測試直接拿 `Guid.NewGuid()` 當 access token（而非
`TestSessionFactory.CreateAccessToken(fx)`，後者會把 SessionInfo 寫進 cache 因而永不觸及 DB）。

**別靠靜態 grep 判定範圍。** 觸發面比想像廣：不只 `IAccessTokenValidator`，任何
`SessionInfoService.Get(未快取 token)` 都算——含 BO 內部的 `GetLangText` /
`GetCurrentCustomizeId` / 查目前公司。**用窮盡掃描，不要用推理代替執行**：
drop 掉 `st_session`，再逐專案跑「`--filter` 排除所有 `SharedDbFixture` 類別」的子集
——建表的類別不參與，依賴該表的測試就必定現形（`--filter` 值要雙引號包住，`&` 否則被 shell 吃掉；
表由下次 `EnsureSchemaAndSeed` 重建，無殘留）。完整命令與 2026-08-04 的實測結果見 gotchas
——**當時此法一次掃出 4 個違規類別，先前純 grep 推理只找到 1 個。**

### 2. 一次重跑轉綠**不足以**判定 flaky

`gh run rerun --failed` 剛好轉綠是競賽條件的正常表現，不是結案依據。重跑只用來**收集證據**：
至少要看「不同 commit 的**首次**執行是否都紅」——都紅就當真 bug 查。

> 這條與上方「並行 flaky 的容錯空間」不衝突：那條講的是**同一 commit 內 isolated 通過 /
> full suite 失敗**（連跑 2–3 次判定）；這條講的是**跨 commit 首次執行都紅**。

### 3. seed 的冪等必須跨行程原子

`SharedDatabaseState` 的 seed 會被多個平行 test 行程對**同一實體 DB**同時執行。
以 per-table `SELECT COUNT(*)>0 then skip` 做冪等**不具跨行程原子性**——兩行程同時見 0 就各插一次。
有 unique 業務鍵（`sys_id`）的表靠 unique 衝突讓輸家丟例外自保；**無唯一業務鍵的表會被重複 seed**。

**正解**：整個 seed 包**單一 transaction**，gate 改判**第一張具 unique `sys_id` 的表**是否已有列。
贏家原子提交全套、輸家 rollback，其他行程因交易隔離只會看到「空」或「完整」兩態。
