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

### 需要資料庫的測試：`[DbFact]` / `[DbTheory]`

需要連接資料庫的測試使用 `[DbFact]` 或 `[DbTheory]` 取代 `[Fact]` / `[Theory]`。
這兩個 Attribute 定義在 `tests/Bee.Tests.Shared/`，會檢查環境變數 `BEE_TEST_DB_CONNSTR`；**未設定則自動跳過**。

- **本機（`.runsettings` 已填連線字串）**：正常執行
- **CI（`build-ci.yml` 啟動 SQL Server service container 並注入 `BEE_TEST_DB_CONNSTR`）**：正常執行
- **本機或 CI 未設 `BEE_TEST_DB_CONNSTR`**：自動標記為 Skipped，不會因缺基礎設施而失敗

```csharp
[DbFact]
[DisplayName("ExecuteDataTable 查詢應回傳有效 DataTable")]
public void ExecuteDataTable_ValidQuery_ReturnsDataTable()
{
    var dbAccess = new DbAccess("common");
    var result = dbAccess.Execute(command);
    Assert.NotNull(result.Table);
}
```

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
