# 計畫：測試專案類別與方法命名統一

## 目標

依照 `rules/testing.md` 的命名慣例，對所有測試專案進行一輪更名：

1. **類別名稱**：統一使用 `Tests` 複數結尾（目前有 11 個檔案用 `Test` 單數）
2. **方法名稱**：統一為三段式 `<方法名稱>_<情境>_<預期結果>`（目前約 20+ 個方法缺少預期結果段）

---

## 一、類別更名（Test → Tests）

包含檔名與類別名稱的同步更名，以及所有繼承關係的修正。

| 專案 | 原檔名 / 類別名 | 新檔名 / 類別名 |
|------|-----------------|-----------------|
| Bee.Tests.Shared | `BaseTest.cs` / `BaseTest` | `BaseTests.cs` / `BaseTests` |
| Bee.Api.AspNetCore.UnitTests | `ApiAspNetCoreTest.cs` / `ApiAspNetCoreTest` | `ApiAspNetCoreTests.cs` / `ApiAspNetCoreTests` |
| Bee.Api.Client.UnitTests | `ConnectTest.cs` / `ConnectTest` | `ConnectTests.cs` / `ConnectTests` |
| Bee.Api.Core.UnitTests | `ApiCoreTest.cs` / `ApiCoreTest` | `ApiCoreTests.cs` / `ApiCoreTests` |
| Bee.Base.UnitTests | `BaseTest.cs` / `BaseTest` | `BaseTests.cs` / `BaseTests` |
| Bee.Business.UnitTests | `BusinessTest.cs` / `BusinessTest` | `BusinessTests.cs` / `BusinessTests` |
| Bee.Db.UnitTests | `DbTest.cs` / `DbTest` | `DbTests.cs` / `DbTests` |
| Bee.Db.UnitTests | `DbConnectionTest.cs` / `DbConnectionTest` | `DbConnectionTests.cs` / `DbConnectionTests` |
| Bee.Definition.UnitTests | `DefineTest.cs` / `DefineTest` | `DefineTests.cs` / `DefineTests` |
| Bee.Definition.UnitTests | `FormSchemaTest.cs` / `FormSchemaTest` | `FormSchemaTests.cs` / `FormSchemaTests` |
| Bee.ObjectCaching.UnitTests | `CacheTest.cs` / `CacheTest` | `CacheTests.cs` / `CacheTests` |
| Bee.Repository.UnitTests | — | （已為 `SessionRepositoryTests`，無需更改）|

> 注意：`BaseTests`（Bee.Tests.Shared）為抽象基底類別，多個測試類別繼承它（`: BaseTest` → `: BaseTests`）。

---

## 二、方法更名（補齊三段式）

### Bee.Api.Core.UnitTests / MessagePackTests.cs

| 原名 | 新名 |
|------|------|
| `DataSet_Serialize` | `DataSet_Serialize_RoundTrip` |
| `DataTable_Serialize` | `DataTable_Serialize_RoundTrip` |
| `DataTable_Serialize_DbNull` | `DataTable_SerializeWithDbNull_PreservesValues` |
| `DataTable_Serialize_RowState` | `DataTable_SerializeWithRowState_PreservesState` |
| `TListItemCollection_Serialize` | `TListItemCollection_Serialize_RoundTrip` |
| `TParameterCollection_Serialize` | `TParameterCollection_Serialize_RoundTrip` |
| `TParameterCollection_Serialize_DataTable` | `TParameterCollection_SerializeWithDataTable_RoundTrip` |
| `TPropertyCollection_Serialize` | `TPropertyCollection_Serialize_RoundTrip` |
| `Filters_Serialize` | `Filters_Serialize_RoundTrip` |
| `Ping_Serialize` | `Ping_Serialize_RoundTrip` |
| `ExecFunc_Serialize` | `ExecFunc_Serialize_RoundTrip` |
| `CreateSession_Serialize` | `CreateSession_Serialize_RoundTrip` |
| `GetDefine_Serialize` | `GetDefine_Serialize_RoundTrip` |
| `GetCommonConfiguration_Serialize` | `GetCommonConfiguration_Serialize_RoundTrip` |
| `SaveDefine_Serialize` | `SaveDefine_Serialize_RoundTrip` |

### Bee.Base.UnitTests / JsonDataSetSerializationTests.cs

| 原名 | 新名 |
|------|------|
| `DataTable_JsonSerialize_RoundTrip` | （已符合，不變）|
| `DataTable_JsonSerialize_DbNull` | `DataTable_JsonSerializeWithDbNull_PreservesValues` |
| `DataTable_JsonSerialize_RowState` | `DataTable_JsonSerializeWithRowState_PreservesState` |
| `DataTable_JsonSerialize_ColumnMetadata` | `DataTable_JsonSerialize_PreservesColumnMetadata` |
| `DataTable_JsonSerialize_PrimaryKey` | `DataTable_JsonSerialize_PreservesPrimaryKey` |
| `DataTable_JsonSerialize_EmptyTable` | `DataTable_JsonSerializeEmptyTable_RoundTrip` |
| `DataTable_JsonSerialize_AllNullRow` | `DataTable_JsonSerializeAllNullRow_RoundTrip` |
| `DataSet_JsonSerialize_RoundTrip` | （已符合，不變）|
| `DataSet_JsonSerialize_WithRelation` | `DataSet_JsonSerializeWithRelation_PreservesRelation` |
| `DataSet_JsonSerialize_EmptyDataSet` | `DataSet_JsonSerializeEmptyDataSet_RoundTrip` |
| `DataTable_JsonSerialize_Null_ReturnsNull` | （已符合，不變）|
| `DataTable_JsonSerialize_ModifiedRow_PreservesOriginalValues` | （已符合，不變）|
| `DataTable_JsonSerialize_DeletedRow_PreservesOriginalValues` | （已符合，不變）|

---

## 三、執行順序

1. 先更名 `Bee.Tests.Shared/BaseTest` → `BaseTests`（基底類別）
2. 更名所有繼承 `BaseTest` 的子類別中的 `: BaseTest` → `: BaseTests`
3. 更名各專案的測試類別（Test → Tests）與檔名
4. 更名方法名稱
5. 執行 `dotnet build` 確認編譯通過
6. 執行 `dotnet test` 確認測試通過

---

## 四、不變更範圍

- `Bee.Tests.Shared` 中的 `GlobalCollection.cs`、`GlobalFixture.cs`、`LocalOnlyFactAttribute.cs`、`LocalOnlyTheoryAttribute.cs` — 非測試類別，不適用此規則
- `TestFunc.cs`（Bee.Api.Core.UnitTests、Bee.Definition.UnitTests）— 輔助類別，非測試類別
- 已符合三段式命名的方法 — 不做無意義變更
