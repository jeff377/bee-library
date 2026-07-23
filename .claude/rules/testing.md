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

> **適用範圍**：僅針對「使用者的本機開發環境（macOS / Windows / Linux 桌面）且本機裝有 docker CLI 並用於跑 DB 容器」的情境。
>
> **不適用情境**：
> - **GitHub Actions / 其他 CI**：`build-ci.yml` 透過 service container 機制由 runner 自動啟動 DB，env vars 由 workflow 注入，不走 docker CLI。本檢查不要帶進任何 yml 改動
> - **本機無 docker** 的環境（Codespaces 預設、純 Linux server 沒裝 docker、Claude 在無 docker 的沙箱裡）：直接跑 `dotnet test`，`[DbFact]` 會依 env var 不設值自動 skip，不需做容器檢查
>
> **判定本規則是否適用的最快方法**：執行 `command -v docker` —— 沒輸出就跳過整套規則，直接跑測試即可。

### 為何要先檢查（本機情境）

`./test.sh` 對「容器不存在 → env var 不設值 → `[DbFact]` 自動 skip」的路徑**不會回報明顯訊號**。只看 `./test.sh` 的輸出，無法區分：

- 「測試按計劃 skip」（容器不在，預期行為）
- 「測試該執行卻沒跑」（環境異常，需要排查）

前置檢查能讓 Claude 在報告測試結果時明確說「X 個 DB 的測試已 skip 因為容器 Y 不在」而非含糊帶過，也能避免把「DB 連線失敗」誤判為「程式 bug」浪費 debug 時間。

### 啟動前檢查（兩步，僅在 docker 可用時）

1. **Docker daemon**：執行 `docker info` 或 `docker ps`。
   - 失敗（daemon 未啟動） → 本條指「agent 直接以 Bash 操作 docker（**不經** `./test.sh`）」的情境：**告知使用者**「請啟動 Docker Desktop」，**不要**嘗試自行 `open -a Docker` 等指令（agent 直接拉起桌面 GUI 工具，啟動時間長且結果不確定）。使用者決定不啟動 docker → 比照「本機無 docker」直接跑測試
     - **例外**：`./test.sh` 已內建 `ensure_docker_daemon` 前置 —— macOS 上 daemon 未啟動時腳本會自動 `open -a Docker` 並輪詢等待就緒（timeout 120s，失敗只警告不中止）。故**走 `./test.sh` 跑測試時 daemon 由腳本自動處理**，agent 不需先手動檢查或提示使用者。
   - 成功 → 進下一步
2. **約定容器存在性**：執行 `docker ps -a --format '{{.Names}}\t{{.Status}}'` 比對下列容器名單：

   | 容器名 | DB 類型 | env var |
   |--------|--------|---------|
   | `sql2025` | SQL Server | `BEE_TEST_CONNSTR_SQLSERVER` |
   | `pgvector-db` | PostgreSQL | `BEE_TEST_CONNSTR_POSTGRESQL` |
   | `mysql8` | MySQL | `BEE_TEST_CONNSTR_MYSQL` |
   | `oracle23ai` | Oracle | `BEE_TEST_CONNSTR_ORACLE` |

   - 任一缺失 → 告知使用者「容器 X 不存在，對應 DB 的測試將被自動 skip」，**不要**自行 `docker run` 創新容器（image 版本、port mapping、volume 掛載、初始 schema 都有約束，亂建會與使用者既有設定衝突）
   - 容器存在但 stopped → 不需動作，`./test.sh` 內部會 `docker start`

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

### 單一驗證：`[Fact]`
```csharp
[Fact]
[DisplayName("建立 Session 應回傳有效 Token")]
public void CreateSession_ReturnsValidToken()
{
    var token = _repo.CreateSession(user);
    Assert.NotNull(token);
}
```

### 參數化：`[Theory]` + `[InlineData]`
```csharp
[Theory]
[InlineData(DefineType.SystemSettings, typeof(SystemSettings))]
[InlineData(DefineType.UserSettings, typeof(UserSettings))]
[DisplayName("ToClrType 回傳正確型別")]
public void ToClrType_ValidType(DefineType defineType, Type expectedType)
{
    var result = defineType.ToClrType();
    Assert.Equal(expectedType, result);
}
```

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

```csharp
[DbFact(DatabaseType.SQLServer)]
[DisplayName("SQL Server 上 ExecuteDataTable 查詢應回傳有效 DataTable")]
public void ExecuteDataTable_SqlServer_ReturnsDataTable()
{
    var dbAccess = new DbAccess("common_sqlserver");
    var result = dbAccess.Execute(command);
    Assert.NotNull(result.Table);
}

[DbFact(DatabaseType.PostgreSQL)]
[DisplayName("PostgreSQL 上 ExecuteDataTable 查詢應回傳有效 DataTable")]
public void ExecuteDataTable_PostgreSQL_ReturnsDataTable()
{
    var dbAccess = new DbAccess("common_postgresql");
    var result = dbAccess.Execute(command);
    Assert.NotNull(result.Table);
}
```

- **本機（`.runsettings` 設好對應 `BEE_TEST_CONNSTR_*`）**：對應 DB 的測試正常執行
- **CI（`build-ci.yml` 啟動對應 service container 並注入 `BEE_TEST_CONNSTR_*`）**：正常執行
- **任一 DB 未設環境變數**：該 DB 的測試自動 Skipped，不影響其他 DB

`DbGlobalFixture` 多 DB 並存且容錯：對每個 `DatabaseType` 偵測對應 env var、驗證連線、建立 schema、寫入 seed；單一 DB 失敗只跳過該 DB，不阻擋其他 DB。

**適用場景**：純資料庫相依的測試（查詢、schema、Repository/BO 相關）。
**不適用**：純邏輯 / 序列化測試 — 這類測試有 bug 應直接修復，不應跳過。

### 需要本機基礎設施的測試：`[LocalOnlyFact]` / `[LocalOnlyTheory]`

需要本機特定基礎設施（例如本機跑著的 API server、專屬資料、或無法在 CI 自動備妥的環境）的測試，使用 `[LocalOnlyFact]` / `[LocalOnlyTheory]`。
定義在 `tests/Bee.Tests.Shared/`，會檢查環境變數 `CI`；**當 `CI=true`（GitHub Actions 預設）時自動跳過**。

```csharp
[LocalOnlyTheory]
[InlineData("http://localhost/jsonrpc/api")]
[DisplayName("ApiConnectValidator 驗證 URL 應回傳遠端連線類型")]
public void Validate_ValidUrl_ReturnsRemoteConnectType(string apiUrl) { ... }
```

**適用場景**：真正需要「本機運行中服務」的整合測試（如需要 API server 回應的 ping 測試）。
**不適用**：只需要 DB 的測試 — 請使用 `[DbFact]` / `[DbTheory]`。

### Per-class fixture（Phase 5 後預設模式）

需要 DI-resolved 後端服務（`IDefineAccess` / `ISessionInfoService` / `IBusinessObjectFactory` 等）的測試，
透過 `IClassFixture<BeeTestFixture>` 取得 per-class `IServiceProvider`：

```csharp
public class MyTests : IClassFixture<BeeTestFixture>
{
    private readonly BeeTestFixture _fx;
    public MyTests(BeeTestFixture fx) { _fx = fx; }

    [Fact]
    public void Foo()
    {
        var access = _fx.GetRequiredService<IDefineAccess>();
        // ...
    }
}
```

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

```csharp
// 1. 在 test 專案根目錄宣告 collection
[CollectionDefinition("DbConnectionState")]
public class DbConnectionStateCollection
{
    // 純 marker，無 fixture
}

// 2. 所有會修改該 static 的 test class 加同一 [Collection]
[Collection("DbConnectionState")]
public class DbConnectionManagerTests { ... }

[Collection("DbConnectionState")]
public class DbAccessFactoryTests { ... }
```

### 目前仍存在的窄序列化

多數測試已改以 fixture-scoped DI instance 取代 process-wide static，race 風險自然消除。實測（2026-07）全 repo 仍有 **20 處** `[Collection("...")]` 序列化，全部用於保護尚未 DI 化的 process-wide static：`ClientInfoState`（`ClientInfo.*`，12 處）、`SysInfoStatic`（`SysInfo.*`，3 處）、`ClientInfo`（MAUI 端 `ClientInfo.*`，3 處）、`ApiClientInfoState`（`ApiClientInfo.*`，2 處）。這些 static 重構為可注入後即可移除。

> 注意：`SysInfoStatic` 與 `ClientInfo` 目前**無對應 `CollectionDefinition`**（xUnit 隱式分組仍運作，但建議補上定義或改用已定義名稱，避免打錯字時不會有編譯錯）。

## 共享 fixture 檔案隔離

`tests/Define/` 內的 XML 檔案（`SystemSettings.xml`、`DbCategorySettings.xml` 等）是**多個測試專案共用的固定資料**，由 `TestProcessBootstrap` 啟動時讀入、提供 schema / settings 種子。任何測試**不得寫入或修改**這些檔案——一旦被改寫（包括 round-trip 序列化造成的 xmlns 順序、縮排、子節點變動），下次測試讀入時會行為異常或 deserialize 失敗，造成連鎖測試錯誤。

### 規則

任何呼叫 `SaveDefine` 系列方法（`SaveDbCategorySettings`、`SaveSystemSettings`、`SaveTableSchema`、`SaveFormSchema`、`SaveDefine` 等）**或會間接觸發其呼叫的測試**，必須透過下列之一切到隔離的暫存資料夾：

1. **fixture-level**（推薦）：`new BeeTestFixture(b => b.UseTempDefinePath())` 或自定 fixture subclass —
   `PathOptions.DefinePath` 指向 `%TEMP%/bee-fixture-<guid>`，dispose 時清理。
2. **method-level**：純測試 `LocalDefineAccess` / `FileDefineStorage` 等 ctor 接 `PathOptions` 的類別時，
   可直接建立 inline temp dir + `PathOptions { DefinePath = tempDir }`，傳入 ctor。

若測試需要先 `GetDefine` 讀取既有 fixture 再 `SaveDefine`：**先用 fixture 預設路徑 Get（從 `tests/Define`）→ 構造 temp `IDefineAccess` → Save**，避免 Get 在空 temp 內讀不到資料。

### Fixture-level 範例

```csharp
public sealed class WritableDefineFixture : BeeTestFixture
{
    public WritableDefineFixture() : base(b => b.UseTempDefinePath()) {}
}

public class MySaveTests : IClassFixture<WritableDefineFixture>
{
    private readonly WritableDefineFixture _fx;
    public MySaveTests(WritableDefineFixture fx) { _fx = fx; }

    [Fact]
    public void SaveDbCategorySettings_WritesFile()
    {
        var access = _fx.GetRequiredService<IDefineAccess>();
        access.SaveDbCategorySettings(new DbCategorySettings());
        Assert.True(File.Exists(_fx.PathOptions.GetDbCategorySettingsFilePath()));
    }
}
```

### Method-level inline temp dir

對純資料寫入測試（不需 DI），inline temp dir 比建立 fixture subclass 更輕：

```csharp
[Fact]
public void SaveSystemSettings_WritesFile()
{
    var tempDir = Path.Combine(Path.GetTempPath(), $"bee-save-{Guid.NewGuid():N}");
    Directory.CreateDirectory(tempDir);
    try
    {
        var paths = new PathOptions { DefinePath = tempDir };
        var access = new LocalDefineAccess(new FileDefineStorage(paths), paths);
        access.SaveSystemSettings(new SystemSettings());
        Assert.True(File.Exists(paths.GetSystemSettingsFilePath()));
    }
    finally
    {
        try { Directory.Delete(tempDir, recursive: true); } catch (IOException) { /* best effort */ }
    }
}
```

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
