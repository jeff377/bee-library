# 計畫：解除 Bee.Definition 對 MessagePack 的相依

**狀態：✅ 已完成（2026-08-09）**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 0 | 可行性驗證（spike）：五項前提實測 | ✅ 已完成（2026-08-09）——原設計否決，改採手寫 formatter（發現 7–9） |
| 1 | `[WireIgnore]` 標註 + 6 支手寫 formatter | ✅ 已完成（2026-08-09） |
| 2 | `FilterNode` 家族外置為 `FilterNodeFormatter` | ✅ 已完成（2026-08-09）——標註待階段 5 連同連通分量一起移除 |
| 3 | `SafeTypelessFormatter` 遷入 Api.Core | ✅ 已完成（2026-08-09） |
| 4 | 移除全部 MessagePack 標註與 `PackageReference`；集合註冊清單退場 | ✅ 已完成（2026-08-09） |
| 5 | 四對雙胞胎合併、四條 analyzer 規則退役、adr-036 與規則文件 | ✅ 已完成（2026-08-09） |

## 目標與理由

**讓定義層不因傳輸序列化的技術選擇而改變。** 未來若出現更適合的傳輸格式，
`src/Bee.Definition/` 應該零改動。

這是**驗收判準**，不只是方向：新增一個傳輸格式時，若必須回頭修改定義型別，
本計畫就沒有達成目的。

### 分界線：BCL 內建的留下，需要外部套件的外置

判準不是「是不是傳輸格式」，而是**「會不會讓定義層長出外部套件相依」**。

| 格式 | 角色 | 是否需外部套件 | 處置 | 現況足跡（`src/Bee.Definition/`） |
|------|------|--------------|------|--------------------------------|
| **XML** | 定義層自己的持久化——定義檔、存檔、快照 | ❌ BCL 內建 | ✅ **留下** | 57 檔 |
| **JSON** | 通用 web API 傳輸，.NET 預設支援 | ❌ BCL 內建 | ✅ **留下** | 20 檔 |
| **MessagePack** | 高效傳輸 | ✅ `PackageReference` | ❌ **外置至 `Bee.Api.Core`** | 37 檔 |

XML 與 JSON 的標註（`[XmlIgnore]` / `[JsonIgnore]`）屬於**平台詞彙**，
定義層使用它們不引入任何相依，也不構成對特定第三方格式的綁定。
MessagePack 不同——它是一個明確的技術選擇，且會沿相依鏈傳染給
`Bee.Db`、`Bee.Repository`、`Bee.Business`、`Bee.UI.Avalonia`、`tools/Bee.Cli`、
`tools/DefineEditor` 這些完全不需要它的消費者。

**未來若出現更適合的傳輸格式**，只要它同樣以套件形式引入，就走本計畫建立的
「Api.Core 註冊 formatter」路徑，定義層零改動——這正是本計畫要建立的機制。

## 現況盤點

`Bee.Definition` 有 **37 個 `.cs` 檔**引用 MessagePack，可分六類綁定：

| # | 綁定型式 | 數量 | 位置 |
|---|---------|------|------|
| 1 | `[MessagePackObject(keyAsPropertyName: true)]` | 約 14 | `SortField`、`PagingOptions`/`PagingInfo`、`DepartmentTree`/`Node`、`CompanyInfo`、`ApiKeySummary`、`CurrencyItem`/`UnitItem`/`NumberFormatItem`/`AllowedCurrencyItem`/`CashRoundingItem`、`Parameter` |
| 2 | `[MessagePackObject]`（裸，集合容器） | 8 | `FilterNodeCollection`、`SortFieldCollection`、`DepartmentNodeCollection`、`CompanyNumberFormats`、`CompanyCashRounding`、`CompanyAllowedCurrencies`、`CurrencySettings`、`UnitSettings`、`ParameterCollection` |
| 3 | `[IgnoreMember]` | 31 | 4 個集合基底（13）、7 個定義型別（17）、`FilterNode.Kind`（1） |
| 4 | `[Key(n)]` + `[Union]` | 約 10 | `FilterNode` / `FilterCondition` / `FilterGroup`、`MessagePackKeyCollectionBase.ItemsForSerialization` |
| 5 | `IMessagePackSerializationCallbackReceiver` | 1 | `MessagePackKeyCollectionBase<T>` |
| 6 | `[MessagePackFormatter]` + formatter 實作 | 2 | `Parameter.Value`、`SafeTypelessFormatter` |

## 範圍收窄：只有 API 合約面需要 MessagePack

**判準**：定義檔相關資料一律以 **XML 字串**傳到前端（`GetDefine` / `SaveDefine` 走
[SystemApiConnector.cs:178-179](../../../src/Bee.Api.Client/Connectors/SystemApiConnector.cs) 的
`XmlCodec.Deserialize<T>(result.Xml)` / `XmlCodec.Serialize`），**從不經 MessagePack**。
只有 API 合約上「以物件形式」傳遞的型別才需要 MessagePack 支援。

依此追出實際的 wire 型別集合（來源：`Bee.Api.Contracts` 與 `Bee.Api.Core/Messages` 的
`using Bee.Definition.*` + 遞移可達）：

| 需 MessagePack（合約面可達） | 入口 |
|---------------------------|------|
| `FilterNode` / `FilterCondition` / `FilterGroup` / `FilterNodeCollection` | `IGetListRequest.Filter` |
| `SortField` / `SortFieldCollection` | `IGetListRequest.SortFields` |
| `PagingOptions` / `PagingInfo` | `IGetListRequest.Paging`（22 處引用，最廣） |
| `DepartmentTree` / `DepartmentNode` / `DepartmentNodeCollection` | `IGetDepartmentTreeResponse.Tree` |
| `CompanyInfo` | `IEnterCompanyResponse.Company` |
| `CompanyNumberFormats` / `NumberFormatItem`、`CompanyCashRounding` / `CashRoundingItem`、`CompanyAllowedCurrencies` / `AllowedCurrencyItem` | **遞移**——`CompanyInfo` 的三個屬性 |
| `ApiKeySummary` | `IListApiKeysResponse.ApiKeys` |
| `Parameter` / `ParameterCollection` | `ApiMessageBase.Parameters` |

**不需 MessagePack（定義檔資料，走 XML 字串）**：

| 型別 | 證據 |
|------|------|
| `CurrencySettings` / `CurrencyItem`、`UnitSettings` / `UnitItem` | 兩者皆為 `DefineType` 成員（[DefineType.cs:47,51](../../../src/Bee.Definition/DefineType.cs)），經 `GetDefineAsync<T>` → XML。**合約面零引用** |
| `FormSchema`、`TableSchema`、`FormLayout`、`SystemSettings`、`ClientSettings`、`DatabaseSettings`、`DbCategorySettings` | 同上，且僅帶 `[IgnoreMember]`（發現 3） |

→ 這 11 個型別的 MessagePack 標註**可直接刪除，無需任何替代機制**。

### typeless 逃生口：實測結論

`Parameter.Value` 是 `object?`（見元件四），`ExecFunc` 可經此通道傳遞**任意** Definition 型別，
`SafeTypelessFormatter.IsTypeAllowed` 委派給 `SysInfo.IsTypeNameAllowed`（`Bee.*` 命名空間放行）。
因此「不在宣告合約面」不等於「runtime 不會經過 MessagePack」。

**但實測顯示這不構成隱患**（見發現 6）：未標註 `[MessagePackObject]` 的集合型別經 typeless
通道 round-trip，內容完整還原，且 wire 格式與 `CollectionBaseFormatter` 完全一致。
移除標註**同時**解決了「必須記得註冊」的問題——因為需要註冊的原因正是標註本身。

## 九個關鍵發現

發現 1–4 來自盤點；**發現 5–9 來自階段 0 spike 實測**——5 收緊執行順序、
6 簡化階段 4、7 否決了 `BeeObjectFormatter` 原設計、8 給出替代解、
9 是超出範圍但值得另案追查的既有問題。

### 發現 5：標註移除有編譯期順序相依（2026-08-09 實測）

發現 2、3 說「這些標註不生效，移除是零行為變更」——**runtime 成立，compile time 不成立**。
MessagePack 自帶的 analyzer 會擋：

| 診斷 | 規則 | 實測觸發 |
|------|------|---------|
| **MsgPack003** | 被 `[MessagePackObject]` 型別**引用**的型別，自己也必須有 `[MessagePackObject]` | 移除 8 個集合容器的裸標記 → 8 個 error（它們被 `GetListRequest` 等 attributed 型別引用） |
| **MsgPack004** | `[MessagePackObject]` 型別的**基底**成員必須帶 `[Key]` 或 `[IgnoreMember]` | 移除 `MessagePackKeyCollectionBase` 的 3 個 `[IgnoreMember]` → `ParameterCollection` 報 3 個 error |

→ **標註不能逐型別漸進移除**，必須沿引用圖由外而內、以連通分量為單位一次處理。
而所有 Definition wire 型別都被 `Bee.Api.Core/Messages/` 的 57 個 `[MessagePackObject]`
型別引用——**連通分量涵蓋 Api.Core 的訊息型別**。

**因應**：Api.Core 訊息型別的 `[MessagePackObject(keyAsPropertyName: true)]` 需**一併移除**。
這在 wire 格式上等價（`keyAsPropertyName` 與 contractless 皆以屬性名為鍵，見 adr-030），
且 Api.Core 屬傳輸層、本就允許保留 MessagePack 相依——改的只是「如何宣告」，不是「能否使用」。

代價是放棄 source generator 退路（原本就是本計畫已接受的成本，見風險表）。

**已驗證可獨立移除者**：7 個定義型別（`FormSchema` / `TableSchema` / `FormLayout` /
`SystemSettings` / `ClientSettings` / `DatabaseSettings` / `DbCategorySettings`）的 17 個
`[IgnoreMember]` 與 `using MessagePack;`——它們不被任何 attributed 型別引用，
移除後 clean Release build 0 error 0 warning，`Bee.Definition.UnitTests` 1071 +
`Bee.Api.Core.UnitTests` 682 全數通過。

### 發現 6：`[MessagePackObject]` 才是集合需要顯式註冊的原因（2026-08-09 實測）

`FormatterResolver` 的 WARNING 稱「未註冊的集合反序列化會擲
`MessagePackSerializationException`」。實測**不成立**——前提是該集合帶 `[MessagePackObject]`。

以 `SpikeParameterValueTests` 六個測試驗證（皆通過）：

| 測試 | 結果 |
|------|------|
| 未註冊的 `KeyCollectionBase<T>` 子型別（`FormFieldCollection`）經 **typeless 通道** | ✅ 內容完整還原（非僅「不擲例外」） |
| 未註冊、**且無 `[MessagePackObject]`** 的 `MessagePackCollectionBase<T>` 子型別 | ✅ 內容完整還原 |
| 有標註且已註冊（`SortFieldCollection`）vs 無標註未註冊，wire 首位元組 | ✅ **皆為 `0x91`（fixarray）——格式完全一致** |

原因：`[MessagePackObject]` 是 **opt-in**，集合上只有零個 `[Key]` 成員 → 空 map；
拿掉標註後 contractless 認得 `Collection<T>` / `KeyedCollection<,>` 是集合，
原生序列化為 array，**與 `CollectionBaseFormatter` 產出的格式相同**。

→ **階段 4 大幅簡化**：移除標註後不需要 resolver 前移、不需要遞迴 base-type 檢查，
8 筆 `CollectionBaseFormatter` 顯式註冊直接退場（階段 4 實測移除後全綠）。

> ⚠️ **本結論只對 `Collection<T>` 成立，對 `KeyedCollection<TKey,TItem>` 不成立**
> （階段 4 實測修正）。contractless 把 keyed collection 綁成 dictionary，
> 元素還原為 `Dictionary<object,object>` 而非 item 型別，直接
> `Deserialize<ParameterCollection>` 會擲 `ArgumentException`。
> spike 當時之所以沒抓到，是因為測的是 **typeless 通道**——payload 自帶具體型別名，
> 走的是另一條還原路徑。
> **`KeyCollectionBaseFormatter` 因此仍是必要元件**，已於階段 4 實作。

→ 連帶修正：本計畫先前所稱「25 個 `KeyCollectionBase` 子型別經 typeless 通道會擲例外
的長期隱患」**不存在**，該敘述已移除。

### 發現 7：`BeeObjectFormatter` 原設計在 AOT 下不可行（2026-08-09 實測，**閘門失敗**）

原型已實作（`BeeObjectFormatter<T>`，純反射、無 `Reflection.Emit`），
一般模式下 4 個測試全過，含 wire 形狀斷言（`0x82` = 2 成員 map，
證明 `Tag` / `SerializeState` / `Collection` 確實排除）。

**但在 reflection-only 模式下 13 個 spike 測試失敗 12 個**：

```
System.NotSupportedException:
  MessagePackWriter/Reader overload is not supported in MessagePackSerializer.NonGenerics
    at MessagePack.MessagePackSerializer.CompiledMethods.ThrowRefStructNotSupported()
```

根因：formatter 內對「任意屬性型別」遞迴時，只能用**非泛型**多載
`MessagePackSerializer.Serialize(Type, ref MessagePackWriter, object, options)`。
而 `MessagePackWriter` 是 **`ref struct`**——非泛型路徑需要 `Reflection.Emit`
產生能傳遞 ref struct 的委派，`IsDynamicCodeSupported=false` 時直接擲例外。

> 這與 [rules/serialization.md](../../../.claude/rules/serialization.md) 記載的
> 「MessagePack 3.x 有 reflection-based fallback，AOT 可用」**不衝突**：
> 該結論針對 MessagePack **自己產生**的 formatter，不涵蓋
> 「自訂 formatter 內呼叫非泛型 API」這條路徑。

**重現方式**（供後續驗證沿用）：在測試專案 csproj 加

```xml
<RuntimeHostConfigurationOption
    Include="System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported"
    Value="false" />
```

**尚待釐清**：contractless resolver 原生的成員納入規則。目前的測試因 item 基底仍保留
`[IgnoreMember]`（`[WireIgnore]` 是**加上去**而非取代）而測不到原生行為——
需移除 `[IgnoreMember]` 後重測，才能判定 `Tag` / `SerializeState` / `Collection`
是否本來就被 contractless 排除。這決定後續走哪條路（見下）。

**三條可能出路**（尚未評估）：

| 出路 | 作法 | 風險 |
|------|------|------|
| A. 泛型成員分派 | 抽象 `Member` 基底 + `Member<TProp>` 泛型子類，改呼叫泛型多載 `Serialize<T>(ref writer, ...)` | 需 `MakeGenericType` + `Activator.CreateInstance`；**值型別（如 `SortDirection` enum）的實例化在 AOT 下可能不存在** |
| B. 以型別形貌取代標註 | 若 contractless 原生已排除唯讀 / 非公開 setter 成員，則只剩 `Tag` 與 `Key` 需處理，改以顯式介面實作使其不可見 | 破壞公開 API 表面（`item.Tag` 需轉型） |
| C. 保留 `[IgnoreMember]` | 放棄本計畫主目標 | `Bee.Definition` 續留 MessagePack 相依 |

→ **B 最便宜且需先做的實驗**：移除 item 基底的 `[IgnoreMember]`，觀察 contractless
納入哪些成員。若原生就排除多數，範圍會急遽縮小。

### 發現 8：手寫 formatter 是 AOT 下唯一可行的路（2026-08-09 實測）

發現 7 的根因是「對**任意**屬性型別遞迴」只能走非泛型 API。但本計畫只需為
**6 個具體型別**做這件事，而它們的屬性型別**編譯期已知**——
手寫 formatter 即可全程使用泛型多載 `Serialize<T>(ref writer, ...)`，零反射。

以 `SortFieldFormatter` 驗證（[SortFieldFormatter.cs](../../../src/Bee.Api.Core/MessagePack/SortFieldFormatter.cs)）：
reflection-only 模式下 4 個測試**全過**，取代了原本失敗的泛型反射版本。

→ **`BeeObjectFormatter<T>`（泛型反射版）否決**，改為每個型別一支手寫 formatter，
與既有的 `DataSetFormatter` / `DataTableFormatter` / `CollectionBaseFormatter`
以及規劃中的 `FilterNodeFormatter` 風格一致。

**維護性防護**：每支 formatter 公開 `WireMemberCount` 常數，wire 測試斷言
map header 與之相符——`SortField` 新增屬性而未同步 formatter 時測試立刻紅。

### 發現 9：現行 wire 路徑在 reflection-only 下大量失敗（2026-08-09 實測，**超出本計畫範圍**）

以 `IsDynamicCodeSupported=false` 跑 `Bee.Api.Core.UnitTests`：

| 分支 | 失敗數 |
|------|-------|
| **乾淨 `main`（無任何本計畫改動）** | **51 / 694** |
| spike 分支（含手寫 `SortFieldFormatter`） | 45 / 693 |

失敗集中於既有 production formatter：`ParameterCollection`（12）、
`System.Data.DataTable`（11）、`DataSet`（3）、`FilterNode` / `FilterGroup`（2）。

**這些失敗是既有的，不是本計畫造成的**——本計畫的手寫 formatter 反而修好 6 個。

> ⚠️ **此發現與 [rules/serialization.md](../../../.claude/rules/serialization.md) 記載的
> 「MessagePack 3.x 有 reflection-based fallback，行動端 AOT 可用」不一致**，
> 值得獨立追查。兩點保留：
> 1. 此為 JIT runtime 上的**模擬**（`RuntimeHostConfigurationOption`），
>    雖是 [apple-mobile-trim.md](../../../.claude/rules/apple-mobile-trim.md) 認可的免實機驗證法，
>    真實裝置 AOT 行為未必相同。**且 `Parameter.Value` 那條路徑擲的是
>    `InvalidProgramException`（"CLR detected an invalid program"）而非乾淨的
>    `NotSupportedException`**——那是「Emit 仍然執行、但產出無效 IL」的徵狀，
>    高度懷疑是模擬本身的假象，不代表實機行為。判讀時務必分開看待這兩種例外。
> 2. 失敗是否會在實際 wire 流程中顯現，取決於這些型別在行動端是否真的走 MessagePack。
>
> **本計畫不處理**——範圍是「解除相依」，不是「修復行動端 AOT」。
> 建議另立 plan 追查，或先在 `docs/repo-ops/future-work.md` 記一筆。

## 前四個發現（來自盤點）

### 發現 1：`ApiContractRegistry` 的 attribute 偵測是惰性的

[adr-030](../../adr/adr-030-messagepack-name-based-keys.md) 寫道，8 個集合型別的裸
`[MessagePackObject]` 標記「仍為 `ApiContractRegistry.ConvertForSerialization` 的判斷依據，
**不可移除**」。

但 [ApiContractRegistry.cs:13-18](../../../src/Bee.Api.Core/Registry/ApiContractRegistry.cs) 自己的
remarks 已說明：**沒有任何 production code 呼叫 `Register`，映射表恆為空，轉換路徑完全惰性。**

因此 [ApiContractRegistry.cs:50](../../../src/Bee.Api.Core/Registry/ApiContractRegistry.cs) 的
`GetCustomAttribute<MessagePackObjectAttribute>()` 只是一個短路；移除 attribute 後，
流程會落到下方的介面迴圈，因映射表為空而原樣回傳——**行為完全相同**。

→ adr-030 的「不可移除」結論**需要修訂**。這是階段 5 的工作項。

### 發現 2：集合容器的標註對序列化不生效

8 個 `MessagePackCollectionBase<T>` 子型別全部在
[MessagePackCodec.cs:29-36](../../../src/Bee.Api.Core/MessagePack/MessagePackCodec.cs) 顯式註冊了
`CollectionBaseFormatter`，而該 formatter 把集合序列化為 **array**，只寫 elements、
完全不讀屬性。

→ 這 8 個型別的裸 `[MessagePackObject]` 與集合基底的 13 個 `[IgnoreMember]`，
在實際 wire 路徑上**不生效**。移除它們是零行為變更。

> 但**顯式註冊本身不可少**——`FormatterResolver` 的自動 fallback 因排在
> `ContractlessStandardResolver` 之後而不可達（見
> [FormatterResolver.cs:13-37](../../../src/Bee.Api.Core/MessagePack/FormatterResolver.cs) 的 WARNING）。
> `BEE4001` 在編譯期把關遺漏。

### 發現 3：真正生效的 `[IgnoreMember]` 只有 8 個

31 個 `[IgnoreMember]` 中：

| 群組 | 數量 | 是否生效 | 原因 |
|------|------|---------|------|
| 容器基底 `MessagePackCollectionBase` | 3 | ❌ | 走 `CollectionBaseFormatter`，序列化為 array，屬性從不被讀（發現 2） |
| 容器基底 `MessagePackKeyCollectionBase` | 3 | ❌ | 唯一子型別 `ParameterCollection` 採 `[MessagePackObject]` **opt-in** + `[Key(0)]` proxy，未標 `[Key]` 的成員預設即排除 |
| 定義型別（`FormSchema` / `TableSchema` / `FormLayout` / `SystemSettings` / `ClientSettings` / `DatabaseSettings` / `DbCategorySettings`） | 17 | ❌ | 這些型別以 **XML 字串**上 wire（見 [IGetFormSchemaResponse.cs](../../../src/Bee.Api.Contracts/System/IGetFormSchemaResponse.cs) 的 remarks），從不經 MessagePack |
| **Item 基底 + `FilterNode.Kind`** | **8** | ✅ | item 走 contractless，屬性逐一序列化 |

生效的 8 個是：

- `MessagePackCollectionItem`：`Tag`、`SerializeState`、`Collection`
- `MessagePackKeyCollectionItem`：`Key`、`Tag`、`SerializeState`、`Collection`
- `FilterNode`：`Kind`

**這 8 個全部是 `Bee.Base` 介面（`ITagProperty` / `IObjectSerialize` / `ICollectionItem` /
`IKeyCollectionItem`）的成員，或是有子類代理的判別屬性。**

`Collection` 屬性尤其關鍵：它是 item 指回容器的反向導航，一旦被序列化就是
**無限遞迴**（child → collection → children → …），不是單純的位元組浪費。

→ 需要替代機制的範圍從 31 縮到 8，且集中在 2 個基底類別 + 1 個判別屬性。

### 發現 4：`Bee.Base` 的既有原則與本計畫的分界線一致

[MessagePackCollectionBase.cs:16-20](../../../src/Bee.Definition/Collections/MessagePackCollectionBase.cs)
的 remarks 說 `Bee.Base` 「takes no external package references at all」，因此無法承載
MessagePack attribute——這是四對雙胞胎型別存在的唯一理由。

而 `Bee.Base` 同時大量使用 `System.Text.Json`（`JsonCodec`、`DataSetJsonConverter`、
`DataTableJsonConverter`，以及 `CollectionBase` / `KeyCollectionBase` / `CollectionItem`
上的 `[JsonIgnore]`）而不違反該原則。

→ `Bee.Base` 實行的正是本計畫的分界線：**BCL 詞彙可用，外部套件不可引**。
本計畫等於把這條既有原則從 `Bee.Base` 延伸到 `Bee.Definition`——不是新發明的規則，
而是把已經在框架底層生效的規則往上推一層。

這也讓階段 4 的雙胞胎合併成立：MessagePack 相依移除後，
`MessagePackCollectionBase` 與 `CollectionBase` 之間**不再有任何差異理由**。

## 附帶收益：消滅四對雙胞胎型別

`Bee.Definition/Collections/` 的四個型別是 `Bee.Base/Collections/` 對應型別的**刻意複製**：

| Bee.Definition（子型別數） | Bee.Base（子型別數） | 實測差異 |
|---------------------------|---------------------|---------|
| `MessagePackCollectionBase<T>`（9） | `CollectionBase<T>`（8） | 僅 attribute |
| `MessagePackCollectionItem`（8） | `CollectionItem`（7） | 公開成員**完全一致** |
| `MessagePackKeyCollectionBase<T>`（1） | `KeyCollectionBase<T>`（25） | attribute + callback 介面 + **已漂移**（見下） |
| `MessagePackKeyCollectionItem`（1） | `KeyCollectionItem`（24） | 公開成員**完全一致** |

`MessagePackCollectionBase.cs` 的 remarks 明文要求「**Keep the two in step**：對其一的行為變更
幾乎總是也屬於另一個——分歧應該只有 attribute」。

**這條要求已經被違反**：`Bee.Base.KeyCollectionBase.GetOrDefault(string key)`
（[KeyCollectionBase.cs:161](../../../src/Bee.Base/Collections/KeyCollectionBase.cs)）
在 `MessagePackKeyCollectionBase` 中**不存在**。靠註解而非編譯期把關的維護稅，已經在付。

**四對全部合併，四個 `MessagePack*` 型別全數刪除**，序列化細節一律由 Api.Core 的 formatter
處理——與 `FilterNodeFormatter`、`SafeTypelessFormatter` 的處置方式一致。

### 前置條件：修好 resolver，讓顯式註冊變成不必要

現行架構要求**每個**集合型別在 `MessagePackCodec` 顯式註冊 `CollectionBaseFormatter`，
漏了就在反序列化時擲 `MessagePackSerializationException`。BEE4001 這條規則存在的唯一理由，
就是替這個手動步驟把關。

但 [FormatterResolver.cs](../../../src/Bee.Api.Core/MessagePack/FormatterResolver.cs) **本來就有**
自動偵測邏輯（比對 `BaseType` 是否為 `MessagePackCollectionBase<>`，反射建構
`CollectionBaseFormatter<,>`），只是不可達。該檔 WARNING 已寫明修法：

> "Making this a real safety net would require the fallback to return null and let the composite
> resolver continue, plus a recursive base-type check (the test below only matches a direct base type)."

本計畫既然要新寫 `BeeContractlessResolver`（元件二），就一併把這兩件事做對：

1. **遞迴 base-type 檢查**（現行只比對直接基底）
2. **不處理時回傳 `null`**，讓 composite resolver 繼續往下走（現行 fallback 委派給
   `StandardResolver`，那會讓所有 contractless 型別失敗，正是它必須排在後面的原因）
3. 在 composite 鏈中**排在 contractless 物件處理之前**

做到之後：任何 `CollectionBase<T>` / `KeyCollectionBase<T>` 子型別自動取得正確 formatter，
**顯式註冊清單與 BEE4001 一併退場**——沒有手動步驟，就沒有「忘記」可言。

### 合併後的最終形貌

| 層 | 內容 |
|----|------|
| `Bee.Base.Collections` | `CollectionBase<T>` / `CollectionItem` / `KeyCollectionBase<T>` / `KeyCollectionItem` —— **唯一一套**，成員標註為 `[XmlIgnore, JsonIgnore, WireIgnore]` |
| `Bee.Api.Core.MessagePack` | `CollectionBaseFormatter` / `KeyCollectionBaseFormatter` / `BeeObjectFormatter` / `BeeContractlessResolver` —— 依 base type 自動解析，零手動註冊 |
| `Bee.Definition` | 對 MessagePack 零引用 |

`MessagePackKeyCollectionBase` 的 `[Key(0)] ItemsForSerialization` proxy 與
`IMessagePackSerializationCallbackReceiver` **一併消失**——`KeyCollectionBaseFormatter`
直接把 items 寫成 array、讀取時重建，不需要 proxy 屬性繞道。

> **不受影響**：BEE4005 / BEE4006（單一 `Add` 多載、無參數建構子）走
> `FrameworkCollectionTypes`，其清單已含 `Bee.Base.Collections.CollectionBase\`1` 與
> `KeyCollectionBase\`1`，合併後照常運作——它們把關的是行動端 AOT `XmlSerializer` 的型別形狀
> （見 [apple-mobile-trim.md](../../../.claude/rules/apple-mobile-trim.md)），與傳輸格式無關。

## 設計

### 元件一：`[WireIgnore]` 標註（`Bee.Base/Attributes/`）

1:1 取代生效中的 `[IgnoreMember]`。Bee 自有詞彙，只依賴 BCL，不引入任何套件相依；
語意為「不參與 Api.Core 外置 formatter 的序列化」，未來新增的外置格式沿用同一標註。

```csharp
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class WireIgnoreAttribute : Attribute { }
```

標註後的形貌為 `[XmlIgnore, JsonIgnore, WireIgnore]`，與現況的
`[XmlIgnore, JsonIgnore, IgnoreMember]` 位置對應，差別只在第三個標註不再來自套件。

不生效的 23 個 `[IgnoreMember]` 直接移除，不轉換——它們是防禦性噪音，
保留只會誤導讀者以為該路徑有作用。

**兩個被否決的替代方案**：

- *讓 `BeeObjectFormatter` 直接沿用 `[JsonIgnore]`，不新增 attribute。*
  7 個 item 基底成員確實都已標 `[JsonIgnore]`，看似可行——但這會把 MessagePack 的行為
  綁在 JSON 的標註上，日後只要出現「JSON 要露出、MessagePack 要隱藏」的成員就無解。
  `FilterNode.Kind` 正是反例：它**刻意不標** `[JsonIgnore]`（在 JSON 上它是多型判別碼），
  卻必須排除於 MessagePack。
- *以「排除 `ITagProperty` / `IObjectSerialize` / `ICollectionItem` 介面成員」的隱含規則取代標註。*
  對未來新增的排除需求沒有出口，且規則不可見於程式碼閱讀處。

### 元件二：每型別手寫 formatter（`Bee.Api.Core/MessagePack/`）

**作用面 6 個型別**，每個一支手寫 formatter，全程使用泛型多載、零反射
（理由見發現 7、8——泛型反射版在 AOT 下不可行）：

| 型別 | 入口 |
|------|------|
| `SortField` | `IGetListRequest.SortFields`（原型已完成並驗證） |
| `DepartmentNode` | `IGetDepartmentTreeResponse.Tree` |
| `NumberFormatItem` / `CashRoundingItem` / `AllowedCurrencyItem` | `CompanyInfo` 遞移 |
| `Parameter` | `ApiMessageBase.Parameters` |

（`FilterCondition` / `FilterGroup` 走元件三；`CurrencyItem` / `UnitItem` 不在合約面上。）

**範圍可能再縮小**：實測顯示 contractless **原生**只納入「public get + public set」的成員——
`SerializeState`（private setter）與 `Collection`（唯讀）本就被排除
（payload `82-A4-4E-61-6D-65-A1-61-A3-54-61-67-C0` = 僅 `Name` 與 `Tag` 兩個成員）。
**真正需要排除的只有 `Tag`，以及 `MessagePackKeyCollectionItem.Key`**（與子類代理屬性重複）。

→ 若改以型別形貌處理這兩個成員（例如顯式介面實作），手寫 formatter 可能連帶不需要。
`.Tag` 全 repo 僅 26 處使用（多數在測試），`Key` 在 wire 型別中僅 `Parameter` 代理。
**此選項待階段 1 開工前定案。**

**維護性防護**：每支 formatter 公開 `WireMemberCount` 常數，wire 測試斷言 map header
與之相符——型別新增屬性而未同步 formatter 時測試立刻紅。

### 元件三：`FilterNodeFormatter`（`Bee.Api.Core/MessagePack/`）

手寫多型判別碼。`FilterNode` / `FilterCondition` / `FilterGroup` 可移除
`[MessagePackObject]`、`[Union]`、`[Key(100..103)]`、`[IgnoreMember]`。

**收益**：
- adr-030 的唯一永久例外消失，`BEE4003`（`UnionMustUseIntegerKeys`）可退役
- `FilterNode.Kind` 在 MessagePack ignore / JSON 判別碼的語意不對稱消失，
  [FilterNode.cs:25-30](../../../src/Bee.Definition/Filters/FilterNode.cs) 的 WARNING 註解可移除

**代價**：新增欄位或第三個子類時必須同步改 formatter，漏改會**靜默丟欄位**。
以 round-trip 測試 + 屬性數量斷言把關（見「驗證策略」）。

`FilterCondition.Value` / `SecondValue` 為 `object?`，formatter 內需顯式 delegate 給
`SafeTypelessFormatter`。

> JSON 端的
> [FilterNodeCollectionJsonConverter](../../../src/Bee.Definition/Filters/FilterNodeCollectionJsonConverter.cs)
> 已經用 `Kind` 屬性自行判型——同一個多型問題，JSON 早就用 converter 解了。
> 它**留在原地不動**（`System.Text.Json` 為 BCL，不產生相依），
> 但它是 `FilterNodeFormatter` 的現成範本。
>
> 兩者並存後，`FilterNode.Kind` 的角色反而更清楚：JSON converter 讀它判型、
> MessagePack formatter 不寫它。現行 `[IgnoreMember]` 帶來的「同一屬性兩種語意」
> 需要 WARNING 註解防守，改為兩個外置 converter/formatter 各自表述後，
> 語意衝突消失在型別之外。

### 元件四：`SafeTypelessFormatter` 遷入 Api.Core

它是貨真價實的 `IMessagePackFormatter<object?>` 實作，卻住在定義層，且被 Api.Core 三處反向引用
（`MessagePackCodec`、`SafeMessagePackSerializerOptions`、`ApiPayloadConverter`）。

搬遷的卡點是 [Parameter.cs:48](../../../src/Bee.Definition/Collections/Parameter.cs) 的
`[MessagePackFormatter(typeof(SafeTypelessFormatter))]`。

但注意它**已被雙重註冊**——[MessagePackCodec.cs:37](../../../src/Bee.Api.Core/MessagePack/MessagePackCodec.cs)
的 formatter 陣列也放了 `SafeTypelessFormatter.Instance`，而 `CompositeResolver` 的 formatter 陣列
優先於 resolver 陣列。走 codec 的路徑上 `object` 成員本就會命中它。

**這不是整潔問題，是安全機制放錯層。** `Parameter.Value` 是 `object?`，是 wire 上唯一的
typeless 成員（`ParameterCollection` 掛在 `ApiMessageBase.Parameters`，每個 request/response 都帶），
payload 必須自帶型別名、讀取端據以實例化——即經典的 deserialization gadget 路徑。
`SafeTypelessFormatter` 以「前置 `ThrowIfDeserializingTypeIsDisallowed` override + 後置型別複驗」
兩層白名單把它關起來。

> **待決 A**：attribute 是否為「不經 codec 的路徑」的最後保險？
>
> **已掃描（2026-08-09）**：production code 的所有 `MessagePackSerializer.*` 呼叫都位於
> `src/Bee.Api.Core/MessagePack/` 內，且每一個不是使用 `MessagePackCodec.Options`，
> 就是轉傳收到的 `options` 參數（`DataSetFormatter` / `DataTableFormatter` /
> `CollectionBaseFormatter`）。**不存在以預設 options 序列化的 production 路徑。**
>
> 但「今天沒有呼叫者」不等於「移除 attribute 後 codec 註冊真的接得住」。這是安全邊界，
> 猜錯的代價是白名單**靜默失效**而非欄位遺失。**階段 0 必須實測**：拿掉 attribute 後，
> 以一個**不在白名單的型別**跑 round-trip，斷言仍被擋下。

搬遷後兩份重複測試（`tests/Bee.Definition.UnitTests/Serialization/SafeTypelessFormatterTests.cs`
與 `tests/Bee.Api.Core.UnitTests/SafeTypelessFormatterTests.cs`）合併為一份。

### 元件五：`KeyCollectionBaseFormatter`

四個 `MessagePack*` 集合型別**全數刪除**，子型別改繼承 `Bee.Base.Collections` 對應型別。
處置依角色分兩組：

| 組 | 型別 | 序列化路徑 | 處置 |
|----|------|-----------|------|
| **A. 容器** | `MessagePackCollectionBase<T>` | `CollectionBaseFormatter` → array，屬性從不被讀 | 3 個 `[IgnoreMember]` 不生效，**直接刪**；型別本身刪除 |
| **A. 容器** | `MessagePackKeyCollectionBase<T>` | `[MessagePackObject]` opt-in + `[Key(0)]` proxy + callback 介面 | 需新增 `KeyCollectionBaseFormatter`（見下）；型別本身刪除 |
| **B. Item** | `MessagePackCollectionItem` | contractless，屬性逐一序列化 | 3 個 `[IgnoreMember]` → `Bee.Base.CollectionItem` 上的 `[WireIgnore]` |
| **B. Item** | `MessagePackKeyCollectionItem` | contractless，屬性逐一序列化 | 4 個 `[IgnoreMember]` → `Bee.Base.KeyCollectionItem` 上的 `[WireIgnore]` |

B 組的 `Collection` 屬性**絕不可漏**——它是 item 指回容器的反向導航，
一旦上 wire 就是無限遞迴（見發現 3）。

`KeyCollectionBaseFormatter` 負責 `KeyedCollection` 的序列化：直接把 items 寫成 array、
讀取時逐一 `Add` 重建。現行以 `[Key(0)] ItemsForSerialization` proxy 加
`IMessagePackSerializationCallbackReceiver` 繞道的做法**整套消失**——
proxy 屬性存在的唯一理由就是「attribute 只能標在屬性上」，改成外置 formatter 後不再需要。

兩個 formatter 皆由 `BeeContractlessResolver` 依 base type **自動解析**，
不再需要 `MessagePackCodec` 的顯式註冊清單。

## 階段拆分

### 階段 0：可行性驗證（spike，不進 main）

三個必須先實測、不可靠推理的問題：

1. `BeeObjectFormatter` 原型能否在 reflection-only 模式下正確 round-trip
   （用 `AppContext.SetSwitch("System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported", false)`
   重現行動端 AOT 路徑，見 [apple-mobile-trim.md](../../../.claude/rules/apple-mobile-trim.md)）
2. 待決 A：拿掉 `Parameter` 的 formatter attribute 後，走 codec 的 round-trip 是否仍命中
   `SafeTypelessFormatter`
3. 發現 2 / 3 的實證確認：移除集合容器與定義型別的標註後，全套序列化測試是否仍綠
4. typeless 逃生口：以 `ExecFunc` 經 `Parameter.Value` 傳遞一個**未在註冊清單上**的集合型別，
   確認 resolver 自動解析後 round-trip 成立
5. 集合 formatter 自動解析（階段 4 前置）：`FormatterResolver` 改為遞迴 base-type 檢查、
   非集合型別回傳 `null` 讓 composite 繼續，再前移至 contractless 之前——
   確認集合型別解析結果與現行顯式註冊一致

**閘門**：五項皆通過才進階段 1。任一項失敗則回報並重新評估範圍。

> **第 5 項的驗證對象是上節 wire 合約表列出的型別**，不是 `FormSchema` / `FormLayout`——
> 後者以 XML 字串上 wire，MessagePack 序列化的是那個 `string`，
> 它們**根本不經過物件 resolver**，拿來驗等於沒驗到這條路徑。
>
> 前移之所以可行，關鍵在「非集合型別回傳 `null`」：如此一來排序改動的**實際影響面
> 僅限集合子型別**，而那些型別現行也是走 `CollectionBaseFormatter`，結果應完全相同。
> 現行 fallback 委派給 `StandardResolver`（要求 `[MessagePackObject]`）才是它必須排在後面的原因。

> **順帶修正**：[FormatterResolver.cs:29-30](../../../src/Bee.Api.Core/MessagePack/FormatterResolver.cs)
> 的 WARNING 以「`FormSchema`、`FormLayout` and friends」當作 contractless 型別的例子，
> 依上述其實**舉錯了例**——它們不走這條路。該註解在階段 4 重寫 resolver 時一併更正。

### 階段 1：`[WireIgnore]` + 手寫 formatter ✅ 已完成（2026-08-09）

**已落地**：

- `Bee.Base/Attributes/WireIgnoreAttribute.cs`——格式中立的「不上 wire」標註
- item 兩個基底的 7 個成員加上 `[WireIgnore]`（與既有 `[IgnoreMember]` 並存，
  依發現 5 尚不能移除後者）
- `Bee.Api.Core/MessagePack/` 新增 6 支手寫 formatter：`SortFieldFormatter`、
  `DepartmentNodeFormatter`、`NumberFormatItemFormatter`、`CashRoundingItemFormatter`、
  `AllowedCurrencyItemFormatter`、`ParameterFormatter`，並於 `MessagePackCodec` 顯式註冊
- 7 個定義型別移除 17 個不生效的 `[IgnoreMember]` 與 `using MessagePack;`
- `Parameter` 移除 `[MessagePackFormatter]`（待決 A 已證非必要）
- 新增 `WireFormatterTests`（10 個測試），每支 formatter 一條 `WireMemberCount` 斷言

**驗證**：clean Release build 0 error 0 warning；一般模式 `Bee.Definition` 1071 +
`Bee.Api.Core` 692 全綠。

**AOT 模擬**：完整套件失敗數由 main 的 **51 降至 40**。
殘留失敗全屬發現 9 的既有問題（`TypelessFormatter` / `DataTable` / `DataSet`），
非本階段引入——`WireFormatterTests` 10 項中僅 2 項失敗，且皆卡在
`Parameter.Value` 的 `System.Object` typeless 路徑（MessagePack 內建
`TypelessFormatter` 自身需要 Emit），不是手寫 formatter 能解決的層次。

**未採用的替代方案**：以型別形貌（顯式介面實作）取代 `[WireIgnore]`，可省下 6 支
formatter。否決理由：`ITagProperty.Tag` / `IKeyCollectionItem.Key` 是公開介面成員，
改動會破壞外部消費者；且手寫 formatter 讓 wire 合約成為程式碼中**看得見、可 review**
的東西，而「讓 contractless 自行決定納入什麼」正是先前踩到 analyzer（發現 5）
與 AOT（發現 7）兩個坑的根源。

> 盤點佐證：`.Tag` 全 repo 26 處使用中，**production 對集合項目的呼叫點為零**
> （其餘為 `TraceContext.Tag`、Avalonia `TabItem.Tag` 等同名不同物，以及測試）。
> 即便如此仍不動公開介面——外部 NuGet 消費者的使用情形不可見。

### 階段 2：`FilterNode` 家族外置 ✅ 已完成（2026-08-09）

**已落地**：`Bee.Api.Core/MessagePack/FilterNodeFormatter.cs`，
以 `Kind` 為判別碼的 map 格式處理 `FilterCondition` / `FilterGroup` 多型，
並於 `MessagePackCodec` 註冊。與 JSON 端的 `FilterNodeCollectionJsonConverter`
採同一套心智模型（皆讀 `Kind` 判型）。

設計要點：

- **判別碼寫成具名成員而非陣列首元素**——payload 自我描述，
  log 出來看得到 `Kind`，不是一個意義寫在別的檔案裡的裸整數
- **反序列化先緩衝再綁定**：判別碼不保證最先到達，故以
  `ReadOnlySequence<byte>` 暫存各成員，待 kind 確定後再綁。
  過濾樹是 request-scoped 的謂詞而非資料集，多一趟的成本可忽略
- 未知子型別擲 `MessagePackSerializationException` 並指名要更新 formatter

**測試**：`FilterNodeWireTests` 10 項——多型還原、三層巢狀樹、
`Value` 各型別（`string` / `int` / `bool` / `Guid` / `DateTime` / `decimal`）、
`Between` 的 `SecondValue`、集合元素子型別保留、null 節點。
含 `ConditionWireMemberCount` / `GroupWireMemberCount` 兩條漂移守衛。

**驗證**：clean Release build 0 error 0 warning；一般模式 1071 + 702 全綠。

> **標註尚未移除**：依發現 5，`[Union]` / `[Key(100..104)]` 必須與整個連通分量
> （含 Api.Core 的 57 個訊息型別）一起拆，故留到階段 5。
> formatter 已在 formatter 陣列中優先於 attribute 路徑，**行為上已走新路**。
> `BEE4003` 亦同——待標註實際移除後才退役。

**AOT 模擬**：10 項中 2 項失敗，皆卡在 `FilterCondition.Value` 的
`System.Object` typeless 路徑（發現 9 的既有問題）。
細節值得記錄：`string` / `int` / `bool` 經 typeless **可以**通過，
`Guid` / `DateTime` / `decimal` 才踩到 Emit——顯示問題出在
`TypelessFormatter` 對非基本型別的處理，而非 typeless 機制本身。

### 階段 3：`SafeTypelessFormatter` 遷移 ✅ 已完成（2026-08-09）

`src/Bee.Definition/Serialization/SafeTypelessFormatter.cs`
→ `src/Bee.Api.Core/MessagePack/SafeTypelessFormatter.cs`，
並由 `public` 改為 **`internal`**——它是傳輸層的安全邊界，
不該出現在框架的公開 API 表面。`Bee.Definition/Serialization/` 資料夾隨之消失。

`Parameter` 的 `[MessagePackFormatter]` 已於階段 1 移除（待決 A 證實非必要），
故本階段無反向相依殘留。

**public API 變更**：`Bee.Definition` 的 7 個 `SafeTypelessFormatter` 條目
自 `PublicAPI.Shipped.txt` 移出並於 `Unshipped.txt` 標記 `*REMOVED*`。
屬**破壞性變更**——依 [releasing.md](../../../.claude/rules/releasing.md)，
pre-stable 允許但須在 CHANGELOG 明列。

**測試合併**：原本一分為二的兩份重複測試
（`Bee.Definition.UnitTests` 測白名單、`Bee.Api.Core.UnitTests` 測 round-trip）
合併為單一 `tests/Bee.Api.Core.UnitTests/SafeTypelessFormatterTests.cs`，
白名單條目取兩者聯集（15 個允許、7 個拒絕），並保留 Definition 版獨有的
`Instance` 單例、nil payload、post-check 例外三項。

**驗證**：clean Release build 0 error 0 warning；
`Bee.Definition` 1052 + `Bee.Api.Core` 710 全綠。
本階段為純搬遷，無序列化行為變更，故未重跑 AOT 模擬。

### 階段 4：刪除四個 `MessagePack*` 集合型別

新增 `KeyCollectionBaseFormatter`，讓 `BeeContractlessResolver` 依 base type 自動解析
（遞迴檢查 + 不處理時回傳 `null`），移除 `MessagePackCodec` 的 8 筆顯式註冊。

四個 `MessagePack*` 型別全數刪除，子型別改繼承 `Bee.Base.Collections` 的對應型別
（方向為併入 `Bee.Base`——該側子型別 25 vs 1、24 vs 1）。
`IMessagePackSerializationCallbackReceiver` 與 `[Key(0)]` proxy 一併消失。

`BEE4001`（`CollectionFormatterRegistrationAnalyzer`）退役——它把關的手動註冊步驟已不存在。
涉及 analyzer 實作、`DiagnosticIds.cs:106`、`AnalyzerReleases.Unshipped.md`、對應測試，
以及 `SerializationAttributeNames` 中兩個 `MessagePack*` 型別名常數。

> 此階段動到 public API 表面（型別搬遷／移除），需處理 `PublicAPI.Unshipped.txt`，
> 且屬**破壞性變更**。依 [releasing.md](../../../.claude/rules/releasing.md)，
> pre-stable 允許但必須在 CHANGELOG 明列。

### 階段 5：移除相依 + 文件修訂

移除 `PackageReference`，並修訂：

- [adr-030](../../adr/adr-030-messagepack-name-based-keys.md)——「`[Union]` 永久例外」與
  「裸標記不可移除」兩項結論皆已不成立
- [rules/serialization.md](../../../.claude/rules/serialization.md)——「`[Union]` 多型永久維持整數
  `[Key]`」整節需重寫
- [docs/analyzer-rules.md](../../analyzer-rules.md) 雙語——`BEE4003` 退役
- [docs/dependency-map.md](../../dependency-map.md)——相依圖更新

> 需要一份**新 ADR** 記錄決策、分界線（BCL 內建格式留在定義層 / 需外部套件的傳輸格式
> 外置至 Api.Core）與取捨，並說明未來新增傳輸格式時的落地路徑。
> 依 [rules/public-docs.md](../../../.claude/rules/public-docs.md)，公開文件不得引用本 plan，
> 理由必須寫進 ADR 本身。

## 風險

| 風險 | 影響 | 緩解 |
|------|------|------|
| `BeeObjectFormatter` 在 AOT reflection-only 下行為分歧 | 行動端 wire 靜默損壞 | 階段 0 以 `IsDynamicCodeSupported=false` 實測；不使用 `Reflection.Emit` |
| 手寫 `FilterNodeFormatter` 漏欄位 | 靜默丟資料，XML/JSON 測不出來 | round-trip 測試 + 屬性數量斷言（新增屬性即紅） |
| 移除 `[MessagePackObject]` 影響 source-gen 退路 | 行動端若被逼上 source generator 需重新標註 | adr-030 保留標記的理由是「免費保險」；本計畫是有意識地放棄它，需在新 ADR 明列。實測已證 MessagePack 3.x 的 reflection fallback 在行動端可用（見 [rules/serialization.md](../../../.claude/rules/serialization.md)） |
| 破壞性 wire 變更 | 外部消費者 | adr-030 已載明「目前無外部實際消費者」；需在動工前重新確認此前提仍成立 |
| 階段 4 的型別合併牽動 public API | 下游編譯錯誤 | 獨立階段、獨立 review；`PublicAPI` analyzer 會擋未申報變更 |
| 階段 1–3 後 wire 型別由 attributed 轉為 contractless | 序列化行為改變 | 6 個需排除成員的型別走顯式 `BeeObjectFormatter`；其餘由 contractless 以屬性名處理，與 `keyAsPropertyName` 等價。階段 0 第 1、3 項專驗 |
| 階段 4 的 resolver 前移 | 集合型別解析路徑改變 | 「非集合回傳 `null`」把影響面限縮在集合子型別；階段 0 第 5 項專驗；每階段跑完整序列化測試套件 |

## 驗證策略

每階段結束皆須通過：

1. `dotnet build Bee.Library.slnx -c Release --no-incremental`（`TreatWarningsAsErrors=true`）
2. `./test.sh` 全綠——特別是 `Bee.Definition.UnitTests` 的 201 個序列化測試與
   `Bee.Api.Core.UnitTests` 的 237 個序列化／合約測試
3. **三棲 round-trip**：每個動到的型別需驗 XML / JSON / MessagePack 皆可還原
4. **AOT 冒煙**：階段 0 與階段 5 各跑一次 reflection-only round-trip

新增測試需求：

- `FilterNodeFormatter` 的巢狀 `FilterGroup` round-trip（含 3 層以上巢狀）
- `FilterCondition.Value` 為各型別（`Guid` / `byte[]` / `DateTime` / `decimal`）時的 round-trip
- item 基底的 `Collection` 反向導航**不**上 wire 的斷言（防無限遞迴回歸）

**最終驗收**：`src/Bee.Definition/` 全域 grep 無 `MessagePack`，且 csproj 無對應
`PackageReference`。

## 待決事項

| # | 問題 | 處置 |
|---|------|------|
| A | `Parameter.Value` 的 formatter attribute 是否為必要保險 | ✅ **已結案（2026-08-09 實測）：非必要**。移除 `[MessagePackFormatter]` 後，帶 `System.Version`（白名單外）的 `Parameter` 經 codec 反序列化仍被擋下；`Bee.Definition` 1071 + `Bee.Api.Core` 684 全綠。codec 的 formatter 陣列註冊已足夠 |
