# 計畫：框架全面 review 與重構路線圖

**狀態：🚧 進行中（2026-07-23）**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| P0 | 正確性／功能風險（wire 反序列化、識別碼比對文化、空洞測試） | ✅ 已完成（2026-07-23） |
| P1 | 安全與序列化標籤一致性 | 📝 待做 |
| P2 | 結構重構（Bee.Definition 職責拆分、大檔／一檔多型別） | 📝 待做 |
| P3 | 文件漂移修正 | 📝 待做 |
| P4 | 觀察／待你裁決（慣例豁免、次要補測） | 📝 待討論 |

## 背景

針對 18 個 `src/` 專案（約 6.4 萬行、964 個 `.cs`）做八面向唯讀 review：架構分層、安全性、維護性、散落／不必要類別、相依分層與循環相依、序列化一致性、公開 API 表面、測試品質與覆蓋缺口。由六個平行子代理分面向掃描（非抽樣，grep/glob 全量），交叉去重後彙整本文件。**本次 review 全程未修改任何程式碼**。

## 總評

**框架整體健康度高。** 架構紀律良好（無循環相依、無反向依賴、雙軌約束與「Server 不依賴 Client」皆守住）；核心安全機制扎實且 fail-closed（加密順序正確、常數時間比較、SQL 全參數化、存取驗證在解密前、未標註 `[ApiAccessControl]` 一律拒絕）；契約軸命名空間↔資料夾 100% 一致、BO 介面純度無違反；`*Func` 靜態類別殘留 0、空 class 0、死碼 0、Newtonsoft.Json 0。

**須處理的重點集中在少數幾個主題**，其中兩項為潛在正確性風險（一項並由兩個獨立代理交叉確認）：

1. **`FilterCondition.In()` 走 MessagePack wire 會反序列化失敗**（功能性 bug，零測試覆蓋）
2. **識別碼型字串比對誤用 `CurrentCulture`**（Turkish-I 正確性風險，散落 4 處主題共 17 個位置）
3. **一個序列化 round-trip 測試名實不符**（只驗 NotNull 不驗還原值，會遮蔽資料遺失）

其餘為安全規範一致性、序列化標籤補齊、結構重構與文件漂移。

---

## P0 — 正確性／功能風險（應優先）

### P0-1. `FilterCondition.In()` 集合值不在 typeless 白名單 → MessagePack 反序列化擲例外

- **位置**：`src/Bee.Definition/Filters/FilterCondition.cs:52`（`object? Value`）、`:117-120`（`In` 工廠）、`src/Bee.Definition/Serialization/SafeTypelessFormatter.cs:40-62`、`src/Bee.Base/SysInfo.cs:64-69, 92-99`
- **問題**：預設 wire 序列化器是 **MessagePack**（`src/Bee.Api.Core/ApiServiceOptions.cs:15`）。`FilterCondition.Value` 型別為 `object`，走 `SafeTypelessFormatter`。`In()` 會把 `Value` 設成 `List<object>` / `object[]`，但白名單（`AllowedPrimitiveTypes` 與 `SysInfo.IsTypeNameAllowed`)**不含** `System.Object[]` 與 `List<object>` → client 送 `In` 篩選給 `GetList` 走 MessagePack，server 端反序列化直接擲 `InvalidOperationException`。全 `tests/` **沒有任何 `ComparisonOperator.In` 的 MessagePack round-trip 測試**，此路徑零覆蓋。
- **建議**：
  1. 先加一個 `GetListRequest` 帶 `FilterCondition.In(...)` 的 MessagePack byte round-trip 測試坐實。
  2. 修法傾向給 `FilterCondition` 增設**強型別** `Values` 欄位（如 `List<string>` 或專用集合）承載 In 清單，徹底避開 `object` typeless 白名單與 AOT 反射風險；次選才是把白名單納入 `System.Object[]`。
- **嚴重度**：高（功能無法運作 + 影響行動端 AOT wire）

### P0-2. 識別碼型字串比對誤用 `CurrentCulture`（Turkish-I 正確性風險，主題散落 4 處）

> 同一主題,`code-style.md` 明訂「string 當 key 且不區大小寫 → `OrdinalIgnoreCase`,禁止 `CurrentCultureIgnoreCase`」。土耳其地區設定下 `ID` ≠ `id`，對「跨 wire 傳遞、可能在不同 locale 反序列化」的集合是實質正確性風險。

- **P0-2a（雙代理交叉確認，最優先）**：`src/Bee.Definition/Collections/MessagePackKeyCollectionBase.cs:25` 用 `CurrentCultureIgnoreCase`，其孿生基底 `src/Bee.Base/Collections/KeyCollectionBase.cs:21` 用 `OrdinalIgnoreCase`。兩個本應行為一致的 keyed collection 基底比對規則不一致（承載 FormSchema／`ParameterCollection` 等定義集合，key 是欄位名／參數名／ProgId）。→ 改 `OrdinalIgnoreCase`。
- **P0-2b**：`src/Bee.Base/StringUtilities.cs` 的 `Contains`(108-109)、`StartsWith`(122-124)、`EndsWith`(137-139)、`IndexOf`(153-155)、`LastIndexOf`(169-171)用 `CurrentCulture*`，但同類別 `IsEquals`(83-86)已正確用 `Ordinal` 並掛 `<remarks>` 說明識別碼比對原則 → 5 方法改 `Ordinal`/`OrdinalIgnoreCase`。
- **P0-2c**：`src/Bee.Base/StringExtensions.cs` 的 `SplitLeft`(24)、`SplitRight`(40)、`LeftCut`(63)、`RightCut`(77)切割 delimiter/prefix/suffix（識別碼型操作，如切 `progId.action`、去 `ref_`/`sys_` 前綴）用 `CurrentCultureIgnoreCase` → 4 處改 `OrdinalIgnoreCase`。
- **P0-2d**：Db provider 型別名正規化用 `.ToLower()`/`.ToUpper()`（current culture）拿去 `switch` 比對 DB 型別識別碼，Turkish locale 下 `"INT".ToLower()` 產生帶點 `ı` 使比對落空 → 7 處改 invariant：`PgTableSchemaProvider.cs:259,325`、`OracleTableSchemaProvider.cs:380`、`SqliteTableSchemaProvider.cs:288`、`MySqlTableSchemaProvider.cs:258`、`SqlTableSchemaProvider.cs:265,312`。
- **嚴重度**：中高（多數 locale 不觸發，土耳其等 locale 觸發正確性 bug；P0-2a 另有 wire round-trip 不對稱風險）

### P0-3. `DefinitionSerializationTests` round-trip 只驗 NotNull，名實不符

- **位置**：`tests/Bee.Definition.UnitTests/DefinitionSerializationTests.cs:19-37`（helper `SerializeObject<T>`），被 7 處呼叫
- **問題**：`[DisplayName]` 宣稱「應正確還原」，但 helper 序列化後只 `Assert.NotNull(value2)`，**完全不比對還原值**。某欄位序列化時整個遺失、或 XML 順序錯亂，只要 deserialize 不回 null 就綠燈。尤其 `SerializeSystemSettings` 涉及 `SecurityKeySettings`（安全型別）卻不驗 key 值是否存活。正確範本見 `tests/Bee.Api.Core.UnitTests/TestFunc.cs:16`（逐屬性 `Assert.Equal`）。
- **建議**：helper 補還原值等價比對（可比較型別 `Assert.Equal`；複雜型別比對 `XmlCodec.Serialize(original) == XmlCodec.Serialize(roundTripped)`），或改用 `TestFunc` 同款逐屬性比對。
- **嚴重度**：中（測試假綠燈，遮蔽序列化資料遺失）

---

## P1 — 安全與序列化標籤一致性

### 安全

- **P1-1 預設 API Key 驗證器僅檢查「非空」**：`src/Bee.Api.Core/Authorization/ApiAuthorizationValidator.cs:44` 僅 `IsNullOrWhiteSpace`，任何非空字串即通過 → `X-Api-Key` 在出廠預設下不構成鑑別閘門（設計為由部署端覆寫但框架未強制）。建議：預設對照合法金鑰集合做**常數時間比較**；或啟動時未覆寫且未設金鑰即記錄明確警告；XML 註解／docs 標示「正式環境必須覆寫或設定」。
- **P1-2 `catch (Exception)` 廣捕 16+ 處**：牴觸 `scanning.md`。實務上皆重拋或包裝（非靜默吞噬），風險受限，屬規範一致性。位置含 `JsonRpcExecutor.cs:120`、`ApiPayloadTransformer.cs:26,56`、`ApiServiceController.cs:103,150`、`ApiConnector.cs:77`、`SystemApiConnector.cs:93`、`DbAccess.cs:328,339,740,755` 等。建議：邊界處保留但註記豁免原因，內層改捕具體型別（`CryptographicException`/`IOException`/序列化例外）。
- **P1-3 bare `catch` 回傳預設值**：`PasswordHasher.cs:68`（`catch { return false; }`）、`DatabaseSettingsCryptor.cs:88`（`return string.Empty`）、`FileHashValidator.cs:43`（`return null`）皆 fail-closed 刻意設計（方向正確），但會連同格式／crypto 例外一併吞掉，不利區分「密碼錯」與「金鑰／資料損毀」。建議：縮小為 `FormatException`/`CryptographicException`，非預期例外記錄（不含明文）後再回傳。
- **P1-4 JSON 解析錯誤回傳 `ex.Message`**：`src/Bee.Api.AspNetCore/Controllers/ApiServiceController.cs:106`（`$"Invalid JSON format: {ex.Message}"`），且此路徑不受 `IsDevelopment` 把關。建議：對外固定訊息，詳細僅記 server log。
- **P1-5 Session 時間戳 kind 不一致**：`src/Bee.Repository/System/SessionRepository.cs:43` `sys_insert_time` 用 `DateTime.Now`（本地），`sys_invalid_time` 用 `DateTime.UtcNow`。到期比較是 UTC vs UTC（功能正確），但兩欄語意不一致，跨時區稽核會混淆。建議：`sys_insert_time` 統一 `UtcNow`。
- **P1-6 登入密碼機密性外包給 TLS**：`src/Bee.Api.Client/Connectors/SystemApiConnector.cs:145-161` `LoginAsync` 以 `Encoded`（序列化+壓縮+Base64，**非加密**）送明文密碼；RSA handshake 只保護 server 回傳的 session key。若部署未強制 HTTPS 則密碼可被 MITM。建議：文件明確要求正式環境強制 HTTPS/HSTS；或提供「server 公鑰預派送 → RSA 加密密碼」的可選強化握手。

### 序列化標籤

- **P1-7 `TableSchema.UpgradeAction` 漏 `[JsonIgnore]`/`[IgnoreMember]`**：`src/Bee.Definition/Database/TableSchema.cs:143-146` 只有 `[XmlIgnore]`。因 public get/set + contractless，暫態升級控制欄會洩漏到 JSON 與 MessagePack wire。建議：補成 `[XmlIgnore, JsonIgnore, IgnoreMember]`。
- **P1-8 `FormSchema`/`TableSchema` 計算欄靠私有 setter 隱性避免上 wire**：`FormSchema.cs:48,66,84`（`SerializeState`/`ObjectFilePath`/`CreateTime`）、`TableSchema.cs:35,53,71` 標了 `[XmlIgnore, JsonIgnore]` 但無 `[IgnoreMember]`；目前不外洩純因 setter 為 private（contractless 不收）。一旦有人把 `ObjectFilePath` setter 改 public，MessagePack 會**靜默開始把 server 端內部檔案路徑上 wire**（牴觸 `security.md`「禁輸出內部路徑」）。建議：三處補 `[IgnoreMember]`，讓「永不序列化」意圖顯式且三棲一致。

---

## P2 — 結構重構

### P2-1. `Bee.Definition` 職責混雜（最被依賴專案夾帶基礎設施職責）

- **位置**：`src/Bee.Definition/Storage/`（`FileDefineStorage` 檔案 IO）、`src/Bee.Definition/Security/`（`MasterKeyProvider`/`EncryptionKeyProtector` 安全金鑰）
- **問題**：`Bee.Definition`（~1.5 萬行）是全圖最被依賴專案（6 個直接下游），是 Clean Architecture 的 Domain Core，卻同時承載檔案 IO 與安全金鑰管理（非「定義資料」）。每個消費者被迫透過相依鏈拉進 IO + 安全金鑰的 API surface，弱化「Domain Core 最純淨最穩定」意圖。
- **建議**：介面留在 Definition、**實作外移** —— `Storage/` 抽到獨立基礎設施套件或 `Bee.Definition.Storage` 子套件；`Security/` 具體實作下沉至 `Bee.Base.Security` 旁，Definition 只留 enum/介面（`ApiProtectionLevel`/`IAccessTokenValidator`）。屬較大工程,建議獨立立案分批進行。
- **附帶（低）**：`Bee.Definition` 根目錄 40+ 散置頂層檔（`DbScope.cs`/`RoundingPolicy.cs`/`NumberKind.cs`/`SysProgIds.cs`…約 1826 行）可自然分組（`Numbers/`、`Actions/`），重構時順手處理（注意命名空間對映或用資料夾邏輯分組例外）。

### P2-2. 一檔多型別（違反「一型別一檔」）

介面 + 其實作塞同檔，兩者皆 public API surface，非緊密耦合輔助型別例外：
- `src/Bee.Definition/IDatabaseSettingsProvider.cs`（+ `DefineAccessDatabaseSettingsProvider`）
- `src/Bee.Definition/IBeeContext.cs`（+ `BeeContext`）
- `src/Bee.Db/IDbAccessFactory.cs`（+ `DbAccessFactory`）
- `src/Bee.Business/IFormBoTypeResolver.cs`（+ `DefaultFormBoTypeResolver`）← 由維護性與 API 兩代理交叉確認
- `src/Bee.ObjectCaching/CacheContainerProvider.cs`（`ICacheContainerProvider` + `CacheContainerProvider`）

建議：各拆為 `IXxx.cs` + `Xxx.cs`。

### P2-3. 大檔拆分候選（> 500 行）

| 檔案 | 行數 | 拆分方向 |
|------|-----:|---------|
| `src/Bee.UI.Avalonia/Controls/GridControl.cs` | 1146 | 依編輯管線／選取／資料綁定／版面拆 partial 或抽子元件 |
| `src/Bee.Db/DbAccess.cs` | 910 | 依 Execute 家族／Schema／Transaction 分檔 |
| `src/Bee.UI.Avalonia/DataObjects/FormDataObject.cs` | 763 | |
| `src/Bee.UI.Avalonia/Views/FormView.cs` | 755 | |
| `src/Bee.Business/Form/FormBusinessObject.cs` | 735 | |
| `src/Bee.Business/System/SystemBusinessObject.cs` | 600 | |
| `src/Bee.UI.Avalonia/Views/ListView.cs` | 534 | |

（`ValueUtilities.cs` 501 行為高內聚型別轉換家族，非 grab-bag，可維持。）

### P2-4. API 表面預留基礎設施標註

- **`ILogBusinessObject` 目前零 BO-to-BO 消費者**（`src/Bee.Business/AuditLog/ILogBusinessObject.cs`）：作為 axis seam 與 Form/System 對稱、不建議刪，但缺 `CreateLogBO` 對稱 helper（另兩軸皆有）。建議二選一：(a) 補 `CreateLogBO` 擴充；或 (b) XML doc 明示「僅供未來 BO-to-BO 稽核查詢預留，尚無內部消費者」。
- **`ApiContractRegistry` 為死路徑**（`src/Bee.Api.Core/Registry/ApiContractRegistry.cs`，消費點 `ApiPayloadConverter.cs:34`）：`Register<>()` 全 repo 僅測試呼叫，production 零註冊 → `_mappings` 恆空。屬「pure POCO 回傳」預留基礎設施（框架 API surface 候選保留，不建議刪）。建議：class XML doc 標明「當前無註冊者，為 X 情境預留」。
- **序列化 `FormatterResolver` 實質死碼**（`src/Bee.Api.Core/MessagePack/FormatterResolver.cs:43-45`）：composite resolver 順序把 `ContractlessStandardResolver` 排在它之前而遮蔽它，且只認直接基底。目前無實害（8 個 `MessagePackCollectionBase<>` 子型別全直接繼承且全在 `MessagePackCodec` 顯式註冊），但給人錯誤的安全感。建議：移除它（既然靠顯式註冊），或前移並改遞迴基底檢查；並在 `bee-serialization` skill 明訂「新增 `MessagePackCollectionBase<>` 集合必須顯式註冊」。

---

## P3 — 文件漂移修正（低成本高價值）

- **P3-1 `docs/api-bo-contract-design.md` 未反映「契約軸分命名空間」重構**：`:36` 範例仍寫 `namespace Bee.Api.Contracts`（實際 `ILoginRequest` 已在 `.System`）；`:56` 標題 `Bee.Api.Core.System` 缺 `.Messages`（實際 `Bee.Api.Core.Messages.System`）。外部框架使用者會照抄錯誤命名空間。→ 更新範例與標題，補軸分規則說明。
- **P3-2 `docs/dependency-map.md` 相依圖缺 `Bee.UI.Avalonia` 兩條邊**：實際有 `→ Bee.Api.Client`（冗餘傳遞，可評估從 csproj 移除）與 `→ Bee.Definition`（真實直接，10+ 檔 `using Bee.Definition`，圖上應補）。
- **P3-3 `Bee.Api.Contracts` 分層定位矛盾**：`dependency-map.md`／`architecture-overview.md` 把 Contracts 歸「API 層」（暗示在 Business 之上），但實際相依方向為 `Business → Contracts`（Contracts 在下）。→ 重新定位為獨立「共用契約／抽象層」（介於 Definition 與 Business/Api.Core 之間）。程式碼不需改。
- **P3-4 `.claude/rules/testing.md` 「Phase 7 後全 repo 0 處 `[Collection]` 序列化」敘述失真**：實際殘留 **20 處**（`ClientInfoState`×12、`SysInfoStatic`×3、`ClientInfo`×3、`ApiClientInfoState`×2），全為保護 production static。且 `SysInfoStatic`、`ClientInfo` 兩名稱**引用不存在的 `CollectionDefinition`**（隱式分組仍運作但潛在不一致）。→ 二擇一：(a) 更新規範敘述為「仍保留 N 處窄序列化，理由為 X」；(b) 依規範把 `SysInfo.TraceListener`/`ApiClientInfo.*`/`ClientInfo.*` 重構為可注入（`TimeProvider`/`ITraceListener`/DI）移除序列化需求。至少為兩個無定義名稱補 `CollectionDefinition` 或改用已定義名稱。
- **P3-5 安全職責分界無文件**：`Bee.Base/Security/`（密碼學原語）vs `Bee.Definition/Security/`（政策/金鑰協定）分界合理但未在任何文件說明。→ 在 `security.md` 或兩專案 README 補一句分界原則。

---

## P4 — 觀察／待你裁決

這些屬慣例判斷或次要補強，需你決定方向而非直接動手：

- **`XxxCollection` + `XxxCollectionExtensions` 同檔（20 檔）**：跨全框架一致的刻意慣例，擴充類與集合緊密耦合。嚴格看違反一型別一檔。→ **請裁決**：認定為「緊密耦合特殊原因」正式豁免（寫進 code-style），或統一拆檔。不宜單獨挑幾檔拆（會造成慣例不一致）。
- **`enum` + 主型別同檔**：`LoginAuditEntry.cs`（`LoginEvent` enum）、`ChangeAuditEntry.cs`、`BeeBlazorOptions.cs`（`BeeBlazorProviderMode`）、`ApiPayloadJsonConverter.cs`（converter + factory）落在「極小 enum 群／緊密耦合輔助型別」豁免灰帶。→ **請裁決**統一拆檔或明列豁免。
- **命名**：`IExcelHelper`（`src/Bee.Definition/Documents/IExcelHelper.cs:8`）用已過時的 `Helper` 後綴，建議改 `IExcelWorkbook`/`IExcelDocument`；同檔 `sPassword`（`:31`，Hungarian 前綴）→ `password`、`topRowisDisplayName`（`:37`）→ `topRowIsDisplayName`、`leftColumnIndex `（`:45,52` XML 註解尾隨空白）。
- **重複實作**：`ListView.cs:434` 與 `FormPage.cs:371` 的欄位去重 `seen` HashSet 邏輯重複（Avalonia/Maui 平行邏輯，待 Maui 移植定案後抽共用）；5 個 `*TableSchemaProvider` 的型別對映 switch 結構近似（各 DB 型別集不同難全合併，至少抽共用 normalize 步驟，順帶修 P0-2d）。
- **次要補測**：`LoginAttemptTracker`（`tests/…/LoginAttemptTrackerTests.cs:44-58`）50ms 鎖定視窗依賴真實牆鐘,2-core CI 有 flaky 風險 → 導入 `TimeProvider` + `FakeTimeProvider`（同類：`MemoryCacheProviderTests`、`AuditLogWriterServiceTests`、`CacheNotifyServiceTests`）；`Bee.Api.Contracts`/`Bee.Repository.Abstractions` 具象 DTO（`PackageUpdateInfo`/`RecordFieldChange`/各 `*Query`）若上 wire 應補三棲 round-trip 測試。
- **序列化次要**：物件型 response（`GetFormSchemaResponse`/`GetFormLayoutResponse`/`GetLanguageResponse`）走 MessagePack 反射 contractless 但只有 in-process dispatch 測試、無真正 byte round-trip（行動端 AOT 最脆弱面），建議各加 byte round-trip 並在 `IsDynamicCodeSupported=false` 驗一次；`FilterNode.Kind`（`:20-21`）get-only 計算欄卻帶 `[Key(10)]`，多型判別已由 `[Union]` tag 承擔，`Kind` 上 wire 冗餘 → 加 `[IgnoreMember]`。

---

## 掃描為「乾淨」的項目（供交叉確認，無需處理）

- 架構：無循環相依、無反向依賴、BO 無 `Bee.Db` 參照、Server 無 `Api.Client` 參照、抽象層未被繞過、Contracts 未被實作污染
- 安全：無 `System.Random` 用於安全用途、無 MD5/SHA1 原始安全雜湊、無 `throw ex;`、無 TLS 憑證繞過、無硬編碼金鑰/密碼/連線字串、無敏感資料寫 log/例外、`NoEncryptionEncryptor` production 無法啟用、XXE 已 hardened
- 維護性：`*Func` 靜態類別殘留 0、空 class 0、未 sealed private nested 0、`[Obsolete]` 死碼 0、TODO/FIXME/HACK 0、IDE0130 不一致 0、grab-bag/上帝類別 0、純 facade 0
- API：契約軸命名空間↔資料夾 100% 一致、wire↔contract↔BO↔Client 四層對齊無孤兒、BO 介面純度無違反、`///` XML 註解全英文無 CJK
- 序列化：Newtonsoft.Json 0、MessagePack ctor 順序 landmine 未觸發（整數-key item 皆有無參數 ctor 走 setter）、`[Union]`⊥keyAsPropertyName 規則遵守（唯一 Union `FilterNode` 維持整數 key）、8 個 `MessagePackCollectionBase<>` 集合全註冊
- 測試：S2701/`Assert.True(true)` 0 處、fixture 檔案污染防護落實良好、加密/安全模組覆蓋扎實、命名規範遵循良好

---

## 建議執行順序

1. **先做 P0**（正確性/功能）—— 尤以 P0-1（`In` 走 MessagePack）與 P0-2a（`MessagePackKeyCollectionBase` comparer）為潛在正確性風險，且 P0-2 四處為同一主題可一併修正；P0-1、P0-3 建議先補測試坐實再修。
2. **再做 P1**（安全與序列化標籤）—— 多為低風險一致性修正，可批次進行。
3. **P3 文件漂移** 成本極低、價值高，可與 P1 平行或穿插。
4. **P2 結構重構** 中 P2-1（`Bee.Definition` 拆分）為較大工程，建議獨立立案；P2-2/P2-3/P2-4 可漸進。
5. **P4** 待你裁決慣例方向後再動（尤其 20 檔 `XxxCollectionExtensions` 的豁免與否會影響一批檔案）。
