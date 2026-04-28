using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.FileProviders;

namespace Bee.ObjectCaching.Providers
{
    /// <summary>
    /// Cache provider implementation backed by <see cref="IMemoryCache"/>.
    /// </summary>
    public class MemoryCacheProvider : ICacheProvider, IDisposable
    {
        private readonly MemoryCache _memoryCache;
        private readonly List<PhysicalFileProvider> _fileProviders = [];
        private readonly object _fileProvidersLock = new();
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="MemoryCacheProvider"/> class
        /// using a dedicated <see cref="MemoryCache"/> with default options.
        /// </summary>
        public MemoryCacheProvider()
            : this(new MemoryCache(new MemoryCacheOptions()))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MemoryCacheProvider"/> class with the specified <see cref="MemoryCache"/>.
        /// </summary>
        /// <param name="memoryCache">The memory cache instance to use.</param>
        public MemoryCacheProvider(MemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
        }

        /// <summary>
        /// Normalizes the cache key for case-insensitive comparison.
        /// </summary>
        /// <param name="key">The original key.</param>
        private static string GetCacheKey(string key)
        {
            return key.ToLowerInvariant();
        }

        /// <summary>
        /// Determines whether a cache entry with the specified key exists in the cache.
        /// </summary>
        /// <param name="key">The cache key.</param>
        public bool Contains(string key)
        {
            return _memoryCache.TryGetValue(GetCacheKey(key), out _);
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
            var options = CreateEntryOptions(policy);
            _memoryCache.Set(cacheKey, value, options);
        }

        /// <summary>
        /// Returns the cache entry for the specified key, or <c>null</c> if the key is not present.
        /// </summary>
        /// <param name="key">The cache key.</param>
        public object? Get(string key)
        {
            return _memoryCache.Get(GetCacheKey(key));
        }

        /// <summary>
        /// Removes the cache entry with the specified key.
        /// </summary>
        /// <param name="key">The cache key.</param>
        public void Remove(string key)
        {
            _memoryCache.Remove(GetCacheKey(key));
        }

        /// <summary>
        /// Returns the total number of cache entries in the cache.
        /// </summary>
        public long GetCount()
        {
            return _memoryCache.Count;
        }

        /// <summary>
        /// Maps a Bee.NET <see cref="CacheItemPolicy"/> to a <see cref="MemoryCacheEntryOptions"/>,
        /// translating absolute / sliding expirations and file watch tokens.
        /// </summary>
        private MemoryCacheEntryOptions CreateEntryOptions(CacheItemPolicy policy)
        {
            var options = new MemoryCacheEntryOptions();
            if (policy.AbsoluteExpiration != DateTimeOffset.MaxValue)
                options.AbsoluteExpiration = policy.AbsoluteExpiration;
            if (policy.SlidingExpiration != TimeSpan.Zero)
                options.SlidingExpiration = policy.SlidingExpiration;

            if (policy.ChangeMonitorFilePaths != null)
            {
                foreach (var path in policy.ChangeMonitorFilePaths)
                {
                    var directory = Path.GetDirectoryName(path);
                    var fileName = Path.GetFileName(path);
                    if (string.IsNullOrEmpty(directory) || string.IsNullOrEmpty(fileName))
                        continue;

                    var fileProvider = new PhysicalFileProvider(directory);
                    lock (_fileProvidersLock)
                    {
                        _fileProviders.Add(fileProvider);
                    }
                    options.AddExpirationToken(fileProvider.Watch(fileName));
                }
            }

            return options;
        }

        /// <summary>
        /// Releases the underlying <see cref="MemoryCache"/> and any file providers created for change monitoring.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases managed resources held by this provider.
        /// </summary>
        /// <param name="disposing"><c>true</c> if called from <see cref="Dispose()"/>; otherwise <c>false</c>.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;
            if (disposing)
            {
                _memoryCache.Dispose();
                lock (_fileProvidersLock)
                {
                    foreach (var fp in _fileProviders)
                        fp.Dispose();
                    _fileProviders.Clear();
                }
            }
            _disposed = true;
        }
    }
}
