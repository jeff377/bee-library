# ADR-036：傳輸序列化外置至 API 層，定義層不再承載 MessagePack

## 狀態

**已採納（Accepted，2026-08-09）** —— 決策已執行。

本 ADR 修訂 [ADR-030](adr-030-messagepack-name-based-keys.md) 的兩項結論
（見下方「對 ADR-030 的修訂」），但不改變 [ADR-004](adr-004-messagepack-payload.md)
「MessagePack 作為 API payload 格式」的決策本身。

## 背景

`Bee.Definition` 是定義層：`FormSchema`、`TableSchema`、`FormLayout` 等結構的宿主，
其消費者包含 `Bee.Db`（含 5 個 provider 的 `FormCommandBuilder`）、`Bee.Repository`、
`Bee.Business`、`Bee.UI.Avalonia`、`tools/Bee.Cli`、`tools/DefineEditor`。

在此決策之前，該套件對 `MessagePack` 有 `PackageReference`，37 個原始檔帶有
`[MessagePackObject]` / `[Key]` / `[Union]` / `[IgnoreMember]` 標註。
上述消費者沒有一個需要 MessagePack，卻全部經相依鏈被迫拉進來。

更根本的問題是**定義層與傳輸格式的技術選擇綁死**：日後若改用其他傳輸格式，
必須回頭修改定義型別本身。

## 決策

**傳輸序列化的一切知識外置至 `Bee.Api.Core`；定義層對傳輸格式零認知。**

### 分界線：BCL 內建的留下，需要外部套件的外置

判準不是「是不是傳輸格式」，而是**「會不會讓定義層長出外部套件相依」**：

| 格式 | 角色 | 需外部套件 | 處置 |
|------|------|-----------|------|
| **XML** | 定義層自己的持久化——定義檔、存檔、快照 | ❌ BCL 內建 | ✅ 留在定義層 |
| **JSON** | 通用 web API 傳輸，.NET 預設支援 | ❌ BCL 內建 | ✅ 留在定義層 |
| **MessagePack** | 高效傳輸 | ✅ `PackageReference` | ❌ 外置至 `Bee.Api.Core` |

`[XmlIgnore]` / `[JsonIgnore]` 屬於**平台詞彙**——用它們不引入任何相依，
也不構成對特定第三方格式的綁定。MessagePack 不同：它是明確的技術選擇，
且會沿相依鏈傳染。

### 機制：每型別手寫 formatter

wire 綁定改由 `src/Bee.Api.Core/MessagePack/` 的手寫 formatter 承擔，
定義型別本身不帶任何傳輸標註：

| Formatter | 對象 |
|-----------|------|
| `SortFieldFormatter`、`DepartmentNodeFormatter`、`NumberFormatItemFormatter`、`CashRoundingItemFormatter`、`AllowedCurrencyItemFormatter`、`ParameterFormatter` | 需排除框架管理成員的合約型別 |
| `FilterNodeFormatter` | `FilterNode` 多型階層（以 `Kind` 為判別碼） |
| `KeyCollectionBaseFormatter` | `KeyedCollection` 子型別 |
| `CollectionBaseFormatter`、`DataSetFormatter`、`DataTableFormatter` | 既有 |

未列出的型別由 `ContractlessStandardResolver` 以屬性名為鍵處理——
與先前 `keyAsPropertyName` 的 wire 格式相同。

## 理由

### 為何手寫而非反射驅動

原設計是一支泛型反射 formatter，讀取自訂標註決定納入哪些成員。
**該設計在行動端 AOT 下不可行**：對任意屬性型別遞迴只能使用非泛型多載
`MessagePackSerializer.Serialize(Type, ref MessagePackWriter, object, options)`，
而 `MessagePackWriter` 是 `ref struct`——該路徑需 `Reflection.Emit` 產生
能傳遞 ref struct 的委派，`IsDynamicCodeSupported=false` 時擲
`NotSupportedException`。

> 這與「MessagePack 3.x 有 reflection-based fallback」的既有結論不衝突：該結論針對 MessagePack **自產**的 formatter，
> 不涵蓋「自訂 formatter 內呼叫非泛型 API」這條路徑。

手寫 formatter 的屬性型別編譯期已知，全程可用泛型多載、零反射，
桌面與裝置走同一條路。

### 手寫的代價與防護

新增屬性而未同步 formatter 會**靜默丟欄位**。每支 formatter 因此公開
`WireMemberCount` 常數，wire 測試斷言 map header 與之相符——型別與 formatter
一旦漂移，測試立刻紅。

### 附帶收益：消滅四對雙胞胎型別

`Bee.Definition.Collections` 的 `MessagePackCollectionBase` /
`MessagePackCollectionItem` / `MessagePackKeyCollectionBase` /
`MessagePackKeyCollectionItem` 是 `Bee.Base.Collections` 對應型別的**刻意複製**，
存在的唯一理由就是「`Bee.Base` 不引外部套件，無法承載 MessagePack 標註」。

標註移除後這個理由消失，四對合併回單一實作。原本靠註解要求
「Keep the two in step」的維護稅隨之消失——而該要求**已經被違反**：
`Bee.Base.KeyCollectionBase.GetOrDefault` 在 MessagePack 版中並不存在。

## 對 ADR-030 的修訂

ADR-030 的兩項結論不再成立：

| ADR-030 的結論 | 現況 |
|---------------|------|
| 「`[Union]` 型別**不得**改 `keyAsPropertyName`，新增多型階層沿用整數 `[Key]` + `[Union]`」 | **不再適用**。多型改由 `FilterNodeFormatter` 以 `Kind` 判別碼處理，`[Union]` 已移除，把關的 `BEE4003` 退役 |
| 「集合型別的裸 `[MessagePackObject]` 為 `ApiContractRegistry.ConvertForSerialization` 的判斷依據，**不可移除**」 | **判定有誤**。該類別無 production 呼叫者、映射表恆為空、轉換路徑惰性；attribute 檢查只是短路，移除後行為完全相同 |

ADR-030 的核心決策（wire 鍵以屬性名為準）**維持不變**——只是實現方式從
`[MessagePackObject(keyAsPropertyName: true)]` 改為 contractless 加顯式 formatter，
兩者 wire 格式相同。

## 後果

### 正面

- 定義層與傳輸格式的技術選擇脫鉤；換格式時 `src/Bee.Definition/` 零改動
- 六個不需要 MessagePack 的下游套件不再被迫相依
- 四對雙胞胎型別合併，維護稅消失
- wire 合約成為程式碼中**看得見、可 review** 的東西，不再是「contractless 自行決定」

### 代價

- **放棄 MessagePack source generator 退路**：source-gen 需要 `[MessagePackObject]` 標記。
  ADR-030 保留標記的理由正是這道「免費保險」，本 ADR 有意識地放棄它。
  依據是 MessagePack 3.x 的 reflection fallback 在行動端經實測可用。
- **新增 wire 型別時須手寫 formatter**（若該型別有需排除的框架管理成員）。
  無此需求者由 contractless 自動處理，不需任何動作。
- **破壞性變更**：`Bee.Definition` 移除 `SafeTypelessFormatter`、
  `Collections.MessagePack*` 四型別及其公開 API 條目。下游改用
  `Bee.Base.Collections` 的對應型別。

### 退役的 analyzer 規則

`BEE4001`（集合須註冊 formatter）、`BEE4002`（JSON 改名與 MessagePack 鍵不一致）、
`BEE4003`（union 階層須用整數 `[Key]`）、`BEE4004`（ctor 參數順序 vs `[Key]` 順序）
—— 四者的把關對象皆為已移除的標註機制。

`BEE4005` / `BEE4006`（單一 public `Add`、無參數建構子）**保留**：
它們把關的是行動端 AOT `XmlSerializer` 的型別形貌，與傳輸格式無關。
`BEE4006` 的判定改以框架集合與集合項目的基底型別為準。

## 未決事項

以 `IsDynamicCodeSupported=false` 模擬行動端 AOT 時，`Bee.Api.Core.UnitTests`
在本決策**之前**即有 51 / 694 項失敗，集中於 `TypelessFormatter`、
`DataTable`、`DataSet`。本決策的手寫 formatter 將其降至更低，但未清零。

該現象與「MessagePack 在行動端 AOT 可用」的既有結論不一致，值得獨立追查。
兩點保留：此為 JIT runtime 上的模擬，真實裝置行為未必相同；
且部分失敗擲 `InvalidProgramException` 而非 `NotSupportedException`，
那是「Emit 仍執行但產出無效 IL」的徵狀，高度懷疑是模擬本身的假象。

**本決策不處理此問題** —— 範圍是解除相依，不是修復行動端 AOT。
