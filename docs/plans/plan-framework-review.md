# 計畫：框架體檢與分級重構（2026-07-28）

**狀態：✅ 已完成（2026-07-28）**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| P0 | 正確性風險：wire 內容全滅、假綠燈測試、時區預設值、Date 編輯器、憑證替換 fail-open | ✅ 已完成（2026-07-28） |
| P1 | 安全：定義檔寫入授權、路徑遍歷、匿名發 token、運算式沙箱、可變參照外洩 | ✅ 已完成（2026-07-28） |
| P2 | 結構重構：UI head 複製收斂、Hosting 資料存取下沉、方言規則共用、死碼清理 | ✅ 已完成（2026-07-28） |
| P3 | 文件漂移：53 條死連結、不存在的 API、CHANGELOG 未記 6 項 breaking | ✅ 已完成（2026-07-28） |
| P4 | 觀察與裁決項：慣例豁免、次要補測、命名 breaking 排程 | ✅ 已裁決（2026-07-28，命名 breaking 排下一 major） |

> **第一批已落地（2026-07-28）**：挑選標準為「明確 bug 修正 + 純加法 + 測試／文件機械修正」，
> 排除需產品裁決者（P0-1 / P0-3）與屬 breaking 者（P2-4 死碼刪除、P2-7 wire 標籤）。
> 驗證：Release build 0 警告 0 錯誤；全套測試 0 失敗（四個 DB 容器均在運行，`[DbFact]` 為實跑非 skip）。

## 背景

依 `bee-framework-review` skill，以 7 個唯讀子代理平行掃描 `src/` 全 18 個專案
（864 個 `.cs` / 64,216 行）與 `tests/`（555 個 `.cs` / 3,884 個測試方法），
涵蓋八個既有面向 + 使用者本輪指定獨立拉出的「文件漂移」面向。

上次體檢基準為 2026-07-24（綜合 ~9.0/10）。本輪距上次僅 4 天，但期間有數個跨層大改動落地：
時區貫通（P0–P3）、`FieldDbType.Time` 新型別、日期一律 `DateOnly`、
時間取值家族重構、`SerializableData*` 改 property-name key。

### 關於分數下降的必要說明

**本輪多數面向分數下降，但真正的「回歸」只有兩項。** 其餘降幅來自掃描深度提升
——本輪把 `PackageReference`、`git show` 歷史比對、實際執行驗證納入掃描，
查出的多是長期存在但未被偵測的既有問題。各代理均以 git 歷史逐項驗證了問題的引入時間。

若以本輪的掃描深度回推，上次的實際分數應落在 8.0–8.5 之間，而非 9.0。
**建議把本文件的「回歸基準」一節寫成具體型別清單，取代「死碼 0」這類無從驗證的斷言。**

真正的本期回歸：

1. **`TestFunc` 假綠燈**（P0-2）—— 由 adr-030 的 name-based keys migration 引入
2. **契約軸 `IGetDepartmentTreeRequest` 缺漏**（P2-6）—— 破壞上次「100% 對齊」基準

---

## 評分總表

| 面向 | 上次（07-24） | 本次（07-28） | 升降 | 主要扣分點 |
|------|:---:|:---:|:---:|------|
| 架構分層 | 9.0 | **8.8** | ▼0.2 | Hosting 承載資料存取、UI head 複製 |
| 相依關係 | 9.5 | **9.2** | ▼0.3 | MessagePack 為 wire ABI 卻未宣告 |
| 安全性 | 8.8 | **7.8** | ▼1.0 | 定義檔寫入無授權模型（連帶三項） |
| 維護性 | 8.7 | **8.5** | ▼0.2 | 識別碼比對文化相依群集 |
| 散落類別 | 9.5 | **7.5** | ▼2.0 | Blazor 雙套件複製、死碼清單 |
| 序列化 | 8.5 | **7.0** | ▼1.5 | 定義類 wire 內容全滅 |
| 公開 API 表面 | 9.5 | **8.5** | ▼1.0 | 6 組 breaking 未標 `!` |
| 測試 | 8.3 | **8.2** | ▼0.1 | 假綠燈 helper、static race |
| 文件漂移 | — | **4.5** | 首次 | 可編譯性破產、設計之錄失真 |

**八面向平均 8.19**（上次 8.98）｜**含文件漂移九面向平均 7.78**

文件面向首次獨立評分即為最低分，這正是把它拉出來單獨計分的價值 ——
過去它散在各面向的 P3，被結構面的高分稀釋掉了。

---

## 交叉確認項（多個代理獨立指出，信心最高）

| 發現 | 獨立指出的面向 | 處理層級 |
|------|------|------|
| Blazor 雙套件 + `FormDataObject` × 4 複製，且程式碼**零 UI 框架相依** | 架構、散落 | P2-1 |
| `SysInfo` 型別白名單用文化相依比對 | 安全、維護性 | P1-6 |
| `IEvictableCache` 被移除 vs ADR 仍描述它為核心機制 | 公開 API、文件 | P3-2 |
| 定義類 wire 失真 ← 測試 helper 失效 + round-trip 只用空物件 | 序列化、測試 | P0-1、P0-2 |

---

## P0：正確性 / 功能風險

### ✅ P0-1. 定義類 response 在 .NET 端反序列化不回內容（沉默失敗）★ 已實測確認

**實測結果**（scratchpad 獨立專案，走公開的 `MessagePackPayloadSerializer`，即實際 wire 路徑）：

```
[before]    Tables=1  Fields=2
[serialize] OK, 135 bytes                        ← 不擲例外
[after]     ProgId='Employee' DisplayName='員工資料'   ← 純量欄位完好
[after]     Tables=0                             ← 內容全滅
```

**根因**：contractless 模式下 MessagePack 依 `IsReadable` 決定寫出、依 `IsWritable` 決定還原。
下列集合屬性**只有 getter**，於是寫得出去、寫不回來，且 MessagePack 沒有 System.Text.Json 的
`JsonObjectCreationHandling.Populate` 這種「填充既有集合」機制：

| 檔案 | 屬性 |
|------|------|
| [FormSchema.cs:168](../../src/Bee.Definition/Forms/FormSchema.cs) | `Tables` |
| [FormSchema.cs:209](../../src/Bee.Definition/Forms/FormSchema.cs) | `Rules` |
| [FormTable.cs:77](../../src/Bee.Definition/Forms/FormTable.cs) | `Fields` |
| [FormLayout.cs:115,134](../../src/Bee.Definition/Layouts/FormLayout.cs) | `Sections` / `Details` |
| [LanguageResource.cs:45,56](../../src/Bee.Definition/Language/LanguageResource.cs) | `Items` / `Enums` |

**影響面**：`SystemApiConnector.GetFormSchemaAsync` / `GetFormLayoutAsync` / `GetLanguageAsync`
這組 API 的 XML doc 寫著「primarily intended for JS frontends」，
但**預設 `PayloadFormat` 是 `Encrypted`（＝MessagePack）**，任何 .NET 呼叫端使用即中招。
走 `ClientDefineAccess.GetDefineAsync<T>`（XML 字串）的既有 .NET UI 路徑**不受影響**。

**連帶問題（P0-1b）**：[KeyCollectionItem.cs:99](../../src/Bee.Base/Collections/KeyCollectionItem.cs)
的 `Collection`、[CollectionBase.cs:44](../../src/Bee.Base/Collections/CollectionBase.cs) 的 `Owner`
等反向參照只標 `[XmlIgnore, JsonIgnore]`，缺 `[IgnoreMember]`。
根因是結構性的：**`Bee.Base` 沒有 MessagePack 的 `PackageReference`，標不了 `[IgnoreMember]`**。
（已驗證：`Bee.Base.csproj` 無任何 `PackageReference`。）

**補充實測（JSON 路徑）**：JSON 序列化「出去」含完整 `Tables` 樹（1299 字元），
「回來」同樣是 `Tables=0`。**兩種 format 皆為單向壞** —— JS 端（只消費 JSON）不受影響，
壞的只有 .NET 端的反序列化。

**修復方式**：**不是**給集合加 setter。定義型別（`FormSchema` / `FormLayout` /
`LanguageResource` / `TableSchema`）的序列化契約就是 XML，其 get-only 集合正是
XmlSerializer 的 populate 語意所需；為了 JSON/MessagePack 去改定義模型的屬性形狀，
等於讓 wire 格式反過來決定定義模型的設計。

正確做法是讓定義資料只有一條傳輸路徑（`GetDefine`，XML 承載），並讓 .NET client
不要提供第二條。框架其實**已為 `GetLanguage` 建立此慣例**（無 .NET client 方法 +
註解指引），只是 `GetFormSchema` / `GetFormLayout` 沒跟上，XML doc 還宣稱
"usable from the .NET client as well"。

已執行：
1. 移除 `SystemApiConnector.GetFormSchemaAsync` / `GetFormLayoutAsync`
   （**production 零呼叫端** —— 所有 UI 走的是同名但不同類的 `ClientDefineAccess`，屬 XML 路徑）
2. 三個定義型 API 統一為 JS-only，client 端以單一註解說明「為何定義資料不走 .NET client」
3. server 端 BO 的 XML doc 由「.NET client **may** keep using `GetDefine`」
   改為明確的 **JS-only API** 標註與原因，對齊 `GetLanguage` 既有措辭

**殘留（已知、可接受）**：server 端這三個方法在 `Encoded` / `Encrypted`（MessagePack）
format 下仍會回傳空殼。JS 端走 `Plain`，.NET 端已無入口，故無實際觸發路徑；
XML doc 已標明。若日後要硬性防護，可在 BO 層對非 `Plain` format 直接拒絕。

### ✅ P0-2. `TestFunc` 假綠燈 —— 12 個核心 wire 契約失去驗證 ★本期回歸

[tests/Bee.Api.Core.UnitTests/TestFunc.cs:24-28](../../tests/Bee.Api.Core.UnitTests/TestFunc.cs)
的比對迴圈以 `keyAttribute != null` 為閘門。

**已驗證**：adr-030 遷移後，`src/` 只剩 3 個檔案還有 `[Key(...)]`
（`FilterGroup` / `FilterCondition` / `MessagePackKeyCollectionBase`），
其餘 **77 個型別已改 `keyAsPropertyName: true`**，屬性上再無 `[Key]`
→ 迴圈一次都不執行 → helper 退化成只剩 `Assert.NotNull(deserialized)`。

**諷刺點**：正是 name-based keys 那次遷移，把用來驗證那次遷移的測試關掉了。

受影響：`MessagePackTests.cs` 的 12 個呼叫點 / 6 個 `[Fact]`。其中
`CreateSessionRequest/Response`、`GetDefineRequest/Response`、`SaveDefineRequest/Response`
共 6 個型別**在整個 `tests/` 只出現在這一處**，目前 wire 保真度為零實質驗證。

**修法**：移除 `[Key]` 閘門，對所有 public readable 屬性一律比對；
或改採 [ContractsDtoRoundTripTests.cs](../../tests/Bee.Api.Core.UnitTests/MessagePack/ContractsDtoRoundTripTests.cs)
的顯式逐欄斷言模式（本 repo 目前最好的範本）。**修好後須立即重跑，確認 6 個測試仍為綠。**

### ✅ P0-3. 時區預設值與文件相反 ★ 已驗證

[SessionInfo.cs:73](../../src/Bee.Definition/Identity/SessionInfo.cs)：
`public string TimeZone { get; set; } = "Asia/Taipei";`

[SystemBusinessObject.Session.cs:275](../../src/Bee.Business/System/SystemBusinessObject.Session.cs)
只在 `st_user.time_zone` 非空時覆寫。

但 [docs/datetime-timezone.md:87,103](../datetime-timezone.md) 寫「An empty value means UTC」
「Existing rows have no value, which reads as UTC」（zh-TW 版 `:76,90` 同義）。

**影響**：升級後未填 `time_zone` 的部署，全體時刻偏移 +8 小時，而文件明說那是無作用狀態。
海外部署會看到系統性錯誤時間卻查不出原因。

**修復方式（採 C 方案）**：實作時發現關鍵事實 —— 轉換層三處
（`FrameworkClock.Now`、`DateTimeZoneConverter.IsNoOp`、`PayloadZoneConverter`）
**早就一律把空字串視為 UTC**，文件說的「空值代表 UTC」是對的；
是 `SessionInfo.TimeZone` 硬編非空預設，讓那條路徑永遠走不到。

因此修法為「讓既有語意真正生效」而非新增語意：

1. `SessionInfo.TimeZone` / `UserInfo.TimeZone` 預設改為空字串
2. 新增 `BackendConfiguration.DefaultTimeZone`，預設 `Asia/Taipei`（升級零位移）
3. 登入填充改為：`st_user.time_zone` 非空 → 用它；否則 → `DefaultTimeZone`
4. 雙語文件同步更正

部署要 UTC 只需把 `DefaultTimeZone` 設為空字串。

### ✅ P0-4. `FieldDbType.Date` 無編輯器對映 ★ 已驗證，程式 bug

[LayoutColumnFactory.cs:65-76](../../src/Bee.Definition/Layouts/LayoutColumnFactory.cs)
的 `ResolveControlType` switch：

```csharp
FieldDbType.Boolean  => ControlType.CheckEdit,
FieldDbType.DateTime => ControlType.DateEdit,
FieldDbType.Time     => ControlType.TimeEdit,
FieldDbType.Text     => ControlType.MemoEdit,
FieldDbType.Short or ... => ControlType.NumericEdit,
_ => ControlType.TextEdit,          // ← FieldDbType.Date 落在這裡
```

`DateTime` 與 `Time` 都有分支，**獨缺 `Date`**。
[docs/temporal-types.md:38,44-45](../temporal-types.md) 宣稱「宣告 `FieldDbType` 後
layout 層自動推導編輯器，不需改 layout 就有日期挑選器」—— 目前為假。

剛完成「日期一律 `DateOnly`」的改動後，宣告 `DbType="Date"` 的欄位在三端都拿到純文字框。

**修法**：加 `FieldDbType.Date => ControlType.DateEdit,` 並補測試。

### ✅ P0-5. `StringUtilities.Replace` 缺 `CultureInvariant` —— 憑證替換 fail-open

[StringUtilities.cs:189](../../src/Bee.Base/StringUtilities.cs)：

```csharp
var options = ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None;
return Regex.Replace(s, Regex.Escape(search), replacement, options, TimeSpan.FromSeconds(1));
```

該類的 XML 文件通篇在解釋 Turkish-I 為何必須避免，`IsEquals` / `StartsWith` / `Contains` /
`IndexOf` 全部正確使用 `StringComparison.Ordinal`，**唯獨 `Replace` 因改走 Regex 而繞過自家防線**。

下游 12 個生產呼叫端全是識別碼替換，其中：
- [DbConnectionManagerService.cs:78,80,82](../../src/Bee.Db/Manager/DbConnectionManagerService.cs) —— `{@DbName}` / `{@UserId}` / `{@Password}`
- [DatabaseRepository.cs:59,61,63](../../src/Bee.Repository/System/DatabaseRepository.cs) —— 同上

`{@UserId}` 含 `I`；tr-TR 地區設定下 `I` 的小寫是 `ı` 而非 `i`，不敏感比對會失配。
**失配後果不是擲例外，而是佔位符原樣留在連線字串裡** —— `Password={@Password}`
會被當成字面密碼送出。

**修法**：改 `s.Replace(search, replacement, StringComparison.OrdinalIgnoreCase)`
（.NET Core 2.0+ 已有此多載，屬 code-style path A），一併省掉 `Regex.Escape` 與 ReDoS timeout。

---

## P1：安全

四項嚴重發現構成一條鏈：**登入 → 覆寫 FormSchema → 伺服器端運算式求值**。
已獨立驗證，並區分「當前可利用」與「潛伏」。

### ✅ P1-1. `SaveDefine` 無權限模型 ★最高投報比

[SystemBusinessObject.Define.cs:242-249](../../src/Bee.Business/System/SystemBusinessObject.Define.cs)
唯一閘門是排除 `SystemSettings` / `DatabaseSettings`。**未排除** `PermissionModels`、
`ProgramSettings`、`DbCategorySettings`、`TableSchema`、`FormSchema`、`FormLayout`、`Language`。

任何最低權限帳號可覆寫 `PermissionModels.xml`（重寫授權模型 → 提權）、
`DbCategorySettings.xml`（把業務表指向別的資料庫 → 跨租戶）、`TableSchema`（下次 upgrade 產生任意 DDL）。

**對照組證明這是遺漏而非設計**：`FormBusinessObject` 每個 action 都走
`Authorize(PermissionAction.X)`，`LogBusinessObject` 有 `EnsureAuditReadAllowed()`。
只有 `SystemBusinessObject` 的 Define 家族裸奔。

**修復方式（比權限模型更嚴格）**：寫入定義是**部署期作業**，不是應用期作業，
因此整個方法改為 `ApiProtectionLevel.LocalOnly` —— 遠端呼叫一律拒絕，
比任何 token 或權限檢查都嚴格，也不需要新增權限模型。

兩道防線並存（刻意不只靠 attribute）：

1. `[ApiAccessControl(LocalOnly, ...)]` —— 擋 JSON-RPC 遠端流量
2. 方法內 `if (!IsLocalCall) throw` —— 擋其他路徑

**第 2 道是實作過程中由測試抓回來的**：最初只留 attribute 並移除方法內檢查，
4 個測試立刻紅 —— 它們直接 `new SystemBusinessObject(isLocalCall: false)` 呼叫方法，
**繞過 `ApiAccessValidator`**。validator 只在 JSON-RPC dispatch 路徑上執行，
in-process 宿主、自訂 dispatcher 或子類都不經過它。

副作用：`P1-2`（路徑遍歷）與 `P1-4`（運算式沙箱）的實際可達性從
「任何已登入帳號」降為「能發起近端呼叫者」，兩者的優先度隨之下降
（仍應修，但不再是可被一般帳號觸發的路徑）。

連帶更新：`BoApiSurfaceTests` baseline（該測試刻意鎖定所有 `[ApiAccessControl]`
的存取等級，任何鬆緊變更都必須經 review）、`docs/api-method-reference` 雙語、
`ClientDefineAccess.SaveDefineAsync` 的 XML doc（讓遠端呼叫者知道為何失敗）。

### ✅ P1-2. 定義檔路徑無 containment（path traversal）

[PathOptions.cs:57,62,68,74](../../src/Bee.Definition/PathOptions.cs) 皆為裸 `Path.Combine`，
無 `Path.GetFullPath` + root 包含性檢查。兩個破口：`../` 跳出；rooted 路徑讓
`Path.Combine` **直接丟棄前面所有段**。

**對照組同樣證明是遺漏**：[CustomizeOnlyPathOptions.cs:39-53](../../src/Bee.Definition/CustomizeOnlyPathOptions.cs)
對 `customizeId` **有**完整檢查，但同類別的 `GetFormLayoutFilePath` / `GetLanguageFilePath` 仍無。

**修法**：抽 `EnsureWithinRoot(root, resolved)` helper（可直接取用 `CustomizeOnlyPathOptions`
現有邏輯），套用到 `PathOptions` 全部 `GetXxxFilePath`。

### ✅ P1-3. `CreateSession` 匿名發 token（**潛伏，目前被 TODO 擋住**）

[SystemBusinessObject.Session.cs:284](../../src/Bee.Business/System/SystemBusinessObject.Session.cs)
為 `ApiAccessRequirement.Anonymous`；[SessionRepository.cs:101-120](../../src/Bee.Repository/System/SessionRepository.cs)
只 `SELECT sys_id, sys_name FROM st_user WHERE sys_id={0}`，查得到就發 token。

**已驗證的緩解**：`AccessTokenValidator` 走 `ISessionInfoService` →
[SessionInfoCache.cs:22](../../src/Bee.ObjectCaching/Database/SessionInfoCache.cs) 的
`CreateInstance` 回傳 `null`（註解自述「尚未實作 DB 載入」），
因此發出的 token 目前**無法**通過 `Authenticated` 檢查。

**這是一個 TODO 擋住了完整的驗證繞過。** 誰補上那個 DB fallback（正如註解所預告），
漏洞當場成立。**修這項時務必連同 `SessionInfoCache` 的 TODO 一起處理**，否則未來補快取的人
不會知道自己打開了什麼。

### P1-4. DynamicExpresso 不是安全沙箱

[DynamicExpressoEvaluator.cs:32](../../src/Bee.Expressions/DynamicExpressoEvaluator.cs)
用 `InterpreterOptions.Default`。類別 remarks 主張「reflection / file / network 都是 unknown identifier」
—— 這對**靜態型別名稱**成立，對**實例成員存取**不成立：`GetType()` 是 `System.Object` 的公開方法，
任何變數（每個欄位值都是變數）都能起手 `some_field.GetType().Assembly...`。

單獨看風險可接受（運算式來自定義檔＝開發者撰寫）；**串上 P1-1 即為伺服器端任意程式碼執行的實際路徑**。

**修法**：優先修 P1-1 切斷注入路徑；並修正類別註解的安全宣告措辭（目前過於樂觀）；
可選擇在 `GetOrCompile` 前用既有的 `DetectIdentifiers` 做識別字黑名單（`GetType` / `Assembly` / `Invoke`）。

### ✅ P1-5. 安全狀態交出可變參照

- [IPValidator.cs:32,40](../../src/Bee.Base/IPValidator.cs) —— `public List<string> Whitelist` / `Blacklist`
  getter-only 直接交出私有欄位**參照**，ctor 收 `List<string>` 未複製 → 呼叫端保有活體 handle，
  可在建構後改寫安全名單
- [ApiClientInfo.cs:49](../../src/Bee.Api.Client/ApiClientInfo.cs) —— `public static byte[] ApiEncryptionKey { get; set; }`，
  連 setter 都繞得過（陣列內容可就地改寫）
- [UpgradeStage.cs:27](../../src/Bee.Db/Schema/UpgradeStage.cs) —— `public List<string> Statements { get; }`，
  可對已算好的升級計畫注入任意 SQL

**修法**：防禦性複製 + 回傳 `IReadOnlyList<T>`。
（框架已在 82 處正確使用 `IReadOnly*`，這些是不一致而非缺乏慣例。）

### ✅ P1-6. 反序列化白名單的兩個缺陷 ★交叉確認

[SysInfo.cs:66](../../src/Bee.Base/SysInfo.cs)：

1. **文化相依比對**：`typeName.StartsWith(ns + ".")` 未指定 `StringComparison`。
   同檔 line 90-92 建清單時明確用 `StringComparer.Ordinal` 並註解說明理由
   —— **建清單用 Ordinal、查清單用 CurrentCulture**，同檔兩處語意不一致。
   可利用性低（.NET 型別解析本身是 Ordinal），但安全邊界不該取決於伺服器地區設定。
2. **命名空間寫錯**：白名單列 `"Bee.Contracts"`，但全 repo **無此命名空間**；
   實際專案是 `Bee.Api.Contracts`，未列入 → 該組 DTO 放進 `object` 欄位會被擋。

### ✅ P1-7. 其他防禦深度

| 項目 | 位置 | 說明 |
|------|------|------|
| `XmlCodec.Deserialize` 未硬化 | [XmlCodec.cs:51-66](../../src/Bee.Base/Serialization/XmlCodec.cs) | 吃 wire 來的 XML，內部實體展開未禁（billion-laughs DoS）。同 repo 的 `ChangeDiffGramReader.LoadHardened` 已是正解範本 |
| Master key 檔案權限 | [MasterKeyProvider.cs:81-92](../../src/Bee.Definition/Security/MasterKeyProvider.cs) | 預設權限受 umask 影響，Unix 上常為 world-readable。建檔後應 `File.SetUnixFileMode` |
| API key 驗證器不驗值 | [ApiAuthorizationValidator.cs:54-58](../../src/Bee.Api.Core/Authorization/ApiAuthorizationValidator.cs) | 知情的預設不安全（已有啟動警告）。建議非 Development 環境未設金鑰即拒絕啟動 |
| `GetCommonConfiguration` 匿名 | [SystemBusinessObject.cs:48](../../src/Bee.Business/System/SystemBusinessObject.cs) | 匿名回傳 `AllowedTypeNamespaces` 等於免費提供 gadget 搜尋起點 |
| `Login` 允許 Plain | [SystemBusinessObject.Session.cs:24](../../src/Bee.Business/System/SystemBusinessObject.Session.cs) | 密碼可未加密進 JSON body，完全倚賴部署端 TLS |
| `SysInfo.IsDebugMode` public setter | [SysInfo.cs:30](../../src/Bee.Base/SysInfo.cs) | `NoEncryptionEncryptor` 的 guard 正確，但其判斷依據可被任何 in-process 程式碼改寫 |
| 使用者列舉 | [SessionRepository.cs:107](../../src/Bee.Repository/System/SessionRepository.cs) | 訊息含 `UserID='x' not found` 且在 user-facing 白名單內。對照 `EnterCompany` 刻意統一錯誤訊息，此處漏做 |

---

## P2：結構重構

### P2-1. UI head 程式碼複製 ★交叉確認（架構 + 散落兩面向獨立指出）

| 複製項 | 規模 | 實測相似度 |
|--------|------|-----------|
| `Bee.Web.Blazor.Server` ↔ `.Wasm` 元件層 | ~950 / 1,245 行 | 14 個共有檔案中 **10 個 `diff` 輸出 0 行** |
| `FormDataObject` × 4 個 head | ~1,100 行 | Blazor 兩份**逐位元相同**；Maui vs Blazor 87% 相同 |

**關鍵事實：這些檔案完全平台中立** —— using 清單只有 `System.Data` / `System.Globalization` /
`Bee.Api.Client.Connectors` / `Bee.Base.Data` / `Bee.Definition`，
**沒有一行 Avalonia / MAUI / Blazor 專屬相依**。

這不是「不同平台各自實作」，是同一份邏輯被複製到多個獨立發佈的 NuGet 套件。
`FieldDbType.Time` 這類新型別的預設值處理一旦漏改一個 head，就會出現
「同一 FormSchema 在 Blazor 正確、在 MAUI 錯」，且無任何編譯期或測試防護。

**建議順序**：
1. 先抽 `Bee.Web.Blazor.Core` RCL 承接兩個 Blazor head 的 10 個相同檔案（單此一步消掉 960 行）
2. `FormDataObject` 的平台中立成員上移 —— 技術上 `Bee.UI.Core` 已具備全部相依，
   但兩個 Blazor 專案目前**未引用** `Bee.UI.Core`，需先確認這是否為刻意的 family 判別準則

> ⚠️ **與 Avalonia UI 試點移植的時序**：既有規劃是 Avalonia 定稿後移植到 Maui / Blazor。
> **在複製收斂前移植，只會把 4 份變成更多份。** 建議這項排在移植之前。
>
> ⚠️ 新增 `src/` 套件須依 checklist 同步 CI pack 清單、`dependency-map`、README 專案數（雙語）。

### ✅ P2-2. `Bee.Hosting` 承載資料存取實作

[Bee.Hosting.csproj](../../src/Bee.Hosting/Bee.Hosting.csproj) **未宣告** `Bee.Db`，
卻靠 `Bee.Repository → Bee.Db` 遞移取得，且不只 DI 註冊，是實質 SQL 組建與執行：

- [CacheNotifyPollSession.cs:96,114-115,146-150](../../src/Bee.Hosting/CacheNotify/CacheNotifyPollSession.cs)
  —— 自組 `SELECT`、五種方言的時間函式分歧處理
- [AuditLogDbSink.cs:68-88](../../src/Bee.Hosting/Audit/AuditLogDbSink.cs) —— `StringBuilder` 組 `INSERT`

Hosting 959 行中非 composition 的實作碼佔 381 行，其中 291 行含直接資料存取。

**文件層自我矛盾**：`dependency-map` 把 Hosting 畫進「API 層」subgraph，
而 `development-constraints:84` 明文禁止「API 層直接引用 Repository 層」。

**已執行（下沉，非維持現狀）**：

實作時釐清的兩點事實，決定了下沉的切法：

1. **`Bee.Db` 的顯式宣告與下沉與否無關，本來就該補** —— `BeeFrameworkServiceCollectionExtensions`
   自身就 `using Bee.Db` / `Bee.Db.CacheNotify` / `Bee.Db.Manager` 做 DI 註冊。
2. **poll session 不能整個搬** —— 它呼叫 `CacheInfo.NotifyVersions.SetVersion`（`Bee.ObjectCaching`），
   而 `Bee.Db` 不引用 `Bee.ObjectCaching`，整搬會產生一條反向新邊。

因此切法為「SQL 下沉、狀態留下」：

- 新增 `Bee.Db/CacheNotify` 的 `ICacheNotifyReader` / `CacheNotifyReader` / `CacheNotifyChange`，
  承接 baseline 與 delta 查詢及五方言的 `NaiveNowCommandText` / `ThresholdBinding`
  —— 與該 namespace 既有的寫端 `CacheNotifyService` 對稱
- 新增 `IAuditLogWriteRepository`（`Bee.Repository.Abstractions`，純加法，不動既有
  `IAuditLogRepository`）與 `AuditLogWriteRepository`（`Bee.Repository/AuditLog`）
- `CacheNotifyPollSession` 只留 mirror / high-water / 版本發布；`AuditLogDbSink` 只留檔案
  fallback 與 logging。**Hosting 現已零 SQL**，959 → 855 行
- `Bee.Hosting.csproj` 補 `Bee.Db` 顯式 `ProjectReference`
- 五方言分支的反射測試隨實作搬到 `Bee.Db.UnitTests`

文件同步：`dependency-map` 雙語把 Hosting 移出「API 層」改列獨立「組合根」subgraph、補
`Hosting --> Db` 邊、補漏記的 `Hosting.Abstractions`（P3-8 標為完成但仍殘留的一項）；
`development-constraints` 雙語為「API 層不得引用 Repository 層」加上組合根例外，消除既有的
自我矛盾；四個 `src/*/README` 目錄樹補上 `AuditLog/` / `CacheNotify/` / `Storage/`
與遺漏的 MySql / Oracle provider。

### ✅ P2-3. 方言無關邏輯在 5 個 provider 各複製一份

`GetKindForTypeChange` / `IsNarrowing` / `GetFamily` / `GetStringCapacity` /
`IsNumericNarrowing` / `GetNumericRank` 在 `Sql|Pg|MySql|Oracle|Sqlite AlterCompatibilityRules.cs`
各有一份，連 `private enum TypeFamily` 也各自宣告。同構問題另見 `*TableRebuildCommandBuilder`
的 `BuildEffectiveSchema` / `CloneWithTableName`（5 份相同）。

**WHY**：型別家族分類與收窄判定**不碰任何 SQL 語法**，是方言無關邏輯。
目前修一個方言的規則，其他四個靜默留在舊行為 —— 這正是 schema 比對類 bug 最難察覺的形態
（對照剛修的 `9933160d` SQLite Guid 永久 diff）。

**已執行**。實測（去註解後逐行 diff）比原描述還乾淨：PostgreSql / MySql / Oracle 與 SqlServer
**只差 namespace 與類別名**，其餘逐字元相同；SQLite 也只有 `GetKindForTypeChange` 一處真差異。

- 新增 `Bee.Db/Schema/AlterCompatibilityRules`（型別家族分類 + narrowing 判定）與
  `Bee.Db/Schema/RebuildSchemaFactory`（`BuildEffectiveSchema` / `CloneWithTableName` /
  `StripNonPrimaryKeyIndexes`）
- 刪除 Sql / Pg / MySql / Oracle 四份規則複本，由各 `TableAlterCommandBuilder` 直接呼叫共用類
  （依 code-style 不留純委派 facade）；`SqliteAlterCompatibilityRules` 只保留真差異
- 測試由五份（968 行）收斂為共用一份 + SQLite 一份，並補上 `FieldDbType.Time` 在字串家族的
  分類與 narrowing 案例——**同時清掉 P2-9「`Time` 在 `*AlterCompatibilityRulesTests` 0 案例」一項**
- 淨減 1,812 行；雙語 `database-dialect-differences` 的參考路徑同步更新

### ✅ P2-4. 死碼與遷移孤兒

**零使用型別**（皆已 grep 跨 `src/ tests/ apps/ samples/ tools/ docs/` 驗證）：

| 型別 | 位置 |
|------|------|
| `IEnterpriseObjectService` + `EnterpriseObjectService` | [Bee.Definition](../../src/Bee.Definition/IEnterpriseObjectService.cs) + [Bee.ObjectCaching](../../src/Bee.ObjectCaching/Services/EnterpriseObjectService.cs) |
| `enum InitializeOptions` / `enum ApplicationType` / `static class SysFuncIDs` | `src/Bee.Definition/` 根目錄 |
| `class VersionFiles` | [Settings/SystemSettings/](../../src/Bee.Definition/Settings/SystemSettings/VersionFiles.cs) |
| `TreeNodeIgnoreAttribute` | [Bee.Base/Attributes/](../../src/Bee.Base/Attributes/TreeNodeIgnoreAttribute.cs) |
| `DefaultBoolean` / `NotSetBoolean` | `src/Bee.Base/` |
| `SystemActions.GetLocalDefine` / `SaveLocalDefine` | [SystemActions.cs:67,75](../../src/Bee.Definition/SystemActions.cs) |

`IEnterpriseObjectService` 最值得注意：它有完整的架構外觀
（`BackendDefaultTypes` 常數 + `BackendComponents` 設定項 + DI singleton 註冊 +
XML 文件描述「統一存取企業常用商業物件，具快取機制」），但**介面零成員、零解析端**。
框架使用者看到設定項會以為可替換實作來擴充，實際上替換了什麼都做不到
—— 這是唯一會直接誤導**外部使用者**的死碼。

**注意**：這些型別各自帶著「可被建立 / 預設值為 X」的佔位測試，
讓死碼在覆蓋率報告上呈現為已測試。刪型別時須連同佔位測試一起刪。

**遷移孤兒**：[DateTimeExtensions.cs:13](../../src/Bee.Base/DateTimeExtensions.cs) 的 `IsEmpty`
以 `< 1753-01-01` 為判準，但 [DbCommandSpec.cs:217-218](../../src/Bee.Db/DbCommandSpec.cs)
已把 SQL Server 升級為 `DbType.DateTime2` —— **1753 這條線已不再是資料庫限制**。
且同一概念有三種不一致判準（`DateTimeExtensions.IsEmpty`、`ValueUtilities.IsEmpty`、
`FieldDbTypeExtensions` 用 `DateTime.MinValue`）。`DateTimeExtensions.IsEmpty` 本身零呼叫，可直接刪。

**安全程式碼重複**：`AesCbcHmacCryptor.CompareBytes` 與 `FileHashValidator.FixedTimeEquals`
實作逐字相同，且全 repo 對 `CryptographicOperations` 引用數為 **0**。
兩者實作皆正確（安全面向已確認），但依 code-style path A 應直接用
`CryptographicOperations.FixedTimeEquals`。（實際有**三份**——`PasswordHasher.FixedTimeEquals`
本表未列。）

**已執行**（範圍經使用者裁決，非全表照刪）：

| 層級 | 處置 |
|------|------|
| 非破壞修正 | 三處手寫常數時間比對改用 `CryptographicOperations.FixedTimeEquals`（皆為 private，零公開表面變更） |
| 真死碼 | 刪除八個型別 + 三個成員（見下），連同佔位測試、README 條目、`terminology` 詞條 |
| 未消費的抽象 | `IDefineField`（`DbField` 實作）、`IElementCapabilityResolver`（實作被 Avalonia 與 DemoCenter 實際使用）**保留** —— 這是「要不要留擴充點」的判斷題，不是死碼 |
| 刻意擴充點 | `CheckPackageUpdate` / `GetPackage` 全棧**保留** —— base 擲 `NotSupportedException` 供子類覆寫，已列入公開 API 參考文件 |

實刪清單：`IEnterpriseObjectService`、`EnterpriseObjectService`、`InitializeOptions`、
`ApplicationType`、`SysFuncIDs`、`VersionFiles`、`DefaultBoolean`、`NotSetBoolean`、
`SystemActions.GetLocalDefine` / `SaveLocalDefine`、`DateTimeExtensions.IsEmpty`。

連帶：`IEnterpriseObjectService` 移除後，其專屬的 `CreateOrDefault` helper 也成為死碼一併刪除
（其餘可替換服務走 `CreateConfigurableService`，已由 `BeeFrameworkServiceResolutionTests` 覆蓋，
故刪除該佔位測試無覆蓋損失）。

⚠️ **本表一處誤判已更正**：`TreeNodeIgnoreAttribute` **不是死碼** —— 它有 7 處生產用途
（`CollectionItem` / `KeyCollectionItem` / `FormField` / `FormRule` / `FormSchema` /
`MessagePackCollectionItem` / `MessagePackKeyCollectionItem`），原掃描以全名 `TreeNodeIgnoreAttribute`
grep，漏掉 `[TreeNodeIgnore]` 簡寫形式。刪除後 build 立刻失敗，已還原。
**下次體檢對 attribute 型別必須同時 grep 全名與去 `Attribute` 後綴的簡寫形式。**

CHANGELOG 雙語於 Unreleased 記為 compile-time breaking，並註明既有 `SystemSettings.xml`
殘留 `EnterpriseObjectService` 節點載入時會被忽略、不需遷移檔案。

### ✅ P2-5. `Bee.Api.Core` 未宣告 MessagePack

`Bee.Api.Core`（70 個檔案使用）與 `Bee.Api.Contracts`（3 個）皆無 MessagePack
`PackageReference`，靠 `Bee.Definition` 遞移取得。

**WHY**：MessagePack 在此框架是 **wire ABI**。版本語意由 `Bee.Definition` 單方持有 →
Definition 調版本時，`Bee.Api.Core` 的線上序列化行為會**在無本地訊號的情況下改變**；
發佈為 NuGet 後消費端的套件圖也看不出這層硬性契約。與 `Bee.Db` 刻意不引用任何
ADO.NET driver 的謹慎程度不一致。

**修法**：兩個 csproj 顯式加 `<PackageReference Include="MessagePack" Version="3.1.7" />`。
長期可評估引入 `Directory.Packages.props`（目前未啟用 Central Package Management）。

### ✅ P2-6. 契約軸缺漏 + 無自動守門 ★本期回歸

[GetDepartmentTreeRequest.cs:10](../../src/Bee.Api.Core/Messages/System/GetDepartmentTreeRequest.cs)
**未實作任何契約介面**，`Bee.Api.Contracts/System/` 無 `IGetDepartmentTreeRequest.cs`。

其他 5 個同樣無參數的 request 全都有空標記契約介面，證明這是慣例而非「無參數就不需要」。

**更重要的是沒有守門測試** —— 上輪宣稱「100% 對齊」，這次破了也沒人發現。

**修法**：補介面 + 補一支反射測試：`Bee.Api.Core.Messages.*` 內每個 `*Request`/`*Response`
都必須實作同名 `I*` 契約。

### ✅ P2-7. 三棲標籤補齊（低風險批次修）

| 項目 | 位置 |
|------|------|
| `SerializeState` / `ObjectFilePath` / `CreateTime` 缺 `[IgnoreMember]` | `FormLayout`、`MenuSettings`、`DatabaseSettings`、`DbCategorySettings`、`SystemSettings`、`ClientSettings`、`ProgramSettings`、`PermissionModels`（`FormSchema` / `TableSchema` 已補齊 → 家族內不一致） |
| `MasterTable` 缺 `[IgnoreMember]` | [FormSchema.cs:190](../../src/Bee.Definition/Forms/FormSchema.cs) —— 註解說明的理由對 MessagePack 同樣成立，目前 master table 被序列化兩次 |
| `RelationFieldReferences` 缺 `[JsonIgnore]` + `[IgnoreMember]` | [FormTable.cs:143](../../src/Bee.Definition/Forms/FormTable.cs) —— 衍生集合，且 getter 在 schema 不一致時會擲例外 → 「序列化到一半爆炸」 |
| `TimeOnly` 未加入 typeless 白名單 | [SafeTypelessFormatter.cs:40-73](../../src/Bee.Definition/Serialization/SafeTypelessFormatter.cs) —— 有 `DateOnly` / `TimeSpan`，缺 `TimeOnly`。公開 API `ValueUtilities.CTimeOnly` 回傳 `TimeOnly?`，塞進 `FilterCondition.Value` 會被擋 |
| `SessionInfo.Roles` 為裸 `ICollection<string>` | [SessionInfo.cs:89](../../src/Bee.Definition/Identity/SessionInfo.cs) —— 全 `Bee.Definition` 唯一裸集合；XmlSerializer 不支援介面型別屬性 |

### ✅ P2-8. `DataTable` JSON wire 的三處失真

| 問題 | 位置 |
|------|------|
| 字串欄存 ISO-8601 樣式文字會被改寫（`"2026-07-28"` → `"07/28/2026 00:00:00"`） | [DataTableJsonConverter.cs:313,348](../../src/Bee.Base/Serialization/DataTableJsonConverter.cs) —— 應依目標欄位型別決定怎麼讀，不要猜 |
| `decimal` 超過 15 位有效數字靜默失精（未試 `TryGetDecimal`） | 同上 `:317-319` —— **與 ERP round-then-sum 鐵則直接衝突** |
| `double`/`float`→`decimal`、`uint`/`ulong` 溢位、`TimeSpan`/`DateOnly`/`TimeOnly` 擲例外 | [DbTypeConverter.cs:26-58,105-137](../../src/Bee.Base/Data/DbTypeConverter.cs) —— AnyCode 報表的原生 SQL DataTable 很容易帶這些型別 |

### ✅ P2-9. 測試品質

複查時發現前三項在 P0/P1/P3 批次與 UI 收斂期間已順帶修掉（現況：四個
`CollectionDefinition` 都在各自組件內、`ApiServiceOptions` 兩個 class 都已入
`[Collection("ApiServiceOptionsState")]`、`PgDialectFactoryTests` 已改 `[DbFact]`）。
本輪處理其餘四項。

| 項目 | 位置 |
|------|------|
| ✅ `ApiServiceOptions.*` static 被兩個平行 class 修改，無 `[Collection]` | 複查已修：兩個 class 皆在 `[Collection("ApiServiceOptionsState")]`，定義位於同組件 |
| ✅ `[Collection("SysInfoStatic")]` / `[Collection("ClientInfo")]` 無對應 `CollectionDefinition` | 複查已修：`ClientInfoState` / `SysInfoStatic` / `ApiServiceOptionsState` / `ApiClientInfoState` 四個名稱的使用與定義都落在同一組件內 |
| ✅ `PgDialectFactoryTests` 用裸 `[Fact]` 但會實際解析 PG 連線 | 複查已修：改為 `[DbFact(DatabaseType.PostgreSQL)]`，並在原處留註解說明為何 |
| ✅ `FieldDbType.Time` 的 DDL / schema-diff 純單元層 **0 案例** | 全數補齊：`AlterCompatibilityRulesTests`（P2-3）＋五家 `*CreateTableCommandBuilderTests`、`SqlSchemaSyntaxTests`、`SqliteSchemaSyntaxTests`。**補這批時抓到一個生產 bug，見下方 P2-9b** |
| ✅ DST 零覆蓋 | 新增 `DateTimeZoneConverterDstTests`（`America/New_York`）：fall-back 重疊、DST 前後偏移不同、spring-forward 缺口。以 `IsInvalidTime` / `IsAmbiguousTime` 斷言前提，避免 tzdata 不同時以錯誤理由通過。**缺口的語意仍待裁決 → 移入 P4** |
| ✅ 3 處真實牆鐘 sleep | 逐處複查後只有兩處是真的：`MemoryCacheProviderTests:100` 改注入可推進假時鐘（`MemoryCacheOptions.Clock`）、`CacheNotifyServiceTests:97` 的 `Thread.Sleep(50)` 直接移除（斷言是非遞減，sleep 不強化任何事）。另兩處（`MemoryCacheProviderTests:146` 檔案監控、`AuditLogWriterServiceTests:88` 背景排空）**已是「輪詢至條件成立 + deadline」的正確寫法**，原清單誤計 |
| 🔶 `PayloadZoneConverter` 用「列舉具體型別」的 switch | 已新增 `PayloadZoneCoverageGuardTests`：反射掃出所有承載 `DataSet` / `DataTable` / `FilterNode` 的 message 型別（現為 10 個），逐一**實跑轉換並斷言值真的位移**——新增帶資料的型別卻忘了接進 switch 會直接紅。`Parameters`（在 `ApiMessageBase`，**每個** request/response 都有，值為 `object`）是否該轉換屬語意裁決 → **移入 P4** |

### ✅ P2-9b. 補 `Time` 覆蓋時抓到的生產 bug（本輪新增，非原清單）

`SqlCreateTableCommandBuilder` 自帶一份與 `SqlSchemaSyntax` 重複的型別對映
（`ConverDbType`，含 typo），而 4.15.0 的「SQL Server `DateTime` 由 `datetime`
遷移至 `datetime2(7)`」（`7df805f4`）**只改了 `SqlSchemaSyntax`**。

後果是遷移只做了一半：走 ALTER / rebuild 的既有表拿到 `datetime2(7)`，
**全新 CREATE 的表拿到 `datetime`**（約 3.33 毫秒捨入、下限 1753-01-01）。
測試抓不到，因為 `SqlCreateTableCommandBuilderTests` 自己斷言的就是 `[datetime]`。

五家 provider 中只有 SQL Server 留了複本，其餘四家都委派
`<X>SchemaSyntax.GetColumnDefinition`——`SqlSchemaSyntax` 的類別註解甚至已寫明
「used by the CREATE and ALTER schema builders」，但 CREATE 實際上沒在用。

已修：移除該複本（連同 `QuoteName` / `EscapeSqlString` 兩個逐字複本與兩個
`GetDefaultValue` 多載），CREATE 改走 `SqlSchemaSyntax`；CHANGELOG 雙語記為修正。

> **與 P2-3 同一個模式**：方言邏輯留複本 → 改一份、其他靜默留舊行為。
> P2-3 收的是「五份相同」，這裡是「兩份已分岔」——後者更危險，因為分岔本身就是 bug。

---

## P3：文件漂移

文件面向 4.5/10，核心問題不是「沒人寫文件」，而是**寫作與失效的節奏脫鉤**：
新主題文件（`docs/` 根 + ADR）品質高且跟得上，但**跨層改動不回頭改 `src/*/README`**，
且**改名／刪型別時無反向索引**。

### ✅ P3-1. 可編譯性破產（外部開發者第一天就會複製的段落）

| 文件 | 錯誤 |
|------|------|
| `docs/development-cookbook.md:494-537`（zh `:490-533`） | 桌面章節**一段範例四個 API 全錯**：`ShowApiConnect()`→`ShowApiConnectAsync()`、`ClientInfo.Initialize()`→`InitializeAsync()`、`PingAsync()` 回 `Task` 卻賦值、`SetEndpoint()`→`SetEndpointAsync()` |
| `docs/development-cookbook.md:556,612,669-670`、`docs/terminology.md:363-364` | `AddBeeWebBlazorServer()` / `AddBeeWebBlazorWasm()` **全 repo 零出現**；實際兩包都是 `AddBeeBlazor(...)` |
| `docs/architecture-overview.md:395,402`、`src/Bee.Db/README.md:46,85,89,93,140` | `DbProviderManager.RegisterProvider(...)` 不存在；實際是 `DbProviderRegistry.Register(...)`。同段的 `DbDialectRegistry.Register` 卻是對的，讓錯的那半看起來可信 |
| `docs/terminology.md:281` | `DatabaseType.SqlServer` / `MySql` / `PostgreSql` —— 實際是 `SQLServer` / `MySQL` / `PostgreSQL`，**大小寫不符即不編譯** |
| `docs/terminology.md` 另 8 處 | `ComparisonOperator.Equals`→`Equal`、`IFilterNode`→`FilterNode`、`FilterNodeType`→`FilterNodeKind`、`FormMode`→`SingleFormMode`、`TableRole` 不存在、`ILogWriter`/`LogEntry`/`ConsoleLogWriter`/`NullLogWriter`/`LogEntryType` 皆已刪除、`SchemaUpgradeAction` 不存在、`IApiProvider`→`IJsonRpcProvider`、`DefineType` 少列 3 個值 |

### ✅ P3-2. 設計之錄（ADR / constraints）失真 ★交叉確認

| 文件 | 問題 |
|------|------|
| `docs/adr/adr-017`、`adr-018` | 要求實作 `IEvictableCache`、呼叫 `ICacheContainer.TryEvict(cacheKey)` —— 三者在 `src/` **皆零出現**（`c45ff350` 已移除）。實際機制是版本發布 + lazy 過期。**adr-017 是快取失效的設計之錄，§4 還被標為核心不變式** |
| `docs/adr/adr-010:155,181`、`adr-016` | `LocalDefineAccess` → 實際 `CacheDefineAccess`；`RemoteDefineAccess` → 實際 `ClientDefineAccess`。`adr-010:181` 給的是完整限定名，且兩處都在**元件位置對照表** |
| `docs/adr/adr-006:5,17,33` | 狀態誤標「已採納」，`## 決策` 仍以現在式陳述已推翻的雙目標框架決定。33 份 ADR 中唯一未依既有慣例標 `已取代（Superseded）` 的反轉案例 |
| `docs/adr/adr-013:25,30,70` | `ShowApiConnect`、`IApiProvider` |
| `docs/adr/adr-027:14,29` | `ILogWriter` / `LogEntry` 已刪除 |
| `docs/development-constraints.md:24` | 「所有 `DbAccess` ctor 都需 `IDbConnectionManager`」—— `DbAccess(DbConnection, DatabaseType, int)` 不需要，框架自己就在用 |
| `docs/development-constraints.md:124-134` | 例外白名單漏了 `ForbiddenException` → `-32004` 且保留原訊息 → 用戶端錯誤處理會誤判 |
| `docs/development-cookbook.md:32-33` vs `:562` | `UseBeeFramework()` 描述自相矛盾，且**兩處皆錯**：既非 no-op、也不註冊任何 middleware/endpoint。`POST /api` 來自 `ApiServiceController` + `MapControllers`，而 Blazor Server 範例完全沒提這一步 → 照抄的 host 沒有 API endpoint |

### ✅ P3-3. 53 條死連結（GitHub 上直接 404）

| 數量 | 內容 | 修法 |
|:---:|------|------|
| **40** | `docs/changelogs/*.md`（14 檔）路徑寫成 repo-root 相對，從 `docs/changelogs/` 解析成 `docs/changelogs/docs/...` | **純機械修正，一次 sed 加 `../` 前綴可清** |
| 10 | 指向已刪除的 `docs/archive/`（`e87ec159` 下架）：`adr-014`、`adr-015`、`adr-017`、`adr-018`、`adr-019` | 永久死連結，需刪除或改指 |
| 3 | 檔案已搬移：`adr-007:43`（`ApiOutputConverter.cs` 在 `Conversion/`）、`samples/Avalonia.Demo/README*.md:59`（`FormView.cs` 在 `Views/`） | 改路徑 |

> `time-semantics` 殘留檢查**零命中** —— `4a2a327c` 的四處呼叫點都已正確改指 `temporal-types*.md`。

### ✅ P3-4. CHANGELOG 未記 6 項 breaking + 一條已被推翻的敘述

`src/Directory.Build.props` = **4.15.0**（注意：`.claude/CLAUDE.md` 寫的 4.13.0 已過期，應一併更正），
tag `v4.15.0` 之後有 63 個 commit，其中 6 個標 `!`，但 `CHANGELOG.md` / `.zh-TW.md`
**完全沒有 Unreleased 區段**。

另 `CHANGELOG.md`（4.15.0 Changed 段）與 `docs/changelogs/4.15.0.md` 明寫
「Deliberately excluded (kept on integer keys): `SerializableData*`」，
但 HEAD 已是 `keyAsPropertyName: true`（`d64decf9`）→ **兩份公開文件互相矛盾**。ADR-030 已修正，CHANGELOG 未修正。

### ✅ P3-5. 發版必辦：6 組實質 breaking 但 commit 未標 `!`

`/changelog-draft` 依 commit 前綴掃描，以下會**全數漏掉**：

| # | commit | 破壞內容 | 型態 |
|---|--------|---------|------|
| 1 | `c45ff350 refactor(caching)` | 移除 public `IEvictableCache`、`ICacheContainer.TryEvict`；`KeyObjectCache<T>`/`ObjectCache<T>` 不再實作該介面 | 編譯錯誤 |
| 2 | `b759894f feat(business,repository)` | `IDataFormRepository.GetNewData()` 加參數 | 實作端編譯錯誤 + binary |
| 3 | `5f28f9a3 feat(expressions,business,ui)` | `IFormRuleProcessor` 五個成員 + `IExpressionEvaluator.Evaluate` 加參數 | 實作端編譯錯誤 + binary |
| 4 | `bb5e4473` + `f7459cc2` | `IDefineStorage` 新增成員 `GetChangeSource` | 實作端編譯錯誤 |
| 5 | `122184e4` / `52ddb24a` / `9aadf9eb` / `1eb0e09f` / `11aedcc4` / `c990aa9e` | 系統時間戳、快取到期、session 到期、DB DEFAULT、PG 參數層**全面改 UTC** | **靜默行為變更（最危險，無編譯訊號）** |
| 6 | `f028ba04` | `FormRowDefaults.Apply` / `FieldDbTypeExtensions.DefaultForDbType` 加預設參數；`Today()` 運算式回傳 `DateTime`→`DateOnly` | binary + 運算式行為 |

> **`IEvictableCache` 是上輪 `IExcelHelper` 失效模式的再次發生** —— 同一個把關缺口第二次漏過。
> 這已不是單點疏失而是流程缺口：commit 前綴是 changelog 的唯一來源，
> 卻無任何機制檢查「public surface 有刪改時 subject 必須帶 `!`」。
>
> ~~**建議引入 `Microsoft.CodeAnalysis.PublicApiAnalyzers` 的 `PublicAPI.Shipped.txt`**，
> 讓漏標 `!` 變成 build 失敗而非人工把關。這是本輪最高槓桿的單一改善。~~
>
> **✅ 已導入（2026-07-28）**：16 個 `src` 專案全數納入，以當時的 HEAD 為 Shipped baseline
> （5,387 行），維運程序見 `docs/repo-ops/public-api-baseline.md`。
> 實測有效——把 `DateTimeExtensions.GetYearMonth` 由 public 改為 internal，build 立即以
> `RS0017` 失敗並指到 baseline 的確切行號。
>
> 導入時順帶暴露一個**驗證面缺口**：`tools/` 既不在 `Bee.Library.slnx` 內，也不在
> `build-ci.yml` 的 path filter 內，因此 P2-4 刪除 `EnterpriseObjectService` 時
> `DefineEditor` 的 axaml 綁定殘留，本機與 CI 都不會紅（`96802256` 修正）。
> **刪除公開型別時須一併建置 `samples` / `tools` / `apps` 三個方案。**

已正確標記（無需補）：`ba56cef0`、`50a2e7d8`、`49641789`、`d64decf9`、`c5578a42`、`d3c6e1bc`。

### ✅ P3-6. `src/*/README` 系統性腐化

17 份中至少 12 份含不存在的型別或錯誤目錄樹。重點：

- `src/Bee.Api.Client/README.md` —— `SyncExecutor` 已於 4.11.0 移除，仍有專屬章節與目錄樹項（8 處）；
  `:27`「Every async method has a synchronous counterpart」為假
- `src/Bee.Db/README.md` —— `DbFunc`（`*Func` 家族已移除）、`DbConnectionManager`（靜態 shim 已移除）、
  整個「Logging & Diagnostics」章節與 `Logging/` 資料夾**皆虛構**
- `src/Bee.Base/README.md:73` —— 「`#if NETSTANDARD2_0` 條件編譯」，
  但無任何專案 target netstandard，且與同檔 `:15`「Target Framework: net10.0」**直接矛盾**
- `src/Bee.Business/README.md:27,50` —— `ISystemBusinessObject` 記為 3 個操作，實際 7 個
  （漏 `Login` / `EnterCompany` / `LeaveCompany` / `Logout` —— 多租戶 company context 全數未記載）
- 目錄樹普遍過時：`Bee.Business` 漏整個 `AuditLog/` 16 檔（`ILogBusinessObject` 這第三種 BO 原型完全不可見）、
  `Bee.Definition` 漏 4 個資料夾與約 20 個 root 檔、`Bee.Repository.Abstractions:19-22` 宣稱 2 個 repository 合約實際 9 個
- `src/Bee.Expressions/` **完全沒有 README**（唯一無文件的已發布 src 套件）

### ✅ P3-7. 本輪大改動在 `src/*/README` 是**完全空白**

無任何 README 提及：`DateTimeZoneConverter`、`PayloadZoneConverter`、`ApiClientInfo.UserTimeZoneId`、
`st_user.time_zone`、`FrameworkClock`、`FieldDbType.Time`、`DateOnly` 慣例、
`SerializableData*` 的 property-name key 轉換。

特別嚴重：`src/Bee.Db/README.md`（200 行、逐 provider 講 DDL/DML）**一次都沒提 `Time` 的各家對映**，
而 `:45` 又要讀者實作 `GetDefaultValueExpression(FieldDbType)`
→ 自訂 dialect 的人不會知道多了一個列舉成員要處理。

另 `src/Bee.Api.Contracts/README.md:70` 仍把通則寫成「使用 `[MessagePackObject]` 與 `[Key(n)]`」（整數鍵形式）。

四份 UI README 的 ControlType 清單漏 `TimeEdit`，且措辭宣稱窮舉
（`Bee.UI.Avalonia/README.md:23`「One per `ControlType`」）→
讀者結論「三端都不支援時刻編輯」，恰與階段 3 剛完成的成果相反。

### ✅ P3-8. 其他文件項

- `docs/README.md:15` / zh 寫「17 個 `src/` 專案」，實際 18（同 repo 的 `dependency-map:5` 寫 18 → 索引與被索引文件自打嘴巴）
- `dependency-map` 外部套件表 4 處與 csproj 不符（`FileProviders.Physical` 已不存在、
  漏 `Hosting.Abstractions`、`Bee.UI.Maui` 標 *(none)* 實際有 `Microsoft.Maui.Controls`、
  Blazor.Server 實為 `FrameworkReference`）。**相依圖本身 20 條邊逐條核對完全正確**
- `docs/terminology.md:240,242` 把 `CDateOnly`/`CDateTime` 的回傳寫成非 nullable（實際 `DateOnly?`/`DateTime?`），
  中間夾著正確的 `CTimeOnly → TimeOnly?`；`docs/date-semantics.md:28` 與同檔 §5 **自我矛盾**
- `docs/date-semantics.md:8,68` 把未發布功能歸給 v4.15（實測 tag 不含）
- `docs/development-cookbook.md:455` 與 `adr-032:254-260`（D9）要求「把資料庫伺服器設為 UTC」已過時
  —— 實際已改由 `GetDefaultValueExpression` 取值，五種 dialect 全部回 UTC，伺服器時區無關
- `docs/datetime-timezone.md:22` SQLite `DateTime` 列 `TIMESTAMP`/`TEXT`，實際宣告型別為 `DATETIME`
- `src/Bee.Api.Core/README.md:51,68,78` 把三個 `internal` 型別列為 Key Public API
- **無 ADR 索引**：33 份 ADR 零份被列出，`docs/adr/` 亦無 index 檔
- `docs/expression-rules.md` 是 `docs/` 下唯一單語文件，且未列入索引 → 形同孤兒
- `docs/development-constraints.zh-TW.md:18` anchor 壞（照抄英文 anchor），全 repo 94 個 anchor 中唯一一個
- 4 份應有雙語卻只有單語：`expression-rules`、`Avalonia.DemoCenter`、`Northwind.Browser`、`DefineEditor`

---

## ✅ P4：觀察 / 待裁決

**全部已裁決**。下表右欄記錄裁決結果；命名類 breaking 依裁決排入下一 major，
未在本輪執行。

| 項目 | 說明 |
|------|------|
| ✅ **保留** —— BO 軸介面零 production 消費者 | `IFormBusinessObject` / `ISystemBusinessObject` / `ILogBusinessObject` 的 `CreateFormBO`/`CreateSystemBO` 呼叫端只有測試。規則本身**無違反**（純 API 方法確實都在具象類別），但按規則字面，一個零跨 BO 消費者的介面正是規則要防的東西。`ILogBusinessObject` 已有誠實註記。需裁決：保留為文件化擴充點，或收斂到具象類別直到出現第一個真實消費者 |
| ✅ **保留 + 標明刻意為空** —— `IReportFormRepository` 空介面 | 有文件說明（`terminology.md:158` 的 AnyCode 策略），屬刻意擴充點。但空介面讓擴充點無法真正使用，且 6 支測試被迫寫 `NotSupportedException` stub。建議**保留但在 XML 註解標明「刻意為空」**；若一年內仍無成員則連同工廠方法移除 |
| ✅ **改 ADR 反映實情** —— `ApiServiceOptions` 與 ADR-011 矛盾 | adr-011 把 5 個 ambient singleton 列在「已移除的靜態 facade」表中，但程式碼仍在且在每次請求的熱路徑上。需擇一修正 |
| ✅ **保留分岔 + 雙向註解** —— `MessagePackCollectionBase` vs `CollectionBase` | 去除註解後 diff 僅 10 行。分岔很可能是刻意的（避免 `Bee.Base` 相依 MessagePack），**但兩個檔案都沒有一句話說明**。建議保留分岔 + 互相指名的雙向註解 |
| 📌 **排下一 major**（180 個呼叫點）—— `PermissionAction` 為 `[Flags]` 卻用單數名（S2342） | 其餘 5 個 `[Flags]` enum 全合規。更名屬 breaking，建議排入下一 major |
| 📌 **排下一 major**（24 個呼叫點）—— `FileUtilities.FileWriteText`/`FileReadText` 命名 stutter | 20 個呼叫端，breaking，同上排 major |
| ✅ **XML doc 已清（僅剩 0 行）**；in-body 16 行 + 31 個 `#region` 併入「碰到再改」—— 中文註解殘留 | 17 行 in-body + 31 個 `#region` + **1 行中文 XML doc**（`BeeBlazorServiceCollectionExtensions.cs:21`，會出現在外部使用者 IntelliSense，優先度最高） |
| ✅ **維持不做專案式清理** —— 99 處 WHAT-not-WHY 註解 | 集中在較舊核心套件。**不建議專案式清理**（改動面大、review 訊噪比低），碰到該檔案時順手刪。對照組：`FieldDbTypeExtensions.cs` 等新程式碼的註解已是範本水準（含 ADR 引用）—— 趨勢向好 |
| ✅ **已完成** —— `PayloadSwap` 拆出獨立檔；`DbAccess` 本就已有 `.Async` 分檔 | 純組織重構，零行為變更，可獨立排程 |
| ✅ **前推一個 DST 差** —— DST spring-forward 缺口語意 | `DateTimeZoneConverter.UserToUtc` 對不存在的本地時刻（如 `America/New_York` 2026-03-08 02:30）擲 `ArgumentException` 並原樣穿透 JSON-RPC。使用者用日期選擇器輸入 02:30 是正常操作。需裁決：前推一個 DST 差、退回標準時間、或轉為具名的框架例外。現況已有測試記錄（`DateTimeZoneConverterDstTests`） |
| ✅ **維持不轉，寫進 adr-032 D4 與 XML doc** —— `ApiMessageBase.Parameters` 是否時區轉換 | `Parameters` 在每個 request/response 上，值為 `object`，`PayloadZoneConverter` 從不處理。難處在於無型別標記可分辨「瞬間 / 日曆日 / 系統時間戳」——全轉是猜測，會破壞刻意放 UTC 的參數。需裁決語意（例如要求 AnyCode 呼叫端自行轉、或引入標記） |

---

## 回歸基準（下次體檢對照用）

### 本次驗證為乾淨（維持則無回歸）

**架構與相依**
- 無循環相依（18 專案 8 層單向拓樸排序成功）
- `Bee.Business` 零 `using Bee.Db`
- 後端專案零 `Bee.Api.Client` 引用（`Bee.Web.Blazor.Server` 為前端 RCL，不算違反）
- `Bee.Repository.Abstractions` 未被繞過（唯一 `using Bee.Repository` 在 composition root）
- `Bee.Api.Contracts` 零方法主體、零商業邏輯
- 前端六專案對後端組件的 `using` 全數為零
- Domain Core 無基礎設施夾帶；`Bee.Db` 零 ADO.NET driver 相依
- dependency-map mermaid 圖 31 條邊與 csproj 100% 吻合

**序列化**
- Newtonsoft.Json 殘留 **0**
- MessagePack item ctor 順序 landmine 未觸發
- `[Union]` ⊥ keyAsPropertyName 遵守（僅 `FilterNode` 帶 `[Union]`，維持整數 `[Key]`）
- `MessagePackCollectionBase<>` 8 個子型別與 formatter 註冊 **一一對應無遺漏**
- `d64decf9` 的 `SerializableData*` 轉換品質高（無 `[Union]`、有無參數 ctor、22 個 `[Key(n)]` 全移除）
- ILLink descriptor wildcard 自動涵蓋近期新增型別

**維護性**
- `CurrentCultureIgnoreCase` / `StringComparer.CurrentCulture` **0 處**
- `.ToLower()`/`.ToUpper()` 無 Invariant **0 處**（37 處全為 Invariant 版）
- `class *Func` / `class *Helper` 在 `src/` **0 處**
- 空 class（S2094）除 `EnterpriseObjectService` 外 0 處；未 sealed private nested 0 處
- `*Exception` 未繼承 `Exception` 0 處；`enum : int` 0 處；S101 違反 0 處
- `Regex` 未傳 timeout **0 處**
- **`new DateTime(` 未指定 `DateTimeKind` 0 處** —— 在剛經歷 5 個 commit 的時間型別重構後仍維持，值得肯定
- 資料夾↔命名空間：18 專案僅 `Bee.Definition/Settings/` 落在文件化例外內
- 註解掉的舊 code（S125 真陽性）**0 處**

**安全**
- SQL 注入 **0**（100+ 處 SQL 組建全走 `{0}` 佔位符 + `QuoteIdentifier`，5 個 dialect 逃逸實作全正確）
- 常數時間比對 100%（零 `SequenceEqual`/`==` 比對雜湊）
- `System.Random` 用於安全用途 **0**
- `throw ex;` **0**；`catch (SystemException)` **0**
- 硬編碼機密 **0**
- TLS 憑證驗證繞過 **0**
- 存取控制 fail-closed（無 attribute 一律拒絕）；attribute 繼承鏈正確
- typeless 反序列化**雙層**白名單；Gzip 50 MB 上限；HTTP body 10 MB 限制
- `DeleteCommandBuilder` 拒絕產生無 WHERE 的 DELETE

**公開 API / 測試 / 文件**
- public 可變欄位 **0**（全部為屬性）
- `[Obsolete]` 遺孤 **0**
- `FieldDbType.Time` **append 至 enum 尾端**（非中間插值），且 `<remarks>` 已把約束寫進 API 文件
- 契約軸命名空間↔資料夾 **100% 對齊**（62 契約檔 + 64 wire 檔零錯位）
- BO 介面純度規則無違反
- XML doc 覆蓋 **99%**（3,663 個 public 宣告僅 43 個缺）
- `[DisplayName]` 覆蓋率 **100%**（3,884/3,884）
- **fixture 污染已架構性根治**：`TestProcessBootstrap` 改為複製 `tests/Define` 到 per-process temp
- 時區測試期望值全部由 `TimeZoneInfo` 動態推導、**零寫死偏移量**
- 安全邏輯測試覆蓋 **11/11**
- **公開文件零 `docs/plans/` 引用**（本輪唯一 pass/fail 硬規則，通過得很乾淨）
- 雙語結構同步：`docs/` 19 對 + `src/` 17 對 + `changelogs/` 14 對，標題／表列數全對齊，**無單邊更新落差**
- `time-semantics` 殘留連結 **0**

### 已知不乾淨（具體清單，取代「死碼 0」這類斷言）

~~死碼：`IEnterpriseObjectService`、`EnterpriseObjectService`、`InitializeOptions`、`ApplicationType`、
`SysFuncIDs`、`VersionFiles`、`TreeNodeIgnoreAttribute`、`DefaultBoolean`、`NotSetBoolean`、
`SystemActions.GetLocalDefine`、`SystemActions.SaveLocalDefine`、`DateTimeExtensions.IsEmpty`、
`IDefineField`、`IElementCapabilityResolver`、`CheckPackageUpdate`/`GetPackage` 全棧（12 檔）~~

**上表已於 P2-4 處理完畢**，剩餘的「已知不乾淨」縮減為下列**刻意保留**項，
下次體檢應視為基準而非新發現：

| 保留項 | 理由 |
|--------|------|
| `IDefineField` | `DbField` 實作它；屬未被消費的抽象而非死碼 |
| `IElementCapabilityResolver` | 實作 `ElementCapabilityResolver.Default` 被 `Bee.UI.Avalonia` 與 `Avalonia.DemoCenter` 實際使用，README 記為擴充點 |
| `CheckPackageUpdate` / `GetPackage` 全棧 | base 擲 `NotSupportedException` 的刻意擴充點，已列入 `docs/api-method-reference` 與 `jsonrpc-frontend-integration` |
| `DateTimeExtensions.GetYearMonth` | 零生產呼叫端，但不在本輪裁決範圍內；下輪可一併評估 |

**這些全部早於上次體檢**（各代理已用 git 逐項驗證最後異動日：2026-04-08 至 2026-05-23），
非本期回歸，而是上次基準判定過於樂觀。

---

## 建議執行順序

| 順位 | 內容 | 理由 |
|:---:|------|------|
| 1 | **P0-2 修 `TestFunc`** | 在動任何序列化程式碼**之前**先恢復驗證能力，否則後續修改沒有安全網 |
| 2 | **P0-1 定義類 wire** | 已實測確認的沉默資料遺失；修法需先裁決 A/B/C |
| 3 | **P1-1 `SaveDefine` 授權** | 單獨修好即可把 P1-2 / P1-4 的可達性從「任何登入使用者」降到「管理員」，投報比最高 |
| 4 | **P0-3 / P0-4 / P0-5** | 各為一到數行的修正，但 P0-3 需產品裁決 |
| 5 | **P1-2 / P1-3 / P1-5 / P1-6** | 安全批次；P1-3 須連同 `SessionInfoCache` 的 TODO 一起處理 |
| 6 | **P3-4 / P3-5 發版把關** | 若近期要發版，這兩項必須先做；並評估引入 `PublicAPI.Shipped.txt` 根治 |
| 7 | **P3-3 的 40 條機械死連結** | 一次 sed 可清，成本最低的文件收益 |
| 8 | **P2-1 UI head 收斂** | **須在 Avalonia → Maui/Blazor 移植之前**，否則 4 份變更多份 |
| 9 | P2 其餘 + P3 其餘 | 依團隊節奏排程 |
| 10 | P4 | 多數需使用者裁決或排入下一 major |

**修掉 P0 全部 + P1-1 後，預估綜合可回到 8.5–8.8；再完成 P2-1 / P2-4 與 P3 文件批次，可上 9.0+。**

> **執行結果**：P0 / P1 / P2 / P3 / P4 全數完成。P4 的兩項命名 breaking 依裁決排入
> 下一 major，其餘就地落地。
>
> 執行過程另外抓到**兩個原清單沒有的生產 bug**，都屬「同一份邏輯留了複本、改了一份」
> 的同型問題：
>
> 1. **SQL Server 全新 CREATE 的 `DateTime` 仍是 `datetime`**（P2-9b）——
>    4.15.0 的 `datetime2(7)` 遷移只改了 `SqlSchemaSyntax`，漏掉
>    `SqlCreateTableCommandBuilder` 自帶的型別對映複本。
> 2. **`DefineEditor` 的 axaml 綁定殘留**——P2-4 刪型別後，`tools/` 既不在
>    `Bee.Library.slnx` 也不在 CI path filter 內，本機與 CI 兩邊都不會紅。
>
> 兩者共同的教訓是**驗證面要涵蓋複本與下游方案**，已分別寫入 P2-9b 與 P3-5 段落。
