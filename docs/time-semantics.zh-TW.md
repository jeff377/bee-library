# 時刻欄位

[English](time-semantics.md)

`FieldDbType.Time` 是框架的第三種時間語意，與 `Date`（日曆日）、`DateTime`（時間點）並列。
時刻是**一日之內的牆上位置**，不繫於任何日期：班別起訖、營業時間、提醒時刻。

值以定寬 `"HH:mm"` 字串儲存 —— 資料庫、`DataSet`、wire 三處皆然。程式碼中以 `TimeOnly` 讀取。

> 設計理由（包含為何是字串而非資料庫原生時刻型別）：
> [ADR-033](adr/adr-033-time-of-day-semantics.md)。

---

## 1. 摘要

| 問題 | 答案 |
|------|------|
| 欄位的 CLR 型別是什麼？ | **`string`**。儲存格內容為 `"08:30"`。 |
| 如何讀成時刻？ | `ValueUtilities.CTimeOnly(row["work_start"])` → `TimeOnly?`。 |
| 值域為何？ | `00:00`–`23:59`，**精度到分**，不含秒。 |
| 「未填」如何表示？ | **空字串**。絕不是 `"00:00"` —— 午夜是合法值。 |
| 在 SQL 裡排序正確嗎？ | **正確**。定寬零填補的值，字典序即時序。 |
| 會受時區影響嗎？ | **不會**。時刻與日曆日同為牆上時間，絕不位移。 |
| 有破壞性變更嗎？ | 新增列舉成員屬 wire 變更，client 與 server 須同版。見 §6。 |

## 2. 何時該用

| 資料 | 型別 | 理由 |
|------|------|------|
| 班別起訖、營業時間、提醒時刻 | **`Time`** | 每天重複的宣告，不附帶日期 |
| 打卡記錄、稽核時戳 | `DateTime` | 必須知道**哪一天**。夜班 06:00 下班是**隔天**的 06:00，只存時刻會永久遺失這個資訊 |
| 某件事花了多久 | `Decimal`（小時） | 時距不是時刻；框架尚無時距型別 |

若你發現自己在相減兩個 `Time` 欄位以求長度，那就是「其實你要的是時距」的訊號。見 §7。

## 3. 宣告時刻欄位

無需特別處理，比照其他欄位型別宣告：

```xml
<DbField FieldName="work_start" Caption="上班時刻" DbType="Time" />
```

在每一種支援的資料庫上都會建成定寬 5 字元欄位：

| 資料庫 | 欄位型別 |
|--------|---------|
| SQL Server | `nchar(5)` |
| PostgreSQL | `char(5)` |
| MySQL | `CHAR(5)` |
| SQLite | `VARCHAR(5)` |
| Oracle | `VARCHAR2(5)` |

版面層會自動將 `Time` 欄位解析為 `ControlType.TimeEdit`，因此不需改動 layout 即可得到時刻輸入控件。

## 4. 讀取與寫入

### .NET

```csharp
// 讀取
TimeOnly? start = ValueUtilities.CTimeOnly(row["work_start"]);
if (start is null) { /* 未填 */ }

// 寫入 —— 框架在寫入時正規化，"8:30" 會存成 "08:30"
row["work_start"] = FieldDbType.Time.ToFieldValue("8:30");   // "08:30"
```

單參數多載回傳 **`TimeOnly?`**，逼呼叫端處理未填的情況，而不是把空欄位靜默讀成午夜 ——
時刻沒有多餘的值可以代表「未設定」，`default(TimeOnly)` 就是 `00:00`。
需要非 null 值時請顯式傳入 fallback：

```csharp
TimeOnly start = ValueUtilities.CTimeOnly(row["work_start"], new TimeOnly(9, 0));
```

整個時間家族（`CDateOnly` / `CDateTime` / `CTimeOnly`）採同一形狀。

它對輸入寬鬆（`"8:30"`、`DateTime`、範圍內的 `TimeSpan` 皆接受），對輸出嚴格：
超出範圍或格式不合一律回 `null`。

### JS / TS 用戶端

值就是字串，不需要任何剖析輔助：

```js
const start = row.current.work_start;   // "08:30"，未填時為 ""
```

該欄在 wire 上的 `FieldDbType` 為 `Time`，因此 schema-less 的消費端不必另取 schema
即可分辨這是時刻而非任意文字欄位。

## 5. 查詢

由於值是定寬零填補，一般字串比較即為時序比較：

```sql
SELECT * FROM ft_shift WHERE work_start BETWEEN '08:00' AND '17:00' ORDER BY work_start
```

此性質在每一種支援的資料庫、每一種 collation 下都成立 —— 涉及的字元只有數字與冒號。

> 這個保證的前提是值必須零填補。凡經 `ToFieldValue` 或時刻編輯控件寫入的值框架都會正規化，
> 唯一可能破壞它的是自寫的 `INSERT`。

## 6. 破壞性變更

`FieldDbType` 新增了成員。該值以底層整數上 wire，因此含 `Time` 欄位的表無法被舊版 client
反序列化 —— 它會擲例外而非靜默讀錯。**client 與 server 必須跑相同（或相容）版本。**

除了兩端一起升級之外，不需要其他動作。

## 7. `Time` 不是什麼

**它不是時距。** `Time` 回答「幾點」，不是「多久」。框架尚無時距型別，目前請用 `Decimal`（小時）。

若你打算用 `TimeOnly` 相減兩個 `Time` 欄位來算班長，這點很重要 ——
`TimeOnly` 的減法**繞過午夜且恆為正值**：

| 運算 | 結果 |
|------|------|
| `22:00` → `06:00` | 8 小時 —— 夜班正確 |
| `08:00` → `08:00` | **0 小時**，不是 24 |

繞回對夜班是對的，但對 24 小時班會靜默算錯 —— 在模 24 的世界裡，「一整天」與「零」是同一個點。
**班長請存成獨立欄位，不要用相減推導。**

**它沒有秒。** 需要秒表示你描述的是事件而非宣告，該用 `DateTime`。

## 相關文件

- [ADR-033](adr/adr-033-time-of-day-semantics.md) —— 為何採定寬字串而非資料庫原生時刻型別，
  含決策背後的實測數據。
- [日曆日與時間點的欄位語意](date-semantics.zh-TW.md) —— `Date` / `DateTime` 的區別。
- [時區處理](datetime-timezone.zh-TW.md) —— 時間點如何在 UTC 與使用者時區間轉換。
  時刻與日曆日一樣，絕不轉換。
