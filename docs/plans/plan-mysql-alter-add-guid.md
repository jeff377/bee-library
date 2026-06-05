# 計畫：修正 MySQL 對既有表 ALTER ADD Guid NOT NULL 欄（replication-unsafe）

**狀態：✅ 已完成（2026-06-05）**

## 問題

框架對 MySQL 的 Guid 欄產生 `char(36) NOT NULL DEFAULT (UUID())`。對**既有表**下：

```sql
ALTER TABLE t ADD COLUMN x char(36) NOT NULL DEFAULT (UUID());
```

在 statement-based binlog 下被 MySQL 視為 **replication-unsafe**（為既有列 materialize 預設時逐列呼叫 `UUID()`，replica 值會不同）→ 直接報錯（error 1592 以 ERR packet 回傳）。

在測試裡這讓 `SharedDatabaseState` 整段 MySQL setup 被 catch 跳過（`MySQL setup skipped`），該表新欄位從沒套上、後續 INSERT 報 `Unknown column`。

**關鍵**：只有「既有表 ALTER ADD」會中；**fresh CREATE TABLE 帶 `DEFAULT (UUID())` 是安全的**（CREATE 整句可確定性 binlog）。故 CI（service container 每次全新）與本機 drop 重建均正常，只有「對既有 MySQL 表新增 Guid NOT NULL 欄」會踩。發現於 record-scope Phase 1（`ft_employee.user_rowid`），見 [[mysql-alter-add-guid-unsafe]]。

## 修法（MySQL dialect ALTER ADD 拆兩段）

對「ADD 一個 NOT NULL 且預設為非確定性函式（`UUID()`）的欄」改發**兩條 statement**：

```sql
-- 1) 常數預設先 ADD（replication-safe；既有列拿到 Guid.Empty —— 對新增的 FK 欄正是「無連結」語意）
ALTER TABLE t ADD COLUMN x char(36) NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
-- 2) 還原真正預設（metadata-only、不觸碰既有列 → safe；新列才拿 UUID()，且與 fresh CREATE schema 一致，
--    comparer 不會偵測到 default 漂移而反覆升級）
ALTER TABLE t ALTER COLUMN x SET DEFAULT (UUID());
```

### 落點
- `src/Bee.Db/Providers/MySql/MySqlSchemaSyntax.cs`：`GetColumnDefinition(field)` 加 optional `defaultOverride` 參數（讓第 1 段以常數預設組欄定義，型別/COMMENT/nullability 仍由同函式產生，不漂移）。
- `src/Bee.Db/Providers/MySql/MySqlTableAlterCommandBuilder.cs`：`AddFieldChange` 的 `GetStatements` 改用 `BuildAddFieldStatements`：
  - 偵測「解析後的預設含 `UUID()`」（非確定性）→ 回傳上述 2 條；
  - 否則維持原單條 `ADD COLUMN`（String/Integer/可為 NULL 的 Guid 等不受影響）。

### 不動
- 其他方言（SQL Server / PostgreSQL / Oracle）的 ALTER ADD —— 它們對 uuid 類預設的處理不同、既有 5 DB ALTER 已正常，不改。
- fresh CREATE TABLE 路徑 —— 本來就安全，不動。
- 既有表已有資料時的「常態為空 String NOT NULL」Oracle 雷（[[tableschema-addcolumn-allownull]]）為另一議題，不在此 plan。

## 測試

| 層 | 測什麼 |
|----|--------|
| SQL 字串（`MySqlTableAlterCommandBuilderTests`） | Guid NOT NULL 的 `AddFieldChange` → 產生 2 條（常數預設 ADD + `SET DEFAULT (UUID())`），且 ADD 不含 `(UUID())`；nullable Guid / 非 Guid → 仍單條（回歸不變） |
| 經驗證（docker exec） | 對 MySQL 既有表實跑這 2 條,確認 MySQL **接受**（不再 1592） |
| 5 DB 回歸 | `TableSchemaBuilderTests` / repository round-trip 全綠（確認 fresh CREATE 與其他方言不受影響） |

## 驗收
- MySQL 既有表能成功 ALTER ADD Guid NOT NULL 欄（不再被 setup skipped）。
- SQL 層測試斷言 2-step；其他型別單條不變。
- build 0w/0e；本機 5 DB 全綠 + CI passed。
