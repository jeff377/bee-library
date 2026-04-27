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
[DisplayName("GetDefineType 回傳正確型別")]
public void GetDefineType_ValidType(DefineType defineType, Type expectedType)
{
    var result = DefineFunc.GetDefineType(defineType);
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
