# 序列化與運算式引擎規範

> 踩雷細節與實測脈絡見 `docs/repo-ops/gotchas/serialization-and-expressions.md`。
> 行動端 trim / AOT 的型別形狀要件見 `rules/apple-mobile-trim.md`。

## MessagePack 是唯一的 wire body 格式

`ApiPayloadOptionsFactory.CreateSerializer` 的 switch **只有 `messagepack` 一個 case** ——
**框架沒有 JSON body serializer**。`PayloadFormat`（Plain/Encoded/Encrypted）是**加密/壓縮維度**，
與 JSON-vs-MessagePack 無關。

因此 **client（含 iOS / Android / WASM head）與 server 兩端都跑 MessagePack**。
「行動端走 JSON、MessagePack 只在桌面/伺服器間」的假設不成立。

## 定義層不得引入傳輸格式套件（adr-036）

`src/Bee.Definition` **不得**有 `MessagePack`（或任何傳輸格式套件）的 `PackageReference`。
判準是「會不會讓定義層長出外部套件相依」：`[XmlIgnore]` / `[JsonIgnore]` 是 BCL 詞彙、
可用；MessagePack 標註不可。全 repo 的 MessagePack 相依只在 **`Bee.Api.Core`** 一處。

wire 綁定由 `src/Bee.Api.Core/MessagePack/` 的**手寫 formatter** 承擔，定義型別不帶標註：

- 需排除框架管理成員（`Tag` / `Collection` 等）的合約型別 → 一支專屬 formatter
- 多型（`FilterNode` 家族）→ `FilterNodeFormatter`，以 `Kind` 為判別碼
- `KeyedCollection` 子型別 → `KeyCollectionBaseFormatter`（**不可省**，見下）
- 其餘 → `ContractlessStandardResolver` 以屬性名為鍵，無需任何動作

**新增 wire 型別時**：若沒有需排除的成員，什麼都不用做；若有，寫一支 formatter 並在
`MessagePackCodec` 註冊，同時公開 `WireMemberCount` 常數並在測試斷言——
編譯器不會把型別與 formatter 綁在一起，該斷言是唯一的漂移守衛。

### 兩個容易誤判的點

1. **`Collection<T>` 不需 formatter，`KeyedCollection<TKey,TItem>` 需要。**
   contractless 認得前者並序列化為 array；後者會被綁成 dictionary，
   元素還原成 `Dictionary<object,object>` 而非 item 型別。
2. **自訂 formatter 內不得使用非泛型 `MessagePackSerializer.Serialize(Type, ref writer, ...)`。**
   `MessagePackWriter` 是 `ref struct`，該多載需 `Reflection.Emit`，行動端 AOT 直接擲例外。
   逐一具名成員、全程走泛型多載。

## 集合 item 的 ctor 參數順序（已不再是雷，2026-08-09）

歷史上 `[Key(n)]` 整數鍵以**位置**對號，集合 item 的參數化建構子若參數順序 ≠ `[Key]`
宣告順序，wire round-trip 會**靜默對調同型別欄位**，而 XML / JSON 抓不到。

**adr-036 後全 repo 已無整數 `[Key]`**，wire 綁定一律以屬性名為準（contractless）
或由 formatter 逐一具名，**此雷不復存在**，把關的 `BEE4004` 亦已退役。
建構子參數順序與屬性宣告順序不同（如 `CurrencyItem`）是正常的。

## AOT：MessagePack 與 DynamicExpresso 皆無需特殊處理

兩者都曾被懷疑在 iOS/WASM AOT（reflection-only、禁 `Reflection.Emit`）不可用，**實測皆推翻**：

- MessagePack 3.x 有 reflection-based fallback，**source generator 非硬前置**
  （保留 `[MessagePackObject]` 標記只是保留 source-gen 大門當免費保險）。
- DynamicExpresso 的 `Expression.Compile()` 在 `IsDynamicCodeSupported=false` 時自動退回**直譯器**。

**行動端 / WASM 不需為 AOT 停用即時運算。** `FormLiveComputation.IsDegraded` 的 degrade 機制是為
「客戶撰寫的運算式語法/識別字錯誤」防護，**與 AOT 無關**。

驗證任何序列化 / 運算引擎的 AOT 相容性，先用免實機重現法（見 `rules/apple-mobile-trim.md`），
別排實機。

## 運算式變數表兩條硬性要求

1. **變數 key 一律用 `FormField.FieldName`（schema 宣告的大小寫）**，不要用
   `DataColumn.ColumnName` —— `DataTableExtensions.AddColumn` 把欄名存**大寫**，而
   **DynamicExpresso 識別字區分大小寫**，用大寫當 key 會 `UnknownIdentifierException`。
   `DataRow` 索引與 `Fields.Contains` 本就大小寫無關，寫回不受影響。
   **回歸測試務必用大寫欄名建 DataTable**（用小寫測等於沒測）。
2. **`ExpressionPolicy.CoerceValue` 不能只靠 `Convert.ChangeType`** —— `Guid` / `byte[]` 非
   `IConvertible`。client 端從 SQLite 讀回的 GUID 欄是 **String 型**，且可能是**空字串**。
   規則：`Guid` → 空/空白回 `Guid.Empty`、否則 `Guid.Parse`；`byte[]` → 空字串回空陣列、
   否則 `FromBase64String`。對齊「null/DBNull → 型別預設值」政策。
