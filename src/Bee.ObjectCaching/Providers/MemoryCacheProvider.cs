using Microsoft.Extensions.Caching.Memory;

namespace Bee.ObjectCaching.Providers
{
    /// <summary>
    /// Cache provider implementation backed by <see cref="IMemoryCache"/>.
    /// </summary>
    public class MemoryCacheProvider : ICacheProvider, IDisposable
    {
        private readonly MemoryCache _memoryCache;
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
                    if (string.IsNullOrEmpty(path))
                        continue;
                    options.AddExpirationToken(new FileModificationToken(path));
                }
            }

            return options;
        }

        /// <summary>
        /// Releases the underlying <see cref="MemoryCache"/>.
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
                _memoryCache.Dispose();
            _disposed = true;
        }

        /// <summary>
        /// Lazy file-modification change token: compares current LastWriteTimeUtc against the
        /// snapshot taken at construction time. No background timer avoids the race condition
        /// where an immediately-firing polling timer evicts entries before they can be read.
        /// MemoryCache checks HasChanged on every TryGetValue call, so lazy detection is sufficient.
        /// </summary>
        private sealed class FileModificationToken : IChangeToken
        {
            private readonly string _filePath;
            private readonly DateTime _initialWriteTime;
            private volatile bool _hasChanged;

            public FileModificationToken(string filePath)
            {
                _filePath = filePath;
                _initialWriteTime = GetWriteTime(filePath);
            }

            private static DateTime GetWriteTime(string path)
            {
                try { return File.GetLastWriteTimeUtc(path); }
                catch { return DateTime.MinValue; }
            }

            public bool HasChanged
            {
                get
                {
                    if (_hasChanged) return true;
                    _hasChanged = GetWriteTime(_filePath) != _initialWriteTime;
                    return _hasChanged;
                }
            }

            public bool ActiveChangeCallbacks => false;

            public IDisposable RegisterChangeCallback(Action<object?> callback, object? state)
                => CancellationToken.None.Register(callback!, state);
        }
    }
}
