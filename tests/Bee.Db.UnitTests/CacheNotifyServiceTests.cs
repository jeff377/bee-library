using System.ComponentModel;
using System.Globalization;
using Bee.Db.CacheNotify;
using Bee.Db.Manager;
using Bee.Definition.Database;
using Bee.Tests.Shared;

namespace Bee.Db.UnitTests
{
    /// <summary>
    /// Integration tests for <see cref="CacheNotifyService"/> against a live database per dialect.
    /// Targets the <c>st_cache_notify</c> table created by <see cref="SharedDbFixture"/> on each
    /// configured database; each test uses a fresh <see cref="Guid"/>-based key so prior runs and
    /// other tests cannot interfere. Tests skip automatically when the dialect's
    /// <c>BEE_TEST_CONNSTR_*</c> env var is unset.
    /// </summary>
    public class CacheNotifyServiceTests : IClassFixture<SharedDbFixture>
    {
        private readonly SharedDbFixture _fx;

        public CacheNotifyServiceTests(SharedDbFixture fx) { _fx = fx; }

        private static string NewKey() => $"CacheNotifyTest:{Guid.NewGuid():N}";

        /// <summary>
        /// Touches <paramref name="cacheKey"/> in its own transaction on the given database and
        /// commits, mirroring how a business write path bumps the notification row alongside its
        /// data change.
        /// </summary>
        private void Touch(DatabaseType databaseType, string cacheKey)
        {
            var databaseId = TestDbConventions.GetDatabaseId(databaseType);
            var connectionManager = _fx.GetRequiredService<IDbConnectionManager>();
            var service = _fx.GetRequiredService<ICacheNotifyService>();

            using var connection = connectionManager.CreateConnection(databaseId);
            connection.Open();
            using var transaction = connection.BeginTransaction();
            service.Touch(cacheKey, transaction, databaseType);
            transaction.Commit();
        }

        private (long Version, DateTime UpdateTime) ReadRow(DatabaseType databaseType, string cacheKey)
        {
            var databaseId = TestDbConventions.GetDatabaseId(databaseType);
            var dbAccess = _fx.NewDbAccess(databaseId);

            string tbl = databaseType.QuoteIdentifier("st_cache_notify");
            string key = databaseType.QuoteIdentifier("cache_key");
            string ver = databaseType.QuoteIdentifier("cache_version");
            string upd = databaseType.QuoteIdentifier("sys_update_time");

            var table = dbAccess.ExecuteDataTable(
                $"SELECT {ver}, {upd} FROM {tbl} WHERE {key} = {{0}}", cacheKey)
                ?? throw new InvalidOperationException("Query returned no table.");

            Assert.Single(table.Rows);
            var row = table.Rows[0];
            return (Convert.ToInt64(row[0], CultureInfo.InvariantCulture),
                Convert.ToDateTime(row[1], CultureInfo.InvariantCulture));
        }

        private long ReadVersion(DatabaseType databaseType, string cacheKey)
            => ReadRow(databaseType, cacheKey).Version;

        // Asserts the version semantics: first touch creates the row at version 1, a second
        // touch of the same key increments to 2 (atomic DB-side +1), and an unrelated key is
        // untouched at 1 — bumps are isolated per key.
        private void RunVersionSemantics(DatabaseType databaseType)
        {
            var keyA = NewKey();
            var keyB = NewKey();

            Touch(databaseType, keyA);
            Assert.Equal(1L, ReadVersion(databaseType, keyA));

            Touch(databaseType, keyA);
            Assert.Equal(2L, ReadVersion(databaseType, keyA));

            Touch(databaseType, keyB);
            Assert.Equal(1L, ReadVersion(databaseType, keyB));
            // keyB's bump must not have disturbed keyA.
            Assert.Equal(2L, ReadVersion(databaseType, keyA));
        }

        // Asserts sys_update_time is refreshed on each touch. The clock is wall-clock server time
        // and some dialects (SQL Server datetime) have coarse granularity, so the invariant is
        // non-decreasing rather than strictly increasing; the version assertion carries the
        // strict "the row really was touched again" guarantee.
        private void RunUpdateTimeRefresh(DatabaseType databaseType)
        {
            var key = NewKey();

            Touch(databaseType, key);
            var first = ReadRow(databaseType, key);

            Thread.Sleep(50);

            Touch(databaseType, key);
            var second = ReadRow(databaseType, key);

            Assert.Equal(2L, second.Version);
            Assert.True(second.UpdateTime >= first.UpdateTime,
                $"Expected update time to advance: first={first.UpdateTime:O}, second={second.UpdateTime:O}.");
        }

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("SQL Server：Touch 版本號自增且逐 key 隔離")]
        public void Touch_SqlServer_VersionSemantics() => RunVersionSemantics(DatabaseType.SQLServer);

        [DbFact(DatabaseType.PostgreSQL)]
        [DisplayName("PostgreSQL：Touch 版本號自增且逐 key 隔離")]
        public void Touch_PostgreSQL_VersionSemantics() => RunVersionSemantics(DatabaseType.PostgreSQL);

        [DbFact(DatabaseType.MySQL)]
        [DisplayName("MySQL：Touch 版本號自增且逐 key 隔離")]
        public void Touch_MySQL_VersionSemantics() => RunVersionSemantics(DatabaseType.MySQL);

        [DbFact(DatabaseType.Oracle)]
        [DisplayName("Oracle：Touch 版本號自增且逐 key 隔離")]
        public void Touch_Oracle_VersionSemantics() => RunVersionSemantics(DatabaseType.Oracle);

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("SQL Server：Touch 刷新 sys_update_time")]
        public void Touch_SqlServer_RefreshesUpdateTime() => RunUpdateTimeRefresh(DatabaseType.SQLServer);

        [DbFact(DatabaseType.PostgreSQL)]
        [DisplayName("PostgreSQL：Touch 刷新 sys_update_time")]
        public void Touch_PostgreSQL_RefreshesUpdateTime() => RunUpdateTimeRefresh(DatabaseType.PostgreSQL);

        [DbFact(DatabaseType.MySQL)]
        [DisplayName("MySQL：Touch 刷新 sys_update_time")]
        public void Touch_MySQL_RefreshesUpdateTime() => RunUpdateTimeRefresh(DatabaseType.MySQL);

        [DbFact(DatabaseType.Oracle)]
        [DisplayName("Oracle：Touch 刷新 sys_update_time")]
        public void Touch_Oracle_RefreshesUpdateTime() => RunUpdateTimeRefresh(DatabaseType.Oracle);
    }
}
