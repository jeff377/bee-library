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

## `DefaultValue` 寫成該型別的內建預設值 → schema 比對永遠判定「需升級」

**症狀**：新增一個 `DbType="Boolean" DefaultValue="0"` 的欄之後，`TableSchemaBuilder` 對該表
**永遠**回 `DbUpgradeAction.Upgrade`，`GetCommandText` 永遠吐一段 drop-then-add default constraint
的 SQL——即使升級剛跑完、DB 裡的欄位和 default 都完全正確。三個「結構已同步應回 None / 空字串 /
false」的測試（`TableSchemaBuilderTests`）因此連續失敗。

**根因**：讀回端會把「等於內建預設」的 default **正規化成空字串**。
`SqlTableSchemaProvider.ParseDBDefaultValue` 先把 SQL Server 存的 `((0))` 剝成 `0`，接著比對
`SqlSchemaSyntax.GetDefaultValueExpression(FieldDbType.Boolean)`——也是 `"0"`——相同就回
`string.Empty`。於是 DB 側讀出 `""`，定義側是 `"0"`，
`SqlTableAlterCommandBuilder` 的 `IsEquals(oldField.DefaultValue, newField.DefaultValue)`
永遠不成立，每次比對都要求重建 constraint。

**為何 `st_api_key` 不踩**：它的 `enabled` / `key_type` 都是 `DefaultValue="1"`，
不等於內建的 `"0"`，所以讀回端原樣保留 `"1"`，兩側一致。**只有「顯式寫出的值剛好等於內建預設」
才會撞上**——數值型與 Boolean 的 `0`、String 的空字串。

**正解**：**不要顯式寫 `DefaultValue="0"`**。內建預設本來就是 0，DDL 生成端
（`SqlSchemaSyntax`，其他 provider 同構）在 `DefaultValue` 為空時就會輸出 `DEFAULT (0)`，
CREATE 與 ALTER ADD 都涵蓋，既有列一樣會被填 0。省略它得到的 DDL 完全相同，卻不會製造這個
永久 diff。

> 更根本的修法是讓比對兩側都做同一套正規化，但那會動到全部 5 個 provider 的 diff 行為；
> 在那之前，加欄時記得「預設值等於內建值就別寫」。

> **同一族的症狀在本檔還有兩則** —— Oracle 的可空性投射（見上）與 SQLite 的描述差異（見下）。
> 共通形狀是**「定義側寫得出、該 dialect 側存不進或讀不回」**，於是兩側永遠對不上。
> 這類差異不是「還沒做」，是**不具可比性**，正解一律是讓比對器不要拿它來判差異。

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

## MySQL：`MODIFY COLUMN` 會整段換掉欄位定義 —— 漏 `AUTO_INCREMENT` 就把自增拔掉（已修）

**症狀**：MySQL `Field 'sys_no' doesn't have a default value`，出現在毫不相干的 INSERT 測試上
（`EmployeeBuildSelectIntegrationTests` 一次 6 個）。**而且第二輪重跑就全綠** ——
極像 flaky，實際不是。

**根因**：MySQL 的 `ALTER TABLE ... MODIFY COLUMN` 是**整段替換**，片段裡沒寫的東西一律消失。
`MySqlSchemaSyntax.GetColumnDefinition` 產生的是 `type + nullability + default + comment`，
**不含 `AUTO_INCREMENT`**（那在 `GetAutoIncrementColumnDefinition` 裡）。拿它對 identity 欄下
MODIFY，欄位就變成純 `BIGINT NOT NULL` 且無預設值，之後每次 INSERT 都失敗。

**為何第二輪會自己好**：下一輪比對讀回的 `sys_no` 已不是 AutoIncrement，
`AlterCompatibilityRules` 把 AutoIncrement 的型別家族變動判為 **Rebuild**，整張表以
`CREATE TABLE` 重建 → 自增與 comment 都回來了。**「重跑就過」在這裡是破壞已被掩蓋，不是 flaky。**

**觸發路徑**：description sync 落地 MySQL 時，對「caption 漂移但沒有結構異動」的欄位下
MODIFY COLUMN 補 `COMMENT`。該欄剛好是 `sys_no` 就中。ALTER 路徑本身碰不到 ——
AutoIncrement 的變動一律被路由到 Rebuild。

**已修**：新增 `MySqlSchemaSyntax.GetModifyColumnDefinition`，AutoIncrement 欄改輸出
`BIGINT NOT NULL AUTO_INCREMENT COMMENT '...'`（不帶 `PRIMARY KEY`，表上已經有了），
`MySqlDescriptionSyncCommandBuilder` 與 `MySqlTableAlterCommandBuilder` 兩處都改走它。

**通則**：**只有 MySQL 把描述存在欄位定義裡**，所以只有它的描述同步得下 `MODIFY COLUMN`
這種破壞性語句；Oracle / PostgreSQL 的 `COMMENT ON` 與 SQL Server 的 extended property
都是純 metadata、動不到欄位定義。往 MySQL 加任何「只是想改個附屬屬性」的 MODIFY 之前，
先確認片段有沒有把 `AUTO_INCREMENT`、`GENERATED`、`ON UPDATE` 這類子句一起帶上。

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

## SQLite：沒有 COMMENT 機制 → 描述差異永遠清不掉（已修）

**症狀**：沒有錯誤、沒有 SQL、沒有任何徵兆。只有在問「這張表升級完了嗎」時才看得出來 ——
只要 TableSchema 有任何 caption，`CompareToDiff` 就永遠回報有差異，`Plan` 永遠回傳
**零 stage 的 `Alter`**，`UpgradeExecutionMode.NoChange` 永遠不會出現。

**根因**：SQLite 沒有 `COMMENT ON`、也沒有欄位註解欄位，`SqliteTableSchemaProvider`
因此把每個 `Caption` 一律讀回**空字串**（`Caption = string.Empty`，寫死的）。
比對器的保守政策是「define 有值、real 沒有 → 算一筆 `DescriptionChange`」，
而 `TableSchemaDiff.IsEmpty` 把 `DescriptionChanges` 算進去 —— 於是差異必然存在、
且沒有任何語句能消除它。

**為何不是「補一個 SQLite 的 description sync builder」就好**：那個 builder 產不出東西。
這不是「還沒接」，是**該 dialect 根本無處可寫**。給它一個回傳空清單的 builder，
差異照樣留在 diff 裡，`IsEmpty` 照樣是 false。

**已修**：`TableSchemaComparer.PopulateDescriptionChanges` 在 `DatabaseType.SQLite` 直接
return —— 無法持久化的東西就不該被列為差異。這與 Oracle 的 `NormalizeNullability` 同一個
思路：**該 dialect 控制不了的屬性，兩側不具可比性，不要拿來判差異**。

**判別法**：新增任何「定義有、但某個 dialect 存不進資料庫」的中繼資料時，先問
**這個差異有沒有任何語句能消除它？** 沒有就不該讓它進 diff，否則它會靜靜地把該表
永久釘在「未同步」狀態 —— 不報錯，只是永遠答否。

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

## Oracle：參數以「位置」綁定，佔位符寫錯順序就綁到別的欄位（已修）

**症狀**：Oracle 上 `ORA-00932: 表示式 (:1) 為 TIMESTAMP 資料類型, 與預期的資料類型 BINARY 不相容`。
其餘四家同一句 SQL 完全正常。壓測時的外顯是「`GetList` 100% 失敗」，但錯的不是 `GetList`
—— 那句 SELECT 一個 bind 變數都沒有，是登入路徑先炸、VU pool 快取了 faulted task。

**根因**：`Oracle.ManagedDataAccess` 的 `OracleCommand.BindByName` 預設 `false`
——SQL 裡第 n 個 bind 變數拿到參數集合的第 n 筆，**與名稱無關**。
其餘四家一律以名稱綁定，而 `{0}` / `{Name}` 佔位符 API 的語意就是以名稱對應。
`SessionRepository.UpdateSession` 的佔位符順序是 `{1} {2} {0}`，於是
`DateTime` 被送進 `access_token`（`RAW(16)`）。

**為什麼型別相容時更可怕**：兩個都是字串欄的錯位**不會有任何錯誤**，只會寫錯欄位。
`ORA-00932` 是運氣好才炸出來的。

**正解**：`DbCommandSpec.CreateCommand` 對 Oracle 的 text command 設 `BindByName = true`
（反射設定，`Bee.Db` 不參考任何 ADO.NET driver）。**不要逐句改寫 SQL 遷就位置綁定**
——那要求每個寫 SQL 的人記住一條沒有機制檢查的 Oracle 專屬規則。
閘門是 `tests/Bee.Db.UnitTests/ParameterBindingOrderTests.cs`（五家 provider 各兩支）。

## Oracle：`RAW(16)` 讀回來是 `byte[]`，`is Guid` 一律判 false（已修，但有殘留）

**症狀**：`Cannot coerce value of type 'System.Byte[]' into Guid`（`GetData` / `Save`）；
或更安靜的版本 —— 查得到列、拿得到值，但每個 `is Guid` 分支都走 else，於是
「既有資料看起來不存在」（壓測工具的 `ResolveRowId` 因此讓第二次 `prepare` 重插而撞唯一鍵）。

**根因**：Oracle 沒有 UUID 型別，`FieldDbType.Guid` 對映 `RAW(16)`。**寫入端早就處理了**
（`DbCommandSpec.NormalizeParameterValue` 轉 `byte[]`），讀取端各自為政。

**正解**：轉型一律走 `ValueUtilities.CGuid(object)` —— 它**本來就認 16-byte 陣列**。
`DataFormRepository` 會漏是因為自帶了一份平行實作（`TryCoerceToGuid`）。
FormSchema 驅動的結果表另在 `MarkFromSchema` 就地把宣告為 Guid 卻裝 `byte[]` 的欄位換成
真正的 Guid 欄位 —— 否則消費端拿到的是「宣告 Guid、實際 byte[]」的 DataTable。

**殘留**：`FormDataGuard`、各 UI head 的 grid（`GridControl.Cells`、`DynamicGrid`、`ListView`）
仍是裸 `is Guid`。經 `MarkFromSchema` 的資料沒問題，其他來源未查證。

## SQLite：日期欄讀回來是 `string`，`is DateTime` 一律判 false（已修，但有殘留）

**症狀**：`ApiKeyRepository.GetEnabledById` 在 SQLite 上把有到期時間的金鑰讀成
`ExpiredAt = null` —— 沒有例外、沒有警告，只是**到期時間憑空消失**，於是
`ApiKeyInfo.IsExpired` 永遠回 false，已過期的金鑰照樣通行。

**根因**：SQLite 沒有日期型別，欄位以 TEXT 存放，`Microsoft.Data.Sqlite` 在沒有 schema
可依循的臨機查詢下把它交回成 `string`。`expiredAt is DateTime dt ? dt : null` 於是走 else。
與上一則的 Oracle `RAW(16)` 是同一個形狀的錯誤：**驅動交回的 CLR 型別不是宣告型別，
而裸 `is T` 把「型別不符」和「值不存在」壓成同一個答案**。

**正解**：轉型走 `ValueUtilities.CDateTime(object?)`（回 `DateTime?`，認 `DBNull`、
空字串與可剖析的字串）。FormSchema 驅動的路徑不受影響 —— `MarkFromSchema` 已依宣告型別
把欄位正規化過；受害的一律是**自己拼 SQL、自己讀 DataRow** 的 framework repository。

**為何拖到現在才發現**：`ApiKeyRepository` 走 `DbScope.Common`，而測試 fixture 把
`common` 綁在 SQL Server —— 那幾支 `[DbFact(DatabaseType.SQLite)]` 實際跑的是 SQL Server。
盤點與修法見 `ProviderScopedRouter`（`tests/Bee.Tests.Shared/`）。

**殘留**：其他自己拼 SQL 讀日期欄的地方未逐一查證。看到裸 `is DateTime` 就該問一句
「這個值在 SQLite 上是什麼型別」。

## Oracle：壓測工具與單元測試共用同一個 schema，會互相破壞

**症狀**：跑過 `dotnet run --project tools/Bee.LoadTests -- prepare --provider Oracle` 之後，
單元測試 fixture 整組失敗於
`InvalidOperationException: Change narrows a column (AlterFieldChange)`
（`InvalidOperationException` 不是 `DbException`，`RunStep` 不攔，整個 Oracle setup 中止）。

**根因**：`databaseNamePrefix` 對 Oracle 無效 —— 五張表全在單一 `testuser` schema 下
（見 `.runsettings` 的註解）。壓測用 `apps/Bee.Northwind/Define` 的 `st_user`
（`password` 長度 200），單元測試用 `tests/Define` 的（長度 40），互相覆蓋。

**正解（2026-09-08 起）**：改用**專屬 schema**，不要再讓兩邊共用。工具現在會擋下無法
以資料庫名隔離的連線字串，補救方式是設 `BEE_LOADTEST_CONNSTR_ORACLE` 指向一個保留給
壓測的 user——建立步驟見 [`docs/repo-ops/load-testing.md`](../load-testing.md)。

**下面那段復原 SQL 只在「已經踩到」時用**（也就是專屬 schema 之前跑過壓測的機器）。
它會逐條刪掉 `loadtest_user_%` 與 `loadtest` 公司列，並把 `st_user.password` 改回 40 ——

```sql
delete from st_user_company where company_rowid in (select sys_rowid from st_company where sys_id='loadtest');
delete from st_company where sys_id='loadtest';
delete from st_user where sys_id like 'loadtest_user_%';
commit;
alter table st_user modify (password varchar2(40 char));
```

`loadtest_user_%` 那幾列必須先刪 —— 它們的密碼雜湊有 79 字元，不刪就 `ORA-01441`。
之後要再壓測只需重跑 `prepare`（指向專屬 schema）。

> **這段 SQL 執行過會留下痕跡，而且看起來不像人做的。** 它只刪帳號、不刪
> `ft_customer`，所以事後看到的是「十萬列壓測資料還在，但植入它們的帳號不見了」——
> 很容易被誤讀成測試套件把帳號清掉了。實際上 `tests/` 沒有任何 `DELETE` / `TRUNCATE` /
> `DROP` 做得到這件事。判別法：**看 `st_user.password` 的長度**，40 表示這段跑過
> （壓測定義是 200）。

## 深分頁：`OFFSET` 的成本隨頁碼成長，四家都躲不掉

**症狀**：同一張表、同一個 `pageSize`，翻到後面的頁明顯變慢，而第一頁再怎麼加資料量都不動。

**根因**：頁碼式分頁靠 `OFFSET`，引擎必須走過並丟棄偏移量之前的每一列。這是 offset 分頁的
固有行為，**不是框架的 dialect 實作有問題** —— 換一家 provider 不會解決。

**2026-09-08 在一台 macOS 開發機上量到的**（`ft_customer` 100,000 列、`pageSize` 50、
淺頁自第 1 頁、深頁自第 1,900 頁即 `OFFSET 94,950`、各走 10 頁、20 VU、Local + Encrypted、
封閉模型、warm-up 30s + 量測 120s；前三家各兩輪、Oracle 三輪，皆零錯誤）：

| Provider | 淺頁 p50 | 深頁 p50 | 倍數 |
|---|---:|---:|---:|
| SQL Server | 1.2 | 18.5 | 15.4×（另一輪 14.5×） |
| PostgreSQL | 0.68 | 11.16 | 16.4×（15.6×） |
| MySQL | 1.17 | 22.39 | 19.1×（20.1×） |
| Oracle | 0.7 | 19.3 ~ 24.6 | 27.6× / 33.3× / 35.1× |

單位為毫秒。這是**當時在那台機器量到的，不是框架的效能規格**：client 與 server 同機、
封閉模型（送出速率隨系統變慢而下降，因此不顯示飽和點）、100,000 列對四家都仍屬小表。
**絕對值不可跨 provider 比**（各容器設定不同），倍數關係才是重點。

Oracle 那列另有兩個保留：倍數逐輪遞增而非上下震盪（前三家兩輪差距都在 1 以內），
且它**兩個場景都**帶著約 0.8～1.9s 的 p99 與 2.1～2.4s 的 max ——
**淺頁也有**，而只掃 50 列的第一頁不可能因偏移量而慢，所以那條尾巴是那個容器的週期性停頓、
與分頁無關，**成因未查明**。它的吞吐也只有前三家的五分之一左右。

**決定**：**不處理**（2026-09-08）。ERP 畫面都有篩選、結果集通常在數十頁內，不值得為此
換掉跳頁能力。keyset pagination 能讓成本與偏移量無關，代價是使用者不能直接跳到第 500 頁 ——
那是產品決策，不是技術決策。`PagingOptions.Page` 的 XML doc 已標明這件事。

**遇到真的需要深翻的畫面時**：先想能不能用篩選把結果集縮小，那通常才是使用者真正要的；
真的需要「一路翻到底」（例如匯出），再回頭評估 keyset。

### 別家 ERP 怎麼處理（2026-09-08 查證）

查這個是為了確認「不處理」不是偷懶。結論：**兩家的主線都是 offset**，keyset 只出現在
SAP CAP 的一個選配功能裡、而且是為了**一致性**而非效能；真正迴避掉深翻的是 **UI 層**的設計，
不是框架層的演算法。

**Odoo —— 跟我們現在完全一樣。** 它是與本框架最接近的類比（ORM + 泛用列表視圖 + 任意跳頁）：

- `odoo/tools/query.py` 的 `select()` 尾端就是 `LIMIT %s` + `OFFSET %s`，`models.py` 的
  `_read_group` 同樣；該檔與 `models.py` 都 grep 不到 keyset / cursor / seek。
- 它的 pager（`addons/web/static/src/core/pager/pager.js`）**不只有上下頁** —— 點一下數字
  可輸入 `min[,max]` 範圍，parse 後 clamp 進 offset，等於允許直接跳到任意深度。

**SAP —— 靠設計不讓使用者走到那裡，而不是靠 keyset。** 分三層看：

- **UI 層**：SAPUI5 文件寫明 responsive table 一次不超過 200 筆、更多（上限 1000）要用
  growing 功能並「make sure the user can filter the data」。Fiori elements 的 list report
  預設是 growing table（`More` 按鈕），**只能往下追加、沒有頁碼**，主互動是頂部的 filter bar。
- **RAP（ABAP，OData V4）的查詢 API 是 offset 制**。`IF_RAP_QUERY_PAGING` 給的是
  `get_page_size()` 與 **`get_offset()`**，SAP 自家 openSAP 教材的範例就是
  `DATA(top) = io_request->get_paging( )->get_page_size( ).` /
  `DATA(skip) = io_request->get_paging( )->get_offset( ).`，再對應 SQL 的
  `UP TO n ROWS` 與 `OFFSET`。RAP 確實會在客戶端沒有妥當分頁時補一個帶 `$skiptoken` 的
  next link（未給 `$top` 時預設截到 100 筆、`$top` 上限硬性 5000），但那是**框架的封頂機制**，
  底下的 API 仍然只給得出 offset。
- **而 UI5 客戶端根本就送 `$skip` / `$top`**。openui5#3487 有人主張這是「server side paging
  not properly implemented」，UI5 維護者的回覆是：OData 是無狀態協定，規格要求的是
  `$top` 搭配穩定排序（`$orderby` 未給時服務端必須自行施加穩定順序），server-driven paging
  的用途不是引入狀態。**議題以 completed 關閉。** 所以 Fiori 的 grid table 把捲軸拖到深處，
  送出的就是一個大的 `$skip`。

**SAP 真正有 keyset 的地方是 CAP，而且是選配。** CAP 的 **Reliable Pagination**
（`cds.query.limit.reliablePaging`，Java 為 `.enabled`）**以「該頁最後一列的值」產生 skip token**
—— 這是貨真價實的 keyset。但三件事要一起看：

1. **預設關閉**，且僅限 OData V4 端點。
2. **它解的是一致性，不是深頁延遲** —— 官方說法是數字型 skip token「can result in duplicate or
   missing rows if the entity set is modified between the calls」。
3. **限制不小**：`$orderby` 不能用函式／算術運算式的結果、元素必須是簡單型別、若有 `$select`
   則所有 `$orderby` 元素都得包含在內、不支援複雜的結果集串接。

順帶一提，SAP 自家的 ABAP 關鍵字文件講 `SELECT ... OFFSET` 時只寫「必須搭配 `ORDER BY`」
與嚴格語法檢查，**沒有提效能**。

**對本條的意義（兩點）**：

**其一**，成熟 ERP 對「深翻很慢」的答案是**不要讓使用者走到那裡**，而不是讓 `OFFSET` 變快。
上一段那句「先想能不能用篩選把結果集縮小」在 SAP 是硬性 UI 準則，不是建議。真要做 UI 的話，
growing / `More` 比頁碼跳轉更貼近這個結論，且**不需要動 `PagingOptions`**。

**其二，也是更值得記的：業界做 keyset 的動機是「一致性」，不是「深頁變快」。**
CAP 與 Microsoft 的 ASP.NET OData 都明講，換掉數字 offset 是為了避免資料在兩次呼叫之間被改動
而造成重複列或漏列。**那跟我們量的是不同的問題** —— 我們階段 B 只評估了延遲。
若日後要重啟這個決定，**一致性會是比延遲更強的理由**，而且本框架有同樣的曝險：
`PagingOptions.IncludeTotalCount` 預設 `false`、靠 `PageSize + 1` 探測 `HasMore`，
那個設計對「翻頁期間資料被改」一樣沒有保護。**這一點目前沒有測過，也沒有結論。**

**若日後真的要走 keyset**，`$skiptoken` 那個形狀值得抄：**由伺服端在回應裡發不透明續傳權杖**，
呼叫端原樣帶回。這樣底層從 offset 換成 keyset 時 wire contract 不必跟著改 ——
而現行 `PagingOptions` 是頁碼制，換過去就是破壞性變更。

> **查證邊界，別把這段當定論**：Odoo 那三點是讀 17.0 的原始碼確認的（程式碼會搬家，
> 對到新版本前先確認路徑還在）；SAP 的 UI 準則、CAP 的 Reliable Pagination、RAP 的
> `get_offset()` 與 openSAP 範例、UI5 維護者在 openui5#3487 的回覆，都是官方或 SAP 自家來源。
>
> **這段修正過一次**：先前寫「`$skiptoken` 實際被當 offset 用」時引的是社群教學，
> 而且差點把一段描述**誤植給 SAP** —— 「skiptoken 由 orderby 值加上最後一列的鍵組成」
> 那個說法出自 **Microsoft 的 ASP.NET OData WebAPI**（而且連它都是 per-route 選配，
> 預設仍用 `$skip`），不是 SAP。查框架行為時**先確認那句話的主詞是誰**。
>
> 仍未查：SAP Gateway（OData V2）各服務自訂的 `$skiptoken` 語意 —— 那本來就是
> per-service 的實作決定，不存在單一答案。

## 跨 DB seed 的雜項

- 識別符一律 `dbType.QuoteIdentifier(...)`——**Oracle 會把它大寫**（`"FT_CATEGORY"`），其餘保留原樣。
- seed JSON 值皆為字串（含數字型 PK 如 order `"10248"`），**依目標欄 `FieldDbType` 轉型**，
  不可靠值猜型。
- Date 綁定用 `DateTimeKind.Utc` —— PG 的 Date 對映 `timestamptz`，Npgsql 拒收 Unspecified/Local；
  Utc 對 5 個 DB 皆安全。
- 持久 DB 若殘留舊 seed，gate 會 skip → 需手動清空該 DB 的相關表（含 gate 表本身才能重開 gate）。
