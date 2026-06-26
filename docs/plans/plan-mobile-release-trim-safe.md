# 計畫：修正 Avalonia 行動端只能跑 Debug（Release AOT / Trim 的 XML 序列化問題）

**狀態：🚧 核心完成（2026-06-27）— 半 A+B 免實機驗證通過；iOS 實機 AOT run 為形式收尾**

> 開發全在 bee-library 來源端 `apps/Bee.Northwind` + `src/Bee.Definition`；`bee-northwind-avalonia` 複本最後再 sync。

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 0 | 排除 iOS 目前「無法正常編譯」的前置 build / 工具鏈問題 | ✅ 已完成（2026-06-26，Xcode 版本鎖；Debug 模擬器跑通） |
| 1 | descriptor 內嵌 `Bee.Definition` + Android full-trim 驗半 A（trim metadata） | ✅ 已完成（2026-06-26，Android emulator 實測） |
| 2 | iOS 免實機 AOT build + 半 B（reflection-only）驗證 | ✅ 已完成（2026-06-27，免實機；實機 AOT run 為低風險形式收尾） |
| 3 | （條件）實機 fallback 不過 → 改走 Sgen 預編序列化組件 | 📝 待做 |
| 4 | （條件）行動端若改用 MessagePack wire 上線 → 補 iOS Release 驗證 | 📝 待做 |

---

## 背景

> **來源優先（單 repo 開發）**：本案在 **bee-library 內的來源端 `apps/Bee.Northwind`** 開發、迭代、驗證。`bee-northwind-avalonia` 是 graduation 的**下游複本**（目前與來源逐字一致），不在開發過程中改動——等修正落地並驗證通過，再走既有 graduation sync 一次性同步過去。因此**開發階段沒有跨 repo**：head csproj（`apps/Bee.Northwind/Bee.Northwind.iOS` / `.Android`）與型別/descriptor 修正（`src/Bee.Definition`）都在 bee-library 同一 repo 內。見記憶 `northwind-graduation-keep-source`。

`apps/Bee.Northwind` 的 Avalonia 行動 head（`Bee.Northwind.iOS` / `Bee.Northwind.Android`）目前**只能在 Debug 跑**，無法以 Release 打包上架。原因是整套定義驅動大量靠反射，序列化也走反射；iOS / Android 的 Release 會做 trim 與 AOT，把「看似沒人用」的反射路徑與 metadata 砍掉，導致序列化在執行期失敗。

實務上「上架」等同於「要能跑 Release」：

- **iOS**：Apple 政策禁 JIT，App Store 一律要求 AOT，Debug 包根本送不進去。
- **Android**：Play Store 期待 Release 包，Debug 包又大又慢，不該散布。

### 實測觀察（本次討論確認）

- 之前在 iOS 上**只遇到 XML 反序列化問題**，沒遇到 JSON / MessagePack 問題。
- 典型錯誤訊號：`XmlSerializeErrorDetails, 2, 2`。`2, 2` 是 XML 第 2 行第 2 欄（根節點開頭），**看起來像 XML 壞掉，實際上是 type metadata 被 trim 砍掉**。
- JSON wire（`System.Text.Json`）在行動端已驗證可過：DTO 扁平、反射 fallback 退化溫和。
- MessagePack wire **在 iOS 上未被測到**——因為行動 demo 當時用的是 JSON payload，MessagePack 預設的 `Reflection.Emit` 路徑從頭到尾沒被執行。「沒報錯」是「沒走到那條」，不等於 AOT 安全。

### 相關既有紀錄

- `.claude/rules/maui.md`「Apple Release-mode trim 決策樹」一節已記錄此雷與已知不可行 / 可行的修法。
- 記憶 `ios-xmlserializer-ambiguous-add`：iOS/AOT 下集合反射的兩個雷（多載 `Add` → `AmbiguousMatch`、集合缺無參數建構子 → `MissingMethod`）**已修正**，XML 格式不變、桌面 / WASM 零影響。本案要處理的是**剩下的 trim / AOT metadata 保留**，不是那兩個。

---

## 問題的兩個半

修正前要先認清這問題其實是**兩個獨立的半**，不同平台只暴露不同半：

| | 半 A：trim 砍掉型別 metadata | 半 B：AOT 不准 `Reflection.Emit` |
|---|---|---|
| 成因 | trimmer 判定「沒人用」把建構子 / 屬性砍掉 | iOS 禁 JIT，執行期不能動態生成程式碼 |
| **Android** | 會發生（**但要設定才重現**，見下） | **不發生**——Mono 有 JIT / 直譯器，emit 照跑 |
| **iOS** | 會發生 | 會發生 |
| 對應錯誤 | `2, 2` metadata 被砍 | XmlSerializer 走不到 emit 快路徑 |

**關鍵推論**：

1. **Android 的 JIT / 直譯器會把半 B 整個遮掉**。XmlSerializer 在 Android 上照用 emit 快路徑，於是 iOS 被逼著走的「反射 fallback」在 Android 根本不會被執行到。**Android 綠 ≠ iOS 綠。**
2. **Android Release 預設只 trim SDK 組件、不 trim `Bee.Definition`**，所以 `2, 2` 預設不會在 Android 重現。要重現半 A，必須明確設 `<TrimMode>full</TrimMode>`（或 `AndroidLinkMode=Full`）把 app 組件也納入裁切。iOS 預設裁得較徹底。
3. 半 A 的修正**會轉移到 iOS**（兩平台都 trim）；半 B 的驗證**只有 iOS 做得到**。

---

## 三種解法

> 詳細優劣對照見本次討論；此處留結論。

| 解法 | 解半 A | 解半 B | 多型覆蓋 | binary | 投入 |
|------|--------|--------|---------|--------|------|
| **Sgen 預編**（`Microsoft.XmlSerializer.Generator`） | ✅ | ✅（唯一） | 需 `[XmlInclude]` | 中 | 中 |
| **`[DynamicallyAccessedMembers]`（DAM）** | ✅ | ❌（靠 fallback） | **弱，易漏** | 最小 | 單低總高 |
| **TrimmerRootDescriptor（linker.xml）** | ✅ | ❌（靠 fallback） | **強（wildcard 全包）** | 較大 | 最低 |

- **Sgen**：build 期把 XmlSerializer 反射路徑展開成靜態強型別 serializer，執行期既不反射也不 emit。**唯一連半 B 都解掉的**，AOT 最穩。代價是整合最麻煩、複雜型別有時產不乾淨、多型仍需 `[XmlInclude]`。
- **DAM**：在反射目標型別標註，trimmer 看到就保留 metadata。精準、binary 最小，但**多型覆蓋是硬傷**——標基底不保子類，`FormSchema` 子型別一多就容易漏。只解半 A。
- **linker.xml**：一份 XML 列出「不准砍」的 namespace / 型別，可 wildcard 一次 root 一整族。正中「子型別多到難逐一標」的痛點，成本最低。只解半 A。

### 為何不能純靠 Android 驗證

iOS 若是真正目標，linker.xml / DAM **只解半 A，半 B 靠賭 iOS 的反射 fallback 能跑**，而那個「能不能跑」只有 iOS 驗得出來。相對地 **Sgen 是唯一連半 B 都一起解掉**（不需 emit、也不需 fallback）。要把 iOS-only 的不確定性壓到最低，最穩是 Sgen。

---

## 建議策略：descriptor 內嵌函式庫、Android 驗半 A、iOS 實機驗證延後

決定性特徵是**定義層多型子型別密集**，這直接淘汰「純 DAM 逐類標」當主力（覆蓋率撐不住）。兩項已拍板的前提：

- **descriptor 嵌進 `Bee.Definition`（非 per-app head）**：以 `ILLink.Descriptors.xml` 作 `EmbeddedResource` 隨 NuGet 發佈，trimmer 自動讀取。任何下游行動 app（含外部框架使用者）都自動保留定義型別 metadata，不必各自重推一份。框架正解，亦呼應對外散佈目標。
- **手上只有 iOS 模擬器**：模擬器對兩個半都不忠實——它允許 JIT（`Reflection.Emit` 照跑，**遮掉半 B**），且 linking 比 device 寬鬆（**半 A 不一定重現**）。故 iOS 模擬器只能驗「Debug 功能正確」，**驗不出 trim/AOT 任一半**。

排序如下：

1. **（前置）先把 iOS 編譯問題解掉** —— 見階段 0。編不過就沒有驗證的場子，這跟 trim 無關，必須先處理。
2. **descriptor 鋪底（嵌 `Bee.Definition`）**：wildcard root 整個定義族，用最低成本一次蓋滿最難的「多型覆蓋完整性」。半 A 用最穩的方式解掉。
3. **Android full-trim 驗半 A**：Android 是**唯一能由我們完全掌控、忠實重現半 A** 的場子（`TrimMode=full` 把 app 組件納入裁切）。descriptor 在這裡迭代到序列化 round-trip 通過。
4. **iOS AOT 編譯 build（免實機）**：device-target Release build 在 build 期就跑 AOT 編譯器，能抓 AOT 編譯期錯誤；不需實機。
5. **iOS device 執行期 round-trip（延後，需實機）**：半 B 的反射 fallback 只有 device 真跑得出來。**取得實機前此項掛起**。但有正面證據：記憶 `ios-xmlserializer-ambiguous-add` 的兩個雷正是 iOS AOT 反射 fallback 跑出來且**已修好**——半 B fallback 已被證明能跑，故**不預付 Sgen 整合成本**。
6. **（條件）Sgen 升級**：僅當日後實機驗證 fallback 真的不過時才上。Sgen 同時去掉半 A+B、不依賴實機，是「保底大絕」。
7. **DAM 收尾補洞**：定義 namespace 以外的零星葉節點，或日後想縮 binary 時，再局部換掉粗 descriptor。

一句話：**descriptor 嵌 `Bee.Definition` 鋪底 → Android full-trim 驗半 A → iOS 免實機 AOT build → device 執行期驗證延後（不預付 Sgen，因 fallback 已有證據能跑）**。

---

## 階段細節

### 階段 0：排除 iOS 編譯問題（前置，必先做）

「iOS 無法正常編譯」與 trim / AOT 是**兩個不同層級**的問題：編譯失敗屬 build / 工具鏈（workload、Xcode、provisioning、TFM 設定等），跟序列化無關。

- [x] 重現並記錄 iOS 編譯失敗的確切錯誤訊息
- [x] 確認環境前置：iOS workload、Xcode、模擬器
- [x] 先求 **Debug 在 iOS 模擬器編得過、跑得起來**（這是後續所有驗證的地基）
- [x] 記錄解法到本 plan

#### 階段 0 結論（2026-06-26）

- **根因**：純工具鏈版本鎖，與 code / trim 無關。已安裝的 .NET iOS workload（`Microsoft.iOS.Sdk 26.5.10284`）鎖 Xcode 26.5，本機 Xcode 為 26.6：
  `error: This version of .NET for iOS (26.5.10284) requires Xcode 26.5. The current version of Xcode is 26.6.`
- **解法**：build 加 `-p:ValidateXcodeVersion=false`（SDK 的 `_ValidateXcodeVersion` target 內建開關），跳過 minor 版檢查、**不需動 workload**（避免影響已驗好的 Android）。
- **結果**：`dotnet build -c Debug -p:RuntimeIdentifier=iossimulator-arm64 -p:ValidateXcodeVersion=false` 編過（0 錯誤）；`xcrun simctl install/launch` 後 app 在 iPhone 17 模擬器正常啟動，Avalonia 渲染出 ConnectionView。
- **注意**：`ValidateXcodeVersion=false` 是**本機工具鏈權宜**，不寫進 csproj（會對所有人/CI 關掉安全檢查）；正解是 Xcode 與 workload 對齊版本。回寫 `maui.md` 待整理。

### 階段 1：descriptor 內嵌 `Bee.Definition` + Android full-trim 驗半 A

descriptor 寫在**函式庫端**（隨 NuGet 發佈，下游自動受益），用 Android 當**唯一能完全掌控、忠實重現半 A** 的迭代迴圈。

- [x] 在 `src/Bee.Definition` 加 `ILLink.Descriptors.xml`，以 `<EmbeddedResource LogicalName="ILLink.Descriptors.xml" />` 內嵌，wildcard root `Bee.Definition.*` + `Bee.Base.Collections.*`（`preserve="all"`）。已確認成為 manifest resource、函式庫照常建置。
- [ ] 在 `apps/Bee.Northwind/Bee.Northwind.Android` Release 設 `<TrimMode>full</TrimMode>`，**先確認 `2, 2` 類錯誤能在 Android 重現**（不能重現代表沒裁到 app 組件，linker 沒上工）
- [ ] 反覆跑 Android Release，調 descriptor 到序列化 round-trip 通過
- [ ] 記錄最終 descriptor 涵蓋範圍

#### Android full-trim 實機（emulator）實測結論（2026-06-26）

> 先試桌面 `TrimMode=full` publish 當代理 → 不成立（桌面 trimmer 對 `Deserialize<T>` 靜態可達型別保守保留成員，半 A 不重現）。改用 **bee_pixel emulator + Android Release full-trim**，在 app 啟動注入 reflection-only round-trip 探針（反序列化 → 重序列化 → 比對屬性 token，**刻意不靜態引用任何定義屬性**避免自我 root），結果寫 logcat。診斷鷹架測完已移除。

**Bee.Definition.dll 大小（決定性，排除「沒裁到 app 組件」陷阱）：**

| 版本 | bytes | 砍除 |
|------|------:|-----:|
| 未 trim（函式庫原版） | 211,968 | — |
| **無** descriptor + full-trim | 91,136 | **57%** |
| **有** descriptor + full-trim | 207,872 | ~2% |

- trimmer 確實 aggressive 處理了 app 組件（212K→91K），陷阱排除。
- **有無 descriptor，Order/Customer/Product/Employee FormSchema round-trip 在 emulator 上都 `PASS; all tokens preserved`**——半 A 不會硬性打斷反序列化。
- descriptor **有實效、非 no-op**：無它砍 57%（我測的 FormSchema 路徑剛好倖存，但那 57% 可能含其他定義型別/邊角的序列化成員）；有它幾乎全保留。故 descriptor 是有效的「鋪底全覆蓋保險」，正中 plan 對多型密集定義族的設計意圖。
- **半 A 在 Android 上視為已解並驗證**（descriptor 保證完整覆蓋）。
- **對 iOS 的推論**：連 full-trim 砍半都不打斷 FormSchema 反序列化 → iOS 的 `2,2` **極可能是半 B（reflection-only 機制）而非半 A**，而半 B 已知雷在記憶 `ios-xmlserializer-ambiguous-add` **已修**。iOS XML 這條**可能前次修正後就已能跑**；descriptor 另保證 iOS 若 trim 更狠也有半 A 防護。待實機定論。
- logcat 另見 `Bee.Definition.XmlSerializers not found` → Mono 找預編 Sgen 組件未果、退回反射，符合預期（階段 3 才會導入 Sgen）。

### 階段 2：iOS 驗證——免實機 AOT build（半 B 執行期延後）

> 模擬器遮掉半 B（JIT 可用）、半 A 也不忠實，故模擬器只用來確認 Debug 功能正確；trim/AOT 的真正驗證走下列。

- [x] 把階段 1 內嵌 descriptor 帶進來（函式庫端已涵蓋，head 不需額外動作）
- [x] **免實機**：跑 iOS device-target Release build，確認 AOT 編譯器在 build 期不報錯（trim analysis / `load_aot_module` 類）
- [x] **免實機驗半 B**：強制 `RuntimeFeature.IsDynamicCodeSupported=false`（＝iOS AOT 鎖定的 reflection-only XmlSerializer 路徑），在真實 iOS 模擬器 runtime + Release trim 跑 round-trip → PASS
- [ ] **（低風險形式收尾，需實機+簽章）**：取得 iOS 實機後，Release（AOT 編譯版）跑同一 round-trip 做最終確認
  - 不通過（極不可能）→ 進階段 3（Sgen）

#### 免實機 AOT build 結論（2026-06-26）

- `dotnet build -c Release -p:RuntimeIdentifier=ios-arm64 -p:ValidateXcodeVersion=false -p:EnableCodeSigning=false`：
  - **0 錯誤**（~1m52s，含 trim + AOT）。產出 `bin/Release/net10.0-ios/ios-arm64/Bee.Northwind.iOS.app`，AOT 確實發生（`*.aotdata.arm64` 一批、主執行檔 52MB 靜態原生碼）。
  - linked `Bee.Definition.dll` = **211,968 bytes**（≈未 trim 全量）→ **descriptor 在 iOS linker 上同樣生效，半 A 在 iOS 亦覆蓋**。
- **device-target 預設止於簽章**（`金鑰鏈中找不到任何有效的 iOS 程式碼簽署金鑰`，AOT 前）；`-p:EnableCodeSigning=false` 跳過簽章即可完成 AOT build。簽章/實機安裝屬本案**非範圍**（需 Apple Developer 帳號）。

#### 半 B 免實機驗證（2026-06-27）— 強制 reflection-only

> 突破點：強迫模擬器「AOT」太 fiddly，但其**本質**＝讓 `RuntimeFeature.IsDynamicCodeSupported=false`，XmlSerializer 即改走 reflection-only reader/writer（iOS device AOT 鎖定的同一條 BCL 路徑，平台無關程式碼）。直接設這個 switch 比硬湊 AOT 可靠。

- 設定法：runtimeconfig 的 SDK 預設會把值鎖 `true`，故改在進入點第一行 `AppContext.SetSwitch("System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported", false)`（趕在任何 serializer 建立前）。
- **桌面 full-trim + switch=false**：`IsDynamicCodeSupported=False`，FormSchema/Settings round-trip 全 PASS。
- **真實 iOS 模擬器 Release（trim，linked Bee.Definition.dll=211,968B）+ switch=false**：logcat `IsDynamicCodeSupported=False` + `PASS round-trip 5050->4491; all tokens preserved`。**半 A（trim+descriptor）與半 B（reflection-only）兩半同時施加、在真實 iOS runtime 通過。**
- 與實機唯一差異＝device 跑「AOT 編譯版」的同一份 reflection-only 程式碼；AOT 編譯已證 0 錯誤、已知 AOT 反射雷已修（`ios-xmlserializer-ambiguous-add`）→ **實機執行期殘餘風險極低，降為形式收尾**。
- 診斷鷹架（iOS/Android TrimProbe + 內嵌 XML）測完已移除。

### 階段 3：（條件）改走 Sgen 預編

僅在階段 2 的反射 fallback 不過時啟動。

- [ ] 為定義型別導入 `Microsoft.XmlSerializer.Generator` build pass
- [ ] 補齊多型 `[XmlInclude]`，確保子型別都被產生
- [ ] 處理複雜型別產不乾淨的個案
- [ ] iOS Release 重新驗證

### 階段 4：（條件）MessagePack wire 補驗

僅在行動端**改用 MessagePack 上線**時才相關；維持 JSON 上線則整段略過。

- [ ] 把行動端 `PayloadFormat` 切到 MessagePack
- [ ] iOS Release 實機跑一次 API 呼叫，確認 MessagePack AOT source generator 路徑可過
- [ ] 不過 → 導入 MessagePack 的 AOT generator（`mpc` / source gen）

---

## 範圍與非範圍

- **範圍**：讓 Avalonia 行動 head 能以 Release 打包（iOS 為主，Android 連帶）；核心是 XML 定義持久化的 trim / AOT 相容。
- **開發位置（單 repo）**：型別修正（linker.xml / DAM / Sgen 目標）與 head csproj 設定**全在 bee-library**——前者落 `src/Bee.Definition`、後者落 `apps/Bee.Northwind/Bee.Northwind.iOS` / `.Android`。驗證也在來源端跑。
- **下游同步（最後一步）**：來源端驗證通過後，再走 graduation sync 把修正同步到 `bee-northwind-avalonia` 複本——這是**唯一**碰到第二個 repo 的時點，非開發過程。
- **非範圍**：
  - 桌面 / Browser（WASM）——本就跑得過，不受影響。
  - JSON wire——已驗證可過。
  - MessagePack wire——預設不在路上，僅階段 4 條件性處理。
  - iOS code-signing / notarization / App Store 上架流程——另案。

---

## 完成定義（DoD）

- [x] Android 能以 Release（含 app 組件 trim，`TrimMode=full`）正常跑定義序列化 round-trip（**半 A 主要證據**，emulator 實測）。
- [x] iOS device-target Release **通過 AOT 編譯期**（免實機，0 錯誤）。
- [x] iOS 真實 runtime（模擬器）+ reflection-only（`IsDynamicCodeSupported=false`）+ Release trim → round-trip PASS（**半 B 驗證**）。
- [ ] **（低風險形式收尾）** iOS 實機 Release（AOT 編譯版）安裝、啟動、執行期 round-trip——需 Apple Developer 簽章 + 實機；取得前以「待驗」記錄，不阻擋其餘 DoD。
- [x] 採用解法（descriptor 內嵌 `Bee.Definition`）與涵蓋範圍記錄在本 plan。Sgen（階段 3）未觸發。
- [x] 回寫 `maui.md` 的 Apple trim 決策樹為本次實測結論（#4 升為採用解法 + 當前對策更新）。
- [x] 行動端 README（中英）的「Debug-only」說明更新為實際狀態（Release trim 已驗證）。
- [ ] **（gated on release）** 複本 `bee-northwind-avalonia` @NuGet `4.12.0` 尚無 descriptor（4.12.0 之後才進 `Bee.Definition`）。descriptor 經**下個框架版本發佈 + 複本 bump PackageReference** 流入，非檔案複製；複本 README/heads 的 trim 狀態同步須與該版本 bump 一起做，否則 claim 與能力不符。
