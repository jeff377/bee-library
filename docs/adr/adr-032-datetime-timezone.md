# ADR-032：DateTime 以 UTC 為單一時區來源，Connector 為唯一轉換點

## 狀態

已採納（2026-07-25）

> 分階段實作中。P0（本 ADR + 系統時間戳 UTC 化）與 P1（wire guard、`DateTimeMode` 正規化、
> 時間來源接縫）已落地；P2（Connector 雙向轉換）、P3（跨 DB / 跨時區 / 行動端驗證）待做。
> 執行細節見 `docs/plans/plan-datetime-timezone.md`。

## 背景

框架要支援跨時區部署——資料庫時間以 UTC 儲存、使用者檢視時轉換為其時區——
同時把單一時區部署要承擔的**複雜度**壓到最低（見 D10 對「零成本」的界定）。

### 現況並非「全鏈路本地時間」

盤點後發現框架同時存在三個時間基準：

| 基準 | 位置 |
|------|------|
| **UTC** | `SessionRepository`、`AccessTokenValidator`、`AuditEntry.LogTimeUtc`、`LoginAttemptTracker`、`PingResult.ServerTime` |
| **DB server clock** | cache-notify 的 `sys_update_time`（`getdate()` / `LOCALTIMESTAMP` 為 server local，但 SQLite `CURRENT_TIMESTAMP` 是 UTC） |
| **Local** | 業務資料預設值、trace、定義檔 `CreateTime` |

兩個推論：既有資料若要遷移**必須逐欄判斷**（`st_session` 已是 UTC，一律轉會轉錯）；
而「naive 欄位存 UTC」這個模式**已在五家 DB 上有生產驗證**（`st_session` 就是），不需重新論證。

### 序列化實測是本決策的核心約束

payload 由 `+08:00` 端產生、讀取端 `TZ=America/New_York`：

| wire 上的值 | JSON 讀回 | MessagePack 讀回 |
|------------|----------|-----------------|
| `2026-01-01T09:00:00`（Unspecified） | `09:00` Unspecified ✅ | `09:00` Unspecified ✅ |
| `2026-01-01T09:00:00Z`（Utc） | `09:00Z` Utc ✅ | `09:00` **Unspecified**（Kind 被抹） |
| `2026-01-01T09:00:00+08:00`（**Local**） | **`2025-12-31T20:00:00-05:00`** ❌ 跨日 | `09:00` Unspecified |

上表走的是 `DataTable` 路徑。補測 `DataSet` 儲存格與強型別 DTO 兩種載體後，發現行為並不一致：

| 載體 | `Local` 值的下場 | 說明 |
|------|-----------------|------|
| `DataSet` 儲存格 | 不位移 | `DataColumn` 依 `DateTimeMode` 先把 `Kind` 正規化掉，formatter 看不到 `Local` |
| 強型別 DTO 屬性（MessagePack） | **寫出端位移**（`09:00`+08 → `01:00Z`） | msgpack timestamp 擴充存絕對瞬間，`Local` 被轉為 UTC |
| 強型別 DTO 屬性（JSON） | **讀取端位移**（可跨日） | 偏移寫進 wire，讀取端依自身時區重算 |

另外 **XML 是第三條序列化路徑**（稽核 `WriteXml(DiffGram)` 走它），且是唯一會依
`DataColumn.DateTimeMode` 決定要不要寫出時區偏移的格式——.NET 預設的 `UnspecifiedLocal`
正是「會寫出偏移」的那個值，偏移一旦進了 XML，跨區讀回就位移甚至跨日。

三個結論貫穿以下所有決策：

1. **MessagePack 不保留 `Kind` 資訊**（`DataTable` 路徑抹為 `Unspecified`、DTO 路徑一律回 `Utc`），
   因此「由值自己帶時區資訊（ISO 8601 的 `Z`）」在本框架不成立。
2. **`Kind=Local` 在兩種格式上都會位移數值**——JSON 於讀取端（可跨日）、MessagePack 於寫出端。
   `Local` 沒有任何逃生路徑。
3. **同一個 UTC 值經兩種格式讀回的 `Kind` 不同**（JSON `Utc` / MessagePack `Unspecified`），
   而 `PayloadFormat` 是部署期可切換的——**任何依 `Kind` 分支的邏輯都會隨部署設定而行為分岔**。

## 考慮過的選項

### 1. wire 傳 ISO 8601 帶時區偏移，由值本身表達（否決）

MessagePack 不保留 `Kind`（實測結論 1），偏移資訊無法存活。跨格式不一致。

### 2. 非對稱設計：用戶端送使用者時區，伺服端依 `SessionInfo.TimeZone` 轉回（否決）

會讓「顯示用的時區」（用戶端決定）與「寫回解讀用的時區」（伺服端 session 決定）成為兩個獨立來源。
一旦不一致（使用者出差、裝置時區與公司設定不同、session 時區未填），失敗模式是
**使用者看到 09:00、輸入 09:00、存進去卻是別的時刻、重新載入後畫面跳掉**——靜默且資料損毀。

### 3. 雙向 UTC（採納）

只有單一時區來源，兩個方向必為反函數，round-trip 恆等。即使時區設錯，
錯誤也只降級為「顯示偏移」而非資料錯亂。附帶效益：JS client 送 UTC 就是
`date.toISOString()` 的原生行為。

### 4. 欄位層 `DateTimeSemantics` 標記，提供第三種語意 `Local`（否決）

原構想是為「綁定某地當地時間、與觀看者無關」的欄位（如會議排程「當地 09:00」）
新增 `DbField.DateTimeSemantics` 屬性或新增 `FieldDbType` 列舉值。否決理由有三：

1. **層次錯置**。`FieldDbType` 描述「欄位存什麼型別的資料」；「該用 UTC 還是使用者時區」
   是傳輸與呈現的約定，而該約定已由本 ADR 定死（wire 一律 UTC、轉換點唯一在 Connector）。
   把時區政策塞進型別描述，等於讓同一件事有兩個決定者。
2. **per-column 解不了真實需求**。實務上「依特定地點時區呈現」的案例——如 HRM 出勤要看
   員工工作地時區——是 **per-row** 的：員工分駐各地，每筆的時區不同。標在欄位上只能表達
   「整欄綁同一地點」，根本解不了。而 per-column `Local` 真能成立的情境舉不出非造作的例子；
   排班「早班 08:00」、營業時間這類其實是時刻表，本就不該是 `DateTime` 欄位。
3. **成本不成比例**。為此要動核心持久化 enum 或在每個 `DbField` 加屬性，
   換到一個解不了真實需求的語意。

## 決策

### D1：DB 一律存 UTC，全 provider 用 naive 欄位

SQL Server `datetime2`、PostgreSQL `timestamp`（無 tz）、Oracle `TIMESTAMP`、
MySQL `DATETIME`、SQLite `TEXT`。時區轉換不交給資料庫。

不採用 PostgreSQL `timestamptz`：它會依 server tz 隱式轉換，成為不可控變因，
且造成跨 provider 行為分歧。

### D2：兩種序列化格式都不介入時區

MessagePack 與 JSON 都只搬運數值。轉換責任全在伺服端與用戶端。

### D3：wire 上的 `DateTime` 兩個方向都是 UTC

伺服端送 UTC、用戶端也送 UTC。**伺服端在資料路徑上完全不做時區轉換**，直接讀寫 UTC。

### D4：Connector 為唯一轉換點

用戶端的時區轉換集中在 `Connector`（API 介接層），不由各 UI 層各自處理。
收到回應時 UTC → 使用者時區；送出請求前 使用者時區 → UTC。

- **判斷依據是隨 payload 同行的 `FieldDbType` 標記**（ADR-031）：`Date` 絕不轉、
  `DateTime` 一律視為時間點並轉換。**完全不需要 `FormSchema`**，報表 / AnyCode 等
  schema-less 場景同樣適用。
- **強型別 DTO 的 `DateTime` 屬性一律維持 UTC，不轉**（`PingResult.ServerTime`、
  `SessionInfo.ExpiredAt`、`AuditEntry.LogTimeUtc` 等本就是系統時間戳）。
- **`FilterCondition.Value` / `SecondValue` 必須套用相同轉換**，語意由值的 CLR 型別自我描述：
  `DateOnly` 絕不轉、`DateTime` 視為時間點。遺漏的症狀是「查今天的單據」跨區少查到資料且不報錯。
- **轉換掛在 Connector 進出點，不掛序列化入口**，且**轉換前必須深拷貝 `DataSet`**。
  in-process（`LocalApiProvider` + `PayloadFormat.Plain`）沒有序列化邊界、物件以參考傳遞——
  掛序列化入口會整個繞過，就地轉換則會改到呼叫端自己那一份。
- **一律忽略 `Kind`**，依 D3 視為 UTC（實測結論 3）。
- **時區來源為 `SessionInfo.TimeZone`，不使用裝置 OS 時區。** 權威來源是伺服端使用者設定，
  換裝置 / 出差不影響資料語意。「跟隨裝置時區」可作為使用者可選設定，但不是預設。

### D5：框架只提供兩種時間語意

即 `FieldDbType` 已經在區分的兩種：`Date`（日曆日，絕不轉）與 `DateTime`（時間點，轉換）。

**不提供 per-column 時區覆寫**（否決理由見上）。有「依特定地點時區呈現」需求時，
以**「時間欄（UTC）+ 時區欄」顯式建模**，由應用層決定呈現時區——這是資料模型決策，
不由框架代勞。

> 此條刻意載明，否則日後會有人「順手補上」`DateTimeSemantics`。

### D6：時間表示紀律與 wire guard

依載體分成兩條不變式，**守的是不同東西，缺一不可**：

| 載體 | 不變式 |
|------|--------|
| `DataSet` / `DataTable` | 所有 `DateTime` 欄位的 **`DataColumn.DateTimeMode` 必須是 `Unspecified`** |
| 強型別 DTO 屬性 | `DateTime` 的 **`Kind` 不得為 `Local`** |

`DataSet` 那條不查 `Kind`：儲存格的 `Kind` 由 `DateTimeMode` 決定，查值恆得 `Unspecified`、
查了等於沒查；真正決定「XML 寫出會不會帶偏移」的是 `DateTimeMode`。
`AddColumn` 已設 `Unspecified`，破口在 `DbDataAdapter.Fill` / `DataSet.ReadXml` 等
會落回 .NET 預設 `UnspecifiedLocal` 的路徑。

DTO 那條查 `Kind`：沒有 `DataColumn` 的正規化緩衝，`Local` 在**兩條 wire 上都會位移數值**
（MessagePack 於寫出端、JSON 於讀取端）。`Local` 極易誤入——`DateTime.Now`、`DateTime.Today`、
UI 控件產出的值、`ToLocalTime()` 的結果，`Kind` 全都是 `Local`。

- **guard 為 fail fast：debug 與 release 都擲例外**，不做「修正後放行」。
  兩種修法都會靜默產生錯資料：`SpecifyKind(Unspecified)` 保留牆上時間、丟掉時區資訊
  （台北端誤送 `Local` 09:00 會被伺服端當 UTC 09:00 存入，偏移 8 小時）；
  `ToUniversalTime()` 則依**裝置 OS 時區**換算，而 D4 已否決裝置時區作為權威來源。
  `Kind=Local` 進 wire 是**框架自身的程式錯誤**，不是外部輸入的資料狀況。
- **guard 掛在 Connector 進出點**，理由同 D4（in-process 無序列化邊界）。
- **guard 永遠開啟，不受任何部署設定影響。**
- DB 讀出的時間點值統一 `SpecifyKind(Utc)`；日曆日欄位維持 `Unspecified`。

### D7 / D8：持久化物件與系統時間戳一律 UTC

持久化物件的時間屬性一律為 UTC（`SessionUser.EndTime`、`SessionInfo.ExpiredAt`、
`AuditEntry.LogTimeUtc`），序列化過程不介入時區。稽核與 trace 一律 `UtcNow`。

定義檔的 `CreateTime`（`FormSchema` / `TableSchema` / 各 `*Settings`）雖標了
`[XmlIgnore, JsonIgnore, IgnoreMember]`、從未被持久化，仍一併改為 `UtcNow`——
純粹為了讓「時間屬性一律 UTC」零例外；保留為 Local 例外的話，日後無人敢動這些欄位的語意。

快取到期時間（`CacheItemPolicy.AbsoluteExpiration`）同樣採 `UtcNow`。

> trace 的 `TraceEvent.Time` / `TraceContext.Start` 與 `CacheItemPolicy.AbsoluteExpiration`
> 型別都是 `DateTimeOffset`，**本就攜帶偏移、跨區可比**，改 `UtcNow` 不是為了修正可比性，
> 而是為了讓序列化與 log 呈現不隨部署時區變動，並消除「日後被轉成 `DateTime` 或落入 naive 欄位時
> 偏移遭丟棄」的陷阱。規則零例外的價值即在此：不必逐處判斷「這個 `DateTimeOffset` 會不會被降型」。
>
> 快取尤其如此：**目前是行程內快取，但日後若改用跨機器的分散式快取**（Redis 等），
> 到期時間會跨行程傳遞、經第三方序列化落地——而**偏移在序列化時被丟棄正是本 ADR 已實測到的
> 既有現象**（見背景章節：MessagePack 不保留 `Kind`）。屆時「值本身就是 UTC」是唯一
> 不依賴序列化器是否保留偏移的基準。

### D12：「今天」與「現在」以使用者時區為基準

**「今天」= `SessionInfo.TimeZone` 的今天**，不是裝置 OS 的今天，也不是伺服端機器的今天。

理由是業務語意：請假單的請假日期預設為「當天」，那個當天必然是使用者所在時區的當天。
權威來源取 `SessionInfo.TimeZone` 而非裝置時區——否則使用者在紐約出差登打台北公司的假單，
預設日期會變成前一天。與 D4 的時區權威來源一致。

**伺服端與用戶端必須用同一定義**：`Date` 欄位 Connector 絕不轉換（D4），兩側算出的「今天」
若不一致，同一張單在兩側會是不同日期。伺服端求值同樣走 session 時區，不用機器時區。

實作上把散落的 `DateTime.Now` / `DateTime.Today` 收斂為單一接縫（`FormRowDefaults`、
`FieldDbTypeExtensions`、`DynamicExpressoEvaluator` 的 `Today()` / `Now()`），由該接縫依
使用者時區推導。

**兩條路徑分開處理。** D12 的業務案例落在欄位型別預設值路徑，不在運算式路徑上：

| 路徑 | 處理 |
|------|------|
| 欄位型別預設值（`FormRowDefaults` / `FieldDbTypeExtensions`） | 走使用者時區 |
| 運算式時間函式（`DynamicExpressoEvaluator`） | 不做時區處理，語意固定、由運算式作者自行選用 |

`FormExpressionCalculator` 拿 `DateTime` 做運算的機率很小，為它建置「隨求值側變化」的機制
不成比例。運算式函式集為 `Today()`（使用者時區的今天，**與欄位預設值共用同一接縫**）、
`Now()`（不做時區處理）、`UtcNow()`（UTC 當下，供作者明示）。

`Today()` 共用接縫是刻意的：日曆日欄位絕不轉換（D4），共用不引入二次轉換問題；而讓同一個名字
在兩處是兩種意思，是日後最容易踩的坑。

> **殘餘風險（刻意接受）**：用戶端求值的運算式若以 `Now()` / `UtcNow()` 填進 `DateTime` 儲存格，
> 送出時仍會被 Connector 當成使用者時區值再轉一次。此處不設 guard——Connector 無從得知某儲存格
> 是運算式填的。因 `DateTime` 運算式罕見而接受，作者需自行確認語意。

### D13：日期一律 `DateOnly`，`DataSet` 是唯一例外；時區一律以引數傳遞

**兩條規則，一起構成日期處理的形狀。**

#### (a) 日期的載體

日期一律以 `DateOnly` 表達。**唯一例外是 `DataSet`**——`DataColumn` 透過 `IConvertible` 強制
轉型，而 `DateOnly` 未實作它（實測：`row["d"] = new DateOnly(...)` 對 `DateTime` 欄位擲
`ArgumentException`），故日曆日欄位維持 `typeof(DateTime)`，「日期時間 vs 日期」的區別由
`FieldDbType` 標記承載（ADR-031 已建立此機制）。

轉換發生在**寫進 `DataSet` 的那一刻**，而不是讓整個框架為了一個消費端改說 `DateTime`。

#### (b) 時區的傳遞

**前後端共用的日期時間函式，時區一律以引數傳遞，不從 ambient 狀態解析。**

理由不只是「乾淨」，是這類程式碼**兩側都會跑**：從看不見的地方讀時區的 helper，在伺服端與
用戶端會有不同行為，而那正是最難察覺的分歧。具體到本框架：

- 伺服端**沒有** ambient「當前使用者」——`ISessionInfoService` 以 access token 為鍵，
  並行服務多位使用者時沒有單一 session 可查。
- `IExpressionEvaluator` 註冊為 **singleton**，任何「建構時固定時區」的設計都表達不出
  per-user 時區。
- 傳 id 而非傳 `IUserInfo`，讓 `FrameworkClock` 得以留在 `Bee.Base`（在身分模型之下）；
  持有 `IUserInfo` 的呼叫端傳 `.TimeZone` 即可，介面照樣發揮作用。

**例外**：`FieldDbTypeExtensions.GetDefaultValue` 無使用者情境可傳——`AddColumn` 與
`DbParameterSpecCollection` 都是為 NOT NULL 欄位補值，屬**資料完整性後備**而非使用者讀到的
值，故以 UTC 產生。使用者看得到的新列預設值走 `FormRowDefaults`，該處收時區引數。

### D9：cache-notify 刻意不 UTC 化

`sys_update_time` 的 high-water mark **只與自己比較**，UTC 化無實質效益。

> **前提條件（不可省略）**：各 provider 的時間函式基準不同——`getdate()` / `LOCALTIMESTAMP`
> 為 server local，而 **SQLite `CURRENT_TIMESTAMP` 是 UTC**。此處刻意不統一；
> 日後若有人「順手統一」，會踩到這個差異。

### D10：轉換永遠執行，同時區時為恆等轉換

**「零成本」指的是複雜度成本，不是執行成本。** 轉換管線一律運作，不因部署設定而繞過；
使用者時區 == 系統時區時退化為**恆等轉換**（值不變），而非跳過。

> 原訂的「同時區時 no-op、行為與今天逐位元一致」與 D1 直接衝突：台北單一時區部署下，
> 若轉換真是 no-op，使用者看到的就是 DB 原值——要讓使用者看到台北時間，DB 就得存台北時間，
> 這推翻 D1。反之若 DB 真存 UTC，台北使用者一定要轉換，短路永遠不觸發。
>
> 若改讓單一時區部署不存 UTC，D1 的「一律」破功，且日後升級為跨區部署時需要資料遷移——
> 而 D11 已決定不做遷移工具。故選擇讓轉換永遠執行：D1 零例外，恆等轉換的成本微不足道
> （每欄一次判斷，非每格）。代價是「單一時區部署行為與今天逐位元一致」不再成立，
> DB 內容會從本地牆上時間變成 UTC；因無外部消費者（D11），僅涉及本機 / CI / demo 資料重建。

**例外**：D6 的 guard 不受任何設定影響。否則 `Local` 混入時完全不會被察覺，
等到第一個跨區客戶才爆，而那時錯誤已寫進歷史資料。

### D11：目前不做既有資料遷移

框架目前沒有外部實際消費者，切換時沒有需要保全語意的既有生產資料。
本機 / CI / demo 資料皆可重建。

> **日後真的需要遷移時的前提條件**：
>
> 1. **一次切換，不設相容期**。相容期需要 per-row 標記新舊語意、讀寫兩路徑都要分支處理，
>    成本高於停機。
> 2. **逐欄判斷，不可全表套用**：`st_session` 等已是 UTC 的欄位不可再轉；日曆日欄位不動。
> 3. **固定 offset 只在「部署期間該時區無 DST 變動」時成立**。`Asia/Taipei` 無 DST，
>    固定 +8 安全且可逆。**若客戶位於有 DST 的時區，遷移必須改為 tz-aware 逐筆轉換。**
>
> 遷移需求出現時通常伴隨時間壓力，屆時不會有餘裕重新推導，故在此載明。

### `FieldDbType.Time` 的未來歸屬

`FieldDbType` 目前無 `Time`（純時刻值）。日後新增時：

- **`Time` 屬於「絕不轉時區」**，與 `Date` 同列——純時刻值與日曆日同為牆上時間，
  套用時區位移會得到無意義的結果。此結論在此預先載明，`Time` 動工時不需重新推導。
- **新值必須加在列舉尾端**：`FieldDbType` 未顯式指定數值且會上 MessagePack wire，
  中間插值會讓其後所有值位移，打斷既有 payload 與定義檔相容性。

討論稿見 `docs/plans/plan-time-semantics.md`。

## 後果

**正面**

- 單一時區來源，兩個方向必為反函數，round-trip 恆等；時區設錯只降級為顯示偏移。
- Connector 完全 schema-less，報表 / AnyCode 等無 schema 場景同樣安全。
- 轉換路徑單一：同時區時退化為恆等轉換，不需為「有沒有跨區」維護兩套行為。

**負面 / 風險**

- **`Kind=Local` 混入 wire** 是最脆弱的一環。guard 為 fail fast 後失敗模式從「靜默錯資料」
  變為「當場例外」，但 guard 本身被移除或繞過的風險仍在，測試優先級最高。
- **日曆日誤轉**：標記方案不能保證欄位一定有標記——BO 自寫 SQL 未以 `SetDateColumns` 宣告的
  日曆日欄位仍會被當時間點轉換（ADR-031 已載明此殘餘破口與 BO 作者的標記責任）。
- **`TimeZoneInfo.FindSystemTimeZoneById` 在 WASM / iOS / Android 未經驗證**。
  依賴 ICU 與 tz database，trim + AOT 下失敗形態是 `TimeZoneNotFoundException`，
  桌面完全不重現。
- **in-process 路徑無序列化邊界**，實作時極易退回「掛序列化入口」的直覺做法。

## 相關

- ADR-031（日曆日欄位語意以顯式標記承載）——本 ADR 的 D4 判斷依據
- `docs/plans/plan-datetime-timezone.md`——執行計畫與階段拆分
- `docs/plans/plan-time-semantics.md`——`FieldDbType.Time` 討論稿
