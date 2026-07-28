---
name: bee-framework-review
description: 對 bee-library 框架做「全面體檢」的可重複方法論 —— 九面向(架構分層、相依分層與循環相依、安全性、維護性、散落/不必要類別、序列化一致性、公開 API 表面、測試品質與覆蓋、文件漂移)唯讀審查,以平行子代理分面向掃描(非抽樣),交叉去重後彙整成分級(P0~P4)重構計畫 + 每項 10 分制評分。內建各面向的具體檢查清單、已知雷區、與「應為乾淨」的基準項(供回歸偵測)。當使用者要「全面體檢」、「架構體檢」、「健康檢查/健康度」、「framework review」、「全面 review」、「架構審查」、「幫框架打分/評分」、「有沒有散落或不必要的類別」、「提重構計畫」之類需求時使用,即使沒明講「體檢」也要在這類全框架審查請求時主動觸發。**只負責唯讀審查、評分與產出重構計畫,不直接改 code**(修正另循一般流程)。
---

# bee-library 框架全面體檢

對整個框架(18 個 `src/` 專案)做結構化健康檢查,產出**分級重構計畫**與**每項 10 分制評分**。核心方法是**平行子代理分面向唯讀掃描**,再交叉去重彙整。

## 何時用 / 產出什麼

- **觸發**:使用者要求全面 review / 體檢 / 架構審查 / 打分 / 找散落類別 / 提重構計畫。
- **產出**:`docs/plans/plan-framework-review.md`(分級發現 + 執行順序)+ 對話內評分總表。若使用者只要口頭結論可略過落檔,但預設落檔(符合 CLAUDE.md「執行前先擬計畫」)。
- **紀律**:**全程唯讀,不改任何 code**。修正是後續獨立步驟,由使用者 review 計畫後決定。

## 執行前先問(用 AskUserQuestion)

依 `~/.claude/CLAUDE.md` 提問風格,每題附選項 + 標建議:

1. **追加面向**(multi-select):預設九面向已涵蓋;問是否要再加(如效能/熱路徑、並行全域狀態、跨平台 AOT)。
2. **範圍**(single):全部 18 專案(建議) / 只核心後端(排除 UI heads Avalonia/Maui/Blazor,重構訊號密度最高) / 使用者指定。
3. **執行方式**(single):唯讀 review + 計畫文件(建議) / 只口頭回報 / 多代理 workflow 深掃(需使用者明確同意大規模編排)。

## 九個檢查面向

| # | 面向 | 核心問題 |
|---|------|---------|
| 1 | 架構分層合理性 | N-Tier + Clean Architecture + MVVM 是否貫徹;Domain Core 是否純淨 |
| 2 | 相依分層與循環相依 | 有無循環、反向依賴、跨層繞道、Server 依賴 Client |
| 3 | 安全性 | 加密管線、Session/Token、SQL 注入、XXE、亂數、資源釋放、存取控制 |
| 4 | 維護性 | 命名一致性、註解、一型別一檔、識別碼比對文化、大檔 |
| 5 | 散落/不必要類別 | grab-bag、純 facade、`*Func` 遺留、一檔多型別、死碼 |
| 6 | 序列化一致性 | XML/JSON/MessagePack 三棲標籤、`[Union]` 多型、typeless 白名單、trim/AOT |
| 7 | 公開 API 表面 | 契約軸命名空間一致性、BO 介面純度、breaking-change 面、四層對齊 |
| 8 | 測試品質與覆蓋 | 無效斷言(S2699)、覆蓋缺口、fixture 污染、flaky、`[Collection]` 序列化 |
| 9 | 文件漂移 | 公開文件宣稱 vs 程式碼實際、死連結、雙語落差、CHANGELOG 未記 breaking |

> 面向 1 與 2 高度重疊,可合派一個代理但分開評分。
>
> **面向 9 為 2026-07-28 起的常設項**,原本散在各面向的 P3。首次獨立評分即為九面向最低(4.5/10)
> —— 被結構面的高分稀釋掉正是它該獨立計分的理由:文件漂移不會讓 build 紅、不會讓測試失敗,
> 只會讓外部開發者照著寫出不能編譯的程式碼,沒有任何自動化機制會發現。

## 方法:平行子代理分面向掃描

派 **7 個 `general-purpose` 子代理**(背景平行),各掃一面向(架構+相依合派、其餘各一)。每個代理:
- **嚴格唯讀**,prompt 明示「絕對不可修改/寫入任何檔案,只回報發現」。
- 收到**該面向的完整檢查清單 + 已知雷區**(見下)+ 相關規範摘要(從 `~/.claude/rules/` 與 `.claude/rules/` 提煉,別叫代理自己去讀整包)。
- 回報格式統一:分三級 + 每項附 `專案/檔案:行號`、問題(WHY)、建議。
- 用 grep/glob **全量掃描,非抽樣**;要求列**完整清單**(不接受「有一些」)。

全部回報後,主代理**交叉去重**:同一發現被兩個代理獨立指出 → 提高信心、優先處理(這次 `MessagePackKeyCollectionBase` comparer 即由維護性與序列化兩代理確認)。

> 這用一般子代理委派(非計費的大規模 workflow 編排)。若使用者選 workflow 深掃,才改用 Workflow 工具。

## 與 CI build gate 的分工(尤其 code style,別重掃)

體檢是**語意/結構審查**,不是格式檢查器。凡 `build-ci.yml` strict build 已擋的,體檢**不重掃**——build 綠燈本身即證明,重跑無新訊號:

- 純格式(縮排 / LF / UTF-8 無 BOM / `using` 排序,`.editorconfig` 管)。
- 已 `.editorconfig` 硬性化的 analyzer 規則(CA1052/CA1822/IDE0044/IDE0051/CA1725/CA1305/CA1861… 見 `sonarcloud.md` 開頭「已硬性化,不再列入」清單)。
- `TreatWarningsAsErrors=true` 下的 nullable/CS 警告(編不過就進不了 PR)。
- SonarCloud 已自動掃的 issue。

體檢**只驗機器管不到的 code style 語意規則**(已內含於面向 4 維護性):識別碼比對 `Ordinal` vs `CurrentCulture` 的**語意選擇**(CA1305 只管 `IFormatProvider`,管不到 `StringComparison`)、一型別一檔、資料夾↔命名空間的**個別資料夾例外**(IDE0130 全域規則無法對個別資料夾開例外,故 prompt 層把關)、靜態工具歸屬(path A/B/C/D)、grab-bag / 純 facade、`*Func`/`*Helper` 命名棄用、註解 WHY-not-WHAT 與 S125 人工判定。

> 一句話:**格式與 analyzer 硬規則歸 build gate,語意與結構歸體檢。不設獨立「code style 面向」——那半機器管、這半在維護性裡。**

## 各面向檢查清單(派給代理時貼這些)

### 1+2. 架構分層與相依
- 讀 `docs/dependency-map.md`、`docs/architecture-overview.md`、`docs/development-constraints.md` 建基準。
- 逐一擷取各 `.csproj` 的 `<ProjectReference>`,畫實際相依圖,拓樸排序驗**無循環**。
- 硬約束驗證:BO(`Bee.Business`)**無** `Bee.Db` 參照;後端(AspNetCore/Hosting/Business/Repository/Db)**無** `Bee.Api.Client` 參照(注意 `Bee.Web.Blazor.Server` 是前端 RCL,參照 Api.Client 屬正確);Repository 抽象(`Bee.Repository.Abstractions`)未被繞過;`Bee.Api.Contracts` 未被實作污染。
- 找:上帝專案(職責過載 vs 職責廣度 —— 行數大不等於該拆,看內聚)、Domain Core 夾帶基礎設施職責、文件相依圖 vs 實際 csproj 的落差。

### 3. 安全性(規範源:`.claude/rules/security.md` + `~/.claude/rules/scanning.md`)
- SQL:一律 `DbCommandSpec` 的 `{0}` 佔位符;grep `$"...SELECT/INSERT/UPDATE`、`string.Format` 組 SQL;識別符須經 `QuoteIdentifier` 逃逸。
- 加密:AES-CBC-HMAC(256-bit + SHA-256 + 隨機 IV),HMAC 用常數時間 `CompareBytes`(非 `==`);payload 管線序列化→壓縮→加密不可調換;存取驗證須在**解密前**。
- 亂數:安全用途一律 `RandomNumberGenerator`,禁 `System.Random`。
- XXE:解析不受信任 XML 須 `DtdProcessing.Prohibit` + `XmlResolver=null`。
- 例外:禁 `catch(Exception)` 基底、空 catch、`throw ex;`;例外/log 禁洩漏金鑰/token/密碼/堆疊/內部路徑。
- 資源:`IDisposable` 用 `using`,禁散落手動 `.Dispose()`。
- 存取控制:對外 method 是否都有適當 `[ApiAccessControl]`;未標註是否 fail-closed(拒絕而非放行);預設驗證器是否真正驗金鑰值(非只檢查非空)。
- 硬編碼:金鑰/憑證/連線字串密碼;MD5/SHA1 用於安全雜湊;`NoEncryptionEncryptor` 在非 debug 是否可被啟用。

### 4. 維護性(規範源:`~/.claude/rules/code-style.md` + `.claude/rules/sonarcloud.md`)
- **識別碼型字串比對誤用文化相依**(高價值檢查):grep `CurrentCultureIgnoreCase`、`CurrentCulture`、`.ToLower()`/`.ToUpper()`(無 Invariant)。集合 key、欄位名、ProgId、型別名、delimiter 切割一律 `Ordinal`/`OrdinalIgnoreCase`/invariant —— Turkish-I 正確性風險。
- `*Func` 靜態類別殘留(規範說 2026-05-01 已全移除,驗證回歸)。
- 一型別一檔:grep 一檔多個 `public class/interface/enum`(尤其介面 + 實作同檔)。
- 資料夾↔命名空間一致(IDE0130);大檔(> 500 行)拆分候選。
- 命名違規:Hungarian 前綴、`*Helper` 後綴、參數非 camelCase。
- 註解掉的舊 code(S125)、空 class(S2094)、未 sealed 的 private nested(S3260)。

### 5. 散落/不必要類別
- grab-bag / 上帝類(名稱含 Helper/Utils/Common/Misc/Manager 而內容發散)。
- 純 facade / 1-line delegation wrapper(無附加價值,但 DI 抽象縫例外)。
- 死碼:0-caller 的**非公開** API、`[Obsolete]` 且無呼叫者(框架公開 API surface 即使 0-caller 仍保留,純 BCL wrapper 才刪)。
- 重複實作(S4144):多處相同邏輯應合併。
- **澄清陷阱**:`ExecFunc*` 家族是 domain 型別(JSON-RPC「執行函式」模式),非被棄用的 `*Func` 靜態類,勿誤報。

### 6. 序列化一致性(規範源:`bee-serialization` skill)
- **預設 wire 是 MessagePack**(`Bee.Api.Core/ApiServiceOptions.cs`),放大「JSON/XML 沒事、MessagePack 出事」問題。
- **typeless 白名單**(最實在的雷):`object` 型欄位走 `SafeTypelessFormatter`,值型別須在 `AllowedPrimitiveTypes` + `SysInfo.IsTypeNameAllowed` 白名單內。特別查 `FilterCondition.In()` 這類把 `object` 設成 `List<object>`/`object[]` 的路徑 —— 不在白名單會反序列化擲例外。
- MessagePack item 參數化 ctor 參數順序須對齊 `[Key]` 順序(有無參數 ctor 走 setter 則不觸發)。
- `[Union]` 多型 ⊥ keyAsPropertyName:多型型別維持整數 `[Key]`;非多型型別用 name-based(adr-030)。
- 三棲標籤完整:衍生/計算/暫態欄位須三套都 ignore(`[XmlIgnore, JsonIgnore, IgnoreMember]`);別靠私有 setter 隱性避免上 wire(contractless 下改 public setter 就靜默洩漏)。
- 集合:`MessagePackCollectionBase<>` 子型別須在 `MessagePackCodec` 顯式註冊 formatter,否則沉默出空集合;Definition 集合禁裸 `List<T>`/`Collection<T>`。
- Newtonsoft.Json 殘留(應為 0)。

### 7. 公開 API 表面
- 契約軸命名空間↔資料夾一致(`Bee.Api.Contracts.{System,Form,AuditLog}` ↔ `Bee.Api.Core.Messages.{...}`)。
- **BO 介面純度**:`I<Axis>BusinessObject` 只放跨 BO 會呼叫的方法;純 API 方法(Ping/GetFormSchema/GetFormLayout/GetLanguage)只在具象類別 public + `[ApiAccessControl]`,不放介面。
- 四層對齊抽查:wire DTO(`XxxRequest/Response`)↔ 契約介面(`IXxxRequest/Response`)↔ BO 實作 ↔ Client connector,命名與簽章一致,無孤兒。
- breaking-change 面:public 可變欄位、public 暴露具體集合型別(對外契約應以 `IReadOnlyList` 收斂)、應 internal 卻 public 的實作型別。
- 死路徑基礎設施:production 零註冊/零呼叫但掛在熱路徑的機制(標註「預留」而非誤判已生效)。
- `///` XML 文件註解:公開 API 缺漏或用中文(應英文)。

### 8. 測試品質與覆蓋(規範源:`.claude/rules/testing.md`)
- 無效斷言(S2699):`[Fact]/[Theory]` 無 `Assert.*`;驗「無例外」須 `Record.Exception` + `Assert.Null`。**注意**:委派到 asserting helper(如 `AssertXxx`、`TestFunc`)是合法的,別誤報 —— 讀可疑檔案確認。
- **空洞 round-trip**:序列化測試 helper 是否只 `Assert.NotNull` 不比對還原值(名實不符的假綠燈)。正確範本 `tests/Bee.Api.Core.UnitTests/TestFunc.cs`。
- 覆蓋缺口:src vs tests 檔數對照;安全邏輯(加密/雜湊)、序列化三棲 round-trip、公開 BO 方法必測;找 `In` MessagePack 這類零覆蓋的關鍵路徑。
- fixture 污染:`SaveDefine` 系列測試須切 temp 目錄,禁寫 `tests/Define/`。
- `[Collection]` 序列化(規範宣稱 Phase 7 後清零,驗回歸)、測試改 production static、真實牆鐘 flaky(建議 `TimeProvider` + `FakeTimeProvider`)。

### 9. 文件漂移(規範源:`.claude/rules/public-docs.md`)

範圍是**公開文件**(寫給 NuGet 套件使用者看的):repo 根 `README*` / `CHANGELOG*`、`docs/` 根目錄全部 `.md`、
`docs/adr/`、`docs/changelogs/`、以及**所有位置**的 `README.md` / `README.zh-TW.md`(`src/` `samples/` `apps/` `tools/`)。
`docs/plans/`、`docs/repo-ops/`、`docs/internal/`、`.claude/` 不是公開文件。

- **可編譯性(最高價值)**:文件中的型別名、方法名、DI 擴充方法、列舉成員逐一比對原始碼是否存在且大小寫相符。
  外部開發者第一天就會複製的段落(cookbook、快速上手、README 範例)優先。**這類錯誤沒有任何自動化機制會抓到。**
- **設計之錄失真**:ADR 描述的機制是否仍存在(型別已更名 / 已移除卻仍被當作現行機制描述);
  `development-constraints` 的硬約束是否仍成立;被推翻的 ADR 是否已標 `已取代(Superseded)`。
- **改名 / 刪型別的反向索引**:每次移除或更名公開型別,`grep -rn "<舊名>" --include="*.md"` 是否已清乾淨
  —— 這是本面向約六成問題的來源。
- **死連結**:全量檢查相對連結與 anchor。特別注意 `docs/changelogs/*.md` 這類**子目錄**中誤用 repo-root
  相對路徑的整批錯誤(從子目錄解析會多一層,GitHub 上全 404)。
- **雙語同步**:比對雙語配對的章節結構與內容量,找單邊更新;找應有雙語卻只有單語者。
- **CHANGELOG**:`Directory.Build.props` 版本 vs tag 之後的 commit,標 `!` 的是否都已記載;
  是否有 Unreleased 區段;既有敘述是否已被後續 commit 推翻。
- **量化基準**:文件宣稱的專案數 / 相依邊 / 套件清單 vs 實際 csproj。

## 彙整與產出

### 分級(P0~P4)
| 級 | 含義 |
|----|------|
| **P0** | 正確性/功能風險(wire 反序列化失敗、識別碼比對文化 bug、假綠燈測試) |
| **P1** | 安全與序列化標籤一致性(多為低風險批次修) |
| **P2** | 結構重構(職責拆分、大檔、一檔多型別、死路徑標註) |
| **P3** | 文件漂移(相依圖、契約文件、規範敘述失真) —— 低成本高價值 |
| **P4** | 觀察/待使用者裁決(慣例豁免、次要補測) |

每項附 `檔案:行號`、問題(WHY)、建議、嚴重度。文件末附「掃描為乾淨的項目」清單(供未來回歸偵測)+「建議執行順序」。

### 評分(每項 10 分制)
彙整後給評分總表:每面向一分數 + 主要扣分點(對應發現編號)+ 加權平均綜合分。評分邏輯:
- **9+**:該面向零技術債累積、規範全守,僅文件層或極少數瑕疵。
- **7~8.5**:底子好但有明確可修的一致性缺口。
- **6~7**:有具體功能/正確性 bug 拉低(非機制設計問題)。
- 標明「修掉哪幾項 P0/P1 後可回升到幾分」,給使用者提分路徑。

## 計畫文件格式
遵循 CLAUDE.md 的 plan 規範:頂部單行狀態列 `**狀態:📝 擬定中(YYYY-MM-DD)**` + 多階段(P0~P4)階段表格。回覆中附 `[plan-framework-review.md](docs/plans/plan-framework-review.md)` 連結。

## 方法論教訓(2026-07-28 該輪學到,下次直接沿用)

**1. 基準要寫具體清單,不要寫「死碼 0」這種無從驗證的斷言。**
上輪基準宣稱「空 class 0、死碼 0」,本輪查出至少 15 個零使用型別且**全部早於上次體檢**
—— 上輪判定過於樂觀(很可能只掃了完全無引用的檔案,沒追到「宣告 + DI 註冊」或
「宣告 + 佔位測試」這類假陽性存活的型別)。**佔位測試會讓死碼在覆蓋率報告上呈現為已測試。**

**2. 分數下降多半是掃描變深,不是程式碼退步 —— 但必須逐項用 git 驗證才能這樣說。**
要求每個代理對「本輪新發現」用 `git log`/`git show` 查出問題引入時間,分清「回歸」與「既有問題首次被掃出」。
兩者的處理優先序不同,混為一談會誤導使用者。

**3. P0 級發現值得付出實測成本。**
本輪「定義類 response 在 wire 上內容全滅」原本只是理論推斷,在 scratchpad 建獨立 console 專案
(ProjectReference 到目標套件、走**公開**的序列化入口)實測後,才釘死失敗模式是
**沉默空殼而非擲例外**,而修法選擇正好取決於這個答案。注意 `MessagePackCodec` 是 internal,
外部探測要走 `MessagePackPayloadSerializer`。

**4. 代理的結論要交叉驗證,尤其牽涉「這段是死碼」的判斷。**
本輪有代理把 `ApiAccessValidator` 的 `LocalOnly` 判斷標為死碼,實際上只有條件後半段冗餘,
機制本身有效 —— 若照單全收會誤判防護無效。同理,建議的修法可能建立在錯誤前提上
(把定義型別當 wire DTO),**送出建議前先確認該型別的序列化契約**。

## 已知基準(上次體檢結果,供對照回歸)

上次 **2026-07-28**:八面向平均 **8.19**、含文件漂移九面向 **7.78**。
各面向:架構 8.8、相依 9.2、安全 7.8、維護 8.5、散落 7.5、序列化 7.0、公開 API 8.5、測試 8.2、文件 4.5。

**應維持為乾淨**(由乾淨變不乾淨即為回歸,標紅優先):
無循環/反向依賴、BO 無 Db 參照、Server 無 Client 參照、`*Func` 殘留 0、Newtonsoft 0、
契約軸 100% 對齊(已有 `ApiContractPairingTests` 守門)、BO 介面純度無違反、
MessagePack ctor 順序 landmine 未觸發、`[Union]`⊥keyAsPropertyName 遵守、
`MessagePackCollectionBase<>` 子型別 formatter 註冊無遺漏、
`new DateTime(` 未指定 `Kind` 0 處、`Regex` 未傳 timeout 0 處、
`CurrentCultureIgnoreCase` 0 處、public 可變欄位 0、公開文件零 `docs/plans/` 引用。

**已知不乾淨(具體清單,取代「死碼 0」)**:`IEnterpriseObjectService`、`EnterpriseObjectService`、
`InitializeOptions`、`ApplicationType`、`SysFuncIDs`、`VersionFiles`、`TreeNodeIgnoreAttribute`、
`DefaultBoolean`、`NotSetBoolean`、`SystemActions.GetLocalDefine`/`SaveLocalDefine`、
`IDefineField`、`IElementCapabilityResolver`、`CheckPackageUpdate`/`GetPackage` 全棧。
下次體檢應確認這些是否已清理,而非重新「發現」一次。

**已建立的守門機制**(下次體檢應確認仍存在且有效):
`BoApiSurfaceTests`(鎖定所有 `[ApiAccessControl]` 存取等級的 baseline)、
`ApiContractPairingTests`(wire DTO ↔ 契約配對)、
`TestFunc` 的 `comparedCount > 0` 斷言(防 helper 靜默退化)。
**尚未建立**:public API surface 快照(`PublicAPI.Shipped.txt`)—— breaking change 漏標 `!`
已連續兩輪發生(`IExcelHelper`、`IEvictableCache`),這是目前最高槓桿的單一改善。
