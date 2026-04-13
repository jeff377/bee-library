# Plan: 序列化安全性審查與修復

## 背景

API 傳輸支援兩種序列化格式（JSON / MessagePack），皆採用命名空間白名單策略限制可反序列化的型別。審查後發現以下問題：

| 嚴重度 | 問題 | 影響範圍 |
|--------|------|---------|
| **嚴重** | `SafeTypelessFormatter` 在物件實例化**之後**才驗證型別 | MessagePack 反序列化 |
| **中等** | `DataTable` / `DataRow` 列入無條件允許的原始型別白名單 | `SafeTypelessFormatter` |
| **中等** | `AllowedTypeNamespaces` 為 public mutable List | `SysInfo` |
| **低** | `BuildAllowedTypeNamespaces` 與 `IsTypeNameAllowed` 大小寫比較不一致 | `SysInfo` |

---

## 修復項目

### 1. [嚴重] SafeTypelessFormatter — 改為預先驗證型別名稱

**問題**：目前 `SafeTypelessFormatter.Deserialize()` 先呼叫 `TypelessFormatter.Instance.Deserialize()` 完成物件建構，再以 `ValidateType()` 檢查型別。惡意型別的建構函式/屬性 setter 在驗證前已執行。

**修復方案**：在呼叫 `TypelessFormatter` 之前，先從 MessagePack payload 讀取型別名稱字串並驗證。

MessagePack `TypelessFormatter` 的序列化格式為 2-element array：`[typeName, serializedData]`。可利用 `MessagePackReader`（struct，可複製）先窺探型別名稱，驗證通過後再用原始 reader 交給 `TypelessFormatter` 處理。

**修改檔案**：`src/Bee.Definition/Serialization/SafeTypelessFormatter.cs`

```csharp
public object Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
{
    if (reader.TryReadNil())
        return null;

    // 複製 reader 以窺探型別名稱（MessagePackReader 是 struct，複製不影響原始 reader）
    var peekReader = reader.CreatePeekReader();

    // TypelessFormatter 使用 2-element array: [typeName, data]
    var arrayLength = peekReader.ReadArrayHeader();
    if (arrayLength >= 1)
    {
        // 讀取型別名稱（第一個元素）
        var typeName = peekReader.ReadString();
        if (typeName != null)
        {
            ValidateTypeName(typeName);
        }
    }

    // 驗證通過，使用原始 reader 進行完整反序列化
    return TypelessFormatter.Instance.Deserialize(ref reader, options);
}
```

新增 `ValidateTypeName(string typeName)` 方法，在取得 `Type` 物件之前就以字串比對白名單。原有的 `ValidateType(Type type)` 保留作為第二道防線（belt-and-suspenders）。

**測試**：
- 新增測試：序列化一個不在白名單的型別，驗證反序列化時拋出 `InvalidOperationException`，且物件**未被建構**
- 新增測試：白名單內的型別可正常往返序列化/反序列化
- 新增測試：原始型別（System.Int32, System.String 等）可正常反序列化

---

### 2. [中等] 將 DataTable / DataRow 從原始型別白名單移除

**問題**：`DataTable` 是已知高風險反序列化型別（可攜帶 Expression 欄位觸發程式碼執行），不應列入「無條件允許」清單。

**修復方案**：從 `AllowedPrimitiveTypes` 移除 `System.Data.DataTable` 和 `System.Data.DataRow`。這兩個型別在 MessagePack 管道中已有專屬的 `DataTableFormatter` / `DataSetFormatter` 處理，不需要經由 `SafeTypelessFormatter` 的原始型別白名單通行。

**修改檔案**：`src/Bee.Definition/Serialization/SafeTypelessFormatter.cs`

**測試**：
- 確認 DataTable 仍可透過 `DataTableFormatter` 正常序列化/反序列化
- 確認透過 `SafeTypelessFormatter` 路徑的 DataTable 型別名稱會被攔截

---

### 3. [中等] AllowedTypeNamespaces 改為唯讀集合

**問題**：`SysInfo.AllowedTypeNamespaces` 是 `public static List<string>` 且有 `set`，任何程式碼都能在執行期替換或清空白名單。

**修復方案**：
- 將屬性型別改為 `IReadOnlyList<string>`
- 移除 public setter，改為僅透過 `Initialize()` 方法設定
- 內部儲存為 `List<string>`，對外公開為 `IReadOnlyList<string>`

**修改檔案**：`src/Bee.Base/SysInfo.cs`

```csharp
private static List<string> _allowedTypeNamespaces = new List<string> { "Bee.Base", "Bee.Definition", "Bee.Contracts" };

/// <summary>
/// Gets the list of type namespaces allowed for JSON-RPC data transfer (read-only).
/// Use <see cref="Initialize"/> to configure.
/// </summary>
public static IReadOnlyList<string> AllowedTypeNamespaces => _allowedTypeNamespaces;
```

**注意**：此變更會影響外部直接指定 `AllowedTypeNamespaces = ...` 的程式碼，需檢查所有參考點。

**修改檔案**：`src/Bee.Base/SysInfo.cs` 及所有直接存取 `AllowedTypeNamespaces` setter 的位置

**測試**：
- 確認 `Initialize()` 能正確設定命名空間
- 確認 `AllowedTypeNamespaces` 不可被外部直接替換

---

### 4. [低] 統一大小寫比較策略

**問題**：`BuildAllowedTypeNamespaces` 用 `StringComparer.OrdinalIgnoreCase` 去重，但 `IsTypeNameAllowed` 的 `StartsWith` 預設為 Ordinal（區分大小寫）。

**修復方案**：將 `BuildAllowedTypeNamespaces` 改為 `StringComparer.Ordinal`，與 `IsTypeNameAllowed` 的行為一致。.NET 型別名稱本身區分大小寫，兩端應統一。

**修改檔案**：`src/Bee.Base/SysInfo.cs`

---

## 執行順序

1. 修復項目 1（SafeTypelessFormatter 預先驗證）— 最高優先
2. 修復項目 2（移除 DataTable/DataRow）
3. 修復項目 3（AllowedTypeNamespaces 唯讀化）
4. 修復項目 4（大小寫一致性）
5. 執行全部相關測試，確認無回歸

## 影響範圍

- `Bee.Definition`：SafeTypelessFormatter
- `Bee.Base`：SysInfo
- `Bee.Api.Core`：MessagePackHelper（間接，無需修改）
- 測試專案：`Bee.Definition.UnitTests`、`Bee.Base.UnitTests`
