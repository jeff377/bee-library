# 日曆日與時間點的欄位語意

[English](date-semantics.md)

`FieldDbType` 區分 `Date`（日曆日：生日、發票日期、帳期）與 `DateTime`（時間點：建立時刻、
登入時刻）。.NET 只有單一 `DateTime` 型別，這個區別很容易在傳遞過程中消失——在 v4.15 之前，
框架確實在值進入 CLR 與 wire 之後就把它弄丟了。

本文說明改了什麼、你不必動手就能得到什麼，以及唯一需要自行宣告語意的情況。

> 設計理由與背後的實測數據：[ADR-031](adr/adr-031-calendar-day-column-semantics.md)。

---

## 1. 摘要

| 問題 | 答案 |
|------|------|
| `DataColumn.DataType` 變了嗎？ | **沒有。** 日曆日欄位仍是 `DateTime` 欄位。`RowFilter`、`Sort`、`Compute`、字串寫回、既有 cast 全部不變。 |
| 那改了什麼？ | 欄位**宣告的** `FieldDbType` 現在會跟著欄位走，並正確出現在 wire 上。過去它是從 CLR 型別反推的，而反推會把每個日曆日都回報成 `DateTime`。 |
| payload 結構變了嗎？ | **沒有。** 兩種 wire 格式本來就逐欄攜帶 `FieldDbType`，只是值變準確。既有 client 照常運作。 |
| 我需要做什麼嗎？ | 只有**自寫 SQL** 需要。schema 驅動的查詢會自行標記。 |
| 有破壞性變更嗎？ | `ValueUtilities.CDate` 改為回傳 `DateOnly`。見 §5。 |

## 2. 不必動手就能得到的部分

任何由框架依 `FormSchema` 建立的 `DataTable` 都會帶著宣告的欄位型別：

- `DataFormRepository` 的查詢——`GetList`、`GetData`（master 與 detail）。
- `GetNewData` 建立的空白骨架。
- 任何經由 `DataTableExtensions.AddColumn(name, FieldDbType)` 建立的欄位。

標記在兩種 wire 格式的兩個方向都會保留，因此 client 反序列化後看到的語意，
與伺服端送出的完全一致。

## 3. 讀取語意

### .NET 消費端

```csharp
using Bee.Base.Data;

var dbType = column.ResolveFieldDbType();          // 有標記回標記值，未標記則反推
if (dbType == FieldDbType.Date)
{
    // 日曆日：不要做時區位移、不要顯示時刻
}

var declared = column.GetDeclaredFieldDbType();    // 未標記時回傳 null
```

`ResolveFieldDbType` 在欄位未標記時會回退為由 `DataColumn.DataType` 反推，
因此隨時呼叫都安全——未標記的 `DateTime` 欄位讀出來就是 `FieldDbType.DateTime`，與過去相同。

### JavaScript / TypeScript 消費端

欄位的 `type` 一直都在，現在它變準確了：

```jsonc
{
  "name": "order_date",
  "type": "Date",           // v4.15 之前是 "DateTime"
  "allowNull": false,
  // …
}
```

```ts
const isCalendarDay = (col: DataTableColumn) => col.type === 'Date';
```

`Date` 欄位請當成單純的日曆日期處理：不顯示時刻，且**不要**透過瀏覽器時區轉換——
在 UTC 以西的時區，`new Date("2026-07-25T00:00:00")` 會落到前一天。
周邊的 wire 結構見 [JSON-RPC 前端串接](jsonrpc-frontend-integration.zh-TW.md)。

## 4. 自寫 SQL：由你宣告

ADO.NET 把 `date` 欄位一律回報為 `System.DateTime`，因此非框架產生的查詢沒有任何可據以還原
語意的來源。規則是：

> **框架產生的 SQL 由框架標記；你自己寫的 SQL 由你標記。**

兩種等價寫法，共用同一份實作：

```csharp
// A. 宣告貼著查詢寫——選項與 SQL 同處一地。
var spec = new DbCommandSpec(DbCommandKind.DataTable,
    "SELECT order_date, created_at, amount FROM ft_order WHERE amount > {0}", 1000m);
spec.DateColumns.Add("order_date");
var table = dbAccess.Execute(spec).Table!;

// B. 事後標記——適用於自行組裝、或來自他處的表格。
table.SetDateColumns("order_date", "due_date");
```

兩者的欄名比對都**不區分大小寫**（結果欄名已正規化為小寫），且對**比對不到的欄名一律擲例外**
而非略過——打錯字時「看起來宣告了、實際沒作用」正是這個機制要消除的失敗模式。
把 `DateColumns` 用在不回傳表格的 `DbCommandKind` 上同樣會擲例外，理由相同。

若你手上已經有對應的 `FormTable`，可以直接重播整份 schema，不必逐欄列名：

```csharp
using Bee.Definition.Forms;

formTable.ApplyFieldDbTypes(table);   // 標記 schema 宣告的每個欄位
```

schema 未涵蓋的欄位會被略過（彙總欄、運算式欄屬常態），
schema 宣告了但查詢未回傳的欄位也不會報錯（部分欄位查詢屬常態）。

**忘了宣告是本設計保留下來的唯一失敗模式。** 未標記的日曆日欄位對下游而言就是時間點——
影響最大的是時區轉換，可能造成跨日偏移。

## 5. 破壞性變更：`ValueUtilities.CDate`

```csharp
// 之前
public static DateTime CDate(object value, DateTime defaultValue = default)

// v4.15 起
public static DateOnly CDate(object value, DateOnly defaultValue = default)
```

寫成 `DateTime d = ValueUtilities.CDate(x)` 的呼叫端會變成**編譯錯誤**，而非 runtime 失敗。
兩種遷移方式：

```csharp
DateOnly day = ValueUtilities.CDate(row["order_date"]);        // 要日曆日
DateTime dt  = ValueUtilities.CDateTime(row["order_date"]);    // 要 DateTime
```

`CDateTime` 未變更，且現在也接受 `DateOnly` 輸入。

> **不要把 `DateOnly` 寫回 `DataTable`。** 日曆日欄位是帶著標記的 `DateTime` 欄位，
> `DataColumn` 會直接拒絕 `DateOnly` 值——`DateOnly` 未實作 `IConvertible`，
> 一般的轉換路徑根本不會執行。值要寫回列時請用 `CDateTime`。

## 6. 同一機制順帶修好的部分

另外三組 `FieldDbType` 同樣共用 CLR 型別而無法還原，現在在 wire 上也變準確了：

| 宣告型別 | CLR 型別 | 過去回報 | 現在回報 |
|---------|---------|---------|---------|
| `Date` | `DateTime` | `DateTime` | `Date` |
| `Text` | `string` | `String` | `Text` |
| `Currency` | `decimal` | `Decimal` | `Currency` |
| `AutoIncrement` | `int` | `Integer` | `AutoIncrement` |

由於 `DbTypeConverter.ToType` 對這些值對映到的 CLR 型別與過去相同，
client 依 payload 重建表格時得到的**欄位型別完全一致**，只有回報的 `FieldDbType` 改變。

## 相關文件

- [ADR-031：日曆日欄位語意](adr/adr-031-calendar-day-column-semantics.md)——決策本身、被否決的替代方案，以及背後的 `DataColumn`/`DateOnly` 實測數據
- [JSON-RPC 前端串接](jsonrpc-frontend-integration.zh-TW.md)——JS/TS 消費端的 wire 結構
- [資料庫命名規範](database-naming-conventions.zh-TW.md)——欄位命名與跨 DB 大小寫敏感度
- [開發限制與反面模式](development-constraints.zh-TW.md)——撰寫 AnyCode 查詢前值得一讀的框架限制
