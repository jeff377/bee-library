using System.ComponentModel;
using Bee.Base.Data;
using Bee.Db.Manager;
using Bee.Db.Providers;
using Bee.Db.Schema;
using Bee.Definition.Database;
using Bee.Tests.Shared;

namespace Bee.Db.UnitTests
{
    /// <summary>
    /// End-to-end coverage for <see cref="FieldDbType.Time"/> on every provider: a time-of-day
    /// column is created as a fixed-width character column, round-trips its <c>"HH:mm"</c> value,
    /// and — crucially — compares clean on the next schema comparison.
    /// </summary>
    /// <remarks>
    /// The convergence assertion is the regression guard that matters. The database reports the
    /// column as a 5-length string, never as <c>Time</c>, so if the physical-shape reduction in
    /// <c>DbField.Compare</c> is ever lost, every comparison reports drift and the upgrade
    /// orchestrator re-issues an ALTER forever. That failure is silent in unit tests (ADR-033).
    /// </remarks>
    public class TimeOfDayColumnIntegrationTests : IClassFixture<SharedDbFixture>
    {
        private readonly SharedDbFixture _fx;
        public TimeOfDayColumnIntegrationTests(SharedDbFixture fx) { _fx = fx; }

        private const string TableName = "st_time_column_test";

        private static TableSchema BuildSchema()
        {
            var schema = new TableSchema { TableName = TableName };
            schema.Fields!.Add("sys_rowid", "Row ID", FieldDbType.Guid);
            schema.Fields!.Add("work_start", "Start", FieldDbType.Time);
            schema.Indexes!.AddPrimaryKey("sys_rowid");
            return schema;
        }

        private static IDialectFactory Dialect(DatabaseType databaseType)
            => DbDialectRegistry.Get(databaseType);

        /// <summary>
        /// Creates the table from the schema, round-trips a value, then compares the definition
        /// against the database's own read-back and asserts the comparison converges.
        /// </summary>
        private void RunColumnLifecycle(DatabaseType databaseType, string dropSql)
        {
            var databaseId = TestDbConventions.GetDatabaseId(databaseType);
            var dbAccess = _fx.NewDbAccess(databaseId);
            var connectionManager = _fx.GetRequiredService<IDbConnectionManager>();
            var schema = BuildSchema();

            void Drop()
            {
                // The table is absent on the first run; every provider words that differently,
                // so the drop is best-effort rather than pattern-matched.
                try { dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery, dropSql)); }
                catch (System.Data.Common.DbException) { /* table absent */ }
            }

            Drop();
            try
            {
                var createSql = Dialect(databaseType).CreateCreateTableCommandBuilder().GetCommandText(schema);
                dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery, createSql));

                dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                    $"INSERT INTO {TableName} (sys_rowid, work_start) VALUES ({{0}}, {{1}})",
                    Guid.NewGuid(), FieldDbType.Time.ToFieldValue("8:30")));

                var read = dbAccess.Execute(new DbCommandSpec(DbCommandKind.DataTable,
                    $"SELECT work_start FROM {TableName}"));
                var stored = read.Table!.Rows[0].GetFieldValue<string>("work_start");
                Assert.Equal("08:30", stored.Trim());

                // The comparison must converge on the time column: the database says String(5),
                // the definition says Time. The assertion is scoped to that column rather than the
                // whole table because unrelated pre-existing quirks (SQLite reports a database-side
                // default for Guid columns that the definition does not carry) would otherwise mask
                // what this test is guarding.
                var real = Dialect(databaseType)
                    .CreateTableSchemaProvider(databaseId, connectionManager)
                    .GetTableSchema(TableName);
                var compared = new TableSchemaComparer(BuildSchema(), real, databaseType).Compare();
                Assert.Equal(DbUpgradeAction.None, compared.Fields!["work_start"].UpgradeAction);
            }
            finally
            {
                Drop();
            }
        }

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("SQL Server：Time 欄位建表、round-trip 與 schema 比對應收斂")]
        public void TimeColumn_SqlServer_RoundTripsAndConverges()
        {
            RunColumnLifecycle(DatabaseType.SQLServer,
                $"IF OBJECT_ID(N'{TableName}', N'U') IS NOT NULL DROP TABLE [{TableName}];");
        }

        [DbFact(DatabaseType.PostgreSQL)]
        [DisplayName("PostgreSQL：Time 欄位建表、round-trip 與 schema 比對應收斂")]
        public void TimeColumn_PostgreSql_RoundTripsAndConverges()
        {
            RunColumnLifecycle(DatabaseType.PostgreSQL, $"DROP TABLE IF EXISTS {TableName};");
        }

        [DbFact(DatabaseType.MySQL)]
        [DisplayName("MySQL：Time 欄位建表、round-trip 與 schema 比對應收斂")]
        public void TimeColumn_MySql_RoundTripsAndConverges()
        {
            RunColumnLifecycle(DatabaseType.MySQL, $"DROP TABLE IF EXISTS {TableName};");
        }

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("SQLite：Time 欄位建表、round-trip 與 schema 比對應收斂")]
        public void TimeColumn_Sqlite_RoundTripsAndConverges()
        {
            RunColumnLifecycle(DatabaseType.SQLite, $"DROP TABLE IF EXISTS {TableName};");
        }

        [DbFact(DatabaseType.Oracle)]
        [DisplayName("Oracle：Time 欄位建表、round-trip 與 schema 比對應收斂")]
        public void TimeColumn_Oracle_RoundTripsAndConverges()
        {
            RunColumnLifecycle(DatabaseType.Oracle,
                $"BEGIN EXECUTE IMMEDIATE 'DROP TABLE {TableName}'; EXCEPTION WHEN OTHERS THEN NULL; END;");
        }
    }
}
