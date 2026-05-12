namespace Bee.ObjectCaching
{
    /// <summary>
    /// Base class for single-object caches.
    /// </summary>
    public abstract class ObjectCache<T> where T : class
    {
        private readonly string _cachePrefix;

        #region 建構函式

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectCache{T}"/> class.
        /// </summary>
        /// <param name="cachePrefix">
        /// Per-owner namespace prepended to <see cref="GetKey"/>. Allows separate
        /// <see cref="CacheContainerService"/> instances (e.g. per-fixture test containers)
        /// to share the process-wide <see cref="CacheInfo.Provider"/> without colliding.
        /// Empty for the legacy non-prefixed path.
        /// </param>
        protected ObjectCache(string cachePrefix = "")
        {
            _cachePrefix = cachePrefix ?? string.Empty;
        }

        #endregion

        /// <summary>
        /// Gets the cache item expiration policy.
        /// </summary>
        protected virtual CacheItemPolicy GetPolicy()
        {
            // Default: sliding expiration of 20 minutes
            var policy = new CacheItemPolicy(CacheTimeKind.SlidingTime, 20);
            return policy;
        }

        /// <summary>
        /// Gets the cache key.
        /// </summary>
        protected virtual string GetKey()
        {
            return string.IsNullOrEmpty(_cachePrefix)
                ? typeof(T).Name
                : _cachePrefix + "_" + typeof(T).Name;
        }

        /// <summary>
        /// Creates an instance of the cached object.
        /// </summary>
        protected virtual T? CreateInstance()
        {
            return default;
        }

        /// <summary>
        /// Gets the cached object.
        /// </summary>
        public virtual T? Get()
        {
            // Get the cache key
            string key = GetKey();
            // Return the cached object if it already exists in the cache
            if (CacheInfo.Provider.Get(key) is T cached)
                return cached;

            // Create and insert the object into the cache, then return it
            var value = CreateInstance();
            if (value != null)
            {
                CacheInfo.Provider.Set(key, value!, GetPolicy());
            }
            return value;
        }

        /// <summary>
        /// Stores the object in the cache.
        /// </summary>
        /// <param name="value">The object to store in the cache.</param>
        public virtual void Set(T value)
        {
            string key = GetKey();
            CacheInfo.Provider.Set(key, value!, GetPolicy());
        }

        /// <summary>
        /// Removes the object from the cache.
        /// </summary>
        public virtual void Remove()
        {
            string key = GetKey();
            CacheInfo.Provider.Remove(key);
        }
    }
}
