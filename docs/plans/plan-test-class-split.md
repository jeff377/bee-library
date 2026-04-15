# 計畫：拆分混雜的測試類別

## 目標

將測試類別與被測類別對齊，遵循 **一個測試類別對應一個被測類別** 的原則。

---

## 一、Bee.Base.UnitTests / `BaseTests`

原類別混合了 4 個不同被測類別，拆分為：

| 被測類別 | 新測試檔案 | 搬移的方法 |
|---------|----------|-----------|
| `IPValidator` | `IPValidatorTests.cs` | `IsIpAllowed_WhitelistAndBlacklist_ReturnsExpectedResult` |
| `BaseFunc` | `BaseFuncTests.cs` | `IsNumeric_VariousTypes_ReturnsExpectedResult`、`RndInt_ReturnsValueWithinRange` |
| `StrFunc` | `StrFuncTests.cs` | `Like_PatternWithOptions_ReturnsExpectedMatch`、`GetNextId_VariousBaseAndId_ReturnsExpectedNextId` |
| `MemberPath` | `MemberPathTests.cs` | `Of_StaticProperty_ReturnsFullMemberPath` |

拆分完成後刪除 `BaseTests.cs`。

---

## 二、Bee.Db.UnitTests / `DbTests`

原類別混合了 3 個被測類別，且 `BuildSelectCommandTests.cs` 已存在：

| 被測類別 | 目標檔案 | 搬移的方法 |
|---------|---------|-----------|
| `DbAccess` | `DbAccessTests.cs`（新建） | `ExecuteDataTable_*`、`ExecuteNonQuery_*`、`ExecuteScalar_*`、`Query_*`、`QueryAsync_*`、`UpdateDataTable_*`、`ExecuteBatch_*`、`ExecuteBatchAsync_*`（共 10 個方法 + 內部類別 `User`、`User2`） |
| `SqlTableSchemaProvider` | `SqlTableSchemaProviderTests.cs`（新建） | `SqlTableSchemaProvider_GetTableSchema_ReturnsSchema` |
| `SqlFormCommandBuilder` | `BuildSelectCommandTests.cs`（已存在，追加） | `BuildSelectCommand_WithAndWithoutFields_ReturnsCommands`、`BuildSelectCommand_WithFilterAndSort_ReturnsCommands` |

拆分完成後刪除 `DbTests.cs`。

---

## 三、Bee.Api.Client.UnitTests / `ConnectTests`

原類別混合了 2 個被測類別：

| 被測類別 | 新測試檔案 | 搬移的方法 |
|---------|----------|-----------|
| `ApiConnectValidator` | `ApiConnectValidatorTests.cs` | `ApiConnectValidator_ValidUrl_ReturnsRemoteConnectType` |
| `SystemApiConnector` | `SystemApiConnectorTests.cs` | `SystemApiConnector_CreateSession_ReturnsValidToken` |

拆分完成後刪除 `ConnectTests.cs`。

---

## 四、Bee.Business.UnitTests / `BusinessTests`

所有方法都測試 `SystemBusinessObject`，僅需更名：

`BusinessTests.cs` → `SystemBusinessObjectTests.cs`

---

## 五、Bee.Api.Core.UnitTests / `ApiCoreTests`

原類別混合了序列化測試與 API 執行測試：

| 被測類別 | 目標檔案 | 搬移的方法 |
|---------|---------|-----------|
| `JsonRpcRequest` 序列化 | `JsonRpcSerializationTests.cs`（已存在，追加） | `JsonRpcRequest_Serialize_ReturnsValidJson` |
| `JsonRpcExecutor` | `JsonRpcExecutorTests.cs`（新建） | `Ping_ValidRequest_ReturnsOkStatus`、`GetCommonConfiguration_ValidRequest_ReturnsNotNull`、`ExecFunc_Hello_ReturnsNotNull`（含私有輔助方法 `ApiExecute<T>`、`GetAccessToken`） |

拆分完成後刪除 `ApiCoreTests.cs`。

---

## 六、Bee.Definition.UnitTests / `DefineTests`

原類別混合了功能測試與序列化測試：

| 被測類別 | 新測試檔案 | 搬移的方法 |
|---------|----------|-----------|
| `DefineFunc` | `DefineFuncTests.cs` | `GetDefineType_ValidType_ReturnsExpectedType` |
| 定義物件序列化 | `DefinitionSerializationTests.cs` | `SerializeListItems_*`、`SerializeParameters_*`、`SerializeSystemSettings_*`、`SerializePing_*`、`SerializeFilters_*`（含私有輔助方法 `SerializeObject<T>`、`CreateDataSet`、`CreateDataTable`） |

拆分完成後刪除 `DefineTests.cs`。

---

## 七、不變更的類別

以下類別命名已與被測類別對齊，不需調整：

- `ApiAspNetCoreTests`（測試 `ApiServiceController`，名稱反映模組層級，可接受）
- `CacheTests`（測試快取模組整合行為，可接受）
- `DbConnectionTests`、`FormSchemaTests` 等已對齊

---

## 執行順序

1. 建立新檔案並搬移方法
2. 追加方法至已存在的檔案
3. 刪除已清空的舊檔案
4. `dotnet build` 確認編譯通過
5. `dotnet test` 確認測試通過
