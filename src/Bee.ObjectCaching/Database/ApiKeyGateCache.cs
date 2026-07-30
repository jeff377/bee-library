using Bee.Definition;
using Bee.Definition.Security;

namespace Bee.ObjectCaching.Database
{
    /// <summary>
    /// Whether the API key gate is in force for this deployment. A single-entry cache over
    /// <see cref="ICacheDataSourceProvider.GetApiKeyGateState"/>, so the question is not asked of the
    /// database on every request.
    /// </summary>
    /// <remarks>
    /// Shares the cache group of <see cref="ApiKeyInfo"/>: issuing, disabling or deleting a key has
    /// to invalidate this entry as well as the key's own, and one group keeps both notify keys in the
    /// same namespace. Missing that invalidation is the most likely bootstrap surprise — the first
    /// key gets issued and the deployment appears to stay ungated until the entry lapses.
    /// <para>
    /// WARNING: a database failure must NOT be cached. The load path lets the exception propagate
    /// instead of returning a not-in-force state, so nothing is stored and the caller fails closed;
    /// caching a failure would freeze the gate open for a whole lifetime after one blip.
    /// </para>
    /// </remarks>
    public class ApiKeyGateCache : KeyObjectCache<ApiKeyGateState>
    {
        private readonly Func<ICacheDataSourceProvider>? _dataSource;

        /// <summary>
        /// Initializes a new <see cref="ApiKeyGateCache"/> without a data source, leaving
        /// <see cref="KeyObjectCache{T}.Set(T)"/> as the only way in.
        /// </summary>
        /// <param name="cachePrefix">Per-owner cache namespace (see <see cref="KeyObjectCache{T}"/>).</param>
        public ApiKeyGateCache(string cachePrefix = "") : this(null, cachePrefix) { }

        /// <summary>
        /// Initializes a new <see cref="ApiKeyGateCache"/> bound to a data source.
        /// </summary>
        /// <param name="dataSource">
        /// Lazy accessor for the cache data source; <c>null</c> disables read-through.
        /// </param>
        /// <param name="cachePrefix">Per-owner cache namespace (see <see cref="KeyObjectCache{T}"/>).</param>
        /// <remarks>
        /// WARNING: <paramref name="dataSource"/> must stay a factory. See
        /// <see cref="CompanyInfoCache"/> for the construction cycle that resolving it eagerly would
        /// close.
        /// </remarks>
        internal ApiKeyGateCache(Func<ICacheDataSourceProvider>? dataSource, string cachePrefix)
            : base(cachePrefix)
        {
            _dataSource = dataSource;
        }

        /// <summary>
        /// Gets the cache group, shared with <see cref="ApiKeyInfo"/> so key changes and gate changes
        /// are notified under one group.
        /// </summary>
        public override string CacheGroup => nameof(ApiKeyInfo);

        /// <summary>
        /// Gets the gate state, loading it on a miss.
        /// </summary>
        public ApiKeyGateState? GetState()
        {
            return Get(ApiKeyGateState.CacheKey);
        }

        /// <summary>
        /// Removes the cached gate state, so the next read reloads it.
        /// </summary>
        public void RemoveState()
        {
            Remove(ApiKeyGateState.CacheKey);
        }

        /// <summary>
        /// Loads the gate state from the data source.
        /// </summary>
        /// <param name="key">Always <see cref="ApiKeyGateState.CacheKey"/>.</param>
        protected override ApiKeyGateState? CreateInstance(string key)
        {
            return _dataSource?.Invoke().GetApiKeyGateState();
        }

        /// <summary>
        /// Returns an absolute expiration policy, matching <see cref="ApiKeyCache"/>.
        /// </summary>
        /// <param name="key">Always <see cref="ApiKeyGateState.CacheKey"/>.</param>
        protected override CacheItemPolicy GetPolicy(string key)
        {
            return new CacheItemPolicy(CacheTimeKind.AbsoluteTime, ApiKeyCache.AbsoluteMinutes);
        }
    }
}
