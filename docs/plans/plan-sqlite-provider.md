# 計畫：Bee.Db 加入 SQLite Provider（第三 RDBMS 支援）

**狀態：🚧 進行中（2026-04-27 起）**

> **先決條件已解**：[`plan-postgresql-provider.md`](archive/plan-postgresql-provider.md) 已於 2026-04-27 完成，dialect factory 抽象層經 SQL Server + PostgreSQL 兩 provider 驗證穩定；測試基礎設施（`[DbFact(DatabaseType)]` 參數化、`DbGlobalFixture` 多 DB 容錯、env var 命名 `BEE_TEST_CONNSTR_{DBTYPE}`）已支援任意 DB 擴增，本計畫不需修改 infra。

## 背景

`DatabaseType` 列舉有 SQLServer / MySQL / SQLite / Oracle / PostgreSQL 五種；目前 SQL Server 與 PostgreSQL 已完整實作，其餘三種僅在 `DbFunc.DbParameterPrefixes` / `QuoteIdentifiers` 有預先註冊。

本計畫加入 **SQLite** 作為第三 RDBMS provider，目標支援**檔案式單機 / 嵌入式應用**情境（小程式、桌面工具、測試替代）。SQLite 不適用 ERP 高並發 production，但對小工具與本機測試非常有用。

## 目標

1. `DatabaseType.SQLite` 可建立連線、執行 CRUD、DDL、Schema 比對升級
2. SQL Server / PostgreSQL 完全不變
3. CI 與本機可跑 SQLite 整合測試（不需 service container，純驅動）
4. SQLite 特有的 DDL 限制有明確且可預期的策略

### 不涵蓋

- SQLite 特有能力（FTS5 全文、JSON1、虛擬表、Window function、RTREE 等）
- SQLite extension 載入機制
- 跨 DB 資料遷移工具
- 高並發寫入優化（這是 SQLite 本質限制，不在框架層處理）

## SQLite 與其他 provider 的關鍵差異

### 1. ALTER TABLE 嚴重受限

| 變更 | SQL Server / PG | SQLite |
|------|----------------|--------|
| `ADD COLUMN` | ✅ | ✅ |
| `RENAME COLUMN` | ✅ | ✅（3.25+） |
| `DROP COLUMN` | ✅ | ✅（3.35+） |
| `ALTER COLUMN TYPE` | ✅ | ❌ |
| 變更 nullability | ✅ | ❌ |
| 變更 default | ✅ | ❌ |
| 變更 PK | ✅ | ❌ |
| `RENAME TO`（表） | ✅ | ✅ |

**策略**：`SqliteAlterCompatibilityRules` 把絕大多數欄位修改判為 `Rebuild`；只有 `AddFieldChange` / `RenameFieldChange` / `AddIndexChange` / `DropIndexChange` 走 `Alter`，所有 `AlterFieldChange` 一律 `Rebuild`。這是 provider-specific 的決策，框架的 ALTER-rebuild 策略本來就準備好這條 fallback 路。

### 2. `INTEGER PRIMARY KEY AUTOINCREMENT` 是內聯特例

`AUTOINCREMENT` 必須**直接寫在欄位定義**裡，不能透過外部 `CONSTRAINT pk_xxx PRIMARY KEY (...)` 子句達成：

```sql
-- ✅ 正確
CREATE TABLE "t" ("id" INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL, ...);

-- ❌ 不能這樣寫
CREATE TABLE "t" ("id" INTEGER NOT NULL, CONSTRAINT "pk_t" PRIMARY KEY ("id"));
-- AUTOINCREMENT 無法附在外部 PK 上
```

**策略**：`SqliteCreateTableCommandBuilder` 偵測到欄位 `DbType=AutoIncrement` 時：
- 該欄位內聯為 `INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL`
- 跳過外部的 `CONSTRAINT ... PRIMARY KEY (...)` 行（PG/SQL Server 都會有）
- 衝突檢測：若 schema 同時有 AutoIncrement 欄位**和**指向其他欄位的 PK 索引，視為定義錯誤，拋 `InvalidOperationException`

### 3. 沒有原生 COMMENT ON

SQLite 沒有 `COMMENT ON TABLE` / `COMMENT ON COLUMN`，也沒有 `sp_addextendedproperty` 等價物。

**策略**：第一階段 **silent no-op** —— `SqliteCreateTableCommandBuilder` 不產生任何 description 語句，`SqliteTableSchemaProvider` 讀回 DisplayName / Caption 永遠為空字串。記為已知限制；若日後有需求，可考慮 sidecar metadata 表（`_bee_metadata`），但本計畫不做。

### 4. 型別系統採 TYPE AFFINITY

SQLite 沒有嚴格型別，只有 5 個 storage class（NULL / INTEGER / REAL / TEXT / BLOB），但**儲存宣告型別字串**並用 affinity 規則對應。

**策略**：`SqliteTypeMapping` 仍宣告完整型別字串（如 `VARCHAR(50)`、`BOOLEAN`、`DATETIME`、`UUID`），SQLite 會原樣儲存；`SqliteTableSchemaProvider` 讀 `PRAGMA table_info` 拿到的 `type` 欄做反向映射。

| `FieldDbType` | SQLite 宣告型別 | 實際 affinity |
|--------------|----------------|---------------|
| `String` | `VARCHAR(n)` | TEXT |
| `Text` | `TEXT` | TEXT |
| `Boolean` | `BOOLEAN` | NUMERIC（儲存 0/1） |
| `Short` / `Integer` / `Long` / `AutoIncrement` | `SMALLINT` / `INTEGER` / `BIGINT` / `INTEGER` | INTEGER |
| `Decimal` | `NUMERIC(p,s)` | NUMERIC |
| `Currency` | `NUMERIC(19,4)` | NUMERIC |
| `Date` / `DateTime` | `DATE` / `DATETIME` | NUMERIC（推薦 ISO 8601 字串） |
| `Guid` | `UUID` | NUMERIC（實際 TEXT） |
| `Binary` | `BLOB` | BLOB |

### 5. 沒有 schema 概念

SQLite 不分 schema（沒有 `public` / `dbo`）。所有表在 `main` 資料庫，identifier 直接 unqualified。

## Schema 讀取

| 用途 | SQLite 來源 |
|------|------------|
| 表存在性 | `SELECT count(*) FROM sqlite_master WHERE type='table' AND name={0}` |
| 欄位清單 | `PRAGMA table_info(<table>)` — 回傳 cid / name / type / notnull / dflt_value / pk |
| 索引清單 | `PRAGMA index_list(<table>)` — 回傳 seq / name / unique / origin / partial |
| 索引欄位 | `PRAGMA index_info(<index>)` — 回傳 seqno / cid / name |

**特別說明**：`PRAGMA index_list` 的 `origin` 欄位區分索引來源（`pk` / `u` / `c`）。`origin='pk'` 表示由主鍵約束自動建立的索引，`origin='c'` 是 CREATE INDEX。`SqliteTableSchemaProvider.GetTableIndexes` 走 PRAGMA + 後處理組合而非單一 SQL 查詢。

## 連線字串與測試環境

### 連線字串範本

```
Data Source={@DbName}.db
```

或 in-memory shared cache（多 connection 共用同一資料庫）：

```
Data Source={@DbName};Mode=Memory;Cache=Shared
```

### 測試 fixture

`tests/Bee.Tests.Shared/GlobalFixture.cs` 加入 `RegisterSqlite()`：

- 偵測 `BEE_TEST_CONNSTR_SQLITE`，未設則跳過
- 註冊 `DbProviderManager.RegisterProvider(DatabaseType.SQLite, Microsoft.Data.Sqlite.SqliteFactory.Instance)`
- 註冊 `DbDialectRegistry.Register(DatabaseType.SQLite, new SqliteDialectFactory())`
- 註冊 `DatabaseItem(Id="common_sqlite", DatabaseType=SQLite, ConnectionString=connStr)`

`DbGlobalFixture.GetSeedExpressions` 加 SQLite case：

- UUID：`lower(hex(randomblob(4)) || '-' || hex(randomblob(2)) || '-4' || substr(hex(randomblob(2)), 2) || '-' || substr('89ab', 1 + (abs(random()) % 4), 1) || substr(hex(randomblob(2)), 2) || '-' || hex(randomblob(6)))` — 模擬 v4 UUID
- Now：`datetime('now')` 或 `CURRENT_TIMESTAMP`

簡化版（夠用）：`hex(randomblob(16))` 為 UUID（不是 v4 但唯一性足夠）；`CURRENT_TIMESTAMP` 為 now。

### CI

`build-ci.yml` **不需新 service container**（SQLite 是純驅動）。只需在 Test step 注入：

```yaml
BEE_TEST_CONNSTR_SQLITE: "Data Source=/tmp/bee_test.db"
```

或更乾淨：

```yaml
BEE_TEST_CONNSTR_SQLITE: "Data Source=file:bee_ci?mode=memory&cache=shared"
```

## 實作步驟（PR 切分）

| PR | 範圍 | 預估規模 |
|----|------|---------|
| **PR S1** | `Providers/Sqlite/` 骨架：`SqliteDialectFactory` 五個方法都 throw `NotImplementedException`、`SqliteSchemaHelper`、`SqliteTypeMapping` | 小（~150 LOC） |
| **PR S2** | `SqliteFormCommandBuilder` + 5 tests（與 PR 4 對稱） | 小 |
| **PR S3** | `SqliteCreateTableCommandBuilder` + tests（含 AutoIncrement 內聯 PK 處理 + 衝突檢測 + COMMENT no-op） | 中（~400 LOC + ~400 test LOC） |
| **PR S4** | `SqliteAlterCompatibilityRules` + `SqliteTableAlterCommandBuilder` + `SqliteTableRebuildCommandBuilder` + tests（限制路徑為主） | 中 |
| **PR S5** | `SqliteTableSchemaProvider`（PRAGMA queries）+ `Microsoft.Data.Sqlite` PackageReference + `GlobalFixture` 註冊 + 整合測試（含 SQLite 限制驗證） + CI 注入連線字串 | 中大 |
| **PR S6** | 文件更新：README（雙語）SQLite 區段 + 限制清單；architecture-overview 列入 SQLite | 小 |

PR S1–S6 各自獨立可 merge；S5 起整合測試開跑。

## 已知風險

1. **AutoIncrement + 顯式 PK 衝突**：FormSchema 若同時設 AutoIncrement 欄位與另一個 PK 索引（例如 GUID rowid），SQLite 不支援；`SqliteCreateTableCommandBuilder` 應拋 `InvalidOperationException`。需要在 unit test 涵蓋此分支。
2. **Description 流失**：DisplayName / Caption 在 SQLite 不被持久化。若應用層 expectations 是 round-trip，需另案處理（sidecar table），不在本計畫範圍。
3. **Migration 路徑代價**：因為大多數欄位修改要走 rebuild，SQLite 上的 schema upgrade 比 SQL Server / PG 多複製整表的 IO；對小資料量的「小程式」不是問題。
4. **`Microsoft.Data.Sqlite` 平台相依**：底層需要 SQLitePCLRaw bundle（含 native binary）；CI 上 ubuntu-latest + .NET 10 已內建支援。

## 完成定義（DoD）

- [ ] `Providers/Sqlite/` 完整實作（DialectFactory、Form、CreateTable、TableAlter、TableRebuild、TableSchema、TypeMapping、SchemaHelper、AlterCompatibilityRules）
- [ ] `Bee.Db.csproj` 維持零 driver 相依（`Microsoft.Data.Sqlite` 只在 `Bee.Tests.Shared`）
- [ ] `DbAccess` 可在 SQLite 上執行 CRUD / DDL
- [ ] `TableSchemaBuilder` + `TableUpgradeOrchestrator` 在 SQLite 上能比對並走 alter / rebuild 正確路徑（rebuild 為主）
- [ ] CI `build-ci.yml` 跑 `[DbFact(DatabaseType.SQLite)]` 測試
- [ ] `Bee.Db/README.md`（雙語）與 `docs/architecture-overview.md`（雙語）反映 SQLite 支援
- [ ] DDL 限制（ALTER 受限、無 COMMENT、AutoIncrement 內聯）在文件明確記載

## 設計決策（已與使用者確認）

1. ~~**AutoIncrement + 顯式 PK 衝突的處理**~~
   - ✅ **A. 拋 `InvalidOperationException`**。Fail-fast、語意保真。
   - 觸發條件：AutoIncrement 欄位存在 **AND**（PK 指向其他欄位 / 沒有 PK / 多個 AutoIncrement 欄位）。
   - 訊息範例：`On SQLite, AutoIncrement field 'X' must be the single-column primary key. Either restructure the schema or use FieldDbType.Integer instead.`
   - 既有 `st_user` / `st_session` 採「AutoIncrement = PK」模式，不會觸發例外。
2. ~~**Description 流失策略**~~
   - ✅ **A. Silent no-op**。SQLite 不持久化 `DisplayName` / `Caption`；應用層從 FormSchema XML 取得。
   - 實作上 `IDialectFactory` 增加 `SupportsDescriptionPersistence` capability flag（SQL Server / PG = `true`，SQLite = `false`），`TableUpgradeOrchestrator` 的 description 階段對 SQLite 跳過，避免 Comparer 每次都觸發無意義 drift。
3. ~~**In-memory vs file**~~
   - ✅ **B. In-memory shared cache**。`Data Source=file:bee_test_sqlite?mode=memory&cache=shared` —— 零 IO、零 cleanup、CI / 本機一致。
   - 本機若需 debug 落地，自行修改 `.runsettings` 改成 `Data Source=...db` 即可。
4. ~~**`Microsoft.Data.Sqlite` 版本**~~
   - ✅ **A. 鎖定 `9.0.4`**（與 Npgsql 9.0.4 同主版本）。`Bee.Db.csproj` **零 driver 相依**，driver 僅引用於 `tests/Bee.Tests.Shared`。

### PK 索引名稱 round-trip（次要實作議題）

SQLite 為 PK 自動產生索引名 `sqlite_autoindex_*`，與 schema 定義的 `pk_xxx` 不同。在 PR S5 處理：

- 優先：確認 `TableSchemaComparer` 對 PK 索引以 `PrimaryKey=true` 旗標匹配（不比對名稱）—— 若 SQL Server / PG 已是這樣，SQLite 自動受惠
- 次選：`SqliteTableSchemaProvider.ParsePrimaryKey` 把 `sqlite_autoindex_*` 正規化為 schema 期望的 PK 名稱

## 參考

- [SQLite ALTER TABLE limits](https://www.sqlite.org/lang_altertable.html)
- [SQLite Type Affinity](https://www.sqlite.org/datatype3.html)
- [Microsoft.Data.Sqlite 文件](https://learn.microsoft.com/dotnet/standard/data/sqlite/)
- [`docs/plans/archive/plan-postgresql-provider.md`](archive/plan-postgresql-provider.md) — 第二 provider 完整實作參考
