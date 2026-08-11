using System.ComponentModel;
using Bee.Base.Data;
using Bee.Definition.Database;
using Bee.Tests.Shared;

namespace Bee.Db.UnitTests
{
    /// <summary>
    /// 驗證 ADR-032 D1：五種 provider 都以**無時區欄位**原樣存放 UTC 值，讀回逐 tick 相同。
    /// </summary>
    /// <remarks>
    /// 這條測試守的是「資料庫不介入時區」這個前提。若某個 provider 或其 ADO.NET 驅動在寫入或
    /// 讀取時依 server / client 時區隱式換算，整條轉換鏈的算式就會多一次偏移——而症狀只會是
    /// 「時間差了幾小時」，沒有任何錯誤訊息。PostgreSQL 的 <c>timestamptz</c> 正是因為會做這種
    /// 隱式換算而被 D1 排除，此處確認選用的 <c>timestamp</c> 不會。
    ///
    /// 測試值刻意選在 UTC 與常見開發／CI 時區都不同日的時刻，使任何一次時區換算都會改變日期，
    /// 而非只改變時分——後者在偏移恰為 0 的環境下驗不出來。
    /// </remarks>
    public class DateTimeUtcStorageRoundTripTests : IClassFixture<SharedDbFixture>
    {
        private readonly SharedDbFixture _fx;
        public DateTimeUtcStorageRoundTripTests(SharedDbFixture fx) { _fx = fx; }

        /// <summary>UTC 的 23:30；台北為隔日 07:30、紐約為同日 18:30——任一換算都會跨日或跨時。</summary>
        private static readonly DateTime s_utcValue =
            new DateTime(2026, 3, 15, 23, 30, 45, DateTimeKind.Unspecified);

        private const string TableName = "dt_utc_storage_test";

        private DateTime WriteThenRead(string databaseId, string createSql, string dropSql)
        {
            var dbAccess = _fx.NewDbAccess(databaseId);
            dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery, dropSql));
            dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery, createSql));
            dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                $"INSERT INTO {TableName} (dt) VALUES ({{0}})", s_utcValue));
            var result = dbAccess.Execute(new DbCommandSpec(DbCommandKind.DataTable,
                $"SELECT dt FROM {TableName}"));
            return result.Table!.Rows[0].GetFieldValue<DateTime>("dt");
        }

        private void RunRoundTrip(string databaseId, string createSql, string dropSql)
        {
            try
            {
                var readBack = WriteThenRead(databaseId, createSql, dropSql);

                Assert.Equal(s_utcValue.Ticks, readBack.Ticks);
            }
            finally
            {
                _fx.NewDbAccess(databaseId).Execute(new DbCommandSpec(DbCommandKind.NonQuery, dropSql));
            }
        }

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("SQL Server：datetime2 原樣存放 UTC 值，讀回逐 tick 相同")]
        public void RoundTrip_SqlServer_StoresUtcVerbatim()
        {
            RunRoundTrip("common_sqlserver",
                $"CREATE TABLE [{TableName}] ([dt] [datetime2](7) NOT NULL);",
                $"IF OBJECT_ID(N'{TableName}', N'U') IS NOT NULL DROP TABLE [{TableName}];");
        }

        [DbFact(DatabaseType.PostgreSQL)]
        [DisplayName("PostgreSQL：timestamp（無 tz）原樣存放 UTC 值，不做隱式換算")]
        public void RoundTrip_PostgreSQL_StoresUtcVerbatim()
        {
            RunRoundTrip("common_postgresql",
                $"CREATE TABLE {TableName} (dt timestamp NOT NULL);",
                $"DROP TABLE IF EXISTS {TableName};");
        }

        [DbFact(DatabaseType.MySQL)]
        [DisplayName("MySQL：DATETIME(6) 原樣存放 UTC 值，讀回逐 tick 相同")]
        public void RoundTrip_MySQL_StoresUtcVerbatim()
        {
            RunRoundTrip("common_mysql",
                $"CREATE TABLE {TableName} (dt DATETIME(6) NOT NULL);",
                $"DROP TABLE IF EXISTS {TableName};");
        }

        [DbFact(DatabaseType.Oracle)]
        [DisplayName("Oracle：TIMESTAMP 原樣存放 UTC 值，讀回逐 tick 相同")]
        public void RoundTrip_Oracle_StoresUtcVerbatim()
        {
            RunRoundTrip("common_oracle",
                $"CREATE TABLE {TableName} (dt TIMESTAMP(7) NOT NULL)",
                $"BEGIN EXECUTE IMMEDIATE 'DROP TABLE {TableName}'; EXCEPTION WHEN OTHERS THEN NULL; END;");
        }

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("SQLite：TEXT 欄位原樣存放 UTC 值，讀回逐 tick 相同")]
        public void RoundTrip_SQLite_StoresUtcVerbatim()
        {
            RunRoundTrip("common_sqlite",
                $"CREATE TABLE {TableName} (dt TEXT NOT NULL);",
                $"DROP TABLE IF EXISTS {TableName};");
        }
    }
}
