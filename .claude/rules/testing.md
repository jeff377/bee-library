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
