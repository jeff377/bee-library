using Bee.Base.Serialization;
using Bee.Definition;
using Bee.Definition.Settings;

namespace Bee.ObjectCaching.Define
{
    /// <summary>
    /// Permission model registry cache.
    /// </summary>
    public class PermissionModelsCache : ObjectCache<PermissionModels>
    {
        private readonly PathOptions _paths;

        /// <summary>
        /// Initializes a new <see cref="PermissionModelsCache"/>.
        /// </summary>
        /// <param name="paths">Path options used to resolve the PermissionModels.xml location.</param>
        /// <param name="cachePrefix">Per-owner cache namespace (see <see cref="ObjectCache{T}"/>).</param>
        public PermissionModelsCache(PathOptions paths, string cachePrefix = "") : base(cachePrefix)
        {
            _paths = paths ?? throw new ArgumentNullException(nameof(paths));
        }

        /// <summary>
        /// Gets the cache item expiration policy.
        /// </summary>
        protected override CacheItemPolicy GetPolicy()
        {
            var policy = new CacheItemPolicy(CacheTimeKind.SlidingTime, 20);
            policy.ChangeMonitorFilePaths = new string[] { _paths.GetPermissionModelsFilePath() };
            return policy;
        }

        /// <summary>
        /// Creates an instance of the permission model registry, rejecting an invalid
        /// registry at load time.
        /// </summary>
        /// <returns>The permission model registry instance.</returns>
        protected override PermissionModels? CreateInstance()
        {
            string filePath = _paths.GetPermissionModelsFilePath();
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"The file {filePath} does not exist.");

            var models = XmlCodec.DeserializeFromFile<PermissionModels>(filePath);
            if (models != null)
            {
                var errors = models.Validate();
                if (errors.Count > 0)
                    throw new InvalidOperationException(
                        $"PermissionModels validation failed: {string.Join("; ", errors)}");
            }
            return models;
        }
    }
}
