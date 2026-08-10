# 計畫：解除 Bee.Definition 對 MessagePack 的相依

**狀態：📝 擬定中（2026-08-09）**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 0 | 可行性驗證（spike）：`BeeObjectFormatter` 原型 + 五項不可推理的前提實測 | 📝 待做 |
| 1 | `[WireIgnore]` 標註 + `BeeObjectFormatter`（6 個型別顯式註冊）落地 | 📝 待做 |
| 2 | `FilterNode` 家族外置為 `FilterNodeFormatter`，移除 `[Union]` / `[Key]` | 📝 待做 |
| 3 | `SafeTypelessFormatter` 遷入 Api.Core，移除 `Parameter` 的 formatter attribute | 📝 待做 |
| 4 | 刪除四個 `MessagePack*` 集合型別，改由 resolver 自動解析 formatter；BEE4001 退役 | 📝 待做 |
| 5 | 移除 `Bee.Definition` 的 `PackageReference`，修訂 adr-030 與規則文件 | 📝 待做 |

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
[SystemApiConnector.cs:178-179](../../src/Bee.Api.Client/Connectors/SystemApiConnector.cs) 的
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
| `CurrencySettings` / `CurrencyItem`、`UnitSettings` / `UnitItem` | 兩者皆為 `DefineType` 成員（[DefineType.cs:47,51](../../src/Bee.Definition/DefineType.cs)），經 `GetDefineAsync<T>` → XML。**合約面零引用** |
| `FormSchema`、`TableSchema`、`FormLayout`、`SystemSettings`、`ClientSettings`、`DatabaseSettings`、`DbCategorySettings` | 同上，且僅帶 `[IgnoreMember]`（發現 3） |

→ 這 11 個型別的 MessagePack 標註**可直接刪除，無需任何替代機制**。

### ⚠️ 但 formatter 註冊不可一併刪：typeless 逃生口

`Parameter.Value` 是 `object?`（見元件四），`ExecFunc` 可經此通道傳遞**任意** Definition 型別。
`SafeTypelessFormatter.IsTypeAllowed` 委派給 `SysInfo.IsTypeNameAllowed`，Bee 命名空間放行。

因此「不在宣告合約面」**不等於**「runtime 不會經過 MessagePack」。兩者的容錯度不同：

- **屬性標註**：移除安全——contractless 路徑對未標註型別照樣以屬性名序列化
- **集合的 formatter 解析**：**必須涵蓋**——集合型別若解析不到 `CollectionBaseFormatter`，
  序列化正常但**反序列化擲 `MessagePackSerializationException`**（見
  [FormatterResolver.cs:32-36](../../src/Bee.Api.Core/MessagePack/FormatterResolver.cs) 的 WARNING）

`CurrencySettings` / `UnitSettings` 本身即集合型別，即使不在宣告合約面上，
仍必須解析得到 formatter。

**階段 4 的 resolver 自動解析正是為此**（見「附帶收益」）：改為依 base type 遞迴解析後，
涵蓋面從「8 筆手動註冊」擴大到「所有集合子型別」，這個 typeless 隱患被一併消除，
而不是靠人記得維護註冊清單。

> **階段 0 加驗**：以 `ExecFunc` 經 `Parameter.Value` 傳遞一個**未在註冊清單上**的集合型別，
> 確認自動解析後 round-trip 成立。

## 四個關鍵發現（推翻既有假設）

盤點過程中證實四件事，把本計畫的難度從「幾乎不可行」降到「有明確路徑」。

### 發現 1：`ApiContractRegistry` 的 attribute 偵測是惰性的

[adr-030](../adr/adr-030-messagepack-name-based-keys.md) 寫道，8 個集合型別的裸
`[MessagePackObject]` 標記「仍為 `ApiContractRegistry.ConvertForSerialization` 的判斷依據，
**不可移除**」。

但 [ApiContractRegistry.cs:13-18](../../src/Bee.Api.Core/Registry/ApiContractRegistry.cs) 自己的
remarks 已說明：**沒有任何 production code 呼叫 `Register`，映射表恆為空，轉換路徑完全惰性。**

因此 [ApiContractRegistry.cs:50](../../src/Bee.Api.Core/Registry/ApiContractRegistry.cs) 的
`GetCustomAttribute<MessagePackObjectAttribute>()` 只是一個短路；移除 attribute 後，
流程會落到下方的介面迴圈，因映射表為空而原樣回傳——**行為完全相同**。

→ adr-030 的「不可移除」結論**需要修訂**。這是階段 5 的工作項。

### 發現 2：集合容器的標註對序列化不生效

8 個 `MessagePackCollectionBase<T>` 子型別全部在
[MessagePackCodec.cs:29-36](../../src/Bee.Api.Core/MessagePack/MessagePackCodec.cs) 顯式註冊了
`CollectionBaseFormatter`，而該 formatter 把集合序列化為 **array**，只寫 elements、
完全不讀屬性。

→ 這 8 個型別的裸 `[MessagePackObject]` 與集合基底的 13 個 `[IgnoreMember]`，
在實際 wire 路徑上**不生效**。移除它們是零行為變更。

> 但**顯式註冊本身不可少**——`FormatterResolver` 的自動 fallback 因排在
> `ContractlessStandardResolver` 之後而不可達（見
> [FormatterResolver.cs:13-37](../../src/Bee.Api.Core/MessagePack/FormatterResolver.cs) 的 WARNING）。
> `BEE4001` 在編譯期把關遺漏。

### 發現 3：真正生效的 `[IgnoreMember]` 只有 8 個

31 個 `[IgnoreMember]` 中：

| 群組 | 數量 | 是否生效 | 原因 |
|------|------|---------|------|
| 容器基底 `MessagePackCollectionBase` | 3 | ❌ | 走 `CollectionBaseFormatter`，序列化為 array，屬性從不被讀（發現 2） |
| 容器基底 `MessagePackKeyCollectionBase` | 3 | ❌ | 唯一子型別 `ParameterCollection` 採 `[MessagePackObject]` **opt-in** + `[Key(0)]` proxy，未標 `[Key]` 的成員預設即排除 |
| 定義型別（`FormSchema` / `TableSchema` / `FormLayout` / `SystemSettings` / `ClientSettings` / `DatabaseSettings` / `DbCategorySettings`） | 17 | ❌ | 這些型別以 **XML 字串**上 wire（見 [IGetFormSchemaResponse.cs](../../src/Bee.Api.Contracts/System/IGetFormSchemaResponse.cs) 的 remarks），從不經 MessagePack |
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

[MessagePackCollectionBase.cs:16-20](../../src/Bee.Definition/Collections/MessagePackCollectionBase.cs)
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
（[KeyCollectionBase.cs:161](../../src/Bee.Base/Collections/KeyCollectionBase.cs)）
在 `MessagePackKeyCollectionBase` 中**不存在**。靠註解而非編譯期把關的維護稅，已經在付。

**四對全部合併，四個 `MessagePack*` 型別全數刪除**，序列化細節一律由 Api.Core 的 formatter
處理——與 `FilterNodeFormatter`、`SafeTypelessFormatter` 的處置方式一致。

### 前置條件：修好 resolver，讓顯式註冊變成不必要

現行架構要求**每個**集合型別在 `MessagePackCodec` 顯式註冊 `CollectionBaseFormatter`，
漏了就在反序列化時擲 `MessagePackSerializationException`。BEE4001 這條規則存在的唯一理由，
就是替這個手動步驟把關。

但 [FormatterResolver.cs](../../src/Bee.Api.Core/MessagePack/FormatterResolver.cs) **本來就有**
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

> **副作用（正向）**：`Bee.Base.KeyCollectionBase<T>` 的 25 個子型別（`FormFieldCollection`、
> `DbFieldCollection` 等定義集合）原本不在註冊清單上，若經 typeless 逃生口上 wire 會擲例外；
> 自動解析後它們**恰好也被涵蓋**，這個長期隱患順帶消失。

> **不受影響**：BEE4005 / BEE4006（單一 `Add` 多載、無參數建構子）走
> `FrameworkCollectionTypes`，其清單已含 `Bee.Base.Collections.CollectionBase\`1` 與
> `KeyCollectionBase\`1`，合併後照常運作——它們把關的是行動端 AOT `XmlSerializer` 的型別形狀
> （見 [apple-mobile-trim.md](../../.claude/rules/apple-mobile-trim.md)），與傳輸格式無關。

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

### 元件二：`BeeObjectFormatter<T>`（`Bee.Api.Core/MessagePack/`）

以屬性名為鍵序列化（行為對齊 contractless），額外遵守 `[WireIgnore]`。

**作用面只有 6 個型別**，不是全域機制。需要 `[WireIgnore]` 生效的是繼承 item 基底、
且在合約面上的型別：

| 型別 | 入口 |
|------|------|
| `SortField` | `IGetListRequest.SortFields` |
| `DepartmentNode` | `IGetDepartmentTreeResponse.Tree` |
| `NumberFormatItem` / `CashRoundingItem` / `AllowedCurrencyItem` | `CompanyInfo` 遞移 |
| `Parameter` | `ApiMessageBase.Parameters` |

（`FilterCondition` / `FilterGroup` 同為 item 子型別，但走元件三的 `FilterNodeFormatter`，
不需要本元件；`CurrencyItem` / `UnitItem` 不在合約面上，無需處理。）

**註冊方式：`MessagePackCodec.Options` 的 formatter 陣列顯式列出這 6 個**——
沿用既有 `CollectionBaseFormatter` 的註冊模式，formatter 陣列優先於 resolver 鏈，
因此**完全不需要改動 `ContractlessStandardResolver` 的位置**，其餘所有型別行為不變。

實作要點：

- reflection 取得 public 可讀寫實例屬性，過濾 `[WireIgnore]`
- `PropertyInfo[]` 需 cache（`ConcurrentDictionary<Type, ...>`）
- **不得用 `Reflection.Emit`**——行動端 AOT 為 reflection-only 路徑
  （見 [apple-mobile-trim.md](../../.claude/rules/apple-mobile-trim.md)）
- 巢狀型別遞迴時 delegate 回 `options.Resolver`

> **這是刻意收窄的結果。** 前一版設計是「寫一個 `BeeContractlessResolver` 取代
> `ContractlessStandardResolver`」——那會改變**所有**型別的解析路徑，風險遠大於需求。
> 依「MessagePack 只要管 API 合約能正常序列化」的判準，6 個顯式註冊即足夠。

### 元件三：`FilterNodeFormatter`（`Bee.Api.Core/MessagePack/`）

手寫多型判別碼。`FilterNode` / `FilterCondition` / `FilterGroup` 可移除
`[MessagePackObject]`、`[Union]`、`[Key(100..103)]`、`[IgnoreMember]`。

**收益**：
- adr-030 的唯一永久例外消失，`BEE4003`（`UnionMustUseIntegerKeys`）可退役
- `FilterNode.Kind` 在 MessagePack ignore / JSON 判別碼的語意不對稱消失，
  [FilterNode.cs:25-30](../../src/Bee.Definition/Filters/FilterNode.cs) 的 WARNING 註解可移除

**代價**：新增欄位或第三個子類時必須同步改 formatter，漏改會**靜默丟欄位**。
以 round-trip 測試 + 屬性數量斷言把關（見「驗證策略」）。

`FilterCondition.Value` / `SecondValue` 為 `object?`，formatter 內需顯式 delegate 給
`SafeTypelessFormatter`。

> JSON 端的
> [FilterNodeCollectionJsonConverter](../../src/Bee.Definition/Filters/FilterNodeCollectionJsonConverter.cs)
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

搬遷的卡點是 [Parameter.cs:48](../../src/Bee.Definition/Collections/Parameter.cs) 的
`[MessagePackFormatter(typeof(SafeTypelessFormatter))]`。

但注意它**已被雙重註冊**——[MessagePackCodec.cs:37](../../src/Bee.Api.Core/MessagePack/MessagePackCodec.cs)
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
   重現行動端 AOT 路徑，見 [apple-mobile-trim.md](../../.claude/rules/apple-mobile-trim.md)）
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

> **順帶修正**：[FormatterResolver.cs:29-30](../../src/Bee.Api.Core/MessagePack/FormatterResolver.cs)
> 的 WARNING 以「`FormSchema`、`FormLayout` and friends」當作 contractless 型別的例子，
> 依上述其實**舉錯了例**——它們不走這條路。該註解在階段 4 重寫 resolver 時一併更正。

### 階段 1：`[WireIgnore]` + `BeeObjectFormatter` 落地

新增 attribute 與 formatter，於 `MessagePackCodec` 顯式註冊 6 個型別，
把生效中的 `[IgnoreMember]` 換掉，移除不生效的。
**不動 resolver 鏈**。此階段結束時 `Bee.Definition` 仍引用 MessagePack。

### 階段 2：`FilterNode` 家族外置

新增 `FilterNodeFormatter`，移除家族的所有 MessagePack attribute。

`BEE4003`（`UnionKeyStrategyAnalyzer`）退役涉及四處：analyzer 實作、
`src/Bee.Analyzers/DiagnosticIds.cs:117`、`src/Bee.Analyzers/AnalyzerReleases.Unshipped.md:27`、
`tests/Bee.Analyzers.UnitTests/Serialization/UnionKeyStrategyAnalyzerTests.cs`。

### 階段 3：`SafeTypelessFormatter` 遷移

搬檔、移除 `Parameter` 的 attribute、合併重複測試。

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
> 且屬**破壞性變更**。依 [releasing.md](../../.claude/rules/releasing.md)，
> pre-stable 允許但必須在 CHANGELOG 明列。

### 階段 5：移除相依 + 文件修訂

移除 `PackageReference`，並修訂：

- [adr-030](../adr/adr-030-messagepack-name-based-keys.md)——「`[Union]` 永久例外」與
  「裸標記不可移除」兩項結論皆已不成立
- [rules/serialization.md](../../.claude/rules/serialization.md)——「`[Union]` 多型永久維持整數
  `[Key]`」整節需重寫
- [docs/analyzer-rules.md](../analyzer-rules.md) 雙語——`BEE4003` 退役
- [docs/dependency-map.md](../dependency-map.md)——相依圖更新

> 需要一份**新 ADR** 記錄決策、分界線（BCL 內建格式留在定義層 / 需外部套件的傳輸格式
> 外置至 Api.Core）與取捨，並說明未來新增傳輸格式時的落地路徑。
> 依 [rules/public-docs.md](../../.claude/rules/public-docs.md)，公開文件不得引用本 plan，
> 理由必須寫進 ADR 本身。

## 風險

| 風險 | 影響 | 緩解 |
|------|------|------|
| `BeeObjectFormatter` 在 AOT reflection-only 下行為分歧 | 行動端 wire 靜默損壞 | 階段 0 以 `IsDynamicCodeSupported=false` 實測；不使用 `Reflection.Emit` |
| 手寫 `FilterNodeFormatter` 漏欄位 | 靜默丟資料，XML/JSON 測不出來 | round-trip 測試 + 屬性數量斷言（新增屬性即紅） |
| 移除 `[MessagePackObject]` 影響 source-gen 退路 | 行動端若被逼上 source generator 需重新標註 | adr-030 保留標記的理由是「免費保險」；本計畫是有意識地放棄它，需在新 ADR 明列。實測已證 MessagePack 3.x 的 reflection fallback 在行動端可用（見 [rules/serialization.md](../../.claude/rules/serialization.md)） |
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
| A | `Parameter.Value` 的 formatter attribute 是否為必要保險 | 階段 0 實測決定 |
