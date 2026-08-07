# 序列化與運算式引擎規範

> 踩雷細節與實測脈絡見 `docs/repo-ops/gotchas/serialization-and-expressions.md`。
> 行動端 trim / AOT 的型別形狀要件見 `rules/apple-mobile-trim.md`。

## MessagePack 是唯一的 wire body 格式

`ApiPayloadOptionsFactory.CreateSerializer` 的 switch **只有 `messagepack` 一個 case** ——
**框架沒有 JSON body serializer**。`PayloadFormat`（Plain/Encoded/Encrypted）是**加密/壓縮維度**，
與 JSON-vs-MessagePack 無關。

因此 **client（含 iOS / Android / WASM head）與 server 兩端都跑 MessagePack**。
「行動端走 JSON、MessagePack 只在桌面/伺服器間」的假設不成立。

## `[Union]` 多型永久維持整數 `[Key]`

一般型別已於 adr-030 全面轉為 `[MessagePackObject(keyAsPropertyName:true)]`（72 型別）。
**但 `[Union]` 多型用整數鍵陣列＋判別碼，與 `keyAsPropertyName` 不相容。**

- `FilterNode`（+`FilterCondition` / `FilterGroup`）**永久維持整數 `[Key]`**。
- **新增任何多型 MessagePack 階層一律整數 `[Key]` + `[Union]`。**
- 集合容器（自訂 formatter / proxy）與 `SerializableData*`（DataSet plumbing）亦維持整數。

## 集合 item 的 ctor 參數順序必須＝`[Key]` 順序

`MessagePackCollectionItem` 子型別（走 `CollectionBaseFormatter<TColl,TItem>` 上 wire 的集合 item）
若有參數化建構子，**參數順序必須與 `[Key(n)]` 宣告順序一致**。

反序列化挑「參數最多的建構子」並**依 Key 排序位置**塞值（position-based，非 by-name）。順序不符
→ 同型別欄位被**靜默對調**，而 **XML / JSON round-trip 抓不到**（它們走屬性名，永遠對）。
**務必為每個此類 item 加 MessagePack wire round-trip 測試**（範本見 `UnitSettingsMessagePackTests`）。

> **限整數 `[Key]` 型別（即 `[Union]` 家族）。** `keyAsPropertyName: true` 的型別以**名稱**比對，
> 建構子參數順序顛倒仍能正確 round-trip，不受此限——BEE4004 也刻意把它們排除在外
> （見 `MessagePackConstructorOrderAnalyzer` 的 remarks）。
> 誤把此規則套到 name-based 型別，會導出「必須調換參數順序」的錯誤結論；
> `CurrencyItem` 的 ctor 順序與屬性宣告序不同即為正常，**不是缺陷**。

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
