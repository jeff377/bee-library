# Plan：`FieldDbType.Time` 純時刻型別（討論稿）

**狀態：📝 擬定中（2026-07-27）**

> **這是討論稿，不是可執行計畫。** 目的是把
> [plan-datetime-timezone.md](plan-datetime-timezone.md) 討論過程中推導出的約束記下來，
> 避免日後動工時重新推導或誤踩。有實際需求時再展開為正式 plan。
>
> 2026-07-27 第二輪討論：修正 §3.1 的理由、補上反向相容性破口、把 §4 兩個議題就地結案
> （CLR 承載型別、typeless 白名單），並新增 provider 語意分裂與工作量級距兩節。

---

## 1. 現況

`FieldDbType`（[../../src/Bee.Base/Data/FieldDbType.cs](../../src/Bee.Base/Data/FieldDbType.cs)）目前為：
`String` / `Text` / `Boolean` / `AutoIncrement` / `Short` / `Integer` / `Long` /
`Decimal` / `Currency` / `Date` / `DateTime` / `Guid` / `Binary` / `Unknown`。

**無 `Time`**。純時刻值（上班時間 08:30、營業時間、班別起訖）目前只能以
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

> **定義檔不受影響**（2026-07-27 修正）。定義檔存的是 enum **名稱**不是數值
> （`FormSchema` 實測：`DbType="AutoIncrement"`），改順序不會壞定義檔。
> 原稿寫「打斷既有定義檔的相容性」有誤，理由留錯會導致日後做出過度保守的決定。
> **結論不變，只有理由收窄為 wire。**

### 3.2 append-only 只保護舊值，新值仍是對舊 client 的單向破壞

append 保證舊 payload 在新版仍讀得對，但**反向不成立**：新 server 回傳 `Time`（= 新序號）給舊 client，
舊 client 的 `DbTypeConverter.ToType` 走 `default:` 直接擲 `InvalidOperationException`
（`ToDbType` 同樣擲 `ArgumentOutOfRangeException`）。

→ `Time` 上線需要「舊 client 不會取到含 `Time` 欄位的表」的部署紀律，或在 `Ping` / 版本協商層擋。
**這不是可以靠 append-only 迴避的問題**，展開為正式 plan 時必須有明確決策。

### 3.3 `Time` 屬於「絕不轉時區」

純時刻值與日曆日同為牆上時間，套用時區位移會得到無意義的結果。
在時區 plan 的 Connector 判斷表中，`Time` 與 `Date` 同列（絕不轉）。

→ 此結論已載入時區 plan 的 ADR，`Time` plan 不需重新推導。

### 3.4 CLR 承載型別：`DataColumn` 用 `TimeSpan`，取值層回 `TimeOnly`

**`Date` 的教訓不能直接套用。** `Date` 改採顯式標記（見 [plan-date-semantics.md](plan-date-semantics.md)）
是三個條件疊加的結果：`Date` / `DateTime` 共用 CLR `DateTime`、DB 讀回來就是 `DateTime`、且有既有資料。
`DateOnly` 不實作 `IConvertible`，`DataColumn` 賦值直接掛（教訓已記在
[../../src/Bee.Base/Data/FieldDbTypeExtensions.cs](../../src/Bee.Base/Data/FieldDbTypeExtensions.cs)
的 `ToFieldValue` 註解）。

`Time` **三個條件一個都不成立**：綠地、無既有資料、`TimeSpan` 與 `Time` 一對一。推論有二：

- **`Time` 欄位自我描述，不需要 `ExtendedProperties` 標記** ——
  `Date` 需要標記是因為與 `DateTime` 撞 CLR 型別，`Time` 沒有這個問題。
  這是 `Time` 比 `Date` 便宜一整個階段的地方。
- **`DataColumn` 承載型別選 `TimeSpan` 而非 `TimeOnly`** ——
  `TimeOnly` 同樣不實作 `IConvertible`，選它等於重蹈 `DateOnly` 覆轍。

取值層 `CTime` 則回 `TimeOnly`，與 `CDate` 回 `DateOnly` 對稱。
「儲存層 `TimeSpan` / 取值層 `TimeOnly`」的分工與 `Date` 現況（儲存 `DateTime` + 標記、取值 `DateOnly`）同構。

## 4. 待討論議題

| 議題 | 說明 |
|------|------|
| 抽象層語意界定 | **可行性的第一前提**，見 §5——各 provider 的 `TIME` 語意不一致，須先釘死抽象層的定義 |
| Oracle 落地方式 | 見 §5，建議 `INTERVAL DAY(0) TO SECOND(n)`，但需實測 ODP.NET 雙向對應 |
| 取值層 | `ValueUtilities` 新增 `CTime` 回 `TimeOnly`，與 `CDate` / `CDateTime` 家族對稱 |
| JSON wire | 需顯式補分支，見 §6 |
| 標記 helper | `ResolveFieldDbType` / `ApplyFieldDbType` / `GetDeclaredFieldDbType` 需納入新值 |
| UI 層 | `FormField` 與各 UI 端（Avalonia / MAUI / Blazor）的時刻編輯控件 |

**已於 2026-07-27 就地結案、不再列為待議**：

- ~~CLR 承載型別~~ → 見 §3.4。
- ~~typeless 白名單~~ → `System.TimeSpan` **已在**
  [../../src/Bee.Definition/Serialization/SafeTypelessFormatter.cs](../../src/Bee.Definition/Serialization/SafeTypelessFormatter.cs)
  白名單中（`DateOnly` 當初的缺口不會重演）。
- ~~三棲序列化~~ → MessagePack 側 cell 走 `Dictionary<string, object?>` typeless，
  白名單既已涵蓋即為零成本；XML 走 `TimeSpan` 內建支援。剩下的只有 JSON，見 §6。

## 5. provider 語意分裂（可行性關鍵）

原稿只寫「Oracle 無 `TIME`」，但真正的雷更大——**各家 `TIME` 的語意本身就不一致**：

| DB | 型別 | 語意 |
|----|------|------|
| SQL Server | `time(7)` | 牆上時刻 `00:00:00` – `23:59:59.9999999` |
| PostgreSQL | `time` | 牆上時刻 `00:00:00` – `24:00:00` |
| MySQL | `TIME` | **duration，`-838:59:59` – `838:59:59`** |
| SQLite | `TEXT` | 無型別 |
| Oracle | 無 | 需替代方案 |

MySQL 的 `TIME` 根本不是牆上時刻，而是可正可負、可超過一日的 duration。

→ 第一件事是**在抽象層釘死「`FieldDbType.Time` = 牆上時刻、`[0, 24)`、非負」**，
再於 MySQL / Oracle 以 CHECK 約束或框架層驗證壓住範圍。沒先釘死，跨 DB 行為必然不一致。

**Oracle 落地建議**：`INTERVAL DAY(0) TO SECOND(n)` —— ODP.NET 與 `TimeSpan` 天然對應，
優於下列替代：

| 替代方案 | 問題 |
|---------|------|
| `DATE` + 固定基準日 | 基準日會洩漏到查詢與顯示 |
| `NUMBER`（午夜起算秒數） | 可排序，但處處要轉、DB 端不可讀 |
| `VARCHAR2(8)` `'HH24:MI:SS'` | 定寬零填補故可字典序排序，但丟失型別 |

`INTERVAL DAY TO SECOND` 本身也是 duration 語意（同 MySQL），故仍需上述範圍約束。

## 6. 兩份 wire 的成本不對稱

| wire | 成本 |
|------|------|
| MessagePack | **幾乎零成本**。cell 走 `Dictionary<string, object?>` typeless，`System.TimeSpan` 已在白名單 |
| JSON | **必須顯式補分支（讀寫兩向）** |

JSON 側的原因：[../../src/Bee.Base/Serialization/DataTableJsonConverter.cs](../../src/Bee.Base/Serialization/DataTableJsonConverter.cs)
的 `ConvertValue` 尾端走 `Convert.ChangeType`，`TimeSpan` 不實作 `IConvertible` → 落 catch →
回傳原 string → `DataRow` 賦值擲例外。需比照 `byte[]` / `Guid` / `DateTime` 加一條 `TimeSpan` 分支，
寫入端同樣需明確格式（建議 `"HH:mm:ss.fffffff"` 定寬，避免文化相依）。

## 7. 工作量級距

新增一個 `FieldDbType` 值要動的檔案：

| 範圍 | 檔數 |
|------|------|
| 5 個 provider × 6 檔（TypeMapping / SchemaSyntax / CreateTableCommandBuilder / TableRebuildCommandBuilder / AlterCompatibilityRules / TableSchemaProvider 反推） | ~30 |
| `Bee.Base`（`DbTypeConverter` / `FieldDbTypeExtensions` / `DataTableExtensions` / `ValueUtilities` 等） | ~6 |
| JSON wire | 1 |

**約 40 個檔位，不是小改。** 唯一的好消息是這些 switch 幾乎都有 `default: throw`，
漏改會**大聲失敗**而非沉默出錯——這讓「先動工再逐一補齊」成為可行策略。

## 8. 展開時機

**這是延後、不是擱置**——`Time` 是確定要補的表達力缺口，只是排在時區 plan 之後。
待有實際業務需求（排班、營業時間、班別定義等）牽動優先序時，將本文展開為正式 plan。
在此之前不動 `FieldDbType`。

**唯一值得提前做的**：§5 的 Oracle 落地是可行性關鍵前提，且與其餘設計解耦。
可在無需求時就實測 ODP.NET `INTERVAL DAY TO SECOND` ↔ `TimeSpan` 的 DDL 與雙向對應，
把最大的不確定性先拆掉；其餘部分等真有需求時再動，屆時剩下的多是機械工。
