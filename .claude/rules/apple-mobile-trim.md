# 行動端 trim / AOT 規範（iOS / Android / Mac Catalyst）

本檔記錄**與 UI 框架無關**的行動端建置雷區：Release 模式的 trim 會砍掉反射相依，
AOT 會禁用 `Reflection.Emit`，兩者都會打斷 `XmlSerializer` 這類靠反射運作的機制。

適用對象是所有行動 / Apple 平台 head，目前為 `Bee.UI.Avalonia` 的
`net10.0-ios` / `net10.0-android`（見 `apps/Bee.Northwind/Bee.Northwind.iOS`、`.Android`）。

> 推導脈絡、實測數據與 build / 驗證的完整命令配方見
> `docs/repo-ops/gotchas/mobile-trim-aot.md`（按需讀，不常駐）。
> 本檔只留判準與硬性要件。

## Sandbox 與 IO

iOS / Mac Catalyst 的 `.app` bundle 是唯讀。任何把設定寫回 assembly 所在目錄的做法
（`FileUtilities.GetAssemblyPath()`、`AppContext.BaseDirectory`）在行動端必定失敗。

需要持久化使用者資料時改用平台提供的可寫位置：per-user 應用資料目錄、可重建的快取目錄、
或 key-value 偏好儲存。框架端的接縫是 `IEndpointStorage`——行動 head 啟動時置換為
平台對應實作，不要沿用桌面的檔案式預設。

## trim：已解，解法是內嵌 ILLink descriptor

`XmlSerializer` 在 Apple Release trim 下的失效**已於 2026-06-27 解決並實測驗證**：
`ILLink.Descriptors.xml` 以 `<EmbeddedResource LogicalName="ILLink.Descriptors.xml">`
內嵌於 `Bee.Definition`（`src/Bee.Definition/ILLink.Descriptors.xml`），trimmer 自動掃描該
logical name，**所有下游 trim/AOT app（含外部框架使用者）自動受益**。

- **不要再嘗試 `<PublishTrimmed>false</PublishTrimmed>` / `<MtouchLink>None</MtouchLink>` /
  `<MtouchLink>SdkOnly</MtouchLink>` / 單獨開 `UseInterpreter`** —— 四條都試過且不可行
  （Apple SDK 強制 trim、AOT 編譯不全 SIGABRT、SdkOnly 不保護 SDK 自身）。細節見 gotchas。
- 新增定義型別**不需要**為 trim 做任何事：descriptor 用 wildcard root
  `Bee.Definition.*` + `Bee.Base.Collections.*`，一次蓋滿。

> ⚠️ 本節只涵蓋 **`XmlSerializer`（定義檔）那一半**，**不含 MessagePack wire**。
> wire 路徑另有一套要求（型別一律顯式註冊 formatter），見 `rules/serialization.md`。

## AOT：Android 驗不到動態碼那半

.NET for iOS SDK 對 **iOS / tvOS / MacCatalyst 的每一種組態**（Debug 與 Release、
裝置與模擬器）預設就把 `DynamicCodeSupport` 設為 `false`，除非顯式啟用直譯器。
「只有 Release 才要擔心」是錯的。

**Android 沒有這一條**——保有 JIT，`IsDynamicCodeSupported` 維持 `true`。
**凡與 `Reflection.Emit` / 動態碼有關的疑慮，Android emulator 驗不到**，
它只能驗 trim。別再用 Android 當這半的證據。

桌面重現不必改 csproj，一個命令列屬性即可（用的是 SDK 的同一條路徑）：

```bash
dotnet test <測試專案> -c Release --settings .runsettings -p:DynamicCodeSupport=false
```

### 判讀：例外「種類」不可當診斷依據，pass / fail 邊界才可以

同一個失敗在 CoreCLR＋開關關掉、NativeAOT、Mono 三種 runtime 擲**不同**例外
（對照表見 gotchas）。桌面重現特有的 `InvalidProgramException` 只表示**症狀**失真，
**不表示失敗是假的**。

**判別法：拿掉開關會不會過？會過而開著不過，就是真的踩到無動態碼路徑。**

要真 Apple runtime 時 Mac Catalyst 最便宜、其次 iOS 模擬器；要「真的沒有 Emit」用本機
NativeAOT console。命令配方見 gotchas。

## AOT 與 Interpreter 的組合

別亂組合這些設定：

- `MtouchLink` + AOT 預設組合穩定（SDK 預設）
- `MtouchLink=None` + `UseInterpreter=true` 看起來「都解掉」，但 CoreLib AOT 版本仍會不符 → 更壞
- 全用 Interpreter（`<MtouchInterpreter>-all</MtouchInterpreter>`）理論上可行，但啟動慢、性能差

## 序列化型別的行動端相容要件

reflection-only 的 `XmlSerializer`（iOS AOT 路徑）對型別形狀比桌面嚴格：集合型別**只能公開
一個** public instance `Add`、**必須有無參數建構子**、對映為重複 `[XmlElement]` 的集合屬性
**必須有 public setter**。三者違反時分別擲 `AmbiguousMatchException` /
`MissingMethodException` / `ArgumentException: Property set method not found`，
**桌面完全不會顯現**。

違反者一律是定義層型別 → 完整條文與 setter 的正確寫法見 `src/Bee.Definition/CLAUDE.md`；
反射盤點手法見 gotchas。

## 診斷雜訊（省得再走一次冤枉路）

- 錯誤 `There is an error in XML document (2, 2)` 看起來像 XML 內容壞掉，**實際多半是 AOT 路徑
  問題**（`2,2` 只是根節點開頭）。不要往 XML 內容查。
- 真正的 on-device `XmlSerializer` 觸發點是**行動 head 這一端**：head 是 remote JSON-RPC client，
  FormSchema 以 **XML 字串夾在 JSON wire** 傳到 client，client 端 `XmlCodec.Deserialize<T>(result.Xml)`
  才反序列化 —— **不是** Server 讀 `Define/*.xml`。
- 曾誤判為單純 trim 雷，試 `MtouchLink=None` 與 linker.xml `TrimmerRootDescriptor` 皆無效，
  真因是多載 `Add`。
- **Apple app bundle 不吃增量重建**：改動框架組件後再 build，bundle 內可能留著舊的受管組件，
  而 `dotnet build` 回報成功、零警告。徵狀是啟動即 SIGABRT（`Main` 從未執行），或
  app 跑得起來但行為是舊版的。**先比對 bundle 內組件時間戳再查程式碼**，正解是
  `rm -rf bin obj` 重建。詳見 gotchas。

## 相關

- `docs/repo-ops/gotchas/mobile-trim-aot.md` —— 推導脈絡、實測數據、命令配方
- `rules/avalonia.md` —— Avalonia 專屬規範（版本相容性、控件雷區）
- `rules/serialization.md` —— MessagePack / DynamicExpresso 的 AOT 結論。**兩者結論相反**：
  MessagePack 的 contractless **沒有** reflection fallback，wire 型別一律要顯式註冊 formatter；
  DynamicExpresso 則自動退回直譯器、無需處理。
  （「MessagePack 也有 fallback」是 2026-08-10 被實測推翻的舊結論，別照它推導。）
- `src/Bee.Definition/ILLink.Descriptors.xml` —— 採用解法的實際檔案
