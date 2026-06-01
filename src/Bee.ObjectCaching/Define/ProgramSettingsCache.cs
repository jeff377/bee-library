using Bee.Definition;
using Bee.Definition.Settings;
using Bee.Definition.Storage;

namespace Bee.ObjectCaching.Define
{
    /// <summary>
    /// Program settings cache.
    /// </summary>
    public class ProgramSettingsCache : ObjectCache<ProgramSettings>
    {
        private readonly IDefineStorage _storage;
        private readonly PathOptions _paths;

        /// <summary>
        /// Initializes a new <see cref="ProgramSettingsCache"/>.
        /// </summary>
        /// <param name="storage">The define storage backing this cache.</param>
        /// <param name="paths">Path options used for file-change monitoring when <paramref name="storage"/> is a <see cref="FileDefineStorage"/>.</param>
        /// <param name="cachePrefix">Per-owner cache namespace (see <see cref="ObjectCache{T}"/>).</param>
        public ProgramSettingsCache(IDefineStorage storage, PathOptions paths, string cachePrefix = "") : base(cachePrefix)
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
                policy.ChangeMonitorFilePaths = new string[] { _paths.GetProgramSettingsFilePath() };
            return policy;
        }

        /// <summary>
        /// Creates an instance of the program settings.
        /// </summary>
        protected override ProgramSettings? CreateInstance()
        {
            return _storage.GetProgramSettings();
        }
    }
}
