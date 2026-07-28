namespace Bee.Db.CacheNotify
{
    /// <summary>
    /// Read side of the cache-notify mechanism: queries the <c>st_cache_notify</c> table that
    /// <see cref="ICacheNotifyService"/> writes to, so a poller can observe version bumps made by
    /// this or any other process / node.
    /// </summary>
    /// <remarks>
    /// Lives alongside the write side rather than in the hosting layer because the statements it
    /// issues diverge across all five dialects (server-time expression, timestamp cast, literal
    /// format) and belong with the rest of the provider-aware SQL.
    /// </remarks>
    public interface ICacheNotifyReader
    {
        /// <summary>
        /// Reads the baseline cursor for a fresh poller: <c>max(sys_update_time)</c>, falling back to
        /// the database server's current time when the table is empty.
        /// </summary>
        /// <param name="databaseId">The database whose notification table is polled.</param>
        DateTime ReadBaseline(string databaseId);

        /// <summary>
        /// Reads every notification row whose <c>sys_update_time</c> is at or after
        /// <paramref name="threshold"/>.
        /// </summary>
        /// <remarks>
        /// The <c>&gt;=</c> comparison deliberately overlaps rows the caller has already seen; the
        /// caller's version comparison keeps that overlap idempotent while covering a long
        /// transaction whose update time precedes its commit visibility.
        /// </remarks>
        /// <param name="databaseId">The database whose notification table is polled.</param>
        /// <param name="threshold">The inclusive lower bound on <c>sys_update_time</c>.</param>
        IReadOnlyList<CacheNotifyChange> ReadChangesSince(string databaseId, DateTime threshold);
    }
}
