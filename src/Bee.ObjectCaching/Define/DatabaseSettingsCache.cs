using Bee.Base.Serialization;
using Bee.Definition;
using Bee.Definition.Settings;

namespace Bee.ObjectCaching.Define
{
    /// <summary>
    /// Database settings cache.
    /// </summary>
    public class DatabaseSettingsCache : ObjectCache<DatabaseSettings>
    {
        private readonly PathOptions _paths;

        /// <summary>
        /// 0 until the settings have been loaded once; 1 afterwards.
        /// </summary>
        private int _loadedOnce;

        /// <summary>
        /// Initializes a new <see cref="DatabaseSettingsCache"/>.
        /// </summary>
        /// <param name="paths">Path options used to resolve the DatabaseSettings.xml location.</param>
        /// <param name="cachePrefix">Per-owner cache namespace (see <see cref="ObjectCache{T}"/>).</param>
        public DatabaseSettingsCache(PathOptions paths, string cachePrefix = "") : base(cachePrefix)
        {
            _paths = paths ?? throw new ArgumentNullException(nameof(paths));
        }

        /// <summary>
        /// Gets the cache item expiration policy.
        /// </summary>
        protected override CacheItemPolicy GetPolicy()
        {
            var policy = new CacheItemPolicy(CacheTimeKind.SlidingTime, 20);
            policy.ChangeMonitorFilePaths = new string[] { _paths.GetDatabaseSettingsFilePath() };
            return policy;
        }

        /// <summary>
        /// Creates an instance of the database settings.
        /// </summary>
        protected override DatabaseSettings? CreateInstance()
        {
            string filePath = _paths.GetDatabaseSettingsFilePath();
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"The file {filePath} does not exist.");

            var settings = XmlCodec.DeserializeFromFile<DatabaseSettings>(filePath);

            // WARNING: Only a *re*load is a change. The first load is not, and announcing it as one
            // had a concrete cost: `DbConnectionManagerService` clears its connection cache on this
            // event, and it reaches this method from inside its own `GetOrAdd` value factory
            // (GetConnectionInfo → provider.Get() → here). Every first miss therefore wiped every
            // connection entry that had been built up to that point.
            //
            // The event still fires on a genuine reload, and it has to: `GetPolicy` puts a file
            // monitor on DatabaseSettings.xml, so an edit to that file evicts this entry and the
            // next read lands here. That is the *only* path by which an edited settings file reaches
            // the connection cache — moving the event to `SaveDatabaseSettings` would silently break
            // it, because a file edited outside the process never goes through Save.
            if (Interlocked.Exchange(ref _loadedOnce, 1) == 1)
            {
                GlobalEvents.RaiseDatabaseSettingsChanged();
            }

            return settings;
        }
    }
}
