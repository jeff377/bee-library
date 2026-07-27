# Plan：`FieldDbType.Time` 純時刻型別（討論稿）

**狀態：📝 擬定中（2026-07-27）**

> **這是討論稿，不是可執行計畫。** 目的是把
> [plan-datetime-timezone.md](plan-datetime-timezone.md) 討論過程中推導出的約束記下來，
> 避免日後動工時重新推導或誤踩。有實際需求時再展開為正式 plan。
>
> **2026-07-27 第二輪**：修正 §3.1 的理由、補上反向相容性破口、CLR 承載型別就地定案，
> 並新增 provider 語意分裂與工作量級距兩節。
>
> **2026-07-27 第三輪（實測）**：以四個本機 DB 容器與 wire 實跑驗證（方法見 §9），
> 修正 §3.4 的**理由**（原引用的 `IConvertible` 論據經實測不成立，結論不變）、
> 補上 §3.5 空值 sentinel 缺口、把 §5 的 Oracle 疑問轉為已知事實、
> 並結掉「範圍約束落在哪一層」這題。

---

## 1. 現況

`FieldDbType`（[../../src/Bee.Base/Data/FieldDbType.cs](../../src/Bee.Base/Data/FieldDbType.cs)）目前為：
`String` / `Text` / `Boolean` / `AutoIncrement` / `Short` / `Integer` / `Long` /
`Decimal` / `Currency` / `Date` / `DateTime` / `Guid` / `Binary` / `Unknown`。

**無 `Time`**。純時刻值（上班時間 08:30、營業時間、班別起訖）目前只能以
`String` 或 `DateTime` 勉強表達。

## 2. 為何現在不做

時區 plan 只需要「日曆日 vs 時間點」這條界線，`Date` / `DateTime` 兩個現有值已足夠。
`Time` 是獨立議題，與時區設計無互鎖。

## 3. 已確立的約束（動工時直接沿用）

### 3.1 新值必須加在列舉尾端

`FieldDbType` 未顯式指定數值（隱含 `0..N`），而它會上 MessagePack wire——
[../../src/Bee.Api.Core/MessagePack/SerializableDataColumn.cs](../../src/Bee.Api.Core/MessagePack/SerializableDataColumn.cs)
的 `DataType` 即為一例。**enum 一律以底層整數上 wire，與鍵style 無關**——
`keyAsPropertyName`（ADR-030）改的是成員鍵，不影響 enum 值本身。
在中間插入 `Time`（例如排在 `Date` 旁邊求語意相鄰）會讓其後所有值的數值位移，**打斷既有 payload**。

→ 一律 append 至尾端；或此次順帶改為顯式指定數值後再新增。

> **定義檔不受影響**（2026-07-27 修正）。定義檔存的是 enum **名稱**不是數值
> （`FormSchema` 實測：`DbType="AutoIncrement"`），改順序不會壞定義檔。
> 原稿寫「打斷既有定義檔的相容性」有誤，理由留錯會導致日後做出過度保守的決定。
> **結論不變，只有理由收窄為 wire。**

### 3.2 append-only 只保護舊值，新值仍是對舊 client 的單向破壞

append 保證舊 payload 在新版仍讀得對，但**反向不成立**：新 server 回傳 `Time`（= 新序號）給舊 client，
舊 client 的 `DbTypeConverter.ToType` 走 `default:` 直接擲 `InvalidOperationException`
（`ToDbType` 同樣擲 `ArgumentOutOfRangeException`）。

→ `Time` 上線需要「舊 client 不會取到含 `Time` 欄位的表」的部署紀律，或在 `Ping` / 版本協商層擋。
**這不是可以靠 append-only 迴避的問題**，展開為正式 plan 時必須有明確決策。

### 3.3 `Time` 屬於「絕不轉時區」

純時刻值與日曆日同為牆上時間，套用時區位移會得到無意義的結果。
在時區 plan 的 Connector 判斷表中，`Time` 與 `Date` 同列（絕不轉）。

→ 此結論已載入時區 plan 的 ADR，`Time` plan 不需重新推導。

### 3.4 CLR 承載型別：`DataColumn` 用 `TimeSpan`，取值層回 `TimeOnly`

**兩層分工，各有其型別**：

| 層 | 型別 | 依據 |
|----|------|------|
| 儲存 / 傳輸（`DataColumn`、wire） | **`TimeSpan`** | 實測：三家 provider 讀回來一律 `TimeSpan`；`DataSet` XML 拒收 `TimeOnly` |
| 取值層 / 應用層（`CTime`、BO、UI binding） | **`TimeOnly`** | 語意精確：非負、`< 24h`、加法繞回、`IsBetween` 支援跨午夜 |

與 `Date` 現況（儲存 `DateTime` + 標記、取值 `CDate` 回 `DateOnly`）同構。

**兩條決定性的實測證據**（方法見 §9）：

1. **provider 讀出端一律給 `TimeSpan`**。SQL Server / PostgreSQL / MySQL 三家的參數層
   `TimeOnly` 與 `TimeSpan` 都收，但 `DataTable` 讀回來的欄位型別**全部是 `TimeSpan`**——
   這是 ADO.NET 的 schema 型別對應（`time` → `TimeSpan`），與參數傳什麼無關。
   走 `DataTable` 的框架，欄位型別**必然**是 `TimeSpan`。
2. **`DataSet` XML 拒收 `TimeOnly`**。`DataColumn(typeof(TimeOnly))` 可建、可賦值、
   `WriteXml` 也寫得出來，但 `ReadXml` 擲
   `InvalidOperationException: Type 'System.TimeOnly' is not allowed here`
   （.NET 的 `DataSet` 允許型別白名單）。框架以 `DataSet` XML 做持久化，這條路直接斷。
   `TimeSpan` 全程通過（XML 以 ISO 8601 duration `PT8H30M15S` 表示）。

> **第二輪的理由已修正（2026-07-27）**。第二輪主張「選 `TimeSpan` 是因為 `TimeOnly` 不實作
> `IConvertible`，會重蹈 `DateOnly` 覆轍」——實測 `TimeOnly` / `TimeSpan` / `DateOnly`
> **三者皆非 `IConvertible`**，`TimeSpan` 並沒有比較好。`DateOnly` 當初出事的真正原因是
> **欄位型別為 `DateTime`、值為 `DateOnly`，需要轉換才失敗**；只要**欄位型別 = 承載型別**
> 就不會走到轉換。**結論不變，理由換成上述兩條實測。**

另一個第二輪結論仍成立：**`Time` 欄位自我描述，不需要 `ExtendedProperties` 標記**——
`Date` 需要標記是因為與 `DateTime` 撞 CLR 型別，`Time` 用 `TimeSpan` 沒有這個問題。
這是 `Time` 比 `Date` 便宜一整個階段的地方。

### 3.5 `Time` 沒有空值 sentinel，必須允許 NULL

框架對時間型別的空值慣例寫在
[../../src/Bee.Base/Data/FieldDbTypeExtensions.cs](../../src/Bee.Base/Data/FieldDbTypeExtensions.cs)
的 `ToDbFieldValue`：`DateTime.MinValue` 視為空、轉 `DBNull`。這在 `Date` / `DateTime` 上成立，
因為西元 1 年 1 月 1 日不是合法業務值。

**`Time` 沒有這種值可用**——`TimeSpan.Zero` 就是 `00:00:00`，午夜是完全合法的時刻
（班別「00:00 起」、營業時間「00:00–24:00」）。影響三處：

| 位置 | 影響 |
|------|------|
| `GetDefaultValue(Time)` | 回 `TimeSpan.Zero` 等於「預設午夜」而非「預設空」，與 `Date` / `DateTime` 回 `UtcNow` 不同層 |
| `ToDbFieldValue` | 無 sentinel 可判，`Time` 必須走 `DBNull` 直傳，是該 switch 的第一個例外 |
| 「文字/數值欄 NOT NULL」的既有偏好 | `Time` 欄若 NOT NULL，「未填」無法表達 → **這類欄位必須允許 NULL** |

→ 不先定這條，實作時會在 `ToDbFieldValue` 硬塞「`TimeSpan.Zero` 視為空」的分支，
然後在「00:00 的班別存不進去」的 bug 上耗掉一天。

### 3.6 範圍約束由取值層型別把關，不另寫 CHECK

MySQL `TIME` 與 Oracle `INTERVAL DAY TO SECOND` 底層都容得下負值與超過一日（見 §5），
但 **`TimeOnly` 本身就是守門員**：`TimeOnly.FromTimeSpan` 遇到負值或 `≥ 24h` 直接擲例外。
只要取值層一律經 `CTime`，非法值進不了業務邏輯。

→ **不另加 DB CHECK 約束**（五家 DB 語法各異、維護成本高、且擋不住繞過框架的直接 SQL）。
DB 層維持寬鬆，型別層收斂。

## 4. 待討論議題

| 議題 | 說明 |
|------|------|
| 抽象層語意界定 | `FieldDbType.Time` = **牆上時刻**、`[0, 24)`、非負。若日後需表達「工時 7.5 小時」，那是另一個 `FieldDbType.Duration`，**不要讓 `Time` 一詞兩用** |
| Oracle 參數綁定 | 已實測失敗，成因與解法見 §5——需 provider-specific 參數型別，比照 `datetime2` 的處理位置 |
| 取值層 | `ValueUtilities` 新增 `CTime` 回 `TimeOnly`，與 `CDate` / `CDateTime` 家族對稱 |
| JSON wire | 需顯式補分支，見 §6 |
| MessagePack 白名單 | 若 `TimeOnly` 會進 `FilterCondition.Value`，需補 `SafeTypelessFormatter`，見 §6 |
| 標記 helper | `ResolveFieldDbType` / `ApplyFieldDbType` / `GetDeclaredFieldDbType` 需納入新值 |
| UI 層 | `FormField` 與各 UI 端（Avalonia / MAUI / Blazor）的時刻編輯控件 |

**已就地結案、不再列為待議**：

- ~~CLR 承載型別~~ → §3.4（第三輪以實測定案）。
- ~~空值表達~~ → §3.5。
- ~~範圍約束落在哪一層~~ → §3.6。
- ~~typeless 白名單（`TimeSpan`）~~ → `System.TimeSpan` **已在**
  [../../src/Bee.Definition/Serialization/SafeTypelessFormatter.cs](../../src/Bee.Definition/Serialization/SafeTypelessFormatter.cs)
  白名單中，實測 raw round-trip 通過。
- ~~三棲序列化~~ → MessagePack 側 cell 走 typeless 白名單既已涵蓋；XML 走 `TimeSpan` 內建支援
  （ISO 8601 duration）。剩下的只有 JSON，見 §6。

## 5. provider 語意分裂與實測結果

### 5.1 各家 `TIME` 的語意本身就不一致

| DB | 型別 | 語意 |
|----|------|------|
| SQL Server | `time(7)` | 牆上時刻 `00:00:00` – `23:59:59.9999999` |
| PostgreSQL | `time` | 牆上時刻 `00:00:00` – `24:00:00` |
| MySQL | `TIME` | **duration，`-838:59:59` – `838:59:59`** |
| SQLite | `TEXT` | 無型別 |
| Oracle | 無 `TIME` | 需替代方案 |

MySQL 的 `TIME` 根本不是牆上時刻，而是可正可負、可超過一日的 duration。
**不需要退回字串儲存**——除 Oracle 外都有原生時刻型別。範圍收斂由 §3.6 的型別層負責。

### 5.2 實測：參數寫入與讀出型別

環境與方法見 §9。

| DB | 欄位型別 | 傳入 `TimeOnly` | 傳入 `TimeSpan` | 讀回的 CLR 型別 |
|----|---------|----------------|----------------|----------------|
| SQL Server | `time(7)` | ✅ | ✅ | **`TimeSpan`** |
| PostgreSQL | `time` | ✅ | ✅ | **`TimeSpan`** |
| MySQL | `TIME(6)` | ✅ | ✅ | **`TimeSpan`** |
| Oracle | `INTERVAL DAY(0) TO SECOND(6)` | ❌ `ArgumentException` | ❌ `ORA-50028` | — |
| SQLite | `TEXT` | 未測 | 未測 | （無型別，存字串） |

### 5.3 Oracle：不是做不到，是框架綁不出來

實測失敗訊息為 `ORA-50028: Invalid parameter binding`——**問題在框架的參數層，不在 Oracle**。
`DbCommandSpec` 走通用 `DbType`，而 Oracle 的 interval 綁定需要顯式
`OracleDbType.IntervalDS`，通用 `DbType` 沒有對應值。

→ 解法比照 SQL Server `datetime2` 的既有處理：在 `DbCommandSpec` 的參數正規化階段
做 **Oracle-only** 的 provider-specific 型別指派。**注意這是全域改動的雷區**——
與 `datetime2` 那次同樣，改在共用的型別對應表會波及其他 provider。

**Oracle 欄位型別仍建議 `INTERVAL DAY(0) TO SECOND(n)`**，優於下列替代：

| 替代方案 | 問題 |
|---------|------|
| `DATE` + 固定基準日 | 基準日會洩漏到查詢與顯示 |
| `NUMBER`（午夜起算秒數） | 可排序，但處處要轉、DB 端不可讀 |
| `VARCHAR2(8)` `'HH24:MI:SS'` | 定寬零填補故可字典序排序，但丟失型別 |

## 6. 兩份 wire 的成本不對稱

| wire | 成本 | 實測 |
|------|------|------|
| MessagePack（`TimeSpan`） | **零成本** | raw round-trip ✅（`System.TimeSpan` 已在白名單） |
| MessagePack（`TimeOnly`） | 需補白名單 | raw round-trip ❌ `MessagePackSerializationException` |
| JSON | **必須顯式補分支（讀寫兩向）** | — |

`TimeOnly` 只有在**進入 `FilterCondition.Value`**（typeless 路徑）時才需要補
`SafeTypelessFormatter` 白名單。若取值層的 `TimeOnly` 一律在送 wire 前轉回 `TimeSpan`，
則不需要——這是設計時可選的邊界。

JSON 側的原因：[../../src/Bee.Base/Serialization/DataTableJsonConverter.cs](../../src/Bee.Base/Serialization/DataTableJsonConverter.cs)
的 `ConvertValue` 尾端走 `Convert.ChangeType`，`TimeSpan` 非 `IConvertible` → 落 catch →
回傳原 string → `DataRow` 賦值擲例外。需比照 `byte[]` / `Guid` / `DateTime` 加一條 `TimeSpan` 分支，
寫入端同樣需明確格式（建議 `"HH:mm:ss.fffffff"` 定寬，避免文化相依）。

## 7. 工作量級距

新增一個 `FieldDbType` 值要動的檔案：

| 範圍 | 檔數 |
|------|------|
| 5 個 provider × 6 檔（TypeMapping / SchemaSyntax / CreateTableCommandBuilder / TableRebuildCommandBuilder / AlterCompatibilityRules / TableSchemaProvider 反推） | ~30 |
| `Bee.Base`（`DbTypeConverter` / `FieldDbTypeExtensions` / `DataTableExtensions` / `ValueUtilities` 等） | ~6 |
| JSON wire | 1 |
| Oracle 參數綁定特例（§5.3） | 1 |

**約 40 個檔位，不是小改。** 唯一的好消息是這些 switch 幾乎都有 `default: throw`，
漏改會**大聲失敗**而非沉默出錯——這讓「先動工再逐一補齊」成為可行策略。

## 8. 展開時機

**這是延後、不是擱置**——`Time` 是確定要補的表達力缺口，只是排在時區 plan 之後。
待有實際業務需求（排班、營業時間、班別定義等）牽動優先序時，將本文展開為正式 plan。
在此之前不動 `FieldDbType`。

第二輪列為「唯一值得提前做」的 Oracle 可行性驗證**已於第三輪完成**（§5.3）：
落地型別確定為 `INTERVAL DAY(0) TO SECOND(n)`，且已知需要 provider-specific 參數綁定。
**最大的不確定性已拆除**，剩下的多是機械工。

## 9. 實測方法與環境（2026-07-27）

以用完即刪的 xUnit probe 跑，未留在版控中。重現方式：

- **DB 端**：在 `tests/Bee.Db.UnitTests/` 建 probe class，`IClassFixture<SharedDbFixture>` +
  `[DbFact(DatabaseType.X)]`，對每家 DB 建暫存表 → 以參數 INSERT `TimeOnly` / `TimeSpan` →
  `SELECT` 回讀，記錄 `DataColumn.DataType` 與 cell 的實際型別。
- **CLR / XML 端**：`DataColumn(typeof(TimeOnly))` 與 `typeof(TimeSpan)` 各跑
  賦值 → `WriteXml(WriteSchema)` → `ReadXml`，並反射檢查 `IConvertible`。
- **wire 端**：在 `tests/Bee.Api.Core.UnitTests/` 以 `MessagePackCodec` 對兩型別做
  raw round-trip 與 `SerializableDataTable` round-trip。

**環境**：provider 版本 `Microsoft.Data.SqlClient` 7.0.0、`Npgsql` 9.0.4、
`MySqlConnector` 2.4.0、`Oracle.ManagedDataAccess.Core` 23.26.200、
`Microsoft.Data.Sqlite` 9.0.4、`MessagePack` 3.1.7；DB 容器 `sql2025` / `pgvector-db` /
`mysql8` / `oracle23ai`。**provider 升版後結論可能改變**（尤其 Oracle 的綁定支援），
動工前建議以同法重跑一次。
