# Plan：日曆日語意的顯式標記（`FieldDbType.Date` 貫通 wire）

**狀態：📝 擬定中（2026-07-25）**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 1 | 標記基礎建設：`DataColumn.ExtendedProperties` 標記讀寫 + `SerializableDataTable` 兩向承接 + `AddColumn` 自動標記 | 📝 待做 |
| 2 | SQL 讀取路徑（T3）：路徑一 Repository 依 schema 標記；路徑二 `SetDateColumns` + `DbCommandSpec.DateColumns` | 📝 待做 |
| 3 | 取值層：`ValueUtilities.CDate` 回傳型別改 `DateOnly`（breaking，見 T5） | 📝 待做 |

> 三階段**可分別開發、分別發布**，無互鎖出貨約束（此為改採標記方案後最大的成本改善之一）。

> 目標：讓 `FieldDbType.Date` 的「日曆日 vs 時間點」語意從定義層一路貫通到 `DataSet` 儲存格與
> wire payload，使 schema-less 消費端（報表 / AnyCode / JS client）不必另取 schema 也能判別。
> **由定義產生的 SQL 由框架負責標記；呼叫端自寫的 SQL 由呼叫端宣告**（T3 兩路徑）。
>
> 本 plan 與 [plan-datetime-timezone.md](plan-datetime-timezone.md) 是**兩個獨立議題**，
> 依賴為單向：時區 plan 依賴本 plan，本 plan **不依賴時區設計定案**，可獨立動工與發布。

> **本 plan 於 2026-07-25 換過方案。** 原案（`DbTypeConverter.ToType(Date)` 回傳
> `typeof(DateOnly)`，讓 CLR 型別自己承載語意）在實測後否決，改為顯式標記。
> 否決依據與實測數據見 §6，保留以免日後重複評估。

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

1. **wire 上早就有這個槽位，只是填錯了。** `SerializableDataColumn.DataType` 本身就是
   `FieldDbType`，只要改變它的**來源**（從 CLR 型別反推 → 優先讀顯式標記），
   就把 payload 的自我描述能力接回來——**不新增任何 wire 欄位**。
2. **schema-less 場景是真正的破口。** Repository 雙軌策略下，報表 / 批次（AnyCode）產出的
   `DataTable` 背後沒有 `FormSchema`，消費端**查不到欄位語意**。
   JS client 亦然：payload 自我描述後，JS 端不需另外取 schema。
3. **時區 plan 的 D4 需要一個安全預設。** 沒有欄位語意，schema-less 路徑上的日期欄位
   會被當成 Instant 做時區轉換，造成跨日偏移。

---

## 2. 決策

### T1：語意載體為 `DataColumn.ExtendedProperties`，**不改 CLR 型別**

`DbTypeConverter.ToType(FieldDbType.Date)` **維持回傳 `typeof(DateTime)`**。
`DataColumn.DataType` 不變，因此繫結層、`DataView.RowFilter` / `Sort`、`DataTable.Compute`、
`Convert.ChangeType`、既有 `(DateTime)row[...]` 全部**零影響**。

日曆日語意改由 `DataColumn.ExtendedProperties` 顯式攜帶：

```csharp
// 建議：以擴充方法封裝，避免 magic string 散落
column.SetFieldDbType(FieldDbType.Date);
var dbType = column.GetFieldDbType();      // 未標記時回傳 null
```

> 為何不讓 CLR 型別自己承載語意（原案，已否決）見 §6。一句話：`DateOnly` 在
> `DataColumn` 上走 `ObjectStorage` 嚴格型別比對，會打斷框架自己的字串寫回繫結層，
> 且永久失去 `RowFilter` / `Compute`。

### T2：`SerializableDataTable` 兩個方向都承接標記

| 方向 | 位置 | 變更 |
|------|------|------|
| 序列化 | `SerializableDataTable.cs:78` | `DataType` 來源改為「**優先讀 `ExtendedProperties` 標記，未標記才 `ToFieldDbType(col.DataType)` 反推**」 |
| 反序列化 | `SerializableDataTable.cs:152` | 依 `ToType(dataType)` 建欄後，**把 wire 的 `FieldDbType` 寫回 `ExtendedProperties`** |

`ExtendedProperties` 本身不上 wire（它是本地的），語意由既有的 `SerializableDataColumn.DataType`
欄位承載；反序列化時再落回用戶端的 `ExtendedProperties`。兩端因此對稱：
**伺服端標記什麼，用戶端就拿到什麼。**

### T3：SQL 取回的 `DataTable` 也必須帶標記 ★

**這是本 plan 的第二個主體，不是附帶項。** 沒有它，`FieldDbType.Date` 在報表 / AnyCode 路徑上
從頭到尾不會出現，§1 的第 2 點立論即不成立。

**問題**：`DbAccess` 的兩條讀取路徑都由 ADO.NET provider 決定欄位型別，中間**沒有任何後處理**：

```csharp
adapter.Fill(table);            // DbAccess.cs:419（同步）
table.Load(reader);             // DbAccess.Async.cs:208（非同步）
table.LowercaseColumnNames();   // 只改欄名，不動 metadata
```

SQL Server 的 `date` 欄位經 SqlClient 回報為 `System.DateTime`——**`ToType` 根本沒被呼叫**。
整條鏈因此全程被視為時間點：

| 環節 | 語意決定者 | 未做 T3 時 |
|------|-----------|-----------|
| SQL → `DataTable` | ADO.NET provider | 無標記 ❌ |
| → wire（`ToFieldDbType(col.DataType)`，`SerializableDataTable.cs:78`） | 無標記時回退至 CLR 型別反推 | 標成 `FieldDbType.DateTime` ❌ |
| → 用戶端重建 | wire 的 `FieldDbType` | 標成 `DateTime` ❌ |

**做法：兩路徑，依 SQL 的來源決定誰負責標記。**

| 路徑 | SQL 來源 | 標記處理 | 依據 |
|------|---------|---------|------|
| **一** | 由定義產生（`DataFormRepository` 等 schema 驅動查詢） | **框架處理**：取回 `DataTable` 後，依 schema 對 `FieldDbType.Date` 欄位寫入標記 | `FormTable.Fields` / `DbField` |
| **二** | 呼叫端直接下 SQL（AnyCode / 報表 / 批次） | **框架不處理**，由呼叫端顯式宣告 | 呼叫端 |

一句話規則：**框架產生的 SQL，框架負責標記；呼叫端自己寫的 SQL，呼叫端負責標記。**

**`DbAccess` 的讀取機制不需要改動。** 路徑一在 Repository 層後處理即可——`DataFormRepository`
本來就會拿著 schema 逐欄後處理取回的 `DataTable`（`ApplyMasterDefaults`，
`DataFormRepository.cs:378`，走 `formTable.Fields` 依欄名比對）。
「依 schema 標記 `Date` 欄」是這個既有模式的自然延伸。

> **為何不採「以 `DbDataReader.GetDataTypeName` 全域偵測」**（已否決）：
>
> 1. **provider 相依的局部解**。實測顯示 SQLite 的運算式欄位一律回 `TEXT`（見 §6 實測表），
>    判不出來。而 SQLite 正是開發 / 測試環境——會造成**開發環境與正式環境行為不同**，
>    比「明確劃線、兩邊都可預期」糟得多。
> 2. **schema 比型別名更可靠**。用 schema 不依賴各家 provider 的型別名字串
>    （`date` / `DATE` / `Date` 大小寫不一），也沒有「Oracle 既有表以 `DATE` 存時間點」的誤判風險。
> 3. **成本更高**。為了插入型別判斷得放棄 `adapter.Fill` 改手寫 reader 迴圈，
>    這是**所有查詢**都要付的代價，包括完全沒有日期欄位的查詢。
>
> 附帶澄清：`GetDataTypeName` 本身**每欄呼叫一次、非每列**，開銷可忽略——
> 否決它的理由是可靠性與重構成本，不是這個呼叫的效能。

**路徑二的「自行處理」＝取回 `DataTable` 後自行標記欄位。** 提供**兩種並行的宣告方式**，
共用同一個標記實作：

| 方式 | 形式 | 適用時機 |
|------|------|---------|
| **`DataTable` 擴充方法** | `table.SetDateColumns("order_date", "due_date")` | 事後標記。表格來源不一（自行組裝、來自他處、需條件式處理）時使用。**這是基元** |
| **`DbCommandSpec` 選項** | `new DbCommandSpec(...) { DateColumns = [...] }` | 宣告貼著 SQL 寫，語意與查詢同處一地。`DbAccess` 建好 `DataTable` 後代為呼叫上述擴充方法 |

擴充方法歸入既有的 `Bee.Base.Data.DataTableExtensions`（與 `AddColumn` / `LowercaseColumnNames`
同處，符合 `<TypeName>Extensions` 慣例）。`DbCommandSpec` 選項是薄糖衣，不重複實作。

> `DbCommandSpec` 選項使 `DbAccess` 需要一處小幅追加（建表後套用），但**仍不需要**
> 把 `adapter.Fill` 換成手寫 reader 迴圈——被否決的是那個重構，不是這個追加。

**三個實作細節（採建議值，無需另行討論）**：

- **欄名比對用 `OrdinalIgnoreCase`**——`LowercaseColumnNames()` 已把欄名轉小寫，
  若要求呼叫端記得傳小寫是不必要的陷阱。（符合 code-style：識別碼型字串用 Ordinal 家族。）
- **指定了不存在的欄名應擲例外**，不可靜默略過——打錯字時「看起來宣告了、實際沒作用」
  正是本 plan 要消除的靜默失敗模式。
- **`DbCommandSpec.DateColumns` 僅對回傳 `DataTable` / `DataSet` 的 `DbCommandKind` 有效**，
  用於其他 kind 時應擲例外而非忽略。

因此對下游而言兩條路徑產出的 `DataTable` 是同構的——標記就是標記，不論它由框架或由 BO 寫入。
Connector 與 wire 的行為不因路徑而異。

### T4：`AddColumn` 順手寫入標記

`DataTableExtensions.AddColumn(name, dbType)`（`DataTableExtensions.cs:62` 一帶）**已經拿著
`FieldDbType`**，建欄時順手寫標記即可。

這使**用戶端依 schema 自建的空白 `DataSet`**（`FormDataObject.Events.cs:158` 等）
與伺服端 SQL 取回的資料**天生同構**，不需額外接線。原案在此處有「同一欄兩種 CLR 型別」的
出貨互鎖問題，標記方案不存在該問題。

### T5：`ValueUtilities.CDate` 回傳型別改 `DateOnly`（breaking）

取值層提供 `DateOnly` 出口，讓消費端要日曆日時能直接拿到正確型別：

```csharp
public static DateOnly CDate(object value, DateOnly defaultValue = default)
```

- **`CDateTime` 不動**（仍回 `DateTime`）；`CDate` 由「`CDateTime(...).Date`」改為回傳 `DateOnly`。
- **框架內連鎖僅一處**：`FieldDbTypeExtensions.ToFieldValue(FieldDbType.Date, ...)`
  （`FieldDbTypeExtensions.cs:57`）目前回 `CDate(value)`。因 `DataColumn` 的 `DateTime` 欄
  **不接受 `DateOnly` 值**（`DateOnly` 未實作 `IConvertible`，實測見 §6），
  此處必須改為明確取 `DateTime` 的日期部分，不可跟著改成 `DateOnly`。
- **`CDate` 呼叫端全 repo 僅 3 處**（src 1 + tests 2），框架內破壞面接近 0。
- 風險在**外部使用者**：`DateTime d = ValueUtilities.CDate(x)` 變成編譯錯誤（source breaking），
  且為 binary breaking。不等主版本，隨一般版次發布，但 commit 與 CHANGELOG
  **必須明確標記 breaking** 並附遷移指引（改用 `CDateTime` 或接 `DateOnly`）。

> **為何 `CDate` 可以改、`DataColumn.DataType` 不可以**：前者是單一取值函式的簽章，
> 呼叫端接到編譯錯誤、當場改掉；後者會讓「字串 / `DateTime` 寫入日期欄」在 **runtime** 擲例外，
> 且打斷 `RowFilter` / `Compute`——失敗模式與破壞半徑差一個量級。

### T6：`TimeOnly` 暫不引入

`FieldDbType` 目前沒有對應的「純時刻」型別，沒有要接的槽位。
待真有需求時再一併新增 `FieldDbType.Time` 與 `TimeOnly` 對映。

---

## 3. 實作範圍

### 3.1 標記基礎建設（階段 1）

| 位置 | 變更 |
|------|------|
| `DataColumnExtensions`（新增或併入既有檔） | `SetFieldDbType(this DataColumn, FieldDbType)` / `GetFieldDbType(this DataColumn)`，內部用單一 `ExtendedProperties` key 常數，不散落 magic string |
| `SerializableDataTable.cs:78`（序列化） | `DataType` 改為優先讀標記，未標記才 `ToFieldDbType(col.DataType)` |
| `SerializableDataTable.cs:152`（反序列化） | 建欄後把 wire 的 `FieldDbType` 寫回 `ExtendedProperties` |
| `DataTableExtensions.AddColumn` | 建欄時順手寫入標記（T4） |
| `DbTypeConverter` | **不動**（`ToType(Date)` 維持 `typeof(DateTime)`） |

### 3.2 SQL 讀取路徑（階段 2）

**`ExecuteDataTableCore` 的讀取機制不變**——維持 `adapter.Fill` / `table.Load`，
僅在建表完成後追加「套用 `DbCommandSpec.DateColumns`」一步（緊接 `LowercaseColumnNames()`）。

| 位置 | 變更 |
|------|------|
| `DataFormRepository` 等 schema 驅動查詢 | 取回 `DataTable` 後，依 `FormTable.Fields` / `DbField` 對 `FieldDbType.Date` 欄寫入標記（比照既有 `ApplyMasterDefaults` 的後處理模式） |
| `DataTableExtensions.SetDateColumns`（新增） | 「將指定欄位標記為 `FieldDbType.Date`」的單一實作，公開 API。路徑一由框架呼叫、路徑二由 BO 呼叫 |
| `DbCommandSpec.DateColumns`（新增屬性） | 宣告式選項；`DbAccess` 於建表後代呼叫上述擴充方法。薄糖衣，不重複實作 |

### 3.3 取值層（階段 3）

| 位置 | 變更 |
|------|------|
| `ValueUtilities.CDate` | 回傳型別 `DateTime` → `DateOnly`（含 `defaultValue` 參數型別） |
| `FieldDbTypeExtensions.ToFieldValue`（`:57`） | `FieldDbType.Date` 分支改為明確取 `DateTime` 日期部分，**不可**回傳 `DateOnly`（`DataColumn` 的 `DateTime` 欄不接受） |
| `tests/Bee.Base.UnitTests/ValueUtilitiesTests.cs`（`:388`、`:396`） | 斷言型別改 `DateOnly` |

### 驗證

- **wire 自我描述**：`Date` 欄位經 round-trip 後，`SerializableDataColumn.DataType`
  應為 `FieldDbType.Date` 而非 `DateTime`；用戶端重建後的 `DataColumn` 應帶回標記。
- **兩路徑同構**：schema 驅動查詢與 `SetDateColumns` 標記過的自訂 SQL，產出的 wire payload
  對同一欄位應標成相同的 `FieldDbType`。
- **未標記時的回退**：無標記的 `DateTime` 欄仍應標成 `FieldDbType.DateTime`（行為不變）。
- 三棲序列化 round-trip：XML / JSON / MessagePack。
  （`ExtendedProperties` 不參與 XML 持久化，需確認 `DataSet.WriteXml` 路徑不受影響。）
- **回歸**：`DataColumn.DataType` 未變更，既有 `DataTable` 行為（欄名小寫、RowState、
  繫結層字串寫回、`RowFilter` / `Sort` / `Compute`）應完全不變——這是本方案相對原案的核心優勢，
  需有測試把它釘住。
- 跨五家 provider 驗證僅需確認「schema 驅動查詢取回後標記正確」，
  **不需驗證 `DateOnly` 參數寫入**（參數層維持既有 `DbType.Date`，從未被抹平）。

---

## 4. 風險

- **路徑二漏標的欄位會被當成時間點**——BO 自寫 SQL 後未標記的日曆日欄位在 wire 上仍是
  `FieldDbType.DateTime`，在時區 plan 的 D4 下預設當 Instant 轉換，可能跨日偏移。
  這是兩路徑方案的主要代價：路徑二的正確性取決於 BO 作者。
  緩解是共用 helper 夠簡單（一行）＋文件明確說明此責任分界。
  **註**：這也是標記方案相對原案唯一保留的靜默失敗模式；原案以「宣告型別即決定語意」消除它，
  但代價是 §6 所列的破壞面。
- **`CDate` 簽章 breaking 外溢至外部使用者**——source + binary breaking，
  但為編譯期錯誤（非 runtime），且呼叫端稀少。
- **`ExtendedProperties` 在 `DataTable` 複製 / 合併時的保留行為**——
  `DataTable.Copy()` / `Clone()` 保留 `ExtendedProperties`，但 `Merge()`、`DataView.ToTable()`、
  `Select()` 後自行組表等路徑需逐一確認，漏失處會靜默退回「反推 CLR 型別」。
  此為新方案需額外驗證的面向。

---

## 5. 與時區 plan 的分工

[plan-datetime-timezone.md](plan-datetime-timezone.md) 的 D4（Connector 雙向轉換）需要知道
「哪些欄位不該轉時區」。本 plan 完成後，該判斷的依據是 `SerializableDataColumn.DataType`
（wire 側）或 `DataColumn.ExtendedProperties`（本地側）標記為 `FieldDbType.Date`，
**而非 CLR 型別**。時區 plan 中所有「靠 `DateOnly` 型別自我描述」的敘述需同步改為「靠標記」。

---

## 6. 附錄：原案（CLR 型別承載語意）與否決依據

原案為 `DbTypeConverter.ToType(FieldDbType.Date)` 回傳 `typeof(DateOnly)`，
讓「宣告欄位型別即決定語意」，從根本消除「忘了標」的失敗模式。方向正確，但實測後成本不可接受。

### 6.1 `DataColumn` 對 `DateOnly` 的實測（2026-07-25，`net10.0`）

`DateOnly` 不是 `DataColumn` 的原生儲存型別，走 `ObjectStorage` **嚴格型別比對，完全不做轉換**：

| 操作 | `DateTime` 欄（現況） | `DateOnly` 欄 |
|------|---------------------|--------------|
| `row["d"] = "2026-07-25"`（字串） | ✅ 自動 parse | ❌ `ArgumentException` |
| `row["d"] = new DateTime(...)` | ✅ | ❌ `ArgumentException` |
| `row["dt"] = new DateOnly(...)` | — | ❌ `ArgumentException`（`DateOnly` 未實作 `IConvertible`） |
| `Convert.ChangeType(v, typeof(DateTime))` | ✅ | ❌ `InvalidCastException` |
| `DataView.RowFilter = "d >= #...#"` | ✅ | ❌ `EvaluateException` |
| `DataTable.Compute("MAX(d)")` | ✅ | ❌ `DataException` |
| `DataView.Sort` / `PrimaryKey.Find` / `Expression` 欄 | ✅ | ✅ |
| `DataColumn.DefaultValue` | `DateTime.MinValue` | `DBNull` |

### 6.2 三個否決理由

1. **打斷框架自己的繫結層。** Avalonia 繫結層是字串進出——
   `DateEdit.cs:166` 的 `_binder.WriteBack(date.DateTime.ToString("yyyy-MM-dd"))`，
   `GridControl.Cells` 同理。它現在能運作**純粹是靠 `DataColumn` 對 `DateTime` 欄自動 parse 字串**。
   欄位一變 `DateOnly`，每次選日期都擲 `ArgumentException`。
   （原案「框架內破壞半徑為 0」的結論只盤點了讀取方向，寫入方向才是破口。）
2. **永久失去 `RowFilter` / `Compute`。** BCL 的 `DataTable` 運算式引擎不認識 `DateOnly`，
   無繞法。對 ERP 而言 `order_date` 不能下 `RowFilter` 是實質功能倒退。
3. **反方向亦擋。** `DateOnly` 值塞不進 `DateTime` 欄，導致分階段落地期間沒有可運行的中間態，
   必須綁同版次一起發布。

相對地，標記方案的 `DataColumn.DataType` 完全不變，上述三項全部不存在。
代價是保留「路徑二忘了標」的靜默失敗模式（見 §4 第 1 條）。

### 6.3 附帶實測：五家 provider 的 `GetDataTypeName`（2026-07-25，全部容器實跑）

此實測原用於評估「全域偵測」方案，該方案已否決（見 T3），數據保留供日後參考。

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

> **附帶發現（現況，非本 plan 造成）**：SQLite 的 `GetFieldType` 對日期欄位一律回 `String`。
> 即目前 `ExecuteDataTable` 在 SQLite 上取回的日期欄位，`DataColumn.DataType` 是 `string`
> 而非 `DateTime`，wire 上因而標成 `FieldDbType.String`。此現況同時影響
> [plan-datetime-timezone.md](plan-datetime-timezone.md) 的 D4（字串欄位不會被時區轉換）。
> **本 plan 的標記方案可順帶蓋住此坑**——路徑一依 schema 標記，不看 provider 回報的型別。
