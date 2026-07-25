# Plan：區分 `DateTime` 與 `DateOnly` 型別

**狀態：📝 擬定中（2026-07-25）**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 1 | 型別對映（T1）：`DbTypeConverter` 兩個方向、`ValueUtilities`、`FieldDbTypeExtensions`、`ILMapper`、UI 繫結 | 📝 待做 |
| 2 | SQL 讀取路徑（T3）：路徑一 Repository 層依 schema 後處理 + `SetDateOnlyColumns` + `DbCommandSpec.DateOnlyColumns` | 📝 待做 |

> **兩階段必須一起發布，不可只出貨階段 1。** 見 §5「出貨約束」。

> 目標：讓 `FieldDbType.Date` 欄位在 CLR 型別上就是 `DateOnly`，使「日曆日」與「時間點」的區別
> 從定義層一路貫通到 `DataSet` 儲存格與 wire payload。
> **由定義產生的 SQL 由框架負責型別；呼叫端自寫的 SQL 由呼叫端宣告**（T3 兩路徑）。
>
> 本 plan 與 [plan-datetime-timezone.md](plan-datetime-timezone.md) 是**兩個獨立議題**，
> 依賴為單向：時區 plan 依賴本 plan，本 plan **不依賴時區設計定案**，可獨立動工與發布。

---

## 1. 問題

`FieldDbType` 刻意區分 `Date` 與 `DateTime`，正是因為 .NET 只有單一 `DateTime` 型別、
無法表達「日曆日 vs 時間點」的差別——定義層早就把這件事講清楚了。

但這個區別在 CLR 層被抹平：

```csharp
case FieldDbType.Date:
case FieldDbType.DateTime:
    return typeof(DateTime);        // DbTypeConverter.cs:124
```

連帶造成 wire payload 也失去這個資訊。`SerializableDataColumn.DataType` 逐欄攜帶
`FieldDbType`（`SerializableDataColumn.cs:21`），來源是：

```csharp
DataType = DbTypeConverter.ToFieldDbType(col.DataType)   // SerializableDataTable.cs:78
```

因為它以 **CLR 型別**為 key，而日曆日欄位的 CLR 型別是 `DateTime`，
**日曆日欄位在 wire 上永遠被標成 `FieldDbType.DateTime`**。

值得注意的是 **DB 參數層並未抹平**：`FieldDbType.Date` → `DbType.Date`
（`DbTypeConverter.cs:85`），資料庫端始終知道這是日曆日。被收斂掉的只有 CLR 表示法與 wire 標記。

### 為何現在要處理

1. **wire 上早就有這個槽位，只是被抹掉。** 改用 `DateOnly` 等於**不新增任何 wire 欄位**
   就把 payload 的自我描述能力接回來。
2. **schema-less 場景是真正的破口。** Repository 雙軌策略下，報表 / 批次（AnyCode）產出的
   `DataTable` 背後沒有 `FormSchema`，消費端**查不到欄位語意**，只能從 CLR 型別判斷。
   JS client 亦然：payload 自我描述後，JS 端不需另外取 schema。
3. **能用型別系統消除的失敗模式，不該退回靠紀律。** 替代方案（`ExtendedProperties` 或新增
   wire 欄位標記語意）都保留「忘了標」的靜默失敗模式；改型別後**不可能忘記**——
   宣告欄位型別即決定語意。

---

## 2. 決策

### T1：`DbTypeConverter.ToType(FieldDbType.Date)` 回傳 `typeof(DateOnly)`

`DataSet` / `DataTable` 的日曆日欄位在 CLR 型別上與時間點欄位區分開。

> **實測（2026-07-25）**：`DataColumn.DataType = typeof(DateOnly)` 在 BCL 這側**完全可用**——
> 值以 `DateOnly` 原型存放、`DataView` 排序正常、`DataSet.WriteXml` 正常。
> 唯一擋路的是框架自己：Bee 的 MessagePack wire 擲
> `InvalidOperationException: DateOnly can't convert to FieldDbType`
> （`ToFieldDbType` 無 `DateOnly` case，其 `TypeCode` 為 `Object` 而落入 default）。
>
> 也就是說本變更的成本**不是**「BCL / provider 支援不足」，而是下述的 breaking change 處置。

### T2：當一般變更發布，但必須標記 breaking

- 框架內破壞半徑為 **0**——全 repo 無任何 `(DateTime)row[...]` 直接 cast，
  取值一律走 `ValueUtilities.CDateTime` / `CDate`（接 `object`）。
- 風險在**外部使用者**的 BO 程式碼：`(DateTime)row["birthday"]` 編譯期無感、
  runtime 才擲 `InvalidCastException`。
- 不等主版本，隨一般版次發布。但 commit 與 CHANGELOG **必須明確標記 breaking**，
  並附遷移指引（外部應改用 `ValueUtilities.CDateTime` / `CDate` 取值，不要直接 cast）。

### T3：SQL 取回的 `DataTable` 也必須產出 `DateOnly` 欄位 ★

**這是本 plan 的第二個主體，不是附帶項。** 沒有它，`DateOnly` 在報表 / AnyCode 路徑上
從頭到尾不會出現，本 plan §1 的第 2 點立論（schema-less 場景靠型別自我描述）即不成立。

**問題**：`DbAccess` 的兩條讀取路徑都由 ADO.NET provider 決定欄位型別，中間**沒有任何型別後處理**：

```csharp
adapter.Fill(table);            // DbAccess.cs:419（同步）
table.Load(reader);             // DbAccess.Async.cs:208（非同步）
table.LowercaseColumnNames();   // 只改欄名，不動型別
```

SQL Server 的 `date` 欄位經 SqlClient 回報為 `System.DateTime`——**`ToType` 根本沒被呼叫**。
整條鏈因此全程維持 `DateTime`：

| 環節 | 型別決定者 | 僅做 T1 時 |
|------|-----------|-----------|
| SQL → `DataTable` | ADO.NET provider | `DateTime` ❌ |
| → wire（`ToFieldDbType(col.DataType)`，`SerializableDataTable.cs:78`） | 來源欄位 CLR 型別 | 標成 `FieldDbType.DateTime` ❌ |
| → 用戶端重建（`ToType`，`SerializableDataTable.cs:152`） | wire 的 `FieldDbType` | 重建為 `DateTime` ❌ |

**做法：兩路徑，依 SQL 的來源決定誰負責型別。**

| 路徑 | SQL 來源 | 型別處理 | 依據 |
|------|---------|---------|------|
| **一** | 由定義產生（`DataFormRepository` 等 schema 驅動查詢） | **框架處理**：取回 `DataTable` 後，依 schema 把 `FieldDbType.Date` 欄位轉為 `DateOnly` | `FormTable.Fields` / `DbField` |
| **二** | 呼叫端直接下 SQL（AnyCode / 報表 / 批次） | **框架不處理**，由呼叫端顯式宣告 | 呼叫端 |

一句話規則：**框架產生的 SQL，框架負責型別；呼叫端自己寫的 SQL，呼叫端負責型別。**

**`DbAccess` 的讀取機制不需要改動。** 路徑一在 Repository 層後處理即可——`DataFormRepository`
本來就會拿著 schema 逐欄後處理取回的 `DataTable`（`ApplyMasterDefaults`，
`DataFormRepository.cs:378`，走 `formTable.Fields` 依欄名比對）。
「依 schema 把 `Date` 欄轉成 `DateOnly`」是這個既有模式的自然延伸，
**不需要把 `adapter.Fill` 換成手寫 reader 迴圈**。

> **為何不採「以 `DbDataReader.GetDataTypeName` 全域偵測」**（前一版方案，已否決）：
>
> 1. **provider 相依的局部解**。實測顯示 SQLite 的運算式欄位一律回 `TEXT`（見下表），
>    判不出來。而 SQLite 正是開發 / 測試環境——會造成**開發環境與正式環境行為不同**，
>    比「明確劃線、兩邊都可預期」糟得多。
> 2. **schema 比型別名更可靠**。用 schema 不依賴各家 provider 的型別名字串
>    （`date` / `DATE` / `Date` 大小寫不一），也沒有「Oracle 既有表以 `DATE` 存時間點」的誤判風險。
> 3. **成本更高**。為了插入型別判斷得放棄 `adapter.Fill` 改手寫 reader 迴圈，
>    這是**所有查詢**都要付的代價，包括完全沒有日期欄位的查詢。
>
> 附帶澄清：`GetDataTypeName` 本身**每欄呼叫一次、非每列**，開銷可忽略——
> 否決它的理由是可靠性與重構成本，不是這個呼叫的效能。

**路徑二的「自行處理」＝取回 `DataTable` 後自行轉換欄位型別。** 呼叫端在拿到結果、
交還給上層之前，把該視為日曆日的欄位轉成 `DateOnly`。

提供**兩種並行的宣告方式**，共用同一個轉換實作：

| 方式 | 形式 | 適用時機 |
|------|------|---------|
| **`DataTable` 擴充方法** | `table.SetDateOnlyColumns("order_date", "due_date")` | 事後轉換。表格來源不一（自行組裝、來自他處、需條件式處理）時使用。**這是基元** |
| **`DbCommandSpec` 選項** | `new DbCommandSpec(...) { DateOnlyColumns = [...] }` | 宣告貼著 SQL 寫，語意與查詢同處一地。`DbAccess` 建好 `DataTable` 後代為呼叫上述擴充方法 |

擴充方法歸入既有的 `Bee.Base.Data.DataTableExtensions`（與 `AddColumn` / `LowercaseColumnNames`
同處，符合 `<TypeName>Extensions` 慣例）。`DbCommandSpec` 選項是薄糖衣，不重複實作。

> `DbCommandSpec` 選項使 `DbAccess` 需要一處小幅追加（建表後套用），但**仍不需要**
> 把 `adapter.Fill` 換成手寫 reader 迴圈——被否決的是那個重構，不是這個追加。

**三個實作細節（採建議值，無需另行討論）**：

- **欄名比對用 `OrdinalIgnoreCase`**——`LowercaseColumnNames()` 已把欄名轉小寫，
  若要求呼叫端記得傳小寫是不必要的陷阱。（符合 code-style：識別碼型字串用 Ordinal 家族。）
- **指定了不存在的欄名應擲例外**，不可靜默略過——打錯字時「看起來宣告了、實際沒作用」
  正是本 plan 要消除的靜默失敗模式。
- **`DbCommandSpec.DateOnlyColumns` 僅對回傳 `DataTable` / `DataSet` 的 `DbCommandKind` 有效**，
  用於其他 kind 時應擲例外而非忽略。

因此對下游而言兩條路徑產出的 `DataTable` 是同構的——`DateOnly` 欄位就是 `DateOnly` 欄位，
不論它由框架或由 BO 轉換。Connector 與 wire 的行為不因路徑而異。

#### 實測：五家 provider 的 `GetDataTypeName`（2026-07-25，全部容器實跑）

查詢**運算式**（`CAST(... AS date)` / `DATE '...'`）時：

| Provider | 純日期欄 | 時間點欄 | `GetFieldType` |
|----------|---------|---------|---------------|
| SQL Server | `date` | `datetime2` | `DateTime` |
| PostgreSQL | `date` | `timestamp without time zone` | `DateTime` |
| MySQL | `DATE` | `DATETIME` | `DateTime` |
| Oracle | `Date` | `TimeStamp` | `DateTime` |
| SQLite | **`TEXT`** ❌ | **`TEXT`** ❌ | **`String`** |

查詢**真實資料表欄位**時，SQLite 改為回傳宣告型別：

| Provider | 純日期欄 | 時間點欄 | `GetFieldType` |
|----------|---------|---------|---------------|
| SQLite | `DATE` ✅ | `DATETIME` ✅ | **`String`** |

此實測即是否決全域偵測方案的依據：**SQLite 的運算式欄位一律回 `TEXT`**，而報表 SQL
大量使用運算式，所以該方案在 SQLite 上對報表無效——形成 provider 相依的行為分歧。
四家關聯式 DB 雖可行（Oracle 亦然：`OracleTypeMapping.cs:70` 刻意以 `DATE` vs `TIMESTAMP(6)`
區分，ODP.NET 如實回報），但「只有四家可行」正是問題所在。

> **附帶發現（現況，非本 plan 造成）**：SQLite 的 `GetFieldType` 對日期欄位一律回 `String`。
> 即目前 `ExecuteDataTable` 在 SQLite 上取回的日期欄位，`DataColumn.DataType` 是 `string`
> 而非 `DateTime`，wire 上因而標成 `FieldDbType.String`。此現況同時影響
> [plan-datetime-timezone.md](plan-datetime-timezone.md) 的 D4（字串欄位不會被時區轉換）。

### T4：`TimeOnly` 暫不引入

`FieldDbType` 目前沒有對應的「純時刻」型別，沒有要接的槽位。
待真有需求時再一併新增 `FieldDbType.Time` 與 `TimeOnly` 對映。

---

## 3. 實作範圍

### 3.1 型別對映（T1）

| 位置 | 變更 |
|------|------|
| `DbTypeConverter.ToType` | `FieldDbType.Date` → `typeof(DateOnly)` |
| `DbTypeConverter.ToFieldDbType` | 加 `DateOnly` case（`TypeCode` 為 `Object`，現況落入 default 擲例外） |
| `ValueUtilities.CDateTime` / `CDate` | 加 `DateOnly` 處理 |
| `FieldDbTypeExtensions` | 預設值 / 值轉換路徑對 `Date` 的處理改回傳 `DateOnly` |
| `ILMapper.cs:162` | reader → **實體物件 `T`** 的對映加 `DateOnly`（各 provider 對 `GetFieldValue<DateOnly>` 支援不一，必要時走 `GetDateTime` 再轉）。**注意此路徑與 `DataTable` 無關**，補它不能取代 T3 |
| UI 控件（`DateEdit` 等） | 繫結型別 |

### 3.2 SQL 讀取路徑（T3）

**`ExecuteDataTableCore` 的讀取機制不變**——維持 `adapter.Fill` / `table.Load`，
僅在建表完成後追加「套用 `DbCommandSpec.DateOnlyColumns`」一步（緊接 `LowercaseColumnNames()`）。

| 位置 | 變更 |
|------|------|
| `DataFormRepository` 等 schema 驅動查詢 | 取回 `DataTable` 後，依 `FormTable.Fields` / `DbField` 把 `FieldDbType.Date` 欄位轉為 `DateOnly`（比照既有 `ApplyMasterDefaults` 的後處理模式） |
| `DataTableExtensions.SetDateOnlyColumns`（新增） | 「將指定欄位由 `DateTime` 欄改建為 `DateOnly` 欄並搬移值」的單一實作，公開 API。路徑一由框架呼叫、路徑二由 BO 呼叫 |
| `DbCommandSpec.DateOnlyColumns`（新增屬性） | 宣告式選項；`DbAccess` 於建表後代呼叫上述擴充方法。薄糖衣，不重複實作 |

### 驗證

- 跨五家 provider（SQL Server / PostgreSQL / MySQL / Oracle / SQLite）驗證：
  - `DateOnly` **參數寫入**與讀取——各家 ADO.NET 對 `DateOnly` 參數的支援尚未實測，仍需確認
  - **`date` 欄位經 SQL 查詢回來後，`DataColumn.DataType` 為 `DateOnly`**（T3 的核心驗證）
  - ~~`GetDataTypeName` 對各家純日期型別的實際回傳值~~ ✅ 已完成（見 T3 實測表）
- 三棲序列化 round-trip：XML / JSON / MessagePack 對 `DateOnly` 欄位的 `DataTable`。
- wire 自我描述驗證：`Date` 欄位經 round-trip 後，`SerializableDataColumn.DataType`
  應為 `FieldDbType.Date` 而非 `DateTime`。
- **回歸**：同步 / 非同步兩條路徑改寫後，既有 `DataTable` 行為（欄名小寫、RowState、
  空結果集、null 值）不變。

---

## 4. 風險

- **外部消費端 `(DateTime)row[...]` 於 runtime 擲例外**——編譯期無感，是此變更唯一的外溢風險。
  遷移指引與 breaking 標記是主要緩解手段。
- **provider 對 `DateOnly` 參數的支援落差**——Oracle ODP.NET 為主要不確定因素；
  若不支援需在 `DbCommandSpec` 參數層做例外對映（轉 `DateTime` 再送）。
- **路徑二漏轉的欄位會被當成時間點**——BO 自寫 SQL 後未轉換的日曆日欄位維持 `DateTime`，
  在時區 plan 的 D4 下預設當 Instant 轉換，可能跨日偏移。這是兩路徑方案的主要代價：
  路徑二的正確性取決於 BO 作者。緩解是共用 helper 夠簡單（一行）＋文件明確說明此責任分界。
- **欄位改建的 O(rows) 成本**——`DataColumn.DataType` 在有資料後不可變更，轉換須新建欄位、
  搬值、移除舊欄並復位順序。只在 schema 驅動查詢且確有 `Date` 欄位時發生，但大結果集需留意。
- **`DataColumn` 的 `ObjectStorage` 路徑**——`DateOnly` 非 `DataColumn` 的原生儲存型別之一，
  實測排序與 `WriteXml` 正常，但大量資料下的效能特性需一併觀察。

---

## 5. 出貨約束：兩階段必須一起發布

**只完成階段 1 會讓同一個欄位出現兩種 CLR 型別，取決於 `DataSet` 從哪裡來。**

`AddColumn(field.FieldName, field.DbType)` 走 `DbTypeConverter.ToType`，因此階段 1 一落地，
**用戶端依 schema 自建的空白 `DataSet`** 立刻產出 `DateOnly` 欄位——例如
`FormDataObject.Events.cs:158`（Avalonia）、`Bee.Web.Blazor.Server` / `Bee.Web.Blazor.Wasm`
的 `FormDataObject.cs:307`。

但此時伺服端由 SQL 取回的資料仍是 `DateTime`（階段 2 尚未做），經 wire 標為
`FieldDbType.DateTime`、用戶端重建亦為 `DateTime`。於是同一個 ProgId 的同一個欄位：

| `DataSet` 來源 | 階段 1 完成時的欄位型別 |
|---------------|---------------------|
| 用戶端新增空白單據（依 schema 自建） | `DateOnly` |
| 用戶端載入既有資料（來自伺服端 SQL） | `DateTime` |

任何同時處理這兩條路徑的程式碼（`FormDataObject`、欄位編輯繫結、`UpdateDataTables` 差異比對）
都會踩到型別不一致。

**因此**：

- 兩階段可分別開發、分別 commit，但**必須在同一個版次一起發布**。
- 若中途需要發版，階段 1 不可單獨併入 `main` 的可發布狀態——或於階段 1 完成後立即接續階段 2。
- 驗收條件應涵蓋「新增空白單據 → 存檔 → 重新載入」的完整往返，這條路徑同時經過兩種來源，
  是型別不一致最容易顯現的地方。
