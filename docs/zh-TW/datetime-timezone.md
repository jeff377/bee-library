# 時區處理

[English](../en/datetime-timezone.md) · [← 文件索引](README.md)

資料庫的每個時間點都以 UTC 儲存，每位使用者看到的則是自己時區的時間。轉換只發生在一個地方
——用戶端的 API connector——因此你的 Business Object 與 UI 程式碼都不需要自己換算。

`DateTime` 值只接受伺服端寫入：存檔時用戶端送來的 `DateTime` 不會被採用，而是由伺服端補值，
或以資料庫裡的原值覆蓋。

本文說明框架替你做了什麼、哪些情況需要你動手，以及如何設定使用者時區。

> 設計理由與背後的實測：[ADR-032](../adr/adr-032-datetime-timezone.md)。
> 日曆日與時間點的語意區別，以及另外兩種時間型別：[時間型別總覽](temporal-types.md)。

---

## 1. 摘要

| 問題 | 答案 |
|------|------|
| 時間在哪裡轉換？ | 用戶端的 `Connector`。回應裡的 `DateTime` 轉入使用者時區；請求只轉過濾條件的值。其他地方都不轉。 |
| 資料庫存什麼？ | UTC，存在無時區的一般欄位（`datetime2`、`timestamp`、`DATETIME`、`TIMESTAMP`）。 |
| wire 上傳什麼？ | 回應一律是 UTC。請求中過濾條件的值是 UTC；存檔送出的 `DataSet` 保留畫面上的值，伺服端不採用其中的 `DateTime`。 |
| 用戶端送來的 `DateTime` 會存進資料庫嗎？ | 不會。新增列由伺服端補值，修改與刪除列以資料庫的原值覆蓋，`sys_insert_time` / `sys_update_time` 由框架戳記。見 §2。 |
| 哪些欄位會被轉換？ | CLR 型別為 `DateTime`、且沒有標記為 `Date` 的欄位。帶著 `Date` 標記的日曆日欄位不轉換；未標記的會被當成時間點轉換，見 §3。 |
| 強型別屬性與 `Parameters` 呢？ | 兩個方向都不轉，一律是 UTC，由呼叫端負責（例如 `ExpiredAt`、`FromUtc` / `ToUtc`）。 |
| 使用者的時區從哪來？ | `st_user.time_zone`，隨 session 帶出——絕不取裝置時區。 |
| 我的 BO 要改嗎？ | 不用，除非它自寫 SQL 且以日期做過濾，或需要接受使用者輸入的 `DateTime`。見 §3。 |

## 2. 什麼都不做就有的行為

由 `FormSchema` 產出的 `DataSet` / `DataTable` 會攜帶每個欄位宣告的 `FieldDbType`，connector 據此判斷：

- `DateTime` 欄位在收到時由 UTC 轉為使用者時區。
- `Date` 欄位維持原樣。位移一個日曆日會把生日或發票日期挪到錯誤的那一天。

存檔時 connector 不換算 `DataSet`，伺服端的 `FormBusinessObject.Save` 也不採用其中的 `DateTime`。
它在執行任何規則之前，先把這些欄位換成伺服端的值：

- 新增列：`sys_insert_time`、`sys_update_time`，以及沒有預設值運算式的 `DateTime` 欄位，填入存檔當下的 UTC；
  有 `DefaultValueExpression` 的欄位交給運算式求值。
- 修改與刪除列：`DateTime` 欄位以資料庫的原值覆蓋，修改列的 `sys_update_time` 再填入存檔當下的 UTC。

因此規則、稽核與寫入看到的都是 UTC；只改了別的欄位就存回時，時間值也不會因為換算而改變，
包括 DST 回撥重疊的那一小時。伺服端 BO 之間呼叫 `Save` 時同樣如此。

UI 新增的列會以使用者自己的今天填入預設值——在紐約登打台北帳號的假單，請假日期仍是台北的日期。

由於判斷依據是欄位標記而非查 schema，這一切對報表與 AnyCode 的結果同樣成立，即使它們背後沒有
`FormSchema`。

## 3. 需要你動手的情況

### 自寫 SQL 且結果含日曆日欄位

框架會替由 schema 產生的欄位加標記。你自己寫的查詢必須宣告哪些是日曆日欄位，否則 connector 會把它們
當成時間點而位移、造成跨日：

```csharp
var command = new DbCommandSpec(DbCommandKind.DataTable, sql) { DateColumns = { "invoice_date" } };
```

這與[時間型別總覽 §4](temporal-types.md) 描述的是同一件事，時區不需要額外宣告。

### 過濾條件的值

過濾條件是請求方向唯一會換算的地方。它沒有欄位可依附，因此**由值本身的型別表達語意**：

```csharp
FilterCondition.Equal("invoice_date", someDateOnly);   // 日曆日——絕不位移
FilterCondition.Equal("created_at", someDateTime);     // 時間點——送出時轉為 UTC
```

該用日曆日時誤傳 `DateTime` **不會有任何錯誤**，只是在接近午夜時查到錯誤的資料列——這是最難察覺的
一類錯誤。欄位是 `Date` 時請優先使用 `DateOnly`（`ValueUtilities.CDateOnly` 回傳的正是它）。

### 需要接受使用者輸入的 `DateTime`

框架預設不支援。純 `FormSchema` 表單的 `DateTime` 欄位只能由伺服端寫入，使用者要編輯的日期請設計成
`Date` 欄位；系統時間戳記欄則在 `FormSchema` 標 `ReadOnly`，免得使用者改一個存不進去的值。

確實需要時，在自訂 BO 覆寫 `FormBusinessObject.NormalizeDateTimes`：先讀出使用者送來的值，呼叫基底實作，
再把值依使用者時區轉成 UTC 寫回。授權與寫入範圍檢查在它之前就已經執行。

### JavaScript 與其他非 .NET 用戶端

這些用戶端沒有 connector 代勞：顯示 `DateTime` 值時由 UTC 換算，過濾條件的 `DateTime` 值送出前換回 UTC。
存檔送出的 `DataSet` 不必換算，伺服端不採用其中的 `DateTime`。`Date` 值則必須原樣傳遞——尤其別讓
`new Date(...)` 用瀏覽器時區重新解讀它。欄位型別會隨 payload 一起送達，用戶端不需額外取 metadata
就能分辨兩者，見 [jsonrpc-frontend-integration.md](jsonrpc-frontend-integration.md)。

## 4. 設定使用者時區

`st_user.time_zone` 存 IANA id（`Asia/Taipei`、`America/New_York`）。登入時複製到 session 並回傳給用戶端。

使用者若沒有自己的值，會退回 `BackendConfiguration.DefaultTimeZone`，其出廠預設為 `Asia/Taipei`。
請把它設成部署實際所在的時區——或設為空字串以採用 UTC，因為**所有轉換點對空時區一律視為 UTC**。

**框架絕不退回裝置時區**：否則使用者帶著筆電移動就會改變自己輸入資料的意義，
而且「看到的值」與「伺服端存下的值」會來自兩個不同來源。

框架**刻意不提供**公司層級或欄位層級的覆寫。若某個值必須以**另一個**時區呈現——例如出勤紀錄要看
員工工作地的時區——請以「UTC 時間欄 + 自訂的時區欄」建模，因為那個需求是逐列的，任何欄位層級的
設定都表達不了。

## 5. `DataSet` 以外的日期

日期在框架中一律以 `DateOnly` 表達，**唯一例外是 `DataSet` 儲存格**——`DataColumn` 只能承載
`DateTime`，框架會在該邊界替你轉換。運算式的 `Today()` 回傳依使用者時區的 `DateOnly`，
`UtcNow()` 則明示 UTC。完整函式清單見[運算式規則](expression-rules.md)。

## 相關文件

- [時間型別總覽：`Date`、`DateTime`、`Time`](temporal-types.md) —— 跨層對照參考：
  三種語意如何選擇，以及各自在每一層的承載方式。
- [ADR-032](../adr/adr-032-datetime-timezone.md) —— 決策本身與背後的實測數據。
