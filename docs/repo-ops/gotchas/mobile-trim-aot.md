# 踩雷誌：行動端 trim / AOT（iOS / Android / Mac Catalyst）

維護者視角的按需脈絡。硬規則與判準常駐於 `.claude/rules/apple-mobile-trim.md`；
本檔收的是**推導過程、實測數據與操作配方** —— 只在實際要建置 / 驗證行動端時才需要讀。

適用對象是所有行動 / Apple 平台 head，目前為 `Bee.UI.Avalonia` 的
`net10.0-ios` / `net10.0-android`（見 `../../../apps/Bee.Northwind/Bee.Northwind.iOS`、`.Android`）。

---

## 一、trim 決策樹的推導脈絡

**已解（2026-06-27，實測驗證）**：descriptor 內嵌 `Bee.Definition` 即解
（`../../../src/Bee.Definition/ILLink.Descriptors.xml`）。以下保留脈絡供理解 why。

`net10.0-ios` / `net10.0-maccatalyst` 在 Release 下，Mono linker 會砍未引用的反射相依。
最常踩到的是 `System.Xml.Serialization` 的反射 fallback 被砍 →
`XmlCodec.Deserialize<FormSchema>` 拋：

```
Error: XmlSerializeErrorDetails, 2, 2
```

`2, 2` 是 line 2 col 2（XML 根節點開頭），看起來像 XML 壞掉，**實際上是 type metadata 被砍**。

> **實測補正**：問題有兩半——半 A（trim 砍 metadata）與半 B（AOT 禁 `Reflection.Emit`，
> XmlSerializer 改走 reflection-only path）。實測發現連 full-trim 砍 `Bee.Definition` 57%
> 都不打斷 FormSchema 反序列化，故 `2,2` **多半是半 B 而非半 A**。descriptor 主要保證
> 半 A 的完整覆蓋（多型子型別不漏）。

### 已知不可行的「修法」

| 嘗試 | 結果 |
|------|------|
| `<PublishTrimmed>false</PublishTrimmed>` | Apple SDK 強制要求 `true`，build 退件 |
| `<MtouchLink>None</MtouchLink>` 單獨 | AOT 編譯不全 → `load_aot_module mismatch` SIGABRT |
| `<UseInterpreter>true</UseInterpreter>` 單獨 | 部分 assembly 仍走 AOT，CoreLib 版號不符 SIGABRT |
| `<MtouchLink>SdkOnly</MtouchLink>` | XmlSerializer 反射仍被砍（SdkOnly 不保護 SDK 自身） |

### 曾評估但未採用的修法（依投入成本由低到高）

1. **直接用 Debug 跑**（demo / 開發階段）——不做 trim 與 AOT 限制，但包大、慢、不能 ship。
2. **`Microsoft.XmlSerializer.Generator` 預編 Sgen 組件**——build 期把反射路徑展開為靜態程式碼。
3. **補 `[DynamicallyAccessedMembers]` 註記**——最徹底但影響面大。

### 採用解法的細節

**`ILLink.Descriptors.xml` 內嵌於函式庫**（隨 NuGet 發佈）：

- 以 `<EmbeddedResource LogicalName="ILLink.Descriptors.xml">` 內嵌於 `Bee.Definition`，
  trimmer 自動掃描此 logical name，**所有下游 trim/AOT app（含外部框架使用者）自動受益**。
- wildcard `preserve="all"` root `Bee.Definition.*` + `Bee.Base.Collections.*`
  ——「FormSchema 子 type 過多」正是用 wildcard 一次蓋滿的理由。
- 實測：Android emulator full-trim 無 descriptor 砍 57%、有 descriptor 保 ~98%；round-trip 皆過。

### 驗證狀態

- **行動端 Release trim/AOT `XmlSerializer` 已驗證可過**：Android emulator full-trim
  round-trip PASS；iOS device-target AOT build 0 錯誤；iOS 模擬器與 Mac Catalyst Release
  （皆為真 Mono、皆 `IsDynamicCodeSupported=False`）round-trip PASS。
  唯 iOS **實機** AOT 執行期為低風險形式收尾（需 Apple Developer 簽章 + 實機）。

---

## 二、reflection-only 重現法的保真度（2026-08-10 實測）

「半 B 免實機驗證法」有效，但**判讀時有兩件事非知道不可**，否則會把真缺陷判成假象
（2026-08-09 就這樣判過一次，見 `serialization-and-expressions.md` 與
`.claude/rules/serialization.md`）。

### 這個開關就是 iOS SDK 自己設的，不是人造情境

`Microsoft.iOS.Sdk` 的 `Xamarin.Shared.Sdk.targets`：

```xml
<DynamicCodeSupport Condition="'$(DynamicCodeSupport)' == ''
    And ('$(MtouchInterpreter)' == '' And '$(UseInterpreter)' != 'true')
    And ('$(_PlatformName)' == 'iOS' Or '$(_PlatformName)' == 'tvOS'
         Or '$(_PlatformName)' == 'MacCatalyst')">false</DynamicCodeSupport>
```

`Microsoft.NET.Sdk.targets` 再把 `DynamicCodeSupport` 映射成
`RuntimeFeature.IsDynamicCodeSupported` 的 `RuntimeHostConfigurationOption`。

推論：

- **iOS / tvOS / MacCatalyst 的每一種組態**（Debug 與 Release、裝置與模擬器）預設都關掉動態碼，
  除非顯式啟用直譯器。「只有 Release 才要擔心」是錯的。
- **Android 沒有這一條**——保有 JIT，`IsDynamicCodeSupported` 維持 `true`。

### 例外「種類」不可當診斷依據

同一個失敗案例在三種 runtime 擲不同例外：

| runtime | 例外 |
|---------|------|
| CoreCLR + 開關關掉（桌面重現） | `InvalidProgramException`（有 JIT 卻被告知不可用，反射 invoke 走 interpreted thunk，而 `MessagePackWriter` 是 `ref struct`） |
| NativeAOT（真無動態碼） | `InvalidOperationException` / `NotSupportedException` / `MissingMethodException` |
| Mono（Mac Catalyst / iOS 模擬器） | `FormatterNotRegisteredException` 這類純受管的判斷與桌面一致；泛型具現類未取得樣本 |

`InvalidProgramException` **確實**是桌面重現特有的症狀——但那只表示**症狀**失真，
**不表示失敗是假的**。

---

## 三、操作配方

### 桌面重現（不必改 csproj）

```bash
dotnet test <測試專案> -c Release --settings .runsettings -p:DynamicCodeSupport=false
```

用的是 SDK 的同一條路徑（`DynamicCodeSupport` → `RuntimeHostConfigurationOption`），
與 iOS SDK 的做法完全相同。

### 要真 Apple runtime 時：Mac Catalyst 最便宜，其次 iOS 模擬器

兩者的 `DynamicCodeSupport` 都被 SDK 設為 `false`，跑的是 Mono——正是桌面重現與
NativeAOT 都涵蓋不到的那一格。把待測邏輯編成最小 app 即可：

```bash
# Mac Catalyst：直接執行 bundle 內的可執行檔，stdout 走 os_log
dotnet build -c Release -p:ValidateXcodeVersion=false
./bin/Release/net10.0-maccatalyst/maccatalyst-arm64/<App>.app/Contents/MacOS/<App>

# iOS 模擬器：先 build 再 -t:Run（切勿 simctl install 手動 build 的 .app）
dotnet build -c Release -f net10.0-ios -r iossimulator-arm64 -p:ValidateXcodeVersion=false
dotnet build -t:Run -c Release -f net10.0-ios -r iossimulator-arm64 \
  -p:ValidateXcodeVersion=false -p:_DeviceName=:v2:udid=<sim udid>

# iOS 裝置 target：無簽章也能完成 AOT 編譯，驗「整個閉包編得出來」
dotnet build -c Release -f net10.0-ios -r ios-arm64 \
  -p:ValidateXcodeVersion=false -p:EnableCodeSigning=false
```

專案需要 `ApplicationId`（否則 `A bundle identifier is required`）。

**iOS 編譯前置雷**：上面幾條配方帶 `-p:ValidateXcodeVersion=false`，是因為它們的目的只是
「驗整個閉包編不編得出來」，用哪一版 Xcode 無所謂。**一般的 iOS head 建置不要這樣繞** ——
對應版本的 Xcode 通常已側裝在 `/Applications/` 下，用 `DEVELOPER_DIR` 指過去才是正解
（判準見 `.claude/rules/apple-mobile-trim.md`「建 iOS 前先確認用的是哪個 Xcode」）：

```bash
DEVELOPER_DIR=/Applications/<對應版本>.app/Contents/Developer dotnet build <iOS 專案> -c Release
```

指定之後**同一輪的 `xcrun` / `simctl` 也要帶同一個 `DEVELOPER_DIR`**，否則工具鏈會分裂到兩版。

device-target build 止於簽章可加 `-p:EnableCodeSigning=false` 完成 AOT build（驗證用）。

### 雷：Apple app bundle 不吃增量重建

改動框架組件後對同一輸出樹再 build，bundle 內可能留著**舊的受管組件**——
`dotnet build` 會回報成功，什麼警告都沒有。兩種徵狀，都不會指向真因：

1. **啟動即 SIGABRT，`Main` 從未執行、一行輸出都沒有**（AOT container 與受管組件對不上）。
   crash report 認得出來：`mono_jit_init` → `mini_init` → `mono_aot_get_method`
   → `load_container_amodule` → `load_aot_module` → `abort`，全在受管碼之前。
2. **app 跑得起來，但行為是舊版的**——例如 client 仍用舊 wire 格式對新 server 說話，
   錯誤訊息還會被框架的邊界包成含糊的「An error occurred during the data decoding process.」，
   看起來像後端壞了。2026-08-10 實際踩到：bundle 內的 `Bee.Api.Core.dll` 是六天前的。

**先驗證再查程式碼**——比對 bundle 內的組件時間戳，一秒定案：

```bash
ls -la <sim device>/.../<App>.app/Bee.Api.Core.dll   # iOS 模擬器
```

**正解是 `rm -rf bin obj` 重建。** 注意順序：clean 之後必須先 `dotnet build` 再
`-t:Run`，直接 `-t:Run` 會擋在
`The app must be built before the arguments to launch the app using mlaunch can be computed`。

### 需要「真的沒有 Emit」時：用 NativeAOT，不必排實機

桌面重現的 runtime 底下仍是 JIT。要一個**真正**沒有 `Reflection.Emit` 的環境，
最便宜的是本機 NativeAOT console：

```bash
dotnet publish -c Release -r osx-arm64 -p:Aot=true -o ./aotout   # csproj 內以 $(Aot) 條件開 PublishAot
```

> `PublishAot` 要寫在 csproj 內以自訂屬性開關，**不要直接下 `-p:PublishAot=true`**——
> 命令列屬性會流進所有 `ProjectReference`，`Bee.Analyzers`（netstandard2.0）會以
> `NETSDK1207` 退件。

**但 NativeAOT ≠ Mono full-AOT**：NativeAOT 對執行期泛型具現更嚴格
（`MakeGenericMethod` / `MakeGenericType` 直接沒有原生碼），Mono 對參考型別有共享具現。
故 NativeAOT 上的失敗要分兩類看：純受管邏輯（如 resolver 依開關拒絕產生 formatter）在
Mono 上必然相同；泛型具現類的失敗則未必。

---

## 四、盤點手法

盤點全定義層有無「集合屬性型別形狀」問題的做法（一次掃完，不要逐檔看）：
反射列出所有 `CollectionBase<>` / `KeyCollectionBase<>` 屬性，篩出
「帶 `[XmlElement]`、無 public setter、未標 `[XmlIgnore]`」者。
2026-08-10 掃描結果：全 repo 僅 `LanguageEnum.Entries` 一處，已修。

型別形狀的硬性要件本身常駐於 `.claude/rules/apple-mobile-trim.md`。

---

## 五、歷史

原本記於 `.claude/rules/maui.md`。`src/Bee.UI.Maui` 於 2026-07-28 移除
（Avalonia 已覆蓋 iOS/Android），但這套 trim / AOT 知識**不隨之失效**——
它描述的是 Mono linker 與 AOT 的行為，不是 MAUI 的行為。
