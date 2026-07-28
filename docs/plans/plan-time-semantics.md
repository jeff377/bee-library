# Plan：`FieldDbType.Time` 純時刻型別

**狀態：✅ 已完成（2026-07-27）**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 1 | 型別基礎與 DDL：`FieldDbType.Time` + `Bee.Base` 核心 + 五家 provider 對應與等價規則 + ADR | ✅ 已完成（2026-07-27） |
| 2 | 取值層與運算式：`ValueUtilities.CTimeOnly` + `ExpressionPolicy.CoerceValue` | ✅ 已完成（2026-07-27，併入階段 1） |
| 3 | UI 層：時刻編輯控件（Avalonia 先行，再移植 MAUI / Blazor） | ✅ 已完成（2026-07-27） |
| 4 | 公開文件：雙語 `time-semantics` + 術語表時間語意詞條 + CHANGELOG breaking 標記 | ✅ 已完成（2026-07-27） |

> **目標**：補上框架缺少的「時刻」表達力——班別起訖、營業時間、提醒時刻這類
> **一日之內、不繫於特定日期**的值，目前只能以 `String` 或 `DateTime` 勉強承載。
>
> **承載方案**：**DB 與 `DataSet` 存 5 碼字串 `"HH:mm"`，程式碼經 `CTimeOnly` 取得 `TimeOnly`，
> `FieldDbType.Time` 作為語意標記。** 完整理由見 §2，被否決的原生型別方案與實測證據見 §7。
>
> **出貨約束**：**階段 1 是其餘三階段的前提**；階段 2 / 3 / 4 彼此無序，
> **可分別開發、分別發布**。階段 1 自身即為最小可用單元（可宣告 `Time` 欄位、
> 五家能建表、schema 升級收斂）。
>
> **前置依賴**：無。`ValueUtilities.CDate` → `CDateOnly` 的更名已於 2026-07-27 完成
> （commit `49641789`），`CTimeOnly` 的命名規律已就位。
>
> **相關**：[plan-date-semantics.md](plan-date-semantics.md)（日曆日語意，已完成）、
> [plan-datetime-timezone.md](plan-datetime-timezone.md)（時區，已完成）。

---

## 1. 現況與目標

`FieldDbType`（[../../src/Bee.Base/Data/FieldDbType.cs](../../src/Bee.Base/Data/FieldDbType.cs)）目前為：
`String` / `Text` / `Boolean` / `AutoIncrement` / `Short` / `Integer` / `Long` /
`Decimal` / `Currency` / `Date` / `DateTime` / `Guid` / `Binary` / `Unknown`。

**無 `Time`**。框架目前提供的時間語意只有「日曆日 vs 時間點」兩種（`Date` / `DateTime`），
純時刻值沒有型別可依，UI 端也因此無從得知該給時刻編輯控件。

**用詞**（本 plan 全文遵循）：

| 詞 | 語意 | 承載 |
|----|------|------|
| 日曆日 | 哪一天 | `DateOnly` / `FieldDbType.Date` |
| **時刻** | 幾點（一日之內） | 語意 `TimeOnly`，儲存 5 碼字串 / `FieldDbType.Time` |
| 時間點 | 哪一天的幾點 | `DateTime` / `FieldDbType.DateTime` |
| 時距 | 多久 | `TimeSpan`（未來的 `Duration`） |

「時間」僅作為泛指上述四者的上位詞使用。

## 2. 設計定案（規格）

### 2.1 承載方案

```
DB 欄位                     定寬 5 碼字元欄（見 §3），內容 "HH:mm"
DataColumn.DataType         typeof(string)
FieldDbType                 Time            ← 語意標記，不隨底層型別退回 String
ValueUtilities.CTimeOnly    string → TimeOnly?（ParseExact "HH:mm"，空字串回 null）
```

**值域固定 `00:00`–`23:59`，精度到分。** 不支援秒——實務上時刻定義（班別、營業起訖、
提醒時刻）不到秒；需要秒的是打卡流水那類**時間點**，本來就該用 `DateTime`。
故格式單一、無寬度分支，`TableSchema` 不需宣告長度。

**`FieldDbType.Time` 必須存在，不可退回「就用 `String` 欄位自行約定格式」**——
本 plan 的核心目的就是讓欄位語意能自我描述：UI 端據此給時刻編輯控件，
報表與 schema-less 消費端（AnyCode / JS client）據此判別這欄是時刻而非任意字串。
標記與底層存什麼無關。

附帶性質：

- **排序與範圍查詢照常** —— 定寬零填補的 `"HH:mm"` 字典序 = 時序，
  `BETWEEN '08:00' AND '17:00'` 直接成立；數字字串在任何 collation 下排序一致。
- **儲存成本相當** —— 5 bytes vs `time(0)` 的 3–5 bytes。
- **有成熟先例** —— SAP 的 `TIMS` 即 `CHAR(6)`（`HHMMSS`）、`DATS` 即 `CHAR(8)`。
- **raw SELECT 可讀** —— 這是相對原生型別方案的關鍵優勢（見 §7）。

### 2.2 空值即空字串，欄位維持 NOT NULL

框架以 `DateTime.MinValue` 當時間空值 sentinel
（[../../src/Bee.Base/Data/FieldDbTypeExtensions.cs](../../src/Bee.Base/Data/FieldDbTypeExtensions.cs)
的 `ToDbFieldValue`）。**時刻沒有等價的 sentinel** —— `00:00` 是合法的午夜。

字串承載讓這個問題消失：**空字串即「未填」**，`Time` 欄比照其他文字欄維持
NOT NULL、預設空字串，與既有慣例一致，`ToDbFieldValue` 不需新增例外分支。

> `GetDefaultValue(Time)` 回**空字串**，不是 `"00:00"`——午夜是合法時刻，不能當「未設定」用。

### 2.3 範圍與格式由取值層把關，DB CHECK 為可選

`char(5)` 欄位在 DB 端塞得進 `"25:99"` 或 `"abc"`。防線放在取值層：

```csharp
TimeOnly.TryParseExact(s, "HH:mm", out var t)   // 一條就夠
```

`CTimeOnly` 一律經此，非法值進不了業務邏輯。**DB CHECK 約束為可選**——
五家語法各異、維護成本高，且擋不住繞過框架的直接 SQL；需要時由專案自行加。

寫入端同樣要正規化：`"8:30"` → `"08:30"`、`TimeOnly` → `ToString("HH:mm")`，
確保定寬零填補的排序前提永遠成立。

### 2.4 schema 反推需視 `Time` ≡ `String(5)` 等價

**這是字串承載唯一的代價。** `TableSchemaProvider` 從 DB 反推型別時，看到 `char(5)`
**只能推出 `String`**。若不處理：

> 定義說是 `Time` → DB 反推說是 `String(5)` → `TableSchemaComparer` 判定有差異 →
> **每次比對都想 ALTER，永遠收斂不了**

→ **在 `DbField.Compare` 將兩側化約為物理形狀後再比較**（`Time` → `String(5)`）。
diff 的閘門就在這一處，改在此收斂即可，也免去五份 provider 規則各自漂移。
五家 `AlterCompatibilityRules` 另將 `Time` 歸入字串家族，供「已判定有差異後」決定 ALTER/REBUILD 之用。

未採「以 DB extended property 存語意標記」：SQL Server 有現成機制
（框架已有 `SqlExtendedPropertyCommandBuilder`），但 MySQL / SQLite 無等價機制，
五家做不齊會變成 provider 特例——那正是本方案要避開的東西。

`Date` 沒有這個問題，因為它反推得回來（`date` 型別存在）。

### 2.5 取值層：`CTimeOnly` 回 `TimeOnly?`

```csharp
public static TimeOnly? CTimeOnly(object value)   // 空字串 / 非法格式 → null
```

**必須回 nullable，不能沿用 `Cxxx` 家族的 default 參數形狀。**
`CDateOnly(object, DateOnly defaultValue = default)` 的空值回 `0001-01-01`，這安全，
因為它**不是合法業務值**；但 `default(TimeOnly)` = **`00:00` 是完全合法的時刻**，
照抄會讓未填欄位靜默變成午夜，正是 §2.2 要避開的事。

`CDateOnly` / `CTimeOnly` / `CDateTime` 三者形成一致的命名規律（方法名 = 回傳型別名）。

### 2.6 列舉值必須 append 至尾端；舊 client 破口接受並標 breaking

`FieldDbType` 未顯式指定數值（隱含 `0..N`），而它會上 MessagePack wire——
[../../src/Bee.Api.Core/MessagePack/SerializableDataColumn.cs](../../src/Bee.Api.Core/MessagePack/SerializableDataColumn.cs)
的 `DataType` 即為一例。**enum 一律以底層整數上 wire，與鍵style 無關**（`keyAsPropertyName`
改的是成員鍵）。在中間插入 `Time` 會讓其後所有值位移，**打斷既有 payload**。

→ **一律 append 至尾端。**（定義檔不受影響——它存的是 enum **名稱**，實測 `DbType="AutoIncrement"`。）

反向破口**無法靠 append 迴避**：新 server 回傳 `Time` 給舊 client，舊 client 的
`DbTypeConverter.ToType` 走 `default:` 直接擲 `InvalidOperationException`。

→ **決策：接受，以 breaking 標記處理，不寫版本協商機制。** 理由同 ADR-030
（client 與 server 同版發佈、無外部消費者）。階段 4 於 `CHANGELOG` 明標
**breaking — wire**、要求同版升級。為單一列舉值寫協商機制不成比例。

### 2.7 `Time` 絕不轉時區

時刻與日曆日同為牆上時間，套用時區位移會得到無意義的結果。在時區 plan 的
Connector 判斷表中，`Time` 與 `Date` 同列（絕不轉）。改採字串承載後**更安全**——
字串不可能被誤判為時間點而位移。

## 3. provider 型別對應

五家一致，無語意分裂、無特例：

| DB | 欄位型別 | 選型理由 |
|----|---------|---------|
| SQL Server | `nchar(5)` | 留在 N-family，與 `N'...'` 預設字面值及其解析路徑自洽；`char` 在反推表無對應 |
| PostgreSQL | `char(5)` | 定寬 |
| MySQL | `CHAR(5)` | 定寬 |
| SQLite | `VARCHAR(5)` | SQLite 無型別，但宣告型別會被反推——`TEXT` 會反推成 `Text`，`VARCHAR(5)` 才反推成 `String(5)` |
| Oracle | `VARCHAR2(5)` | Oracle 無定寬字元的實益；且 `''`＝`NULL` 使該欄比照 `String` 走 nullable |

> 值恆為 5 碼，定寬型別省去長度前綴，也讓「不足 5 碼」的髒資料在寫入端就顯眼。

## 3.1 實作結果（階段 1–2，2026-07-27）

全 solution Release build 0 警告 0 錯誤；全測試 **5,085 通過 / 0 失敗 / 1 skip**（新增 30 項）。
ADR：[../adr/adr-033-time-of-day-semantics.md](../adr/adr-033-time-of-day-semantics.md)。

**階段 2 併入階段 1 出貨**：`ToFieldValue` 的正規化需要時刻剖析，而那正是 `CTimeOnly` 的本體，
拆兩階段會讓同一份邏輯寫兩次。

與 plan 的差異（皆為實作期發現，非設計變更）：

| 項目 | plan 原文 | 實作 | 原因 |
|------|----------|------|------|
| 等價規則的落點 | 「五家 `AlterCompatibilityRules` 各補一條規則」 | **`DbField.Compare` 一處**化約物理形狀；五家 rules 另補「字串家族」歸類 | diff 的實際閘門是 `DbField.Compare` 的 `DbType != source.DbType`，五家 rules 只在**已判定有差異後**決定 ALTER/REBUILD，根本走不到。改在單一處化約，也免去五份規則各自漂移 |
| 標記 helper | 列為需改動 | **不需改動** | `ResolveFieldDbType` / `ApplyFieldDbType` / `GetDeclaredFieldDbType` 是型別無關的泛用實作 |
| `ExpressionPolicy.CoerceValue` | 「補 `Time` 分支」 | 改補 **`TimeOnly` → `string`** 的邊界轉換 | `Time` 的 `clrType` 是 `string`，字串值本來就走得通；真正會炸的是反向——`TimeOnly` 非 `IConvertible`，會從 `Convert.ChangeType` 擲出 |
| SQL Server 型別 | `char(5)` | **`nchar(5)`** | `char` 在 `SqlTableSchemaProvider` 的反推表無對應（回 `Unknown`），且預設值字面值以 `N'...'` 產生、解析端卻走 `('...')` 分支，兩頭對不上。留在 N-family 兩者自洽。連帶修好 `nchar` 的長度換算（原僅 `NVARCHAR` 除以 2） |
| SQLite 型別 | `TEXT` | **`VARCHAR(5)`** | `TEXT` 反推為 `FieldDbType.Text`，與化約後的 `String(5)` 對不上；`VARCHAR(5)` 反推即 `String(5)` |
| Oracle | 未特別提及 | 另補 `''`＝`NULL` 的既有處置 | 時刻的未填值是空字串，命中 Oracle 把 `''` 視為 `NULL` 的老問題，需比照 `String` 欄位改為 nullable 並捨棄空預設 |
| 整合測試斷言 | 「schema 比對零 diff」 | 斷言**時刻欄位**的 `UpgradeAction` 為 `None` | 整表斷言會被無關的既有行為汙染——SQLite 的 `Guid` 欄位在 DB 端有 `hex(randomblob(16))` 預設、定義端沒有，永遠有 diff |

**順帶發現（未修，與本 plan 無關）**：SQLite 的 `Guid` 欄位定義與 DB 反推之間有永久性 diff，
成因為 DB 端預設值 `hex(randomblob(16))` 未反映於定義。任何 SQLite 表的 schema 比對都會被判 `Upgrade`。

## 3.2 實作結果（階段 3，2026-07-27）

全 solution Release build 0 警告 0 錯誤；全測試 **5,097 通過 / 0 失敗 / 1 skip**（新增 12 項）。

| 端 | 控件 |
|----|------|
| 定義層 | `ControlType.TimeEdit`（append 至尾端）；`LayoutColumnFactory` 將 `FieldDbType.Time` 自動解析為它 |
| Avalonia | `TimeEdit : TextEdit`，`MaxLength = 5`、提交時正規化 |
| MAUI | `Entry` + `MaxLength = 5` + 數字鍵盤，`Unfocused` 時正規化 |
| Blazor（Server / Wasm） | `<input type="text" inputmode="numeric" maxlength="5" pattern="…">`，`onchange` 正規化 |

三個決定：

- **不用原生時刻選擇器**（`<input type="time">` / MAUI `TimePicker`）。它們的**值**格式雖然也是
  `"HH:mm"`，但**呈現**隨瀏覽器 / 裝置語系（12 小時制），違反「顯示格式 = 儲存格式」的定案。
  改用遮罩文字輸入後，三端呈現天然一致。
- **正規化在提交時做，不在每次按鍵**。`"8:3"` 是輸入過程的合法中間狀態。
- **無法解析的輸入保留前一個有效值，不清空欄位**（比照 `NumericEdit`）——
  游標滑一下不該靜默毀掉資料。清空輸入框仍是明確的「取消設定」。

`GridControl` **不需改動**：`IsAlwaysOnEditor` 與儲存格 `DatePicker` 分支只涵蓋選擇器式控件，
文字輸入走既有的一般路徑——這是選遮罩文字輸入的附帶收穫。

**順帶發現（未修，與本 plan 無關）**：`LayoutColumnFactory.ResolveControlType` 只把
`FieldDbType.DateTime` 對應到 `DateEdit`，**`FieldDbType.Date` 沒有對應**，會落到 `TextEdit`。
看似是日曆日 plan 的遺漏；因會改變既有 `Date` 欄位的 UI 行為，未在本 plan 順手改。

## 3.3 實作結果（階段 4，2026-07-27）

| 產出 | 內容 |
|------|------|
| [../time-semantics.md](../time-semantics.md) / [.zh-TW.md](../time-semantics.zh-TW.md) | 雙語公開文件：何時該用、宣告方式、五家欄位型別、讀寫、查詢排序保證、破壞性變更，以及「`Time` 不是什麼」 |
| [../terminology.md](../terminology.md) / [.zh-TW.md](../terminology.zh-TW.md) | 新增「時間語意」一節：日曆日 / 時刻 / 時間點 / 時距四詞與對應型別、判別法；並更新 `FieldDbType` 與 `ControlType` 的值清單 |
| [../README.md](../README.md) / [.zh-TW.md](../README.zh-TW.md) | 文件索引新增時刻欄位條目 |

`public-docs.md` 的落地檢查通過（公開文件無任何指向 `docs/plans/` 的連結）。

**CHANGELOG 未寫，刻意延後至發版**：本 repo 的慣例是 changelog 於 `chore(release)` commit
連同版本號一次寫成（由 `changelog-draft` 從 git 歷史整理），目前 `CHANGELOG.md` 最上方的
`[4.15.0]` 是**已發佈**版本，且下一版版號尚未決定。現在插入條目會既違反慣例、又需憑空認定版號。

> **發版時務必納入的 breaking 條目**（兩則，皆已在 commit message 標明 `BREAKING CHANGE`）：
> 1. `FieldDbType` 新增 `Time` —— 含 `Time` 欄位的表無法被舊版 client 反序列化，須同版升級。
> 2. `ValueUtilities.CDate` 更名為 `CDateOnly`（source-level）。
>
> 另有一項**尚未修正的既有文件錯誤**：`docs/date-semantics.*` 有 6 處寫「v4.15 起」，
> 但該變更（`c5578a42`）落在 `v4.15.0` tag 之後，實際屬下一版。發版時應一併更正版號。

## 4. 階段細節

### 階段 1 — 型別基礎與 DDL

**最小可用單元。** 完成後即可在 `FormSchema` / `TableSchema` 宣告 `Time` 欄位、
五家建得出表、schema 升級能收斂。

**`Bee.Base`**

| 檔案 | 改動 |
|------|------|
| `Data/FieldDbType.cs` | **append** `Time` 至尾端 |
| `Data/DbTypeConverter.cs` | `ToType` → `typeof(string)`；`ToDbType` → `DbType.String`；`ToFieldDbType` 不變（字串仍推 `String`） |
| `Data/FieldDbTypeExtensions.cs` | `GetDefaultValue` → `string.Empty`；`ToFieldValue` → 正規化為 `"HH:mm"`；`ToDbFieldValue` 沿用字串路徑 |
| `Data/DataColumnExtensions.cs` / `DataTableExtensions.cs` | 標記 helper（`ResolveFieldDbType` / `ApplyFieldDbType` / `GetDeclaredFieldDbType`）納入新值 |

**`Bee.Db` provider（28 檔，以 `grep -rln "FieldDbType\." src/Bee.Db/Providers/` 為準）**

| provider | 檔案 |
|----------|------|
| SQL Server | `SqlSchemaSyntax` / `SqlCreateTableCommandBuilder` / `SqlTableRebuildCommandBuilder` / `SqlTableSchemaProvider` / `SqlAlterCompatibilityRules` |
| PostgreSQL | `PgTypeMapping` / `PgSchemaSyntax` / `PgTableRebuildCommandBuilder` / `PgTableSchemaProvider` / `PgAlterCompatibilityRules` |
| MySQL | `MySqlTypeMapping` / `MySqlSchemaSyntax` / `MySqlCreateTableCommandBuilder` / `MySqlTableRebuildCommandBuilder` / `MySqlTableSchemaProvider` / `MySqlAlterCompatibilityRules` |
| SQLite | `SqliteTypeMapping` / `SqliteSchemaSyntax` / `SqliteCreateTableCommandBuilder` / `SqliteTableRebuildCommandBuilder` / `SqliteTableSchemaProvider` / `SqliteAlterCompatibilityRules` |
| Oracle | `OracleTypeMapping` / `OracleSchemaSyntax` / `OracleCreateTableCommandBuilder` / `OracleTableRebuildCommandBuilder` / `OracleTableSchemaProvider` / `OracleAlterCompatibilityRules` |

> 多數是一行對應到 `char(5)`；**`AlterCompatibilityRules` 五檔是唯一含判斷邏輯的**（§2.4）。
> `TableSchemaProvider`（反推）維持推出 `String`，不嘗試辨識 `Time`——由等價規則吸收。

**ADR**：新增 `docs/adr/adr-033-time-of-day-semantics.md`，記錄承載方案的取捨與
**被否決的原生型別方案**（§7 的實測證據移入）。決策紀錄必須落在 ADR，不能只存在於本 plan
（plan 是階段性文件，公開文件不得引用）。

**測試**

- 五家 `[DbFact]`：宣告 `Time` 欄位 → 建表 → INSERT / SELECT round-trip → 值為 `"HH:mm"`
- 五家 `[DbFact]`：建表後跑 schema 比對，**斷言零 diff**（§2.4 的回歸守衛，這是最容易回歸的一條）
- `ToFieldValue` 正規化：`"8:30"` → `"08:30"`、`TimeOnly` 輸入 → `"HH:mm"`
- `GetDefaultValue(Time)` 回空字串（**不是** `"00:00"`）

**驗收**：五家建表通過、schema 比對零 diff、Release build 0 警告 0 錯誤、全測試綠。

### 階段 2 — 取值層與運算式

| 檔案 | 改動 |
|------|------|
| `Bee.Base/ValueUtilities.cs` | 新增 `CTimeOnly`（§2.5），置於 `CDateTime / CDateOnly` region 之後 |
| `Bee.Expressions/ExpressionPolicy.cs` | `CoerceValue` 補 `Time` 分支。`ToClrType` **不需改**——委派 `DbTypeConverter.ToType`，`Time` → `string` 自動跟上 |

**測試**

- `CTimeOnly`：合法值、`"8:30"` 寬鬆輸入、空字串 → `null`、`"25:99"` / `"abc"` → `null`、`DBNull` → `null`
- 計算欄取用 `Time` 欄位不擲例外（漏補 `CoerceValue` 的回歸守衛）

**驗收**：全測試綠。與階段 1 無出貨互鎖。

### 階段 3 — UI 層

**先在 Avalonia 定稿，再移植 MAUI / Blazor**（`Bee.UI.Avalonia` 是 UI 架構試點）。

- `FormField` / `LayoutFieldBase`：時刻欄位的 `ControlType`（新增 `TimeEdit` 或沿用既有輸入控件加遮罩）
- Avalonia：時刻編輯控件，輸入遮罩 + 失焦正規化為 `"HH:mm"`
- MAUI / Blazor：比照移植

**顯示格式定案：固定 `08:00`（24 小時制、零填補），不隨語系。**
即**顯示格式 = 儲存格式**，UI 層不做任何格式轉換，只負責輸入遮罩與失焦正規化
（`"8:30"` → `"08:30"`）。

這條讓階段 3 大幅簡化：

| 免掉的東西 | 說明 |
|-----------|------|
| 語系感知的格式化 / 剖析 | 不需 `CultureInfo`、不需 `LanguageResource` 參與 |
| 顯示值 ≠ 儲存值造成的往返誤差 | 兩者同一份字串，無轉換即無失真 |
| 三端格式一致性的測試矩陣 | 三端都只是遮罩輸入，行為天然一致 |

控件本體因此可以是**帶遮罩的文字輸入**，不必是時鐘 / 轉盤式選擇器。

**驗收**：Avalonia demo 可編輯時刻欄位並正確存回；三端顯示皆為 `HH:mm`、行為一致。

### 階段 4 — 公開文件

- `docs/time-semantics.md` / `.zh-TW.md`（雙語，比照 `date-semantics`）：
  何時用 `Time`、與 `DateTime` 的分界、`"HH:mm"` 格式約定、`CTimeOnly` 用法
- `docs/terminology.md` / `.zh-TW.md`：補「時間語意」一節，定義日曆日 / 時刻 / 時間點 / 時距
  四詞與對應型別（術語表目前**完全沒有**這組詞，而 `Date` / `DateTime` 兩種語意已在跑）
- `CHANGELOG` 雙語 + `docs/changelogs/<version>.md`：標明 **breaking — wire**（§2.6）

**驗收**：雙語同步；`.claude/rules/public-docs.md` 的落地檢查通過（公開文件不得引用 `docs/plans/`）。

## 5. wire：零成本

`DataColumn` 為 `string` 之後，三份序列化管線全部不需改動：

| wire | 狀態 |
|------|------|
| MessagePack | `System.String` 早在 `SafeTypelessFormatter` 白名單 |
| JSON | `ConvertValue` 的 string 路徑本來就通 |
| XML（`DataSet` 持久化） | `string` 欄位無任何限制 |

原生型別方案在此處要付的三份分支（含 `TimeSpan` 非 `IConvertible` 導致的 JSON 讀取炸點）**全數歸零**。

## 6. 工作量級距

| 階段 | 範圍 | 檔數 | 性質 |
|------|------|------|------|
| 1 | provider | 28 | 23 檔多為一行；`AlterCompatibilityRules` 5 檔含邏輯 |
| 1 | `Bee.Base` | ~4 | 含格式正規化 |
| 1 | ADR | 1 | 新增 |
| 2 | `Bee.Base` / `Bee.Expressions` | 2 | |
| 3 | UI 三端 | 待估 | 依控件複用程度 |
| 4 | 公開文件 | 5 | 雙語 + 術語表 + changelog |
| — | wire | **0** | §5 |

這些 switch 幾乎都有 `default: throw`，漏改會**大聲失敗**而非沉默出錯——
「先動工再逐一補齊」是可行策略。

**框架內無既有欄位需要遷移**：掃過 `tests/Define/`、`apps/`、`src/Bee.Definition/Defaults/`
全部定義檔，**零個**以 `String` 土法承載時刻的欄位（唯一命中的 `time_zone` 是 IANA 時區字串）。
對照 43 個 `Date` / `DateTime` 欄位。下游應用專案的個案不在本 plan 範圍。

## 7. 被否決的方案：DB 原生時刻型別 + `TimeSpan` 承載（實測紀錄）

保留此節是為了**避免日後重新推導**；階段 1 撰寫 ADR 時應將本節內容移入 ADR。
原案為「DB 用原生 `time` 型別、`DataColumn` 用 `TimeSpan`、取值層用 `TimeOnly`」。

### 7.1 實測：參數寫入與讀出型別

| DB | 欄位型別 | 傳入 `TimeOnly` | 傳入 `TimeSpan` | 讀回的 CLR 型別 |
|----|---------|----------------|----------------|----------------|
| SQL Server | `time(7)` | ✅ | ✅ | `TimeSpan` |
| PostgreSQL | `time` | ✅ | ✅ | `TimeSpan` |
| MySQL | `TIME(6)` | ✅ | ✅ | `TimeSpan` |
| Oracle | `INTERVAL DAY(0) TO SECOND(6)` | ❌ `ArgumentException` | ❌ `ORA-50028` | — |

### 7.2 `DataSet` 拒收 `TimeOnly`

`DataColumn(typeof(TimeOnly))` 可建、可賦值、`WriteXml` 也寫得出來，但 `ReadXml` 擲
`InvalidOperationException: Type 'System.TimeOnly' is not allowed here`（.NET 的 `DataSet`
允許型別白名單）。`TimeSpan` 全程通過（XML 為 ISO 8601 duration `PT8H30M15S`）。

→ 原案的 `DataColumn` **只能**用 `TimeSpan`，而 `TimeSpan` 在 raw SELECT 與 XML 下都不可讀。

> 附帶更正一個常見誤解：`TimeOnly` / `TimeSpan` / `DateOnly` **三者皆非 `IConvertible`**。
> `DateOnly` 當初在 `Date` plan 出事的真正原因是「欄位型別為 `DateTime`、值為 `DateOnly`，
> 需要轉換才失敗」，不是型別本身的缺陷。

### 7.3 Oracle 不是做不到，是框架綁不出來

失敗訊息為 `ORA-50028: Invalid parameter binding`——問題在框架參數層：`DbCommandSpec`
走通用 `DbType`，而 Oracle 的 interval 綁定需要顯式 `OracleDbType.IntervalDS`，通用 `DbType`
無對應值。可修（比照 SQL Server `datetime2` 的 provider-specific 處理），但那是原案獨有的成本。

### 7.4 各家原生 `TIME` 的語意本身不一致

| DB | 型別 | 語意 |
|----|------|------|
| SQL Server | `time(7)` | 時刻 `00:00:00` – `23:59:59.9999999` |
| PostgreSQL | `time` | 時刻 `00:00:00` – `24:00:00` |
| MySQL | `TIME` | **時距，`-838:59:59` – `838:59:59`** |
| Oracle | 無 `TIME` | — |

原案必須在抽象層額外釘死「時刻、`[0,24)`、非負」並自行收斂；字串承載沒有這個問題。

### 7.5 `TimeOnly` 的算術語意（供階段 2 之後的呼叫端參考）

| 運算 | `TimeSpan` | `TimeOnly` |
|------|-----------|-----------|
| `22:00 + 8h` | `1.06:00:00`（累積） | `06:00`（**繞回**） |
| `06:00 - 22:00` | `-16:00:00` | **`08:00:00`**（繞過午夜，恆正） |
| 跨午夜區間判斷 | 自行實作 | `IsBetween(22:00, 06:00)` ✅ |
| `FromTimeSpan(-01:00 / 24:00 / 25:00)` | — | 全部擲 `ArgumentOutOfRangeException` |
| `ToString()` 預設 | `08:30:15` | `08:30`（**不含秒**） |

兩個要點：

- **繞回減法對「班別定義」是正解**（夜班 `22:00–06:00` 直接得 8 小時），
  但**對實際事件（打卡流水）是陷阱**——時序顛倒的異常資料會被靜默算成合法的長班。
- **繞回減法在起訖相同時退化**：`08:00 – 08:00` 得 `0` 而非 24 小時。
  模 24 的世界裡「一整天」與「零」是同一點。
  → **班長不應由兩個 `Time` 相減得出**，應為獨立欄位或由未來的 `Duration` 承載。

## 8. 實測方法與環境（2026-07-27）

§7 的數據以用完即刪的 xUnit probe 跑，未留在版控中。重現方式：

- **DB 端**：`tests/Bee.Db.UnitTests/` 建 probe class，`IClassFixture<SharedDbFixture>` +
  `[DbFact(DatabaseType.X)]`，對每家 DB 建暫存表 → 以參數 INSERT → `SELECT` 回讀，
  記錄 `DataColumn.DataType` 與 cell 的實際型別。
- **CLR / XML 端**：`DataColumn(typeof(TimeOnly))` 與 `typeof(TimeSpan)` 各跑
  賦值 → `WriteXml(WriteSchema)` → `ReadXml`，並反射檢查 `IConvertible`。
- **wire 端**：`tests/Bee.Api.Core.UnitTests/` 以 `MessagePackCodec` 對兩型別做
  raw round-trip 與 `SerializableDataTable` round-trip。
- **算術語意**：`tests/Bee.Base.UnitTests/` 直接跑 `TimeOnly` / `TimeSpan` 的加減、
  `IsBetween`、`FromTimeSpan` 邊界與 `ToString` 預設格式。

**環境**：`Microsoft.Data.SqlClient` 7.0.0、`Npgsql` 9.0.4、`MySqlConnector` 2.4.0、
`Oracle.ManagedDataAccess.Core` 23.26.200、`Microsoft.Data.Sqlite` 9.0.4、`MessagePack` 3.1.7；
DB 容器 `sql2025` / `pgvector-db` / `mysql8` / `oracle23ai`。
