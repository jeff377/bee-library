using Bee.Definition;
using Bee.Definition.Settings;
using Bee.Definition.Storage;

namespace Bee.ObjectCaching.Define
{
    /// <summary>
    /// System-level currency master cache.
    /// </summary>
    public class CurrencySettingsCache : ObjectCache<CurrencySettings>
    {
        private readonly IDefineStorage _storage;
        private readonly PathOptions _paths;

        /// <summary>
        /// Initializes a new instance of <see cref="CurrencySettingsCache"/>.
        /// </summary>
        /// <param name="storage">The define storage backing this cache.</param>
        /// <param name="paths">Path options used for file-change monitoring when <paramref name="storage"/> is a <see cref="FileDefineStorage"/>.</param>
        /// <param name="cachePrefix">Per-owner cache namespace (see <see cref="ObjectCache{T}"/>).</param>
        public CurrencySettingsCache(IDefineStorage storage, PathOptions paths, string cachePrefix = "") : base(cachePrefix)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _paths = paths ?? throw new ArgumentNullException(nameof(paths));
        }

        /// <summary>
        /// Gets the cache item expiration policy.
        /// </summary>
        protected override CacheItemPolicy GetPolicy()
        {
            var policy = new CacheItemPolicy(CacheTimeKind.SlidingTime, 20);
            // File storage uses file-watch for cross-process invalidation; the DB storage relies on
            // the cache-notify table instead, so no file path is monitored there.
            if (_storage is FileDefineStorage)
                policy.ChangeMonitorFilePaths = new string[] { _paths.GetCurrencySettingsFilePath() };
            return policy;
        }

        /// <summary>
        /// Creates an instance of the currency master.
        /// </summary>
        protected override CurrencySettings? CreateInstance()
        {
            return _storage.GetCurrencySettings();
        }
    }
}
