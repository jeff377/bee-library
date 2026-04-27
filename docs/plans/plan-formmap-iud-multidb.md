# 計畫：FormMap INSERT / UPDATE / DELETE 多 DB 實作

**狀態：📝 擬定中（待開工）**

> **設計原則**：`IFormCommandBuilder` 僅負責產生 SQL 字串（純函式、無副作用），執行由呼叫端（Repository / BO）負責。

## 待開工前確認決議（2026-04-27）

1. 系統欄位（`sys_insert_time` / `sys_update_user_rowid` 等）填值責任：**由 BO 填，IUD builder 不補**
2. UPDATE 無欄位變更時行為：**throw `InvalidOperationException`**。理由：builder 是純 SQL 生成器，無合法 SQL 可產時即視為呼叫端誤用；呼叫端責任是先判斷 `DataRow.RowState == Modified` 且確實有欄位變更才呼叫
3. 撰寫順序：INSERT → DELETE → UPDATE（純開發節奏，與 runtime 無關；runtime 由呼叫端決定要呼叫哪個 `Build*` 方法）
4. Bee.Repository 整合：**不含於本 plan**（後續另起）

## 背景

FormMap 的 SELECT 路徑已完整實作（SQL Server / PostgreSQL，兩 provider 委派共用核心 `SelectCommandBuilder`，方言差異透過 `DbFunc` 與 `IDialectFactory` 抽象）。

INSERT / UPDATE / DELETE 介面（既有命名 `Build{Insert,Update,Delete}Command`）已宣告但實作為 `throw new NotSupportedException()`，是 greenfield 工作。本 plan 同時將四個方法重新命名為 `Build{Select,Insert,Update,Delete}`（去掉冗餘的 `Command` 後綴）。

## 設計前提

調查階段已確認：

- `sys_rowid` 為 `DbType="Guid"`，**用戶端產生**（非 DB IDENTITY/SEQUENCE）→ 跨 DB 「server-generated identity 取回」差異不存在
- 識別子引號、參數前綴、型別差異**已由 `DbFunc` / `DbCommandSpec` / ADO.NET 處理**
- IUD SQL 結構簡單（無 JOIN、無 subquery），跨 DB 方言差異點極少

詳見 [FormMap 設計文件](../formmap.zh-TW.md) 與專案記憶 `project_rowid_client_generated.md`。

## 設計決策（已拍板）

| # | 議題 | 決議 |
|---|---|---|
| 1 | 樂觀鎖（UPDATE/DELETE WHERE 加版本檢查） | **不做**（先做 happy path；需要時另起 plan） |
| 2 | INSERT/UPDATE 方法簽章 | 吃 `DataRow`，直接從變更資料綁參數 |
| 3 | DELETE 範圍 | 支援 `FilterNode`（與 SELECT 一致）。理由：刪除 Master/Detail 時，Detail 需 `WHERE sys_master_rowid = ?` |
| 4 | 批次操作（多列 VALUES） | **不含**（批次走 BO + AnyCode） |
| 5 | UPSERT（MERGE / ON CONFLICT） | **不含**（跨 DB 差異最大，本期不開） |
| 6 | 本期 DB 範圍 | SQL Server + PostgreSQL（與既有 provider 範圍一致；SQLite 由 `plan-sqlite-provider.md` 接力） |

## 範圍

### 範圍內

- **前置：命名空間重整** `Bee.Db.Query` → `Bee.Db.Sql`（資料夾 `Query/` → `Sql/`，並把 `SelectCommandBuilder` 從 `Providers/` 歸位至 `Sql/`）— 詳見下節
- 三個共用核心 builder：`InsertCommandBuilder` / `UpdateCommandBuilder` / `DeleteCommandBuilder`
- `IFormCommandBuilder` 介面：所有四個方法重新命名（`Build{Select,Insert,Update,Delete}Command` → `Build{Select,Insert,Update,Delete}`）+ IUD 三個方法簽章調整
- SQL Server 與 PostgreSQL 兩 provider 的 IUD 實作（委派共用核心）
- 對應單元測試（`Bee.Db.UnitTests`）+ DB 整合測試（`[DbFact]`）
- 文件更新：[FormMap 設計文件](../formmap.zh-TW.md) 第 5 節「實作對應」移除 IUD 待開發提示

### 範圍外

- 樂觀鎖、批次操作、UPSERT（已決議排除）
- SQLite / MySQL / Oracle 的 IUD 實作（後續 plan）
- IUD 對 RelationField 的支援（Insert/Update 只動 master table 自身欄位，跨 FormSchema 寫入不在範圍）
- IUD 的 FilterNode 跨表 / RelationField 條件（DELETE 只支援目標表自身欄位的條件，避免 DELETE-with-JOIN 跨 DB 差異）
- DDL 命令 builder（`ICreateTableCommandBuilder` / `ITableAlterCommandBuilder` / `ITableRebuildCommandBuilder` / `ITableSchemaProvider`）保留在 `Providers/` 不動 — 屬於另一個 dialect 抽象範疇

## 命名空間重整（前置作業）

**目的**：`Query/` 名稱暗示「讀取」，加入 IUD 後語意不再準確。改為 `Sql/` 涵蓋所有 SQL 命令組裝，並順便把 provider-neutral 的 `SelectCommandBuilder` 從 `Providers/` 移到正確位置。

**改動清單**：

| 變動 | 檔案 |
|---|---|
| 資料夾搬移 | `src/Bee.Db/Query/` → `src/Bee.Db/Sql/`（17 檔） |
| Namespace 變更 | `Bee.Db.Query` → `Bee.Db.Sql`（17 檔內部） |
| 檔案移動 | `src/Bee.Db/Providers/SelectCommandBuilder.cs` → `src/Bee.Db/Sql/SelectCommandBuilder.cs` |
| Namespace 變更 | `Bee.Db.Providers` → `Bee.Db.Sql`（SelectCommandBuilder） |
| Using 更新（src） | `Providers/SqlServer/SqlFormCommandBuilder.cs`、`Providers/PostgreSql/PgFormCommandBuilder.cs` 等內部引用 |
| Using 更新（tests） | `tests/Bee.Db.UnitTests/` 內 6+ 個檔案 |
| 測試資料夾搬移 | `tests/Bee.Db.UnitTests/Query/` → `tests/Bee.Db.UnitTests/Sql/`（依 `code-style.md` 資料夾與 namespace 一致原則） |

**外部依賴**：經掃描，`Bee.Db.Query` namespace 僅被 Bee.Db 內部與 tests 引用，**無下游套件依賴**（`Bee.Repository` / `Bee.Business` / `Bee.Api.*` 等皆未 import），重整衝擊範圍可控。

**Commit 切割**：拆兩個 commit：
- Commit 1：純命名空間重整（無語意變更，編譯器即可驗證）
- Commit 2：IUD 實作

## 架構

```
重整後結構：
src/Bee.Db/Sql/                                ← 由 Query/ 改名
├── (既有 17 檔，namespace 改為 Bee.Db.Sql)
├── SelectCommandBuilder.cs                    ← 從 Providers/ 搬入
├── InsertCommandBuilder.cs                    ← 新增
├── UpdateCommandBuilder.cs                    ← 新增
└── DeleteCommandBuilder.cs                    ← 新增

修改（介面與兩 provider）：
src/Bee.Db/Providers/IFormCommandBuilder.cs              ← 介面簽章調整
src/Bee.Db/Providers/SqlServer/SqlFormCommandBuilder.cs  ← 委派共用核心 + using 更新
src/Bee.Db/Providers/PostgreSql/PgFormCommandBuilder.cs  ← 委派共用核心 + using 更新

測試：
tests/Bee.Db.UnitTests/Sql/                    ← 由 Query/ 改名
├── (既有測試檔的 using/namespace 更新)
├── InsertCommandBuilderTests.cs               ← 新增
├── UpdateCommandBuilderTests.cs               ← 新增
└── DeleteCommandBuilderTests.cs               ← 新增

tests/Bee.Db.UnitTests/FormCommandBuilderIudIntegrationTests.cs ← 新增 [DbFact] 整合測試
```

## IFormCommandBuilder 介面變更

```csharp
public interface IFormCommandBuilder
{
    // 變更前：BuildSelectCommand(...)
    // 變更後：去掉 Command 後綴
    DbCommandSpec BuildSelect(string tableName, string selectFields,
        FilterNode? filter = null, SortFieldCollection? sortFields = null);

    // 變更前：BuildInsertCommand()                    → throw NotSupportedException()
    // 變更後：吃 tableName + DataRow（與 SELECT 簽章對稱），方法名去 Command 後綴
    DbCommandSpec BuildInsert(string tableName, DataRow row);

    DbCommandSpec BuildUpdate(string tableName, DataRow row);

    // 變更前：BuildDeleteCommand()                    → throw NotSupportedException()
    // 變更後：吃 tableName + FilterNode
    DbCommandSpec BuildDelete(string tableName, FilterNode filter);
}
```

`tableName` 用於指定 FormSchema 內的目標 table（master 或 detail）。

## 各 Builder 設計概述

### InsertCommandBuilder

**輸入**：`FormSchema`、`DatabaseType`、`tableName`、`DataRow`
**輸出**：`DbCommandSpec`（INSERT SQL + 參數）

**邏輯**：

1. 取得 `FormTable` 並過濾可寫入欄位（排除 RelationField 與計算欄位）
2. 從 `DataRow` 收集**有設值且非預設**的欄位，加入欄位列表與值列表
3. 自動補上系統欄位（如 `sys_insert_time` / `sys_insert_user_rowid`）— 由 BO 上層填入或在此補預設
4. 識別子用 `DbFunc.QuoteIdentifier(databaseType, name)` 包裝
5. 參數使用 `{N}` 位置佔位符（既有 `DbCommandSpec` 慣例）

**SQL 模板**（雙 DB 一致）：

```sql
INSERT INTO {table} ({col1}, {col2}, ...) VALUES ({0}, {1}, ...)
```

### UpdateCommandBuilder

**輸入**：`FormSchema`、`DatabaseType`、`tableName`、`DataRow`
**輸出**：`DbCommandSpec`（UPDATE SQL + 參數）

**邏輯**：

1. 透過 `DataRow.RowState == Modified` 判斷有變更的欄位（比較 `Original` 與 `Current` 兩版本）
2. SET 子句僅包含**實際改變**的欄位 — 沒改的欄位不寫進 SQL，避免不必要的 UPDATE 寬度
3. PK（`sys_rowid`）排除於 SET，作為 WHERE 條件
4. 自動補 `sys_update_time` / `sys_update_user_rowid` 系統欄位（由 BO 上層管理）
5. WHERE 子句固定為 `sys_rowid = {N}`（無樂觀鎖）

**SQL 模板**：

```sql
UPDATE {table} SET {col1} = {0}, {col2} = {1}, ... WHERE {sys_rowid} = {N}
```

若 `DataRow` 沒有任何欄位變更（`RowState != Modified` 或所有欄位 Original/Current 相同）→ **throw `InvalidOperationException`**。builder 是純 SQL 生成器，沒有合法 SQL 可產時即視為呼叫端誤用。呼叫端責任：先確認有變更才呼叫 `BuildUpdate`。

### DeleteCommandBuilder

**輸入**：`FormSchema`、`DatabaseType`、`tableName`、`FilterNode`
**輸出**：`DbCommandSpec`（DELETE SQL + 參數）

**邏輯**：

1. 不接受跨表 / RelationField 條件（FilterNode 只能引用目標表自身欄位）
2. WHERE 子句重用 `WhereBuilder.Build(filter, selectContext: null, includeWhereKeyword: true)`
   - 不傳 `SelectContext` → 不做欄位重映射，直接以欄位名出現
3. 識別子加 quote

**SQL 模板**：

```sql
DELETE FROM {table} WHERE {filter-clause}
```

**典型用例**：
- 刪除主檔：`FilterCondition.Equal("sys_rowid", masterRowId)`
- 刪除明細：`FilterCondition.Equal("sys_master_rowid", masterRowId)`

## 跨 DB 處理對照

| 差異點 | 處理方式 | 狀態 |
|---|---|---|
| 識別子引號 | `DbFunc.QuoteIdentifier(databaseType, name)` | ✅ 既有 |
| 參數前綴與佔位符 | `DbCommandSpec` `{N}` + `DbFunc.GetParameterPrefix` | ✅ 既有 |
| 型別差異（DateTime / Json / Guid） | ADO.NET parameter 自動處理 | ✅ 既有 |
| NULL / 預設值 | 統一忽略未設值欄位（讓 DB 預設值生效） | 由本實作決定 |
| Boolean（SQL Server `bit` vs PG `boolean`） | ADO.NET 處理 | ✅ 既有 |
| 保留字欄位名（`user`, `order` 等） | QuoteIdentifier | ✅ 既有 |

→ 共用核心**不需要 dialect-specific 分支**；兩 provider 的 IUD 實作只是 `new InsertCommandBuilder(formSchema, DatabaseType.X).Build(...)` 一行委派。

## 撰寫順序

依「先做簡單再做複雜」的開發節奏排序，與 runtime 無關（runtime 由呼叫端決定要呼叫哪個 `Build*` 方法）。

### 順序 0：命名空間與介面命名重整（前置，純機械重構）

#### 順序 0.1：Namespace 與資料夾重整

0.1a. 搬移 `src/Bee.Db/Query/` → `src/Bee.Db/Sql/`，更新所有檔案 `namespace Bee.Db.Query` → `namespace Bee.Db.Sql`
0.1b. 搬移 `src/Bee.Db/Providers/SelectCommandBuilder.cs` → `src/Bee.Db/Sql/SelectCommandBuilder.cs`，更新其 namespace
0.1c. 更新 src 內所有 `using Bee.Db.Query;` → `using Bee.Db.Sql;`（`Providers/SqlServer/SqlFormCommandBuilder.cs`、`Providers/PostgreSql/PgFormCommandBuilder.cs`、其他內部）
0.1d. 同步更新 tests：`tests/Bee.Db.UnitTests/Query/` → `tests/Bee.Db.UnitTests/Sql/`，所有 test 檔的 `using` 與資料夾命名空間
0.1e. `dotnet build --configuration Release` + `dotnet test` 通過後，**commit**（純重整，零語意變更）

#### 順序 0.2：IFormCommandBuilder 方法去 `Command` 後綴

0.2a. 修改 `IFormCommandBuilder` 介面：`BuildSelectCommand` → `BuildSelect`，`BuildInsertCommand` → `BuildInsert`，`BuildUpdateCommand` → `BuildUpdate`，`BuildDeleteCommand` → `BuildDelete`（IUD 三個方法此時仍維持原 `throw NotSupportedException` 實作，**不調整簽章**，留待後續順序處理）
0.2b. 同步更新 SqlFormCommandBuilder.cs / PgFormCommandBuilder.cs 的方法名稱
0.2c. 同步更新 tests 內 `BuildSelectCommand` 呼叫點
0.2d. `dotnet build` + `dotnet test` 通過後，**commit**（純命名重整，零語意變更）

### 順序 1：INSERT（邏輯最單純）

1. 寫 `InsertCommandBuilder`
2. 修改 `IFormCommandBuilder.BuildInsert` 簽章（吃 `string tableName, DataRow row`）
3. SqlFormCommandBuilder / PgFormCommandBuilder 委派實作
4. 單元測試（給定 FormSchema + DataRow，比對輸出 SQL 與參數）
5. DB 整合測試 `[DbFact(DatabaseType.SQLServer)]` / `[DbFact(DatabaseType.PostgreSQL)]`
   - 流程：取現成測試表 → INSERT → SELECT 驗證 → DELETE 收尾

### 順序 2：DELETE（重用 WhereBuilder）

6. 寫 `DeleteCommandBuilder`（重用 `WhereBuilder`）
7. 簽章調整與 provider 委派
8. 單元測試 + DB 整合測試（INSERT → DELETE → SELECT 驗證）

### 順序 3：UPDATE（最複雜，需 RowState 處理）

9. 寫 `UpdateCommandBuilder`（含 `DataRow.RowState == Modified` 判斷與 Original/Current 比對；無變更則 throw `InvalidOperationException`）
10. 簽章調整與 provider 委派
11. 單元測試（涵蓋無變更 throw、單欄變更、多欄變更）
12. DB 整合測試（INSERT → UPDATE → SELECT 驗證）

### 收尾

13. 更新 [FormMap 設計文件](../formmap.zh-TW.md) 與英文版第 5 節（移除「IUD 待開發」提示）
14. 更新 [Bee.Db README.md](../../src/Bee.Db/README.md) + 繁中版「多資料庫支援」段落（IUD 從預留改為已支援）
15. 在本 plan 頂部標記 `**狀態：✅ 已完成（YYYY-MM-DD）**`

## 測試策略

### 單元測試（純邏輯，不接 DB）

- 給定 FormSchema XML + DataRow → 期望 SQL 字串與參數列
- 驗證項：欄位順序穩定、識別子正確 quote、保留字欄位 quote、參數佔位符對齊
- 雙方言各跑一份（SQL Server: `[...]`、PostgreSQL: `"..."`）

### 整合測試（`[DbFact]` 接真實 DB）

- 使用 `tests/Define/FormSchema/Employee.FormSchema.xml` 等既有 sample
- 完整 round-trip：INSERT → SELECT → UPDATE → SELECT → DELETE → SELECT(空)
- 兩 DB 各跑一份；環境變數未設則自動 skip（既有 `[DbFact]` 慣例）

## 風險與取捨

| 風險 | 緩解 |
|---|---|
| `DataRow.RowState` 判斷遺漏 edge case（如 `Detached` / 整列刪除） | 單元測試覆蓋每個 `DataRowState` 值 |
| FilterNode 包含 RelationField 時行為不明（DELETE 不應跨表） | 在 `DeleteCommandBuilder` 偵測並 throw `NotSupportedException` 並提供清楚錯誤訊息 |
| 系統欄位（`sys_insert_time` 等）填值責任歸屬不清 | 本期約定：**呼叫端（BO 層）負責填**，IUD builder 不主動補；FormMap doc 補充說明 |
| UPDATE 無變更欄位時 SQL 形狀 | **throw `InvalidOperationException`**（builder 是純 SQL 生成器，無合法 SQL 可產即視為誤用） |
| DataRow 含 RelationField（read-only 投影欄位）誤被寫入 | 過濾邏輯排除 `FieldType == FieldType.RelationField` |

## 對既有程式碼的衝擊

- **Namespace 重整**：`Bee.Db.Query` → `Bee.Db.Sql`，外部依賴掃描結果為 0 跨套件引用（僅 Bee.Db 內部 + tests），衝擊可控
- **`SelectCommandBuilder` 從 `Bee.Db.Providers` 搬至 `Bee.Db.Sql`** — 若有外部呼叫端 `using Bee.Db.Providers; new SelectCommandBuilder(...)`，需更新 using（掃描顯示無此情況）
- `IFormCommandBuilder` 三個方法簽章變更 → 屬於 breaking change，但因目前實作均 throw `NotSupportedException`，**外部呼叫端不可能依賴**，等同新增介面
- `Bee.Repository` 等下游若有預期 IUD 介面（順序 1 開工前確認）需同步調整
- `Bee.Definition.SysFields` 不需動
- DDL 命令 builder（`ICreateTableCommandBuilder` 等）保留在 `Providers/`，不因本次調整而移動

