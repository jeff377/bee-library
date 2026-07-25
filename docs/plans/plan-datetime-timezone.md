# Plan：DateTime 時區處理機制（討論稿）

> 狀態：📝 擬定中（討論用，尚未動工）· 2026-07-25
> 目標：讓 bee-library 支援跨時區部署——**資料庫時間以 UTC 儲存，使用者檢視時轉換為其時區**——
> 同時保證單一時區部署零額外成本、既有資料可平滑遷移。
> 本稿只界定**設計決策與取捨**，逐條附選項與建議，供 review 後再定案動工。

---

## 1. 現況盤點（已查證）

| 面向 | 現況 | 影響 |
|------|------|------|
| `SessionInfo.TimeZone` | **欄位已存在**（預設 `Asia/Taipei`，註解建議 IANA），但**全 repo 無任何讀寫** | 掛載點現成、休眠中，可直接沿用 |
| `SessionInfo.Culture` | 已存在（`zh-TW`），同樣未見消費端 | 與 TimeZone 同屬「使用者環境」語意 |
| `DateTimeKind` | 全鏈路一律 `Unspecified`（`FormRowDefaults` 用 `DateTime.Now`/`Today`、DB 讀回亦然） | 存的是「牆上時間」，無時區語意 |
| DB 參數層 | SQL Server `DateTime`→`datetime2(7)`（`DbCommandSpec.NormalizeDbType`）；PG/Oracle 不變 | 只解**精度**，與時區正交 |
| 欄位型別 | `FieldDbType` 僅 `Date` / `DateTime`；無「instant vs 日曆」語意 | 無法區分「該轉時區」與「不該轉」的欄位 |
| 稽核時間戳 | `TraceEvent`/`TraceContext` 用 `DateTimeOffset.Now`（**本地**時間） | 跨區部署下時間戳不可比 |
| 序列化 | XML/JSON/MessagePack 對 `DateTime` 無 Kind 保證 | UTC 語意可能在 wire round-trip 中遺失 |

**一句話結論**：框架今天把 `DateTime` 當「無時區的牆上時間」直存直取。要支援跨時區，需補上「語意分類 + 寫入正規化 + 讀出轉換 + Kind/序列化紀律」四塊，並確保不破壞單一時區現況。

---

## 2. 核心設計決策（需 review 拍板）

### 決策 A：時間欄位的語意分類 ★最關鍵

不是所有 `DateTime` 都該轉時區。混淆這點會把生日、發票日期一起錯位。建議把時間欄位分三類：

| 語意 | 定義 | 範例 | 該不該轉時區 |
|------|------|------|------------|
| **Instant（絕對時間點）** | 全球同一瞬間，本質是 UTC | `created_at`、`login_at`、單據建立時間 | ✅ 存 UTC、依用戶時區顯示 |
| **Local / Wall-clock（牆上時間）** | 綁在某地當地時間、與觀看者無關 | 會議排程「當地 09:00」 | ⚠️ 不轉（或綁固定時區），少見 |
| **DateOnly（日曆日期）** | 只有日期、無時刻 | 生日、發票日期、帳期 | ❌ 絕不轉（會跨日錯位） |

> **選項 A1（建議）**：新增欄位語意標記（見決策 F），預設對映——`FieldDbType.Date` → DateOnly；
> `FieldDbType.DateTime` → **預設 Instant**（跨區時轉換），但可在定義層覆寫為 Local。
> **選項 A2**：不分類，所有 `DateTime` 一律轉——**否決**，會弄壞 DateOnly 欄位。
> **選項 A3**：只有明確標記的欄位才轉，其餘維持現狀——最保守，但要逐欄標記，遷移期人工成本高。

> 待討論：`FieldDbType.DateTime` 的預設要「Instant」還是「Local」？ERP 業務欄位多為 Instant（建立/異動/過帳時間），
> 但也有純日期時間如「約定交貨時刻」語意模糊。建議預設 Instant + 提供 Local 覆寫。

### 決策 B：儲存策略與跨 DB 欄位型別

> **選項 B1（建議）**：**所有 provider 一律在「naive 欄位」存 UTC 值**（SQL Server `datetime2`、
> PostgreSQL `timestamp`(無 tz)、Oracle `TIMESTAMP`），時區轉換全在應用層做。
> 優點：跨 DB 行為一致、不依賴各家 tz 型別的隱式轉換（PG `timestamptz` 會依 server tz 自動轉，反成不可控變因）。
> **選項 B2**：PostgreSQL 用 `timestamptz`、其餘 naive——**否決**，跨 provider 行為分歧、難測。

### 決策 C：轉換邊界（在哪一層轉）

框架是多端統一後端（含**純 JS client 無 .NET**），轉換點的選擇影響全端。

> **選項 C1（建議）**：**Server 一律以 UTC 對外傳輸（ISO 8601 帶 `Z`），轉換在「呈現端」做。**
> - .NET client（Avalonia/MAUI）：UI 層 `DateEdit` 依 `SessionInfo.TimeZone` 轉。
> - JS client：收到 ISO `Z` 字串,由前端依用戶時區 render（JS `Date` 原生支援）。
> 優點：server 保持無狀態 UTC、wire 格式單一、前端各自貼合在地顯示。
> **選項 C2**：Server 端就依 session 時區把值轉好再回傳。
> 優點：瘦客端、JS 端零時區邏輯；缺點：同一份資料對不同 session 回不同值,快取/比對複雜,違反「wire 傳 UTC」單純性。

> 待討論：報表/匯出（伺服端算好的字串）較適合 C2（server 端就轉）；互動表單較適合 C1。
> 可能是**混合**：結構化資料走 C1（傳 UTC），伺服端產生的顯示字串（報表）走 C2。

### 決策 D：DateTimeKind 紀律與序列化

> **建議**：確立單一不變式——**DB 存 UTC；materialize 出來的 instant 一律 `Kind=Utc`**。
> - 寫入前：把 `Unspecified`/`Local` 依來源時區正規化為 UTC（見決策 E）。
> - 讀出後：`SpecifyKind(Utc)`。
> - 序列化：JSON 用帶 offset 的 ISO 8601（`O`/`Z`）；MessagePack 確認 `DateTime` formatter 保留 Kind；
>   XML（持久化用，多為定義檔非 instant，影響小，但仍需一致規則）。
> - DateOnly 欄位維持 `Unspecified`，不套 UTC。

### 決策 E：使用者時區的來源與寫入時的來源時區

- **讀出轉換**用 `SessionInfo.TimeZone`（現成欄位）。需補：登入時從**使用者設定 / 公司預設 / client 回報**填入。
- **寫入正規化**的難點：client 送上來的 `DateTime` 是哪個時區的？
  > **選項 E1（建議）**：client 一律送 ISO 8601 **帶 offset**（或直接送 UTC `Z`），server 不需猜來源時區。
  > **選項 E2**：client 送 naive 值，server 假設等於 `SessionInfo.TimeZone` 再轉 UTC——容錯差、跨端不一致。
- **無 session 的系統時間戳**（稽核、trace）：一律 `DateTime.UtcNow` / `DateTimeOffset.UtcNow`，不經使用者時區。
  （順帶修正現況 `TraceEvent` 用本地時間的問題。）

### 決策 F：欄位語意標記的承載處

決策 A 需要一個「這欄的時間語意」欄位。

> **選項 F1（建議）**：在 `DbField`（或 `FormField`）加一個可選屬性，如 `DateTimeSemantics`
> （`Instant` / `Local` / `DateOnly`），預設由 `FieldDbType` 推導（Date→DateOnly、DateTime→Instant），
> 定義檔可覆寫。向後相容：不設 = 走預設推導。
> **選項 F2**：不加欄位，純由 `FieldDbType` 推導——無法表達「DateTime 但不轉」的少數欄位。

### 決策 G：單一時區部署零成本短路

> **建議**（比照 `CustomizeId` 空值短路模式）：部署層設定「系統時區」；當**使用者時區 == 系統時區**（或未啟用跨區）時，
> 轉換為 no-op，行為與今天逐位元一致。跨區只在多時區部署才付成本。

### 決策 H：既有資料遷移

> 現有資料是「本地牆上時間」。切 UTC 前需一次性遷移：把既存 instant 欄位由「部署時區」批次轉 UTC。
> **建議**：提供遷移工具 + 每個 instant 欄位標記「已遷移」；DateOnly/Local 欄位不動。
> 待討論：是否需要「相容期」讓新舊並存（風險高，傾向一次切）。

---

## 3. 建議實作分階段（定案後才動工）

| 階段 | 範圍 | 產出 |
|------|------|------|
| P0 | 決策定案 + 寫 ADR | `docs/adr/adr-0xx-datetime-timezone.md` |
| P1 | 欄位語意標記（決策 F）+ 預設推導 + 定義層覆寫 | `DbField`/`FormField` 屬性、序列化相容 |
| P2 | 寫入正規化（→UTC）+ 讀出 Kind 紀律（決策 D/E） | Repository/BO 邊界的轉換點 |
| P3 | wire 格式：ISO 8601 帶 offset（決策 C1）+ 序列化 Kind 保證 | JSON/MessagePack round-trip 測試 |
| P4 | 呈現端轉換：`SessionInfo.TimeZone` 填充 + `DateEdit` 依時區顯示（.NET）/ JS 端 render | UI + 前端整合文件 |
| P5 | 系統時間戳改 UTC（稽核/trace）+ 單一時區短路（決策 G） | trace/audit 修正 |
| P6 | 既有資料遷移工具（決策 H） | 遷移腳本 + 驗證 |

> 每階段皆須跨 DB（SQL Server/PostgreSQL/SQLite/MySQL/Oracle）round-trip 測試,
> 且驗證單一時區部署行為零變化（回歸防護）。

---

## 4. 主要風險與未決

- **DateOnly 誤轉**：最容易出的錯（生日跨日）。決策 A 的分類是防線,測試需專門覆蓋。
- **既有資料語意未知**：舊資料存的到底是哪個時區的牆上時間,遷移前需確認部署史。
- **`DateTime.Now` 散落**：`FormRowDefaults` 等預設值用本地 now,需盤點全 repo 改為語意正確的來源。
- **快取交互**：若走 C2（server 端轉）,同筆資料對不同 session 值不同,與定義快取「共享唯讀」原則衝突——這也是傾向 C1 的原因。
- **未決清單**：決策 A（DateTime 預設 Instant/Local）、決策 C（C1/C2/混合）、決策 H（是否相容期）。

---

## 5. 給 review 的三個關鍵提問

1. **`FieldDbType.DateTime` 的預設語意**要 Instant（跨區轉）還是 Local（不轉）？（決策 A/F）
2. **轉換邊界**走 C1（server 傳 UTC、呈現端轉）、C2（server 端轉好）、還是混合？（決策 C）
3. **既有資料**要一次切 UTC,還是需要新舊並存的相容期？（決策 H）
