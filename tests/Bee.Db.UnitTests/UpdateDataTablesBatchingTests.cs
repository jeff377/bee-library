using System.ComponentModel;
using System.Data;
using System.Globalization;
using Bee.Base.Data;
using Bee.Db.Dml;
using Bee.Definition;
using Bee.Definition.Database;
using Bee.Tests.Shared;

namespace Bee.Db.UnitTests
{
    /// <summary>
    /// <see cref="DbAccess.UpdateDataTables"/> 在各 provider 上的多列寫入。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>ApplySpec</c> 會在 provider 支援時開啟 <c>UpdateBatchSize</c>，把「每列一次 round trip」
    /// 收成每批一次。批次會改變 adapter 送命令的方式，最可能出錯的兩件事是<b>回傳的異動列數</b>
    /// 與<b>失敗時的例外</b> —— 兩者都只有真的連上各家資料庫才驗得到。
    /// </para>
    /// <para>
    /// 補這一組的理由是覆蓋缺口：在此之前 adapter 寫入路徑的 DB 測試<b>只有 SQLite</b>，
    /// 而 SQLite 恰好是不支援批次的兩個之一 —— 也就是說批次實際生效的三個 provider
    /// （SQL Server / MySQL / Oracle）對這條路徑是零覆蓋。
    /// </para>
    /// <para>
    /// 每個 provider 各一個 <c>[DbFact]</c>，容器不在時各自跳過。列數刻意大於 1，
    /// 否則批次與不批次跑起來沒有差別，測了等於沒測。
    /// </para>
    /// <para>
    /// <b>這一組守的是「批次不改變結果」，不是「批次有開著」</b> —— 把 <c>TryEnableBatching</c>
    /// 停掉它仍然全綠，那是刻意的：行為不變正是驗收條件。批次能力本身由
    /// <c>ProviderBatchingSupportTests</c> 釘住，實際效益則靠量測，兩者都不在這裡。
    /// </para>
    /// </remarks>
    public class UpdateDataTablesBatchingTests : IClassFixture<SharedDbFixture>
    {
        /// <summary>一次寫入的列數；要大於 1 才會實際走到批次。</summary>
        private const int RowCount = 5;

        private readonly SharedDbFixture _fx;

        public UpdateDataTablesBatchingTests(SharedDbFixture fx) { _fx = fx; }

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("SQL Server（支援批次）：多列 Added / Modified / Deleted 應正確落地並回報列數")]
        public void SqlServer_MultiRow_AppliesAndReportsAffected() => RunFor(DatabaseType.SQLServer);

        [DbFact(DatabaseType.MySQL)]
        [DisplayName("MySQL（支援批次）：多列 Added / Modified / Deleted 應正確落地並回報列數")]
        public void MySql_MultiRow_AppliesAndReportsAffected() => RunFor(DatabaseType.MySQL);

        [DbFact(DatabaseType.Oracle)]
        [DisplayName("Oracle（支援批次）：多列 Added / Modified / Deleted 應正確落地並回報列數")]
        public void Oracle_MultiRow_AppliesAndReportsAffected() => RunFor(DatabaseType.Oracle);

        [DbFact(DatabaseType.PostgreSQL)]
        [DisplayName("PostgreSQL（不支援批次，退回逐列）：多列寫入的結果應與支援批次者一致")]
        public void PostgreSql_MultiRow_AppliesAndReportsAffected() => RunFor(DatabaseType.PostgreSQL);

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("SQLite（不支援批次，退回逐列）：多列寫入的結果應與支援批次者一致")]
        public void Sqlite_MultiRow_AppliesAndReportsAffected() => RunFor(DatabaseType.SQLite);

        /// <summary>
        /// 在指定資料庫上跑一輪多列寫入。
        /// </summary>
        /// <param name="databaseType">目標資料庫。</param>
        private void RunFor(DatabaseType databaseType)
        {
            var dbAccess = _fx.NewDbAccess(TestDbConventions.GetDatabaseId(databaseType));

            // Oracle 的識別碼長度限制較嚴，表名壓在 30 字元內。
            string table = "tb_udb_" + Guid.NewGuid().ToString("N")[..8];
            string qt = databaseType.QuoteIdentifier(table);
            string qRowId = databaseType.QuoteIdentifier(SysFields.RowId);
            string qName = databaseType.QuoteIdentifier("name");

            dbAccess.ExecuteNonQuery(CreateTableSql(databaseType, qt, qRowId, qName));
            try
            {
                var schema = new TableSchema { TableName = table };
                schema.Fields!.Add(SysFields.RowId, "Row ID", FieldDbType.String, 50);
                schema.Fields!.Add("name", "Name", FieldDbType.String, 50);

                // --- 1. 多列 Added ---
                var added = NewTable(table);
                var ids = new string[RowCount];
                for (int i = 0; i < RowCount; i++)
                {
                    ids[i] = Guid.NewGuid().ToString("N");
                    added.Rows.Add(ids[i], $"row-{i}");
                }
                var counts = dbAccess.UpdateDataTables(
                    [new TableSchemaCommandBuilder(databaseType, schema).BuildUpdateSpec(added)]);

                // 批次會改變命令送出的方式，但不該改變回報的列數。
                Assert.Equal(RowCount, Assert.Single(counts));
                Assert.Equal(RowCount, CountRows(dbAccess, qt));

                // --- 2. 多列 Modified + 一列 Deleted ---
                var loaded = dbAccess.Execute(new DbCommandSpec(DbCommandKind.DataTable,
                    $"SELECT {qRowId},{qName} FROM {qt}")).Table!;
                loaded.TableName = table;
                loaded.AcceptChanges();

                int modified = 0;
                foreach (DataRow row in loaded.Rows)
                {
                    var id = Convert.ToString(row[SysFields.RowId], CultureInfo.InvariantCulture);
                    if (id == ids[0]) { row.Delete(); }
                    else { row["name"] = "changed"; modified++; }
                }

                counts = dbAccess.UpdateDataTables(
                    [new TableSchemaCommandBuilder(databaseType, schema).BuildUpdateSpec(loaded)]);

                Assert.Equal(modified + 1, Assert.Single(counts));
                Assert.Equal(RowCount - 1, CountRows(dbAccess, qt));

                int changed = Convert.ToInt32(dbAccess.ExecuteScalar(
                    $"SELECT COUNT(*) FROM {qt} WHERE {qName}={{0}}", "changed"), CultureInfo.InvariantCulture);
                Assert.Equal(modified, changed);
            }
            finally
            {
                dbAccess.ExecuteNonQuery($"DROP TABLE {qt}");
            }
        }

        private static DataTable NewTable(string tableName)
        {
            var t = new DataTable(tableName);
            t.Columns.Add(SysFields.RowId, typeof(string));
            t.Columns.Add("name", typeof(string));
            return t;
        }

        private static int CountRows(DbAccess dbAccess, string quotedTable)
            => Convert.ToInt32(dbAccess.ExecuteScalar($"SELECT COUNT(*) FROM {quotedTable}"), CultureInfo.InvariantCulture);

        /// <summary>
        /// 各方言的建表語句。
        /// </summary>
        /// <remarks>
        /// 刻意手寫而不繞道 schema 引擎：這一組測的是寫入路徑，建表只是前置，
        /// 走 <c>TableSchemaBuilder</c> 會把定義檔載入與升級規劃一起拖進來。
        /// </remarks>
        private static string CreateTableSql(DatabaseType databaseType, string qt, string qRowId, string qName)
            => databaseType switch
            {
                DatabaseType.SQLServer =>
                    $"CREATE TABLE {qt} ({qRowId} NVARCHAR(50) NOT NULL PRIMARY KEY, {qName} NVARCHAR(50) NULL)",
                DatabaseType.MySQL =>
                    $"CREATE TABLE {qt} ({qRowId} VARCHAR(50) NOT NULL PRIMARY KEY, {qName} VARCHAR(50) NULL)",
                DatabaseType.Oracle =>
                    $"CREATE TABLE {qt} ({qRowId} VARCHAR2(50) NOT NULL PRIMARY KEY, {qName} VARCHAR2(50))",
                DatabaseType.PostgreSQL =>
                    $"CREATE TABLE {qt} ({qRowId} VARCHAR(50) NOT NULL PRIMARY KEY, {qName} VARCHAR(50) NULL)",
                DatabaseType.SQLite =>
                    $"CREATE TABLE {qt} ({qRowId} TEXT PRIMARY KEY, {qName} TEXT)",
                _ => throw new ArgumentOutOfRangeException(nameof(databaseType), databaseType, "未涵蓋的資料庫類型。"),
            };
    }
}
