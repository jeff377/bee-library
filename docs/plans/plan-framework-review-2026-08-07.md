# 框架全面體檢（2026-08-07）

**狀態：🚧 進行中（2026-08-07）**

對 17 個 `src/` 專案做十一面向唯讀體檢，產出分級重構計畫與評分。
方法：10 個平行唯讀子代理分面向全量掃描 → 交叉去重 → P0/P1 逐項人工複驗。

- 基準版本：v4.17.0
- 上輪體檢：2026-07-28（`archive/plan-framework-review-2026-07-28.md`）
- 期間變更：167 commit（docs 77／feat 36／fix 21／refactor 12／test 9），`src/` 558 檔異動

---

## 評分總表

| # | 面向 | 上輪 | 本輪 | 變化 | 主要扣分 |
|---|------|------|------|------|---------|
| 1 | 架構分層 | 8.8 | **8.6** | ▼0.2 | A-3、A-1 |
| 2 | 相依分層 | 9.2 | **9.0** | ▼0.2 | A-2、A-4 |
| 3 | 安全性 | 7.8 | **7.0** | ▼0.8 | **S-1、S-2**、S-3 |
| 4 | 維護性 | 8.5 | **8.5** | — | M-1、M-2 |
| 5 | 散落／不必要類別 | 7.5 | **7.0** | ▼0.5 | D-1、D-2 |
| 6 | 序列化一致性 | 7.0 | **8.5** | ▲1.5 | Z-1、Z-2 |
| 7 | 公開 API 表面 | 8.5 | **8.5** | — | X-1、X-2 |
| 8 | 測試品質與覆蓋 | 8.2 | **8.5** | ▲0.3 | T-1 |
| 9 | 文件漂移 | 4.5 | **6.0** | ▲1.5 | **C-1**、C-2 |
| 10 | 效能／熱路徑 | — | **6.0** | 新 | **P-1**、P-2 |
| 11 | 並行與全域狀態 | — | **7.0** | 新 | N-1、N-2 |

- **九面向平均：7.96**（上輪 7.78，▲0.18）
- **八面向平均（不含文件）：8.20**（上輪 8.19，持平）
- **十一面向平均：7.69**

> 兩個新面向（效能 6.0、並行 7.0）拉低總平均，但這是**首次測量而非退步**。
> 八面向平均持平說明結構品質穩定；九面向的提升幾乎全部來自文件面向的 +1.5。

---

## 執行階段

| 階段 | 範圍 | 項目數 | 狀態 |
|------|------|--------|------|
| P0 | 正確性／可利用安全風險 | 4 | ✅ 已完成（2026-08-07，S-1 / S-2 / P-1 / C-1） |
| P1 | 一致性缺口與潛伏 landmine | 9 | 🚧 進行中（S-3 / N-3 / N-4 / Z-1 / X-1 ✅ 已完成，S-5 ⬇️ 降 P4，剩 S-4 / N-2 / N-5 待裁決） |
| P2 | 結構重構與死碼清理 | 12 | 📝 擬定中（D-1 ❌ 駁回、D-3 / D-5 另立 plan、D-4 維持不動，含由 P0 降級的 N-1） |
| P3 | 文件漂移 | 8 | 🚧 進行中（C-2 / C-3 / C-4 / X-3 / C-6 / Z-2 ✅ 已完成 2026-08-07，剩 C-5 / X-2 待裁決） |
| P4 | 觀察／待裁決 | 6 | 📝 擬定中 |

---

## 待列入 CHANGELOG 的破壞性變更（累計）

本輪修正累積的破壞性變更，**尚未寫進 CHANGELOG**，發版時必須逐條列入。
此表存在的理由就是 X-2 的教訓——`IExcelHelper` 的移除當時也只寫在 commit message 裡，
於是整整一版沒有人補上。

| # | 變更 | 來源 | 型別 |
|---|------|------|------|
| 1 | `InvokeExecFunc` 未標 `[ExecFuncAccessControl]` 時改為拒絕（fail-closed） | S-1 | 行為變更 |
| 2 | 移除 `ISerializableClone` 與 `DatabaseSettings.CreateSerializableCopy()`（3 筆 Shipped API） | N-1 附帶 | source-breaking |
| 3 | 帳號鎖定預設啟用（`ILoginAttemptTracker` 由 `AddBeeFramework` 預設註冊，5 次 / 15 分鐘） | S-3 | 行為變更 |
| 4 | `DbParameterSpecCollection` 的兩個便利 `Add` 多載移為擴充方法（2 筆 Shipped 移除 + 3 筆新增） | Z-1 | 原始碼相容、binary-breaking |
| 5 | 移除 `SystemActions.ExecFuncLocal` 與兩個 `ExecFuncLocalAsync`（3 筆 Shipped API） | X-1 | source-breaking（原本呼叫必炸） |

---

## P0 — 正確性與可利用安全風險

### ✅ S-1 `System.ExecFunc` → `UpgradeTableSchema`：任何已認證使用者可對任意資料庫執行破壞性 DDL

**已人工複驗確認，鏈路完整。**

| 環節 | 位置 | 事實 |
|------|------|------|
| 入口 | `src/Bee.Business/BusinessObject.cs:257` | `[ApiAccessControl(Public, Authenticated)]` |
| 派發 | `src/Bee.Business/System/SystemBusinessObject.cs:95` | `InvokeExecFunc(Authenticated, …)` |
| 預設值 | `src/Bee.Business/ExecFuncHandlerExtensions.cs:38` | 無 attribute → 預設 `Authenticated` |
| 檢查 | 同上 `:40-41` | 只在 `required==Authenticated && current==Anonymous` 時拒絕 → **通過** |
| 目標 | `src/Bee.Business/System/SystemExecFuncHandler.cs:53` | `UpgradeTableSchema` **無** `[ExecFuncAccessControl]` |
| 後果 | `src/Bee.Db/Schema/TableUpgradeOrchestrator.cs:108-124` | ALTER 不可行時走 rebuild：建暫表 → `DROP TABLE` → rename |

只需一個有效 AccessToken（不需公司、角色、deployment admin），即可對**呼叫端指定的任一 `DatabaseId`** 觸發破壞性 schema 升級。

同檔 `Hello` **有**標 `[ExecFuncAccessControl(Anonymous)]` → 機制是刻意在用的，漏標是疏忽而非決策。

**根因**：ExecFunc 派發面無 analyzer 覆蓋。BEE3001（`BusinessObjectAccessControlAnalyzer`）只掃 `BusinessObject` 子類的 public method，掃不到 `IExecFuncHandler` 實作。

**修法**（三項一起做）：
1. `UpgradeTableSchema` 改為 `LocalOnly` 具名 BO 方法（與 `SaveDefine` 同級），或加 deployment-admin 檢查。
2. `InvokeExecFunc` 的預設從「無 attribute → `Authenticated`」改為**拒絕**，與 `ApiAccessValidator` 的 fail-closed 語意一致。
3. 補 analyzer（比照 BEE3001）掃 `IExecFuncHandler` 實作的 public method 必須宣告 `[ExecFuncAccessControl]`。

**引入時間**：既有問題，非本輪回歸。上輪安全面向未涵蓋 ExecFunc 派發面。

**✅ 已完成（2026-08-07）**：
- `ExecFuncAccessControlAttribute` 新增 `LocalOnly` 屬性
- `UpgradeTableSchema` / `TestConnection` 標記 `LocalOnly = true`
- `InvokeExecFunc` 新增帶 `isLocalCall` 的多載；舊多載視同遠端呼叫（保留二進位相容）
- **預設改為 fail-closed**：未標 `[ExecFuncAccessControl]` 一律拒絕，不再預設 `Authenticated`
- 新增 **BEE3003** analyzer（`ExecFuncAccessControlAnalyzer`），在建置期攔截漏標
- `FormExecFuncHandler.Hello` 顯式標記 `Authenticated`（保持原行為，未放寬）
- 雙語 `docs/analyzer-rules.md` 同步；全套件 5,362 項測試通過

---

### ✅ S-2 `System.ExecFunc` → `TestConnection`：SSRF + 連線字串注入

**位置**：`src/Bee.Business/System/SystemExecFuncHandler.cs:67`、`src/Bee.Repository/System/DatabaseRepository.cs:36-73`

同一條 ExecFunc 鏈，同樣只需 `Authenticated`。`DatabaseItem` 位於 `Bee.Definition.*` → 通過 typeless 白名單，可完整由 wire 構造。兩個獨立利用面：

1. **SSRF**：`ServerId` 留空時 `connectionString` 與 `DatabaseType` 完全來自呼叫端（`:38-39`），`connection.Open()`（`:71`）使伺服器對攻擊者指定的 host:port 發出站連線。錯誤訊息不外洩，但回應時間差構成埠掃描 oracle。
2. **連線字串注入**：`ServerId` 有值時，`{@DbName}` / `{@UserId}` / `{@Password}` 以**未逃逸的字串替換**代入（`:63-68`）。`DbName = "x;Integrated Security=false;…"` 可注入額外參數。

**修法**：比照 S-1 改 `LocalOnly` 或 admin gated。若須保留遠端：強制 `ServerId` 必填、禁傳整條 `ConnectionString`、代入改用 `DbConnectionStringBuilder` 而非字串替換。

---

### ✅ P-1 `JsonCodec` 每次序列化／反序列化都新建 `JsonSerializerOptions`

**已人工複驗確認。**

**位置**：`src/Bee.Base/Serialization/JsonCodec.cs:18-39`，呼叫點 `:54`、`:71`

`JsonSerializerOptions` 是 System.Text.Json 的**型別 metadata 快取容器**。每次新建 → contract 快取 100% miss，且每次再配置 3 個 converter。每請求 4 次（server 端 request 反序列化 + response 序列化；client 端同）。

**對照**：隔壁 `src/Bee.Api.Core/MessagePack/MessagePackCodec.cs:23` 的 `Options` 是 `static readonly`。同一 repo 內 MessagePack 做對了、JSON 沒有 → 指向 Newtonsoft → STJ 遷移（`9421070b`, 2026-04-15）的機械式轉譯遺留（Newtonsoft 的 `JsonSerializerSettings` 無等價快取語意，per-call 建立無害）。

**修法**：改為依 `(ignoreDefaultValue, ignoreNullValue)` 三種組合的 `static readonly` 實例（STJ options 首次使用後即凍結，可安全共用）。**本輪投報比最高的單項。**

順帶：同檔 `:22` 的 `WriteIndented = true` 在傳輸路徑上。`ToJson` 應改 `false`，`SerializeToFile`（給人看的定義檔）維持 `true`。

**✅ 已完成（2026-08-07）**：
- 6 個 `static readonly` options（3 種 ignore condition × 縮排與否），`GetJsonSerializerOptions` 改為挑選而非建構
- wire 路徑（`Serialize` / `Deserialize` / `ToJson`）改 compact；`SerializeToFile` 維持縮排
- 類別 `<remarks>` 加 WARNING 說明為何不可把建構搬回方法內（STJ 的 contract 快取）
- 連帶修正 `LookupDefinitionTests` 一處斷言——它原本比對縮排文字（冒號後空格），改以 `JsonDocument` 斷言值，比原本更紮實

---

### ✅ C-1 `docs/getting-started` 三處錯誤，教學第一天路徑走不通

**已人工複驗確認。**

| 位置 | 問題 | 影響 |
|------|------|------|
| `getting-started.md:51-59` + zh-TW:52-59 | using 清單有 4 個，缺 `Bee.Definition.Database`；`DatabaseType` 在該命名空間 | **CS0103 ×2**，卡在第 3 步 |
| `:61` + zh-TW:61 | 「Swap for `SqlServer` / `PostgreSql`」→ `SqlServerDialectFactory` / `PostgreSqlDialectFactory` **不存在**（實為 `SqlDialectFactory` / `PgDialectFactory`）；`*ProviderFactory` 全 repo 只有 `SqliteProviderFactory` | **CS0246**，4 家 provider 全掛 |
| `:162` + zh-TW:162 | `BusinessObject="MyApp.EchoBusinessObject, MyApp"`，但範例宣告 `namespace MyApp.Server.BusinessObjects`、組件 `MyApp.Server` | **執行期靜默 fallback** 到預設 `FormBusinessObject`，診斷方向被導向 API 層 |

範例刻意附了完整 using 清單，讀者不會懷疑清單不完整。第三項是 `19a630cc`（2026-08-06）改寫本段時新引入。

全 repo 沒有任何 SQLite 以外的 `DbProviderRegistry.Register` 範例，讀者無正確樣本可對照。

**修法**：補 using、改為逐 provider 的實際型別對照表、BO 綁定改 assembly-qualified 全名。雙語同步。**半小時，回收最大。**

**✅ 已完成（2026-08-07）**：
- 補 `using Bee.Definition.Database;`
- 「Swap Sqlite for…」換成 5 家 provider 的實際型別對照表（provider factory / dialect factory / NuGet 套件），並附自足的 SQL Server 完整範例
- 一併修正列舉大小寫：文件寫的 `SqlServer` / `PostgreSql` / `MySql` 實為 `SQLServer` / `PostgreSQL` / `MySQL`（原體檢未列此項）
- BO 綁定改 `MyApp.Server.BusinessObjects.EchoBusinessObject, MyApp.Server`
- **驗證方式**：把文件的 csharp 區塊逐字抽出、在 scratchpad 建獨立專案 ProjectReference 到 `Bee.Db` / `Bee.Definition` 實際編譯 → 0 error

---

### ⬇️ N-1 `SerializeDefine` 序列化共用快取實例（**2026-08-07 由 P0 降級為 P2**）

**降級理由**：深入查證後，體檢的原始描述有兩處失準，且三條修法各有實質代價，而實際危害很窄。
本輪只修其中無爭議的兩項（見文末），本體留待日後處理。

#### 機制實情（查證後）

```
XmlCodec.Serialize(obj)
  → SetSerializeState(Serialize)   在「來源物件」上翻旗標，遞迴到所有子集合
  → getter：if (IsSerializeEmpty(SerializeState, _tables)) return null;
       空集合在序列化期間回 null → XmlSerializer 省略該元素
  → SetSerializeState(None)
```

這套機制的目的是**讓磁碟上的定義檔乾淨**——定義檔會被人工閱讀且需持久化，不該輸出
`<Tables />` 這類多餘元素。**機制本身是對的，不應移除。**

問題只在於 `SerializeDefine` 序列化的是 process-wide 快取實例，序列化視窗內共用物件的旗標被翻起。

#### 體檢原始描述的兩處失準

1. **「`ISerializableClone` 守門只覆蓋 1/10 型別」的推論方向對，但理由錯。**
   該介面的 XML doc 宣稱用途是「`BeforeSerialize` 加密敏感欄位」——**那個機制不存在**：
   `DatabaseSettings` 未實作 `IObjectSerializeProcess`，加解密是
   `CacheDefineAccess.SaveDatabaseSettings` / `GetDatabaseSettings` 顯式呼叫
   `DatabaseSettingsCryptor` 完成的。且 `DatabaseItem.Clone()` **原樣複製 `Password`**，
   而快取實例在 `DecryptInPlace` 後持有**明文**——所以那個 clone 對密碼保護毫無作用，
   它實際達成的就只有「不翻共用實例的旗標」。
2. **這套狀態機制不只服務定義檔。** `JsonRpcRequest` / `JsonRpcResponse` / `ApiPayload` /
   `ApiMessageBase` 都實作 `SetSerializeState` 並向下傳播（`JsonRpcRequest → Params → Value`），
   信封型別自己也靠它省略空元素。比體檢假設的更承重。

#### 三條修法與各自的阻礙

| 路線 | 做法 | 阻礙 |
|------|------|------|
| A. 無條件 clone | `SerializeDefine` 一律複製 | 需為 10 個型別新寫 `Clone()`（300–400 行）；為保護一個暫態旗標深拷貝整棵樹，比例失衡；且不治根因——其他地方序列化快取實例仍會污染 |
| B. 改 `ShouldSerializeXxx()` | 移除 state 依賴 | **不可行**：只有 XmlSerializer 認得，而這 24 個 getter 三種格式都在用，改掉是 JSON/MessagePack 的 wire 行為變更 |
| C. wire 路徑不觸發 lifecycle | 加一個不設 state 的序列化進入點 | wire XML 會多出空元素。定義檔要給人看，此路線只在「wire 絕不落檔」的前提下成立——client 端確實是純記憶體快取，但這仍是行為變更 |
| D. 改執行緒範圍狀態 | `[ThreadStatic]` 取代物件狀態 | 序列化全同步故技術可行，但信封型別也在用這套傳播，牽動面比預期大；且 `IObjectSerialize.SetSerializeState` 是公開 API |

#### 實際危害（重新評估：接近 P3）

- 序列化視窗（微秒級）內，另一執行緒讀到**空集合** getter 會拿到 `null`。
  `IsSerializeEmpty` 只在集合為空時才回 null，而 request path 的讀取端實測都有 null guard；
  唯 `src/Bee.Db/Dml/SelectContextBuilder.cs:198` 與 `src/Bee.Definition/Forms/FormTable.cs:161,168`
  是裸 `!` 解參考，靠「該集合實務上不會空」擋著。
- 兩請求同時序列化同一實例 → 輸出中空元素是否省略變成非決定性。**反序列化結果等價**，不影響正確性。

> 兩個代理對嚴重度評 P1 vs P3。查證後採**接近 P3** 的判斷。

#### 本輪已處理

- ✅ `SystemBusinessObject.Plugin.cs:58` 直接 `XmlCodec.Serialize(cached)`、連現有守門都沒走，
  改為走 `SerializeDefine`。純一致性修正。
- ✅ **`ISerializableClone` 整條移除**（介面 + `DatabaseSettings` 實作 + 兩處分支 + 3 筆 Shipped API）。

移除的關鍵在於**換掉 API 回應的資料來源**：`SystemBusinessObject.GetDefineCore` 對
`DatabaseSettings` 改為直接讀原始檔而非取快取。
`DatabaseSettingsCache.CreateInstance` 讀檔時密碼仍是 `enc:` 密文，是 `GetDatabaseSettings()` 的
`DecryptInPlace` 讓快取實例變明文。直接讀檔因此一次解決兩件事：

1. **回傳的密碼維持密文** —— 修掉「定義類 API 回應含明文密碼」（本體檢先前未列此項）。
   這也讓 `GetDefine` 真正符合它自己宣告的契約「serve the definition **as stored**」。
2. **回傳的是新實例** —— 沒有共用物件可被污染，clone 的唯一實質作用自然消失。

**改在 BO 層而非 `CacheDefineAccess`**，兩個理由（後者由使用者指出，比前者更根本）：

- `CacheDefineAccess.GetDefine` 是框架通用的定義存取入口，`IDefineAccess` 有 5 個 default
  interface method（`GetMenuSettings` / `GetPluginSettings` / `GetPermissionModels` /
  `GetCurrencySettings` / `GetUnitSettings`）繞道它，在那裡特例化會改變所有內部消費者拿到的東西。
- **一個以「快取」為名的類別若對某型別靜默繞過快取，是名實不符的陷阱**——日後直接使用
  `CacheDefineAccess` 的人會誤以為 `DatabaseSettings` 有快取。

驗證過無消費端需要明文，且 SQL 路徑不受影響：`IDefineAccess.GetDatabaseSettings()` 是抽象成員
（非繞道 `GetDefine` 的 default method），連線解析走
`DbConnectionManagerService → IDatabaseSettingsProvider → DefineAccessDatabaseSettingsProvider
→ GetDatabaseSettings()`，仍是快取 + 解密且外層另有 `ConcurrentDictionary` 快取；
`DatabaseRepository.TestConnection` 同理。`DefineEditor` 直接讀檔不走 API。

順帶清掉一個錯誤敘事的來源：原測試
`CreateSerializableCopy_ThenEncrypt_DoesNotMutateOriginalCache` 的註解宣稱
「GetDefineCore must serialize a deep copy so that **the encrypt step's** in-place mutation…」，
但 `GetDefineCore` 從來沒有加密步驟——該測試在模擬一個不存在的情境。已改寫為驗證真正的保證。

#### 剩餘部分（P2）

`ISerializableClone` 移除後，所有定義型別一律不 clone——狀態變得**一致且誠實**，
不再有「守門只覆蓋一個型別」的誤導。剩下的就是本節開頭描述的機制本身：
序列化共用快取實例時，視窗內空集合 getter 會回 `null`。

若要真正處理，建議先釐清一個前提：**`SerializeState` 當初設計時，是否本就假設「快取實例不會被並行序列化」？**
若是刻意前提，則本項應改為「在 `IObjectSerialize` 的 doc 明示此限制」而非改機制。

**相關**：定義檔機密的儲存策略（現況為 master key 加密至檔案 vs 改用 `${ENV_VAR}` 參照）
是另一個獨立議題，已確認機密數量隨 `DatabaseServer` 而非 `DatabaseItem` 擴張（多公司部署下
仍只有 1–3 組），環境變數參照可行性高。應另立 ADR，不在本體檢範圍。


## P1 — 一致性缺口與潛伏 landmine

| # | 項目 | 位置 | 說明 |
|---|------|------|------|
| **✅ S-3** | ~~`ILoginAttemptTracker` 有完整實作但**從未註冊**~~ **已完成 2026-08-07** | `src/Bee.Hosting/BeeFrameworkServiceCollectionExtensions.cs:246-249`（只有註解） | `Login` 是唯一可匿名觸達的憑證驗證面。預設容器不註冊 → `tracker` 恆 null → 三處 lockout 邏輯全部短路。**開箱即用的部署完全無帳號鎖定**。實作躺在 repo 裡且無相依。修法：`TryAddSingleton<ILoginAttemptTracker, LoginAttemptTracker>()` 一行。**已完成**：`AddBeeFramework` 預設註冊（5 次 / 15 分鐘），採 `TryAdd` 故 host 自訂實作仍勝出；補 2 個註冊測試。**行為變更**：既有部署升上來後密碼連錯 5 次會開始鎖定，發版時需於 CHANGELOG 標示 |
| **S-4** | API key gate 預設 presence-only，且撤銷最後一把金鑰會使防護倒退 | `src/Bee.Api.Core/Authorization/ApiAuthorizationValidator.cs:147-157` | 無 enabled 金鑰時任何非空 `X-Api-Key` 皆通過。輪替時先停舊金鑰 → gate 退回 presence-only，是**由正常維運動作觸發**的降級。上輪已提、未修。修法：非 Development 環境 `InForce==false` 由 warning 升為啟動失敗 |
| **⬇️ S-5** | ~~`GetLookup` 同時繞過 layer-1 權限與 layer-2 record scope~~ **2026-08-07 維護者裁決：不套 record scope，降為 P4 觀察** | `src/Bee.Business/Form/FormBusinessObject.cs:100-122` | 註解只論證了 layer-1 豁免（挑參照值不需瀏覽權），**沒論證 layer-2**。`:109-111` 的 `CombineWithScope` 兩引數是搜尋條件與 `GetLookupFilter()`，record scope 完全沒進來。後果：任一 progId 可用 `SearchText` 做 `LIKE %…%` 逐字元枚舉 + `PageSize` 上限 1000 翻完整表。**裁決（2026-08-07）**：lookup 選資料通常只顯示編號與名稱、不含機敏資料，故不套 record scope。
此與 `GetLookup` 既有 remarks 的推理一致（「Exposure is bounded by the `FormSchema.LookupFields` declaration」），
確認豁免為刻意設計而非疏漏。

**但此裁決使 `FormSchema.LookupFields` 成為安全介面**：`GetLookup` 是
`[ApiAccessControl(Public, Authenticated)]`，progId 由呼叫端指定，且**不驗證目標 form 是否真為 lookup 對象**，
每頁上限 1000（`DataFormRepository.cs:106`），`SearchText` 對所有 String 型 lookup 欄位做 `Contains`。
也就是任一已認證使用者可對任意 progId 翻頁列舉 `sys_rowid` + LookupFields。

安全性因此完全依賴「作者不把機敏欄位放進 `LookupFields`」，而框架不強制此事——
日後若有人把薪資、身分證號等欄位加進某張表的 `LookupFields`，會靜默對全體已認證使用者可讀。

**下輪應評估**：(a) 加 analyzer 或文件規範，把「LookupFields 不得含機敏欄位」寫成硬性要求；
(b) `GetLookup` 是否該驗證目標 form 確實被某個 relation 參照（需要反向索引，成本較高）。 |
| **N-2** | `ApiClientInfo` 把 per-user 狀態放 process-wide static | `src/Bee.Api.Client/ApiClientInfo.cs:23,43,49`；寫入 `SystemApiConnector.cs:167` | `LoginAsync` 把 **session 專屬**加密金鑰寫進 static。`Bee.Web.Blazor.Server` 是多使用者 process → B 登入覆蓋 A 的金鑰 → A 後續 Encrypted 請求解密失敗（fail-closed，非外洩，但互相踢下線）。`BeeApiConnectorFactory` 已對 `accessToken` 做對了，剩三個 static 沒跟上 |
| **✅ N-3** | ~~`DbProviderRegistry` / `DbDialectRegistry` 用裸 `Dictionary`~~ **已完成 2026-08-07** | `src/Bee.Db/Manager/DbProviderRegistry.cs:16-17`、`DbDialectRegistry.cs:16` | production 啟動後唯讀，但**測試中不是**：`DbProviderRegistryTests` 平行呼叫 `Register`（含 `Remove`），其他測試同時 `Get`。resize 期間並行讀 → `IndexOutOfRangeException` / 無限迴圈。典型「本機綠、CI 紅」根因。修法：改 `ConcurrentDictionary`，零行為變更。**已完成**：兩個 registry 改 `ConcurrentDictionary`，`Remove` 改 `TryRemove`，並在型別上加 WARNING 說明為何必須維持 concurrent（測試層會並行讀寫） |
| **✅ N-4** | ~~`ClientDefineAccess` 快取是裸 `Dictionary`，從 thread-pool 並行進入~~ **已完成 2026-08-07** | `src/Bee.Api.Client/ClientDefineAccess.cs:29,88-107` | XML doc 宣稱「Concurrent reads of the same key share a single in-flight request」，但 `TryGetValue` + 索引賦值非原子。觸發源：`ListView.cs:291,371` 的 fire-and-forget + `ConfigureAwait(false)` 續行落 thread pool。修法：`ConcurrentDictionary` + `GetOrAdd`。**已完成**：改 `ConcurrentDictionary<string, Lazy<Task<object>>>`——`Lazy` 是關鍵，`GetOrAdd` 的 value factory 在競爭下可能被呼叫多次，對「啟動請求」的 factory 而言就是這個快取要防的重複往返；失效改用 compare-and-remove 的 `TryRemove(KeyValuePair)` 多載，消除 TryGetValue/Remove 之間的空隙。兩處使用點（`GetDefineAsync` / `GetCustomizeAsync`）皆已轉換 |
| **N-5** | `SessionInfo` 多欄位更新不具原子性 | `src/Bee.Business/Session/SessionCompanyBinder.cs:66-79`、`SystemBusinessObject.Session.cs:184-192` | 同 token 的並行請求共用同一 reference。連寫 6 個欄位期間，另一請求可讀到「新公司 + 舊角色」→ **授權決策讀到不一致狀態**。修法：收斂成 immutable value object 一次替換，或 write-replace 而非 in-place mutate |
| **✅ X-1** | ~~`ExecFuncLocal` 是永久壞掉的公開 API~~ **已完成 2026-08-07** | `src/Bee.Definition/SystemActions.cs:117`、`SystemApiConnector.cs:73`、`FormApiConnector.cs:79` | BO 端方法於 v3.5.1（2025-10-03）移除，常數與兩個 connector 方法留著。`JsonRpcExecutor.GetMethod` 找不到即 `MissingMethodException` → **任何外部呼叫必炸**。破了 10 個月、零測試覆蓋。v4.16.0 建立快照時**原樣追認為「已發布 API」**——這是快照機制的固有盲點：守得住「不要變」，守不到「本來就是錯的」。**已完成**：3 處表面全數移除、3 筆 Shipped API 刪除。查證確認移除者為 `6706bab4`（2025-10-03），該 commit 同時引入 `ExecFuncAccessControlAttribute`——也就是 local-only 語意當時就已改由 attribute 承載，`ExecFuncLocal` 這個 wire action 自那時起即無對應 BO 方法。S-1 加上 `LocalOnly` 屬性後，該語意的唯一入口更明確就是 `ExecFunc` + attribute。連帶更正 `bee-add-bo-method` skill 中把它列為「歷史合理寫法」的敘述 |
| **✅ Z-1** | ~~序列化 analyzer 只掛 3 個專案，gate 沒接滿~~ **已完成 2026-08-07** | 掛載：`Bee.Business` / `Bee.Api.Core` / `Bee.Definition`；未掛：`Bee.Api.Contracts`（3 個 wire DTO）、`Bee.Db`（5 個框架集合） | BEE4002–4006 對這兩個專案靜默（僅 BEE4001 跨 assembly）。`Bee.Db/DbParameterSpecCollection.cs:18,33` 是全 repo 唯一的多重 public `Add` 違規，正因未掛 analyzer 而未被擋。**已完成**：兩專案各補 `ProjectReference`（`OutputItemType="Analyzer"` + `ReferenceOutputAssembly="false"`），未提到 `Directory.Build.props`——那會連 `Bee.Analyzers` 自己與 UI 專案都掛上，而掃描確認除這兩個外無其他專案含集合子類或 MessagePack 型別。補掛後 `Bee.Api.Contracts` 乾淨、`Bee.Db` 如預期紅在 BEE4005，兩個便利 `Add` 多載位移為 `DbParameterSpecCollectionExtensions`（同檔，比照 `Bee.Definition` 20 個集合）。`DataRowVersion` 的 optional 參數拆成兩個明確多載——位移後兩個 `Add` 參數個數相同，RS0027 要求帶 optional 者須為參數最多的多載。**公開 API**：2 筆 Shipped 移除 + 3 筆新增，原始碼相容、二進位破壞性 |

---

## P2 — 結構重構與死碼清理

| # | 項目 | 位置 | 說明 |
|---|------|------|------|
| **❌ D-1** | ~~上輪 14 項死碼清單只清了 10 項~~ **2026-08-07 查證後駁回：整項誤報** | — | 所謂「未清 4 項」在上輪**全部經裁決明確保留**，不是漏清。上輪 plan 的刪除清單下方緊接著一張〈刻意保留〉表，本輪掃描只讀到被 `~~刪除線~~` 劃掉的原始清單、沒讀那張表。逐項複驗：`TreeNodeIgnoreAttribute` 上輪已標「⚠️ 本表一處誤判已更正」（7 處生產用途，防反射循環），本輪又列一次；`IDefineField` 由 `DbField` 實作，屬未被消費的抽象；`IElementCapabilityResolver` 的實作 `ElementCapabilityResolver.Default` 有 5 處生產呼叫（`LayoutCapabilityApplier` / `ListView.Commands` / `FormView` / DemoCenter ×3）；`CheckPackageUpdate`/`GetPackage` 是 base 擲 `NotSupportedException` 的刻意擴充點，已列入 `docs/api-method-reference` 與 `jsonrpc-frontend-integration`。上輪唯一真正遞延到本輪的是 `DateTimeExtensions.GetYearMonth`（零生產呼叫端）——但 BCL 無「當月一日」等價方法，非純 wrapper，依 code-style「0-caller 框架公開 API 保留」應留。**本項無動作。** |
| **D-2** | `PermissionBindingValidator` 看似防護、production 零呼叫 → **查證後升級：公開文件宣稱的保證不存在** | `src/Bee.Definition/Settings/Permission/PermissionBindingValidator.cs` | 驗證 `PermissionModelId` / `ScopeRole` 綁定正確性，只有測試呼叫 7 次。**權限綁定錯誤在 runtime 不會被攔下**。建議接進定義載入或 `Bee.Cli` validate 指令（與 `Bee.Analyzers` 那條線是同一件事的兩種實作）。**2026-08-07 查證補充**：這不只是死碼——三處**公開文件**明文宣稱它在載入期生效：`docs/permission-authorization.md:66,160`（"is a load-time validation error"／"fails at load time"）、其 zh-TW 對應處、`docs/adr/adr-019:37`（「由 `PermissionBindingValidator` 於載入期報錯」）。與 X-2 同型：文件與事實不符比缺口本身更危險。**兩條路互斥、需裁決**：(a) 接進定義載入使文件成真（行為變更，既有部署若有無效綁定會在啟動時失敗）；(b) 改文件為「提供驗證 API，需自行呼叫」並考慮接 `Bee.Cli` |
| **D-3** | ~~`TreeNode` 屬性叢集為死碼~~ → **改判：未接線的設計，另定 plan 處理** | `src/Bee.Base/Attributes/TreeNode{,Ignore}Attribute.cs`、`IDisplayName.cs` | **不刪除，且不在本次體檢範圍內處理**——見 [plan-definition-editor.md](plan-definition-editor.md)。原判定為 WinForms 遺留有誤：標註模型是連貫的領域階層描述（`CollectionFolder` 描述結構、7 個 `[TreeNodeIgnore]` 全數用於防反射循環），意圖是「定義物件 → 前端 TreeView」。實際為 71 處標註（41 + 23 無參數 + 7 ignore），非 62 |
| **D-5** | **（新增）定義類別上另有 579 處純編輯器 metadata 零消費端** | `src/Bee.Definition/**`（`[Description]` 312、`[Category]` 115、`[Browsable]` 68、`[TypeConverter]` 13；`[DefaultValue]` 144 另有序列化用途故不計入） | 與 D-3 同源、規模大 7 倍。本輪散落類別掃描漏掉，因其掃描目標是「型別有無 caller」，而這些是 BCL attribute。根因：歷史工作模式為 TreeView + PropertyGrid 雙控件驅動，移植 Avalonia 時因**無內建 PropertyGrid** 而改為手寫面板（57 個 DataTemplate + 141 個欄位繫結），metadata 就此斷線。**另定 plan 處理**，見 [plan-definition-editor.md](plan-definition-editor.md) |
| **D-4** | `IUIViewService` 抽象縫 production 零實作 → **需裁決** | `src/Bee.UI.Core/IUIViewService.cs`、`ClientInfo.cs:169,354` | 唯一實作是三個測試 fake。所有實際 head 走另一個多載 → `ClientInfo.UIViewService` 恆 null。`ClientInfoInitializeTests` 註解自稱「補強覆蓋率」，4 個測試全為執行這條死路徑。**2026-08-07 查證補充**：事實成立——四個 head（Northwind.UI / Avalonia.Demo / DemoCenter / DefineEditor）全走 `InitializeAsync(string endpoint)`，各自寫 `ConnectionViewModel` 而非 dialog-callback。但它**不是無主的死碼，是有文件的宿主擴充點**：`development-cookbook:720` 教「1. Implement IUIViewService」、`terminology` 有詞條、`Bee.UI.Core/README` 列為 API、`adr-013:30,78` 以它為「Blazor 為何不屬 `Bee.UI.*`」的論據、`dependency-map:130` 更把它寫進 family 判別準則。依 code-style「0-caller 框架公開 API 保留」應留。**若要移除，牽動 A-3 提出的 family 判別準則改寫**（兩項應一起裁決）。純測試面的小問題（4 個測試只為執行死路徑）可獨立處理 |
| **A-1** | `Bee.Api.AspNetCore` 越層直取 `ICacheContainer` | `BeeFrameworkApplicationBuilderExtensions.cs:2,50`；csproj 只宣告 `Bee.Hosting` | 跨層繞道 + 未宣告的遞移相依。本 repo 對顯式宣告有明確慣例（`Bee.Hosting.csproj:22-24` 註解）。**2026-07-28 後回歸**（`d66dc510`）。修法：暴露 `IApiKeyGateProbe` 於 `Bee.Hosting`，或補顯式 ProjectReference + 更新 constraints 文件 |
| **A-2** | `BackendDefaultTypes` 以反射字串反指 8 個外層具象型別 | `src/Bee.Definition/BackendDefaultTypes.cs:15-53` | Domain Core（L2）指名 `Bee.Business` / `Bee.ObjectCaching` / `Bee.Repository`（L4）。**編譯期與相依圖都看不見**，改名只在執行期炸。卡在此處是因 `[DefaultValue]` 需編譯期常數。修法：移除 `[DefaultValue]`、常數搬到 `Bee.Hosting`（已引用全部三者），fallback 走既有的 `IsNullOrWhiteSpace` 成例 |
| **A-3** | `Bee.UI.Core/Permissions/` 位置錯誤，導致 Blazor head 缺敏感欄位降級 | `ElementCapabilityResolver.cs`（零 UI 框架型別）；缺口 `DynamicForm.razor.cs:47`、`DynamicGrid.razor.cs:49-50` | `dependency-map` 把 Blazor 劃出 `Bee.UI.*` 的理由是「無 file IO、無 dialog service」——**不適用於 `Permissions/`**。實際後果：同一份 FormSchema、同一組權限，Avalonia 有 per-role 降級、Blazor 沒有。修法：三檔下沉到 `Bee.Definition`（相依零阻力），family 判別準則改為「是否消費**平台服務**抽象」 |
| **A-4** | `GlobalEvents` 靜態事件造成隱形相依邊 + 訂閱洩漏 | `src/Bee.Definition/GlobalEvents.cs:11`；訂閱 `DbConnectionManagerService.cs:31`（無 `-=`、不實作 `IDisposable`） | **兩個代理獨立指出**。`Bee.ObjectCaching` 與 `Bee.Db` 無任何 ProjectReference，卻有真實行為相依。每建一個 DI 容器就永久掛一個訂閱者 → 測試中跨 fixture 干擾。另有 re-entrancy 疑點（`ConcurrentDictionary` valueFactory 內回頭 `Clear()` 同一字典）。修法：DI 化為 `IDatabaseSettingsChangeNotifier`，或至少實作 `IDisposable` |
| **P-2** | `SerializableData*` 的 wire 形狀是所有資料傳輸成本的乘數 | `src/Bee.Api.Core/MessagePack/SerializableDataTable.cs:96-101,115-124`、`SerializableDataRow.cs:15,20` | (a) Unchanged 列同送 Current + Original，而 `GetData` 結尾 `AcceptChanges()` 使**所有讀取列都是 Unchanged** → payload 與 CPU 皆 2×；(b) `Dictionary<string,object?>` 使欄名逐列重複上 wire，且每格走 typeless 派發（`Guid` / `decimal` 首當其衝——`sys_rowid` 與所有金額欄）。修法：(a) 可立即做；(b) 改 `object?[]` 是破壞性 wire 變更，需與 client 同版發布 |
| **P-3** | 「查一次定義」被寫成「查十次定義」 | `FormBusinessObject.Permission.cs:32,48,73,98,138`、`Audit.cs:55`、`FormBusinessObject.cs:246,384`、`RepositoryFactory.cs:265` | 單次 Save 約 10 次 `GetFormSchema(ProgId)`，而 `MemoryCacheProvider` 每次 `TryGetValue` 都檢 expiration token → `File.GetLastWriteTimeUtc` **syscall**。修法：(a) `FileModificationToken` 加最小重檢間隔，或 (b) 請求進入時取一次 schema 往下傳（順帶消掉字串配置） |
| **P-4** | 權限檢查對 `Grants` 全表線性掃描 + 每次配置 `HashSet` | `src/Bee.Definition/Identity/CompanyRolePermissions.cs:48-62,97-111` | `|Grants|` = 角色數 × 模型數 × 動作數，數千至上萬列是 ERP 常態。每次檢查 O(|Grants|) 且 `SessionInfo.Roles` 為 `IReadOnlyList` → **每次呼叫新建 HashSet**。修法：建構時預建索引字典；`Roles` 改暴露 `ISet<string>` |

---

## P3 — 文件漂移

| # | 項目 | 說明 |
|---|------|------|
| **✅ C-2** | 套件 README 型別名整批落後 3–9 個月 | 34 個位置、約 60 個編譯錯誤點。`AccessTokenValidationProvider`（3.5 月）、`LayoutGroup`/`LayoutItem`/`ColumnControlType`（3 月）、`ILogWriter` 群（3 月）、`CompareBytes`（改的當天有人碰同一份 README 卻漏這行）、`IApiProvider`→`IJsonRpcProvider`、`ISessionRepository.CreateSession`（不存在）、`Bee.Api.Client` 9 個方法全部漏 `Async` 後綴、`GridControl` 基底型別寫錯。**完整清單見掃描報告的「照抄會編譯不過」表** |
| **✅ C-3** | 已移除套件（`Bee.UI.Maui` / `Bee.Web.Blazor.Wasm`）殘留敘述 | `development-cookbook` / `terminology` / `architecture-overview` 三份最大文件仍以現在式描述。cookbook 的前端決策樹指向一個**整份文件不存在的章節**（Blazor WASM）。諷刺的是 `35504636`（2026-07-31）標題就是「清除已移除 UI 套件的殘留參照」，卻沒碰這三份 |
| **✅ C-4** | `ClientInfo.Initialize` → `InitializeAsync` 未跟（6 處） | v4.11.0 破壞性變更（`d9400c5a`, 2026-06-24），CHANGELOG 已載明，文件漂 6 週。cookbook `:731` 的 `if (!ClientInfo.InitializeAsync(...))` 為 **CS0023** |
| **C-5** | `AssemblyVersion` / `FileVersion` 未隨 4.17.0 升版 | `src/Directory.Build.props:5-6` 為 `4.16.0.0`。4.8.0→4.16.0 每版都三個一起升，`a0cd9de6` 只改了 `<Version>`。**已發布的 NuGet 4.17.0 套件內組件 identity 是 4.16.0.0**。建議 4.18.0 修正並於 CHANGELOG 說明，不重發 4.17.0 |
| **X-2** | `IExcelHelper` 破壞性移除從未進任何 CHANGELOG | 移除於 `206d29ff`（v4.16.0），該 commit message 自己寫「須列入 CHANGELOG breaking change」卻沒執行。而 `docs/repo-ops/public-api-baseline.md:19` 與 `gotchas/test-ci-release.md:137` 都把它寫成「已關閉的流程缺口」——**文件與事實不符比漏標本身更危險** |
| **✅ X-3** | `api-method-reference.md` 雙語各漏 2 個 System 方法；`ICacheContainer` 新增成員漏標 | 缺 `GetCustomizeFormLayout` / `GetCustomizeLanguage`（`bbd2fd2a`, 2026-08-01）。v4.17.0 CHANGELOG 列了三個介面新增 `PluginSettings` 成員，漏第四個 `Bee.ObjectCaching.ICacheContainer`（同為 source-breaking） |
| **✅ C-6** | 13 項 ADR 漂移 | 最需處理：ADR-008:70,72（`Bee.Db.Logging` 整個 namespace + 3 型別不存在）、ADR-013:55,69（`SyncExecutor` 已移除、`IApiProvider` 應為 `IJsonRpcProvider`）、ADR-010:155,180,181（`DefinePathInfo` / `LocalDefineAccess` 已刪）、ADR-021/022:9（`GridControl` 基底寫成 `DataGrid`，實為 `ContentControl`——寫的當天就錯）。**需標 Superseded 者 0**；缺的是 ADR-008/009/010/013/021/022 各補一段〈實作演進〉，比照 ADR-017 的範例 |
| **✅ Z-2** | 三處 shipped doc + skill 對「formatter 漏註冊」的失敗模式描述與實測相反 | `MessagePackCodec.cs:39-42`、`FormatterResolver.cs:32-33`、`bee-serialization/SKILL.md:70,93` 說「沉默出空集合」；`CollectionFormatterRegistrationAnalyzer.cs:18-24` 的 MessagePack 3.1.7 **實測**結論是「序列化正確，**反序列化**擲 `MessagePackSerializationException`」。失敗模式決定修法優先序，描述反了會讓後續判斷失準。另 `rules/serialization.md:24-31` 的 ctor 順序規則缺 `keyAsPropertyName` 例外，已誤導出一處錯誤 XML doc（`UnitItem.cs:25-32`） |


**✅ 2026-08-07 已完成 6 項**（commit `b7e929ee` / `95646b36` / `d2bdab2b` / `0da02e93`）：

以「文件反引號識別符 vs repo 全部宣告識別符」全量比對取代照抄清單，逐一驗證後修正，雙語同步。
過程中駁回四項體檢誤報：`samples/Web.Js.Demo` 的 `RpcError` 是 JavaScript 類別（初掃只比對 `.cs`）；
ADR-031 的 `ValueUtilities.CDate` 位於「破壞性變更」段落屬正確歷史紀錄；ADR-020 已有〈實作演進〉
補記；ADR-011 表列的 `DefinePathInfo` 在「被 DI 取代的 static locator」清單中。

同時找到體檢未列的項目：`Bee.Definition/README` 的 Layouts 資料夾清單不只名字錯，是**整份過期**
（漏 `LayoutGrid` / `LayoutColumn` / `FormEditModes`）；`docs/database-settings-guide` 的
`RemoteDefineAccess` 是**憑空捏造**的設定選項；Z-2 的錯誤敘述實際有**六處**而非三處，
其中兩處是 **skill 的 frontmatter description**——也就是這輪體檢的序列化代理很可能就是被自己的
skill 誤導，修掉後該回路才斷。
---

## P4 — 觀察／待裁決

| # | 項目 | 說明 |
|---|------|------|
| **M-1** | 消費端撞名的公開型別（趁 pre-stable 決定） | `Bee.Definition.Identity.IAuthorizationService` 撞 `Microsoft.AspNetCore.Authorization.IAuthorizationService`（對一個以 ASP.NET Core 為主 host 的框架是實質風險，Blazor `_Imports.razor` 常預設帶）；`Bee.Base.Tracing.TraceListener` 撞 `System.Diagnostics.TraceListener`。皆 CS0104。改名日後是破壞性變更，**現在最便宜** |
| **M-2** | 註解紀律：27 處中文 `#region` + 16 行中文註解 + ~190 處 WHAT-only 註解 | 公開 NuGet repo 應全英文。`#region` 是機械替換（30 分鐘）。XML doc（唯一進消費端 IntelliSense 的層）**100% 英文且零 `<param>` 名稱不符**，故不急 |
| **T-1** | 新元件未跟上測試慣例（同一模式重複三次） | 2026-07-30 後新增：11 個 ApiKey/DeploymentAdmin wire 型別無 round-trip、`ExpiredSessionCleanupService`（唯一會**刪資料**的背景服務）零覆蓋、`LogApiConnector`（9 個稽核 API 的 client 端）零覆蓋。代表補測試靠自律而非 gate。建議加反射式 guard 對所有 `ApiRequest`/`ApiResponse` 子型別做泛型 round-trip smoke |
| **T-2** | 4 處名實不符的空洞 round-trip | `DtoSerializationTests.cs:288,304`、`FileDefineStorageTests.cs:126,159`：用**零資料**物件序列化再反序列化，唯一斷言 `Assert.NotNull`，而 `[DisplayName]` 寫「應正確還原」。同檔既有正確寫法並存 |
| **Z-3** | `ScopeResolver` 繞過 `FilterCondition.In()` 的 `object[]` 具現化不變式 | `src/Bee.Business/Permission/ScopeResolver.cs:142,163` 直接塞 `List<object>`。`In()` 的 `ToArray()` 是刻意的（白名單允許 `object[]` 不允許 `List<object>`）。**現況不上 wire 故不會失敗**，屬潛伏 landmine：一旦有人序列化 scope filter（稽核留痕、快取、AnyCode 轉送）即擲例外 |
| **A-5** | Domain Core 夾帶約 1,119 行檔案 IO，且被 ILLink descriptor 強制 preserve 到行動端 | `Storage/` + `Defaults.cs` + `PathOptions` + `MasterKeyProvider`。descriptor 對整個組件下 `preserve="all"` → 行動端帶著結構上不可能使用（`.app` bundle 唯讀）的檔案 IO。**不建議本輪拆套件**（破壞性）；性價比最高的第一步是把 descriptor 從 `<type fullname="*">` 收窄為只 root 定義型別階層——無破壞性 |

---

## 建議執行順序

1. **S-1 + S-2**（同一條 ExecFunc 鏈，一次修完並補 analyzer）— 唯一可實際利用的權限提升
2. **P-1**（`JsonCodec` static options）— 一處改動、每請求 4 次的全域收益
3. **C-1**（getting-started 三處）— 半小時，外部開發者第一天路徑
4. **S-3**（`LoginAttemptTracker` 一行 DI 註冊）+ **S-5**（`GetLookup` 一行 filter 組合）— 投報比最高的安全修正
5. **N-3 + N-4**（三處 `ConcurrentDictionary`）— 機械式、零行為變更
6. **N-1**（`SerializeDefine` 無條件 clone，前置補 9 個 `Clone()`）
7. **Z-1**（analyzer 補掛兩專案）→ 會讓 `DbParameterSpecCollection` build 紅，需一併修
8. **D-1**（清完上輪剩下的 4 組死碼）+ **D-2/D-3/D-4**
9. **C-2**（README 型別名全量 diff，掃描報告已是完整清單）+ **C-3/C-4/C-5**
10. P2 結構重構與 P3 其餘文件項

---

## 掃描為乾淨的項目（供下輪回歸偵測）

**維持乾淨（逐一重驗仍成立）**

- 30 條組件相依邊**零循環**；四條硬約束全綠（BO 無 `Bee.Db`、後端無 `Bee.Api.Client`、Repository 抽象未被繞過、Contracts 零實作污染）
- mermaid 相依圖與 csproj **30/30 逐條吻合**
- `*Func` 靜態類殘留 0、`*Helper` 後綴型別 0、Newtonsoft 0、`[Obsolete]` 0、空 class 0、grab-bag 命名 0
- `CurrentCultureIgnoreCase` 0、`new DateTime(` 未指定 Kind 0、`Regex` 未傳 timeout 0、public 可變欄位 0
- `[Union]` ⊥ keyAsPropertyName 遵守；`MessagePackCollectionBase<>` 子型別 formatter 註冊 **8/8 無遺漏**；ctor 順序 landmine 未觸發
- 契約軸 100% 對齊（`ApiContractPairingTests` 守門）
- 公開文件零 `docs/plans/` 引用（5 條落地檢查全通）
- SQL 注入 0（值全參數化、識別符全經 `QuoteIdentifier`、字面值全經 `EscapeSqlString`）；XXE 0（4 個解析入口全設 `DtdProcessing.Prohibit` + `XmlResolver=null`）；`new Random(` 0；硬編碼機密 0；MD5 0；裸手動 `Dispose` 0；`throw ex;` 0
- 上輪 4 個 `SharedDbFixture` 誤用**全數修復**且無新違規；`CollectionDefinition` 缺口已補齊
- fixture 污染 0；牆鐘 flaky 0；零案例 Theory 0；`[DisplayName]` 100%
- S2699 全量掃 163 個疑似無斷言測試，逐一驗證後 **0 違規**
- 死連結 **0**（1291 條相對連結 + 108 個 anchor）；CHANGELOG 雙語條目數 17 版全等；明細檔 16 版 bullets/headings 逐版全等；65 組語言切換連結零缺漏
- XML doc **0 行含中日韓字元、0 處 `<param>` 名稱不符**
- 量化宣稱全中：analyzer 22/22、reserved names 17/17、DefineType 13/13、expression 函式 5/5、docs/README 長度標籤 23/23
- DI captive dependency **0**（50 個服務全 Singleton + 1 Transient，無 Scoped，結構上不可能）
- `async void` 0；`Task.Run` 包同步碼 0;四處 `lock` 全乾淨（私有鎖物件、無巢狀、鎖內無 IO）；`DepartmentTree` 的 double-checked locking 含正確 `volatile`
- `XmlSerializer` 已快取（最經典的嚴重洩漏完全避開）；`HttpClient` 依 host 池化 + `PooledConnectionLifetime`；`KeyCollectionBase` 為真正 O(1)（未踩 `dictionaryCreationThreshold` 陷阱）；`CommandTimeout` 8 條路徑無一漏網
- **零處 per-row 的 LINQ 線性欄位查找**（schema-driven 框架的典型重災區）；N+1 查詢 0；無界記憶體洩漏 0

**本輪新增的守門機制（下輪應確認仍存在）**

- `Bee.Analyzers` 的 BEE4001–4006 六條序列化規則（每條 XML doc 附 MessagePack 3.1.7 **實測**結論而非推測）
- public API 快照：`PublicApiAnalyzers` 全域啟用 + 16 對 `PublicAPI.{Shipped,Unshipped}.txt` + `docs/repo-ops/public-api-baseline.md` + `tools/scripts/gen-public-api.py`，v4.17.0 已跑過完整一輪。**上輪點名的「最高槓桿單一改善」已關閉**
- `BoApiSurfaceTests`（`[ApiAccessControl]` baseline）、`ApiContractPairingTests`（含 `WireMessageTypes_IsNotEmpty` 防假綠燈）、`TestFunc` 的 `comparedCount > 0`

---

## 方法論教訓（下輪沿用）

1. **「標記完成」需要獨立回驗環節。** 上輪 P3-8 把 `dependency-map` 外部套件表標 ✅，但 `f9ef0af0` 的 diff 只動了 mermaid 圖與散文，**該表一行未改**。同理 `IExcelHelper` 的 CHANGELOG 缺口被三份文件寫成「已關閉」而實際未補。下輪應把「宣稱已修的項目逐條回驗」列為固定第一步。
2. **P0 必須人工複驗，且複驗會改變結論。** 本輪 5 項 P0 全部走過完整鏈路驗證（讀 attribute → 讀派發預設 → 讀目標方法 → 讀最終效果），全部確認。同時兩個代理對 `SerializeDefine` 的嚴重度判斷相差兩級（P1 vs P3），只有讀原始碼能定案。
3. **兩個代理獨立指出同一問題 = 提高信心 + 提高優先序。** 本輪交叉命中：`SerializeDefine`、`GlobalEvents`、`CheckPackageUpdate` 全棧、ApiKey wire 型別零測試、`LogApiConnector` 零覆蓋、`ILLink.Descriptors.xml` 的 plan 引用。
4. **分數上升不等於問題變少。** 序列化 +1.5 的主因是失敗模式從沉默轉為編譯期擋下；文件 +1.5 中約 1.0 是真實改善、約 0.5 是掃描深度增加後**仍**上升所反映的結構性進步。要求代理明確拆分歸因，否則無法區分。
5. **新面向首次測量的低分不是退步。** 效能 6.0 / 並行 7.0 是首次有基準，下輪才有回歸意義。

---

## 附註：`rules/public-docs.md` 落地檢查的涵蓋缺口

兩個代理獨立撞到同一件事：檢查 (4)(5) 只 grep `*.cs` `*.axaml` `*.razor`，**`.xml` / `.csproj` / `.props` 不在內**。因此下列 3 處長期漏網，且 `ILLink.Descriptors.xml` 是以 `<EmbeddedResource>` 打進 NuGet 套件的**實際發佈物**：

- `src/Bee.Definition/ILLink.Descriptors.xml:23`
- `src/Bee.Definition/Bee.Definition.csproj:63`
- `src/Directory.Build.props:56`

建議把 `--include="*.xml" --include="*.csproj" --include="*.props" --include="*.targets"` 加入檢查 (4)。
