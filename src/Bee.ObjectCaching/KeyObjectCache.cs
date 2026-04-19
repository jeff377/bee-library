using Bee.Base;

namespace Bee.ObjectCaching
{
    /// <summary>
    /// Base class for caching same-type objects accessed by key.
    /// </summary>
    public abstract class KeyObjectCache<T> where T : class
    {
        #region 建構函式

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyObjectCache{T}"/> class.
        /// </summary>
        protected KeyObjectCache()
        { }

        #endregion

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
        /// Gets the cache key, normalized to lowercase to avoid case-sensitivity issues.
        /// </summary>
        /// <param name="key">The member key.</param>
        protected virtual string GetCacheKey(string key)
        {
            return (typeof(T).Name + "_" + key).ToLowerInvariant();
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
            // Get the cache key
            string cacheKey = GetCacheKey(key);
            // Return the cached object if it already exists in the cache
            if (CacheInfo.Provider.Contains(cacheKey))
                return (T)CacheInfo.Provider.Get(cacheKey);

            // Create and insert the object into the cache, then return it
            var value = CreateInstance(key);
            if (value != null)
            {
                CacheInfo.Provider.Set(cacheKey, value!, GetPolicy(key));
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
