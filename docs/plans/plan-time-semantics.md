# Plan：`FieldDbType.Time` 純時刻型別（討論稿）

**狀態：📝 擬定中（2026-07-27）**

> **這是討論稿，不是可執行計畫。** 目的是把
> [plan-datetime-timezone.md](plan-datetime-timezone.md) 討論過程中推導出的約束記下來，
> 避免日後動工時重新推導或誤踩。有實際需求時再展開為正式 plan。
>
> **2026-07-27 第二輪**：修正 §3.1 的理由、補上反向相容性破口、新增 provider 語意分裂與工作量級距。
>
> **2026-07-27 第三輪（實測）**：以四個本機 DB 容器與 wire 實跑（方法見 §10），
> 驗證原生時刻型別方案的可行性。
>
> **2026-07-27 第四輪（方案改採字串承載）**：**本輪換過方案。**
> 原案（DB 用原生 `time` 型別、`DataColumn` 用 `TimeSpan`）在第三輪實測後暴露多個管線成本，
> 且 `TimeSpan` 在 raw SELECT 下可讀性差。改採
> **「DB 與 `DataSet` 存 5 碼字串 `"HH:mm"`、程式碼用 `TimeOnly`」**。
> §3.4 / §3.5 / §6 / §7 為本輪重寫；原案的實測證據保留於 §9 供日後回查。
>
> **2026-07-27 第五輪（逐項定案）**：§3.2 舊 client 破口、§3.7 schema 反推、
> 取值層命名與空值形狀三項拍板（見各節），並補上 `Bee.Expressions` 的工作量缺口。

---

## 1. 現況

`FieldDbType`（[../../src/Bee.Base/Data/FieldDbType.cs](../../src/Bee.Base/Data/FieldDbType.cs)）目前為：
`String` / `Text` / `Boolean` / `AutoIncrement` / `Short` / `Integer` / `Long` /
`Decimal` / `Currency` / `Date` / `DateTime` / `Guid` / `Binary` / `Unknown`。

**無 `Time`**。純時刻值（上班時刻 08:30、營業起訖、班別起訖）目前只能以
`String` 或 `DateTime` 勉強表達。

## 2. 為何現在不做

時區 plan 只需要「日曆日 vs 時間點」這條界線，`Date` / `DateTime` 兩個現有值已足夠。
`Time` 是獨立議題，與時區設計無互鎖。

## 3. 已確立的約束（動工時直接沿用）

### 3.1 新值必須加在列舉尾端

`FieldDbType` 未顯式指定數值（隱含 `0..N`），而它會上 MessagePack wire——
[../../src/Bee.Api.Core/MessagePack/SerializableDataColumn.cs](../../src/Bee.Api.Core/MessagePack/SerializableDataColumn.cs)
的 `DataType` 即為一例。**enum 一律以底層整數上 wire，與鍵style 無關**——
`keyAsPropertyName`（ADR-030）改的是成員鍵，不影響 enum 值本身。
在中間插入 `Time`（例如排在 `Date` 旁邊求語意相鄰）會讓其後所有值的數值位移，**打斷既有 payload**。

→ 一律 append 至尾端；或此次順帶改為顯式指定數值後再新增。

> **定義檔不受影響**。定義檔存的是 enum **名稱**不是數值
> （`FormSchema` 實測：`DbType="AutoIncrement"`），改順序不會壞定義檔。
> 第一稿寫「打斷既有定義檔的相容性」有誤。**結論不變，理由收窄為 wire。**

### 3.2 append-only 只保護舊值，新值仍是對舊 client 的單向破壞

append 保證舊 payload 在新版仍讀得對，但**反向不成立**：新 server 回傳 `Time`（= 新序號）給舊 client，
舊 client 的 `DbTypeConverter.ToType` 走 `default:` 直接擲 `InvalidOperationException`
（`ToDbType` 同樣擲 `ArgumentOutOfRangeException`）。

**這不是可以靠 append-only 迴避的問題**，且**不因改採字串承載而消失**——
破口在列舉值本身，與底層存什麼無關。

> **決策（2026-07-27）：接受破口，以 breaking 標記處理，不寫版本協商機制。**
> 理由同 ADR-030：框架 client 與 server 同版發佈，目前無外部消費者。
> 上線時於 `CHANGELOG` 明標 **breaking — wire**、要求 client 與 server 同版升級即可。
> 未採「`Ping` / 版本協商層擋」——為單一列舉值寫協商機制不成比例。

### 3.3 `Time` 屬於「絕不轉時區」

純時刻值與日曆日同為牆上時間，套用時區位移會得到無意義的結果。
在時區 plan 的 Connector 判斷表中，`Time` 與 `Date` 同列（絕不轉）。

→ 此結論已載入時區 plan 的 ADR，`Time` plan 不需重新推導。
改採字串承載後**更安全**——字串不可能被誤判為時間點而位移。

### 3.4 承載方案：DB 與 `DataSet` 存 5 碼字串，程式碼用 `TimeOnly`

```
DB 欄位                     CHAR(5) / VARCHAR2(5)，內容 "HH:mm"
DataColumn.DataType         typeof(string)
FieldDbType                 Time            ← 語意標記，不隨底層型別退回 String
ValueUtilities.CTimeOnly    string → TimeOnly?（ParseExact "HH:mm"，空字串回 null）
```

**值域固定 `00:00`–`23:59`，精度到分。** 不支援秒——實務上時刻定義（班別、營業起訖、
提醒時刻）不到秒；需要秒的是打卡流水那類**時間點**，本來就該用 `DateTime`。
故格式單一、無寬度分支，`TableSchema` 不需宣告長度。

**`FieldDbType.Time` 必須保留，不可退回「就用 `String` 欄位自行約定格式」**——
這個 plan 的原始目的就是讓欄位語意能自我描述：UI 端要據此給時刻編輯控件，
報表與 schema-less 消費端（AnyCode / JS client）要據此判別這欄是時刻而非任意字串。
標記與底層存什麼無關。

**字串承載一次解決的五件事**（對照原案的成本）：

| 原案的問題 | 字串承載 |
|-----------|---------|
| Oracle 無 `TIME`、`INTERVAL` 綁定失敗（§9.3） | **消失**——`VARCHAR2(5)` 五家皆有，零 provider 特例 |
| `DataSet` XML / MessagePack / JSON 三份管線各需補分支 | **消失**——`string` 全通，零工作 |
| `TimeSpan` / `TimeOnly` 的承載型別拉扯 | **消失**——`DataColumn` 就是 `string` |
| 無空值 sentinel、被迫允許 NULL | **消失**——見 §3.5 |
| `TimeSpan` 在 raw SELECT 下可讀性差 | **解決**——這是本輪換方案的起點 |

附帶好處：

- **排序與範圍查詢照常** —— 定寬零填補的 `"HH:mm"` 字典序 = 時序，
  `BETWEEN '08:00' AND '17:00'` 直接成立；數字字串在任何 collation 下排序一致。
- **儲存成本相當** —— 5 bytes vs `time(0)` 的 3–5 bytes。
- **有成熟先例** —— SAP 的 `TIMS` 即 `CHAR(6)`（`HHMMSS`）、`DATS` 即 `CHAR(8)`。
  ERP 以定寬字串承載日期時刻是行之有年的做法。

### 3.5 空值即空字串，欄位維持 NOT NULL

原案的困境：框架以 `DateTime.MinValue` 當時間空值 sentinel
（[../../src/Bee.Base/Data/FieldDbTypeExtensions.cs](../../src/Bee.Base/Data/FieldDbTypeExtensions.cs)
的 `ToDbFieldValue`），但 `TimeSpan.Zero` 就是合法的 `00:00`，沒有等價的 sentinel 可用，
逼得 `Time` 欄必須允許 NULL——與「文字/數值欄 NOT NULL」的既有偏好衝突。

**字串承載讓這個困境整個消失**：空字串即「未填」，`Time` 欄比照其他文字欄
維持 NOT NULL、預設空字串，與既有慣例完全一致，`ToDbFieldValue` 也不需要新增例外分支。

> 承前，`GetDefaultValue(Time)` 回**空字串**，不是 `"00:00"`——
> 午夜是合法時刻，不能當「未設定」用。

**取值層對應地必須回 nullable**，見 §3.8。

### 3.6 範圍與格式由取值層把關，DB CHECK 為可選

`CHAR(5)` 欄位在 DB 端塞得進 `"25:99"` 或 `"abc"`。防線放在框架的取值層：

```csharp
TimeOnly.TryParseExact(s, "HH:mm", out var t)   // 一條就夠
```

`CTime` 一律經此，非法值進不了業務邏輯。**DB CHECK 約束為可選**——
五家語法各異、維護成本高，且擋不住繞過框架的直接 SQL；需要時由專案自行加。

寫入端同樣要正規化：`"8:30"` → `"08:30"`、`TimeOnly` → `ToString("HH:mm")`，
確保定寬零填補的排序前提永遠成立。

### 3.7 schema 反推需視 `Time` ≡ `String(5)` 等價

**這是字串承載唯一的新問題。** `TableSchemaProvider` 從 DB 反推欄位型別時，
看到 `CHAR(5)` **只能推出 `String`**，推不出 `Time`。若不處理：

> 定義說是 `Time` → DB 反推說是 `String(5)` → `TableSchemaComparer` 判定有差異 →
> **每次比對都想 ALTER，永遠收斂不了**

> **決策（2026-07-27）：`AlterCompatibilityRules` 視 `Time` ≡ `String(5)` 等價。**
> 便宜，且語意正確——升級方向本來就只從定義往 DB 走，DB 端反推只用於「要不要改」的判斷，
> 判等價即可。五家各補一條規則。

未採「以 DB extended property 存語意標記」：SQL Server 有現成機制
（框架已有 `SqlExtendedPropertyCommandBuilder`），但 MySQL / SQLite 無等價機制，
五家做不齊會變成 provider 特例——那正是本方案要避開的東西。

`Date` 沒有這個問題，因為它反推得回來（`date` 型別存在）。這是字串承載要付的帳。

### 3.8 取值層命名：`CTimeOnly` 回 `TimeOnly?`

```csharp
public static TimeOnly? CTimeOnly(object value)   // 空字串 / 非法格式 → null
```

**必須回 nullable，不能沿用 `Cxxx` 家族的 default 參數形狀。** 理由：

`CDate` 現行簽章為 `CDate(object value, DateOnly defaultValue = default)`，空值回
`default(DateOnly)` = `0001-01-01`。這安全，因為它**不是合法業務值**。
但 `default(TimeOnly)` = **`00:00`，是完全合法的時刻** ——
若照抄，未填的欄位會靜默變成午夜，正是 §3.5 要避開的事。

**連帶更名（獨立於 `Time`，可先行）**：`CDate` → **`CDateOnly`**。
`CDate` 自 4.15.0 已回傳 `DateOnly`，方法名與回傳型別對齊後，
`CDateOnly` / `CTimeOnly` / `CDateTime` 三者形成一致的命名規律（方法名 = 回傳型別名），
呼叫端一眼可知拿到什麼。

> **更名是 breaking（source）**，但與 `Time` 無依賴，可獨立於本 plan 先行。
> 更名**不改變 `CDate` 的 nullability** —— 它維持 `DateOnly` + default 參數，
> 因為 `0001-01-01` 這個 sentinel 對日曆日仍然成立。

## 4. 待討論議題

| 議題 | 說明 |
|------|------|
| 標記 helper | `ResolveFieldDbType` / `ApplyFieldDbType` / `GetDeclaredFieldDbType` 需納入新值（機械工，列此備忘） |
| UI 層 | `FormField` 與各 UI 端（Avalonia / MAUI / Blazor）的時刻編輯控件；顯示格式是否隨語系（`08:30` vs `上午 8:30`） |
| 既有 `String` 欄位遷移 | 目前以 `String` 土法承載時刻的欄位，改標 `Time` 後值格式是否需正規化 |

**已就地結案、不再列為待議**：

- ~~抽象層語意界定~~ → `FieldDbType.Time` = **時刻**、`00:00`–`23:59`（§3.4）。
  若日後需表達「工時 7.5 小時」，那是另一個 `FieldDbType.Duration`（時距），
  **不要讓 `Time` 一詞兩用**。
- ~~CLR 承載型別~~ → §3.4。
- ~~空值表達~~ → §3.5。
- ~~範圍約束落在哪一層~~ → §3.6。
- ~~schema 反推撞牆~~ → §3.7（`AlterCompatibilityRules` 判等價）。
- ~~取值層命名與空值形狀~~ → §3.8（`CTimeOnly` 回 `TimeOnly?`）。
- ~~舊 client 破口的處置~~ → §3.2（接受，標 breaking）。
- ~~Oracle 落地方式~~ → 隨字串承載消失（§3.4 對照表）。
- ~~三份 wire 的成本~~ → 隨字串承載歸零（§6）。
- ~~typeless 白名單~~ → `System.String` 本就在白名單。

## 5. provider 型別對應

五家一致，無語意分裂、無特例：

| DB | 欄位型別 |
|----|---------|
| SQL Server | `char(5)` |
| PostgreSQL | `char(5)` |
| MySQL | `CHAR(5)` |
| SQLite | `TEXT` |
| Oracle | `VARCHAR2(5)` |

> 定寬 `char` 優於 `varchar`：值本來就恆為 5 碼，定寬省去長度前綴，
> 也讓「不足 5 碼」這種髒資料在寫入端就顯眼。SQLite 無型別，`TEXT` 即可。

## 6. wire：零成本

`DataColumn` 為 `string` 之後，三份序列化管線全部不需要改動：

| wire | 狀態 |
|------|------|
| MessagePack | `System.String` 早在 `SafeTypelessFormatter` 白名單 |
| JSON | `ConvertValue` 的 string 路徑本來就通 |
| XML（`DataSet` 持久化） | `string` 欄位無任何限制 |

原案在此處要付的三份分支（含 `TimeSpan` 非 `IConvertible` 導致的 JSON 讀取炸點）**全數歸零**。

## 7. 工作量級距

| 範圍 | 檔數 | 性質 |
|------|------|------|
| 5 個 provider × TypeMapping / SchemaSyntax / CreateTable / TableRebuild | ~20 | **多為一行**——對應到 `char(5)` |
| `AlterCompatibilityRules` 等價規則（§3.7） | 5 | 需要判斷邏輯，非一行 |
| `Bee.Base`（`DbTypeConverter` / `FieldDbTypeExtensions` / `ValueUtilities.CTimeOnly` 等） | ~6 | 含格式正規化與 `CTimeOnly` |
| `Bee.Expressions`（`ExpressionPolicy.CoerceValue`） | 1 | 運算式欄位取用時刻的必經處 |
| wire | **0** | §6 |

**約 30 個檔位，但其中約 20 個是一行對應**，實質工作量遠低於原案的「約 40 檔位且多含邏輯」。
這些 switch 幾乎都有 `default: throw`，漏改會**大聲失敗**而非沉默出錯。

> `ExpressionPolicy.ToClrType` **不需改** —— 它委派給 `DbTypeConverter.ToType`，
> `Time` → `string` 會自動跟上。要補的只有 `CoerceValue` 的 `Time` 分支
> （見 [../../src/Bee.Expressions/ExpressionPolicy.cs](../../src/Bee.Expressions/ExpressionPolicy.cs)）。
> 漏補會在計算欄取用時刻欄位時踩到。

## 8. 展開時機

**這是延後、不是擱置**——`Time` 是確定要補的表達力缺口，只是排在時區 plan 之後。
待有實際業務需求（排班、營業時間、班別定義等）牽動優先序時，將本文展開為正式 plan。
在此之前不動 `FieldDbType`。

原本列為「唯一值得提前做」的 Oracle 可行性驗證**已無必要**——改採字串承載後，
Oracle 不再是特例。**可行性上已無未知數**，剩下的都是機械工。

## 9. 被否決的方案：DB 原生時刻型別 + `TimeSpan` 承載（實測紀錄）

保留此節是為了**避免日後重新推導**。原案為「DB 用原生 `time` 型別、`DataColumn` 用 `TimeSpan`、
取值層用 `TimeOnly`」，於第三輪實測後否決。

### 9.1 實測：參數寫入與讀出型別

| DB | 欄位型別 | 傳入 `TimeOnly` | 傳入 `TimeSpan` | 讀回的 CLR 型別 |
|----|---------|----------------|----------------|----------------|
| SQL Server | `time(7)` | ✅ | ✅ | `TimeSpan` |
| PostgreSQL | `time` | ✅ | ✅ | `TimeSpan` |
| MySQL | `TIME(6)` | ✅ | ✅ | `TimeSpan` |
| Oracle | `INTERVAL DAY(0) TO SECOND(6)` | ❌ `ArgumentException` | ❌ `ORA-50028` | — |

### 9.2 `DataSet` 拒收 `TimeOnly`

`DataColumn(typeof(TimeOnly))` 可建、可賦值、`WriteXml` 也寫得出來，但 `ReadXml` 擲
`InvalidOperationException: Type 'System.TimeOnly' is not allowed here`（.NET 的 `DataSet`
允許型別白名單）。`TimeSpan` 全程通過（XML 為 ISO 8601 duration `PT8H30M15S`）。

→ 故原案的 `DataColumn` **只能**用 `TimeSpan`，而 `TimeSpan` 在 raw SELECT 與 XML 下都不可讀。

> 附帶更正一個常見誤解：`TimeOnly` / `TimeSpan` / `DateOnly` **三者皆非 `IConvertible`**。
> `DateOnly` 當初在 `Date` plan 出事的真正原因是「欄位型別為 `DateTime`、值為 `DateOnly`，
> 需要轉換才失敗」，不是型別本身的缺陷。

### 9.3 Oracle 不是做不到，是框架綁不出來

失敗訊息為 `ORA-50028: Invalid parameter binding`——問題在框架參數層：`DbCommandSpec`
走通用 `DbType`，而 Oracle 的 interval 綁定需要顯式 `OracleDbType.IntervalDS`，通用 `DbType`
無對應值。可修（比照 SQL Server `datetime2` 的 provider-specific 處理），但那是原案獨有的成本。

### 9.4 各家原生 `TIME` 的語意本身不一致

| DB | 型別 | 語意 |
|----|------|------|
| SQL Server | `time(7)` | 時刻 `00:00:00` – `23:59:59.9999999` |
| PostgreSQL | `time` | 時刻 `00:00:00` – `24:00:00` |
| MySQL | `TIME` | **時距，`-838:59:59` – `838:59:59`** |
| Oracle | 無 `TIME` | — |

原案必須在抽象層額外釘死「時刻、`[0,24)`、非負」並自行收斂；字串承載沒有這個問題。

### 9.5 `TimeOnly` 的算術語意（若日後在取值層使用）

實測結果，供 `CTime` 回 `TimeOnly` 後的呼叫端參考：

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
  → **班長不應由兩個 `Time` 相減得出**，應為獨立欄位或由 `Duration` 承載。

## 10. 實測方法與環境（2026-07-27）

以用完即刪的 xUnit probe 跑，未留在版控中。重現方式：

- **DB 端**：在 `tests/Bee.Db.UnitTests/` 建 probe class，`IClassFixture<SharedDbFixture>` +
  `[DbFact(DatabaseType.X)]`，對每家 DB 建暫存表 → 以參數 INSERT → `SELECT` 回讀，
  記錄 `DataColumn.DataType` 與 cell 的實際型別。
- **CLR / XML 端**：`DataColumn(typeof(TimeOnly))` 與 `typeof(TimeSpan)` 各跑
  賦值 → `WriteXml(WriteSchema)` → `ReadXml`，並反射檢查 `IConvertible`。
- **wire 端**：在 `tests/Bee.Api.Core.UnitTests/` 以 `MessagePackCodec` 對兩型別做
  raw round-trip 與 `SerializableDataTable` round-trip。
- **算術語意**：在 `tests/Bee.Base.UnitTests/` 直接跑 `TimeOnly` / `TimeSpan` 的
  加減、`IsBetween`、`FromTimeSpan` 邊界與 `ToString` 預設格式。

**環境**：provider 版本 `Microsoft.Data.SqlClient` 7.0.0、`Npgsql` 9.0.4、
`MySqlConnector` 2.4.0、`Oracle.ManagedDataAccess.Core` 23.26.200、
`Microsoft.Data.Sqlite` 9.0.4、`MessagePack` 3.1.7；DB 容器 `sql2025` / `pgvector-db` /
`mysql8` / `oracle23ai`。
