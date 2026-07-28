namespace Bee.Db.CacheNotify
{
    /// <summary>
    /// One row read from the <c>st_cache_notify</c> table: a cache key with its current version
    /// and the naive (tz-less) update time recorded by the database server.
    /// </summary>
    /// <param name="CacheKey">The logical cache key, using the <c>"group:entity"</c> convention.</param>
    /// <param name="Version">The current version of the key; a poller acts only on a strictly higher value.</param>
    /// <param name="UpdateTime">The row's <c>sys_update_time</c>, used to advance the poller's high-water mark.</param>
    public readonly record struct CacheNotifyChange(string CacheKey, long Version, DateTime UpdateTime);
}
