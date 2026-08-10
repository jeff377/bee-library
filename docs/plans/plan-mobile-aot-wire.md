# 計畫：修復行動端（iOS）AOT 下的 MessagePack wire 路徑

**狀態：✅ 已完成（2026-08-10）**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 0 | 回歸閘門：把 `DynamicCodeSupport=false` 納入測試流程，先讓缺陷可見且不再擴大 | ✅ 已完成（2026-08-10） |
| 1 | typeless 通道改為封閉型別集的手寫 formatter（修既有缺陷） | ✅ 已完成（2026-08-10） |
| 2 | `DataTable` / `DataSet` 的內部 DTO 補手寫 formatter | ✅ 已完成（2026-08-10） |
| 3 | wire 合約型別全面脫離 contractless（修 adr-036 引入的放大） | ✅ 已完成（2026-08-10） |
| 4 | 真 Mono-iOS 端到端驗證，收尾並更新結論 | ✅ 已完成（2026-08-10） |

## 執行結果（2026-08-10）

| 量測 | 修復前 | 修復後 |
|------|-------|-------|
| `-p:DynamicCodeSupport=false` 下 `Bee.Api.Core.UnitTests` | 186 失敗 | **0 失敗**（1 略過，見下） |
| NativeAOT probe（真無動態碼） | 11 失敗 | **全數通過** |
| 一般（JIT）全套測試 16 個專案 | 綠 | 綠 |

### 階段 4：五個環境的實測矩陣

| 環境 | 性質 | 結果 |
|------|------|------|
| CoreCLR + `DynamicCodeSupport=false` | 桌面閘門（受管層面同 iOS） | 0 失敗 / 718 |
| NativeAOT（osx-arm64） | 真無動態碼，泛型具現最嚴格 | 18 條全過 |
| **Mac Catalyst Release** | **Mono + Apple SDK** | **18 條全過** |
| **iOS 模擬器 Release** | **真 iOS runtime** | **18 條全過** |
| iOS 裝置 `ios-arm64` full-AOT build | 全閉包的 AOT 編譯 | 建置成功、0 錯誤 |

兩個 Apple runtime 都回報 `IsDynamicCodeSupported = False`；對照組（未註冊 POCO 走
contractless）在兩者上皆如預期失敗，證明閘門在 Mono 上同樣有辨識力。
`LanguageEnum.Entries` 的 XML 修正也在真 iOS runtime 上驗證通過。

**唯一未驗證的是 iOS 實機的執行期**（需 Apple Developer 簽章與實機）。實機相對模擬器的
差異只在「Mono 完全沒有 JIT」，而該面向已由 NativeAOT 涵蓋，故列為低風險的形式缺口。

略過的那一條是 `[DynamicCodeFact]`：`Parameter.Value` 帶**未註冊型別**時走具名型別分支，
需非泛型多載，在無動態碼的 runtime 上本來就不可用——那是
`SysInfo.AllowedTypeNamespaces` 這個可設定擴充點的固有限制，不是缺陷。

實作決策記於 [../adr/adr-037-wire-explicit-registration.md](../adr/adr-037-wire-explicit-registration.md)。
**wire 格式有破壞性變更**（`object` 值的封套），發版時須在 CHANGELOG 明列。

### 順帶修掉的 XML 半邊缺陷

階段 3 的閘門把一個**與 MessagePack 無關**的既有 iOS 缺陷逼了出來：
`LanguageEnum.Entries` 是對映為重複 `[XmlElement]` 的 get-only 集合，而 reflection-only
的 `XmlSerializer`（iOS 路徑）對這種成員是**指派**而非 `Add`，於是擲
`ArgumentException: Property set method not found`，外顯為誤導的
「There is an error in XML document (9, 20)」。補上 setter 即解。
全定義層掃描確認同型問題**僅此一處**（`[XmlArray]` 的 get-only 集合不受影響）。

> 本計畫源於 2026-08-10 的調查。調查本身已完成，結論已寫入
> [../adr/adr-036-wire-serialization-externalized.md](../adr/adr-036-wire-serialization-externalized.md)、
> [../../.claude/rules/serialization.md](../../.claude/rules/serialization.md) 與
> [../../.claude/rules/apple-mobile-trim.md](../../.claude/rules/apple-mobile-trim.md)。
> 本檔只處理「怎麼修」。

## 一句話（動工當下的問題陳述）

**iOS head 的 MessagePack wire 是壞的**——幾乎每個 payload 型別都擲
`FormatterNotRegisteredException`。這不是模擬假象，且 adr-036 把它放大了約 5 倍。

## 調查結論（本計畫的前提）

### 1. 「模擬」就是 iOS 建置的預設值，不是人造情境

`Microsoft.iOS.Sdk` 的 `Xamarin.Shared.Sdk.targets` 有這一條：

```xml
<DynamicCodeSupport Condition="'$(DynamicCodeSupport)' == ''
    And ('$(MtouchInterpreter)' == '' And '$(UseInterpreter)' != 'true')
    And ('$(_PlatformName)' == 'iOS' Or '$(_PlatformName)' == 'tvOS'
         Or '$(_PlatformName)' == 'MacCatalyst')">false</DynamicCodeSupport>
```

而 `Microsoft.NET.Sdk.targets` 把 `DynamicCodeSupport` 直接映射成
`RuntimeHostConfigurationOption`：`System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported`。

**亦即：先前用來「模擬」的那個開關，就是 iOS SDK 自己設的那一個**，
且對 iOS / tvOS / MacCatalyst 的**每一種組態**（Debug 與 Release、裝置與模擬器）
都成立，除非顯式啟用直譯器。

**Android 沒有這一條**——Android 保有 JIT，`IsDynamicCodeSupported` 維持 `true`，
完全不受影響。**用 Android emulator 驗這件事驗不到任何東西。**

### 2. 真無動態碼 runtime 實測：失敗是真的

以 NativeAOT（`IsDynamicCodeSupported=False` 且真的沒有 `Reflection.Emit`）跑同一組
round-trip，得到**與模擬同一組**的失敗。故「失敗」為真。

### 3. `InvalidProgramException` 確實是模擬特有的症狀

同一個案例在三種 runtime 擲不同例外：

| runtime | 例外 |
|---------|------|
| CoreCLR + 開關關掉（模擬） | `InvalidProgramException`（有 JIT 卻被告知不可用，反射 invoke 走 interpreted thunk，而 `MessagePackWriter` 是 `ref struct`） |
| NativeAOT（真無動態碼） | `InvalidOperationException` / `NotSupportedException` / `MissingMethodException` |

**例外種類不可當診斷依據，pass / fail 邊界才是。** 先前「擲 `InvalidProgramException`
高度懷疑是模擬假象」的推論——症狀判對了，結論判錯了：症狀是假象，失敗本身不是。

### 4. 決定性對照實驗：reflection fallback 只涵蓋「有標註的合約型別」

在 NativeAOT 下，只用 MessagePack 自己的 resolver（不涉任何 Bee formatter）：

| 案例 | 結果 |
|------|------|
| `[MessagePackObject(keyAsPropertyName: true)]` 型別 + `StandardResolver` | ✅ PASS |
| 無標註 POCO + `ContractlessStandardResolver` | ❌ `FormatterNotRegisteredException` |

**MessagePack 3.x 的 reflection fallback 只對有標註的合約型別成立；contractless 沒有 fallback。**

adr-030 階段 0 的原始實測（整數 key 與 `keyAsPropertyName` 皆可 round-trip）**沒有錯**
——那兩種都是**有標註**的型別。錯在後續把它一般化成「MessagePack 在 AOT 可用」，
而 adr-036 正是踩在這個一般化上，把全部型別搬到 contractless。

### 5. 量化：adr-036 放大了缺陷，而非縮小

同一測試專案、同一開關，以「失敗訊息含 `MessagePack`」為計數口徑
（此口徑不受工作區 DB 環境差異干擾）：

| 版本 | MessagePack 相關失敗 |
|------|--------------------|
| adr-036 之前（`9e3fceae`，v4.18.0） | **37** |
| adr-036 之後（`836b4468`，v4.19.0） | **185** |

轉折點在階段 4（移除 `[MessagePackObject]` 標註）：`FormatterNotRegisteredException`
由 2 筆暴增至 170 筆。adr-036「本決策的手寫 formatter 將其降至更低」的敘述與實測相反。

> 先前記載的 51 / 694 未能重現；口徑不明，不再沿用。

### 6. 剩餘的 37 筆是既有缺陷，集中在兩處

- **typeless 通道**：`Parameter.Value` / `FilterCondition.Value` 這類 `object` 成員。
  `String` / `Int32` / `Boolean` / `Int64` / `Double` 可過（`TypelessFormatter` 有
  primitive 快路徑），**`Decimal` / `Guid` / `DateTime` / `DateOnly` / `Byte[]` 不可過**
  ——它們落到 `MessagePackSerializer.NonGeneric`，該路徑需要 `Reflection.Emit`
  才能對 `ref struct` writer 建委派。
- **`DataTable` / `DataSet`**：內部 DTO（`SerializableDataTable` / `SerializableDataSet`）
  與其逐格 `object` 值。

## 影響面

| head | 是否受影響 |
|------|-----------|
| `apps/Bee.Northwind/Bee.Northwind.iOS`（`net10.0-ios`） | ❌ **wire 不通** |
| `apps/Bee.Northwind/Bee.Northwind.Android`（`net10.0-android`） | ✅ 不受影響（有 JIT） |
| 桌面 / 伺服器 / 測試 | ✅ 不受影響 |

框架以 NuGet 發佈，故**任何外部使用者的 iOS / MacCatalyst head 同樣受影響**。

## 修復選項與取捨

| 選項 | 內容 | 取捨 |
|------|------|------|
| **A. head 啟用直譯器** | iOS head 設 `MtouchInterpreter` / `UseInterpreter=true`，`DynamicCodeSupport` 即維持 `true` | 最快，但只治 head 不治框架；外部使用者仍要自己踩一次；且啟動與執行效能有代價；`rules/apple-mobile-trim.md` 記載過 `UseInterpreter` 單獨使用曾 SIGABRT，需重驗 |
| **B. 手寫 formatter 覆蓋全部 wire 型別** | 延續 adr-036 既定方向，把 contractless 的部分補上 | 與現行架構一致、定義層零污染；工作量大，且需要一道防漂移閘門 |
| **C. 退回 `[MessagePackObject]` 標註** | 讓 reflection fallback 重新生效 | 直接牴觸 adr-036 的核心決策（定義層不得帶傳輸格式標註）；否決 |
| **D. 導入 MessagePack source generator** | 以編譯期產生的 formatter 取代 Emit | 產生器需要型別帶標註，對定義層型別同樣不可行；否決 |

**採 B 為框架解，A 作為 iOS head 的過渡緩解（需先重驗 SIGABRT 是否仍成立）。**
B 順帶把 typeless 那半也一併處理掉——見階段 1。

## 階段

### 階段 0：回歸閘門

先讓缺陷可見，避免後續改動繼續擴大它。

重現只需一個命令列屬性，**不需改任何 csproj**：

```bash
dotnet test tests/Bee.Api.Core.UnitTests/Bee.Api.Core.UnitTests.csproj -c Release --settings .runsettings -p:DynamicCodeSupport=false
```

- 建立一份「目前已知失敗」的基線清單，納入測試流程；新增失敗即視為回歸。
- 待階段 1–3 逐步清空後，閘門改為「零失敗」。
- CI 以獨立 job 執行，不影響主流程時間。

### 階段 1：typeless 通道改為封閉型別集

`SafeTypelessFormatter` 已經維護一份 `AllowedPrimitiveTypes` 白名單——**通道本來就是封閉的**。
以該白名單為準寫一支判別式 formatter：寫入時以型別碼 + 泛型多載逐一具名，
讀取時依型別碼分派，全程不碰 `MessagePackSerializer.NonGeneric`。

副作用（正面）：`TypelessFormatter` 是以型別名還原任意型別的機制，本來就是
反序列化攻擊面；改為封閉集合後，`SafeMessagePackSerializerOptions` 的
兩層防護有一層可以退場。

**wire 格式相容性是本階段的主要風險**，須先決定：新格式是否與現行 typeless 位元相容。
不相容則屬破壞性變更，需 CHANGELOG 明列並確認無混版部署。

### 階段 2：`DataTable` / `DataSet`

`SerializableDataTable` / `SerializableDataRow` / `SerializableDataColumn` /
`SerializableDataRelation` / `SerializableDataSet` 補手寫 formatter；
逐格值走階段 1 的封閉型別集。

### 階段 3：wire 合約型別脫離 contractless

`Bee.Api.Core.Messages.*` 與其承載的定義層型別全面補 formatter。
數量大，需要先盤點：以階段 0 的失敗清單反推型別集合，而非人工列舉。

同時要處理 adr-036 已記載的漂移守衛問題——`WireMemberCount` 斷言必須隨每支新
formatter 一起加，否則型別新增屬性時不會有任何東西擋下。
盤點時一併評估「能否以 analyzer 規則取代人工斷言」（`BEE40xx` 系列的既有做法）。

### 階段 4：真 Mono-iOS 驗證

前三階段都在 CoreCLR（模擬）與 NativeAOT（真無動態碼）上驗證。
兩者都不是 Mono full-AOT，而 **NativeAOT 對執行期泛型具現比 Mono 嚴格**
（實測有 `MakeGenericMethod` 相關的失敗只在 NativeAOT 出現）。
故需一次真 Apple runtime 的端到端確認。

**執行結果**：Mac Catalyst Release 與 iOS 模擬器 Release 皆 18 條全過，
iOS 裝置 `ios-arm64` full-AOT build 成功。矩陣見本檔開頭。

作法（探針專案不入版控，重跑時照此重建）：把 round-trip 探針編成
`net10.0-maccatalyst` / `net10.0-ios` 的最小 app，前者直接執行 bundle 內的可執行檔，
後者以 `dotnet build -t:Run -p:_DeviceName=:v2:udid=<sim udid>` 送上模擬器讀 os_log。
兩者都需要 `-p:ValidateXcodeVersion=false`（workload 鎖的 Xcode 版本與本機不符），
裝置 target 另加 `-p:EnableCodeSigning=false` 以在無簽章下完成 AOT 編譯。

> **踩到的雷**：改動組件閉包後對同一輸出樹增量重建，app bundle 內的 AOT container
> 會與受管組件對不上，啟動即 `load_aot_module` → `abort()`，**且 `Main` 從未執行、
> 一行輸出都沒有**，看起來像程式碼炸掉。`rm -rf bin obj` 重建即正常。
> 已收進 `rules/apple-mobile-trim.md`。

## 驗收判準

1. `-p:DynamicCodeSupport=false` 下 `Bee.Api.Core.UnitTests` 零失敗。
2. NativeAOT probe 全數 PASS（含 `Parameter.Value` 的 `Guid` / `DateTime` /
   `Decimal` / `DateOnly` / `Byte[]`、`DataTable`、`DataSet`）。
3. 一次真 Apple runtime 的端到端 round-trip。
4. 桌面 / 伺服器行為與 wire 位元格式不變（或變更已在 CHANGELOG 明列）。

## 風險

| 風險 | 影響 | 緩解 |
|------|------|------|
| 階段 1 改變 typeless wire 位元格式 | 新舊版 client / server 不相容 | 先決定是否維持位元相容；不相容則列為破壞性變更 |
| 階段 3 型別數量大，人工盤點漏型別 | 修完仍有型別在 iOS 上壞掉 | 以階段 0 的失敗清單驅動，不靠人工列舉 |
| 手寫 formatter 與型別漂移 | 新增屬性靜默不上 wire | `WireMemberCount` 斷言；評估 analyzer 化 |
| Mono full-AOT 與 NativeAOT 行為不同 | 桌面全綠但 iOS 仍壞 | 階段 4 不可省略 |
