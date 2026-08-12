---
name: bee-serialization
description: bee-library 物件「三棲序列化」(XML / JSON / MessagePack)的設計指引。核心兩軸用途——XML 用於持久化(存檔 / 定義檔 / 快照 / 落 DB)、JSON + MessagePack 用於 API 傳遞(JSON-RPC 信封恆為 JSON；payload body 在 Encoded/Encrypted 下是 MessagePack)。涵蓋物件 recipe(無參數 ctor + XML/JSON 標籤，定義層不帶任何序列化套件標註)、集合(繼承 Bee.Base.Collections 基底 + 必到 WireContracts 顯式註冊 formatter)、wire 傳遞模式(物件本身 vs XML string)、行動端 AOT 型別形狀要件、踩雷與三棲 round-trip 測試樣板。當使用者要「物件要支援 XML/JSON/MessagePack」、「序列化」、「傳前端又要存檔」、「跨 wire 傳物件」、「KeyCollectionBase 集合序列化」、「新增 wire 型別」、「可序列化物件設計」之類需求時使用。
---

# bee-library 三棲序列化（XML / JSON / MessagePack）

> **2026-08-11 全面改寫。** 本檔先前教的是 `[MessagePackObject]` / `[Key(n)]` /
> `MessagePackCollectionBase<T>` 那一套，**那些型別與標註已於 ADR-036 全數移除**（`src/` 內
> 宣告數為 0），照舊版寫出來的程式碼會編譯不過、或在行動端擲例外。

## 核心：兩軸用途（先記這個，其餘都從這推導）

| 序列化 | 用途軸 | 載體 | 何時用 |
|--------|--------|------|--------|
| **XML** | **持久化** | `XmlCodec` | 存檔、定義檔（FormSchema / TableSchema…）、快照、任何落磁碟 / DB 的物件 |
| **JSON** | **API 傳遞** | `System.Text.Json` | JSON-RPC 信封恆為 JSON；`PayloadFormat.Plain` 時 payload 值也內嵌為 JSON |
| **MessagePack** | **API 傳遞** | `MessagePackCodec` | `PayloadFormat.Encoded` / `Encrypted` 時的 payload body（base64 的 MessagePack bytes） |

- **XML = 持久化軸**；**JSON + MessagePack = 傳輸軸**。
- **`PayloadFormat` 是「加密／壓縮」維度，不是「JSON vs MessagePack」的選擇器**——
  但它間接決定 body 格式：`Plain` 走 JSON 內嵌，`Encoded`/`Encrypted` 走 MessagePack。
  框架**沒有** JSON body serializer（`ApiPayloadOptionsFactory.CreateSerializer` 只有
  `messagepack` 一個 case）。
- 一個「既要傳前端、又要存快照」的物件 → **三棲都要**（如 `DepartmentTree`）。
- 一個只跨 wire 的 API DTO → 需 JSON + MessagePack，不需 XML。

> **不要把持久化格式當 wire**：`GetDefine` 回 `XmlCodec.Serialize` 的 XML string 是 define 物件的
> **歷史用法**。新物件的 API 傳遞走物件本身（見下「wire 傳遞模式」）。
> 附帶效果：FormSchema 以 XML 字串夾在 wire 上，所以**它不在 wire 型別閉包內**。

## 物件 recipe（純物件，無集合）

```csharp
public class Foo : IKeyObject          // IKeyObject 僅在要進 KeyObjectCache 時
{
    public Foo() { }                    // 無參數 ctor — 三種序列化器都要求

    // XML 要標；JSON 自動（不標即包含）；MessagePack 不在型別上標，見下
    [XmlAttribute] public Guid RowId { get; set; }
    [XmlAttribute] public string Name { get; set; } = string.Empty;

    public string GetKey() => RowId.ToString();

    // 衍生 / 索引 / owner：兩個軸都要跳過
    [XmlIgnore, JsonIgnore] private Dictionary<string, Foo>? _index;
    [XmlIgnore, JsonIgnore] public SomethingDerived Derived => ...;
}
```

- **定義層不帶任何序列化套件標註**（ADR-036）。`[XmlIgnore]` / `[JsonIgnore]` 是 BCL 詞彙、可用；
  MessagePack 的 `[MessagePackObject]` / `[Key]` / `[IgnoreMember]` **不可用**，`src/Bee.Definition`
  也不得有 `MessagePack` 的 `PackageReference`。
- **`[JsonIgnore]` 同時是 wire 的排除機制**：wire 成員的定義＝**public 可讀可寫、未標
  `[JsonIgnore]`** 的屬性。想讓某成員不上 MessagePack，標 `[JsonIgnore]`。
- **無參數 ctor 必備**：`XmlSerializer` / `System.Text.Json` 都要它；`BEE4006` 於建置期把關。

## wire 型別必須顯式註冊（ADR-037，最容易漏）

**新增 wire 型別**（`Bee.Api.Core.Messages.*`、其遞移可達的定義層型別、集合）
**一定要到 `src/Bee.Api.Core/MessagePack/WireContracts.*.cs` 註冊**，否則行動端擲
`FormatterNotRegisteredException`（不是變慢）。漏補會被 `WireContractDriftTests` 擋下。

> **完整註冊程序、三個容易誤判的點、`object` 封套機制 →
> [`src/Bee.Api.Core/CLAUDE.md`](../../../src/Bee.Api.Core/CLAUDE.md)**（觸及該專案時自動載入）。
> 那是唯一權威，本檔不複寫。

## 集合

元素與集合各自有基底，且 MessagePack **必須顯式註冊 formatter**：

```csharp
// 元素 — KeyCollectionItem（有 key）或 CollectionItem（無 key）
public sealed class FooNode : CollectionItem
{
    [XmlAttribute] public Guid RowId { get; set; }
}

// 集合 — Bee.Base.Collections 的基底
public class FooNodeCollection : CollectionBase<FooNode> { }
```

```csharp
// MessagePackCodec.BuildFormatters()：沒有這行，行動端讀不回來
new CollectionBaseFormatter<FooNodeCollection, FooNode>(),
```

### 集合基底選型

| Base | 用途 |
|------|------|
| `Bee.Base.Collections.CollectionBase<T>` | item 無 key 的集合 |
| `Bee.Base.Collections.KeyCollectionBase<T>` | item 有 key、需 keyed 索引（如 `ParameterCollection`） |

- **定義層的公開集合屬性禁用裸 `List<T>` / `Collection<T>` / `IList<T>`**（`rules/definition.md`）；
  `BEE3002` 於建置期把關。
- `KeyCollectionBase<T>` 以 `StringComparer.OrdinalIgnoreCase` 建構，是**真 O(1)**
  （`dictionaryCreationThreshold = 0`，第一次 `Add` 即建字典）。

### 行動端 AOT 的型別形狀要件（桌面完全看不出來）

reflection-only 的 `XmlSerializer`（iOS 路徑）對型別形狀比桌面嚴格：**只能有一個 public
instance `Add`**、**必須有無參數建構子**、**重複 `[XmlElement]` 的集合屬性必須有 public
setter**。前兩者由 `BEE4005` / `BEE4006` 把關，第三者只有 CI 的 AOT 閘門會抓。

> **三者的完整條文、例外訊息對照與 setter 的正確寫法（清空後逐一 `Add`，不換欄位）→
> [`rules/apple-mobile-trim.md`](../../rules/apple-mobile-trim.md) § 序列化型別的行動端相容要件
> 與 [`src/Bee.Definition/CLAUDE.md`](../../../src/Bee.Definition/CLAUDE.md)。**

## `object` 成員走判別式封套

`Parameter.Value` / `FilterCondition.Value` 這類 `object` 成員由 `WireValueFormatter` 處理，
**不走 `TypelessFormatter`**。要在行動端傳新的值型別 → 加進 `WireValueCode` 封閉集，
不要指望白名單逃生門（該分支只在有動態碼的 runtime 上可用）。

**`WireValueCode` 的數值是 wire 格式的一部分，不可重新編號** —— 跨版本不相容，
且 drift 測試抓不到。機制細節見 `src/Bee.Api.Core/CLAUDE.md`。

## wire 傳遞模式（API 端）

| 模式 | 作法 | 何時 |
|------|------|------|
| **物件本身**（建議） | `Response.Tree = DepartmentTree`；到 `WireContracts` 註冊 | **新的 API 傳遞**；樣板 `GetDepartmentTreeResponse` |
| **XML string**（歷史） | `Response.Xml = XmlCodec.Serialize(obj)`；client 端 `XmlCodec.Deserialize<T>` | **僅 define 物件**（`GetDefine` / `GetFormSchema`）——持久化格式借作 wire；新物件不走 |

## 踩雷清單

1. **註冊面的四個雷**（忘了註冊、`Collection<T>` 與 `KeyedCollection<,>` 都要註冊、
   base 型別註冊不涵蓋子型別、自訂 formatter 不得用非泛型 `Serialize(Type, ref writer, …)`）
   → 全部見 `src/Bee.Api.Core/CLAUDE.md` § 三個容易誤判的點。**共通症狀：桌面完全無感、
   行動端才炸**，所以「桌面測起來沒事」不構成任何證據。
2. **衍生 / index / owner 欄位少標一個軸 → 外洩或循環**：`[XmlIgnore, JsonIgnore]` 兩個一組。
3. **型別白名單只用於 `object` 逃生門與 `ApiPayload.TypeName`**，且它表列的是**命名空間**
   （`SysInfo.AllowedTypeNamespaces`）。**驗證 assembly-qualified name 一律用
   `WireTypeWhitelist.IsAssemblyQualifiedNameAllowed`**，不要自己切字串——泛型參數的逗號排在
   組件分隔之前，切第一個逗號會讓參數完全不受檢查（2026-08-11 修過一次未認證可達的繞過）。
4. **序列化 process-wide 快取實例會污染來源**：`XmlCodec.Serialize(obj)` 透過
   `IObjectSerialize.SetSerializeState` 在**來源物件**上翻旗標並遞迴到子集合（讓空集合 getter 在
   序列化期間回 `null`，磁碟上的定義檔才不會有 `<Tables />` 這種多餘元素）。因此
   **它不能當免費 deep clone**，要 mutate 快取取出的物件一律先 `Clone()`（見 `rules/definition.md`）。
5. **lazy index 反序列化後要能重建**：序列化只帶扁平狀態，查詢 index 在還原後第一次查詢時
   lazy 建（thread-safe）；index 本身不序列化。
6. **Oracle Guid 讀回是 `byte[]`（RAW 16）**：`ValueUtilities.CGuid` 已支援 `byte[]` coerce；
   自寫 raw DataTable 讀 Guid 欄時別用會落空的轉換。
7. **JSON 自訂 converter 僅多型才需**：單一型別集合 `System.Text.Json` 直接列舉即可；
   多型（如 `FilterNode`）才需 `JsonConverter`（見 `FilterNodeCollectionJsonConverter`）。

## 三棲 round-trip 測試樣板

```csharp
// XML（持久化軸）— 放定義物件所在的 *.UnitTests
var xml = XmlCodec.Serialize(obj);
var fromXml = XmlCodec.Deserialize<Foo>(xml)!;

// JSON（傳輸軸）— 同上
var json = JsonSerializer.Serialize(obj);
var fromJson = JsonSerializer.Deserialize<Foo>(json)!;

// MessagePack（傳輸軸）— 放 Bee.Api.Core.UnitTests（codec 在那）
var bytes = MessagePackCodec.Serialize(obj);
var fromMp = MessagePackCodec.Deserialize<Foo>(bytes)!;
```

- **要比對還原後的值，不要只 `Assert.NotNull`**。用零資料物件 round-trip 再斷言非 null 是
  名實不符的假綠燈——沒有東西可掉。正確範本 `tests/Bee.Api.Core.UnitTests/TestFunc.cs`
  （含 `comparedCount > 0` 防 helper 靜默退化）。
- **集合成員要帶值測**。廣度測試（`ApiContractSerializationTests`）對 `IEnumerable` 一律填 `null`，
  所以集合成員只有「空實例能 round-trip」的保證。
- 空集合 / 單節點邊界各測一次。
- **行動端閘門**：`dotnet test <專案> -c Release --settings .runsettings -p:DynamicCodeSupport=false`
  ——預期零失敗。CI 對 `Bee.Api.Core` / `Bee.Definition` / `Bee.Base` 三個專案跑同一關。

## 完整 checklist

- [ ] 定位用途：要持久化（XML）？要 API 傳遞（JSON/MessagePack）？還是三棲？
- [ ] 物件：無參數 ctor + `[XmlAttribute]`/`[XmlElement]` + 衍生欄位 `[XmlIgnore, JsonIgnore]`
- [ ] **定義層不帶 MessagePack 標註，也不加 `MessagePack` 的 `PackageReference`**
- [ ] 集合：元素 `: CollectionItem` / `KeyCollectionItem`，集合 `: CollectionBase<T>` / `KeyCollectionBase<T>`
- [ ] 集合只有一個 public `Add`、有無參數 ctor；`[XmlElement]` 對映的集合屬性有 public setter
- [ ] **到 `WireContracts.<Axis>.cs` 註冊 contract**；集合註冊 `CollectionBaseFormatter<,>`；
      新的封閉泛型具現補進 `WireContracts.Generics.cs`
- [ ] `object` 成員的新值型別加進 `WireValueCode` 封閉集，不倚賴逃生門
- [ ] 不對 `IDefineAccess.GetX(...)` 取得的快取實例做 mutate 或 `XmlCodec.Serialize`（要動先 `Clone()`）
- [ ] round-trip 測試**比對值**（非只 `Assert.NotNull`），集合成員帶值，含空集合邊界
- [ ] `dotnet build Bee.Library.slnx -c Release --no-incremental` 0w/0e
- [ ] `-p:DynamicCodeSupport=false` 閘門零失敗

## 參考檔案（讀程式碼對著看）

| 用途 | 檔案 |
|------|------|
| 三棲物件 + 集合樣板 | `src/Bee.Definition/Organization/DepartmentTree.cs` / `DepartmentNode.cs` / `DepartmentNodeCollection.cs` |
| 多型集合（含 JsonConverter） | `src/Bee.Definition/Filters/FilterNodeCollection.cs` / `FilterGroup.cs` |
| 集合基底 | `src/Bee.Base/Collections/CollectionBase.cs` / `KeyCollectionBase.cs` / `CollectionItem.cs` / `KeyCollectionItem.cs` |
| **wire 側全部**（註冊清單、formatter、resolver 鏈、`object` 封套、白名單、漂移閘門） | `src/Bee.Api.Core/MessagePack/` —— **檔案清單見 `src/Bee.Api.Core/CLAUDE.md`**，本檔不列 |
| XML 持久化 codec | `src/Bee.Base/Serialization/XmlCodec.cs` |
| 序列化生命週期（SerializeState 傳播） | `src/Bee.Base/Serialization/IObjectSerialize.cs` |
| round-trip 測試樣板 | `tests/Bee.Api.Core.UnitTests/TestFunc.cs`、`tests/Bee.Api.Core.UnitTests/WireFormatterTests.cs` |

## 相關規範

本 skill 是「設計一個三棲物件」的操作面；三個權威來源各管一段，**細節一律回去讀，不在此複寫**：

| 來源 | 管什麼 | 載入方式 |
|------|--------|---------|
| `rules/serialization.md` | wire 綁定的硬性規則（MessagePack 是唯一 body 格式、定義層不得引入傳輸套件） | 常駐 |
| `src/Bee.Api.Core/CLAUDE.md` | **wire 註冊程序、誤判點、`object` 封套、AOT 實測與回歸閘門** | 觸及該專案時 |
| `rules/apple-mobile-trim.md` | 行動端 trim / AOT 的完整脈絡與型別形狀要件 | 常駐 |
| `rules/definition.md` + `src/Bee.Definition/CLAUDE.md` | 定義層集合基底、cache 不可異動、setter 寫法 | 常駐 / 觸及時 |
