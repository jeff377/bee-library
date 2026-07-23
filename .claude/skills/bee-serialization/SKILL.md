---
name: bee-serialization
description: bee-library 物件「三棲序列化」(XML / JSON / MessagePack)的設計指引。核心兩軸用途——XML 用於持久化(存檔 / 定義檔 / 快照 / 落 DB)、JSON + MessagePack 用於 API 傳遞資料(wire payload，由 PayloadFormat 決定上線哪個，JSON 給 JS 友善、MessagePack 高效)。涵蓋物件三棲 recipe(無參數 ctor + 三標籤共存 + 衍生欄位 ignore)、集合三棲(MessagePackCollectionBase + 必顯式註冊 CollectionBaseFormatter，否則沉默出空集合)、wire 傳遞模式(物件本身 vs XML string)、踩雷與三棲 round-trip 測試樣板。當使用者要「物件要支援 XML/JSON/MessagePack」、「序列化」、「傳前端又要存檔」、「跨 wire 傳物件」、「KeyCollectionBase / MessagePack 集合序列化」、「可序列化物件設計」之類需求時使用。
---

# bee-library 三棲序列化（XML / JSON / MessagePack）

## 核心：兩軸用途（先記這個，其餘都從這推導）

| 序列化 | 用途軸 | 載體 | 何時用 |
|--------|--------|------|--------|
| **XML** | **持久化** | `XmlCodec` | 存檔、定義檔（FormSchema / TableSchema…）、**組織異動快照**、任何落磁碟 / DB 的物件 |
| **JSON** | **API 傳遞** | `System.Text.Json` | wire payload；JS 前端友善（`JSON.parse`） |
| **MessagePack** | **API 傳遞** | `MessagePackCodec` | wire payload；高效二進位 |

- **XML = 持久化軸**；**JSON + MessagePack = 傳輸軸**（同一物件的兩種 wire 表示，由 `PayloadFormat` 決定上線哪個，前端各取所需）。
- 一個「既要傳前端、又要存快照」的物件 → **三棲都要**（如 `DepartmentTree`）。
- 一個只跨 wire 的 API DTO → 通常只需 MessagePack（+ JSON），不需 XML。
- 一個只存檔的定義物件 → 以 XML 為主（JSON 多半附帶可用）。

> **不要把持久化格式當 wire**：`GetDefine` 回 `XmlCodec.Serialize` 的 XML string 是 define 物件的**歷史用法**（持久化格式順手借作 wire）。新物件的 API 傳遞**走 JSON/MessagePack 物件本身**（見下「wire 傳遞模式」），因為 JS 處理 XML 麻煩、且 XML 不是傳輸軸。

## 物件三棲 recipe（純物件，無集合）

```csharp
[MessagePackObject]
public class Foo : IKeyObject          // IKeyObject 僅在要進 KeyObjectCache 時
{
    public Foo() { }                    // 無參數 ctor — 三種序列化器都要求

    // 三標籤共存：XML attribute + MessagePack Key + JSON 自動（不標即包含）
    [Key(100)] [XmlAttribute] public Guid RowId { get; set; }
    [Key(101)] [XmlAttribute] public string Name { get; set; } = string.Empty;

    public string GetKey() => RowId.ToString();

    // 衍生 / 索引 / owner：三種都要跳過
    [IgnoreMember, XmlIgnore, JsonIgnore] private Dictionary<...>? _index;
    [IgnoreMember, XmlIgnore, JsonIgnore] public SomethingDerived Derived => ...;
}
```

- **`[Key(n)]` 從 100 起**：0–99 留給 base（`ApiRequest`/`MessagePackCollectionItem` 等）。
- **三標籤要同時到位**：`[Key]`(MessagePack) ＋ `[XmlAttribute]`/`[XmlElement]`(XML) ＋ JSON 自動；要排除的成員必須 `[IgnoreMember, XmlIgnore, JsonIgnore]` **三個都標**（少一個 → 該軸外洩或循環）。
- **無參數 ctor 必備**：`XmlSerializer` / `System.Text.Json` 反序列化要它。

## 集合三棲（最容易錯——沉默失敗）

集合元素與集合各自有 base，**且 MessagePack 必須在 `MessagePackCodec` 顯式註冊 formatter**：

```csharp
// 元素 — 繼承 MessagePackCollectionItem（Owner/SerializeState/Tag 已三標籤跳過）
[MessagePackObject]
public sealed class FooNode : MessagePackCollectionItem
{
    [Key(100)] [XmlAttribute] public Guid RowId { get; set; }
    // ...
}

// 集合 — 繼承 MessagePackCollectionBase<T>（無 ItemsForSerialization 代理，最乾淨）
[MessagePackObject]
public class FooNodeCollection : MessagePackCollectionBase<FooNode> { }
```

```csharp
// ⚠️ 關鍵：MessagePackCodec.cs 的 formatter 陣列「必須」顯式註冊，否則跨 MessagePack 出空集合
new CollectionBaseFormatter<FooNodeCollection, FooNode>(),
```

**為什麼一定要顯式註冊**：`MessagePackCodec` 的 resolver 鏈中 `ContractlessStandardResolver` 排在 `FormatterResolver`(動態偵測 `MessagePackCollectionBase<>`)**之前**，會搶先用錯誤方式序列化集合 → **反序列化得到空 items、不拋任何錯**（沉默失敗，最難察覺）。`FilterNodeCollection` / `SortFieldCollection` 都靠這行顯式註冊才正確。

### 集合 base 選型

| Base | XML/JSON | MessagePack | 用途 |
|------|----------|-------------|------|
| `KeyCollectionBase<T>` | ✅ | ❌(`Owner` 循環) | 只持久化 / 不跨 MessagePack 的定義集合 |
| `MessagePackCollectionBase<T>` | ✅ | ✅(顯式註冊 formatter) | **三棲首選**(無代理屬性、最乾淨) |
| `MessagePackKeyCollectionBase<T>` | ✅ | ✅(`ItemsForSerialization` 代理) | 需 keyed 索引的集合(如 `ParameterCollection`) |

> `MessagePackKeyCollectionBase` 的 `ItemsForSerialization` 只標 `[Key(0)]`、未標 XmlIgnore/JsonIgnore；它能三棲是因為 `KeyedCollection<,>` 本身被 XML/JSON 當 `IEnumerable` 列舉、不掃該代理屬性。能用 `MessagePackCollectionBase` 就用它(更乾淨)。

## wire 傳遞模式（API 端）

| 模式 | 作法 | 何時 |
|------|------|------|
| **物件本身**(建議) | `Response.Tree = DepartmentTree`(`[Key(100)]`)；wire 由 PayloadFormat 決定 JSON 或 MessagePack | **新的 API 傳遞**——前端 JS 拿 JSON、效能要 MessagePack；樣板 `GetFormSchema.Schema` |
| **XML string**(歷史) | `Response.Xml = XmlCodec.Serialize(obj)`；前端 `XmlCodec.Deserialize<T>` | **僅 define 物件**(`GetDefine`)——持久化格式借作 wire；新物件不走，JS 解 XML 麻煩 |

新物件要傳前端 → 走「物件本身」+ 三棲(JSON/MessagePack 都備好)，不要再走 XML string。

## 踩雷清單

1. **MessagePackCollectionBase 沒顯式註冊 formatter → 沉默出空集合**：最隱蔽，跨 MessagePack 後 `Count == 0`、不拋錯。一律在 `MessagePackCodec` 加 `CollectionBaseFormatter<TColl, TElem>`，並補 MessagePack round-trip 測試把關。
2. **衍生/index/owner 欄位少標一個軸 → 外洩或循環**：`[IgnoreMember, XmlIgnore, JsonIgnore]` 三個一組。
3. **`[Key]` 與 base 衝突**：從 100 起算（0–99 base 保留）。
4. **`SafeTypelessFormatter` 白名單**：跨 wire 的型別 namespace 必須在 `SysInfo.AllowedTypeNamespaces`（`Bee.Base` / `Bee.Definition` / `Bee.Api.Core` / `Bee.Business` 等）內，否則反序列化被擋。
5. **`ISerializableClone`**：若序列化管線會**就地改值**（如加密 password 欄位），快取物件必須先 `CreateSerializableCopy()` 深複製再序列化，避免污染快取副本（`GetDefine` 對 define 物件即如此）。無敏感欄位的純資料物件不需要。
6. **lazy index 反序列化後要能重建**：序列化只帶扁平狀態，查詢 index 在還原後第一次查詢時 lazy 建（thread-safe）；index 本身不序列化。
7. **Oracle Guid 讀回是 `byte[]`(RAW 16)**：持久化讀回端，`ValueUtilities.CGuid` 已支援 `byte[]` coerce；自寫 raw DataTable 讀 Guid 欄時別用會落空的轉換。
8. **JSON 自訂 converter 僅多型才需**：單一型別集合（如 `DepartmentNodeCollection`）`System.Text.Json` 直接列舉即可；多型 union（如 `FilterNode`）才需 `JsonConverter`（見 `FilterNodeCollectionJsonConverter`）。

## 三棲 round-trip 測試樣板

每個三棲物件都應有三種各一的 round-trip（還原後**值完整 + 衍生 index 正確重建**）：

```csharp
// XML（持久化軸）— 放 Bee.Definition.UnitTests
var xml = XmlCodec.Serialize(obj);
var fromXml = XmlCodec.Deserialize<Foo>(xml)!;

// JSON（傳輸軸）— 放 Bee.Definition.UnitTests
var json = JsonSerializer.Serialize(obj);
var fromJson = JsonSerializer.Deserialize<Foo>(json)!;

// MessagePack（傳輸軸）— 放 Bee.Api.Core.UnitTests（codec 在那）
var bytes = MessagePackCodec.Serialize(obj);
var fromMp = MessagePackCodec.Deserialize<Foo>(bytes)!;

// 三者都驗證集合 Count、查詢結果、index 重建一致；含空集合邊界
```

- **MessagePack 測試放 `Bee.Api.Core.UnitTests`**（`MessagePackCodec` 在該 assembly）；XML/JSON 放定義物件所在的 `*.UnitTests`。
- 空集合 / 單節點邊界各測一次（防 NRE、防空集合沉默）。

## 完整 checklist

- [ ] 定位用途：要持久化(XML)？要 API 傳遞(JSON/MessagePack)？還是三棲？
- [ ] 物件：無參數 ctor + 屬性三標籤(`[Key(100+)]`/`[XmlAttribute]`/JSON) + 衍生欄位 `[IgnoreMember, XmlIgnore, JsonIgnore]`
- [ ] 集合：元素 `: MessagePackCollectionItem`、集合 `: MessagePackCollectionBase<T>`
- [ ] **`MessagePackCodec` 顯式註冊 `CollectionBaseFormatter<TColl, TElem>`**
- [ ] 型別 namespace 在 `SysInfo.AllowedTypeNamespaces` 白名單內
- [ ] 敏感欄位 → `ISerializableClone`；lazy index → 反序列化後可重建
- [ ] wire 傳遞走「物件本身」(新物件)，不走 XML string
- [ ] 三棲 round-trip 測試(XML+JSON 在定義層、MessagePack 在 Api.Core)，含空集合邊界
- [ ] `dotnet build Bee.Library.slnx -c Release` 0w/0e

## 參考檔案（讀程式碼對著看）

| 用途 | 檔案 |
|------|------|
| 三棲物件 + 集合樣板 | `src/Bee.Definition/Organization/DepartmentTree.cs` / `DepartmentNode.cs` / `DepartmentNodeCollection.cs` |
| 三棲集合既有樣板 | `src/Bee.Definition/Filters/FilterNodeCollection.cs` + `FilterGroup.cs`（含多型 JsonConverter） |
| 集合 base | `src/Bee.Definition/Collections/MessagePackCollectionBase.cs` / `MessagePackCollectionItem.cs` / `MessagePackKeyCollectionBase.cs` |
| MessagePack 設定 + formatter 註冊 | `src/Bee.Api.Core/MessagePack/MessagePackCodec.cs` / `CollectionBaseFormatter.cs` |
| XML 持久化 codec | `src/Bee.Base/Serialization/XmlCodec.cs` |
| 型別白名單 | `src/Bee.Base/SysInfo.cs`（`AllowedTypeNamespaces`） / `src/Bee.Definition/Serialization/SafeTypelessFormatter.cs` |
| 序列化保護 | `src/Bee.Base/Serialization/ISerializableClone.cs` |
| wire 物件本身樣板 | `src/Bee.Api.Core/Messages/System/GetFormSchemaResponse.cs`（物件） vs `GetDefineResponse.cs`（XML string，歷史） |
| 三棲 round-trip 測試樣板 | `tests/Bee.Definition.UnitTests/Organization/DepartmentTreeTests.cs`（XML/JSON） / `tests/Bee.Api.Core.UnitTests/Organization/DepartmentTreeMessagePackTests.cs`（MessagePack） |
