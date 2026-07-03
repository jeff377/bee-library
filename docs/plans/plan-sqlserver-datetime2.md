# 計畫：SQL Server 日期時間欄位由 `datetime` 遷移至 `datetime2`

**狀態：✅ 已完成（2026-07-03）**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 1 | forward DDL 輸出 `datetime2(7)` + reverse-map 認得 `datetime2` 並讀入 DateTime scale；新建欄位即為 datetime2 | ✅ 已完成 |
| 2 | comparer 具 DateTime 精度感知 → 既有 `datetime` 欄位自動 ALTER 升級為 `datetime2(7)` | ✅ 已完成 |
| 3 | `DbTypeMapper` 寫入參數 `DateTime → DbType.DateTime2`（跨 provider），實現亞毫秒 + pre-1753 | ✅ 已完成 |

## 背景

`FieldDbType.DateTime` 在 SQL Server dialect 目前對映到 `[datetime]`
（[SqlSchemaSyntax.cs:65](../../src/Bee.Db/Providers/SqlServer/SqlSchemaSyntax.cs#L65)），
與 .NET `DateTime` 存在兩個落差：

| 維度 | .NET `DateTime` | SQL `datetime` | SQL `datetime2(7)` |
|------|----------------|----------------|-----------------|
| 精度 | 100 ns（1 tick） | ~3.33 ms（1/300 秒） | 100 ns |
| 範圍 | 0001 ~ 9999 | 1753 ~ 9999 | 0001 ~ 9999 |

具體風險：

1. **精度截斷**：寫入 `10:00:00.001` 被 round 成 `.000`；round-trip 後 `original == fromDb` 為 false。
2. **範圍溢位**：`DateTime.MinValue`（0001 年）或任何 < 1753 的值寫入 `datetime` 直接拋 `SqlDateTimeOverflow`。
   框架若以 `DateTime.MinValue` 當「空值」哨兵（見 [FieldDbTypeExtensions.cs:69](../../src/Bee.Base/Data/FieldDbTypeExtensions.cs#L69)），此路徑有雷。
3. **跨 provider 不一致**：MySQL 用 `datetime(6)`、PostgreSQL 用 `timestamp`，僅 SQL Server 停在 ms 級 `datetime`。

`datetime2` 為 SQL Server 2008 起微軟建議新開發一律採用的型別，與 `DateTime` 完全對齊。

## 已確認決策

- **D1 精度：`datetime2`（預設精度 7，100 ns）** — 最貼近 .NET `DateTime` 的 tick 解析度。DDL 輸出 `[datetime2](7)`。
- **D2 遷移範圍：強制升級既有欄位** — 既有 `datetime` 欄位於下次 schema upgrade 時自動 ALTER 為 `datetime2(7)`。
- **D3 寫入參數型別：`DbType.DateTime2`（跨 provider）** — 見下方「關鍵發現」。僅改 schema 不足以達成目標。

## 關鍵發現：schema 改動不足，寫入參數層才是精度/範圍的真正瓶頸

`DbParameterSpec` 是**所有 provider 共用的唯一寫入參數路徑**，其
[DbTypeMapper.Infer](../../src/Bee.Db/DbTypeMapper.cs#L28) 把 .NET `DateTime` 一律推斷為 `DbType.DateTime`。
即使欄位是 `datetime2(7)`，SqlClient 在**送出前**就會：

- **截成 ms 精度**（client 端先 round 成 datetime 解析度才上線）→ `.1234567` 尾數在參數層就消失。
- **pre-1753 直接拋 `SqlDateTimeOverflow`**（下限檢查在參數層，值到不了欄位）。

故必須讓 SQL Server 的 DateTime 參數送出時型別為 `DbType.DateTime2`。

> **⚠️ 修正（2026-07-03）**：最初把 `DbTypeMapper.Infer` 全域改為 `DbType.DateTime2`，導致
> **PostgreSQL / Oracle 的 Northwind seed 回歸**（Npgsql 對 `Kind=Utc` 值 + `timestamp`-without-tz
> 欄位在 DateTime2 下拒寫，seed 靜默 0 rows；本機因 shared DB 有舊 seed 資料而未重現，CI fresh
> 容器才炸）。正解：**保持 `DbTypeMapper.Infer` 為 `DbType.DateTime`（跨 provider 不動）**，改在
> **provider-aware 的 `DbCommandSpec.NormalizeDbType` 內只對 SQL Server 把 `DateTime → DateTime2`**
> （與既有 Oracle `Guid → Binary` 改寫同一機制）。其他 provider 的 DateTime 行為與改動前完全一致。

> D1 選 7 位（非 6）代表精度高於 MySQL `datetime(6)` / PostgreSQL `timestamp`（微秒級）；
> 跨 provider 讀寫時第 7 位小數在另兩家會被截，但這是各 DB 精度上限差異、非本框架 bug。

## 核心機制：如何讓既有 `datetime` 被偵測並升級

`TableSchemaComparer` 在 **`FieldDbType` 層** 比對欄位（[DbField.Compare](../../src/Bee.Definition/Database/DbField.cs#L165)），
而 `datetime` 與 `datetime2` reverse-map 後**都是 `FieldDbType.DateTime`** → 預設情況下 comparer 看不出差異、不會 ALTER。

**解法**：利用 SQL Server `sys.columns.scale` 可區分兩者的事實
（[GetColumns 查詢已讀入 scale](../../src/Bee.Db/Providers/SqlServer/SqlTableSchemaProvider.cs#L192)）：

| 型別 | `sys.columns.scale` |
|------|--------------------|
| `datetime`（舊） | **3** |
| `datetime2(7)`（新） | **7** |
| `date` | 0（已是獨立 `FieldDbType.Date`，不受影響） |

於是：
1. reverse-map 對 DateTime 欄位**一併讀入 scale**（目前僅 Decimal 讀 scale）。
2. `DbField.Compare` 對 DateTime **加入 scale 正規化比對**：定義端未設 scale → 正規化為 canonical 7；
   實體端沿用 DB 實際 scale。
3. 既有 `datetime`（real scale=3）vs 定義 `datetime2`（normalized 7）→ Compare 回 false
   → comparer 產生 `AlterFieldChange` → 執行 `ALTER COLUMN … datetime2(7)`。
4. 升級後該欄 real scale=7 = normalized 7 → 收斂，後續 compare 無 phantom change。

`ALTER COLUMN datetime → datetime2` 屬 DateTime 同 family（[SqlAlterCompatibilityRules L90-92](../../src/Bee.Db/Providers/SqlServer/SqlAlterCompatibilityRules.cs#L90)）
→ 天然 ALTER-compatible、非 narrowing、in-place widening 無資料遺失，**無需改 ALTER 規則**。

---

## 階段 1：新建欄位改用 `datetime2(7)`（forward + reverse map）

### 1.1 DDL 輸出（forward map）
[SqlSchemaSyntax.cs:64-65](../../src/Bee.Db/Providers/SqlServer/SqlSchemaSyntax.cs#L64)
```csharp
case FieldDbType.DateTime:
    return "[datetime2](7)";   // was: "[datetime]"
```

### 1.2 Reverse map — 型別
[SqlTableSchemaProvider.GetFieldDbType](../../src/Bee.Db/Providers/SqlServer/SqlTableSchemaProvider.cs#L285)：
新增 `case "DATETIME2":` 與既有 `case "DATETIME":` 併回 `FieldDbType.DateTime`（保留 `DATETIME` 以相容既有欄位）。

### 1.3 Reverse map — 讀入 DateTime scale
[SqlTableSchemaProvider.ParseDbField](../../src/Bee.Db/Providers/SqlServer/SqlTableSchemaProvider.cs#L238)：
目前只在 Decimal 分支設 `Scale`；新增 DateTime 分支從 `Decimals` 欄讀入實際 scale（datetime→3、datetime2→7）。

### 1.4 Reverse map — 預設值
[ParseDBDefaultValue L322-325](../../src/Bee.Db/Providers/SqlServer/SqlTableSchemaProvider.cs#L322)：
`DATETIME2` 併入 `DATE`/`DATETIME` 的 `LeftRightCut("(", ")")` 分支。
`GetDefaultValueExpression` 維持 `getdate()`（隱式轉入 datetime2，round-trip 比對仍相符）。

### 階段 1 驗收
- 新建 SQL Server 表 DateTime 欄位為 `datetime2(7)`。
- reverse-map：`datetime` 與 `datetime2` 皆回 `FieldDbType.DateTime`；DateTime 欄 `Scale` 正確填入。
- 既有 `datetime` 欄位此階段**尚不會**被 ALTER（Compare 尚未比 scale）——連續 compare 無 phantom change。

---

## 階段 2：既有 `datetime` 強制升級為 `datetime2(7)`

### 2.1 `DbField.Compare` 加 DateTime scale 比對
[DbField.cs:165](../../src/Bee.Definition/Database/DbField.cs#L165)，於現有 Decimal 比對之後新增：
```csharp
// DateTime precision: unset define scale normalizes to canonical 7 (datetime2 default).
// Legacy `datetime` reverse-maps to scale 3, so it differs from a defined datetime2(7)
// and triggers an in-place ALTER upgrade.
if (DbType == FieldDbType.DateTime)
{
    int definedScale = Scale > 0 ? Scale : 7;
    int actualScale = source.Scale > 0 ? source.Scale : 7;
    if (definedScale != actualScale) return false;
}
```
> 兩處呼叫端皆為 `defineField.Compare(realField)`（this=define、source=real），正規化雙向對稱、無方向風險。

### 2.2 首次升級的行為說明
啟用後，**第一次對既有 DB 跑 schema upgrade 時，所有含 `datetime` 欄位的表會各產生一次 `ALTER COLUMN`**。
屬預期的一次性遷移；升級後即收斂。需在 release note / CHANGELOG 標註此行為（避免使用者誤以為 schema 漂移）。

### 階段 2 驗收
- 以原生 SQL 手動建 `datetime` 欄位的表 → 跑 comparer → 產生 `AlterFieldChange`（datetime2）。
- 執行 ALTER 後再次 compare → 無 change（收斂）。
- ALTER 後既有資料完整、無精度遺失（widening）。

---

## 測試（Bee.Db.UnitTests + Bee.Repository.UnitTests）

**單元（無 DB）**
- `ConvertDbType(DateTime)` → `[datetime2](7)`。
- `GetFieldDbType("datetime2", …)` 與 `GetFieldDbType("datetime", …)` 皆 → `FieldDbType.DateTime`。
- `Compare`：define DateTime(scale 0) vs real datetime2(scale 7) → 相等；vs real datetime(scale 3) → 不等。

**`[DbFact(DatabaseType.SQLServer)]`**
- 亞毫秒 round-trip：寫入 `.001` 秒與 `.1234567` 尾數，讀回精度不被吃掉（對比舊 datetime 的 ms round）。
- 範圍：寫入 `new DateTime(1200,1,1,…,DateTimeKind.Unspecified)` 不拋 `SqlDateTimeOverflow`。
- 升級路徑：原生 SQL 建 `datetime` 欄 → comparer 產 ALTER → 執行後 compare 收斂、資料完整。
- schema 穩定性：對既有 SQL Server 測試 schema 連續兩次 compare，datetime2 欄位不誤判 phantom ALTER。

## 風險與緩解

| 風險 | 緩解 |
|------|------|
| reverse-map 漏認 `datetime2` → 每次 compare 誤判 rebuild | 1.2/1.3 為必改；schema 穩定性測試把關 |
| 首次升級對既有 DB 觸發大量 ALTER | 為 D2 預期行為；CHANGELOG 標註；ALTER 為 in-place widening、無停機資料風險 |
| DateTime 欄位帶 DEFAULT/index 時 ALTER 失敗 | 沿用既有 `SqlTableAlterCommandBuilder`（已處理 default/index）；升級測試涵蓋帶 default 欄位 |
| `getdate()` 預設值在 datetime2 欄僅 ms 精度 | 隱式轉換合法；預設值精度非資料精度問題，可接受 |
| DateTime 讀入 `Scale` 影響其他 DbField.Scale 消費端 | forward DDL 對 DateTime 不讀 Scale（固定 7），define 端 Scale 仍 0；僅 Compare 讀取，影響面封閉 |

## 階段 3：SQL Server 寫入參數型別 `DateTime → DbType.DateTime2`（provider-aware，僅 SQL Server）

`DbTypeMapper.Infer` 維持 `DateTime → DbType.DateTime`（跨 provider 不動）。
改在 [DbCommandSpec.NormalizeDbType](../../src/Bee.Db/DbCommandSpec.cs#L163) 對 SQL Server 改寫：
```csharp
if (databaseType == DatabaseType.SQLServer && dbType == DbType.DateTime)
    return DbType.DateTime2;
```
與既有 Oracle `Guid → Binary` 改寫同一 provider-aware 機制。**只影響 SQL Server**，
PostgreSQL / MySQL / SQLite / Oracle 的 DateTime 參數行為與改動前完全一致（無回歸）。

### 階段 3 驗收
- SQL Server：亞毫秒（100 ns）DateTime 經參數寫入 datetime2(7) round-trip 無精度遺失。
- SQL Server：pre-1753 DateTime 經參數寫入不拋 `SqlDateTimeOverflow`。
- PostgreSQL / MySQL / Oracle / SQLite：DbType.DateTime2 參數寫入正常 round-trip（相容性驗證）。

## 影響範圍

- **SQL Server dialect**（型別對映、reverse-map、比對）：階段 1–2。
- **`DbCommandSpec.NormalizeDbType`**（provider-aware 參數改寫）：階段 3，**僅對 SQL Server** 把
  DateTime 參數升為 DateTime2。`DbTypeMapper.Infer` 不動。
- MySQL / PostgreSQL / SQLite / Oracle 的 **DDL 型別對映與 DateTime 參數行為皆不動**，零回歸。

## 驗收標準（整體）

- [ ] 新建 SQL Server 表 DateTime 欄位為 `datetime2(7)`
- [ ] 既有 `datetime` 欄位於 upgrade 時自動 ALTER 為 `datetime2(7)`，之後 compare 收斂
- [ ] 亞毫秒 round-trip 測試通過（`.001` 秒不再被 round 掉）
- [ ] < 1753 年 DateTime 寫入不拋溢位
- [ ] `dotnet build -c Release` 0 warning、`./test.sh` SQL Server 相關測試綠燈
- [ ] CHANGELOG 標註「首次升級會將既有 datetime 欄位遷移為 datetime2」
