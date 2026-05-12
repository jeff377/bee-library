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
        /// <param name="cachePrefix">Per-owner cache namespace (see <see cref="ObjectCache{T}"/>).</param>
        public DbCategorySettingsCache(IDefineStorage storage, string cachePrefix = "") : base(cachePrefix)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        }

        /// <summary>
        /// Gets the cache item expiration policy.
        /// </summary>
        protected override CacheItemPolicy GetPolicy()
        {
            var policy = new CacheItemPolicy(CacheTimeKind.SlidingTime, 20);
            if (_storage is FileDefineStorage)
                policy.ChangeMonitorFilePaths = new string[] { DefinePathInfo.GetDbCategorySettingsFilePath() };
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
