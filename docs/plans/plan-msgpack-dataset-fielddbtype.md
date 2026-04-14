# 計畫：MessagePack DataSet 欄位型別安全強化

## 背景

`SerializableDataTable` 在序列化 DataColumn 時，直接使用 `AssemblyQualifiedName` 字串傳遞型別資訊，反序列化時透過 `Type.GetType()` 直接載入。此路徑缺乏型別白名單驗證，攻擊者可透過竄改 payload 讓伺服器載入惡意型別。

雖然 `SafeTypelessFormatter` 已保護 row 值的型別安全，但 column 的 `DataType` 欄位完全未驗證。

## 目標

將 `SerializableDataColumn.DataType` 從任意型別字串改為 `FieldDbType` 列舉，限制為 13 種固定型別（排除 `Unknown`），消除任意型別載入風險。同時為未來 STJ 遷移時的 DataSet Converter 建立共用的型別對應基礎。

## 修改範圍

### 1. 新增型別對應工具（Bee.Base）

**檔案**：`src/Bee.Base/Data/FieldDbTypeMapper.cs`（新增）

建立 `FieldDbType` ↔ `System.Type` 的雙向對應：

```csharp
public static class FieldDbTypeMapper
{
    /// <summary>
    /// Converts a FieldDbType to its corresponding .NET Type.
    /// </summary>
    public static Type ToClrType(FieldDbType fieldDbType) => fieldDbType switch
    {
        FieldDbType.String => typeof(string),
        FieldDbType.Text => typeof(string),
        FieldDbType.Boolean => typeof(bool),
        FieldDbType.AutoIncrement => typeof(int),
        FieldDbType.Short => typeof(short),
        FieldDbType.Integer => typeof(int),
        FieldDbType.Long => typeof(long),
        FieldDbType.Decimal => typeof(decimal),
        FieldDbType.Currency => typeof(decimal),
        FieldDbType.Date => typeof(DateTime),
        FieldDbType.DateTime => typeof(DateTime),
        FieldDbType.Guid => typeof(Guid),
        FieldDbType.Binary => typeof(byte[]),
        _ => throw new ArgumentOutOfRangeException(nameof(fieldDbType), $"Unsupported FieldDbType: {fieldDbType}")
    };

    /// <summary>
    /// Converts a .NET Type to the closest FieldDbType.
    /// </summary>
    public static FieldDbType FromClrType(Type type);
}
```

`FromClrType` 需處理的對應（注意多對一）：
- `typeof(string)` → `FieldDbType.String`
- `typeof(bool)` → `FieldDbType.Boolean`
- `typeof(short)` → `FieldDbType.Short`
- `typeof(int)` → `FieldDbType.Integer`
- `typeof(long)` → `FieldDbType.Long`
- `typeof(decimal)` → `FieldDbType.Decimal`
- `typeof(DateTime)` → `FieldDbType.DateTime`
- `typeof(Guid)` → `FieldDbType.Guid`
- `typeof(byte[])` → `FieldDbType.Binary`
- 其他型別 → 拋出 `ArgumentOutOfRangeException`

### 2. 修改 SerializableDataColumn

**檔案**：`src/Bee.Api.Core/MessagePack/SerializableDataColumn.cs`

```diff
- [Key(1)]
- public string DataType { get; set; }
+ [Key(1)]
+ public FieldDbType DataType { get; set; }
```

### 3. 修改 SerializableDataTable

**檔案**：`src/Bee.Api.Core/MessagePack/SerializableDataTable.cs`

**FromDataTable()（序列化端）**：
```diff
  sdt.Columns.Add(new SerializableDataColumn
  {
      ColumnName = col.ColumnName,
-     DataType = col.DataType.AssemblyQualifiedName,
+     DataType = FieldDbTypeMapper.FromClrType(col.DataType),
      ...
  });
```

**ToDataTable()（反序列化端）**：
```diff
- var type = Type.GetType(col.DataType) ?? typeof(string);
+ var type = FieldDbTypeMapper.ToClrType(col.DataType);
```

### 4. 更新測試

**檔案**：`tests/Bee.Api.Core.UnitTests/MessagePackTests.cs`

- 現有測試應自動通過（行為未變，只是型別解析路徑不同）
- 新增測試：驗證不支援的型別（如 `typeof(object)`）在 `FromClrType` 時拋出例外

**檔案**：`tests/Bee.Base.UnitTests/`（新增 FieldDbTypeMapper 測試）

- 驗證所有 13 種 `FieldDbType` 的雙向對應正確性
- 驗證 `Unknown` 和不支援的型別拋出例外

## 注意事項

- **破壞性變更**：`SerializableDataColumn.DataType` 從 `string` 改為 `FieldDbType`（列舉），MessagePack 序列化格式會改變。此為內部 DTO，不影響外部 API 合約，但已序列化的資料需重新產生
- **共用基礎**：`FieldDbTypeMapper` 未來 STJ 的 `JsonConverter<DataSet>` 可直接復用
- `AutoIncrement` 對應 `int`，若實際資料庫使用 `long` 作為自增欄位，需確認是否需要調整

## 驗證方式

```bash
dotnet test tests/Bee.Base.UnitTests/Bee.Base.UnitTests.csproj
dotnet test tests/Bee.Api.Core.UnitTests/Bee.Api.Core.UnitTests.csproj
```
