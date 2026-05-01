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

### 測試集合共用初始化
需要共用狀態時使用 `[Collection("Initialize")]`。

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
[CollectionDefinition("SysInfo")]
public class SysInfoCollection
{
    // 純 marker，無 fixture
}

// 2. 所有會修改 SysInfo 的 test class 加同一 [Collection]
[Collection("SysInfo")]
public class ApiServiceOptionsTests { ... }

[Collection("SysInfo")]
public class ApiPayloadOptionsFactoryTests { ... }
```

### 已知違反案例（待逐步遷移）

- `Bee.Api.Core.UnitTests` 中三個 class 透過 `try/finally` 切換 `SysInfo.IsDebugMode`
  - 短期：已用 `[Collection("SysInfo")]` 串行
  - 長期：`ApiPayloadOptionsFactory.CreateEncryptor` 應改為接受 `isDebugMode` 參數

## 共享 fixture 檔案隔離

`tests/Define/` 內的 XML 檔案（`SystemSettings.xml`、`DbSchemaSettings.xml` 等）是**多個測試專案共用的固定資料**，由 `DbGlobalFixture` 啟動時讀入、提供 schema / settings 種子。任何測試**不得寫入或修改**這些檔案——一旦被改寫（包括 round-trip 序列化造成的 xmlns 順序、縮排、子節點變動），下次測試讀入時會行為異常或 deserialize 失敗，造成連鎖測試錯誤。

### 規則

- 任何呼叫 `SaveDefine` 系列方法（`SaveDbSchemaSettings`、`SaveSystemSettings`、`SaveTableSchema`、`SaveFormSchema`、`SaveDefine` 等）**或會間接觸發其呼叫的測試**，必須包覆 `using var temp = new TempDefinePath();`，將 `BackendInfo.DefinePath` 暫時切換至 `%TEMP%` 下的隔離資料夾
- `TempDefinePath` 位於 `tests/Bee.Tests.Shared/TempDefinePath.cs`，dispose 時自動還原原 path 並清理暫存目錄
- 若測試需要先 `GetDefine` 讀取既有 fixture 再 `SaveDefine`：**先 Get（從原 path）→ 再切到 temp → 再 Save**，避免 GetDefine 在空 temp 內讀不到資料

### 範例

```csharp
[Fact]
[DisplayName("SaveDbSchemaSettings 應寫入 DbSchemaSettings.xml")]
public void SaveDbSchemaSettings_WritesFile()
{
    using var temp = new TempDefinePath();      // 先切到暫存
    var settings = new DbSchemaSettings();

    _access.SaveDbSchemaSettings(settings);

    Assert.True(File.Exists(DefinePathInfo.GetDbTableSettingsFilePath()));
}
```

「先 Get 後 Save」情境（例如驗證 SaveDefine round-trip）：

```csharp
var bo = new SystemBusinessObject(Guid.Empty, isLocalCall: true);
var getResult = bo.GetDefine(new GetDefineArgs { DefineType = DefineType.DbSchemaSettings });

using var temp = new TempDefinePath();          // 取完資料才切
var saveResult = bo.SaveDefine(new SaveDefineArgs
{
    DefineType = DefineType.DbSchemaSettings,
    Xml = getResult.Xml
});
```

### 已知違反案例（已修復）

- `SystemBusinessObjectDefineTests.SaveDefine_LocalCallDbSchemaSettings_Succeeds` 曾未隔離，導致 `tests/Define/DbSchemaSettings.xml` 被覆寫（`<Tables>` 子節點清空、xmlns 順序變動）。已用 `TempDefinePath` 包覆修復。

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
