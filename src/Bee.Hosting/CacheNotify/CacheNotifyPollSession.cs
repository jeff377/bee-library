using Bee.Db.CacheNotify;
using Bee.ObjectCaching;

namespace Bee.Hosting.CacheNotify
{
    /// <summary>
    /// The pollable core of the cache-notify mechanism for a single database: holds an in-memory
    /// mirror of <c>{cache_key → version}</c> and a high-water mark over <c>sys_update_time</c>,
    /// and on each <see cref="Poll"/> reads the incremental delta and routes evictions.
    /// </summary>
    /// <remarks>
    /// Separated from the hosted-service timer shell (<see cref="CacheNotifyPoller"/>) so the
    /// polling logic can be driven deterministically in tests. Not thread-safe: a single poller
    /// loop owns one instance and calls <see cref="Poll"/> sequentially.
    /// <para>
    /// Holds no SQL of its own — every statement comes from <see cref="ICacheNotifyReader"/> in
    /// <c>Bee.Db</c>, alongside the write side that produces the rows being polled.
    /// </para>
    /// <para>
    /// Incremental by <c>sys_update_time</c> (indexed) so a large multi-tenant notification table
    /// is not scanned in full every cycle. The first call only establishes the baseline high-water
    /// mark and evicts nothing — historical rows are stale for a just-started, empty local cache.
    /// Later calls read rows at or after <c>highWater - margin</c>; the overlap covers a long
    /// transaction whose update time precedes its commit visibility, and the version comparison
    /// keeps the overlap idempotent (a key absent from the mirror counts as version 0, so a key
    /// first seen after startup evicts once — a no-op when nothing is cached).
    /// </para>
    /// </remarks>
    public sealed class CacheNotifyPollSession
    {
        private readonly ICacheNotifyReader _reader;
        private readonly string _databaseId;
        private readonly TimeSpan _margin;

        private readonly Dictionary<string, long> _mirror = new(StringComparer.Ordinal);
        private DateTime _highWater;
        private bool _baselineTaken;

        /// <summary>
        /// Initializes a new <see cref="CacheNotifyPollSession"/>.
        /// </summary>
        /// <param name="databaseId">The database whose <c>st_cache_notify</c> table is polled.</param>
        /// <param name="reader">Reader for the notification table.</param>
        /// <param name="marginSeconds">The overlap safety margin in seconds.</param>
        public CacheNotifyPollSession(
            string databaseId,
            ICacheNotifyReader reader,
            int marginSeconds)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(databaseId);
            ArgumentNullException.ThrowIfNull(reader);

            _databaseId = databaseId;
            _reader = reader;
            _margin = TimeSpan.FromSeconds(marginSeconds < 0 ? 0 : marginSeconds);
        }

        /// <summary>
        /// Performs one polling cycle. The first call only takes the baseline cursor; later calls
        /// fetch the incremental delta and publish versions for keys whose version advanced.
        /// </summary>
        public void Poll()
        {
            if (!_baselineTaken)
            {
                _highWater = _reader.ReadBaseline(_databaseId);
                _baselineTaken = true;
                return;
            }

            DateTime maxSeen = _highWater;
            foreach (var change in _reader.ReadChangesSince(_databaseId, _highWater - _margin))
            {
                // Idempotent across the overlap window: act only on a strictly higher version.
                _mirror.TryGetValue(change.CacheKey, out long mirrored);
                if (change.Version > mirrored)
                {
                    // Publish the version; entries carrying this notify key expire on next read.
                    CacheInfo.NotifyVersions.SetVersion(change.CacheKey, change.Version);
                    _mirror[change.CacheKey] = change.Version;
                }

                if (change.UpdateTime > maxSeen) maxSeen = change.UpdateTime;
            }

            _highWater = maxSeen;
        }
    }
}
