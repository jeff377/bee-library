using System.ComponentModel;
using Bee.Db;
using Bee.Db.CacheNotify;
using Bee.Db.Manager;
using Bee.Definition.Database;
using Bee.Hosting.CacheNotify;
using Bee.ObjectCaching;
using Bee.ObjectCaching.CacheNotify;
using Bee.Tests.Shared;

namespace Bee.Hosting.UnitTests
{
    /// <summary>
    /// Integration tests for <see cref="CacheNotifyPollSession"/> against a live database per
    /// dialect. Drives the polling core directly (no timer) with a real
    /// <see cref="CacheNotifyRouter"/> whose eviction action records the evicted entity, then bumps
    /// the notification row via <see cref="ICacheNotifyService"/> and asserts the poller observes it.
    /// Tests skip automatically when the dialect's <c>BEE_TEST_CONNSTR_*</c> env var is unset.
    /// </summary>
    public class CacheNotifyPollerTests : IClassFixture<SharedDbFixture>
    {
        private readonly SharedDbFixture _fx;

        public CacheNotifyPollerTests(SharedDbFixture fx) { _fx = fx; }

        // Bumps the key in its own committed transaction, mirroring a business write path.
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

        private CacheNotifyPollSession NewSession(DatabaseType databaseType, ICacheNotifyRouter router)
        {
            var databaseId = TestDbConventions.GetDatabaseId(databaseType);
            var factory = _fx.GetRequiredService<IDbAccessFactory>();
            var container = _fx.GetRequiredService<ICacheContainer>();
            return new CacheNotifyPollSession(databaseId, factory, container, router, marginSeconds: 5);
        }

        // Baseline poll evicts nothing; a post-baseline bump is observed and routed exactly once;
        // a repeat poll without a new bump is idempotent (version comparison over the margin overlap).
        // The group is unique per invocation so the incremental window cannot catch another test's
        // bump of the same group on the shared database.
        private void RunPollerLifecycle(DatabaseType databaseType)
        {
            string group = $"PollLifecycle_{Guid.NewGuid():N}";
            var router = new CacheNotifyRouter();
            var evicted = new List<string>();
            router.Register(group, (_, entity) => evicted.Add(entity));

            var session = NewSession(databaseType, router);

            session.Poll(); // baseline only
            Assert.Empty(evicted);

            var entity = Guid.NewGuid().ToString("N");
            Touch(databaseType, $"{group}:{entity}");

            session.Poll(); // delta → evict once
            Assert.Equal(new[] { entity }, evicted);

            session.Poll(); // no new bump → no further evict
            Assert.Single(evicted);
        }

        // A bump whose group has no registered route is ignored by the poller. Both the routed and
        // bumped groups are unique per invocation to avoid cross-test interference on the shared database.
        private void RunUnroutedGroupIgnored(DatabaseType databaseType)
        {
            string routedGroup = $"PollRouted_{Guid.NewGuid():N}";
            string unroutedGroup = $"PollUnrouted_{Guid.NewGuid():N}";
            var router = new CacheNotifyRouter();
            var evicted = new List<string>();
            router.Register(routedGroup, (_, entity) => evicted.Add(entity));

            var session = NewSession(databaseType, router);

            session.Poll(); // baseline

            Touch(databaseType, $"{unroutedGroup}:{Guid.NewGuid():N}");

            session.Poll(); // delta sees the row but no route matches its group
            Assert.Empty(evicted);
        }

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("SQL Server：poller 首輪不 evict、bump 後 evict 一次、再輪冪等")]
        public void Poller_SqlServer_Lifecycle() => RunPollerLifecycle(DatabaseType.SQLServer);

        [DbFact(DatabaseType.PostgreSQL)]
        [DisplayName("PostgreSQL：poller 首輪不 evict、bump 後 evict 一次、再輪冪等")]
        public void Poller_PostgreSQL_Lifecycle() => RunPollerLifecycle(DatabaseType.PostgreSQL);

        [DbFact(DatabaseType.MySQL)]
        [DisplayName("MySQL：poller 首輪不 evict、bump 後 evict 一次、再輪冪等")]
        public void Poller_MySQL_Lifecycle() => RunPollerLifecycle(DatabaseType.MySQL);

        [DbFact(DatabaseType.Oracle)]
        [DisplayName("Oracle：poller 首輪不 evict、bump 後 evict 一次、再輪冪等")]
        public void Poller_Oracle_Lifecycle() => RunPollerLifecycle(DatabaseType.Oracle);

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("SQL Server：未註冊路由的 group 不被 evict")]
        public void Poller_SqlServer_UnroutedIgnored() => RunUnroutedGroupIgnored(DatabaseType.SQLServer);

        [DbFact(DatabaseType.PostgreSQL)]
        [DisplayName("PostgreSQL：未註冊路由的 group 不被 evict")]
        public void Poller_PostgreSQL_UnroutedIgnored() => RunUnroutedGroupIgnored(DatabaseType.PostgreSQL);

        [DbFact(DatabaseType.MySQL)]
        [DisplayName("MySQL：未註冊路由的 group 不被 evict")]
        public void Poller_MySQL_UnroutedIgnored() => RunUnroutedGroupIgnored(DatabaseType.MySQL);

        [DbFact(DatabaseType.Oracle)]
        [DisplayName("Oracle：未註冊路由的 group 不被 evict")]
        public void Poller_Oracle_UnroutedIgnored() => RunUnroutedGroupIgnored(DatabaseType.Oracle);
    }
}
