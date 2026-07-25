# ADR-031：日曆日欄位語意以顯式標記承載，不改 CLR 型別

## 狀態

已採納（2026-07-25）

> 三階段已全部實作完成（commit `fddb38f6` / `c7782308` / `c5578a42`）。
> 執行細節與實作期偏離見 `docs/plans/plan-date-semantics.md`；
> 消費端使用方式見 `docs/date-semantics.md`。

## 背景

`FieldDbType` 刻意區分 `Date` 與 `DateTime`，正是因為 .NET 只有單一 `DateTime` 型別、
無法表達「日曆日 vs 時間點」的差別——定義層早就把這件事講清楚了。

但這個區別在 CLR 層被抹平：

```csharp
case FieldDbType.Date:
case FieldDbType.DateTime:
    return typeof(DateTime);        // DbTypeConverter.ToType
```

連帶造成 wire payload 也失去這個資訊。`SerializableDataColumn.DataType`（MessagePack）與
JSON 的 `"type"` 欄位逐欄攜帶 `FieldDbType`，但兩者的來源都是
`DbTypeConverter.ToFieldDbType(col.DataType)`——**以 CLR 型別為 key**。
而日曆日欄位的 CLR 型別是 `DateTime`，因此**日曆日欄位在 wire 上永遠被標成
`FieldDbType.DateTime`**。

被抹平的不只 `Date`。同樣共用一個 CLR 型別而無法還原的還有
`Text`/`String`（皆 `string`）、`Currency`/`Decimal`（皆 `decimal`）、
`AutoIncrement`/`Integer`（皆 `int`）。

值得注意的是 **DB 參數層並未抹平**：`FieldDbType.Date` → `DbType.Date`，
資料庫端始終知道這是日曆日。被收斂掉的只有 CLR 表示法與 wire 標記。

### 為何需要處理

1. **schema-less 場景是真正的破口。** Repository 雙軌策略下，報表 / 批次（AnyCode）產出的
   `DataTable` 背後沒有 `FormSchema`，消費端**查不到欄位語意**。
   純 JS client 亦然：payload 自我描述後，前端不需另外取 schema。
2. **跨時區部署需要一個安全預設。** 沒有欄位語意，schema-less 路徑上的日期欄位
   會被當成時間點做時區轉換，造成跨日偏移（見 `docs/plans/plan-datetime-timezone.md` 的 D4）。
3. **wire 上早就有這個槽位，只是填錯了。** 只要改變它的**來源**——從 CLR 型別反推改為
   優先讀顯式標記——就把 payload 的自我描述能力接回來，**不新增任何 wire 欄位**。

## 考慮過的選項

### 1. 讓 CLR 型別自己承載語意：`ToType(Date)` 回傳 `typeof(DateOnly)`（否決）

方向最乾淨——「宣告欄位型別即決定語意」，從根本消除「忘了標」的失敗模式。
初步評估認為框架內破壞半徑為 0（全 repo 無 `(DateTime)row[...]` 直接 cast）。

**實測後否決**（2026-07-25，`net10.0`）。`DateOnly` 不是 `DataColumn` 的原生儲存型別，
走 `ObjectStorage` **嚴格型別比對，完全不做轉換**：

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

三個否決理由：

1. **打斷框架自己的繫結層。** UI 繫結層是字串進出（`DateEdit` 寫回
   `date.ToString("yyyy-MM-dd")`）。它現在能運作**純粹是靠 `DataColumn` 對 `DateTime` 欄
   自動 parse 字串**。欄位一變 `DateOnly`，每次選日期都擲 `ArgumentException`。
   初評「破壞半徑為 0」只盤點了讀取方向，**寫入方向才是破口**。
2. **永久失去 `RowFilter` / `Compute`。** BCL 的 `DataTable` 運算式引擎不認識 `DateOnly`，
   無繞法。對 ERP 而言 `order_date` 不能下 `RowFilter` 是實質功能倒退。
3. **反方向亦擋**，導致分階段落地期間沒有可運行的中間態，必須綁同版次一起發布。

### 2. `ExtendedProperties` 顯式標記（採納）

`DataColumn.DataType` 完全不變，日曆日語意改由 `DataColumn.ExtendedProperties` 承載。
上述三項代價全部不存在。代價是保留「忘了標」的靜默失敗模式（見〈後果〉）。

### 3. wire 新增獨立語意欄位（否決）

在 `SerializableDataColumn` 加 `IsDateOnly` / `Semantics` 之類欄位與 `DataType` 並存。
語意更顯式，但 wire 變胖，且與現有 `DataType` 欄位語意重疊——後者本來就是 `FieldDbType`，
只是填入的值不準確。修正來源比新增欄位更小。

## 決策

**日曆日語意以 `DataColumn.ExtendedProperties` 顯式標記承載，`DataColumn.DataType` 維持不變。**

| 層 | 決定 |
|---|---|
| 儲存 | `DataColumn.DataType` 維持 `DateTime`；繫結層 / `RowFilter` / `Sort` / `Compute` / 既有 cast 零影響 |
| 語意 | `DataColumn.ExtendedProperties` 記錄宣告的 `FieldDbType`，經 `DataColumnExtensions` 的 `ApplyFieldDbType` / `ResolveFieldDbType` 存取 |
| wire | **不新增欄位**——MessagePack 的 `SerializableDataColumn.DataType` 與 JSON 的 `"type"` 本來就是 `FieldDbType`，只是填入的值變準確 |
| 取值 | `ValueUtilities.CDate` 回傳 `DateOnly`，讓日曆日在使用端可與時間點區分 |

### 標記來源的責任分界（兩路徑）

一句話規則：**框架產生的 SQL，框架負責標記；呼叫端自己寫的 SQL，呼叫端負責標記。**

| 路徑 | SQL 來源 | 標記處理 |
|------|---------|---------|
| **一** | 由定義產生（`DataFormRepository` 等 schema 驅動查詢） | **框架處理**：取回 `DataTable` 後依 `FormTable.Fields` 重播欄位型別 |
| **二** | 呼叫端直接下 SQL（AnyCode / 報表 / 批次） | **呼叫端顯式宣告**：`table.SetDateColumns(...)` 或 `DbCommandSpec.DateColumns` |

**曾評估並否決「以 `DbDataReader.GetDataTypeName` 全域偵測」**：實測顯示 SQLite 的運算式欄位
一律回 `TEXT`（判不出來），而 SQLite 正是開發 / 測試環境，會造成**開發環境與正式環境行為不同**；
且為插入型別判斷須放棄 `adapter.Fill` 改手寫 reader 迴圈，是**所有查詢**都要付的代價。

### 語意決策只有一份實作

wire 序列化有 MessagePack 與 JSON 兩份平行實作，且分居不同套件
（`Bee.Api.Core` 與 `Bee.Base`）。「優先讀標記、未標記才反推」的邏輯抽成
`Bee.Base.Data.DataColumnExtensions` 的單一 helper，兩份 converter 共用。
放 `Bee.Base` 是**必要條件而非偏好**——它是兩者唯一的共同下層。

## 後果

- **正向**：
  - schema-less payload 自我描述，報表 / AnyCode / JS client 不需另取 schema 即可判別日曆日欄位。
  - 跨時區設計（`plan-datetime-timezone.md` 的 D4）取得可靠的「不該轉換」判斷依據。
  - 順帶修好 `Text`/`String`、`Currency`/`Decimal`、`AutoIncrement`/`Integer` 三組同樣被抹平的標記。
    因 `ToType` 對這些值的結果不變，用戶端重建的 CLR 型別完全不受影響。
  - 既有 client 不受影響：payload 結構與大小不變，只有 `FieldDbType` 的值變準確。
- **保留的靜默失敗模式（本決策相對選項 1 的唯一代價）**：路徑二（BO 自寫 SQL）
  忘了宣告的日曆日欄位，在 wire 上仍標成 `DateTime`。
  緩解為 helper 只需一行、錯誤欄名與錯誤 `DbCommandKind` 皆擲例外（不靜默略過）、
  以及文件明確載明此責任分界。
- **破壞性變更**：`ValueUtilities.CDate` 回傳型別由 `DateTime` 改為 `DateOnly`
  （`defaultValue` 參數同步）。外部呼叫端 `DateTime d = ValueUtilities.CDate(x)` 成為
  **編譯錯誤**而非 runtime 失敗；遷移方式為改接 `DateOnly` 或改用 `CDateTime`。
  框架內呼叫端僅一處。
- **需持續留意**：`ExtendedProperties` 在 `DataTable.Merge()` / `DataView.ToTable()` 等
  複製路徑的保留行為。漏失處會靜默退回「反推 CLR 型別」，症狀是 wire 上標記變回 `DateTime`。

## 相關

- ADR-026（數值語意與捨入）——同屬「定義層語意需貫通到資料層」的家族。
- ADR-029（欄位名稱一律小寫）——同樣是「wire 表示法對齊定義層」的決策。
- ADR-030（MessagePack name-based keys）——wire 表示法的另一項決策。
- `docs/date-semantics.md`——消費端（含 JS/TS）的使用指引。
- `docs/plans/plan-date-semantics.md`——本 ADR 的執行計畫與完整實測數據。
- `docs/plans/plan-datetime-timezone.md`——後續的跨時區設計，以本 ADR 為前置。
