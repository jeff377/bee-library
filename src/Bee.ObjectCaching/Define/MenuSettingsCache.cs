using Bee.Definition;
using Bee.Definition.Settings;
using Bee.Definition.Storage;

namespace Bee.ObjectCaching.Define
{
    /// <summary>
    /// Menu definition cache.
    /// </summary>
    public class MenuSettingsCache : ObjectCache<MenuSettings>
    {
        private readonly IDefineStorage _storage;

        /// <summary>
        /// Initializes a new <see cref="MenuSettingsCache"/>.
        /// </summary>
        /// <param name="storage">The define storage backing this cache.</param>
        /// <param name="paths">Retained for constructor symmetry with the other define caches; the monitored file paths come from <paramref name="storage"/>. Still validated as non-null.</param>
        /// <param name="cachePrefix">Per-owner cache namespace (see <see cref="ObjectCache{T}"/>).</param>
        public MenuSettingsCache(IDefineStorage storage, PathOptions paths, string cachePrefix = "") : base(cachePrefix)
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
            var changeSource = _storage.GetChangeSource(DefineType.MenuSettings);
            policy.ChangeMonitorFilePaths = changeSource.FilePaths;
            policy.ChangeNotifyKey = changeSource.NotifyKey;
            return policy;
        }

        /// <summary>
        /// Creates an instance of the menu definition.
        /// </summary>
        /// <remarks>
        /// A host with no menu definition yields an empty menu rather than <c>null</c>, so a shell
        /// asking for the menu always gets a usable object and simply renders nothing.
        /// </remarks>
        protected override MenuSettings? CreateInstance()
        {
            return _storage.GetMenuSettings() ?? new MenuSettings();
        }
    }
}
