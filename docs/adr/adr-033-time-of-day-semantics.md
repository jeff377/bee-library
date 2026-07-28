# ADR-033：時刻語意（`FieldDbType.Time`）以定寬字串承載

## 狀態

**已採納（Accepted，2026-07-27）** —— 決策已執行。

## 背景

框架原本只提供兩種時間語意：**日曆日**（`FieldDbType.Date`，[ADR-031](adr-031-calendar-day-column-semantics.md)）
與**時間點**（`FieldDbType.DateTime`，[ADR-032](adr-032-datetime-timezone.md)）。

第三種語意缺席：**時刻** —— 一日之內的牆上位置，不繫於特定日期。班別起訖、營業時間、
提醒時刻都是這一類。缺少型別的後果不只是表達力：UI 端無從得知該給時刻編輯控件，
報表與 schema-less 消費端也無法判別某個字串欄位其實是時刻。

用詞在本 ADR 中固定為四個互斥的詞：

| 詞 | 語意 | 承載 |
|----|------|------|
| 日曆日 | 哪一天 | `DateOnly` / `Date` |
| **時刻** | 幾點（一日之內） | 語意 `TimeOnly`，儲存定寬字串 / `Time` |
| 時間點 | 哪一天的幾點 | `DateTime` / `DateTime` |
| 時距 | 多久 | `TimeSpan`（尚無對應 `FieldDbType`） |

## 決策

**`FieldDbType.Time` 的值以定寬 5 碼字串 `"HH:mm"` 承載，於資料庫與 `DataSet` 皆然；
程式碼經 `ValueUtilities.CTimeOnly` 取得 `TimeOnly`。**

| 層 | 型別 |
|----|------|
| DB 欄位 | `nchar(5)`（SQL Server）／`char(5)`（PostgreSQL）／`CHAR(5)`（MySQL）／`VARCHAR(5)`（SQLite）／`VARCHAR2(5)`（Oracle） |
| `DataColumn.DataType` | `typeof(string)` |
| 取值層 | `ValueUtilities.CTimeOnly(object) → TimeOnly?` |
| 正規化 | `ValueUtilities.CTimeString(object) → "HH:mm"`（未填或格式不合為空字串） |

**值域 `00:00`–`23:59`，精度到分。** 需要秒的是打卡流水那類**時間點**，應使用 `DateTime`。

附帶的五個決定：

1. **`FieldDbType.Time` 必須存在，不可退回「用 `String` 欄位自行約定格式」。**
   語意標記正是這個型別存在的理由；底層存什麼與標記無關。
2. **空值即空字串，欄位維持 NOT NULL。** 時刻沒有可用的 sentinel —— `00:00` 是合法的午夜。
   `GetDefaultValue(Time)` 因此回空字串而非 `"00:00"`。
3. **範圍與格式由取值層把關，不強制 DB CHECK。** `TimeOnly.TryParseExact` 一條即足；
   五家 CHECK 語法各異、維護成本高，且擋不住繞過框架的直接 SQL。
4. **顯示格式 = 儲存格式。** UI 不做語系感知的格式化，只負責輸入遮罩與失焦正規化。
5. **列舉值 append 至尾端。** `FieldDbType` 以底層整數上 MessagePack wire，插入中間會位移既有值。

## 理由

### 為何不用資料庫原生時刻型別

原案為「DB 用原生 `time`、`DataColumn` 用 `TimeSpan`、取值層用 `TimeOnly`」，實測後否決。
實測環境：`Microsoft.Data.SqlClient` 7.0.0、`Npgsql` 9.0.4、`MySqlConnector` 2.4.0、
`Oracle.ManagedDataAccess.Core` 23.26.200、`MessagePack` 3.1.7。

**1. `DataSet` 拒收 `TimeOnly`。** `DataColumn(typeof(TimeOnly))` 可建、可賦值、`WriteXml`
也寫得出來，但 `ReadXml` 擲 `InvalidOperationException: Type 'System.TimeOnly' is not allowed here`
（.NET 的 `DataSet` 允許型別白名單）。框架以 `DataSet` XML 持久化，這條路直接斷。

**2. provider 讀出端一律給 `TimeSpan`。** SQL Server / PostgreSQL / MySQL 三家的參數層
`TimeOnly` 與 `TimeSpan` 都收，但 `DataTable` 讀回來的欄位型別全部是 `TimeSpan`。
原案的 `DataColumn` 因此只能是 `TimeSpan` —— 而 `TimeSpan` 在 raw SELECT 與 XML
（ISO 8601 duration `PT8H30M15S`）下都不可讀。

**3. Oracle 沒有 `TIME`，且框架綁不出 interval。** `INTERVAL DAY(0) TO SECOND(6)` 以參數寫入時擲
`ORA-50028: Invalid parameter binding` —— `DbCommandSpec` 走通用 `DbType`，而 Oracle 的 interval
綁定需要顯式 `OracleDbType.IntervalDS`。可修，但那是原案獨有的成本。

**4. 各家原生 `TIME` 的語意本身不一致。** SQL Server `time(7)` 與 PostgreSQL `time` 是時刻，
**MySQL `TIME` 是時距**（`-838:59:59` – `838:59:59`）。原案必須在抽象層額外釘死範圍並自行收斂。

### 定寬字串換來的東西

| 原案的成本 | 字串承載 |
|-----------|---------|
| Oracle 綁定特例 | 消失，五家無特例 |
| MessagePack / JSON / XML 三份管線各需補分支 | 消失，`string` 全通 |
| `TimeSpan` / `TimeOnly` 承載型別拉扯 | 消失 |
| 無空值 sentinel、被迫允許 NULL | 消失，空字串即未填 |
| raw SELECT 不可讀 | 解決 |

且**排序與範圍查詢照常**：定寬零填補的 `"HH:mm"` 字典序即時序，`BETWEEN '08:00' AND '17:00'`
直接成立；數字字串在任何 collation 下排序一致。正規化（`"8:30"` → `"08:30"`）是這個保證的前提，
故在 `FieldDbTypeExtensions.ToFieldValue` 統一執行。

**先例**：SAP 的 `TIMS` 即 `CHAR(6)`（`HHMMSS`）、`DATS` 即 `CHAR(8)`。以定寬字串承載日期時刻
在 ERP 是行之有年的做法。

### 為何取值層回 `TimeOnly?` 而非沿用 `Cxxx` 家族形狀

`CDateOnly(object, DateOnly defaultValue = default)` 的空值回 `0001-01-01`，這安全，
因為它不是合法業務值。但 `default(TimeOnly)` = `00:00` **是**完全合法的時刻，
照抄會讓未填欄位靜默變成午夜。`CTimeOnly` 因此回 nullable，由型別逼呼叫端處理未填。

## 取捨

- **schema 反推撞牆（唯一的新代價）**：資料庫把欄位報成 5 長度字串，永遠不會報成 `Time`。
  若不處理，`TableSchemaComparer` 每次比對都判定有差異、無止境重發 ALTER。
  解法是在 `DbField.Compare` 將兩側**化約為物理形狀**（`Time` → `String(5)`）後再比較。
  未採「以 DB extended property 存標記」：SQL Server 有現成機制，但 MySQL / SQLite 無等價機制，
  五家做不齊會變成 provider 特例。
- **精度止於分**：需要秒的場景改用 `DateTime`。
- **DB 端無法用時間函數**：對「宣告型」的時刻資料（班別、營業時間）幾乎不需要；
  真要算術時由呼叫端 `CTimeOnly` 後在 C# 端處理。
- **舊 client 破口**：新 server 回傳 `Time` 給舊 client，舊 client 的 `DbTypeConverter.ToType`
  走 `default:` 擲例外。**接受並以 breaking 標記處理**，理由同 [ADR-030](adr-030-messagepack-name-based-keys.md)：
  client 與 server 同版發佈、無外部消費者。為單一列舉值寫版本協商機制不成比例。

## 影響

- `FieldDbType` 新增 `Time`（append 至尾端）；`DbTypeConverter` 映至 `typeof(string)` / `DbType.String`。
- `FieldDbTypeExtensions`：`GetDefaultValue` 回空字串、`ToFieldValue` 正規化為定寬 `"HH:mm"`。
- `ValueUtilities`：新增 `CTimeOnly` / `CTimeString` 與 `TimeOnlyFormat` / `TimeOnlyLength`。
- `DbField.Compare`：兩側化約為物理形狀後比較（見「取捨」）。
- 五家 provider：型別對應、預設值運算式與字面值、`AlterCompatibilityRules` 的字串家族歸類。
- `ExpressionPolicy.CoerceValue`：`TimeOnly` → `string` 的邊界轉換（`TimeOnly` 非 `IConvertible`，
  否則會從 `Convert.ChangeType` 擲出）。
- **UI 層與公開文件尚未實作**，見後續階段。

**回歸守衛**：`tests/Bee.Db.UnitTests/TimeOfDayColumnIntegrationTests.cs` 於五家資料庫建表、
round-trip，並斷言時刻欄位的 schema 比對收斂 —— 物理形狀化約一旦遺失，該斷言即失敗。
單元測試抓不到這個回歸。

## 相關

- [ADR-031：日曆日欄位語意](adr-031-calendar-day-column-semantics.md) —— 第一種時間語意，
  採「CLR 型別 + `ExtendedProperties` 標記」；`Time` 不需標記以外的手段區分，因為它自有 CLR 表示。
- [ADR-032：DateTime 時區處理](adr-032-datetime-timezone.md) —— 時刻與日曆日同列「絕不轉時區」。
  改採字串承載後更安全：字串不可能被誤判為時間點而位移。
- [ADR-030：MessagePack 合約改採 property-name key](adr-030-messagepack-name-based-keys.md) ——
  舊 client 破口的處置沿用其理由。
