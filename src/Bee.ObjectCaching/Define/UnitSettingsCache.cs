using Bee.Definition;
using Bee.Definition.Settings;
using Bee.Definition.Storage;

namespace Bee.ObjectCaching.Define
{
    /// <summary>
    /// System-level unit-of-measure master cache.
    /// </summary>
    public class UnitSettingsCache : ObjectCache<UnitSettings>
    {
        private readonly IDefineStorage _storage;

        /// <summary>
        /// Initializes a new instance of <see cref="UnitSettingsCache"/>.
        /// </summary>
        /// <param name="storage">The define storage backing this cache.</param>
        /// <param name="paths">Retained for constructor compatibility; the monitored file paths now come from <paramref name="storage"/>. Still validated as non-null.</param>
        /// <param name="cachePrefix">Per-owner cache namespace (see <see cref="ObjectCache{T}"/>).</param>
        public UnitSettingsCache(IDefineStorage storage, PathOptions paths, string cachePrefix = "") : base(cachePrefix)
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
            var changeSource = _storage.GetChangeSource(DefineType.UnitSettings);
            policy.ChangeMonitorFilePaths = changeSource.FilePaths;
            policy.ChangeNotifyKey = changeSource.NotifyKey;
            return policy;
        }

        /// <summary>
        /// Creates an instance of the unit master.
        /// </summary>
        protected override UnitSettings? CreateInstance()
        {
            return _storage.GetUnitSettings();
        }
    }
}
