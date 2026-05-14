# 計畫：SelectCommandBuilder 分頁支援（SQL 層 primitive）

**狀態：✅ 已完成（2026-05-15）**

## 背景

bee-library 的 `SelectCommandBuilder.Build` 是 FormSchema-driven SELECT 的單一
SQL 生成入口，目前已支援 SELECT / JOIN / WHERE / ORDER BY 跨 5 dialect。本 plan
補上「分頁子句」的支援能力 —— 加 `ILimitBuilder` / `LimitBuilder` sub-builder
與 `BuildCount` 新方法，跨 5 dialect 提供統一語意。

### 為什麼獨立成 plan

`SelectCommandBuilder` 未來會作為**所有 FormSchema-driven SELECT 場景的單一
入口**（list / master / detail），分頁能力是基礎建設，多個 plan 都會用到：

| 使用方 | 用法 |
|--------|------|
| GetList 分頁（plan-formbo-getlist-paging） | `BuildSelect(..., skip, take)` |
| Master/Details 取資料（未來 plan） | `BuildSelect(..., filter=Equal(rowid, X))`，不分頁；detail 大時可分頁 |
| 其他 FormSchema-driven 查詢 | 同上 |

把這層獨立成 plan 的好處：
- 產出獨立可用的 SQL primitive，多 plan 共用
- 複雜度集中、reviewer 視角單一（DBA / SQL 語法）
- Risk surface 限縮在 `Bee.Db`，不擾動 Business / API / Client 層

## 範圍

**範圍內**：
- `Bee.Db.Dml.LimitBuilder` 新增（含介面）
- `SelectCommandBuilder.Build` 加 `int? skip, int? take` 選擇性參數
- `SelectCommandBuilder.BuildCount` 新方法
- `IFormCommandBuilder.BuildSelect` 介面 signature 擴張
- 5 個 `*FormCommandBuilder`（SqlServer / Oracle / PostgreSQL / SQLite / MySQL）更新
- 純字串 unit test + `[DbFact]` 整合測試

**範圍外**（拆至後續 plan）：
- BO / Repository / API / Client 層串接 —— 見 `plan-formbo-getlist-paging.md`
- `PagingOptions` / `PagingInfo` 等高層型別 —— 同上
- 「分頁時 `sortFields == null` 套 `sys_no ASC`」之類 schema-aware fallback ——
  SQL 層**不**做，由上層 Repository 處理

## 設計決策（已確認）

| 決策點 | 結果 | 理由 |
|--------|------|------|
| 分頁子句的封裝層次 | 獨立 `LimitBuilder` sub-builder | 與既有 `SelectBuilder` / `FromBuilder` / `WhereBuilder` / `SortBuilder` 模式對稱 |
| 加新方法還是擴既有 | 擴 `BuildSelect` 加選擇性 `int? skip, int? take` | 同一 method 服務 paged / unpaged，未來 master/detail 重用 |
| COUNT 查詢 | 獨立 `BuildCount` 方法 | SQL 結構與 SELECT 差異夠大（無欄位選擇、無 ORDER BY、無 LIMIT） |
| `skip` / `take` 在 SQL 中的呈現 | inline int literal | `int` 型別免注入；避開 dialect 「LIMIT 子句不接受參數」的相容性地雷 |
| Sort 缺失的責任歸屬 | SQL 層**不**補 fallback；缺 ORDER BY + 分頁 → 丟原生 DB 錯誤 | 上層 Repository 已知 FormSchema，更適合決定 fallback |

## 既有架構觀察

`SelectCommandBuilder.Build` 已是 dialect-aware orchestrator，**每個 SQL 子句
都有獨立的 dialect-aware sub-builder**：

```
SelectCommandBuilder.Build(dialect)
  ├─ SelectBuilder (dialect)     → "SELECT ..."
  ├─ FromBuilder (dialect)        → "FROM ... JOIN ..."
  ├─ WhereBuilder (dialect)       → "WHERE ..." + parameters
  └─ SortBuilder (dialect)        → "ORDER BY ..."
```

分頁要遵循同樣模式：**新增 `ILimitBuilder` + `LimitBuilder`**，把 5 dialect 的
分頁語法差異全部關在這個 sub-builder 裡。`SelectCommandBuilder` 只負責「叫它、
拿字串、串到 SQL 尾巴」，不知道 dialect 細節。

## P0 探勘結果（2026-05-14）

5 題全部結論彙整於最下方「P0 探勘決策摘要」段落；下方逐題列出原問題與探勘
所得。

### 1. 5 dialect 分頁語法各自實際行為驗證 ← 最關鍵

每個 dialect 在 4 邊界 case 跑得通嗎：

| Case | SQL 構造 | 預期 |
|------|---------|------|
| `(null, null)` | 不加分頁子句 | 行為與既有 SELECT 一致 |
| `(0, 10)` | OFFSET 0 + 取 10 | 回前 10 列 |
| `(10, 10)` | OFFSET 10 + 取 10 | 回第 11–20 列 |
| `(5, null)` | OFFSET 5 + 不限 | 從第 6 列開始全取 |
| `(null, 3)` | OFFSET 0 + 取 3 | 同 `(0, 3)` |

本機 `[DbFact]` 預先試一輪，避免 P1 時被夾在「介面已改但 SQL 跑不通」的狀態。
重點驗證：
- 各 dialect 是否需特殊 sentinel（MySQL 單獨 OFFSET 不允許）
- 分頁子句固定接在 ORDER BY 後面，無其他語法位置限制

### 2. Oracle 最低支援版本確認 ← 決定 `LimitBuilder` 複雜度

- 看 `OracleFormCommandBuilder` 與 CI service container（`oracle:free` /
  `gvenzl/oracle-free` 等）的 minimum version
- 多數現代部署是 19c / 21c / 23c → 直接走 OFFSET/FETCH（plan 目前假設）
- 若需相容 11g 以下 → `LimitBuilder.BuildOffsetFetch` 大幅變複雜（subquery
  wrap-around + ROW_NUMBER()）；本 plan 範圍會擴張

### 3. `LimitBuilder` 在「inline int literal」模式下的 review 風險

- 框架其他地方都走參數化（避注入）；此處刻意 inline 是否會引起 DBA 或
  security review 困擾？
- 折衷方案：保留參數化選項 `LimitBuilder.BuildParameterized(skip, take)`，
  遇到「LIMIT 子句不接受參數」的 dialect 才 fallback inline；現階段全 inline
  因實作簡單、邊界已驗證安全

### 4. `SelectContext` / `BuildWhereClause` 能否在 count 模式重用（次要）

- 看 `SelectCommandBuilder.Build` 內部能否抽 helper 給 `BuildCount`（共用
  `GetSelectContext` + `BuildFromClause` + `BuildWhereClause`）
- 若 `BuildCount` 需 filter-only joins（不含 select/sort 的 join），看
  `SelectContextBuilder` 能否在 count 模式產生精簡 join 集合

### 5. 既有 `BuildSelect` 呼叫端編譯影響（次要）

- grep `BuildSelect(` 確認所有呼叫端在新增 default 參數後仍能編譯
  （C# default 參數源碼相容）
- 確認 5 個 `*FormCommandBuilder` 實作的呼叫者都會跟著重編譯

---

## P0 探勘決策摘要

| # | 結論 | 影響 |
|---|------|------|
| 1 | 5 dialect 分頁語法皆為官方文件級已知事實；`OFFSET/FETCH`（SQL Server / Oracle 12c+）與 `LIMIT/OFFSET`（PG/SQLite/MySQL）；MySQL 單獨 OFFSET 不允許、用 `LIMIT 18446744073709551615` sentinel | `LimitBuilder` 三分支設計確定；不需 P0 階段寫一次性試跑碼，等 P1 的 27 unit test + `[DbFact]` 整合測試自然證實 |
| 2 | Oracle 23ai (CI 用 `gvenzl/oracle-free@sha256:c9803db5...`) / 12c+（`OracleTableAlterCommandBuilder` 註解已標明本專案最低支援）；`OFFSET/FETCH` 安全可用 | 不需 ROW_NUMBER() 退路；`LimitBuilder.BuildOffsetFetch` 維持簡單版本 |
| 3 | Codebase 既有「框架結構元素 inline、使用者資料值參數化」哲學（`QuoteIdentifier`、alias、表名都 inline）；`skip/take` 是 framework-validated int，inline 與既有哲學一致 | 不需 fallback 到參數化；在 `LimitBuilder` XML doc 加註解釋此 design decision，避免日後 reviewer 改回破壞 dialect 相容 |
| 4 | **`BuildCount` 不能直接重用 `GetSelectContext`**。原因：`selectFields=string.Empty` 會在 `GetUsedFieldNames` 展開全部欄位、引發不需要的 JOIN | 在 `SelectCommandBuilder` 內加 `GetCountContext(formTable, filter)` helper，只收集 filter 涉及的 field、產生最小 JOIN 集合 |
| 5 | 全部 `BuildSelect(` 呼叫端皆用 4 位置參數呼叫（src 5 處 + Repository 1 處 + tests 20+ 處）；加 `int? skip = null, int? take = null` default 參數**源碼完全相容**，既有呼叫端無需修改 | P1 介面擴張不會擾動既有測試與 src 呼叫端 |

### 對 plan 設計細節的修正

P0 #4 的發現需要修正 `BuildCount` 的 code snippet：

```csharp
public DbCommandSpec BuildCount(
    string tableName,
    FilterNode? filter = null)
{
    if (string.IsNullOrWhiteSpace(tableName))
        throw new ArgumentException("tableName cannot be null or whitespace.", nameof(tableName));

    var formTable = _formDefine.Tables!.GetOrDefault(tableName);
    if (formTable == null)
        throw new InvalidOperationException($"Cannot find the specified table: {tableName}");

    // 只收集 filter 涉及的 field，避免展開全部欄位、產生不需要的 JOIN
    var selectContext = GetCountContext(formTable, filter);

    var sqlParts = new List<string>
    {
        "SELECT COUNT(*)",
        BuildFromClause(formTable, selectContext.Joins),
    };

    var (whereClause, parameters) = BuildWhereClause(filter, selectContext);
    if (!string.IsNullOrWhiteSpace(whereClause))
        sqlParts.Add(whereClause);

    string sql = string.Join(Environment.NewLine, sqlParts);
    return new DbCommandSpec(DbCommandKind.Scalar, sql, parameters);
}

private SelectContext GetCountContext(FormTable formTable, FilterNode? filter)
{
    var usedFieldNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    CollectFilterFields(filter, usedFieldNames);  // 既有 helper（line 184）
    return new SelectContextBuilder(formTable, usedFieldNames, _defineAccess).Build();
}
```

`CollectFilterFields` 是 `SelectCommandBuilder` 內已存在的 helper（line 184），
COUNT 路徑可以直接重用，**不需新邏輯、不需擾動 `GetUsedFieldNames`**。

## 設計細節

### `ILimitBuilder` / `LimitBuilder`

```csharp
// src/Bee.Db/Dml/ILimitBuilder.cs
namespace Bee.Db.Dml
{
    /// <summary>
    /// Defines the interface for building the SQL paging clause
    /// (LIMIT/OFFSET or OFFSET/FETCH) for the underlying dialect.
    /// </summary>
    public interface ILimitBuilder
    {
        /// <summary>
        /// Builds the dialect-specific paging clause. Returns an empty string when
        /// both <paramref name="skip"/> and <paramref name="take"/> are null.
        /// </summary>
        /// <param name="skip">Rows to skip; null means no offset.</param>
        /// <param name="take">Rows to take; null means no row limit.</param>
        string Build(int? skip, int? take);
    }
}

// src/Bee.Db/Dml/LimitBuilder.cs
namespace Bee.Db.Dml
{
    /// <summary>
    /// Builds the SQL paging clause for the underlying dialect. Inlines
    /// <c>skip</c> / <c>take</c> as integer literals because:
    /// <list type="bullet">
    /// <item>The values are framework-controlled int (no injection risk).</item>
    /// <item>Several dialects do not accept parameters in LIMIT-style clauses.</item>
    /// <item>Inlining avoids parameter-name conflicts with the WHERE clause.</item>
    /// </list>
    /// </summary>
    public sealed class LimitBuilder : ILimitBuilder
    {
        private readonly DatabaseType _databaseType;

        public LimitBuilder(DatabaseType databaseType)
        {
            _databaseType = databaseType;
        }

        /// <inheritdoc/>
        public string Build(int? skip, int? take)
        {
            if (skip == null && take == null) return string.Empty;
            if (skip is < 0) throw new ArgumentOutOfRangeException(nameof(skip));
            if (take is < 0) throw new ArgumentOutOfRangeException(nameof(take));

            return _databaseType switch
            {
                DatabaseType.SQLServer or DatabaseType.Oracle
                    => BuildOffsetFetch(skip, take),         // 12c+
                DatabaseType.PostgreSQL or DatabaseType.SQLite
                    => BuildLimitOffset(skip, take),
                DatabaseType.MySQL
                    => BuildLimitOffsetMySql(skip, take),
                _ => throw new NotSupportedException($"Paging not supported for: {_databaseType}"),
            };
        }

        // SQL Server / Oracle 12c+
        private static string BuildOffsetFetch(int? skip, int? take)
        {
            var s = skip ?? 0;
            return take.HasValue
                ? $"OFFSET {s} ROWS FETCH NEXT {take} ROWS ONLY"
                : $"OFFSET {s} ROWS";
        }

        // PostgreSQL / SQLite
        private static string BuildLimitOffset(int? skip, int? take)
        {
            if (skip == null) return $"LIMIT {take}";
            if (take == null) return $"OFFSET {skip}";
            return $"LIMIT {take} OFFSET {skip}";
        }

        // MySQL：OFFSET 不可單獨出現，需配合 LIMIT；採 UINT64_MAX sentinel 表「全取」
        private static string BuildLimitOffsetMySql(int? skip, int? take)
        {
            if (skip == null) return $"LIMIT {take}";
            if (take == null) return $"LIMIT 18446744073709551615 OFFSET {skip}";
            return $"LIMIT {take} OFFSET {skip}";
        }
    }
}
```

### `SelectCommandBuilder.Build(...)` 擴張

```csharp
public DbCommandSpec Build(
    string tableName,
    string selectFields,
    FilterNode? filter = null,
    SortFieldCollection? sortFields = null,
    int? skip = null,      // ← 新增，null = 不分頁
    int? take = null)      // ← 新增，null = 不限筆數
{
    // ... 既有 SELECT / FROM / WHERE / ORDER BY 邏輯不動 ...

    var limitClause = new LimitBuilder(_databaseType).Build(skip, take);
    if (!string.IsNullOrWhiteSpace(limitClause))
        sqlParts.Add(limitClause);

    string sql = string.Join(Environment.NewLine, sqlParts);
    return new DbCommandSpec(DbCommandKind.DataTable, sql, parameters);
}
```

> 分頁時若 `sortFields == null`，SQL 行為依 dialect：
> - SQL Server / Oracle 12c+：`OFFSET/FETCH` 強制 ORDER BY，會丟 syntax error
> - PostgreSQL / SQLite / MySQL：可省略 ORDER BY，但結果不確定
>
> 此狀況**SQL 層不替使用者擔**，丟出原生 DB 錯誤即可。上層 Repository 已負責
> 在分頁時補預設 sort（見 `plan-formbo-getlist-paging.md`）。

### `SelectCommandBuilder.BuildCount(...)` 新方法

> ⚠️ P0 #4 探勘修正：**不能**直接 `GetSelectContext(formTable, selectFields:
> string.Empty, ...)` 重用，因為空 selectFields 在 `GetUsedFieldNames` 會展開全部
> 欄位，引發不需要的 JOIN。改用新 helper `GetCountContext`，只 collect filter
> 涉及的 field。完整修正版 code 見「P0 探勘決策摘要 → 對 plan 設計細節的修正」段落。

```csharp
public DbCommandSpec BuildCount(
    string tableName,
    FilterNode? filter = null)
{
    // 同 Build 的前置檢查（tableName 非空、formTable 存在）
    var selectContext = GetCountContext(formTable, filter);  // ← 新 helper，見下方修正段落
    // SELECT COUNT(*) + FROM + JOIN + WHERE，無 ORDER BY、無 LIMIT
    return new DbCommandSpec(DbCommandKind.Scalar, sql, parameters);
}
```

> `DbCommandKind.Scalar` 表示用 `Execute(spec).Scalar` 取結果（單值）。

### `IFormCommandBuilder` 介面擴張

```csharp
public interface IFormCommandBuilder
{
    DbCommandSpec BuildSelect(         // ← 既有方法簽名擴張（加 skip/take）
        string tableName,
        string selectFields,
        FilterNode? filter = null,
        SortFieldCollection? sortFields = null,
        int? skip = null,
        int? take = null);
    DbCommandSpec BuildCount(          // ← 新增
        string tableName,
        FilterNode? filter = null);
    // 既有 Insert / Update / Delete 不動
}
```

> 介面 signature 改變對既有 5 個 `*FormCommandBuilder` 是 source-compatible
> （加 default 參數）但 binary-incompatible。同 repo 內部使用、無第三方實作，
> 全 repo 重 build 即可。

每個 `*FormCommandBuilder` 的 `BuildSelect` 都是 thin wrapper：

```csharp
public DbCommandSpec BuildSelect(string tableName, string selectFields,
    FilterNode? filter = null, SortFieldCollection? sortFields = null,
    int? skip = null, int? take = null)
{
    var builder = new SelectCommandBuilder(FormSchema, DatabaseType.<X>, _defineAccess);
    return builder.Build(tableName, selectFields, filter, sortFields, skip, take);
}

public DbCommandSpec BuildCount(string tableName, FilterNode? filter = null)
{
    var builder = new SelectCommandBuilder(FormSchema, DatabaseType.<X>, _defineAccess);
    return builder.BuildCount(tableName, filter);
}
```

## 測試計畫

### 純字串 unit test（無 DB 依賴）

`tests/Bee.Db.UnitTests/LimitBuilderTests.cs` 新增：

| Theory | InlineData | 預期輸出 |
|--------|-----------|---------|
| `Build_SqlServer_*` | (null,null) → ""；(0,10)；(10,10)；(5,null)；(null,3) | `OFFSET n ROWS FETCH NEXT m ROWS ONLY` |
| `Build_Oracle_*` | 同上 | 同上 |
| `Build_PostgreSql_*` | 同上 | `LIMIT m OFFSET n` / 單獨 LIMIT / 單獨 OFFSET |
| `Build_Sqlite_*` | 同上 | 同 PostgreSQL |
| `Build_MySql_*` | 同上 | 同上，但 (5,null) → `LIMIT 18446744073709551615 OFFSET 5` |
| `Build_NegativeSkip_Throws` | skip=-1 | `ArgumentOutOfRangeException` |
| `Build_NegativeTake_Throws` | take=-1 | `ArgumentOutOfRangeException` |

每 dialect 5 case × 5 dialect = 25 case + 2 邊界 case = **27 unit test**。
這是 P1 的 regression net 主力，不需 container 即可全綠。

### 整合測試 `[DbFact]`（依環境變數 skip）

`tests/Bee.Db.UnitTests/EmployeeBuildSelectIntegrationTests.cs` 擴充：

| 情境 | 預期 |
|------|------|
| 種 5 列 → BuildSelect skip=0 take=2 + ORDER BY sys_no ASC | 回第 1–2 列 |
| 種 5 列 → BuildSelect skip=2 take=2 + ORDER BY sys_no ASC | 回第 3–4 列 |
| 種 5 列 → BuildSelect skip=4 take=2 + ORDER BY sys_no ASC | 回最後 1 列（不滿 2） |
| 種 5 列 + filter（過濾 3 列） → BuildCount | Scalar = 3 |

每情境 5 dialect 跑一輪（SQLite + SQL Server 為主，其餘看本機 / CI container）。

### MySQL UINT64_MAX sentinel 驗證

整合測試特別跑一次「skip=2 take=null」的 MySQL `[DbFact]`，確認 MySQL 不
抱怨此 sentinel literal。

### 既有測試的回歸保護

P1 不應影響任何既有測試的行為：
- `EmployeeBuildSelectTests`（純 SQL 字串測試）：呼叫端不傳 skip/take →
  default null → `LimitBuilder.Build` 回 ""，SQL 與舊版完全相同
- `EmployeeBuildSelectIntegrationTests`（既有 5 dialect round-trip）：同上
- 5 個 `*FormCommandBuilderTests`（如 `MySqlFormCommandBuilderTests`）：同上

P1 完成後 `dotnet test` 全套通過 + 0 個既有測試需要更動。

## 階段切分（PR / commit 粒度）

整個 plan 範圍夠小、且每個檔案的改動高度相關，建議**單一 commit**：

| 動作 | 檔案數 |
|------|--------|
| 新增 `ILimitBuilder.cs` / `LimitBuilder.cs` | 2 |
| 擴 `SelectCommandBuilder.cs`（加 skip/take + 新增 BuildCount） | 1 |
| 擴 `IFormCommandBuilder.cs` 介面 | 1 |
| 更新 5 個 `*FormCommandBuilder.cs` | 5 |
| 新增 `LimitBuilderTests.cs`（27 unit test） | 1 |
| 擴 `EmployeeBuildSelectIntegrationTests.cs`（4 情境 × dialect） | 1 |
| 擴各 `*FormCommandBuilderTests.cs`（驗證 wrapper） | 5 |

合計 ~16 檔。一個 PR 就走完。

## 已知風險

1. **Oracle 12c 以下 OFFSET/FETCH 不支援** —— P0 #2 必須先確認最低支援版本；
   若需相容 11g 以下，`LimitBuilder.BuildOffsetFetch` 需重寫為 ROW_NUMBER()
   subquery 模式（plan 範圍會大幅擴張）
2. **`IFormCommandBuilder.BuildSelect` 介面 signature 改變對 binary 不相容** ——
   外部相依此介面的第三方 plugin 將需要重編譯。同 repo 內部 5 個實作會跟著
   重 build，無風險
3. **`BuildCount` 與 `Build` 共用 `SelectContext` 時的 join 集合差異** —— count
   不需要 select / sort 觸發的 joins，但 `SelectContextBuilder` 可能仍會產生
   它們。需 P0 #4 驗證；若會產生多餘 join，COUNT query 性能下降但結果正確
4. **MySQL UINT64_MAX sentinel 的可讀性** —— SQL log 出現 `LIMIT
   18446744073709551615` 看起來怪，DBA 可能會問。在 LimitBuilder 加 XML doc
   解釋；mitigation 充分
5. **inline int literal 與框架其他地方的「一律參數化」慣例不一致** —— 在
   `LimitBuilder` XML doc 解釋 inline 理由，避免日後 reviewer 改回參數化破壞
   dialect 相容

## 範圍外

- 任何上層串接（BO / Repository / API / Client）— 見
  `plan-formbo-getlist-paging.md`
- `PagingOptions` / `PagingInfo` / `DataFormListResult` 等高層型別 —— 同上
- Cursor-based pagination —— 不在本 plan 範圍
- Oracle 11g 以下相容 —— 視 P0 #2 結果決定是否擴張

## 為後續 session 提示的暖機資訊

實作時最有用的閱讀起點：

| 用途 | 檔案 |
|------|------|
| 既有 sub-builder 樣板 | `src/Bee.Db/Dml/SortBuilder.cs`（與 LimitBuilder 結構最像） |
| `SelectCommandBuilder` orchestrator | `src/Bee.Db/Dml/SelectCommandBuilder.cs` |
| 既有 `IFormCommandBuilder` 介面 | `src/Bee.Db/Dml/IFormCommandBuilder.cs` |
| 5 個 dialect 實作（SqlFormCommandBuilder 為樣板） | `src/Bee.Db/Providers/<X>/<X>FormCommandBuilder.cs` |
| Dialect-aware extension methods | `src/Bee.Db/DatabaseTypeExtensions.cs` |
| `DbCommandSpec` Scalar / DataTable kind | `src/Bee.Db/DbCommandSpec.cs` |
| 整合測試樣板 | `tests/Bee.Db.UnitTests/EmployeeBuildSelectIntegrationTests.cs` |
| 純 SQL 字串測試樣板 | `tests/Bee.Db.UnitTests/EmployeeBuildSelectTests.cs` |
| 下個依賴 plan（上層） | `docs/plans/plan-formbo-getlist-paging.md` |

Plan 載入順序：
1. 先讀本 plan 全文
2. 跑 P0 探勘 5 題（#1 / #2 / #3 為決定性問題）
3. 由 `LimitBuilder` 純字串 unit test 開始 TDD（最容易跨 dialect 對齊行為）
4. 接著擴 `SelectCommandBuilder` + 介面 + 5 個實作
5. 最後跑整合測試與既有測試 regression 驗證
