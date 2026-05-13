using Bee.Base.Serialization;
using Bee.Definition;
using Bee.Definition.Settings;

namespace Bee.ObjectCaching.Define
{
    /// <summary>
    /// Program settings cache.
    /// </summary>
    public class ProgramSettingsCache : ObjectCache<ProgramSettings>
    {
        private readonly PathOptions _paths;

        /// <summary>
        /// Initializes a new <see cref="ProgramSettingsCache"/>.
        /// </summary>
        /// <param name="paths">Path options used to resolve the ProgramSettings.xml location.</param>
        /// <param name="cachePrefix">Per-owner cache namespace (see <see cref="ObjectCache{T}"/>).</param>
        public ProgramSettingsCache(PathOptions paths, string cachePrefix = "") : base(cachePrefix)
        {
            _paths = paths ?? throw new ArgumentNullException(nameof(paths));
        }

        /// <summary>
        /// Gets the cache item expiration policy.
        /// </summary>
        protected override CacheItemPolicy GetPolicy()
        {
            var policy = new CacheItemPolicy(CacheTimeKind.SlidingTime, 20);
            policy.ChangeMonitorFilePaths = new string[] { _paths.GetProgramSettingsFilePath() };
            return policy;
        }

        /// <summary>
        /// Creates an instance of the program settings.
        /// </summary>
        protected override ProgramSettings? CreateInstance()
        {
            string filePath = _paths.GetProgramSettingsFilePath();
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"The file {filePath} does not exist.");

            return XmlCodec.DeserializeFromFile<ProgramSettings>(filePath);
        }
    }
}
