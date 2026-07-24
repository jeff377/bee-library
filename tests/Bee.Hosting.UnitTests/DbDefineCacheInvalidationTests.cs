using System.ComponentModel;
using Bee.Db;
using Bee.Db.CacheNotify;
using Bee.Db.Manager;
using Bee.Db.Storage;
using Bee.Definition.Database;
using Bee.Definition.Forms;
using Bee.Hosting.CacheNotify;
using Bee.ObjectCaching;
using Bee.Tests.Shared;

namespace Bee.Hosting.UnitTests
{
    /// <summary>
    /// End-to-end test of definition-cache invalidation under DB storage: a definition saved by one
    /// "node" (a separate <see cref="DbDefineStorage"/> over the same database) is observed by another
    /// node's <see cref="CacheNotifyPollSession"/> through the notification table and evicted via the
    /// cache container's convention dispatch, after which the cache reloads the new version from the DB.
    /// Skips when the dialect's <c>BEE_TEST_CONNSTR_*</c> env var is unset.
    /// </summary>
    public class DbDefineCacheInvalidationTests : IClassFixture<SharedDbFixture>
    {
        private readonly SharedDbFixture _fx;

        public DbDefineCacheInvalidationTests(SharedDbFixture fx) { _fx = fx; }

        private void RunCrossNodeInvalidation(DatabaseType databaseType)
        {
            var connectionManager = _fx.GetRequiredService<IDbConnectionManager>();
            var cacheNotify = _fx.GetRequiredService<ICacheNotifyService>();
            var dbAccessFactory = _fx.GetRequiredService<IDbAccessFactory>();
            var databaseId = TestDbConventions.GetDatabaseId(databaseType);

            // Node A: a cache container backed by DB storage, with its own cache namespace, plus its
            // own poll session over the same database.
            string nodeAPrefix = "nodeA_" + Guid.NewGuid().ToString("N");
            var storageA = new DbDefineStorage(connectionManager, cacheNotify, databaseId);
            var containerA = new CacheContainerService(storageA, _fx.PathOptions, nodeAPrefix);
            var session = new CacheNotifyPollSession(databaseId, dbAccessFactory, marginSeconds: 5);

            string progId = "E2E_" + Guid.NewGuid().ToString("N");

            // Node A writes v1 and caches it (the cache loads it from the DB through the storage).
            storageA.SaveFormSchema(new FormSchema(progId, "v1"));
            Assert.Equal("v1", containerA.FormSchema.Get(progId)!.DisplayName);

            // Establish the poll baseline, then a second poll so this key enters the version mirror
            // (the first post-baseline sighting evicts-as-new and reloads the same v1).
            session.Poll(); // baseline cursor only
            session.Poll(); // mirror now tracks this key at its current version
            Assert.Equal("v1", containerA.FormSchema.Get(progId)!.DisplayName);

            // Node B (a separate storage instance over the same database) writes v2, bumping the
            // notification row in the same transaction.
            var storageB = new DbDefineStorage(connectionManager, cacheNotify, databaseId);
            storageB.SaveFormSchema(new FormSchema(progId, "v2"));

            // Node A's next poll sees the higher version and publishes it; the cached entry carries
            // that notify key, so it expires on the next read, which reloads v2 from the database.
            session.Poll();
            Assert.Equal("v2", containerA.FormSchema.Get(progId)!.DisplayName);
        }

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("SQL Server：跨節點 DbDefineStorage 存定義 → poller evict → 重載新版")]
        public void CrossNode_SqlServer() => RunCrossNodeInvalidation(DatabaseType.SQLServer);

        [DbFact(DatabaseType.PostgreSQL)]
        [DisplayName("PostgreSQL：跨節點 DbDefineStorage 存定義 → poller evict → 重載新版")]
        public void CrossNode_PostgreSQL() => RunCrossNodeInvalidation(DatabaseType.PostgreSQL);
    }
}
