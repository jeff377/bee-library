# 行動端 trim / AOT 規範（iOS / Android / Mac Catalyst）

本檔記錄**與 UI 框架無關**的行動端建置雷區：Release 模式的 trim 會砍掉反射相依，
AOT 會禁用 `Reflection.Emit`，兩者都會打斷 `XmlSerializer` 這類靠反射運作的機制。

適用對象是所有行動 / Apple 平台 head，目前為 `Bee.UI.Avalonia` 的
`net10.0-ios` / `net10.0-android`（見 `apps/Bee.Northwind/Bee.Northwind.iOS`、`.Android`）。

> 原本記於 `rules/maui.md`。`src/Bee.UI.Maui` 於 2026-07-28 移除（Avalonia 已覆蓋 iOS/Android），
> 但這套 trim / AOT 知識**不隨之失效**——它描述的是 Mono linker 與 AOT 的行為，不是 MAUI 的行為。

## Sandbox 與 IO

iOS / Mac Catalyst 的 `.app` bundle 是唯讀。任何把設定寫回 assembly 所在目錄的做法
（`FileUtilities.GetAssemblyPath()`、`AppContext.BaseDirectory`）在行動端必定失敗。

需要持久化使用者資料時改用平台提供的可寫位置：per-user 應用資料目錄、可重建的快取目錄、
或 key-value 偏好儲存。框架端的接縫是 `IEndpointStorage`——行動 head 啟動時置換為
平台對應實作，不要沿用桌面的檔案式預設。

## Apple Release-mode trim 決策樹

> **已解（2026-06-27，實測驗證）**：descriptor 內嵌 `Bee.Definition` 即解（下方 #4 為採用解法）。
> 以下保留決策樹脈絡供理解 why。

`net10.0-ios` / `net10.0-maccatalyst` 在 Release 下，Mono linker 會砍未引用的反射相依。
最常踩到的是 `System.Xml.Serialization` 的反射 fallback 被砍 → `XmlCodec.Deserialize<FormSchema>` 拋：

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

### 可行修法（依投入成本由低到高）

1. **直接用 Debug 跑**（demo / 開發階段）——不做 trim 與 AOT 限制，但包大、慢、不能 ship。
2. **`Microsoft.XmlSerializer.Generator` 預編 Sgen 組件**——build 期把反射路徑展開為靜態程式碼。
3. **補 `[DynamicallyAccessedMembers]` 註記**——最徹底但影響面大。
4. **✅ 採用解法：`ILLink.Descriptors.xml` 內嵌於函式庫**（隨 NuGet 發佈）
   - 以 `<EmbeddedResource LogicalName="ILLink.Descriptors.xml">` 內嵌於 `Bee.Definition`，
     trimmer 自動掃描此 logical name，**所有下游 trim/AOT app（含外部框架使用者）自動受益**。
   - wildcard `preserve="all"` root `Bee.Definition.*` + `Bee.Base.Collections.*`
     ——「FormSchema 子 type 過多」正是用 wildcard 一次蓋滿的理由。
   - 實測：Android emulator full-trim 無 descriptor 砍 57%、有 descriptor 保 ~98%；round-trip 皆過。
   - 檔案：`src/Bee.Definition/ILLink.Descriptors.xml`。

### 當前狀態

> ⚠️ 本節講的是 **`XmlSerializer`（定義檔）那一半**，涵蓋範圍不含 **MessagePack wire**。
> wire 路徑另有一套要求（型別一律顯式註冊 formatter），見 `rules/serialization.md`。

- **行動端 Release trim/AOT `XmlSerializer` 已驗證可過**：Android emulator full-trim round-trip PASS；
  iOS device-target AOT build 0 錯誤；iOS 模擬器與 Mac Catalyst Release（皆為真 Mono、
  皆 `IsDynamicCodeSupported=False`）round-trip PASS。唯 iOS **實機** AOT 執行期
  為低風險形式收尾（需 Apple Developer 簽章 + 實機）。
- **半 B 免實機驗證法**：進入點第一行
  `AppContext.SetSwitch("System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported", false)`
  （趕在任何 serializer 前），即可在桌面 / 模擬器重現 iOS AOT 的 reflection-only 序列化路徑。
- **iOS 編譯前置雷**：workload 鎖 Xcode 版本不符時，build 加 `-p:ValidateXcodeVersion=false`；
  device-target build 止於簽章可加 `-p:EnableCodeSigning=false` 完成 AOT build（驗證用）。

## reflection-only 重現法的保真度與例外判讀（2026-08-10 實測）

上一節的「半 B 免實機驗證法」有效，但**判讀時有兩件事非知道不可**，否則會把真缺陷判成假象
（2026-08-09 就這樣判過一次，見 `rules/serialization.md`）。

### 1. 這個開關就是 iOS SDK 自己設的，不是人造情境

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
  **凡與 `Reflection.Emit` / 動態碼有關的疑慮，Android emulator 驗不到**，
  它只能驗 trim（半 A）。別再用 Android 當這半的證據。
- 桌面重現不必改 csproj，一個命令列屬性即可，且用的是 SDK 的同一條路徑：

```bash
dotnet test <測試專案> -c Release --settings .runsettings -p:DynamicCodeSupport=false
```

### 2. 例外「種類」不可當診斷依據，pass / fail 邊界才可以

同一個失敗案例在三種 runtime 擲不同例外：

| runtime | 例外 |
|---------|------|
| CoreCLR + 開關關掉（桌面重現） | `InvalidProgramException`（有 JIT 卻被告知不可用，反射 invoke 走 interpreted thunk，而 `MessagePackWriter` 是 `ref struct`） |
| NativeAOT（真無動態碼） | `InvalidOperationException` / `NotSupportedException` / `MissingMethodException` |
| Mono（Mac Catalyst / iOS 模擬器） | `FormatterNotRegisteredException` 這類純受管的判斷與桌面一致；泛型具現類未取得樣本 |

`InvalidProgramException` **確實**是桌面重現特有的症狀——但那只表示**症狀**失真，
**不表示失敗是假的**。判別法：拿掉開關會不會過？會過而開著不過，就是真的踩到無動態碼路徑。

### 3. 要真 Apple runtime 時：Mac Catalyst 最便宜，其次 iOS 模擬器

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

> **雷：Apple app bundle 不吃增量重建。** 改動框架組件後對同一輸出樹再 build，
> bundle 內可能留著**舊的受管組件**——`dotnet build` 會回報成功，什麼警告都沒有。
> 兩種徵狀，都不會指向真因：
>
> 1. **啟動即 SIGABRT，`Main` 從未執行、一行輸出都沒有**（AOT container 與受管組件對不上）。
>    crash report 認得出來：`mono_jit_init` → `mini_init` → `mono_aot_get_method`
>    → `load_container_amodule` → `load_aot_module` → `abort`，全在受管碼之前。
> 2. **app 跑得起來，但行為是舊版的**——例如 client 仍用舊 wire 格式對新 server 說話，
>    錯誤訊息還會被框架的邊界包成含糊的「An error occurred during the data decoding process.」，
>    看起來像後端壞了。2026-08-10 實際踩到：bundle 內的 `Bee.Api.Core.dll` 是六天前的。
>
> **先驗證再查程式碼**——比對 bundle 內的組件時間戳，一秒定案：
>
> ```bash
> ls -la <sim device>/.../<App>.app/Bee.Api.Core.dll   # iOS 模擬器
> ```
>
> **正解是 `rm -rf bin obj` 重建。** 注意順序：clean 之後必須先 `dotnet build` 再
> `-t:Run`，直接 `-t:Run` 會擋在
> `The app must be built before the arguments to launch the app using mlaunch can be computed`。

### 4. 需要「真的沒有 Emit」時：用 NativeAOT，不必排實機

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

## AOT 與 Interpreter 的組合

別亂組合這些設定：

- `MtouchLink` + AOT 預設組合穩定（SDK 預設）
- `MtouchLink=None` + `UseInterpreter=true` 看起來「都解掉」，但 CoreLib AOT 版本仍會不符 → 更壞
- 全用 Interpreter（`<MtouchInterpreter>-all</MtouchInterpreter>`）理論上可行，但啟動慢、性能差

## 序列化型別的行動端相容要件

reflection-only 的 `XmlSerializer`（iOS AOT 路徑）對型別形狀比桌面嚴格：

- 集合型別**只能公開一個** public instance `Add`——多個多載會擲 `AmbiguousMatchException`。
  便利多載必須位移為擴充方法（見 `code-style.md` 的一型別一檔例外條款）。
- 集合型別**必須有無參數建構子**，否則擲 `MissingMethodException`。
- **對映為重複 `[XmlElement]` 的集合屬性必須有 public setter**（2026-08-10 新增）。
  reflection-only 路徑對這種成員是**指派**而非 `Add`，get-only 會擲
  `ArgumentException: Property set method not found`，外顯為誤導的
  「There is an error in XML document (行, 列)」。**`[XmlArray]` 的 get-only 集合不受影響**
  ——差別只在對映方式，不在集合本身。
  setter 寫成「清空後逐一 `Add` 進既有實例」而非直接換掉欄位，才不會斷開 owner 連結
  （實例：`LanguageEnum.Entries`）。

這幾點在桌面完全不會顯現，只在行動端 reflection-only 路徑爆炸。

盤點全定義層有無同型問題的做法（一次掃完，不要逐檔看）：反射列出所有
`CollectionBase<>` / `KeyCollectionBase<>` 屬性，篩出「帶 `[XmlElement]`、無 public setter、
未標 `[XmlIgnore]`」者。2026-08-10 掃描結果：全 repo 僅 `LanguageEnum.Entries` 一處，已修。

## 診斷雜訊（省得再走一次冤枉路）

- 錯誤 `There is an error in XML document (2, 2)` 看起來像 XML 內容壞掉，**實際多半是 AOT 路徑
  問題**（`2,2` 只是根節點開頭）。不要往 XML 內容查。
- 真正的 on-device `XmlSerializer` 觸發點是**行動 head 這一端**：head 是 remote JSON-RPC client，
  FormSchema 以 **XML 字串夾在 JSON wire** 傳到 client，client 端 `XmlCodec.Deserialize<T>(result.Xml)`
  才反序列化 —— **不是** Server 讀 `Define/*.xml`。
- 曾誤判為單純 trim 雷，試 `MtouchLink=None`（→ `load_aot_module` SIGABRT）與 linker.xml
  `TrimmerRootDescriptor` 皆無效，真因是多載 `Add`。

## 相關

- `rules/avalonia.md` —— Avalonia 專屬規範（版本相容性、控件雷區）
- `rules/serialization.md` —— MessagePack / DynamicExpresso 的 AOT 結論。**兩者結論相反**：
  MessagePack 的 contractless **沒有** reflection fallback，wire 型別一律要顯式註冊 formatter；
  DynamicExpresso 則自動退回直譯器、無需處理。
  （「MessagePack 也有 fallback」是 2026-08-10 被實測推翻的舊結論，別照它推導。）
- `src/Bee.Definition/ILLink.Descriptors.xml` —— 採用解法的實際檔案
