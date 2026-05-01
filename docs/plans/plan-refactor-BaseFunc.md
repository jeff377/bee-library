# 計畫：重構 `BaseFunc` 為 .NET idiomatic

**狀態：✅ 已完成（2026-05-01）**

> 主計畫:[plan-funcs-to-net-idiomatic.md](plan-funcs-to-net-idiomatic.md)

## 目前內容

`src/Bee.Base/BaseFunc.cs`(772 行,**47 個 method 簽章**)

這是最後一個 `*Func` 類別,也是最雜的 —— 涵蓋 6 類關注:
1. 值檢查(`IsDBNull` / `IsEmpty` / `IsNullOrDBNull` 等 9 個 overload)
2. 型別轉換(`Cxxx` 家族 13 個方法 + `IsNumeric` / `ConvertToNumber`)
3. 序列化助手(`SetSerializeState` / `IsSerializeEmpty`)
4. Reflection 助手(`GetAttribute` / `GetPropertyValue` 等 5 個)
5. 雜項(`NewGuid` / `RndInt` / `CreateInstance` / `GetCommandLineArgs` / `EnsureNotNullOrWhiteSpace` / `UnwrapException`)
6. 命令列參數解析

> 主計畫進度表寫 46,實際 audit 後 47 個簽章。

## Caller 統計

| 用量 | 方法 |
|------|------|
| 重度(13+)| `CStr`(35)、`IsSerializeEmpty`(26)、`SetSerializeState`(25)、`CBool`(14)、`CInt`(13)|
| 中度(3-12)| `EnsureNotNullOrWhiteSpace`(9)、`IsEmpty`(7)、`IsNullOrDBNull`(5)、`CreateInstance`(3)、`UnwrapException`(3)|
| 輕度(1-2)| `IsNumeric`(2)、`CDateTime`(2)、`NewGuid`(1)、`GetAttribute`(1)、`GetPropertyValue`(1)|
| **0 caller** | **18 個方法**(`IsDBNull`、`IsNullOrEmpty`、`GetEnumName`、`CEnum` x2、`ConvertToNumber`、`CDouble`、`CDecimal`、`CDate`、`CGuid` x2、`CFieldValue`、`CDbFieldValue`、`NewGuidString`、`RndInt`、`GetPropertyAttribute`、`SetPropertyValue`、`IsGenericType`、`CheckTypes`、`GetCommandLineArgs`)|

## 設計策略

### 三大原則

1. **保留型別轉換完整 API**:`Cxxx` 家族(`CStr`/`CInt`/`CDouble`/`CDecimal`/`CDateTime`/`CDate`/`CBool`/`CGuid`/`CEnum`/`CFieldValue`/`CDbFieldValue`/`ConvertToNumber`/`IsNumeric`)即使部分 0 prod caller,**作為框架公開 API 給 ERP 業務碼使用**。同 `StrFunc.IsEmpty` 等不會因為「目前無 caller」就刪 —— 這是給 framework 消費者用的。

2. **真正死碼刪除**:純 BCL wrapper 且 0 caller(如 `NewGuidString`、`RndInt`、`IsDBNull`)+ 完全沒有框架價值的(`IsGenericType`、`CheckTypes`、`GetPropertyAttribute`、`SetPropertyValue`、`GetCommandLineArgs`)直接刪除。

3. **框架 default 封裝**:`Cxxx` 內部已用 `InvariantCulture`、`NumberStyles.Any` 等預設,**呼叫端不需傳這些參數**(沿襲 `StringUtilities` 同原則)。

### 拆分結果

| 路徑 | 方法群 | 動作 |
|------|--------|------|
| **C(改名為 `ValueUtilities`)**| 22 個方法 | `BaseFunc` → `ValueUtilities`(值檢查 + 型別轉換)|
| **D(移到 Serialization namespace)**| `IsSerializeEmpty` | 搬到 `Bee.Base.Serialization.SerializationUtilities` |
| **A inline 至 callers**| `SetSerializeState`(25)、`EnsureNotNullOrWhiteSpace`(9)、`CreateInstance`(3)、`NewGuid`(1)、`GetAttribute`(1)、`GetPropertyValue`(1) | 各自 inline |
| **B 擴充方法**| `UnwrapException`(3) | `ExceptionExtensions.Unwrap(this Exception)` |
| **A 刪除**| 10 個 0-caller 方法 | 純 BCL wrapper / 完全無框架價值 |

## Method Audit 表

### Group A1:Path A 刪除(10 個,0 caller + 純 BCL 或無框架價值)

| 方法 | 原因 |
|------|------|
| `IsDBNull(object)` | 0 caller,純 BCL `Convert.IsDBNull` 包裝 |
| `IsNullOrEmpty(byte[])` | 0 caller,`bytes is null or { Length: 0 }` 模式 |
| `GetEnumName(Enum)` | 0 caller,`Enum.GetName(...)` 包裝 |
| `NewGuidString()` | 0 caller,`Guid.NewGuid().ToString()` |
| `RndInt(int, int)` | 0 caller,`RandomNumberGenerator.GetInt32` 包裝 |
| `IsGenericType(object, Type)` | 0 caller,泛型型別檢查無人用 |
| `CheckTypes(object, params Type[])` | 0 caller,`types.Any(t => t.IsInstanceOfType(value))` |
| `GetPropertyAttribute(object, string, Type)` | 0 caller |
| `SetPropertyValue(object, string, object?)` | 0 caller |
| `GetCommandLineArgs()` + `ParseCommandLineArgs(string[])` | 0 caller(雙刪)|

### Group A2:Path A inline 至 callers(6 個方法,~18 callers)

| 方法 | Caller | BCL 替換 |
|------|--------|---------|
| `SetSerializeState(IObjectSerialize, SerializeState)` | 25 | `os?.SetSerializeState(state)` 直接呼叫 |
| `EnsureNotNullOrWhiteSpace(params (object,string)[])` | 9 | `ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(value))`(.NET 8+)|
| `CreateInstance` x2 | 3 | `AssemblyLoader.CreateInstance(...)` |
| `NewGuid()` | 1 | `Guid.NewGuid()` |
| `GetAttribute(object, Type)` | 1 | `TypeDescriptor.GetAttributes(c)[type]` |
| `GetPropertyValue(object, string)` | 1 | `TypeDescriptor.GetProperties(c)[name]?.GetValue(c)` |

### Group B:Path B 擴充方法(1 個)

| 方法 | 原 caller | 新形式 |
|------|---------|--------|
| `UnwrapException(Exception)` | 3 | `ExceptionExtensions.Unwrap(this Exception)`,呼叫端 `ex.Unwrap()` |

無 BCL 衝突(BCL 沒有 `Exception.Unwrap`),適合擴充方法。

### Group C:Path C 保留 + 改名 `BaseFunc → ValueUtilities`(22 個方法)

整體方向:**框架值處理 API,封裝預設值**,呼叫端不需傳 `CultureInfo` / `NumberStyles` / `StringComparison`。

| 方法 | Caller | 框架封裝 |
|------|--------|---------|
| `IsNullOrDBNull(object?)` | 5 | null + DBNull check |
| `IsEmpty(object)` | (含於 7)| switch on type |
| `IsEmpty(string)` | (含)| 同 `StringUtilities.IsEmpty` |
| `IsEmpty(Guid)` | (含)| `Guid.Empty` 比對 |
| `IsEmpty(DateTime)` | (含)| < SQL Server 1753-01-01 |
| `IsEmpty(IList)` | (含)| count == 0 |
| `IsEmpty(IEnumerable)` | (含)| 空 enumerator |
| `IsEmpty(byte[])` | (含)| null + length |
| `CStr(object, string)` | 35 | null/DBNull → defaultValue;Enum 取 name |
| `CStr(object)` | (含於 35)| overload |
| `CBool(string, bool)` | 14 | "1"/"T"/"TRUE"/"Y"/"YES"/"是"/"真" 視為 true |
| `CBool(object, bool)` | (含)| object → string → bool |
| `CEnum(string, Type)` | 0 | `Enum.Parse(type, value, true)` |
| `CEnum<T>(string)` | 0 | generic overload |
| `CInt(object, int=0)` | 13 | InvariantCulture |
| `CDouble(object, double=0)` | 0 | InvariantCulture |
| `CDecimal(object, decimal=0)` | 0 | InvariantCulture |
| `CDateTime(object, DateTime=default)` | 2 | InvariantCulture + ROC date 解析 |
| `CDate(object, DateTime=default)` | 0 | `CDateTime(...).Date` |
| `CGuid(string)` / `CGuid(object)` | 0 + 0 | null/empty → Guid.Empty |
| `CFieldValue(FieldDbType, object)` | 0 | 依 FieldDbType 路由到對應 Cxxx |
| `CDbFieldValue(FieldDbType, object)` | 0 | DateTime.MinValue → DBNull |
| `IsNumeric(object)` | 2 | InvariantCulture |
| `IsNumeric(string, int)` | 0 | overload |
| `ConvertToNumber(object)` | 0 | InvariantCulture |

完整保留(0 caller 的也留),作為公開 ERP 框架 API。

### Group D:Path D 移到 Serialization namespace(1 個)

`IsSerializeEmpty(SerializeState, object)`(26 callers) → `Bee.Base.Serialization.SerializationUtilities.IsSerializeEmpty`

理由:純粹的 serialization context check,屬於 serialization domain 而非通用值處理。

## 為何選 `ValueUtilities` 作為 BaseFunc 新名

- **對齊 idiom**:`HttpUtilities` / `FileUtilities` / `StringUtilities` 已建立 `<Domain>Utilities` 命名慣例
- **語意明確**:剩餘方法 100% 是「值處理」(checks + conversions),`Value` 比 `Base` 精準
- **CA1724 安全**:`ValueUtilities` 不對應任何 BCL namespace
- **不選 `Convert`**:衝突 `System.Convert`
- **不選 `TypeConverter`**:衝突 `System.ComponentModel.TypeConverter`
- **不選 `BaseUtilities`**:`Base` 過於模糊,不像 domain

## 影響範圍

- **產品端**:147 處 prod caller
- **大部分機械式替換**:`BaseFunc.X` → `ValueUtilities.X`(22 個方法)
- **部分 inline**:6 個方法、~18 個 callers 改寫為 BCL 直呼
- **`SetSerializeState` 25 處**:批次 `BaseFunc.SetSerializeState(os, state)` → `os?.SetSerializeState(state)` mechanical
- **`IsSerializeEmpty` 26 處**:批次改 namespace 引用

## 執行步驟

### 1. 新建 `src/Bee.Base/ValueUtilities.cs`

22 個保留方法。

### 2. 新建 `src/Bee.Base/ExceptionExtensions.cs`

```csharp
public static class ExceptionExtensions
{
    public static Exception Unwrap(this Exception ex) { /* ... */ }
}
```

### 3. 在 `Bee.Base.Serialization` 加 `SerializationUtilities.cs`

```csharp
public static class SerializationUtilities
{
    public static bool IsSerializeEmpty(SerializeState state, object value) { /* ... */ }
}
```

### 4. 機械式 rename(perl)

```bash
perl -i -pe '
# Group C rename
s/BaseFunc\.IsNullOrDBNull\(/ValueUtilities.IsNullOrDBNull(/g;
s/BaseFunc\.IsEmpty\(/ValueUtilities.IsEmpty(/g;
s/BaseFunc\.CStr\(/ValueUtilities.CStr(/g;
s/BaseFunc\.CBool\(/ValueUtilities.CBool(/g;
s/BaseFunc\.CInt\(/ValueUtilities.CInt(/g;
s/BaseFunc\.CDouble\(/ValueUtilities.CDouble(/g;
s/BaseFunc\.CDecimal\(/ValueUtilities.CDecimal(/g;
s/BaseFunc\.CDateTime\(/ValueUtilities.CDateTime(/g;
s/BaseFunc\.CDate\(/ValueUtilities.CDate(/g;
s/BaseFunc\.CGuid\(/ValueUtilities.CGuid(/g;
s/BaseFunc\.CEnum/ValueUtilities.CEnum/g;
s/BaseFunc\.CFieldValue\(/ValueUtilities.CFieldValue(/g;
s/BaseFunc\.CDbFieldValue\(/ValueUtilities.CDbFieldValue(/g;
s/BaseFunc\.IsNumeric\(/ValueUtilities.IsNumeric(/g;
s/BaseFunc\.ConvertToNumber\(/ValueUtilities.ConvertToNumber(/g;
# Group D move
s/BaseFunc\.IsSerializeEmpty\(/SerializationUtilities.IsSerializeEmpty(/g;
# Group B extension
s/BaseFunc\.UnwrapException\(([^()]+)\)/$1.Unwrap()/g;
# Group A2 inline (some via perl, some manual)
s/BaseFunc\.NewGuid\(\)/Guid.NewGuid()/g;
'
```

### 5. 手動 inline(部分需要更精細替換)

- `BaseFunc.SetSerializeState(os, state)` → `os?.SetSerializeState(state)`(perl 替換,但需注意命名空間引入)
- `BaseFunc.EnsureNotNullOrWhiteSpace(...)` → `ArgumentException.ThrowIfNullOrWhiteSpace(...)`(每個 caller 拆)
- `BaseFunc.CreateInstance(...)` → `AssemblyLoader.CreateInstance(...)`
- `BaseFunc.GetAttribute(...)` → BCL inline
- `BaseFunc.GetPropertyValue(...)` → BCL inline

### 6. 刪除 `BaseFunc.cs`

```bash
git rm src/Bee.Base/BaseFunc.cs
```

### 7. 拆解測試

`BaseFuncTests.cs` + `BaseFuncExtraTests.cs` → 拆/改名為:
- `ValueUtilitiesTests.cs`(主)
- 部分測試移到 `ExceptionExtensionsTests.cs`、`SerializationUtilitiesTests.cs`
- 刪除對應 Group A1/A2 deleted 方法的測試

### 8. 更新主計畫

進度表第 12 列 ✅,完成日填入,方法數 `47`(原 46 略多算),處理路徑 `A+B+C+D` 全用上。

## 驗證

```bash
grep -rn "BaseFunc" /Users/jeff/Desktop/repos/bee-library --include="*.cs" --exclude-dir=bin --exclude-dir=obj
# 應只剩 docs/plans/ 內歷史紀錄
dotnet build  # 全 src project 0 警告 0 錯誤
./test.sh tests/Bee.Base.UnitTests/Bee.Base.UnitTests.csproj
# 其他受影響 test projects(基本上全部)
```

## Commit 訊息草稿

```
refactor(base): split BaseFunc into ValueUtilities + extension/move/delete

BaseFunc had 47 methods covering value checks, type conversion, serialization,
reflection, command-line parsing, validation, and exception handling. The
refactor cleans this up into focused homes:

- 22 methods stay as ValueUtilities (path C rename) for value checks (IsEmpty
  overloads, IsNullOrDBNull) and type conversion (CStr, CBool, CInt, CDouble,
  CDecimal, CDateTime, CDate, CGuid, CEnum, CFieldValue, CDbFieldValue,
  ConvertToNumber, IsNumeric). The Cxxx family is preserved complete even
  where prod has zero callers, since these are the framework public API for
  ERP business code; the framework encapsulates InvariantCulture and
  IgnoreCase defaults so call sites do not pass them.

- IsSerializeEmpty (26 callers) moves to SerializationUtilities under
  Bee.Base.Serialization (path D, belongs with serialization domain).

- UnwrapException (3 callers) becomes ExceptionExtensions.Unwrap as a this
  Exception extension method (path B, no BCL conflict).

- 6 methods inline at callers (path A): SetSerializeState 25 callers
  inline as os safe-call, EnsureNotNullOrWhiteSpace 9 callers map to
  ArgumentException.ThrowIfNullOrWhiteSpace per parameter, CreateInstance x2
  3 callers route to AssemblyLoader.CreateInstance directly, NewGuid /
  GetAttribute / GetPropertyValue 1 caller each.

- 10 methods deleted (path A): IsDBNull, IsNullOrEmpty(byte[]), GetEnumName,
  NewGuidString, RndInt, IsGenericType, CheckTypes, GetPropertyAttribute,
  SetPropertyValue, GetCommandLineArgs (with internal ParseCommandLineArgs).
  All zero callers and either pure BCL wrappers or framework-irrelevant.

BaseFunc.cs is removed entirely. Tests reorganized into
ValueUtilitiesTests / SerializationUtilitiesTests / ExceptionExtensionsTests
with deleted-method tests dropped.

Twelfth and final class executed under the *Func to .NET idiomatic refactor
(see docs/plans/plan-funcs-to-net-idiomatic.md). All 12 *Func classes are
now eliminated.
```

## 跨類別決策落地

無新原則,沿用既有:
- **框架封裝預設值**(由 `StrFunc` 定案):`Cxxx` 全家族不要求 caller 傳 `CultureInfo` / `NumberStyles`
- **不擴充 `object`**(由 `DateTimeFunc` 定案):`IsEmpty(object)` / `IsNullOrDBNull(object?)` 不轉擴充方法
- **0-caller 框架公開 API 保留**:`Cxxx` 完整保留,即使部分無 prod caller(消費者用 API)

## 風險與回滾

- 變動範圍極大(147 處 prod caller),但大部分是機械式替換
- 測試大幅重組
- Public API breaking:`BaseFunc` 整個移除 + 多個方法刪除/搬移
- 無外部 NuGet 消費者,可接受
- 失敗單一 `git revert` 即可回滾
