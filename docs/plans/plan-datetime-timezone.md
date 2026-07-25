# Plan：DateTime 時區處理機制

**狀態：📝 擬定中（2026-07-25）**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| P0 | 定案決策寫成 ADR + 系統時間戳改 UTC（trace / 定義檔 `CreateTime`） | 📝 待做 |
| P1 | `Kind` 正本清源（清掉框架自身的 `Local` 來源）→ wire guard + Kind round-trip 測試 | 📝 待做 |
| P2 | Connector 雙向轉換（含 `FilterCondition`）+ `SessionInfo.TimeZone` 填充 | 📝 待做 |
| P3 | 單一時區部署零成本短路 + 跨 DB / 跨時區 / 行動端 tz 可用性回歸測試 | 📝 待做 |

> 目標：讓 bee-library 支援跨時區部署——**資料庫時間以 UTC 儲存，使用者檢視時轉換為其時區**——
> 同時保證單一時區部署零額外成本、既有資料可平滑遷移。
>
> **前置依賴**：[plan-date-semantics.md](plan-date-semantics.md) —— **已於 2026-07-25 完成**
> （commit `fddb38f6` / `c7782308` / `c5578a42`，ADR-031）。日曆日欄位已能在 wire 上自我描述，
> 使 schema-less 場景（報表 / AnyCode）也有安全預設。
>
> **獨立前置小修**（不在本 plan 排程，但為 D4 的前提）：`SafeTypelessFormatter` 白名單補
> `System.DateOnly` —— 見 §5。
>
> **關聯討論稿**：[plan-time-semantics.md](plan-time-semantics.md)（`FieldDbType.Time` 純時刻型別，另案）。

---

## 1. 現況盤點（已查證 + 實測）

### 1.1 框架目前已有三個時間基準並存

「全鏈路都是本地牆上時間」並不正確。實際狀況：

| 基準 | 位置 | 說明 |
|------|------|------|
| **UTC** | `SessionRepository.cs:43`、`AccessTokenValidator.cs:41`、`AuditEntry.LogTimeUtc`、`LoginAttemptTracker`、`PingResult.ServerTime` | 已經以 UTC 寫入 naive 欄位 |
| **DB server clock** | `CacheNotifyPollSession.cs:142` 的 `sys_update_time` | `getdate()` / `LOCALTIMESTAMP` 為 server local，但 **SQLite `CURRENT_TIMESTAMP` 是 UTC** |
| **Local** | `FormRowDefaults` / `FieldDbTypeExtensions` 的 `DateTime.Now`、`TraceEvent` / `TraceContext`、各定義檔的 `CreateTime` | 業務資料預設值與稽核 trace |

兩個推論：

- **既有資料遷移必須逐欄判斷**，不能全表套用固定 offset——`st_session` 已經是 UTC，一律轉會轉錯。
- **選項「naive 欄位存 UTC」已有生產驗證**：`st_session` 就是這個模式在五家 DB 上跑通的既有案例，不需重新論證。

### 1.2 其他現況

| 面向 | 現況 | 影響 |
|------|------|------|
| `SessionInfo.TimeZone` | 欄位已存在（`SessionInfo.cs:73`，預設 `Asia/Taipei`，IANA 格式），但全 repo 無任何讀寫 | 掛載點現成、休眠中，可直接沿用 |
| `DbCommandSpec` 參數層 | SQL Server `DateTime` → `datetime2(7)`；PG / Oracle 不變 | 只解**精度**，與時區正交 |
| `FieldDbType` | 刻意區分 `Date` / `DateTime`，補足 .NET 只有單一 `DateTime` 型別的表達力缺口 | 「日曆日 vs 時間點」已可區分，且**這正是框架提供的全部兩種時間語意**（見 D5） |
| `FilterCondition.Value` | 型別為 `object?`（`[Key(102)]`，走 typeless），`DateTime` 走同一條 wire | 查詢條件的時間值同樣需要正規化（見 D4） |
| 日曆日 wire 標記 | **已完成**：`FieldDbType` 標記由定義層貫通至 `DataColumn.ExtendedProperties` 與 `SerializableDataColumn.DataType`，MessagePack / JSON 兩份 wire 實作皆承接 | Connector 可完全不依賴 `FormSchema` 判斷欄位語意 |
| in-process 傳輸 | `ApiConnector.cs:172`：`LocalApiProvider` 且非 debug 時強制 `PayloadFormat.Plain`，`params` **完全不序列化**，物件以參考直接交給同行程 `JsonRpcExecutor` | **無序列化邊界**——轉換點與 guard 都不能掛在序列化入口（見 D4 / D6） |

### 1.3 序列化實測結果（本 plan 的核心約束來源）

以 `MessagePackCodec` 走真實 `DataTable` 路徑、System.Text.Json 走 `DateTime` 直接序列化，
payload 由 `+08:00` 端產生、讀取端 `TZ=America/New_York`：

| wire 上的值 | JSON 讀回 | MessagePack 讀回 |
|------------|----------|-----------------|
| `2026-01-01T09:00:00`（Unspecified） | `09:00` Unspecified ✅ | `09:00` Unspecified ✅ |
| `2026-01-01T09:00:00Z`（Utc） | `09:00Z` Utc ✅ | `09:00` **Unspecified**（Kind 被抹） |
| `2026-01-01T09:00:00+08:00`（**Local**） | **`2025-12-31T20:00:00-05:00`** ❌ 跨日 | `09:00` Unspecified |

結論：

1. **MessagePack 一律抹掉 `Kind`**（數值原封保留，回傳恆為 `Unspecified`）。
   因此「wire 傳 ISO 8601 帶 `Z`、由值本身表達時區」在 MessagePack 上不成立。
2. **`Kind=Local` 進 wire 時兩種格式語意分岔**：JSON 會依讀取端時區重算牆上時間（可跨日），
   MessagePack 不會。`Unspecified` 與 `Utc` 兩種格式的數值則完全一致。
3. 推論（D6 的來源之一）：**同一個 UTC 值經兩種格式讀回的 `Kind` 不同**（JSON 為 `Utc`、
   MessagePack 為 `Unspecified`），而 `PayloadFormat` 是部署期可切換的。
   任何依 `Kind` 分支的邏輯都會隨格式而行為分岔。

---

## 2. 已定案的設計決策

### D1：DB 一律存 UTC，全 provider 用 naive 欄位

所有 provider 在**無時區欄位**存 UTC 值（SQL Server `datetime2`、PostgreSQL `timestamp`（無 tz）、
Oracle `TIMESTAMP`、MySQL `DATETIME`、SQLite `TEXT`），時區轉換不交給資料庫。

不採用 PostgreSQL `timestamptz`：它會依 server tz 隱式轉換，成為不可控變因，且造成跨 provider 行為分歧。

### D2：兩種序列化格式都不做時區轉換

`MessagePack` 與 `JSON` 都只搬運數值，不介入時區語意。轉換責任全在伺服端與用戶端。

### D3：wire 上的 `DateTime` 兩個方向都是 UTC ★

- **伺服端 → 用戶端**：伺服端送 UTC。
- **用戶端 → 伺服端**：用戶端也送 UTC。

伺服端在資料路徑上**完全不做時區轉換**，直接讀寫 UTC。

> **為何不採非對稱設計（用戶端送使用者時區、伺服端依 `SessionInfo.TimeZone` 轉回）**：
> 那會讓「顯示用的時區」（用戶端決定）與「寫回解讀用的時區」（伺服端 session 決定）成為兩個獨立來源。
> 一旦不一致（使用者出差、裝置時區與公司設定不同、session 時區未填），失敗模式是
> **使用者看到 09:00、輸入 09:00、存進去卻是別的時刻、重新載入後畫面跳掉**——靜默且資料損毀。
> 雙向 UTC 只有單一時區來源，兩個方向必為反函數，round-trip 恆等；即使時區設錯，
> 錯誤也只降級為「顯示偏移」而非資料錯亂。
> 附帶效益：JS client 送 UTC 就是 `date.toISOString()` 的原生行為，比先轉成本地時間再送更簡單。

### D4：Connector 為唯一轉換點，雙向轉換 ★

用戶端的時區轉換集中在 `Connector`（API 介接層），**不由各 UI 層各自處理**。
收到回應時 UTC → 使用者時區；送出請求前 使用者時區 → UTC。

#### 判斷規則（完全 schema-less）

| 欄位標記（`FieldDbType`） | 處理 |
|--------------------------|------|
| `Date` | **絕不轉**（日曆日，跨日錯位） |
| `DateTime` | 一律視為時間點（Instant），雙向轉換 |

判斷依據是**隨 payload 同行的欄位標記**：wire 側讀 `SerializableDataColumn.DataType`、
本地側讀 `DataColumn.ExtendedProperties`。因此**不需要 `FormSchema`**，
沒有 schema 的場景（報表 / AnyCode）同樣適用。

> 標記由誰寫入不影響 Connector：定義驅動的查詢由框架依 schema 標記，BO 自寫 SQL 由 BO 以
> `SetDateColumns` / `DbCommandSpec.DateColumns` 宣告（前置 plan 的兩路徑規則）。
> 兩條路徑產出的 `DataTable` 對 Connector 是同構的。
>
> **殘餘破口**：路徑二忘了標記的日曆日欄位，Connector 會當 Instant 轉換而跨日偏移。
> 這是前置 plan 由型別方案改為標記方案後保留下來的唯一靜默失敗模式。

#### 承載形式二分

| 承載形式 | 規則 |
|---------|------|
| `DataSet` / `DataTable` 儲存格值 | 依上表逐欄判斷 |
| 強型別 DTO 的 `DateTime` 屬性 | **一律維持 UTC，不轉**。需要顯示成當地時間時由該處 UI 自行轉換 |

強型別 DTO 的規則符合現況（`PingResult.ServerTime`、`SessionInfo.ExpiredAt`、`AuditEntry.LogTimeUtc`
等皆為系統時間戳，本就不該依使用者時區位移），也免去逐屬性加標記的成本。

#### `FilterCondition` 的語意來源：值型別自我描述

`FilterCondition.Value` / `SecondValue` **必須套用與資料值相同的語意轉換**——
遺漏的症狀是「查今天的單據」跨區少查到資料，且不會報錯。

但送出方向沒有 `DataColumn` 可掛標記。解法是**由值的 CLR 型別自我描述**：

- 日曆日條件傳 `DateOnly` → 絕不轉
- 時間點條件傳 `DateTime` → 視為 Instant，轉換

`ValueUtilities.CDate` 已於前置 plan 階段 3 改為回傳 `DateOnly`，此規則與取值層天然一致。
**前提**：`SafeTypelessFormatter` 白名單需補 `System.DateOnly`（見 §5）。

#### 轉換點掛載位置與物件安全

**轉換掛在 Connector 的進出點，不掛在序列化入口。** 依 §1.2，in-process 走 `PayloadFormat.Plain`
時完全沒有序列化邊界，掛在序列化入口會讓 in-process 路徑整個繞過轉換。

**轉換前必須深拷貝 `DataSet`。** in-process 下物件以參考傳遞，就地轉換會改到呼叫端自己那一份——
送出方向會把使用者眼前的資料改成 UTC（UI 一重繪就顯示錯誤時間），回應方向可能改到 BO 回傳的共用物件。

#### `Kind` 一律忽略

進出 Connector 的 `DateTime` **一律依 D3 視為 UTC，不看 `Kind`**。
依 §1.3 推論 3，`Kind` 會隨 `PayloadFormat` 而異，任何依它分支的邏輯都會隨部署設定行為分岔。

#### 使用哪個時區

採登入時取回的 `SessionInfo.TimeZone`，**不使用裝置 OS 時區**。
使用者時區的權威來源為伺服端使用者設定，換裝置 / 出差不影響資料語意。
「跟隨裝置時區」可作為使用者可選設定，但不是預設。

### D5：框架只提供兩種時間語意，刻意不提供 per-column 時區覆寫 ★

框架提供的時間語意就是 `FieldDbType` 已經在區分的兩種，不多也不少：

| `FieldDbType` | 語意 | 範例 | 時區處理 |
|---------------|------|------|---------|
| `Date` | 日曆日，無時刻 | 生日、發票日期、帳期 | 絕不轉 |
| `DateTime` | 時間點（Instant），全球同一瞬間 | `created_at`、`login_at`、單據建立時間 | 存 UTC、依使用者時區顯示 |

**不新增 `DateTimeSemantics` 欄位屬性，也不新增 `FieldDbType` 列舉值。**

> **否決記錄（保留理由，避免日後重提）**：本 plan 曾規劃第三種語意 `Local`
> （綁定某地當地時間、與觀看者無關，如會議排程「當地 09:00」），以 `DbField.DateTimeSemantics`
> 屬性或新增 `FieldDbType` 值承載。2026-07-25 討論後否決，理由有三：
>
> 1. **層次錯置**：`FieldDbType` 描述「欄位存什麼型別的資料」；「該用 UTC 還是使用者時區」
>    是傳輸與呈現的約定，而該約定 D3 / D4 已經定死（wire 一律 UTC、轉換點唯一在 Connector）。
>    把時區政策塞進型別描述，等於讓同一件事有兩個決定者。
> 2. **per-column 解不了真實需求**：實務上「依特定地點時區呈現」的案例（如 HRM 出勤要看員工工作地
>    時區）是 **per-row** 的——員工分駐各地，每筆的時區不同。標在欄位上只能表達「整欄綁同一地點」，
>    根本解不了。而 per-column Local 真能成立的情境舉不出非造作的例子；排班「早班 08:00」、
>    營業時間這類其實是時刻表，本就不該是 `DateTime` 欄位（見 [plan-time-semantics.md](plan-time-semantics.md)）。
> 3. **成本不成比例**：為此要動核心持久化 enum 或在每個 `DbField` 加屬性，換到一個解不了真實需求的語意。
>
> **替代建模**：有「依特定地點時區呈現」需求時，以**「時間欄（UTC）+ 時區欄」顯式建模**，
> 由應用層決定呈現時區。這是資料模型決策，不由框架代勞。
>
> 此條必須寫進 ADR。否則日後會有人「順手補上」`DateTimeSemantics`。

**連帶效果**：D4 的判斷完全不需要 `FormSchema`，Connector 100% schema-less。

### D6：`Kind` 紀律與 wire guard ★

**不變式：進 wire 的 `DateTime`，`Kind` 只能是 `Unspecified` 或 `Utc`，絕不能是 `Local`。**

依據實測 1.3：`Local` 是唯一會讓兩種序列化格式語意分岔的 Kind。而 `Local` 極易誤入——
`DateTime.Now`、UI 控件產出的值、`ToLocalTime()` 的結果，`Kind` 全都是 `Local`。

#### guard 行為：fail fast

**debug 與 release 都擲例外**，不做「修正後放行」。

> **為何不修正後放行**：兩種修法都會靜默產生錯資料。
> `SpecifyKind(Unspecified)` 保留牆上時間、丟掉時區資訊——台北端誤送 `Local` 09:00 會被伺服端
> 當成 UTC 09:00 存入，偏移 8 小時；`ToUniversalTime()` 則依**裝置 OS 時區**換算，
> 而 D4 明文否決裝置時區作為權威來源。
>
> `Kind=Local` 進 wire 是**框架自身的程式錯誤**（轉換點唯一在 Connector、呼叫端全在框架手上），
> 不是外部輸入的資料狀況，應當場爆而非帶病前行。

#### guard 掛載位置

**掛在 Connector 進出點，不掛在序列化入口**——理由同 D4：in-process 走 `Plain` 時沒有序列化邊界，
`Kind=Local` 可以毫無阻攔地一路存活到 DB。

#### guard 不受 D10 短路影響

單一時區部署下轉換為 no-op，但 **guard 必須永遠開啟**。否則 `Local` 混入時完全不會被察覺，
等到第一個跨區客戶才爆，而那時錯誤已寫進歷史資料。這是「單一時區零成本」唯一該付的例外成本。

#### 其他

- 補「三種 Kind × 兩種格式 × 兩個時區」的 round-trip 測試釘住此不變式。
- DB 讀出的 instant 值統一 `SpecifyKind(Utc)`；日曆日語意欄位維持 `Unspecified`，不套 UTC。

> 這條比本 plan 其他任何一條都更容易在日後被無聲破壞，測試優先級最高。

### D7：XML 持久化不做時區轉換，但需先讓宣告為真

持久化物件的時間屬性一律為 UTC，序列化 / 反序列化過程不介入時區。

**實際適用對象**是會落 DB / 上 wire 的時間屬性：`SessionUser.EndTime`、`SessionInfo.ExpiredAt`、
`AuditEntry.LogTimeUtc`（後者已是 UTC）。

**前置修正**：`FormSchema` / `TableSchema` / `SystemSettings` / `DatabaseSettings` / `MenuSettings` /
`DbCategorySettings` / `ClientSettings` 的 `CreateTime` 全部使用 `DateTime.Now`（本地），與此宣告不符。
**改為 `DateTime.UtcNow`**。

> 這些 `CreateTime` 全部標了 `[XmlIgnore, JsonIgnore, IgnoreMember]`（如 `FormSchema.cs:85`），
> **根本沒有被持久化**，只是記憶體中的資訊性時間戳。改動零風險、零相容性影響，
> 換來「時間屬性一律 UTC」規則零例外。（若保留為 Local 例外，日後無人敢動這些欄位的語意。）

### D8：無 session 的系統時間戳一律 UTC

稽核、trace 一律使用 `DateTime.UtcNow` / `DateTimeOffset.UtcNow`，不經使用者時區。
順帶修正現況 `TraceEvent` / `TraceContext` 使用本地時間、導致跨區部署時間戳不可比的問題。

### D9：cache-notify 刻意不 UTC 化

`sys_update_time` 的 high-water mark 只與自己比較，UTC 化無實質效益。
**此決定必須寫進 ADR**，否則日後容易被「順手統一」而踩到各 provider 時間函式基準不同的雷
（`getdate()` / `LOCALTIMESTAMP` 為 local，SQLite `CURRENT_TIMESTAMP` 是 UTC）。

### D10：單一時區部署零成本短路

比照 `CustomizeId` 空值短路模式：部署層設定「系統時區」；當使用者時區 == 系統時區
（或未啟用跨區）時，**轉換**為 no-op，行為與今天逐位元一致。跨區成本只在多時區部署才付。

**例外**：D6 的 guard 不在短路範圍內，永遠開啟。

### D11：不做既有資料遷移工具（目前無外部消費者）

**本 plan 不含遷移階段。** 框架目前沒有外部實際消費者，切換時沒有需要保全語意的既有生產資料。

唯一的實務影響是**本機 / CI / demo 的既有資料在切換後語意會混雜**（舊列為本地牆上時間、
新列為 UTC）。這些資料皆可重建，處理方式是重建 seed 與 demo 資料，不需要工具。
涉及 `SharedDatabaseState` seed 與 `apps/Bee.Northwind` 的示範資料。

> **日後真的需要遷移時的約束（本次已推導，保留以免重推）**：
>
> 1. **一次切換，不設相容期**。相容期需要 per-row 標記新舊語意、讀寫兩路徑都要分支處理，
>    成本高於停機。
> 2. **逐欄判斷，不可全表套用**（依 §1.1）：`st_session` 等已是 UTC 的欄位不可再轉；
>    日曆日欄位不動；只轉 `DateTime`（Instant）語意欄位。
> 3. **固定 offset 只在「部署期間該時區無 DST 變動」時成立**。`Asia/Taipei` 無 DST，
>    固定 +8 安全且可逆（記下 offset 即可回滾）。**若客戶位於有 DST 的時區，遷移必須改為
>    tz-aware 逐筆轉換。**
>
> 這三條須寫入 ADR——遷移需求出現時通常伴隨時間壓力，屆時不會有餘裕重新推導。

---

## 3. 各階段實作要點

| 階段 | 要點 |
|------|------|
| **P0** | 寫 `docs/adr/adr-032-datetime-timezone.md`（含 D5 否決理由、D9 / D11 的前提條件、`Time` 未來歸屬）；D8 系統時間戳改 UTC；D7 定義檔 `CreateTime` 改 `UtcNow`。此階段與後續解耦、風險最低，可最先落地。 |
| **P1** | **兩步且順序不可顛倒。**<br>**(a) 正本清源**：清掉框架自身四處 `Local` 來源——`FormRowDefaults.cs:78`、`FieldDbTypeExtensions.cs:28`、`DynamicExpressoEvaluator.cs:46` 的 `Now()`、`TraceEvent` / `TraceContext`（後者屬 D8）。<br>**(b) 立不變式**：D6 guard（Connector 進出點、fail fast、不受短路影響）+ Kind round-trip 測試（**全 plan 最高優先**）；Repository 邊界寫入正規化與讀出 `SpecifyKind(Utc)`。<br>先開 guard 會當場炸在自家程式碼上。 |
| **P2** | D4 Connector 雙向轉換：進出點掛載、轉換前深拷貝 `DataSet`、忽略 `Kind`、`FilterCondition` 依值型別（`DateOnly` / `DateTime`）判斷；登入時填充 `SessionInfo.TimeZone`（使用者設定 / 公司預設 / client 回報）。 |
| **P3** | D10 短路；跨 DB（SQL Server / PostgreSQL / SQLite / MySQL / Oracle）round-trip 測試；跨時區測試（`TZ` 環境變數驅動）；**行動端 / WASM 的 tz 可用性驗證**（見 §4）；**驗證單一時區部署行為零變化**的回歸防護；重建 seed 與 demo 資料（依 D11）。 |

---

## 4. 主要風險

- **`Kind=Local` 混入 wire**（D6）——兩種序列化格式語意分岔且可跨日，是本設計最脆弱的一環。
  guard 改為 fail fast 後失敗模式從「靜默錯資料」變為「當場例外」，但 guard 本身被移除 / 繞過的風險仍在。
- **日曆日誤轉**（D4）——標記方案**不能保證欄位一定有標記**：BO 自寫 SQL 未以 `SetDateColumns`
  宣告的日曆日欄位仍會被當 Instant 轉換。測試需專門覆蓋跨日邊界，文件須明確載明 BO 作者的標記責任。
- **時區資料在 WASM / iOS / Android 不可用** —— `TimeZoneInfo.FindSystemTimeZoneById("Asia/Taipei")`
  依賴 ICU + tz database。目前 repo 未設定 `InvariantGlobalization`（走預設，理論上可用），
  但 Avalonia WASM head 與行動端 head 在 trim + AOT 下**從未測過**，失敗形態是
  `TimeZoneNotFoundException`。與本 repo 過去踩過的 trim 雷同一家族——桌面完全不重現，
  只在 Release 建置浮現。**P3 須各跑一次跨區 round-trip 驗證。**
- **in-process 路徑繞過轉換 / guard** —— `LocalApiProvider` + `Plain` 無序列化邊界（§1.2）。
  D4 / D6 已改為掛在 Connector 進出點並要求深拷貝，但實作時極易退回「掛序列化入口」的直覺做法。
  P3 須有 in-process 路徑的專屬測試。
- **`FilterCondition` 遺漏轉換**（D4）——症狀是查詢少幾筆，不報錯，最難發現。
- **`SessionInfo.TimeZone` 未填**（D4）——Connector 取不到時的 fallback 行為需明確定義，
  不可默默採用裝置時區（會重新引入 D3 否決的雙來源問題）。

---

## 5. 獨立前置小修：`SafeTypelessFormatter` 白名單補 `System.DateOnly`

**不排入本 plan 階段，應獨立先出。**

`SafeTypelessFormatter` 的白名單（`src/Bee.Definition/Serialization/SafeTypelessFormatter.cs:42-68`）
列有 `System.DateTime` / `System.DateTimeOffset` / `System.TimeSpan`，**沒有 `System.DateOnly`**。
MessagePack 3.1.7 本身已內建 `DateOnlyFormatter`，卡點純粹在白名單的前置與後置型別檢查。

**這是 main 上的既有地雷**：前置 plan 階段 3 已讓 `ValueUtilities.CDate` 回傳 `DateOnly`，
任何人寫 `FilterCondition.Equal("hire_date", ValueUtilities.CDate(x))` 就會在 wire 反序列化被擋。
與本 plan 無關，只是 D4 的 `FilterCondition` 規則會讓它變成必經之路。

範圍：補白名單 + MessagePack / JSON 兩條路徑的 round-trip 測試
（JSON 側需另確認 `object?` 宣告型別下 System.Text.Json 的多型行為）。
