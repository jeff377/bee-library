# 時間型別總覽：`Date`、`DateTime`、`Time`

[English](temporal-types.md)

框架區分三種時間語意，而每一種在各層 —— 資料庫欄位、`DataColumn`、CLR 值、三種序列化 ——
的承載方式都不同。本文是跨層的單一對照參考；各語意的深入說明見文末連結。

---

## 1. 該用哪一種

| 語意 | `FieldDbType` | 回答的問題 | 例子 |
|------|---------------|-----------|------|
| **日曆日** | `Date` | *哪一天？* | 生日、發票日期、會計期間 |
| **時間點** | `DateTime` | *哪一天的幾點？* | 建立時間、登入時戳、打卡記錄 |
| **時刻** | `Time` | *幾點（一日之內）？* | 班別起訖、營業時間、提醒時刻 |

判別法：**問這個值需不需要知道是哪一天。**

- 需要 → **時間點**。
- 不需要，而問的是*幾點* → **時刻**。
- 不需要，而問的是*哪一天* → **日曆日**。
- 問的是*多久* → 那是**時距**，框架尚無對應型別，目前以 `Decimal`（小時）承載。見 §8。

## 2. 端到端對照

| | `Date` | `DateTime` | `Time` |
|---|--------|-----------|--------|
| 宣告方式 | `DbType="Date"` | `DbType="DateTime"` | `DbType="Time"` |
| `DataColumn.DataType` | `DateTime` | `DateTime` | **`string`** |
| 光看 CLR 型別能分辨嗎？ | **不能** —— 與 `DateTime` 共用 | 不能 | **能** |
| 語意如何保留 | `ExtendedProperties` 標記 | （該 CLR 型別的預設語意） | CLR 型別本身 |
| 讀取方式 | `CDateOnly` → `DateOnly?` | `CDateTime` → `DateTime?` | `CTimeOnly` → `TimeOnly?` |
| 未填值 | `DateTime.MinValue` → `DBNull` | `DateTime.MinValue` → `DBNull` | **空字串** |
| 會轉時區嗎？ | **絕不** | **會**（UTC ↔ 使用者時區） | **絕不** |
| 預設 UI 控件 | `DateEdit` | `DateEdit` | `TimeEdit` |

唯一的結構性差異：`Date` 與 `DateTime` **共用 CLR 型別**，日曆日語意在值離開定義層的瞬間就會消失，
因此靠欄位上的顯式標記保留。`Time` 不需要標記 —— `string` 欄位本身已無歧義。

## 3. 資料庫層

| 資料庫 | `Date` | `DateTime` | `Time` |
|--------|--------|-----------|--------|
| SQL Server | `date` | `datetime2(7)` | `nchar(5)` |
| PostgreSQL | `date` | `timestamp` | `char(5)` |
| MySQL | `DATE` | `DATETIME(6)` | `CHAR(5)` |
| SQLite | `DATE` | `DATETIME` | `VARCHAR(5)` |
| Oracle | `DATE` | `TIMESTAMP(6)` | `VARCHAR2(5)` |

兩點值得知道：

- **`DateTime` 以 UTC 存於不帶時區的欄位。** 沒有任何 provider 儲存位移量，轉換由框架在讀出時完成。
  見[時區處理](datetime-timezone.zh-TW.md)。
- **`Time` 不使用資料庫原生時刻型別。** 除 Oracle 外每家都有，但它們的語意彼此不一致
  （MySQL 的 `TIME` 是**時距**，跨越 ±838 小時），且 .NET `DataSet` 承載不了它們回傳的 CLR 型別。
  定寬字串一次繞開全部問題，且在 raw `SELECT` 下仍可讀。完整實測見
  [ADR-033](adr/adr-033-time-of-day-semantics.md)。

### 排序與範圍查詢

三者在 SQL 中都能正確排序與範圍掃描。`Time` 之所以成立，是因為值**定寬且零填補**，
使字典序即為時序：

```sql
SELECT * FROM ft_shift WHERE work_start BETWEEN '08:00' AND '17:00' ORDER BY work_start
```

凡經 `ToFieldValue` 或時刻編輯控件寫入的值框架都會正規化，唯一可能破壞此保證的是自寫的 `INSERT`。

## 4. `DataSet` 層

```csharp
table.AddColumn("hire_date",  FieldDbType.Date);       // DataColumn.DataType == typeof(DateTime)
table.AddColumn("created_at", FieldDbType.DateTime);   // DataColumn.DataType == typeof(DateTime)
table.AddColumn("work_start", FieldDbType.Time);       // DataColumn.DataType == typeof(string)
```

框架建立的每個 `DataTable` 都帶著宣告的型別，可用下列方式取回：

```csharp
FieldDbType declared = column.ResolveFieldDbType();    // Date / DateTime / Time
```

由 schema 驅動的查詢（`GetList`、`GetData`、`GetNewData`）與所有經
`AddColumn(name, FieldDbType)` 建立的欄位都會自動帶上。**自寫 SQL 是唯一的例外** ——
見[日曆日與時間點的欄位語意 §4](date-semantics.zh-TW.md)。

> **不要把 `DateOnly` 寫回 `DataTable`。** 日曆日欄位是帶著標記的 `DateTime` 欄位，
> `DataColumn` 會直接拒絕 `DateOnly` —— 它未實作 `IConvertible`，一般的轉換路徑根本不會執行。
> 寫回時請用 `CDateTime`。

## 5. 程式碼層

```csharp
// 單參數 → nullable。未填是編譯器逼你處理的情況。
DateOnly? day     = ValueUtilities.CDateOnly(row["hire_date"]);
DateTime? instant = ValueUtilities.CDateTime(row["created_at"]);
TimeOnly? start   = ValueUtilities.CTimeOnly(row["work_start"]);

// 雙參數 → 非 null，且 fallback 明寫在呼叫端。
DateTime created = ValueUtilities.CDateTime(row["created_at"], DateTime.MinValue);
```

整個家族有兩個一致性質：

- **方法名與回傳型別一致**，呼叫端不必回查即知拿到什麼。
- **單參數多載一律回傳 nullable。** 未填因而是編譯器強制處理的情況，
  而不是要記得比對的 sentinel —— 而 sentinel 外洩是真實的災難：
  報表上印出 `0001-01-01` 比在邊界擲 null 例外更糟。

需要非 null 值時請顯式傳入 fallback。「明寫」正是重點：它讓選擇可見，
而不是藏在被省略的預設參數裡。

## 6. 序列化

三種格式都是自我描述的：欄位的 `FieldDbType` 隨 payload 一起傳遞，消費端**不必另取 schema**
即可分辨日曆日與時間點。

以下範例是這三個值的**實際序列化輸出**：
`hire_date = 2026-07-27`、`created_at = 2026-07-27 08:30:15.1234567`、`work_start = 08:30`。

### XML —— `DataSet` 持久化

宣告型別會寫入 XSD 的 `msprop` 註記，因此能在寫入／讀回的往返中存活：

```xml
<xs:element name="hire_date"  msdata:DateTimeMode="Unspecified" msprop:Bee.FieldDbType="Date"     type="xs:dateTime" />
<xs:element name="created_at" msdata:DateTimeMode="Unspecified" msprop:Bee.FieldDbType="DateTime" type="xs:dateTime" />
<xs:element name="work_start"                                   msprop:Bee.FieldDbType="Time"     type="xs:string" />

<hire_date>2026-07-27T00:00:00</hire_date>
<created_at>2026-07-27T08:30:15.1234567</created_at>
<work_start>08:30</work_start>
```

`DateTimeMode="Unspecified"` 正是讓 XML 不含時區位移的關鍵。.NET 對新建 `DateTime` 欄位的預設是
`UnspecifiedLocal`，那**會**寫入位移量 —— 框架一律設為 `Unspecified`，
使持久化的 `DataSet` 在他處讀回時不會偏移。

注意完整的 100 奈秒精度得以保留。

### JSON

欄位型別以**列舉名稱**輸出：

```json
{
  "columns": [
    { "name": "hire_date",  "type": "Date" },
    { "name": "created_at", "type": "DateTime" },
    { "name": "work_start", "type": "Time" }
  ],
  "rows": [
    { "state": "Unchanged",
      "current": {
        "hire_date":  "2026-07-27T00:00:00",
        "created_at": "2026-07-27T08:30:15.1234567",
        "work_start": "08:30"
      } }
  ]
}
```

JS / TS 消費端：

```js
// 日曆日與時間點的「值」長得一樣 —— 區分它們的是欄位型別。
const day     = row.current.hire_date.slice(0, 10);   // "2026-07-27" —— 不要建成 Date 再格式化
const instant = new Date(row.current.created_at);     // 可安全轉換為使用者時區
const start   = row.current.work_start;               // "08:30"，未填時為 ""
```

> **日曆日不可經由 JS `Date` 再格式化。** 值不帶位移量，瀏覽器會以本地時間解讀，
> 西向時區會把它推移到前一天。請改為擷取日期部分。

### MessagePack

欄位型別以列舉的**底層整數**傳遞（與 JSON 用名稱不同），儲存格值則走 typeless：
`DateTime` 儲存格以 MessagePack 原生 timestamp 傳遞，`Time` 儲存格為字串。
表格 round-trip 後 CLR 型別**與標記**皆還原：

```
col hire_date : clr=DateTime marker=Date     value=2026-07-27 00:00:00
col created_at: clr=DateTime marker=DateTime value=2026-07-27 08:30:15
col work_start: clr=String   marker=Time     value=08:30
```

由於整數是位置式的，**新的 `FieldDbType` 成員一律只能 append** ——
插入到中間會讓其後所有值位移，打斷既有 payload。

### 篩選條件

`FilterCondition.Value` 走 typeless 並以白名單驗證。`System.DateTime`、`System.DateOnly`、
`System.String` 皆在白名單內，因此三種語意都能用於篩選 —— 時刻請以 `"HH:mm"` 字串傳遞：

```csharp
FilterCondition.Equal("hire_date", ValueUtilities.CDateOnly(x));   // DateOnly —— 允許
FilterCondition.Equal("work_start", "08:30");                      // string —— 允許
```

`System.TimeOnly` **不在**白名單內，進入篩選條件前請先轉為字串形式。

## 7. 時區

**只有 `DateTime` 會被轉換。** 日曆日與時刻是牆上時間，套用位移量會得到無意義的結果 ——
生日會移到前一天，08:00 的班別在另一個時區會變成 16:00 開始。

| | 儲存 | 顯示 |
|---|------|------|
| `Date` | 原值 | 原值 |
| `DateTime` | **UTC** | 轉為 session 的時區 |
| `Time` | 原值 | 原值 |

細節（含自寫 SQL 與非 .NET 用戶端該做什麼）見[時區處理](datetime-timezone.zh-TW.md)。

## 8. 三者都不是的東西：時距

三者都不回答*多久*。時距在時鐘或日曆上沒有位置 —— 工時、經過時間、逾時秒數。
**框架尚無時距型別，目前請用 `Decimal`（小時）。**

這一點在你打算用 `TimeOnly` 相減兩個 `Time` 值來求長度時最關鍵 ——
`TimeOnly` 的減法**繞過午夜且恆為正值**：

| 運算 | 結果 | 判定 |
|------|------|------|
| `22:00` → `06:00` | 8 小時 | 夜班正確 |
| `08:00` → `08:00` | **0 小時** | 錯誤 —— 24 小時班會被算成零 |

在模 24 的世界裡，「一整天」與「零」是同一個點。**時距請存成獨立欄位，不要由兩個時刻相減推導。**

## 9. 常見錯誤

| 錯誤 | 後果 | 正確做法 |
|------|------|---------|
| 用 `DateTime` 存班別定義 | 帶著無意義的日期，且會被時區位移 | `Time` |
| 用 `Time` 存打卡記錄 | 遺失哪一天；夜班 06:00 下班無法還原 | `DateTime` |
| 把 `DateOnly` 寫進 `DataTable` | 擲例外 —— `DataColumn` 拒收 | `CDateTime` |
| 日曆日經 JS `Date` 再格式化 | 西向時區會退一天 | 擷取日期部分 |
| 把 `"00:00"` 當成「未設定」 | 午夜是合法值 | 空字串才是未填 |
| 相減兩個 `Time` 求班長 | 24 小時班算成 0 | 班長存成獨立欄位 |
| 在 `FieldDbType` 中間插入新成員 | 打斷所有既有 MessagePack payload | 一律 append |

## 相關文件

- [日曆日與時間點的欄位語意](date-semantics.zh-TW.md) —— `Date` 標記的運作方式，
  以及自寫 SQL 時如何宣告。[ADR-031](adr/adr-031-calendar-day-column-semantics.md)。
- [時刻欄位](time-semantics.zh-TW.md) —— `Time` 型別的深入說明。
  [ADR-033](adr/adr-033-time-of-day-semantics.md)。
- [時區處理](datetime-timezone.zh-TW.md) —— 時間點的 UTC 儲存與轉換。
  [ADR-032](adr/adr-032-datetime-timezone.md)。
- [術語表](terminology.zh-TW.md) —— 日曆日 / 時刻 / 時間點 / 時距 四詞的定義。
