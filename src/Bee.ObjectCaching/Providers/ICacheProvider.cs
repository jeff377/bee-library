namespace Bee.ObjectCaching.Providers
{
    /// <summary>
    /// Defines the contract for a cache provider, specifying the operations supported by the cache.
    /// </summary>
    public interface ICacheProvider
    {
        /// <summary>
        /// Determines whether a cache entry with the specified key exists in the cache.
        /// </summary>
        /// <param name="key">The cache key.</param>
        bool Contains(string key);

        /// <summary>
        /// Inserts a cache entry into the cache.
        /// </summary>
        /// <param name="key">The cache key.</param>
        /// <param name="value">The object to insert into the cache.</param>
        /// <param name="policy">The expiration policy for the cache entry.</param>
        void Set(string key, object value, CacheItemPolicy policy);

        /// <summary>
        /// Returns the cache entry for the specified key.
        /// </summary>
        /// <param name="key">The cache key.</param>
        object Get(string key);

        /// <summary>
        /// Removes the cache entry with the specified key.
        /// </summary>
        /// <param name="key">The cache key.</param>
        /// <returns>The removed cache entry, or null if the entry does not exist.</returns>
        object Remove(string key);

        /// <summary>
        /// Removes a specified percentage of cache entries from the cache.
        /// </summary>
        /// <param name="percent">The percentage of total cache entries to remove.</param>
        /// <returns>The number of cache entries removed.</returns>
        long Trim(int percent);

        /// <summary>
        /// Returns the total number of cache entries in the cache.
        /// </summary>
        long GetCount();

        /// <summary>
        /// Returns a collection of all keys currently in the cache.
        /// </summary>
        /// <returns>A collection of cache key strings.</returns>
        IEnumerable<string> GetAllKeys();
    }
}
