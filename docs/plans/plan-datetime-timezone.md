# Plan：DateTime 時區處理機制

**狀態：📝 擬定中（2026-07-25）**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| P0 | 定案決策寫成 ADR + 系統時間戳改 UTC（trace / 定義檔 `CreateTime`） | 📝 待做 |
| P1 | 欄位時間語意標記（`DateTimeSemantics`，只為 `Local` 覆寫而存在） | 📝 待做 |
| P2 | 寫入正規化（→UTC）+ 讀出 Kind 紀律 + wire Kind guard | 📝 待做 |
| P3 | Connector 雙向轉換（含 `FilterCondition`）+ `SessionInfo.TimeZone` 填充 | 📝 待做 |
| P4 | 單一時區部署零成本短路 + 跨 DB / 跨時區回歸測試 | 📝 待做 |
| P5 | 既有資料遷移工具 | 📝 待做 |

> 目標：讓 bee-library 支援跨時區部署——**資料庫時間以 UTC 儲存，使用者檢視時轉換為其時區**——
> 同時保證單一時區部署零額外成本、既有資料可平滑遷移。
>
> **前置依賴**：[plan-date-semantics.md](plan-date-semantics.md)（日曆日語意的顯式標記）。
> 該 plan 為獨立議題、可先行出貨；本 plan 的 D4 倚賴它提供「日曆日欄位在 wire 上自我描述」，
> 使 schema-less 場景（報表 / AnyCode）也有安全預設。

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
| `SessionInfo.TimeZone` | 欄位已存在（預設 `Asia/Taipei`，IANA 格式），但全 repo 無任何讀寫 | 掛載點現成、休眠中，可直接沿用 |
| `DbCommandSpec` 參數層 | SQL Server `DateTime` → `datetime2(7)`；PG / Oracle 不變 | 只解**精度**，與時區正交 |
| `FieldDbType` | 刻意區分 `Date` / `DateTime`，補足 .NET 只有單一 `DateTime` 型別的表達力缺口 | 「日曆日 vs 時間點」已可區分；僅缺「綁固定當地時間」的少數例外 |
| `FilterCondition.Value` | 型別為 `object?`，`DateTime` 走同一條 wire | 查詢條件的時間值同樣需要正規化 |
| CLR 型別 / wire 標記 | `FieldDbType.Date` 與 `DateTime` 在 CLR 層被抹平為 `typeof(DateTime)`，連帶使 wire 的日曆日欄位標成 `DateTime` | **由前置 plan（[plan-date-semantics.md](plan-date-semantics.md)）處理**；完成後日曆日欄位在 wire 上標為 `FieldDbType.Date` |

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

轉換規則依承載形式二分：

| 承載形式 | 規則 |
|---------|------|
| `DataSet` / `DataTable` 儲存格值 | **依 schema 逐欄判斷**（見 D5），只轉語意為 Instant 的欄位 |
| 強型別 DTO 的 `DateTime` 屬性 | **一律維持 UTC，不轉**。需要顯示成當地時間時由該處 UI 自行轉換 |

- 強型別 DTO 的規則符合現況（`PingResult.ServerTime`、`SessionInfo.ExpiredAt`、`AuditEntry.LogTimeUtc`
  等皆為系統時間戳，本就不該依使用者時區位移），也免去逐屬性加標記的成本。
- **`FilterCondition.Value` / `SecondValue` 必須套用與資料值相同的語意轉換**。
  遺漏的症狀是「查今天的單據」跨區少查到資料，且不會報錯。

**Connector 的判斷順序**（前置 plan 完成後，schema 的角色大幅縮小）：

| 欄位標記（`FieldDbType`） | 處理 | 需要 schema？ |
|--------------------------|------|-------------|
| `Date` | **絕不轉** | ❌ 標記隨 payload 同行 |
| `DateTime` | 預設視為 Instant，轉換 | ⚠️ 僅在需要 `Local` 覆寫時才查 schema |

判斷依據是**欄位標記**：wire 側讀 `SerializableDataColumn.DataType`、本地側讀
`DataColumn.ExtendedProperties`。因此**沒有 `FormSchema` 的場景（報表 / AnyCode）同樣適用**：
標記隨 payload 同行，日曆日欄位不會誤轉；`DateTime` 欄位走 Instant 預設。
schema 只為少數 `Local` 欄位而查——而報表輸出通常不含此類欄位。

> 日曆日欄位如何取得標記，由前置 plan 的兩路徑規則決定：定義驅動的查詢由框架依 schema 標記，
> BO 自寫 SQL 則由 BO 於取回 `DataTable` 後自行標記（共用同一個 helper）。
> **兩條路徑產出的 `DataTable` 對 Connector 是同構的**——Connector 只看標記，
> 不需知道標記由誰寫入。
>
> **殘餘破口**：路徑二忘了標記的欄位，Connector 看不出它是日曆日，會當 Instant 轉換而跨日偏移。
> 這是前置 plan 由型別方案改為標記方案後保留下來的唯一靜默失敗模式（見該 plan §4）。

**Connector 使用哪個時區**：採登入時取回的 `SessionInfo.TimeZone`，**不使用裝置 OS 時區**。
使用者時區的權威來源為伺服端使用者設定，換裝置 / 出差不影響資料語意，並與伺服端產生報表字串時所用的時區一致。
「跟隨裝置時區」可作為使用者可選設定，但不是預設。

### D5：欄位時間語意標記

`DbField`（或 `FormField`）新增可選屬性 `DateTimeSemantics`，值為 `Instant` / `Local` / `DateOnly`：

| 語意 | 定義 | 範例 | 是否轉時區 |
|------|------|------|-----------|
| **Instant** | 全球同一瞬間，本質是 UTC | `created_at`、`login_at`、單據建立時間 | ✅ 存 UTC、依使用者時區顯示 |
| **Local** | 綁定某地當地時間、與觀看者無關 | 會議排程「當地 09:00」 | ❌ 不轉 |
| **DateOnly** | 只有日期、無時刻 | 生日、發票日期、帳期 | ❌ 絕不轉（會跨日錯位） |

**預設推導（不設標記時）**：`FieldDbType.Date` → `DateOnly`，`FieldDbType.DateTime` → `Instant`。

> **這不是啟發式猜測，而是 `FieldDbType` 本來就在承載的語意。** `FieldDbType` 刻意區分 `Date` 與
> `DateTime` 兩種型別，正是因為 .NET 程式碼只有一個 `DateTime` 型別、無法區分「日曆日」與「時間點」。
> 換言之，定義層早已表達過這個區別，D5 只是把它接到時區轉換上——`Date` 就是日曆日（絕不轉），
> `DateTime` 就是帶時刻的時間點（預設為 Instant）。
>
> 因此 `DateTimeSemantics` 標記**只為少數例外而存在**：語意上綁定固定當地時間的 `Local` 欄位
> （如會議排程「當地 09:00」）。絕大多數欄位不需標記。

- 向後相容：不設 = 走上述推導，既有定義檔不需改動。

> **與前置 plan 的分工**：日曆日語意由**欄位標記**承載（見 [plan-date-semantics.md](plan-date-semantics.md)）——
> 本地為 `DataColumn.ExtendedProperties`、wire 為 `SerializableDataColumn.DataType`，
> **不需要 schema**。`DateTimeSemantics` 標記因此只剩一個用途——把某個 `DateTime`
> 欄位覆寫為 `Local`。這也是前置 plan 的價值所在：它讓三種語意裡最危險的一種（日曆日誤轉）
> 在 schema-less 路徑上也判得出來。
>
> **注意**：前置 plan 於 2026-07-25 由「`DateOnly` CLR 型別承載」改為「顯式標記」，
> 因此判斷依據是**標記而非型別**——`DataColumn.DataType` 對兩種語意都是 `typeof(DateTime)`。
> 代價是路徑二（BO 自寫 SQL）若忘了標記，該欄位會被當成 Instant 轉換。

### D6：`Kind` 紀律與 wire guard ★

**不變式：進 wire 的 `DateTime`，`Kind` 只能是 `Unspecified` 或 `Utc`，絕不能是 `Local`。**

依據實測 1.3：`Local` 是唯一會讓兩種序列化格式語意分岔的 Kind。而 `Local` 極易誤入——
`DateTime.Now`、UI 控件產出的值、`ToLocalTime()` 的結果，`Kind` 全都是 `Local`。

- 序列化入口加 guard：debug 下 assert，release 下正規化為 `SpecifyKind(Unspecified)`。
- 補「三種 Kind × 兩種格式 × 兩個時區」的 round-trip 測試釘住此不變式。
- DB 讀出的 instant 值統一 `SpecifyKind(Utc)`；日曆日語意欄位維持 `Unspecified`，不套 UTC。

> 這條比本 plan 其他任何一條都更容易在日後被無聲破壞，測試優先級最高。

### D7：XML 持久化不做時區轉換，但需先讓宣告為真

持久化物件的時間屬性一律為 UTC，序列化 / 反序列化過程不介入時區。

**前置修正**：現況 `FormSchema` / `TableSchema` / `SystemSettings` / `DatabaseSettings` /
`MenuSettings` / `DbCategorySettings` / `ClientSettings` 的 `CreateTime` 全部使用 `DateTime.Now`（本地），
與此宣告不符。**改為 `DateTime.UtcNow`**——這些欄位純資訊性，既有檔案的值偏差一個部署時區、無實質影響，
換來規則零例外。（若保留為 Local 例外，日後無人敢動這些欄位的語意。）

### D8：無 session 的系統時間戳一律 UTC

稽核、trace 一律使用 `DateTime.UtcNow` / `DateTimeOffset.UtcNow`，不經使用者時區。
順帶修正現況 `TraceEvent` / `TraceContext` 使用本地時間、導致跨區部署時間戳不可比的問題。

### D9：cache-notify 刻意不 UTC 化

`sys_update_time` 的 high-water mark 只與自己比較，UTC 化無實質效益。
**此決定必須寫進 ADR**，否則日後容易被「順手統一」而踩到各 provider 時間函式基準不同的雷
（`getdate()` / `LOCALTIMESTAMP` 為 local，SQLite `CURRENT_TIMESTAMP` 為 UTC）。

### D10：單一時區部署零成本短路

比照 `CustomizeId` 空值短路模式：部署層設定「系統時區」；當使用者時區 == 系統時區
（或未啟用跨區）時，轉換為 no-op，行為與今天逐位元一致。跨區成本只在多時區部署才付。

### D11：既有資料一次切換，不設相容期

停機遷移，不做新舊並存。相容期需要 per-row 標記新舊語意、讀寫兩路徑都要分支處理，成本高於停機。

遷移須遵守兩項約束：

1. **逐欄判斷，不可全表套用**（依 §1.1）：`st_session` 等已是 UTC 的欄位不可再轉；
   日曆日 / `Local` 語意欄位不動；只轉 `Instant` 語意欄位。
2. **固定 offset 只在「部署期間該時區無 DST 變動」時成立**。`Asia/Taipei` 無 DST，
   固定 +8 安全且可逆（記下 offset 即可回滾）。**若未來有 DST 時區的客戶，遷移必須改為 tz-aware 逐筆轉換**——
   此前提條件須寫入 ADR。

---

## 3. 各階段實作要點

| 階段 | 要點 |
|------|------|
| **P0** | 寫 `docs/adr/adr-0xx-datetime-timezone.md`（含 D9 / D11 的前提條件）；D8 系統時間戳改 UTC；D7 定義檔 `CreateTime` 改 `UtcNow`。此階段不依賴前置 plan，也與後續階段解耦、風險最低，可最先落地。 |
| **P1** | D5 語意標記：`DbField` 新增 `DateTimeSemantics` 屬性、定義層覆寫、三棲序列化相容（新增欄位不可破壞既有定義檔反序列化）。範圍僅涵蓋 `Local` 覆寫——日曆日語意由前置 plan 的欄位標記承擔。 |
| **P2** | D6 wire guard + Kind round-trip 測試（**最高優先**）；Repository 邊界寫入正規化與讀出 `SpecifyKind(Utc)`；`FormRowDefaults` / `FieldDbTypeExtensions` / `Bee.Expressions` 的 `Now()` / `Today()` 改為語意正確的來源。 |
| **P3** | D4 Connector 雙向轉換（含 `FilterCondition`）；登入時填充 `SessionInfo.TimeZone`（使用者設定 / 公司預設 / client 回報）；伺服端產生報表字串時依 session 時區格式化。 |
| **P4** | D10 短路；跨 DB（SQL Server / PostgreSQL / SQLite / MySQL / Oracle）round-trip 測試；跨時區測試（`TZ` 環境變數驅動）；**驗證單一時區部署行為零變化**的回歸防護。 |
| **P5** | D1 + D11 遷移工具：逐欄清單、固定 offset UPDATE、回滾腳本、驗證報告。 |

---

## 4. 主要風險

- **`Kind=Local` 混入 wire**（D6）——兩種序列化格式語意分岔且可跨日，是本設計最脆弱的一環。
- **日曆日誤轉**（D5）——前置 plan 完成後由欄位標記阻擋，但標記方案**不能保證欄位一定有標記**：
  BO 自寫 SQL 未標記的日曆日欄位仍會被當 Instant 轉換。殘餘風險高於原型別方案，
  測試需專門覆蓋跨日邊界，且文件須明確載明 BO 作者的標記責任。
- **前置 plan 未完成就實作 D4**——日曆日欄位在 schema-less 場景會被當成 Instant 轉時區。
  D4 的實作必須等前置 plan 落地。
- **`FilterCondition` 遺漏轉換**（D4）——症狀是查詢少幾筆，不報錯，最難發現。
- **既有資料語意未知**（D11）——舊資料存的是哪個時區的牆上時間，遷移前需確認部署史。
- **`SessionInfo.TimeZone` 未填**（D4）——Connector 取不到時的 fallback 行為需明確定義，
  不可默默採用裝置時區（會重新引入 D3 否決的雙來源問題）。
