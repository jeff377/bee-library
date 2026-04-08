using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using Bee.Core;

namespace Bee.ObjectCaching.Providers
{
    /// <summary>
    /// Cache provider implementation that uses <see cref="MemoryCache"/>.
    /// </summary>
    public class MemoryCacheProvider : ICacheProvider
    {
        private readonly MemoryCache _memoryCache;

        /// <summary>
        /// Initializes a new instance of the <see cref="MemoryCacheProvider"/> class using the default <see cref="MemoryCache"/>.
        /// </summary>
        public MemoryCacheProvider()
        {
            _memoryCache = MemoryCache.Default;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MemoryCacheProvider"/> class using the specified <see cref="MemoryCache"/>.
        /// </summary>
        /// <param name="memoryCache">The memory cache instance to use.</param>
        public MemoryCacheProvider(MemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
        }

        /// <summary>
        /// Gets the case-insensitive cache key.
        /// </summary>
        /// <param name="key">The original key.</param>
        private string GetCacheKey(string key)
        {
            return StrFunc.ToUpper(key);
        }

        /// <summary>
        /// Determines whether a cache entry with the specified key exists in the cache.
        /// </summary>
        /// <param name="key">The cache key.</param>
        public bool Contains(string key)
        {
            string cacheKey = GetCacheKey(key);
            return _memoryCache.Contains(cacheKey);
        }

        /// <summary>
        /// Inserts a cache entry into the cache.
        /// </summary>
        /// <param name="key">The cache key.</param>
        /// <param name="value">The object to insert into the cache.</param>
        /// <param name="policy">The expiration policy for the cache entry.</param>
        public void Set(string key, object value, CacheItemPolicy policy)
        {
            var cacheKey = GetCacheKey(key);
            var cacheItem = new CacheItem(cacheKey, value);
            var cachePolicy = CacheFunc.CreateCachePolicy(policy);
            _memoryCache.Set(cacheItem, cachePolicy);
        }

        /// <summary>
        /// Returns the cache entry for the specified key.
        /// </summary>
        /// <param name="key">The cache key.</param>
        public object Get(string key)
        {
            string cacheKey = GetCacheKey(key);
            return _memoryCache.Get(cacheKey);
        }

        /// <summary>
        /// Removes the cache entry with the specified key.
        /// </summary>
        /// <param name="key">The cache key.</param>
        /// <returns>The removed cache entry, or null if the entry does not exist.</returns>
        public object Remove(string key)
        {
            string cacheKey = GetCacheKey(key);
            return _memoryCache.Remove(cacheKey);
        }

        /// <summary>
        /// Removes a specified percentage of cache entries from the cache.
        /// </summary>
        /// <param name="percent">The percentage of total cache entries to remove.</param>
        /// <returns>The number of cache entries removed.</returns>
        public long Trim(int percent)
        {
            return _memoryCache.Trim(percent);
        }

        /// <summary>
        /// Returns the total number of cache entries in the cache.
        /// </summary>
        public long GetCount()
        {
            return _memoryCache.GetCount();
        }

        /// <summary>
        /// Returns a collection of all keys currently in the cache.
        /// </summary>
        /// <returns>A collection of cache key strings.</returns>
        public IEnumerable<string> GetAllKeys()
        {
            // MemoryCache may be modified by other threads during enumeration;
            // ToList() is recommended to take a snapshot first
            return _memoryCache.Select(item => item.Key).ToList();
        }
    }
}
