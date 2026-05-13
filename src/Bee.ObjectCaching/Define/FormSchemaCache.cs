using Bee.Definition;
using Bee.Definition.Forms;
using Bee.Definition.Storage;

namespace Bee.ObjectCaching.Define
{
    /// <summary>
    /// Form schema definition cache.
    /// </summary>
    public class FormSchemaCache : KeyObjectCache<FormSchema>
    {
        private readonly IDefineStorage _storage;
        private readonly PathOptions _paths;

        /// <summary>
        /// Initializes a new instance of <see cref="FormSchemaCache"/>.
        /// </summary>
        /// <param name="storage">The define storage backing this cache.</param>
        /// <param name="paths">Path options used for file-change monitoring when <paramref name="storage"/> is a <see cref="FileDefineStorage"/>.</param>
        /// <param name="cachePrefix">Per-owner cache namespace (see <see cref="KeyObjectCache{T}"/>).</param>
        public FormSchemaCache(IDefineStorage storage, PathOptions paths, string cachePrefix = "") : base(cachePrefix)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _paths = paths ?? throw new ArgumentNullException(nameof(paths));
        }

        /// <summary>
        /// Gets the cache item expiration policy.
        /// </summary>
        /// <param name="key">The member key.</param>
        protected override CacheItemPolicy GetPolicy(string key)
        {
            // Program identifier
            string progId = key;
            // Default: sliding expiration of 20 minutes
            var policy = new CacheItemPolicy(CacheTimeKind.SlidingTime, 20);
            if (_storage is FileDefineStorage)
                policy.ChangeMonitorFilePaths = new string[] { _paths.GetFormSchemaFilePath(progId) };
            return policy;
        }

        /// <summary>
        /// Creates an instance of the form schema.
        /// </summary>
        /// <param name="key">The member key, which is the program identifier.</param>
        protected override FormSchema? CreateInstance(string key)
        {
            // Program identifier
            string progId = key;
            return _storage.GetFormSchema(progId);
        }
    }
}
