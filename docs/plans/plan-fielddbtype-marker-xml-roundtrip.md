# 計畫：`DataSet` XML 往返後 `FieldDbType` 標記讀回不被採用

**狀態：✅ 已完成（2026-09-11）**

## 問題

實測（NuGet `Bee.Base` 4.32.0，2026-09-11）：

1. 以 `DataTableExtensions.AddColumn` 建 `Date` / `DateTime` / `Time` 三欄。
2. `DataSet.WriteXml(XmlWriteMode.WriteSchema)` **確實寫出** `msprop:Bee.FieldDbType="Date"` 等註記。
3. `DataSet.ReadXml(XmlReadMode.ReadSchema)` 讀回後，`DataColumn.ExtendedProperties["Bee.FieldDbType"]`
   的值是 **`string`**，不是 `FieldDbType`。
4. [`DataColumnExtensions.GetDeclaredFieldDbType`](../../src/Bee.Base/Data/DataColumnExtensions.cs)
   做的是 `as FieldDbType?`，字串轉不過去 → 回 `null` → `ResolveFieldDbType` 退回
   `DbTypeConverter.ToFieldDbType(column.DataType)` 反推：
   - `Date` 欄 → `DateTime`
   - `Time` 欄 → `String`（CLR 型別本來就是 `string`，只是標記丟了）
   - `DateTime` 欄 → `DateTime`（反推值恰好相同，看不出症狀）

JSON（`DataTableJsonConverter`）與 MessagePack（`SerializableDataTable`）兩條路徑**不受影響**：
兩者重建欄位時都以列舉值呼叫 `ApplyFieldDbType`，標記完整還原。

## 與現行文件的落差

公開文件 [`docs/temporal-types.md`](../temporal-types.md) §6「XML — `DataSet` persistence」寫
「so it survives a write/read round trip」，[`docs/temporal-types.zh-TW.md`](../temporal-types.zh-TW.md)
同段寫「因此能在寫入／讀回的往返中存活」。**寫得出來是真的，讀回來被採用不是。**

[ADR-031](../adr/adr-031-calendar-day-column-semantics.md)「後果」段已把 `ExtendedProperties`
在複製路徑上的保留行為列為「需持續留意」，本問題就是其中一種漏失，症狀也跟 ADR 預告的一樣
（靜默退回反推 CLR 型別）。

## 影響範圍（2026-09-11 查證）

**框架內部：目前沒有路徑受影響。**

- 框架內唯一的 `DataSet` XML 讀回點是稽核日誌
  [`ChangeDiffGramReader.ReadSchemaBound`](../../src/Bee.Business/AuditLog/ChangeDiffGramReader.cs)
  （`ReadXmlSchema` + `ReadXml(DiffGram)`），它以 `ToText` 把值轉成文字，不呼叫
  `ResolveFieldDbType` / `GetDeclaredFieldDbType`。
- `GetDeclaredFieldDbType` 的另一個直接呼叫端 `DataFormRepository.Skeleton`（判斷 `Guid` 標記）
  只處理從資料庫取回的表格，不經 XML。

**外部消費端：以 `ReadXml` 讀回持久化的 `DataSet` 後，再交給依賴 `ResolveFieldDbType` 的元件。**

| 下游 | 讀回後的行為 |
|------|-------------|
| `DateTimeZoneConverter.InstantColumns` | `Date` 欄被當成時間點，**做了 UTC ↔ 使用者時區平移**（文件承諾日曆日「絕不」轉時區） |
| `SerializableDataTable`（MessagePack） | wire 上 `Date` 標成 `DateTime`、`Time` 標成 `String` |
| `DataTableJsonConverter`（JSON） | 同上，`type` 欄位寫出反推值 |

`DbTypeConverter.ToType(FieldDbType.Time)` 回 `typeof(string)`，所以標記還原與否都不會改變
重建後的 CLR 型別，差別只在語意標記本身。

## 方向（2026-09-11 決定採 B）

### A. 修文件（零行為變更）

- 改寫雙語 §6 那句：XML 往返**寫得出**標記，但讀回時是字串、不被採用，會退回反推 CLR 型別；
  需要標記完整還原的場合走 JSON / MessagePack，或讀回後依 schema 重新呼叫
  `FormTableExtensions.ApplyFieldDbTypes`。
- 跑 `./check-public-docs.sh`。

### B. 修實作（行為變更）

- `GetDeclaredFieldDbType` 同時接受 `FieldDbType` 與 `string`；字串以
  `Enum.TryParse<FieldDbType>(text, ignoreCase: false, out var parsed)` 解析，
  **並加 `Enum.IsDefined` 檢查**——`TryParse` 會放行 `"99"` 這類數字字串。解析失敗回 `null`，
  維持「沒有標記就反推」的既有退路。
- 只在 getter 內解析，**不回寫** `ExtendedProperties`（getter 不該有副作用）。
- 雙語 §6 那句維持原意，補一句說明讀回時由框架解析字串形式。
- 測試：
  - `tests/Bee.Base.UnitTests/Data/DataColumnExtensionsTests.cs`：`Date` / `DateTime` / `Time`
    三欄 `WriteXml(WriteSchema)` → `ReadXml(ReadSchema)` 後 `ResolveFieldDbType` 還原；
    非法字串與數字字串回 `null` 並退回反推。
  - `tests/Bee.Api.Core.UnitTests`：XML 讀回的 `Date` 欄經 `DateTimeZoneConverter` 不被平移。
- 發版時 CHANGELOG 列為行為修正（外部消費端讀回的 `Date` 欄從「會被平移」變成「不平移」）；
  目前 CHANGELOG 沒有 Unreleased 段，由發版流程整理。

### 取捨的外部約束

2026 iThome 鐵人賽 **Day 27**（`day-27-temporal-semantics.md` 第 69 行附近）與 **Day 30**
（`day-30-depths-and-map.md` 第 142 行的表格）以「`DataSet` 自己的 XML 往返帶不回標記」為事實。
兩篇**尚未發佈**，使用者確認可以修改文章。

- 選 **A**：與文章一致，無需動文章。
- 選 **B**：文章描述的是修正前的行為，發佈前需改稿。改稿不在本 plan 範圍，交由文章 session 處理。

## 相關發現（使用者 2026-09-11 決定本次不動，是否併入本 plan 待定）

雙語文件 §2 對照表寫 `Time`「光看 CLR 型別能分辨」、語意靠「CLR 型別本身」保留，下方並寫
「`Time` 不需要標記 —— `string` 欄位本身已無歧義」。但 `Time` 與 `String` / `Text` 共用
`string`，`DbTypeConverter.ToFieldDbType(typeof(string))` 回 `String`；
`TimeOfDayValueTests.AddColumn_Time_IsStringColumnCarryingTheMarker` 的註解也寫明沒有標記就會以
`String` 讀回。實際上 `Time` 與 `Date` 一樣靠標記保留語意。
[ADR-033](../adr/adr-033-time-of-day-semantics.md) 第 129 行附近有類似說法。
