# 踩雷誌：序列化與運算式引擎

對應硬規則見 `.claude/rules/serialization.md`；行動端 trim/AOT 見 `.claude/rules/apple-mobile-trim.md`。

## 事實釐清：序列化維度 ≠ 加密維度（曾混淆）

查證過程（2026-07-22）：

- **`PayloadFormat`（Plain / Encoded / Encrypted）＝加密／壓縮維度**，不是 JSON-vs-MessagePack。
- body serializer 由 `ApiPayloadOptions.Serializer` 決定，而 `ApiPayloadOptionsFactory.CreateSerializer`
  的 switch **只有 `messagepack` 一個 case**——框架沒有 JSON body serializer。
- `FormApiConnector` 預設 `Encrypted`、`Login` 用 `Encoded` → authenticated 呼叫 body 一定經
  MessagePack，**client 與 server 兩端都跑**（client = `Bee.Api.Client`，跑在 iOS/Android/WASM head 上）。

**結論：MessagePack 確實在行動端 wire 路徑上。**「行動端走 JSON、MessagePack 只在桌面/伺服器間」
的假設不成立——這個誤解一度導致把 AOT 風險評估掛在錯的引擎上。

## MessagePack item ctor 參數順序 ≠ `[Key]` 順序 → 靜默對調欄位

**症狀**：只有 MessagePack wire round-trip 會露餡；XML / JSON round-trip **永遠是對的**
（它們走屬性名）。所以單看定義檔測試全綠，資料到了 client 卻是錯的。

**根因**：`CollectionBaseFormatter` 對每個 item 呼叫 `MessagePackSerializer.Serialize(item)`，
走標準 `[MessagePackObject]`+`[Key]` 契約。反序列化挑「參數最多的建構子」並**依 Key 排序位置**
把值塞給 ctor 參數（position-based，非 by-name）。

**實例**：`UnitItem` 的 Key 序是 Code(100)/Decimals(101)/Dimension(102)/Name(103)，
ctor 原為 `(code, decimals, name, dimension)` → round-trip 把 Dimension / Name 對調
（commit `eb10bc0c` 修正為 `(code, decimals, dimension, name)`）。

**正解**：ctor 參數順序＝Key 順序，且**為每個此類 item 加 MessagePack wire round-trip 測試**
（範本 `UnitSettingsMessagePackTests`）。

## `[Union]` 多型與 `keyAsPropertyName` 不相容（永久約束）

2026-07-22 執行 name-based migration，72 型別轉 `[MessagePackObject(keyAsPropertyName:true)]`
（57 合約 + 15 DTO/item），Definition 序列化 201 + Api.Core 237 測試全過。決策見
`docs/adr/adr-030-messagepack-name-based-keys.md`（已採納）。

go/no-go 最終選「立即執行」的理由是**無外部消費者 → breaking 無成本**（推翻先前的暫緩決定）。
選 `keyAsPropertyName` 而非「純去標記」的理由：**保留標記＝保留 source-gen 大門**。

**⚠️ 永久約束**：`[Union]` 用整數鍵陣列＋判別碼，與 `keyAsPropertyName` 不相容。全 repo 唯一
Union 型別 `FilterNode`（+`FilterCondition` / `FilterGroup`）**永久維持整數 `[Key]`**；
新增任何多型階層一律整數 `[Key]` + `[Union]`。集合容器（自訂 formatter/proxy）與
`SerializableData*`（DataSet plumbing）亦維持整數。

## AOT 風險評估：兩次推測、兩次被實測推翻

這兩則值得留著，因為**推測的方向都很合理、但都錯了**——遇到類似疑慮時應先實測再改架構。

### MessagePack

**當時的推測**：`MessagePackCodec` 用 `ContractlessStandardResolver.Instance` +
`CompositeResolver.Create`，**兩者都是 Reflection.Emit-based**、無 reflection-only fallback；
MessagePack 的 AOT 正解是 source generator（需 `[MessagePackObject]` 標記）→ iOS 實機 Release AOT
跑 Encrypted 表單呼叫可能在產 formatter 時 `PlatformNotSupportedException`。

**實測結果（Phase 0）**：以 runtimeconfig `IsDynamicCodeSupported=false` 重現無-Emit 路徑，
對整數 key、整數 key+集合、`keyAsPropertyName` 三型別**皆正常 round-trip、未丟例外**。
→ 當時的結論：**MessagePack 3.x 有 reflection-based fallback，source-gen 非硬前置。**

**這則的真正教訓是「樣本涵蓋範圍」（2026-08-10 補正）**：上面三個型別**全都帶
`[MessagePackObject]` 標註**。實測沒錯，錯在把結論一般化成「MessagePack 在 AOT 可用」。
NativeAOT 對照實驗顯示：

| 案例 | 結果 |
|------|------|
| `[MessagePackObject(keyAsPropertyName: true)]` + `StandardResolver` | ✅ round-trip 正常 |
| 無標註 POCO + `ContractlessStandardResolver` | ❌ `FormatterNotRegisteredException` |

**contractless 沒有 fallback。** 原推測（contractless 是 Emit-based、無 reflection-only fallback）
其實是對的——只是那次實測沒有測到 contractless，於是被誤以為推翻。
adr-036 後全 repo 改走 contractless，iOS 端的 wire 因而不通；結算見
[adr-036](../../adr/adr-036-wire-serialization-externalized.md) 的「未決事項」，
修復見 [adr-037](../../adr/adr-037-wire-explicit-registration.md)。

**心法**：實測推翻推測時，先問「我的樣本涵蓋了推測所指的那條路徑嗎？」
這裡的推測指名 contractless，樣本卻全是標註型別。

### DynamicExpresso

**當時的推測**：`Bee.Expressions` 走 `Expression.Compile()`，iOS/WASM AOT 禁 `Reflection.Emit`
→ 需要為行動端做 graceful degrade、停用即時運算。

**實測結果（2026-07-09）**：`Expression.Compile()` 在 `IsDynamicCodeSupported=false` 時自動退回
**直譯器**（CoreCLR 內建 interpreter，Mono 亦有）。`Evaluate`（計算欄 `price*qty`）、
`Evaluate<bool>`（條件規則）、`GetReferencedVariables` 全部正確。**不需停用。**

`FormLiveComputation.IsDegraded` 的 degrade 機制是為「客戶撰寫的運算式語法/識別字錯誤」
（`ExpressionEvaluationException`）防護、避免波及 `FieldValueChanged` handler 弄壞表單，
**與 AOT 無關**——別把它當成 AOT 的補救措施。

### 免實機重現法（這兩次實測共用的工具）

console 專案 csproj 加：

```xml
<RuntimeHostConfigurationOption
    Include="System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported"
    Value="false" Trim="false" />
```

或在進入點第一行（趕在任何 serializer 之前）`AppContext.SetSwitch(...)`。桌面 CLR 即走
iOS device AOT 鎖定的同一條 reflection-only BCL 路徑。**懷疑任何序列化／運算引擎的 AOT 相容性，
先用這招，別排實機。**

**更新（2026-08-10）**：不需改 csproj，一個命令列屬性即可，且走的是 .NET SDK 的同一條路徑
（`DynamicCodeSupport` 會被映射成上面那個 `RuntimeHostConfigurationOption`，
iOS SDK 就是這樣設的）：

```bash
dotnet test <測試專案> -c Release --settings .runsettings -p:DynamicCodeSupport=false
```

判讀時的兩條硬性要求（例外種類不可當診斷依據、Android 驗不到這半、要真無 Emit 就用
NativeAOT）已收進 `.claude/rules/apple-mobile-trim.md`，屬常駐規則不在此重複。

## 運算式引擎雷一：變數 key 大小寫（最先炸、最難查）

**症狀**：client 端「欄位沒即時運算」（不崩潰）；server 端存檔回 JSON-RPC **-32000**。
兩個症狀看起來毫無關聯，其實同一個根因。

**根因**：`DataTableExtensions.AddColumn` 把欄名存**大寫**（`fieldName.ToUpper()`）；
`FormExpressionCalculator.BuildVariables` 一度用 `column.ColumnName`（大寫 `QUANTITY`）當變數 key，
但運算式引用的是**宣告的欄名**（小寫 `quantity`），而 **DynamicExpresso 識別字區分大小寫** →
`UnknownIdentifierException` → 包成 `ExpressionEvaluationException`。

前後端共用 `BuildVariables` 所以兩端都中：client recompute 被 `RunGuarded` catch → latch 停用；
server 存檔無 guard → 未處理例外 → -32000。

**CI 為何沒抓到**：Phase 1 測試全用**小寫**欄名手建 DataTable，從沒測到真實 wire/DataSet 的大寫欄名。

**正解（commit `96821c04`）**：以 `FormField.FieldName`（schema 宣告大小寫）為變數 key。
`DataRow` 索引與 `Fields.Contains` 本就大小寫無關，寫回不受影響。
**回歸測試務必用大寫欄名建 DataTable。**

## 運算式引擎雷二：string 型 Guid/Binary 欄 coerce

**症狀（兩次 demo 連環炸）**：① `InvalidCastException`（client 崩潰視窗）；
② 改用 `Guid.Parse` 後 → `FormatException`，client 被 latch 停用預覽、server 存檔回 -32000。

**根因**：`ExpressionPolicy.CoerceValue`（前後端共用）**不能只靠 `Convert.ChangeType`** ——
`Guid` / `byte[]` 非 `IConvertible`。

**為何 client 才踩、後端不踩**：後端 DB 直接回 `Guid` 型欄；但 client 端 `GetData` 從 SQLite 讀回
GUID 欄是 **String 型**（見 [database.md](database.md) 的 SQLite 那則），wire round-trip 保留該欄型。
`BuildVariables` 對**該列每一欄**（含運算式根本沒引用的 Guid 鍵欄如 `product_rowid`）都 coerce。
第二次炸是因為**空字串** Guid 欄（未選產品的明細列）→ `Guid.Parse("")`。

**正解（commit `e2623195`）**：`Guid` → 空/空白字串回 `Guid.Empty`、否則 `Guid.Parse`；
`byte[]` → 空字串回空陣列、否則 `FromBase64String`。對齊「null/DBNull → 型別預設值」政策。

## 排查心法

- JSON-RPC **-32000 ＝伺服器未處理例外**（不是業務規則；業務中斷是 -32099 UserMessage）。
  非開發模式會把訊息遮蔽成 `Internal server error`，看不出真因。
- client「即時運算突然全部不動」→ 先查 `FormLiveComputation.IsDegraded`（求值/coerce 失敗會
  latch 停用**整個 session**，所以症狀是「全部不動」而不是「某一格不動」）。
- 定義類 response 在 MessagePack wire 上的失敗模式是**沉默空殼**：不擲例外、純量欄位完好、
  巢狀集合歸零。外部探測要走公開的 `MessagePackPayloadSerializer`
  （`MessagePackCodec` 是 internal，測試專案靠 `InternalsVisibleTo`）。
