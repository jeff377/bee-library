namespace Bee.Cache
{
    /// <summary>
    /// Base class for single-object caches.
    /// </summary>
    public abstract class ObjectCache<T>
    {
        #region 建構函式

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectCache{T}"/> class.
        /// </summary>
        public ObjectCache()
        { }

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
            return typeof(T).Name;
        }

        /// <summary>
        /// Creates an instance of the cached object.
        /// </summary>
        protected virtual T CreateInstance()
        {
            return default;
        }

        /// <summary>
        /// Gets the cached object.
        /// </summary>
        public virtual T Get()
        {
            // Get the cache key
            string key = GetKey();
            // Return the cached object if it already exists in the cache
            if (CacheInfo.Provider.Contains(key))
                return (T)CacheInfo.Provider.Get(key);

            // Create and insert the object into the cache, then return it
            var value = CreateInstance();
            if (value != null)
            {
                CacheInfo.Provider.Set(key, value, GetPolicy());
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
            CacheInfo.Provider.Set(key, value, GetPolicy());
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
