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

        /// <summary>
        /// Initializes a new instance of <see cref="CurrencySettingsCache"/>.
        /// </summary>
        /// <param name="storage">The define storage backing this cache.</param>
        /// <param name="paths">Retained for constructor compatibility; the monitored file paths now come from <paramref name="storage"/>. Still validated as non-null.</param>
        /// <param name="cachePrefix">Per-owner cache namespace (see <see cref="ObjectCache{T}"/>).</param>
        public CurrencySettingsCache(IDefineStorage storage, PathOptions paths, string cachePrefix = "") : base(cachePrefix)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            ArgumentNullException.ThrowIfNull(paths);
        }

        /// <summary>
        /// Gets the cache item expiration policy.
        /// </summary>
        protected override CacheItemPolicy GetPolicy()
        {
            var policy = new CacheItemPolicy(CacheTimeKind.SlidingTime, 20);
            // The storage decides what to watch: file storage returns its backing file, the DB storage
            // returns nothing and invalidates through the cache-notify table instead.
            var changeSource = _storage.GetChangeSource(DefineType.CurrencySettings);
            policy.ChangeMonitorFilePaths = changeSource.FilePaths;
            policy.ChangeNotifyKey = changeSource.NotifyKey;
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
