using Bee.Base.Serialization;
using Bee.Definition;
using Bee.Definition.Settings;

namespace Bee.ObjectCaching.Define
{
    /// <summary>
    /// System settings cache.
    /// </summary>
    public class SystemSettingsCache : ObjectCache<SystemSettings>
    {
        private readonly PathOptions _paths;

        /// <summary>
        /// Initializes a new <see cref="SystemSettingsCache"/>.
        /// </summary>
        /// <param name="paths">Path options used to resolve the SystemSettings.xml location.</param>
        /// <param name="cachePrefix">Per-owner cache namespace (see <see cref="ObjectCache{T}"/>).</param>
        public SystemSettingsCache(PathOptions paths, string cachePrefix = "") : base(cachePrefix)
        {
            _paths = paths ?? throw new ArgumentNullException(nameof(paths));
        }

        /// <summary>
        /// Gets the cache item expiration policy.
        /// </summary>
        protected override CacheItemPolicy GetPolicy()
        {
            var policy = new CacheItemPolicy(CacheTimeKind.SlidingTime, 20);
            policy.ChangeMonitorFilePaths = new string[] { _paths.GetSystemSettingsFilePath() };
            return policy;
        }

        /// <summary>
        /// Creates an instance of the system settings.
        /// </summary>
        /// <returns>The system settings instance.</returns>
        protected override SystemSettings? CreateInstance()
        {
            string sFilePath = _paths.GetSystemSettingsFilePath();
            if (!File.Exists(sFilePath))
                throw new FileNotFoundException($"The file {sFilePath} does not exist.");

            return XmlCodec.DeserializeFromFile<SystemSettings>(sFilePath);
        }
    }
}
