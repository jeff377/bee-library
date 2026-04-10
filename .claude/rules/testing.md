# 測試規範

## 測試框架

- **xUnit** v2.9.3
- **coverlet** 進行覆蓋率收集
- 全域 `<Using Include="Xunit" />` 已設定，無需逐一 using

## 測試專案對應

每個 `src/<Module>` 對應 `tests/<Module>.UnitTests`，結構對稱：
```
src/Bee.Core/           → tests/Bee.Core.UnitTests/
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

### 本機專用測試：`[LocalOnlyFact]` / `[LocalOnlyTheory]`

需要連接本機資料庫或 API 服務的測試，使用 `[LocalOnlyFact]` 或 `[LocalOnlyTheory]` 取代 `[Fact]` / `[Theory]`。
這兩個 Attribute 定義在 `tests/Bee.Tests.Shared/`，利用環境變數 `CI=true`（GitHub Actions 預設會設定）自動跳過測試。

- **本機執行**：正常執行
- **CI 環境**：自動標記為 Skipped，不會因基礎設施缺失而失敗

```csharp
// 需要本機 SQL Server 的測試
[LocalOnlyFact]
[DisplayName("ExecuteDataTable 查詢應回傳有效 DataTable")]
public void ExecuteDataTable_ValidQuery_ReturnsDataTable()
{
    var dbAccess = new DbAccess("common");
    var result = dbAccess.Execute(command);
    Assert.NotNull(result.Table);
}

// 參數化版本
[LocalOnlyTheory]
[InlineData("http://localhost/jsonrpc/api")]
[DisplayName("ApiConnectValidator 驗證 URL 應回傳遠端連線類型")]
public void ApiConnectValidator_ValidUrl_ReturnsRemoteConnectType(string apiUrl) { ... }
```

**適用場景**：資料庫連線、API 端點呼叫、需要執行中服務的整合測試。
**不適用**：純邏輯 / 序列化測試 — 這類測試有 bug 應直接修復，不應跳過。

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
