using System.ComponentModel;
using Bee.Base.Data;
using Bee.Definition.Database;
using Bee.Tests.Shared;

namespace Bee.Db.UnitTests
{
    /// <summary>
    /// End-to-end round-trip coverage proving that a .NET <see cref="DateTime"/> written through the
    /// standard parameter path (<c>DbTypeMapper.Infer</c> → <c>DbType.DateTime2</c>) retains sub-millisecond
    /// precision and the full pre-1753 range. With the legacy <c>DbType.DateTime</c> inference these values
    /// were truncated to ~3.33 ms and pre-1753 values threw before reaching the column.
    /// </summary>
    public class DateTimePrecisionRoundTripTests : IClassFixture<SharedDbFixture>
    {
        private readonly SharedDbFixture _fx;
        public DateTimePrecisionRoundTripTests(SharedDbFixture fx) { _fx = fx; }

        private void Drop(string databaseId, string tableName, string ifExistsSql)
        {
            var dbAccess = _fx.NewDbAccess(databaseId);
            dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery, ifExistsSql));
        }

        private DateTime WriteThenRead(string databaseId, string createSql, string dropSql, DateTime value)
        {
            var dbAccess = _fx.NewDbAccess(databaseId);
            dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery, dropSql));
            dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery, createSql));
            dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                "INSERT INTO dt_precision_test (dt) VALUES ({0})", value));
            var result = dbAccess.Execute(new DbCommandSpec(DbCommandKind.DataTable,
                "SELECT dt FROM dt_precision_test"));
            return result.Table!.Rows[0].GetFieldValue<DateTime>("dt");
        }

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("SQL Server：亞毫秒 DateTime 經參數寫入 datetime2(7) 應無精度遺失")]
        public void RoundTrip_SqlServer_SubMillisecondPrecisionPreserved()
        {
            const string drop = "IF OBJECT_ID(N'dt_precision_test', N'U') IS NOT NULL DROP TABLE [dt_precision_test];";
            const string create = "CREATE TABLE [dt_precision_test] ([dt] [datetime2](7) NOT NULL);";
            // 100 ns resolution value: .1234567 seconds cannot be represented by legacy `datetime`.
            var value = new DateTime(2026, 7, 2, 10, 0, 0, DateTimeKind.Unspecified).AddTicks(1234567);
            try
            {
                var readBack = WriteThenRead("common_sqlserver", create, drop, value);
                Assert.Equal(value.Ticks, readBack.Ticks);
            }
            finally
            {
                Drop("common_sqlserver", "dt_precision_test", drop);
            }
        }

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("SQL Server：pre-1753 的 DateTime 經參數寫入 datetime2 不應拋溢位")]
        public void RoundTrip_SqlServer_Pre1753RangeAccepted()
        {
            const string drop = "IF OBJECT_ID(N'dt_precision_test', N'U') IS NOT NULL DROP TABLE [dt_precision_test];";
            const string create = "CREATE TABLE [dt_precision_test] ([dt] [datetime2](7) NOT NULL);";
            var value = new DateTime(1200, 1, 1, 12, 0, 0, DateTimeKind.Unspecified);
            try
            {
                var readBack = WriteThenRead("common_sqlserver", create, drop, value);
                Assert.Equal(value, readBack);
            }
            finally
            {
                Drop("common_sqlserver", "dt_precision_test", drop);
            }
        }

        [DbFact(DatabaseType.PostgreSQL)]
        [DisplayName("PostgreSQL：DbType.DateTime2 參數寫入 timestamp 微秒精度應正確 round-trip")]
        public void RoundTrip_PostgreSql_MicrosecondPrecisionPreserved()
        {
            const string drop = "DROP TABLE IF EXISTS dt_precision_test;";
            const string create = "CREATE TABLE dt_precision_test (dt timestamp NOT NULL);";
            // Microsecond resolution (7th tick digit 0) so PostgreSQL timestamp(6) round-trips exactly.
            var value = new DateTime(2026, 7, 2, 10, 0, 0, DateTimeKind.Unspecified).AddTicks(1234560);
            try
            {
                var readBack = WriteThenRead("common_postgresql", create, drop, value);
                Assert.Equal(value.Ticks, readBack.Ticks);
            }
            finally
            {
                Drop("common_postgresql", "dt_precision_test", drop);
            }
        }
    }
}
