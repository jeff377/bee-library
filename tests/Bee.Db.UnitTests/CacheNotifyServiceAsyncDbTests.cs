using System.ComponentModel;
using System.Globalization;
using Bee.Db.CacheNotify;
using Bee.Db.Manager;
using Bee.Definition.Database;
using Bee.Tests.Shared;

namespace Bee.Db.UnitTests
{
    /// <summary>
    /// 針對 <see cref="CacheNotifyService.TouchAsync"/> 的 DB 整合測試。
    /// 既有 <c>CacheNotifyServiceTests</c> 只測試同步版本 <c>Touch</c>；
    /// 本類別補強非同步執行路徑，確保 UPSERT 在各方言下均能正常非同步執行。
    /// </summary>
    public class CacheNotifyServiceAsyncDbTests : IClassFixture<SharedDbFixture>
    {
        private readonly SharedDbFixture _fx;

        public CacheNotifyServiceAsyncDbTests(SharedDbFixture fx) { _fx = fx; }

        private static string NewKey() => $"CacheNotifyAsyncTest:{Guid.NewGuid():N}";

        private async Task ExecuteTouchAsync(DatabaseType databaseType, string cacheKey)
        {
            var databaseId = TestDbConventions.GetDatabaseId(databaseType);
            var connectionManager = _fx.GetRequiredService<IDbConnectionManager>();
            var service = _fx.GetRequiredService<ICacheNotifyService>();

            using var connection = connectionManager.CreateConnection(databaseId);
            connection.Open();
            using var transaction = connection.BeginTransaction();
            await service.TouchAsync(cacheKey, transaction, databaseType).ConfigureAwait(false);
            transaction.Commit();
        }

        private long ReadVersion(DatabaseType databaseType, string cacheKey)
        {
            var databaseId = TestDbConventions.GetDatabaseId(databaseType);
            var dbAccess = _fx.NewDbAccess(databaseId);

            string tbl = databaseType.QuoteIdentifier("st_cache_notify");
            string key = databaseType.QuoteIdentifier("cache_key");
            string ver = databaseType.QuoteIdentifier("cache_version");

            var table = dbAccess.ExecuteDataTable(
                $"SELECT {ver} FROM {tbl} WHERE {key} = {{0}}", cacheKey)
                ?? throw new InvalidOperationException("Query returned no table.");

            Assert.Single(table.Rows);
            return Convert.ToInt64(table.Rows[0][0], CultureInfo.InvariantCulture);
        }

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("SQL Server：TouchAsync 版本號自增且第二次 Touch 累加至 2")]
        public async Task TouchAsync_SqlServer_VersionIncrements()
        {
            var key = NewKey();
            await ExecuteTouchAsync(DatabaseType.SQLServer, key).ConfigureAwait(false);
            Assert.Equal(1L, ReadVersion(DatabaseType.SQLServer, key));
            await ExecuteTouchAsync(DatabaseType.SQLServer, key).ConfigureAwait(false);
            Assert.Equal(2L, ReadVersion(DatabaseType.SQLServer, key));
        }

        [DbFact(DatabaseType.PostgreSQL)]
        [DisplayName("PostgreSQL：TouchAsync 版本號自增且第二次 Touch 累加至 2")]
        public async Task TouchAsync_PostgreSQL_VersionIncrements()
        {
            var key = NewKey();
            await ExecuteTouchAsync(DatabaseType.PostgreSQL, key).ConfigureAwait(false);
            Assert.Equal(1L, ReadVersion(DatabaseType.PostgreSQL, key));
            await ExecuteTouchAsync(DatabaseType.PostgreSQL, key).ConfigureAwait(false);
            Assert.Equal(2L, ReadVersion(DatabaseType.PostgreSQL, key));
        }

        [DbFact(DatabaseType.MySQL)]
        [DisplayName("MySQL：TouchAsync 版本號自增且第二次 Touch 累加至 2")]
        public async Task TouchAsync_MySQL_VersionIncrements()
        {
            var key = NewKey();
            await ExecuteTouchAsync(DatabaseType.MySQL, key).ConfigureAwait(false);
            Assert.Equal(1L, ReadVersion(DatabaseType.MySQL, key));
            await ExecuteTouchAsync(DatabaseType.MySQL, key).ConfigureAwait(false);
            Assert.Equal(2L, ReadVersion(DatabaseType.MySQL, key));
        }

        [DbFact(DatabaseType.Oracle)]
        [DisplayName("Oracle：TouchAsync 版本號自增且第二次 Touch 累加至 2")]
        public async Task TouchAsync_Oracle_VersionIncrements()
        {
            var key = NewKey();
            await ExecuteTouchAsync(DatabaseType.Oracle, key).ConfigureAwait(false);
            Assert.Equal(1L, ReadVersion(DatabaseType.Oracle, key));
            await ExecuteTouchAsync(DatabaseType.Oracle, key).ConfigureAwait(false);
            Assert.Equal(2L, ReadVersion(DatabaseType.Oracle, key));
        }
    }
}
