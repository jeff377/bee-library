# 計畫：重構 `SerializeFunc` 為 .NET idiomatic

**狀態：✅ 已完成（2026-05-01）**

> 主計畫:[plan-funcs-to-net-idiomatic.md](plan-funcs-to-net-idiomatic.md)

## 目前內容

`src/Bee.Base/Serialization/SerializeFunc.cs`(245 行,**9 public + 3 private = 12 個方法**)

```csharp
namespace Bee.Base.Serialization;

public static class SerializeFunc
{
    private static void DoBeforeSerialize(SerializeFormat, object?);     // lifecycle hook
    private static void DoAfterSerialize(SerializeFormat, object?);
    private static void DoAfterDeserialize(SerializeFormat, object?);
    private static JsonSerializerOptions GetJsonSerializerOptions(bool, bool);

    public static string ObjectToXml(object);
    public static T? XmlToObject<T>(string);
    public static object? XmlToObject(string, Type);
    public static void ObjectToXmlFile(object, string);
    public static T? XmlFileToObject<T>(string);
    public static string ObjectToJson(object, bool, bool, bool);
    public static T? JsonToObject<T>(string, bool);
    public static void ObjectToJsonFile(object, string);
    public static T? JsonFileToObject<T>(string);
}
```

> 主計畫進度表寫 10,實際 9 個 public(主計畫應是把 1 個 private 也算進去)。

## 設計決策

### Path C(分拆 + 改名)

整體採 **path C**,但**分成兩個格式專屬類別**:

- `Bee.Base.Serialization.XmlCodec`(5 個 XML 相關方法)
- `Bee.Base.Serialization.JsonCodec`(4 個 JSON 相關方法)
- `Bee.Base.Serialization.SerializationLifecycle`(`internal static`,共用 lifecycle hooks)

**為何分拆而非單一 `SerializerCodec`**:
- 對齊既有 `MessagePackCodec`(Bee.Api.Core)idiom —— **一個 format 一個 codec**
- 未來新增 format(如 protobuf)直接加新 codec,不需動既有
- 每個檔案 ~80 行,聚合度高

**為何不選 path B(`object`/`string`/`Type` 擴充)**:
- 9 個方法首參數都是 `object`/`string`/`Type` —— 全部都是過度通用型別,擴充會嚴重污染 IntelliSense
- 同 `byte[]` 不擴充 `Gzip`、`object` 不擴充 `IsDate` 一致

**CA1724 檢查**:`XmlCodec` / `JsonCodec` / `SerializationLifecycle` 都不對應任何 BCL namespace 末段,安全。

### 方法名同時改為 BCL idiomatic

類名已含 `Xml` / `Json`,方法名不再重複(同 `NumberFormatPresets.ToFormatString` idiom):

| 原方法 | 新方法 | BCL 對齊 |
|--------|--------|---------|
| `ObjectToXml(value)` | `XmlCodec.Serialize(value)` | 對齊 `JsonSerializer.Serialize` |
| `XmlToObject<T>(xml)` | `XmlCodec.Deserialize<T>(xml)` | 對齊 `JsonSerializer.Deserialize<T>` |
| `XmlToObject(xml, type)` | `XmlCodec.Deserialize(xml, type)` | 同上 |
| `ObjectToXmlFile(value, path)` | `XmlCodec.SerializeToFile(value, path)` | 框架自有(BCL 無檔案直連 helper)|
| `XmlFileToObject<T>(path)` | `XmlCodec.DeserializeFromFile<T>(path)` | 同上 |
| `ObjectToJson(value, ...)` | `JsonCodec.Serialize(value, ...)` | 對齊 BCL |
| `JsonToObject<T>(json, ...)` | `JsonCodec.Deserialize<T>(json, ...)` | 對齊 BCL |
| `ObjectToJsonFile(value, path)` | `JsonCodec.SerializeToFile(value, path)` | 同上 |
| `JsonFileToObject<T>(path)` | `JsonCodec.DeserializeFromFile<T>(path)` | 同上 |

呼叫端對比:
```csharp
// 改前
SerializeFunc.ObjectToXml(value)
SerializeFunc.XmlToObject<T>(xml)
SerializeFunc.JsonToObject<JsonRpcRequest>(json)

// 改後
XmlCodec.Serialize(value)
XmlCodec.Deserialize<T>(xml)
JsonCodec.Deserialize<JsonRpcRequest>(json)
```

更短、更接近 .NET idiom。

## Method Audit 表

| # | 原方法 | Prod | 處理路徑 | 新位置/名稱 |
|---|--------|------|--------|------------|
| 1 | `ObjectToXml(object)` | 5+ | C | `XmlCodec.Serialize(object)` |
| 2 | `XmlToObject<T>(string)` | 5+ | C | `XmlCodec.Deserialize<T>(string)` |
| 3 | `XmlToObject(string, Type)` | 1 | C | `XmlCodec.Deserialize(string, Type)` |
| 4 | `ObjectToXmlFile(object, string)` | 7+ | C | `XmlCodec.SerializeToFile(object, string)` |
| 5 | `XmlFileToObject<T>(string)` | 8+ | C | `XmlCodec.DeserializeFromFile<T>(string)` |
| 6 | `ObjectToJson(object, ...)` | 1 | C | `JsonCodec.Serialize(object, ...)` |
| 7 | `JsonToObject<T>(string, ...)` | 2+ | C | `JsonCodec.Deserialize<T>(string, ...)` |
| 8 | `ObjectToJsonFile(object, string)` | 2 | C | `JsonCodec.SerializeToFile(object, string)` |
| 9 | `JsonFileToObject<T>(string)` | 0 | C | `JsonCodec.DeserializeFromFile<T>(string)` |
| 私 | `Do*Serialize` x 3 | — | 內部抽取 | `internal static SerializationLifecycle.NotifyBefore/After/AfterDeserialize` |
| 私 | `GetJsonSerializerOptions` | — | 留 `JsonCodec` 內 private | — |

`SerializeFunc.cs` 整個刪除。

## 影響範圍

**全 repo grep `SerializeFunc.` 結果(扣除 `bin/obj`)**:30 處 prod caller,跨 11 個檔案。

主要分布:
- `src/Bee.Definition/Storage/FileDefineStorage.cs` — 8(全 XML)
- `src/Bee.ObjectCaching/*` — 6(XML 居多)
- `src/Bee.Api.Client/Connectors/SystemApiConnector.cs` — 3(XML)
- `src/Bee.Base/Serialization/SerializationExtensions.cs` — 6(XML 4 + JSON 2)
- `src/Bee.Business/System/SystemBusinessObject.cs` — 2(XML)
- `src/Bee.Repository/System/SessionRepository.cs` — 2(XML)
- `src/Bee.Api.AspNetCore/Controllers/ApiServiceController.cs` — 1(JSON)
- `src/Bee.Api.Client/Providers/RemoteApiProvider.cs` — 1(JSON)
- `src/Bee.Api.Core/Conversion/ApiInputConverter.cs` — 1(註解 cref)

測試端:`tests/Bee.Base.UnitTests/SerializeFuncTests.cs`(271 行,12 個 test method)。

## 執行步驟

### 1. 新增 3 個檔案

#### 1a. `src/Bee.Base/Serialization/SerializationLifecycle.cs`(internal helper)

```csharp
namespace Bee.Base.Serialization;

internal static class SerializationLifecycle
{
    public static void NotifyBefore(SerializeFormat format, object? value)
    {
        if (value is IObjectSerializeProcess sp) sp.BeforeSerialize(format);
        if (value is IObjectSerialize os) os.SetSerializeState(SerializeState.Serialize);
    }

    public static void NotifyAfter(SerializeFormat format, object? value)
    {
        if (value is IObjectSerialize os) os.SetSerializeState(SerializeState.None);
        if (value is IObjectSerializeProcess sp) sp.AfterSerialize(format);
    }

    public static void NotifyAfterDeserialize(SerializeFormat format, object? value)
    {
        if (value is IObjectSerializeProcess sp) sp.AfterDeserialize(format);
    }
}
```

#### 1b. `src/Bee.Base/Serialization/XmlCodec.cs`

5 個 public 方法,內部用 `SerializationLifecycle.X` + `XmlSerializerCache` + `Utf8StringWriter` + `FileFunc`(後續 `FileFunc` 重構時會再調整)。

#### 1c. `src/Bee.Base/Serialization/JsonCodec.cs`

4 個 public 方法 + private `GetJsonSerializerOptions`,內部用 `SerializationLifecycle.X`。

### 2. 更新所有產品端 caller(30 處)

機械式 sed 替換,對應表:

| 原 | 新 |
|------|------|
| `SerializeFunc.ObjectToXml(` | `XmlCodec.Serialize(` |
| `SerializeFunc.XmlToObject<` | `XmlCodec.Deserialize<` |
| `SerializeFunc.XmlToObject(` | `XmlCodec.Deserialize(` |
| `SerializeFunc.ObjectToXmlFile(` | `XmlCodec.SerializeToFile(` |
| `SerializeFunc.XmlFileToObject<` | `XmlCodec.DeserializeFromFile<` |
| `SerializeFunc.ObjectToJson(` | `JsonCodec.Serialize(` |
| `SerializeFunc.JsonToObject<` | `JsonCodec.Deserialize<` |
| `SerializeFunc.JsonToObject(` | `JsonCodec.Deserialize(` |
| `SerializeFunc.ObjectToJsonFile(` | `JsonCodec.SerializeToFile(` |
| `SerializeFunc.JsonFileToObject<` | `JsonCodec.DeserializeFromFile<` |

11 個檔案內 perl 批次處理。`ApiInputConverter.cs` 內的註解 cref 順手更新為新名稱。

### 3. 刪除 `SerializeFunc.cs`

```bash
git rm src/Bee.Base/Serialization/SerializeFunc.cs
```

### 4. 拆解測試

`SerializeFuncTests.cs` 拆成兩處:
- 9 個 XML 相關 test → 新檔 `XmlCodecTests.cs`(含 `XmlSerializerCache_Get` 與 `Utf8StringWriter_Encoding` 兩個基礎設施 test)
- 3 個 JSON 相關 test → 新檔 `JsonCodecTests.cs`
- 共用的 `IObjectSerialize` test fake / `Dispose` 邏輯,複製到兩檔(輕量,簡潔勝過共用)
- 刪除原 `SerializeFuncTests.cs`

### 5. 更新主計畫

進度表第 8 列:`📝` → `✅`,完成日填入,方法數 `10` → `9`(public),處理路徑記為 `C`。

## 驗證

```bash
grep -rn "SerializeFunc" /Users/jeff/Desktop/repos/bee-library --include="*.cs" --exclude-dir=bin --exclude-dir=obj

# Build 受影響的多個專案
dotnet build src/Bee.Base/Bee.Base.csproj --configuration Release --no-restore
dotnet build src/Bee.Api.Client/Bee.Api.Client.csproj --configuration Release --no-restore
dotnet build src/Bee.Definition/Bee.Definition.csproj --configuration Release --no-restore
dotnet build src/Bee.Business/Bee.Business.csproj --configuration Release --no-restore
dotnet build src/Bee.Repository/Bee.Repository.csproj --configuration Release --no-restore
dotnet build src/Bee.ObjectCaching/Bee.ObjectCaching.csproj --configuration Release --no-restore
dotnet build src/Bee.Api.AspNetCore/Bee.Api.AspNetCore.csproj --configuration Release --no-restore

# Test
./test.sh tests/Bee.Base.UnitTests/Bee.Base.UnitTests.csproj
```

預期結果:
- `grep` 應只剩 `docs/plans/` 內的歷史紀錄
- Build 全部 0 warning, 0 error
- 測試:Bee.Base.UnitTests 數量不變(12 個 test 拆分後總數一樣),全綠

## Commit 訊息草稿

```
refactor(base): split SerializeFunc into XmlCodec and JsonCodec

The 9 public methods split cleanly by format. Each codec mirrors the
existing MessagePackCodec idiom (one format per codec, with codec as
the noun-form static utility name). Method names align with the BCL
JsonSerializer.Serialize / Deserialize convention now that the class
name carries the format identifier.

Mappings:
- ObjectToXml / ObjectToJson → Serialize
- XmlToObject / JsonToObject → Deserialize
- ObjectToXmlFile / ObjectToJsonFile → SerializeToFile
- XmlFileToObject / JsonFileToObject → DeserializeFromFile

Lifecycle hooks (BeforeSerialize / AfterSerialize / AfterDeserialize
on IObjectSerialize / IObjectSerializeProcess) move to a shared
internal static SerializationLifecycle to avoid duplicating across
the two codecs.

Path B is ruled out: every method takes object / string / Type as
the first parameter — extending those would pollute IntelliSense
for all values, all strings, and all reflected types.

SerializeFunc.cs is removed entirely. SerializationExtensions
delegates updated to call XmlCodec / JsonCodec. Tests split into
XmlCodecTests and JsonCodecTests.

Eighth class executed under the *Func to .NET idiomatic refactor
(see docs/plans/plan-funcs-to-net-idiomatic.md).
```

## 跨類別決策落地

新增一條 idiom(由本次定案):

- **多 format 工具類分拆**:當 `*Func` 內含多個 format-specific 方法群(本例 XML 與 JSON),分成各 format 的 `<Format>Codec` 獨立類,對齊既有 `MessagePackCodec`。共用的 lifecycle 邏輯抽 internal helper class 避免重複,不放 base class(C# 靜態類無法繼承)。

沿用既有 idiom:
- 同 `NumberFormatPresets.ToFormatString` 的「類名已含領域字眼,方法名不重複」(`XmlCodec.Serialize`,而非 `XmlCodec.SerializeXml`)
- 不擴充 `object`/`string`/`Type` 等過度通用型別

## 風險與回滾

- 變動範圍:30 處生產端 caller(機械替換)+ 11 個檔案 + 測試重組
- Public API breaking:類別名 + 9 個方法名,但無外部 NuGet 消費者
- 大範圍但每個替換確定性高,失敗單一 `git revert` 即可回滾
