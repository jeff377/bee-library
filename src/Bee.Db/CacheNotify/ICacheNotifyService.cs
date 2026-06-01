using System.Data.Common;
using Bee.Definition.Database;

namespace Bee.Db.CacheNotify
{
    /// <summary>
    /// Bumps the version of a logical cache key in the <c>st_cache_notify</c> notification
    /// table so that cache-invalidation pollers (running in the same or other processes /
    /// nodes) observe the change and evict the corresponding cached object on their next poll.
    /// </summary>
    /// <remarks>
    /// The bump <b>must</b> be committed within the same <see cref="DbTransaction"/> as the
    /// data change that motivated it. Committing the bump in a separate transaction creates a
    /// window where the poller observes the new version while the underlying data change is not
    /// yet visible — the reloaded value would be stale yet marked fresh, leaving the cache
    /// permanently inconsistent. Callers pass their existing write transaction explicitly to
    /// guarantee this ordering.
    /// </remarks>
    public interface ICacheNotifyService
    {
        /// <summary>
        /// Touches the notification row for <paramref name="cacheKey"/> within the given
        /// transaction: increments <c>sys_cache_version</c> by one (or sets it to 1 on first
        /// touch) and refreshes <c>sys_update_time</c> to the database server's current time.
        /// The version increment is computed atomically by the database under a row lock, so
        /// concurrent touches of the same key serialize without lost updates.
        /// </summary>
        /// <param name="cacheKey">
        /// The logical cache key, using the <c>"group:entity"</c> convention
        /// (e.g. <c>"OrgInfo:0001"</c>, <c>"SystemSettings:*"</c>).
        /// </param>
        /// <param name="transaction">
        /// The caller's active write transaction; the bump is bound to it and commits with it.
        /// </param>
        /// <param name="databaseType">
        /// The database type backing <paramref name="transaction"/>, used to build the
        /// dialect-specific UPSERT statement.
        /// </param>
        void Touch(string cacheKey, DbTransaction transaction, DatabaseType databaseType);

        /// <summary>
        /// Asynchronous counterpart of <see cref="Touch(string, DbTransaction, DatabaseType)"/>.
        /// </summary>
        /// <param name="cacheKey">The logical cache key, using the <c>"group:entity"</c> convention.</param>
        /// <param name="transaction">The caller's active write transaction; the bump is bound to it and commits with it.</param>
        /// <param name="databaseType">The database type backing <paramref name="transaction"/>.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        Task TouchAsync(string cacheKey, DbTransaction transaction, DatabaseType databaseType,
            CancellationToken cancellationToken = default);
    }
}
