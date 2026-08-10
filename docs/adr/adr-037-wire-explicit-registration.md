# ADR-037：wire 型別一律顯式註冊 formatter，`object` 值改用判別式封套

- **狀態**：已接受
- **日期**：2026-08-10
- **相關**：[ADR-030](adr-030-messagepack-name-based-keys.md)、[ADR-036](adr-036-wire-serialization-externalized.md)

## 背景

[ADR-036](adr-036-wire-serialization-externalized.md) 把 MessagePack 標註全數移除，
wire 型別改由 `ContractlessStandardResolver` 承載。該決策的依據是「MessagePack 3.x 有
reflection fallback，行動端 AOT 可用」。

**這個依據不成立。** 2026-08-10 的實測（NativeAOT 對照實驗）顯示：

| 案例 | 結果 |
|------|------|
| `[MessagePackObject(keyAsPropertyName: true)]` 型別 + `StandardResolver` | ✅ round-trip 正常 |
| 無標註 POCO + `ContractlessStandardResolver` | ❌ `FormatterNotRegisteredException` |

**contractless 沒有 reflection fallback**；MessagePack 的 fallback 只涵蓋帶標註的合約型別。
而 .NET for iOS SDK 對 iOS / tvOS / MacCatalyst 的**每一種組態**預設
`DynamicCodeSupport=false`（映射為 `RuntimeFeature.IsDynamicCodeSupported`），
因此 ADR-036 之後，iOS 端幾乎每個 payload 型別都無法序列化。

同時另有一處早於 ADR-036 的缺陷：`Parameter.Value` / `FilterCondition.Value` 這類
`object` 成員走 `TypelessFormatter`，其非基本型別路徑經
`MessagePackSerializer.NonGeneric` 傳遞 `ref struct` writer，需要 `Reflection.Emit`。
`String` / `Int32` / `Boolean` / `Int64` / `Double` 因有 primitive 快路徑而通過，
`Decimal` / `Guid` / `DateTime` / `DateOnly` / `Byte[]` 則否。

## 決策

### 1. wire 型別閉包內的每個型別都顯式註冊 formatter

不再把 contractless 當作預設承載機制。閉包涵蓋：

- 訊息合約型別（`Bee.Api.Core.Messages.*`、`Bee.Api.Contracts.*`）
- 其遞移可達的定義層型別與框架集合
- 封閉泛型具現：`List<T>` / `Dictionary<K,V>` / `T?` / 陣列 / **列舉**
  （這些同樣經 `MakeGenericType` 建立，在 AOT 上沒有原生碼）

多數型別以 `WireContract.For<T>().Member(...)` 宣告成員；`WireObjectFormatter<T>` 依該表
逐一具名讀寫。關鍵在於 `Member<TValue>` 的 `TValue` 是**編譯期**泛型參數，
序列化呼叫因而全程是封閉泛型，不觸及反射或動態碼。

contractless 仍留在 resolver 鏈末端，但定位改為**桌面端的便利退路**（例如 host 自己塞進
`Parameter.Value` 的型別），不再是框架型別的承載機制。

### 2. `object` 值改用判別式封套

`TypelessFormatter` 由 `WireValueFormatter` 取代。封套是兩元素陣列：

```
[ <判別碼:int> | <型別名:string> , <值> ]
```

- **判別碼**：框架自有的封閉型別集（`Boolean`…`DataTable`、`DBNull`、`object[]`），
  每個型別在類別初始化時建立封閉泛型的讀寫委派。
- **型別名**：`SysInfo.AllowedTypeNamespaces` 這個可設定擴充點的逃生門。
  它仍走非泛型多載，**因此仍只在有動態碼的 runtime 上可用**。

白名單語意不變（`WireTypeWhitelist`），但檢查**提前到寫入端**，且讀取端在
`Type.GetType` **之前**先篩型別名——白名單外的型別自始不會被載入。

### 3. 漂移由測試把關，不由人工常數

ADR-036 以每支 formatter 的 `WireMemberCount` 常數當守衛。改為
`WireContractDriftTests` 兩條檢查：

1. 走一次 wire 型別閉包，斷言每個型別都有顯式註冊的 formatter；
2. 逐一比對每個 `WireContract` 的成員清單與型別當下的形狀。

wire 成員的定義與 JSON 相同：public 可讀可寫、未標 `[JsonIgnore]` 的屬性
（框架管理成員如 `Tag` / `Key` / `SerializeState` 本就帶該標註）。

### 4. 回歸閘門

`dotnet test … -p:DynamicCodeSupport=false` 納入 CI。`DynamicCodeSupport` 是 .NET SDK
的標準屬性，iOS SDK 用的就是它——這一關跑的不是模擬情境，是行動端建置的實際設定。

## 後果

### 正面

- iOS 端的 wire 由「幾乎全不可用」變為可用。驗證於五個環境：`DynamicCodeSupport=false`
  閘門（0 失敗 / 718）、NativeAOT、**Mac Catalyst Release**、**iOS 模擬器 Release**
  （後兩者為真 Mono、皆回報 `IsDynamicCodeSupported = False`），
  以及 iOS 裝置 target 的 full-AOT 編譯。
- `object` 通道不再以完整組件限定名描述每個值，payload 變小，也不再於 wire 上點名 CLR 組件。
- 反序列化攻擊面縮小：框架自有值走封閉判別集合，不經型別名解析。
- 漂移守衛由人工維護的常數變成自動比對，新增屬性忘記註冊會被測試擋下。

### 代價

- **破壞性 wire 變更**：`object` 值的封套格式改變，client 與 server 必須同版升級。
- 新增 wire 型別時必須補註冊。這不是額外負擔而是把既有的隱性要求顯性化——
  漏補會被 `WireContractDriftTests` 當場擋下，而不是留到行動端才炸。
- 註冊清單體積不小。它由型別閉包機械產生，維護方式是重跑閉包而非人工增刪。

### 對 ADR-036 的修正

ADR-036 的核心決策（定義層不得相依傳輸格式套件）**維持不變**——本 ADR 沒有把
MessagePack 標註放回 `Bee.Definition`。改變的是該決策的**實作代價**：
手寫 formatter 的覆蓋範圍從「有需排除成員的型別」擴大到「全部 wire 型別」。

ADR-036「放棄 source generator 退路」那條代價的依據（reflection fallback 可用）已被推翻，
但結論仍成立：顯式註冊同樣不需要標註，且比 source generator 更可控。

## 未納入

- **iOS 實機的執行期尚未實測**（需 Apple Developer 簽章與實機）。已驗證的環境有五個：
  CoreCLR 搭配關閉的開關、NativeAOT、Mac Catalyst Release、iOS 模擬器 Release，
  以及 iOS 裝置 target 的 full-AOT 編譯。後兩者是真 Mono、皆回報
  `IsDynamicCodeSupported = False`。實機相對模擬器的唯一差異是「Mono 完全沒有 JIT」，
  而該面向已由 NativeAOT 涵蓋，故列為低風險的形式缺口。
- **具名型別逃生門在行動端仍不可用**。要讓 host 自訂型別也能上行動端的 wire，
  需要另一套「host 註冊自己的 formatter」機制，本 ADR 不處理。
