# 計畫：重構 `DefineFunc` 為 .NET idiomatic

**狀態：✅ 已完成（2026-05-01）**

> 主計畫:[plan-funcs-to-net-idiomatic.md](plan-funcs-to-net-idiomatic.md)

## 目前內容

`src/Bee.Definition/DefineFunc.cs`(131 行,**5 個方法** = 2 public + 3 internal)

```csharp
namespace Bee.Definition;

public static class DefineFunc
{
    private static readonly Dictionary<DefineType, string> DefineTypeNames = ...; // 7 個對映

    public static Type GetDefineType(DefineType defineType);            // public
    public static string GetNumberFormatString(string numberFormat);    // public
    internal static ColumnControlType ToColumnControlType(ControlType type);  // internal
    internal static LayoutColumn ToLayoutColumn(FormField field);             // internal
    internal static LayoutGrid GetListLayout(FormSchema formDefine);          // internal
}
```

> 主計畫進度表寫 3 個方法,實際 audit 後 5 個。Internal 3 個是 `FormSchema.GetListLayout()` 的內部實作鏈條。

## Method Audit 表

| # | 方法簽章 | 處理路徑 | 新位置/名稱 | 替代方案備註 |
|---|---------|--------|------------|------------|
| 1 | `GetDefineType(DefineType)` | B | `DefineTypeExtensions.ToClrType(this DefineType)` | 新檔 `src/Bee.Definition/DefineTypeExtensions.cs` |
| 2 | `GetNumberFormatString(string)` | C | `NumberFormatPresets.ToFormatString(string preset)` | 新檔 `src/Bee.Definition/NumberFormatPresets.cs` |
| 3 | `ToColumnControlType(ControlType)` | D | `FormSchema` 內 `private static` helper | 僅 internal,搬入 `FormSchema` 即夠 |
| 4 | `ToLayoutColumn(FormField)` | D | `FormSchema` 內 `private static` helper | 同上 |
| 5 | `GetListLayout(FormSchema)` | D | 直接合併進 `FormSchema.GetListLayout()` instance 方法 | 等於消除 `FormSchema → DefineFunc → FormSchema` 環呼叫 |

5 個方法都搬走後,`DefineFunc.cs` 整個刪除。

### 1. `GetDefineType` — path B 細節

**現行呼叫**:
```csharp
var type = DefineFunc.GetDefineType(args.DefineType);
```

**轉擴充方法後**:
```csharp
var type = args.DefineType.ToClrType();
```

**為何選 path B 而非 C(改名為靜態類別)**:
- 第一參數 `DefineType` 是 domain enum,有明確主體
- `defineType.ToClrType()` 比 `DefineTypeMap.GetClrType(defineType)` 對 IDE IntelliSense 更友善
- `To*` 是 .NET 慣用的「轉換」方法前綴(對齊 `int.ToString()`、`enumValue.ToString()` 等)

**新類別**:`public static class DefineTypeExtensions`,放 `src/Bee.Definition/DefineTypeExtensions.cs`,namespace 沿用 `Bee.Definition`。

**注意事項**:現行 `DefineTypeNames` 私有 dictionary 沿用,順手把 `assembly.GetType(typeName)` 之後的錯誤路徑保留,例外行為(`NotSupportedException`)不變。

### 2. `GetNumberFormatString` — path C 細節

**現行邏輯**:把 `"Quantity"` / `"UnitPrice"` / `"Amount"` / `"Cost"` 等字串 map 到 .NET 格式字串(`"N0"` / `"N2"` / `"N4"`)。

**設計脈絡(2026-05-01 確認)**:
- 這是**框架級的命名約定**,不是 UI 層臨時規則
- 現行 `FormField.NumberFormat` 屬性以字串承載這些命名(`"Amount"` 等)
- 未來 `FormField` 會新增更明確的語意屬性(可能是 enum,描述欄位是單價/金額/數量/成本),屆時這個 mapping 會反向被它呼叫
- 因此必須**保留方法**,只 rehome 到合適位置

**新類別**:`public static class NumberFormatPresets`,放 `src/Bee.Definition/NumberFormatPresets.cs`,namespace 沿用 `Bee.Definition`。
- 「Presets」清楚表達是「框架預定的命名組」
- 不用 `NumberFormat`(易與 BCL `System.Globalization.NumberFormatInfo` 混淆)
- 不用 `FieldNumberFormat`(過早綁定 `FormField` 用途)

**新方法**:`public static string ToFormatString(string preset)`
- 沿 .NET `To*` 轉換命名慣例
- 類名已含 `NumberFormat`,方法名不再重複(原 `GetNumberFormatString` 改為 `ToFormatString`)

**現在不做(避免「為假設的未來設計」)**:
- ❌ 不預先抽 `const string Quantity = "Quantity"` 等常數 —— 等真的有屬性引用才加
- ❌ 不預先建 `NumberFormatPreset` enum —— 等對應 `FormField` 屬性確定加入時才一起做
- ❌ 不預先設計 enum overload —— YAGNI

**為何不走 path B(`string` 擴充方法)**:
- `"Amount".ToNumberFormatString()` 會污染所有 `string` 的 IntelliSense
- 跟先前 `byte[]` 不擴充 `Gzip` 同樣理由,不擴充過度通用型別

### 3-5. Internal 三個方法 — path D 細節

`ToColumnControlType` / `ToLayoutColumn` / `GetListLayout` 都是 internal,且形成這條呼叫鏈:

```
FormSchema.GetListLayout()  [public instance]
    └── DefineFunc.GetListLayout(this)  [internal]
            └── DefineFunc.ToLayoutColumn(field)  [internal]
                    └── DefineFunc.ToColumnControlType(field.ControlType)  [internal]
```

`FormSchema.GetListLayout()` 只是個 1-line wrapper(`return DefineFunc.GetListLayout(this);`),邏輯全在 `DefineFunc`。這是**典型的 wrapper-around-utility** 反模式 —— 邏輯應該住在 `FormSchema` 自己,不需要繞外面再回來。

**搬遷方案**:
- `GetListLayout(FormSchema)` 邏輯**直接內聯**到 `FormSchema.GetListLayout()`,引用 `this` 取代參數
- `ToLayoutColumn(FormField)` 變 `FormSchema` 的 `private static` helper
- `ToColumnControlType(ControlType)` 變 `FormSchema` 的 `private static` helper(順便改寫成 switch expression,對齊 .NET idiom)

`StrFunc.Split` 等 `*Func` 引用**保留不動**(那是 main plan 第 11 步 `StrFunc` 的範圍,別在這次 commit 預先動)。

## 影響範圍

**全 repo grep `DefineFunc` 結果(扣除 `bin/obj`)**:

| 類型 | 檔案 | 出現次數 |
|------|------|---------|
| 產品(類別定義) | `src/Bee.Definition/DefineFunc.cs` | 1 |
| 產品(`GetDefineType` caller) | `src/Bee.Business/System/SystemBusinessObject.cs:203` | 1 |
| 產品(`GetListLayout` caller) | `src/Bee.Definition/Forms/FormSchema.cs:148` | 1(將被內聯消除) |
| 測試 | `tests/Bee.Definition.UnitTests/DefineFuncTests.cs` | 4 直接呼叫(`GetDefineType` x 2、`GetNumberFormatString` x 2) |
| 文件 | `docs/plans/plan-funcs-to-net-idiomatic.md` | 1 |

`GetListLayout` 相關 5 個測試本來就用 `schema.GetListLayout()` 呼叫(沒有直接打 `DefineFunc`),搬到 `FormSchemaTests.cs` 不需改 assertion 邏輯。

## 執行步驟

### 1. 新增 `DefineTypeExtensions.cs`

`src/Bee.Definition/DefineTypeExtensions.cs`(新檔):

```csharp
namespace Bee.Definition;

/// <summary>
/// Extension methods for <see cref="DefineType"/>.
/// </summary>
public static class DefineTypeExtensions
{
    private static readonly Dictionary<DefineType, string> DefineTypeNames = new()
    {
        { DefineType.SystemSettings,   "Bee.Definition.Settings.SystemSettings" },
        { DefineType.DatabaseSettings, "Bee.Definition.Settings.DatabaseSettings" },
        { DefineType.DbSchemaSettings, "Bee.Definition.Settings.DbSchemaSettings" },
        { DefineType.ProgramSettings,  "Bee.Definition.Settings.ProgramSettings" },
        { DefineType.TableSchema,      "Bee.Definition.Database.TableSchema" },
        { DefineType.FormSchema,       "Bee.Definition.Forms.FormSchema" },
        { DefineType.FormLayout,       "Bee.Definition.Layouts.FormLayout" },
    };

    /// <summary>
    /// Gets the CLR type for the specified define type.
    /// </summary>
    /// <param name="defineType">The define data type.</param>
    /// <exception cref="NotSupportedException">Thrown when the define type is not registered.</exception>
    public static Type ToClrType(this DefineType defineType)
    {
        if (!DefineTypeNames.TryGetValue(defineType, out string? typeName))
            throw new NotSupportedException($"Type not found: {defineType}");
        var assembly = typeof(DefineTypeExtensions).Assembly;
        var type = assembly.GetType(typeName);
        if (type == null)
            throw new NotSupportedException($"Type not found: {typeName}");
        return type;
    }
}
```

### 2. 把 `GetListLayout` 鏈內聯到 `FormSchema`

`src/Bee.Definition/Forms/FormSchema.cs`:把現行 1-line wrapper 換成完整實作 + 兩個 private helper。

(完整內容見 `## 預期 FormSchema 變更`)

### 3. 更新生產端 caller

`src/Bee.Business/System/SystemBusinessObject.cs:203`:
```csharp
// 改前
var type = DefineFunc.GetDefineType(args.DefineType);
// 改後
var type = args.DefineType.ToClrType();
```

### 4. 刪除 `DefineFunc.cs`

5 個方法都搬完後,`src/Bee.Definition/DefineFunc.cs` 整個刪除。

### 5. 拆解測試

`tests/Bee.Definition.UnitTests/DefineFuncTests.cs` 拆成兩處:

#### 5a. `GetDefineType` 測試 → 新檔 `DefineTypeExtensionsTests.cs`

- 建新檔 `tests/Bee.Definition.UnitTests/DefineTypeExtensionsTests.cs`
- 搬 `GetDefineType_ValidType_ReturnsExpectedType`(7 個 InlineData)+ `GetDefineType_UnsupportedType_ThrowsNotSupportedException`(1 個 Fact)
- 改名:`GetDefineType_*` → `ToClrType_*`,呼叫改 `defineType.ToClrType()`

#### 5b. `GetListLayout` 測試 → 加到既有 `FormSchemaTests.cs`

- 加到 `tests/Bee.Definition.UnitTests/FormSchemaTests.cs`(已存在)
- 搬 `GetListLayout_ValidSchema_ContainsRowIdAndListFields` 等 5 個 test method,assertion 與呼叫不變(原本就是 `schema.GetListLayout()`)

#### 5c. `GetNumberFormatString` 測試 → 新檔 `NumberFormatPresetsTests.cs`

- 建新檔 `tests/Bee.Definition.UnitTests/NumberFormatPresetsTests.cs`
- 搬 `GetNumberFormatString_KnownFormat_ReturnsExpectedString`(5 個 InlineData)+ `GetNumberFormatString_EmptyOrUnknown_ReturnsEmpty`(3 個 InlineData)
- 改名:`GetNumberFormatString_*` → `ToFormatString_*`,呼叫改 `NumberFormatPresets.ToFormatString(...)`
- assertion 與 InlineData 不變

#### 5d. 刪除原 `DefineFuncTests.cs`

### 6. 更新主計畫

進度表第 3 列:`📝` → `✅`,完成日填入,方法數 `3` → `5`(2 public + 3 internal)。

## 預期 FormSchema 變更

```csharp
// FormSchema.cs(改後)

/// <summary>
/// Gets the list layout for this form schema.
/// </summary>
public LayoutGrid GetListLayout()
{
    var table = MasterTable;
    string[] fieldNames = StrFunc.Split(ListFields, ",");

    var grid = new LayoutGrid { TableName = ProgId };
    // Add sys_RowID hidden column
    grid.Columns!.Add(SysFields.RowId, "Row ID", ColumnControlType.TextEdit).Visible = false;
    // Add list display columns
    foreach (string fieldName in fieldNames)
    {
        var field = table!.Fields![fieldName];
        if (field != null)
        {
            grid.Columns.Add(ToLayoutColumn(field));
        }
    }
    return grid;
}

private static LayoutColumn ToLayoutColumn(FormField field)
{
    var column = new LayoutColumn(field.FieldName, field.Caption, ToColumnControlType(field.ControlType));
    column.Width = field.Width > 0 ? field.Width : 120;
    column.DisplayFormat = field.DisplayFormat;
    column.NumberFormat = field.NumberFormat;
    return column;
}

private static ColumnControlType ToColumnControlType(ControlType type) => type switch
{
    ControlType.TextEdit       => ColumnControlType.TextEdit,
    ControlType.ButtonEdit     => ColumnControlType.ButtonEdit,
    ControlType.DateEdit       => ColumnControlType.DateEdit,
    ControlType.YearMonthEdit  => ColumnControlType.YearMonthEdit,
    ControlType.DropDownEdit   => ColumnControlType.DropDownEdit,
    ControlType.CheckEdit      => ColumnControlType.CheckEdit,
    _                          => ColumnControlType.TextEdit,
};
```

## 驗證

```bash
# 確認沒有遺漏的 DefineFunc 引用
grep -rn "DefineFunc" /Users/jeff/Desktop/repos/bee-library --include="*.cs" --exclude-dir=bin --exclude-dir=obj

# Build
dotnet build src/Bee.Definition/Bee.Definition.csproj --configuration Release --no-restore
dotnet build src/Bee.Business/Bee.Business.csproj --configuration Release --no-restore

# Test
./test.sh tests/Bee.Definition.UnitTests/Bee.Definition.UnitTests.csproj
./test.sh tests/Bee.Business.UnitTests/Bee.Business.UnitTests.csproj
```

預期結果:
- `grep` 應只剩 `docs/plans/` 內的歷史紀錄
- Build 0 warning, 0 error
- 測試:Bee.Definition.UnitTests 數量**不變**(原 `DefineFuncTests` 拆 3 處後總數一致),全綠

## Commit 訊息草稿

```
refactor(definition): split DefineFunc — extension, presets class, FormSchema absorbs list-layout

GetDefineType becomes DefineTypeExtensions.ToClrType (path B —
extension on the domain enum, callers read defineType.ToClrType()).

GetNumberFormatString moves to NumberFormatPresets.ToFormatString
(path C — preserves the framework-level naming convention that maps
"Quantity"/"UnitPrice"/"Amount"/"Cost" to .NET format strings;
future-proofs for FormField gaining a typed semantic property).

GetListLayout and its two internal helpers (ToLayoutColumn,
ToColumnControlType) are inlined into FormSchema.GetListLayout()
(path D — eliminates the FormSchema → DefineFunc → FormSchema wrapper
loop, helpers become private static methods on FormSchema).

DefineFunc.cs is removed entirely. Tests split: GetDefineType cases
move to DefineTypeExtensionsTests, GetNumberFormatString cases move
to NumberFormatPresetsTests, GetListLayout cases move to the existing
FormSchemaTests.

Third class executed under the *Func → .NET idiomatic refactor (see
docs/plans/plan-funcs-to-net-idiomatic.md).
```

## 跨類別決策落地

本次定下的 idiom,後續沿用:

- **Path C 命名延伸**:當 `*Func` 內方法承載「框架級命名約定」的查表(本例 `"Amount" → "N2"`),改名到一個能呈現「這是預設組」語意的類別 —— 例 `NumberFormatPresets`(而非 `NumberFormatNames` 之類過弱的名稱);類名已含領域字眼時,方法名不再重複(`ToFormatString` 而非 `ToNumberFormatString`)
- **Path D 進一步**:當原 `*Func` 方法是某 domain object 上 instance method 的「外包裝實作」(`FormSchema.GetListLayout` → `DefineFunc.GetListLayout`),應**直接內聯**回該 instance method,連帶私有 helper 也搬入,徹底消除 wrapper 環呼叫
- **Enum 擴充方法**:domain enum 的轉換/查詢方法用 `<EnumName>Extensions.ToXxx(this EnumName)` 命名(對齊 path B + .NET `To*` 慣例)
- **不為假設的未來設計**:即便已預期某個 API 未來會擴充(本例 `NumberFormatPresets` 未來可能加 enum overload),**現在只搬最低限度的內容**,不預先抽常數、enum、overload。等真的需要時再加

## 風險與回滾

- 變動範圍:1 處生產端 caller(`SystemBusinessObject`)+ `FormSchema` 內部重構 + 測試重組
- Public API breaking 範圍:`DefineFunc.GetDefineType` / `DefineFunc.GetNumberFormatString` 移到新類別與新方法名,無外部 NuGet 消費者
- 若失敗單一 `git revert` 即可回滾
