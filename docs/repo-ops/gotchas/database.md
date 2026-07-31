# 踩雷誌：資料庫與 provider 差異

對應硬規則見 `.claude/rules/database.md`。本檔記症狀、根因與推導過程。

## Oracle：`''` == `NULL`，讓「常態為空」的 String NOT NULL 欄炸掉

**症狀**：本機 `./test.sh` 全綠、CI 掛，且**只有 Oracle** 爆 `ORA-01400`（cannot insert NULL）。

**根因**：Oracle 沒有「非 null 的空字串」。框架對 Oracle String `AllowNull=false` 生成的
`VARCHAR2(n) DEFAULT '' NOT NULL` 是**自相矛盾**的（`DEFAULT ''` 即 `DEFAULT NULL`，與 `NOT NULL`
衝突）。業務值總是非空的欄（`sys_id`、`sys_name`）不踩雷，因為所有 INSERT 都給非空值、從不依賴
default；**「常態值為空」的欄**（如多租戶 `customize_id`）在 **fresh CREATE TABLE** 下，省略或給
空字串的 INSERT 一律失敗。

**為何本機不重現**：本機持久容器對既有表走 **ALTER ADD**，而 ALTER ADD 對既有資料表加欄會強制
nullable —— 欄根本沒有 NOT NULL 約束。CI 每次 fresh CREATE 才走到真正的定義。

**正解（規劃中）**：修 Oracle dialect —— String/VARCHAR2 欄不論 `AllowNull` 一律建 nullable、
不加 `DEFAULT ''`（Oracle 就是以 NULL 表示空字串），讀取端 `ValueUtilities.CStr(null)→""` 已讓
上層只見空字串。這是成套變更（DDL 生成 + schema diff 的反覆-ALTER 風險 + 10+ Oracle DDL 測試）。
**在該修正落地前**，「常態為空且需 Oracle」的 String 欄暫用 `AllowNull="true"` 過渡。

## MySQL：`TEXT` 不能有 DEFAULT → 省略該欄的 INSERT 直接失敗

**症狀**：只有 MySQL 爆 `Field 'x' doesn't have a default value`（strict mode）。

**根因**：`DbType="Text"` 的 NOT NULL 欄在 MySQL 上**無法**有 `DEFAULT ''`（TEXT/BLOB 語法限制）。
框架 `MySqlSchemaSyntax.GetDefaultExpression` **已正確處理**——Text 型 `AllowNull=false` 一律
**不輸出 DEFAULT**（欄仍 NOT NULL）。其他 dialect 給 NOT NULL 字串隱式空字串預設、MySQL TEXT 沒有。

框架自身的 CRUD / seed INSERT 含全欄故不踩；**踩的是 hand-written 原生 SQL**（測試 helper、
`SharedDatabaseState` seed）。

**正解 = 補齊 INSERT，不是改 nullable。** 2026-07-01 `st_company.number_formats_xml`（Text）中此雷，
**一度誤改 `AllowNull="true"`——使用者否決**（違反 NOT NULL 設計原則）。正解是欄維持 NOT NULL、
把所有 hand-written `st_company` INSERT 補上 `number_formats_xml=''`（seed + 4 個測試 helper）。

要在本機重現 CI 行為：`docker exec` 手動 `ALTER ... MODIFY <col> LONGTEXT NOT NULL`。

## MySQL：既有表 ALTER ADD Guid 欄被判 replication-unsafe（已修）

**症狀**：MySQL error 1592/1674；在測試裡會讓 `SharedDatabaseState` 的整段 MySQL setup 被 catch
跳過（`{dbType} setup skipped`），導致該表新欄位從沒套上、後續 INSERT 報 `Unknown column`
——**表面症狀與根因差了兩層**。

**根因**：框架對 MySQL 的 Guid 欄產生 `char(36) NOT NULL DEFAULT (UUID())`。對**既有表**下
`ALTER TABLE ... ADD COLUMN <guid> NOT NULL DEFAULT (UUID())` 在 statement-based binlog 下被視為
replication-unsafe（system function 每列值不同）。**fresh CREATE TABLE 帶 `DEFAULT (UUID())` 是安全的**
→ CI（每次全新容器）不受影響，只有本機持久容器會中。

**已修（commit `eeea3aad`）**：`MySqlTableAlterCommandBuilder` 對「預設為非確定性函式的 NOT NULL 欄」
ADD 時拆兩段：① 先以常數空 Guid 預設 `ADD COLUMN ... NOT NULL DEFAULT '00000000-...'`（safe，
既有列得 `Guid.Empty`），② 再 `ALTER COLUMN ... SET DEFAULT (UUID())`（metadata-only、不觸碰既有列，
且與 fresh CREATE schema 一致 → comparer 不漂移）。偵測條件＝解析後預設含 `UUID()`。

**殘留**：無框架層殘留。但「ALTER ADD 的跨 dialect 預設值/nullability 差異，本機與 CI 走不同路徑」
這個**模式**會重複出現——見上面 Oracle 那則。

## SQLite：GUID 是區分大小寫的 TEXT（已修，但有殘留）

**症狀**：開既有訂單新增明細，明細**有** INSERT 進 DB，但 reload 後「消失」。實際是孤兒列。

**根因**：SQLite 沒有 GUID 型別，以 **TEXT** 儲存且比對區分大小寫。本專案有多個大小寫來源——
seed / 既有資料**大寫**、`Guid.ToString()` **小寫**、Microsoft.Data.Sqlite 綁 Guid 參數用**大寫** TEXT。
client 端把 master 的 Guid `ToString()`（小寫）寫進 `sys_master_rowid` 字串欄 → 與大寫主檔不符 →
reload 的 `WHERE sys_master_rowid = '大寫'` 找不到。新訂單不踩（master+detail 都走 Guid 參數，
一致大寫），只有「開既有單再加明細」會踩。

**已根治（2026-06-15）**：SQLite GUID(`UUID`) 欄在 CREATE/ALTER 加 `COLLATE NOCASE`
（`SqliteSchemaSyntax.UsesNoCaseCollation` 把 `FieldDbType.Guid` 與 String/Text 並列）。CREATE 與
ALTER ADD 共用 `GetColumnDefinition`，一改兩路徑齊覆蓋。GUID hex 全 ASCII → NOCASE 完整覆蓋。

**殘留（三點都還在）**：

1. 設 GUID 外鍵連結時仍**複製來源原值**、勿經 `Guid.Parse/ToString` round-trip
   （`FormRowDefaults.Apply` 的 masterRowId 用 `object?` 原樣寫入）。與 COLLATE 正交互補。
2. COLLATE 只讓**比對**大小寫無關、**不正規化儲存值**；既有 SQLite 表需重建 schema 才吃到新 collation。
3. **「client 端讀回的 GUID 欄是 String 型」這件事會外溢** —— 運算式引擎的 coerce 雷就是它引起的，
   見 [serialization-and-expressions.md](serialization-and-expressions.md)。

## decimal 精度：框架不設參數 scale，DB 行為不一致

**根因**：`DbCommandSpec.CreateCommand` 只設 `Value`/`DbType`/`Size`/`IsNullable`；
`DbParameterSpec` **沒有 `Precision`/`Scale` 屬性**（scale 由 ADO.NET provider 從值本身推斷）；
`DbField.Scale` 只用於 CREATE TABLE DDL。全 repo（test 除外）寫 DB 前無任何 `Math.Round` /
`decimal.Round` / `Truncate`。

**後果**：SQL Server / PostgreSQL / MySQL / Oracle 超過 column scale → **四捨五入（非截斷）**；
**SQLite 完全不強制 scale → 原樣保留全精度**（NUMERIC affinity 不轉換）。同一筆 decimal 在
SQLite vs SQL Server 可能存出**不同精度**。

**正解**：捨入必須由 **Repository 寫入層**顯式做（CRUD 由 FormSchema/DbField 驅動，握有每欄
`DbField.Scale`）；`DbCommandSpec` 那層拿不到 column scale，掛不上去。

## datetime2：改 schema 不夠，瓶頸在參數推斷層

**症狀**：SQL Server `FieldDbType.DateTime` 的 DDL 已改成 `datetime2(7)`，但仍拿不到亞毫秒精度、
pre-1753 仍拋 `SqlDateTimeOverflow`。

**根因**：`DbParameterSpec` 是所有 provider 唯一寫入參數路徑。`DbTypeMapper.Infer` 把
`DateTime → DbType.DateTime`，SqlClient 在**送出前**就把值 round 成 ms、對 pre-1753 直接拋
——**即使欄位是 datetime2 也一樣**。

**修正過程中踩的雷（重要）**：最初想全域把 `DbTypeMapper.Infer` 改成 `DbType.DateTime2`，
結果**炸掉 PostgreSQL / Oracle 的 Northwind seed**——Npgsql 對 `Kind=Utc` 值在 DateTime2 下解析
型別改變、seed 交易 rollback → 0 rows。本機因 shared DB 有舊 seed 資料**未重現**，CI fresh 容器才炸。

**正解**：`DbTypeMapper.Infer` 維持 `DbType.DateTime`（跨 provider 不動），改在 provider-aware 的
`DbCommandSpec.NormalizeDbType` **只對 SQL Server** 把 `DateTime → DateTime2`（與既有 Oracle
`Guid → Binary` 同一機制）。既有 `datetime` 欄位會在下次 schema upgrade 自動 ALTER 為 datetime2
（comparer 靠 `sys.columns.scale` 3 vs 7 區分）。

**通則**：凡「參數層跨 provider 型別調整」一律走 `NormalizeDbType` 做 provider-gated 改寫，
別動全域 `Infer`。DateTime 參數的 driver 行為 provider 間差異極大。

## 跨 DB seed 的雜項

- 識別符一律 `dbType.QuoteIdentifier(...)`——**Oracle 會把它大寫**（`"FT_CATEGORY"`），其餘保留原樣。
- seed JSON 值皆為字串（含數字型 PK 如 order `"10248"`），**依目標欄 `FieldDbType` 轉型**，
  不可靠值猜型。
- Date 綁定用 `DateTimeKind.Utc` —— PG 的 Date 對映 `timestamptz`，Npgsql 拒收 Unspecified/Local；
  Utc 對 5 個 DB 皆安全。
- 持久 DB 若殘留舊 seed，gate 會 skip → 需手動清空該 DB 的相關表（含 gate 表本身才能重開 gate）。
