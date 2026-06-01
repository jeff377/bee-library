using System.Globalization;
using Bee.Db;
using Bee.ObjectCaching;
using Bee.ObjectCaching.CacheNotify;

namespace Bee.Hosting.CacheNotify
{
    /// <summary>
    /// The pollable core of the cache-notify mechanism for a single database: holds an in-memory
    /// mirror of <c>{cache_key → version}</c> and, on each <see cref="Poll"/>, reads the
    /// notification table and routes an eviction for every key whose version has advanced.
    /// </summary>
    /// <remarks>
    /// Separated from the hosted-service timer shell (<see cref="CacheNotifyPoller"/>) so the
    /// polling logic can be driven deterministically in tests. Not thread-safe: a single poller
    /// loop owns one instance and calls <see cref="Poll"/> sequentially.
    /// <para>
    /// The notification table is UPSERT-bounded (one row per cache key), so each poll reads the
    /// whole (small) table and decides evictions purely from the monotonic version column. This
    /// avoids any reliance on cross-dialect <c>DateTime</c> parameter binding and has no
    /// time-boundary gap to miss: a late-committing transaction simply becomes visible on a later
    /// poll, where its higher version triggers the eviction.
    /// </para>
    /// <para>
    /// The first call only seeds the mirror with current versions and evicts nothing — historical
    /// rows are stale for a just-started, empty local cache. Later calls evict a key only when its
    /// version is strictly greater than the mirrored value (a key absent from the mirror counts as
    /// version 0, so a key first seen after startup evicts once — a no-op when nothing is cached).
    /// </para>
    /// </remarks>
    public sealed class CacheNotifyPollSession
    {
        private const string TableName = "st_cache_notify";
        private const string KeyColumn = "sys_cache_key";
        private const string VersionColumn = "sys_cache_version";

        private readonly IDbAccessFactory _dbAccessFactory;
        private readonly string _databaseId;
        private readonly ICacheContainer _container;
        private readonly ICacheNotifyRouter _router;

        private readonly Dictionary<string, long> _mirror = new(StringComparer.Ordinal);
        private bool _baselineTaken;

        /// <summary>
        /// Initializes a new <see cref="CacheNotifyPollSession"/>.
        /// </summary>
        /// <param name="databaseId">The database whose <c>st_cache_notify</c> table is polled.</param>
        /// <param name="dbAccessFactory">Factory for the database access object.</param>
        /// <param name="container">The cache container eviction actions operate on.</param>
        /// <param name="router">The route table mapping cache groups to eviction actions.</param>
        public CacheNotifyPollSession(
            string databaseId,
            IDbAccessFactory dbAccessFactory,
            ICacheContainer container,
            ICacheNotifyRouter router)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(databaseId);
            ArgumentNullException.ThrowIfNull(dbAccessFactory);
            ArgumentNullException.ThrowIfNull(container);
            ArgumentNullException.ThrowIfNull(router);

            _databaseId = databaseId;
            _dbAccessFactory = dbAccessFactory;
            _container = container;
            _router = router;
        }

        /// <summary>
        /// Performs one polling cycle. The first call only seeds the version mirror; later calls
        /// route evictions for keys whose version advanced since the previous poll.
        /// </summary>
        public void Poll()
        {
            var dbAccess = _dbAccessFactory.Create(_databaseId);
            var databaseType = dbAccess.DatabaseType;

            string tbl = databaseType.QuoteIdentifier(TableName);
            string key = databaseType.QuoteIdentifier(KeyColumn);
            string ver = databaseType.QuoteIdentifier(VersionColumn);

            var table = dbAccess.ExecuteDataTable($"SELECT {key}, {ver} FROM {tbl}");
            if (table == null) return;

            bool seeding = !_baselineTaken;

            foreach (System.Data.DataRow row in table.Rows)
            {
                string cacheKey = Convert.ToString(row[0], CultureInfo.InvariantCulture) ?? string.Empty;
                if (cacheKey.Length == 0) continue;

                long version = Convert.ToInt64(row[1], CultureInfo.InvariantCulture);

                if (seeding)
                {
                    _mirror[cacheKey] = version;
                    continue;
                }

                _mirror.TryGetValue(cacheKey, out long mirrored);
                if (version > mirrored)
                {
                    _router.TryInvoke(_container, cacheKey);
                    _mirror[cacheKey] = version;
                }
            }

            _baselineTaken = true;
        }
    }
}
