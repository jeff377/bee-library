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

- **行動端 Release trim/AOT 序列化已驗證可過**：Android emulator full-trim round-trip PASS；
  iOS device-target AOT build 0 錯誤；iOS 模擬器以 `IsDynamicCodeSupported=false` 強制
  reflection-only path（＝device AOT 同路徑）round-trip PASS。唯 iOS **實機** AOT 執行期
  為低風險形式收尾（需 Apple Developer 簽章 + 實機）。
- **半 B 免實機驗證法**：進入點第一行
  `AppContext.SetSwitch("System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported", false)`
  （趕在任何 serializer 前），即可在桌面 / 模擬器重現 iOS AOT 的 reflection-only 序列化路徑。
- **iOS 編譯前置雷**：workload 鎖 Xcode 版本不符時，build 加 `-p:ValidateXcodeVersion=false`；
  device-target build 止於簽章可加 `-p:EnableCodeSigning=false` 完成 AOT build（驗證用）。

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

這兩點在桌面完全不會顯現，只在行動端 reflection-only 路徑爆炸。

## 相關

- `rules/avalonia.md` —— Avalonia 專屬規範（版本相容性、控件雷區）
- `src/Bee.Definition/ILLink.Descriptors.xml` —— 採用解法的實際檔案
