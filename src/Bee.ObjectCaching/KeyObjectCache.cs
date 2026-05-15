using Bee.Base;

namespace Bee.ObjectCaching
{
    /// <summary>
    /// Base class for caching same-type objects accessed by key.
    /// </summary>
    public abstract class KeyObjectCache<T> where T : class
    {
        private readonly string _cachePrefix;

        #region 建構函式

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyObjectCache{T}"/> class.
        /// </summary>
        /// <param name="cachePrefix">
        /// Per-owner namespace prepended to <see cref="GetCacheKey"/>. Allows separate
        /// <see cref="CacheContainerService"/> instances (e.g. per-fixture test containers)
        /// to share the process-wide <see cref="CacheInfo.Provider"/> without colliding.
        /// Empty for the legacy non-prefixed path.
        /// </param>
        protected KeyObjectCache(string cachePrefix = "")
        {
            _cachePrefix = cachePrefix ?? string.Empty;
        }

        #endregion

        /// <summary>
        /// Sentinel stored in the cache to mark keys whose <see cref="CreateInstance"/>
        /// returned <c>null</c>. Distinguished from cached values by reference equality.
        /// </summary>
        private static readonly object MissMarker = new();

        /// <summary>
        /// Gets the cache item expiration policy.
        /// </summary>
        /// <param name="key">The member key.</param>
        protected virtual CacheItemPolicy GetPolicy(string key)
        {
            // Default: sliding expiration of 20 minutes
            var policy = new CacheItemPolicy(CacheTimeKind.SlidingTime, 20);
            return policy;
        }

        /// <summary>
        /// Gets the cache item expiration policy applied when <see cref="CreateInstance"/>
        /// returns <c>null</c> (negative caching).
        /// </summary>
        /// <remarks>
        /// Returning a non-null policy stores a sentinel marker for the key, so subsequent
        /// <see cref="Get"/> calls within the policy's lifetime return <c>null</c> without
        /// invoking <see cref="CreateInstance"/> again. This guards against repeated lookups
        /// of keys whose underlying data does not exist (cache penetration).
        /// Return <c>null</c> to disable negative caching for this cache type.
        /// </remarks>
        /// <param name="key">The member key.</param>
        protected virtual CacheItemPolicy? GetNegativePolicy(string key)
        {
            // Default: 5-minute absolute expiration, shorter than the positive policy so
            // real data created externally becomes visible within a bounded delay.
            return new CacheItemPolicy(CacheTimeKind.AbsoluteTime, 5);
        }

        /// <summary>
        /// Gets the cache key, normalized to lowercase to avoid case-sensitivity issues.
        /// </summary>
        /// <param name="key">The member key.</param>
        protected virtual string GetCacheKey(string key)
        {
            string suffix = (typeof(T).Name + "_" + key).ToLowerInvariant();
            return string.IsNullOrEmpty(_cachePrefix) ? suffix : _cachePrefix + "_" + suffix;
        }

        /// <summary>
        /// Creates an instance for the specified key.
        /// </summary>
        /// <param name="key">The member key.</param>
        protected virtual T? CreateInstance(string key)
        {
            return default;
        }

        /// <summary>
        /// Gets the object associated with the specified member key.
        /// </summary>
        /// <param name="key">The member key.</param>
        public virtual T? Get(string key)
        {
            string cacheKey = GetCacheKey(key);
            var cached = CacheInfo.Provider.Get(cacheKey);

            // Negative cache hit: short-circuit without invoking CreateInstance.
            if (ReferenceEquals(cached, MissMarker))
                return null;

            if (cached is T t)
                return t;

            var value = CreateInstance(key);
            if (value != null)
            {
                CacheInfo.Provider.Set(cacheKey, value, GetPolicy(key));
            }
            else if (GetNegativePolicy(key) is { } negPolicy)
            {
                CacheInfo.Provider.Set(cacheKey, MissMarker, negPolicy);
            }
            return value;
        }

        /// <summary>
        /// Stores the object in the cache under the specified key.
        /// </summary>
        /// <param name="key">The member key.</param>
        /// <param name="value">The object to store in the cache.</param>
        public virtual void Set(string key, T value)
        {
            string cacheKey = GetCacheKey(key);
            CacheInfo.Provider.Set(cacheKey, value!, GetPolicy(key));
        }

        /// <summary>
        /// Stores the object in the cache. The object must implement <see cref="IKeyObject"/> to provide the member key.
        /// </summary>
        /// <param name="value">The object to store in the cache.</param>
        public virtual void Set(T value)
        {
            if (value is IKeyObject c)
                Set(c.GetKey(), value);
            else
                throw new InvalidOperationException("The value does not implement the IKeyObject interface.");
        }

        /// <summary>
        /// Removes the entry with the specified key from the cache.
        /// </summary>
        /// <param name="key">The member key.</param>
        public virtual void Remove(string key)
        {
            string cacheKey = GetCacheKey(key);
            CacheInfo.Provider.Remove(cacheKey);
        }
    }
}
