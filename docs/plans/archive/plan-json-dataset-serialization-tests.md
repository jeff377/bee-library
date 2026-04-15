# 計畫：JSON DataSet 序列化測試方法

## 目標

參照 MessagePack 的 `SerializableDataSet` / `SerializableDataTable` 中間 DTO 模式，為未來的 STJ（System.Text.Json）DataSet 序列化實作 **事先定義完整的測試方法**。

測試路徑為 `DataSet → SerializableDataSet → JSON → SerializableDataSet → DataSet`，透過中間 DTO 確保完整保留所有元資料（Newtonsoft.Json 原生 DataSet 序列化不保留 TableName、RowState、PrimaryKey、DataRelation、欄位中繼資料等）。涵蓋所有 `FieldDbType` 欄位型別、RowState 保留、DBNull 處理、DataRelation、PrimaryKey、邊界條件等。

## 設計原則

1. **測試先行**：先寫測試定義完整的預期行為，作為 STJ Converter 實作的驗收標準
2. **參照 MessagePack 測試模式**：對齊現有 `MessagePackTests.cs` 的測試結構（`DataSet_Serialize`、`DataTable_Serialize`、`DataTable_Serialize_DbNull`、`DataTable_Serialize_RowState`），擴充更完整的覆蓋
3. **使用現有工具**：複用 `DataTableComparer.IsEqual()` 做 DataTable 比對
4. **遵循測試規範**：方法命名 `<方法>_<情境>_<預期>` 格式，加 `[DisplayName]` 中文描述

## 測試檔案位置

`tests/Bee.Api.Core.UnitTests/JsonDataSetSerializationTests.cs`

> 放在 Bee.Api.Core.UnitTests，因為 `SerializableDataSet` / `SerializableDataTable` 中間 DTO 位於 `Bee.Api.Core`。

## 測試方法清單

### 一、DataTable 基本序列化（對齊 MessagePack 現有測試）

| # | 方法名稱 | DisplayName | 說明 |
|---|---------|-------------|------|
| 1 | `DataTable_JsonSerialize_RoundTrip` | DataTable JSON 序列化往返 | 基本 string + int 欄位，驗證 TableName、欄位數、列數、值正確還原 |
| 2 | `DataTable_JsonSerialize_DbNull` | DataTable JSON 序列化處理 DBNull 值 | 含 DBNull 欄位，還原後 `row.IsNull()` 為 true |
| 3 | `DataTable_JsonSerialize_RowState` | DataTable JSON 序列化保留 RowState | 四種 RowState（Added / Modified / Deleted / Unchanged）全部保留 |

### 二、FieldDbType 全型別覆蓋（13 種固定型別）

使用 `[Theory]` + `[MemberData]` 參數化，確保每種 FieldDbType 對應的 .NET 型別都能正確序列化往返。

| # | 方法名稱 | DisplayName | 說明 |
|---|---------|-------------|------|
| 4 | `DataTable_JsonSerialize_AllFieldDbTypes` | DataTable JSON 序列化支援所有 FieldDbType 欄位型別 | 參數化測試，每個 FieldDbType 搭配對應的測試值 |

測試資料（`MemberData`）：

| FieldDbType | .NET 型別 | 測試值 |
|-------------|----------|--------|
| String | `string` | `"Hello 測試"` |
| Text | `string` | `"Long text content..."` |
| Boolean | `bool` | `true` |
| AutoIncrement | `int` | `42` |
| Short | `short` | `(short)12345` |
| Integer | `int` | `2147483647`（int.MaxValue） |
| Long | `long` | `9223372036854775807L`（long.MaxValue） |
| Decimal | `decimal` | `123456.789m` |
| Currency | `decimal` | `99999.99m` |
| Date | `DateTime` | `new DateTime(2026, 4, 15)` |
| DateTime | `DateTime` | `new DateTime(2026, 4, 15, 10, 30, 45)` |
| Guid | `Guid` | `Guid.NewGuid()` 固定種子 |
| Binary | `byte[]` | `new byte[] { 0x01, 0x02, 0xFF }` |

### 三、DataSet 多表與關聯

| # | 方法名稱 | DisplayName | 說明 |
|---|---------|-------------|------|
| 5 | `DataSet_JsonSerialize_RoundTrip` | DataSet JSON 序列化往返 | 含兩個 DataTable，驗證 DataSetName、Tables.Count、每表資料正確 |
| 6 | `DataSet_JsonSerialize_WithRelation` | DataSet JSON 序列化保留 DataRelation | Master-Detail 關聯（Order → OrderDetail），驗證 RelationName、ParentTable、ChildTable、ParentColumns、ChildColumns |

### 四、欄位中繼資料保留

| # | 方法名稱 | DisplayName | 說明 |
|---|---------|-------------|------|
| 7 | `DataTable_JsonSerialize_ColumnMetadata` | DataTable JSON 序列化保留欄位中繼資料 | 驗證 AllowDBNull、ReadOnly、MaxLength、Caption（DisplayName）、DefaultValue 正確還原 |
| 8 | `DataTable_JsonSerialize_PrimaryKey` | DataTable JSON 序列化保留 PrimaryKey | 設定複合主鍵，還原後 `PrimaryKey` 欄位名稱與順序正確 |

### 五、RowState 細節驗證

| # | 方法名稱 | DisplayName | 說明 |
|---|---------|-------------|------|
| 9 | `DataTable_JsonSerialize_ModifiedRow_PreservesOriginalValues` | DataTable JSON 序列化 Modified 資料列保留原始值 | Modified 資料列的 `row[col, DataRowVersion.Original]` 與 `row[col, DataRowVersion.Current]` 皆正確 |
| 10 | `DataTable_JsonSerialize_DeletedRow_PreservesOriginalValues` | DataTable JSON 序列化 Deleted 資料列保留原始值 | Deleted 資料列只能讀取 Original 版本，值正確 |

### 六、邊界條件

| # | 方法名稱 | DisplayName | 說明 |
|---|---------|-------------|------|
| 11 | `DataTable_JsonSerialize_EmptyTable` | DataTable JSON 序列化空資料表 | 有欄位定義但無資料列，還原後 Rows.Count == 0，Columns 正確 |
| 12 | `DataTable_JsonSerialize_AllNullRow` | DataTable JSON 序列化全 null 資料列 | 所有欄位為 DBNull，還原後每欄 `IsNull()` 為 true |
| 13 | `DataSet_JsonSerialize_EmptyDataSet` | DataSet JSON 序列化空 DataSet | 無任何 DataTable，還原後 Tables.Count == 0 |
| 14 | `DataSet_JsonSerialize_Null_ReturnsNull` | DataSet JSON 序列化 null 值 | `ObjectToJson(null)` → `JsonToObject<DataSet>(json)` 應為 null |

## 輔助方法

在測試類別中定義以下 helper：

```csharp
/// <summary>
/// 執行 JSON 序列化往返，回傳反序列化結果。
/// </summary>
private static T JsonRoundTrip<T>(T value)
{
    string json = SerializeFunc.ObjectToJson(value, includeTypeName: true);
    return SerializeFunc.JsonToObject<T>(json, includeTypeName: true);
}
```

## 實作注意事項

- 目前 `SerializeFunc.ObjectToJson()` 底層使用 Newtonsoft.Json，**原生支援 DataSet**，因此這些測試現在就能通過
- 當未來切換至 STJ 時，這些測試將作為回歸驗收標準：若任一測試失敗，表示 STJ Converter 實作不完整
- `byte[]` 型別在 JSON 中以 Base64 編碼，需確認往返後 `SequenceEqual` 相等
- `DateTime` 精度需注意 JSON 序列化格式差異，比較時可用 `Assert.Equal` 搭配適當精度

## 預估影響

- 新增 1 個測試檔案：`tests/Bee.Base.UnitTests/JsonDataSetSerializationTests.cs`
- 可能需要確認 `Bee.Base.UnitTests.csproj` 已引用 `System.Data` 相關套件
