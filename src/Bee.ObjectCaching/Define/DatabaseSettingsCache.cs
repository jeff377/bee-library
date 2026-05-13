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

            // Raise the global database settings changed event
            GlobalEvents.RaiseDatabaseSettingsChanged();

            return settings;
        }
    }
}
