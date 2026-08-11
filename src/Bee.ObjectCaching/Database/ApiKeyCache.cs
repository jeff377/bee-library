using Bee.Definition;
using Bee.Definition.Security;

namespace Bee.ObjectCaching.Database
{
    /// <summary>
    /// Issued API keys, keyed by <c>sys_id</c>. Reads through to
    /// <see cref="ICacheDataSourceProvider.GetApiKey"/> on a miss.
    /// </summary>
    /// <remarks>
    /// WARNING: The key is whatever <c>sys_id</c> the caller put in its <c>X-Api-Key</c> header, and
    /// the gate runs on every request — so every negative entry in this cache is attacker-chosen.
    /// <para>
    /// The class used to justify the inherited negative policy as "what keeps a caller probing random
    /// identifiers from reaching the database on every attempt". That reasoning does not hold:
    /// <b>distinct</b> identifiers each reach the database exactly once whether or not misses are
    /// cached, so against a caller probing random ids the negative cache buys nothing and costs one
    /// entry per probe. What it genuinely saves is the <b>repeated</b> probe of the <b>same</b> id —
    /// a misconfigured client retrying with a revoked key — which is a much narrower case.
    /// </para>
    /// <para>
    /// It is therefore kept, but at the shortest lifetime the policy type expresses. The residual is
    /// stated rather than hidden: steady-state memory is bounded by probe rate × <see cref="NegativeMinutes"/>,
    /// and the underlying cache has no size limit. Probe traffic belongs behind a rate limit at the
    /// edge; this cache is not the place to solve it.
    /// </para>
    /// </remarks>
    public class ApiKeyCache : KeyObjectCache<ApiKeyInfo>
    {
        /// <summary>
        /// Absolute lifetime, in minutes, of a cached key.
        /// </summary>
        public const int AbsoluteMinutes = 60;

        /// <summary>
        /// Lifetime, in minutes, of a cached miss. Deliberately the shortest the policy type allows —
        /// see the type's remarks for why the inherited five minutes was the wrong trade here.
        /// </summary>
        public const int NegativeMinutes = 1;

        private readonly Func<ICacheDataSourceProvider>? _dataSource;

        /// <summary>
        /// Initializes a new <see cref="ApiKeyCache"/> without a data source, leaving
        /// <see cref="KeyObjectCache{T}.Set(T)"/> as the only way in.
        /// </summary>
        /// <param name="cachePrefix">Per-owner cache namespace (see <see cref="KeyObjectCache{T}"/>).</param>
        public ApiKeyCache(string cachePrefix = "") : this(null, cachePrefix) { }

        /// <summary>
        /// Initializes a new <see cref="ApiKeyCache"/> bound to a data source.
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
        internal ApiKeyCache(Func<ICacheDataSourceProvider>? dataSource, string cachePrefix)
            : base(cachePrefix)
        {
            _dataSource = dataSource;
        }

        /// <summary>
        /// Gets the negative (miss-marker) policy: one minute rather than the inherited five.
        /// </summary>
        /// <param name="key">The API key identifier (unused).</param>
        protected override CacheItemPolicy? GetNegativePolicy(string key)
            => new CacheItemPolicy(CacheTimeKind.AbsoluteTime, NegativeMinutes);

        /// <summary>
        /// Loads the key with the specified identifier from the data source.
        /// </summary>
        /// <param name="key">The key identifier (<c>sys_id</c>).</param>
        protected override ApiKeyInfo? CreateInstance(string key)
        {
            return _dataSource?.Invoke().GetApiKey(key);
        }

        /// <summary>
        /// Returns an absolute expiration policy rather than the sliding default.
        /// </summary>
        /// <param name="key">The key identifier.</param>
        /// <remarks>
        /// IMPORTANT: this is a backstop, not the revocation mechanism, and it is not what enforces
        /// expiry either. A key's <c>expired_at</c> is compared against the current time on every
        /// validation, so an expiry takes effect immediately however long the entry stays cached.
        /// What an absolute lifetime bounds is the damage when the cache-notify chain breaks:
        /// revoking a key relies on that notification, and under the sliding default a busy key
        /// would keep renewing its entry and stay valid indefinitely. An absolute lifetime caps that
        /// exposure at one interval.
        /// <para>
        /// The same property is why a brief database outage does not take the API down: keys already
        /// cached keep serving. Cache-notify remains the only immediate revocation path, so its
        /// reliability has to stay covered by tests.
        /// </para>
        /// </remarks>
        protected override CacheItemPolicy GetPolicy(string key)
        {
            return new CacheItemPolicy(CacheTimeKind.AbsoluteTime, AbsoluteMinutes);
        }
    }
}
