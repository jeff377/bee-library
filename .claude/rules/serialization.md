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
可用；MessagePack 標註不可。全 repo 的 MessagePack 相依只在 **`Bee.Api.Core`**。

wire 綁定由 `src/Bee.Api.Core/MessagePack/` 的**手寫 formatter** 承擔，定義型別不帶標註。

## wire 型別一律顯式註冊（adr-037）

**`ContractlessStandardResolver` 不是承載機制，只是桌面端的便利退路。** 它靠
`Reflection.Emit` 產生 formatter，而 .NET for iOS 對每個建置設 `DynamicCodeSupport=false`
——那裡未註冊的型別不是變慢，是 `FormatterNotRegisteredException`。

**新增 wire 型別時必做**（`Bee.Api.Core.Messages.*`、其遞移可達的定義層型別、集合）：

1. 到 `src/Bee.Api.Core/MessagePack/WireContracts.*.cs` 加一段
   `WireContract.For<T>().Member(...)...Build()`，逐一列出成員。
2. 型別若是框架集合，改註冊 `CollectionBaseFormatter<,>` / `KeyCollectionBaseFormatter<,>`。
3. 成員若引入新的**封閉泛型具現**（`List<T>` / `Dictionary<K,V>` / `T?` / 陣列 /
   **列舉**），到 `WireContracts.Generics.cs` 補一筆——這些同樣經 `MakeGenericType` 建立，
   在 AOT 上沒有原生碼。列舉用 `WireEnumFormatter<T>`。

漏補會被 `WireContractDriftTests` 擋下（它走同一條型別閉包比對註冊清單），
不需要人工維護 `WireMemberCount` 常數。

**wire 成員的定義＝JSON 的定義**：public 可讀可寫、未標 `[JsonIgnore]` 的屬性。
框架管理成員（`Tag` / `Key` / `SerializeState` / `Collection`）本就帶該標註，自動排除。

### 三個容易誤判的點

1. **`Collection<T>` 與 `KeyedCollection<TKey,TItem>` 都要註冊。**
   桌面上 contractless 認得前者（序列化為 array）、把後者錯綁成 dictionary，
   但在 iOS 上兩者都不通——別因為「桌面測起來好像沒事」就省略。
2. **自訂 formatter 內不得使用非泛型 `MessagePackSerializer.Serialize(Type, ref writer, ...)`。**
   `MessagePackWriter` 是 `ref struct`，該多載需 `Reflection.Emit`，行動端 AOT 直接擲例外。
   逐一具名成員、全程走泛型多載。`WireContract.Member<TValue>` 的存在就是為了讓
   `TValue` 停在編譯期。
3. **base 型別註冊不涵蓋子型別。** `FilterNodeFormatter` 註冊在 `FilterNode` 上，
   呼叫端若持有 `FilterCondition` 靜態型別，解析的是
   `IMessagePackFormatter<FilterCondition>`——所以另有
   `FilterConditionFormatter` / `FilterGroupFormatter` 兩支轉接。

### `object` 成員走判別式封套，不走 `TypelessFormatter`

`Parameter.Value` / `FilterCondition.Value` 這類 `object` 成員由 `WireValueFormatter`
處理：框架自有的封閉型別集以 int 判別碼 + 封閉泛型委派讀寫；白名單內的其他型別走
「型別名 + 非泛型多載」的逃生門，**該分支只在有動態碼的 runtime 上可用**。
要在行動端傳新的值型別，把它加進封閉集合（`WireValueCode` + `WireValueFormatter` 註冊），
不要指望逃生門。

## 集合 item 的 ctor 參數順序（已不再是雷，2026-08-09）

歷史上 `[Key(n)]` 整數鍵以**位置**對號，集合 item 的參數化建構子若參數順序 ≠ `[Key]`
宣告順序，wire round-trip 會**靜默對調同型別欄位**，而 XML / JSON 抓不到。

**adr-036 後全 repo 已無整數 `[Key]`**，wire 綁定一律以屬性名為準（contractless）
或由 formatter 逐一具名，**此雷不復存在**，把關的 `BEE4004` 亦已退役。
建構子參數順序與屬性宣告順序不同（如 `CurrencyItem`）是正常的。

## AOT：MessagePack 的 reflection fallback **只涵蓋有標註的型別**（2026-08-10 修正）

> 本節先前寫的是「MessagePack 3.x 有 reflection-based fallback，source generator 非硬前置」。
> **該結論被過度一般化，已於 2026-08-10 實測推翻**，正確版本如下。

NativeAOT（真無動態碼）下的對照實驗，只用 MessagePack 自己的 resolver：

| 案例 | 結果 |
|------|------|
| `[MessagePackObject(keyAsPropertyName: true)]` 型別 + `StandardResolver` | ✅ round-trip 正常 |
| 無標註 POCO + `ContractlessStandardResolver` | ❌ `FormatterNotRegisteredException` |

**contractless 沒有 reflection fallback。** adr-030 階段 0 的原始實測（整數 key 與
`keyAsPropertyName` 皆可 round-trip）本身沒錯——那兩種都是**有標註**的型別；錯在被一般化成
「MessagePack 在 AOT 可用」。

### 歷史：這條結論曾讓 iOS 端的 wire 整條壞掉

adr-036 移除全部標註後，wire 型別改由 contractless 承載，於是在
`IsDynamicCodeSupported=false` 的 runtime 上幾乎每個 payload 型別都擲
`FormatterNotRegisteredException`（同口徑量測：37 → 185 筆失敗）。
**那不是模擬假象**（NativeAOT 上重現），**且該開關就是 .NET for iOS SDK 對每個
iOS / tvOS / MacCatalyst 建置設的預設值**（見 `rules/apple-mobile-trim.md`）。
**Android 不受影響**（保有 JIT）。

已由 adr-037 修復——全部 wire 型別改為顯式註冊，`object` 成員改走判別式封套。
**留著這段是因為它示範了一個反覆出現的錯法**：實測推翻推測時，先問
「我的樣本涵蓋了推測所指的那條路徑嗎？」當時的推測指名 contractless，樣本卻全是標註型別。

### 回歸閘門（一行，不需改 csproj）

```bash
dotnet test tests/Bee.Api.Core.UnitTests/Bee.Api.Core.UnitTests.csproj -c Release --settings .runsettings -p:DynamicCodeSupport=false
```

`DynamicCodeSupport` 是 .NET SDK 的標準屬性，會被映射成
`RuntimeFeature.IsDynamicCodeSupported` 的 `RuntimeHostConfigurationOption`——
與 iOS SDK 的做法完全相同。**預期零失敗**；CI 已納入同一道關卡。

無法在此通過、且確屬「桌面才有的能力」的測試，標 `[DynamicCodeFact]` 讓它自動略過。
**不要拿它來消音**：框架自有的 wire 型別若需要動態碼，那是缺陷不是測試問題。

## AOT：DynamicExpresso 無需特殊處理（此條不變）

DynamicExpresso 的 `Expression.Compile()` 在 `IsDynamicCodeSupported=false` 時自動退回**直譯器**。

**行動端不需為 AOT 停用即時運算。** `FormLiveComputation.IsDegraded` 的 degrade 機制是為
「客戶撰寫的運算式語法/識別字錯誤」防護，**與 AOT 無關**。

## 運算式變數表兩條硬性要求

1. **變數 key 一律用 `FormField.FieldName`（schema 宣告的大小寫）**，不要用
   `DataColumn.ColumnName` —— **DynamicExpresso 識別字區分大小寫**，而運算式寫的是宣告欄名；
   拿 `DataColumn.ColumnName` 當 key 等於把運算式綁死在「記憶體 DataSet 當下用哪種大小寫存欄名」上。
   `DataRow` 索引與 `Fields.Contains` 本就大小寫無關，寫回不受影響。

   > **`AddColumn` 現在存小寫，不是大寫**（`fieldName.ToLowerInvariant()`，
   > `src/Bee.Base/Data/DataTableExtensions.cs`）。歷史上它存大寫，用大寫當 key 會直接
   > `UnknownIdentifierException`；ADR-029（欄名一律小寫，定義／資料／UI 三層一致）已把儲存
   > 大小寫遷移為小寫，**與宣告欄名恰好一致**。這**不代表本條失效**——結論本來就是「與儲存
   > 大小寫解耦」，而不是「避開大寫」；恰好一致只是讓違反此條的寫法暫時看不出症狀。

   **回歸測試務必用「與宣告欄名大小寫不同」的欄名建 DataTable**（現行實作下即大寫）。
   用跟宣告欄名一模一樣的小寫測，兩種寫法都會過，等於沒測到解耦。
2. **`ExpressionPolicy.CoerceValue` 不能只靠 `Convert.ChangeType`** —— `Guid` / `byte[]` 非
   `IConvertible`。client 端從 SQLite 讀回的 GUID 欄是 **String 型**，且可能是**空字串**。
   規則：`Guid` → 空/空白回 `Guid.Empty`、否則 `Guid.Parse`；`byte[]` → 空字串回空陣列、
   否則 `FromBase64String`。對齊「null/DBNull → 型別預設值」政策。
