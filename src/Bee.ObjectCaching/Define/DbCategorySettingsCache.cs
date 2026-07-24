using Bee.Definition;
using Bee.Definition.Settings;
using Bee.Definition.Storage;

namespace Bee.ObjectCaching.Define
{
    /// <summary>
    /// Database category settings cache.
    /// </summary>
    public class DbCategorySettingsCache : ObjectCache<DbCategorySettings>
    {
        private readonly IDefineStorage _storage;

        /// <summary>
        /// Initializes a new instance of <see cref="DbCategorySettingsCache"/>.
        /// </summary>
        /// <param name="storage">The define storage backing this cache.</param>
        /// <param name="paths">Retained for constructor compatibility; the monitored file paths now come from <paramref name="storage"/>. Still validated as non-null.</param>
        /// <param name="cachePrefix">Per-owner cache namespace (see <see cref="ObjectCache{T}"/>).</param>
        public DbCategorySettingsCache(IDefineStorage storage, PathOptions paths, string cachePrefix = "") : base(cachePrefix)
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
            var monitorPaths = _storage.GetChangeMonitorPaths(DefineType.DbCategorySettings);
            policy.ChangeMonitorFilePaths = monitorPaths.Length > 0 ? monitorPaths : null;
            return policy;
        }

        /// <summary>
        /// Creates an instance of the database category settings.
        /// </summary>
        protected override DbCategorySettings? CreateInstance()
        {
            return _storage.GetDbCategorySettings();
        }
    }
}
