using Bee.Definition;
using Bee.Definition.Storage;
using Bee.ObjectCaching.Database;
using Bee.ObjectCaching.Define;

namespace Bee.ObjectCaching
{
    /// <summary>
    /// Default <see cref="ICacheContainer"/> implementation: holds singleton instances of
    /// the framework caches. Constructed once per host (registered as a Singleton in
    /// <c>AddBeeFramework</c>).
    /// </summary>
    /// <remarks>
    /// All caches receive the supplied <see cref="PathOptions"/> for change-monitor file
    /// paths; the storage-backed caches additionally receive the supplied
    /// <see cref="IDefineStorage"/>.
    /// </remarks>
    public sealed class CacheContainerService : ICacheContainer
    {
        /// <summary>
        /// Initializes a new <see cref="CacheContainerService"/> bound to the supplied storage.
        /// Uses empty <see cref="CachePrefix"/> by default so legacy bootstrap-then-DI flows
        /// share the process-wide <see cref="CacheInfo.Provider"/> key namespace across
        /// multiple container instances.
        /// </summary>
        /// <param name="storage">The define storage shared by storage-backed caches.</param>
        /// <param name="paths">Path options used by file-backed caches.</param>
        public CacheContainerService(IDefineStorage storage, PathOptions paths) : this(storage, paths, string.Empty) { }

        /// <summary>
        /// Initializes a new <see cref="CacheContainerService"/> with an explicit cache key
        /// prefix. Test fixtures use a unique prefix to achieve per-instance data isolation
        /// over the shared <see cref="CacheInfo.Provider"/>.
        /// </summary>
        /// <param name="storage">The define storage shared by storage-backed caches.</param>
        /// <param name="paths">Path options used by file-backed caches.</param>
        /// <param name="cachePrefix">Per-owner cache namespace; <see cref="string.Empty"/> means "share the legacy unprefixed namespace".</param>
        public CacheContainerService(IDefineStorage storage, PathOptions paths, string cachePrefix)
        {
            ArgumentNullException.ThrowIfNull(storage);
            ArgumentNullException.ThrowIfNull(paths);
            CachePrefix = cachePrefix ?? string.Empty;

            SystemSettings = new SystemSettingsCache(paths, CachePrefix);
            DatabaseSettings = new DatabaseSettingsCache(paths, CachePrefix);
            ProgramSettings = new ProgramSettingsCache(storage, paths, CachePrefix);
            PermissionModels = new PermissionModelsCache(paths, CachePrefix);
            DbCategorySettings = new DbCategorySettingsCache(storage, paths, CachePrefix);
            CurrencySettings = new CurrencySettingsCache(storage, paths, CachePrefix);
            UnitSettings = new UnitSettingsCache(storage, paths, CachePrefix);
            TableSchema = new TableSchemaCache(storage, paths, CachePrefix);
            FormSchema = new FormSchemaCache(storage, paths, CachePrefix);
            FormLayout = new FormLayoutCache(storage, paths, CachePrefix);
            LanguageResource = new LanguageResourceCache(storage, paths, CachePrefix);
            SessionInfo = new SessionInfoCache(CachePrefix);
            CompanyInfo = new CompanyInfoCache(CachePrefix);
            CompanyRolePermissions = new CompanyRolePermissionsCache(CachePrefix);
            DepartmentTree = new DepartmentTreeCache(CachePrefix);

            // Convention-based eviction dispatch: index every owned cache by its CacheGroup so the
            // poller can invalidate "group:entity" keys without a hand-maintained route table.
            // Adding a new cache above automatically makes it invalidatable — no extra registration.
            IEvictableCache[] caches =
            [
                SystemSettings, DatabaseSettings, ProgramSettings, DbCategorySettings, CurrencySettings, UnitSettings,
                TableSchema, FormSchema, FormLayout, LanguageResource, SessionInfo, CompanyInfo,
                PermissionModels, CompanyRolePermissions, DepartmentTree
            ];
            _evictableByGroup = new Dictionary<string, IEvictableCache>(StringComparer.OrdinalIgnoreCase);
            foreach (var cache in caches)
                _evictableByGroup[cache.CacheGroup] = cache;
        }

        private readonly Dictionary<string, IEvictableCache> _evictableByGroup;

        /// <summary>
        /// The namespace prefix used by every cache instance this container owns.
        /// Empty for the legacy unprefixed mode; non-empty when explicit isolation
        /// is required (e.g. per-fixture test containers).
        /// </summary>
        public string CachePrefix { get; }

        /// <inheritdoc/>
        public SystemSettingsCache SystemSettings { get; }

        /// <inheritdoc/>
        public DatabaseSettingsCache DatabaseSettings { get; }

        /// <inheritdoc/>
        public ProgramSettingsCache ProgramSettings { get; }

        /// <inheritdoc/>
        public PermissionModelsCache PermissionModels { get; }

        /// <inheritdoc/>
        public DbCategorySettingsCache DbCategorySettings { get; }

        /// <inheritdoc/>
        public CurrencySettingsCache CurrencySettings { get; }

        /// <inheritdoc/>
        public UnitSettingsCache UnitSettings { get; }

        /// <inheritdoc/>
        public TableSchemaCache TableSchema { get; }

        /// <inheritdoc/>
        public FormSchemaCache FormSchema { get; }

        /// <inheritdoc/>
        public FormLayoutCache FormLayout { get; }

        /// <inheritdoc/>
        public LanguageResourceCache LanguageResource { get; }

        /// <inheritdoc/>
        public SessionInfoCache SessionInfo { get; }

        /// <inheritdoc/>
        public CompanyInfoCache CompanyInfo { get; }

        /// <inheritdoc/>
        public CompanyRolePermissionsCache CompanyRolePermissions { get; }

        /// <inheritdoc/>
        public DepartmentTreeCache DepartmentTree { get; }

        /// <inheritdoc/>
        public bool TryEvict(string cacheKey)
        {
            if (string.IsNullOrEmpty(cacheKey)) return false;

            // Split on the first ':' into group + entity; the entity may itself contain ':'.
            int separator = cacheKey.IndexOf(':');
            if (separator <= 0) return false;

            string cacheGroup = cacheKey.Substring(0, separator);
            string entity = cacheKey.Substring(separator + 1);

            if (!_evictableByGroup.TryGetValue(cacheGroup, out var cache)) return false;
            cache.Evict(entity);
            return true;
        }
    }
}
