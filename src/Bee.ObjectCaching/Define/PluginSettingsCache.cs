using Bee.Definition;
using Bee.Definition.Settings;
using Bee.Definition.Storage;

namespace Bee.ObjectCaching.Define
{
    /// <summary>
    /// Business plugin binding cache.
    /// </summary>
    public class PluginSettingsCache : ObjectCache<PluginSettings>
    {
        private readonly IDefineStorage _storage;

        /// <summary>
        /// Initializes a new <see cref="PluginSettingsCache"/>.
        /// </summary>
        /// <param name="storage">The define storage backing this cache.</param>
        /// <param name="paths">Retained for constructor symmetry with the other define caches; the monitored file paths come from <paramref name="storage"/>. Still validated as non-null.</param>
        /// <param name="cachePrefix">Per-owner cache namespace (see <see cref="ObjectCache{T}"/>).</param>
        public PluginSettingsCache(IDefineStorage storage, PathOptions paths, string cachePrefix = "") : base(cachePrefix)
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
            var changeSource = _storage.GetChangeSource(DefineType.PluginSettings);
            policy.ChangeMonitorFilePaths = changeSource.FilePaths;
            policy.ChangeNotifyKey = changeSource.NotifyKey;
            return policy;
        }

        /// <summary>
        /// Creates an instance of the plugin bindings.
        /// </summary>
        /// <remarks>
        /// A deployment with no plugin definition yields an empty instance rather than <c>null</c>,
        /// so the save and delete pipelines can ask for the chain unconditionally and simply get
        /// nothing to run. Having no plugins is the normal state, not an error.
        /// </remarks>
        protected override PluginSettings? CreateInstance()
        {
            return _storage.GetPluginSettings() ?? new PluginSettings();
        }
    }
}
