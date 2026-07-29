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
            : this(storage, paths, cachePrefix, dataSource: null) { }

        /// <summary>
        /// Initializes a new <see cref="CacheContainerService"/> whose database-backed caches read
        /// through to the supplied data source on a miss.
        /// </summary>
        /// <param name="storage">The define storage shared by storage-backed caches.</param>
        /// <param name="paths">Path options used by file-backed caches.</param>
        /// <param name="cachePrefix">Per-owner cache namespace; <see cref="string.Empty"/> means "share the legacy unprefixed namespace".</param>
        /// <param name="dataSource">
        /// Lazy accessor for the cache data source; <c>null</c> leaves the database-backed caches
        /// without read-through, populated only through their <c>Set</c> methods.
        /// </param>
        /// <remarks>
        /// WARNING: <paramref name="dataSource"/> must stay a factory. Resolving the provider here
        /// closes the dependency cycle <c>ICacheContainer</c> to <c>ICacheDataSourceProvider</c> to
        /// the repository factory to <c>IDefineAccess</c> and back to <c>ICacheContainer</c>, which
        /// deadlocks service resolution in <c>AddBeeFramework</c>. Deferring the call to the first
        /// cache miss breaks the cycle, because this container is fully constructed by then.
        /// </remarks>
        public CacheContainerService(IDefineStorage storage, PathOptions paths, string cachePrefix,
            Func<ICacheDataSourceProvider>? dataSource)
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
            // SessionInfo has no read-through yet: nothing persists a login session to st_session,
            // so there is nothing to rebuild from. See docs/plans for the persistence work it needs.
            SessionInfo = new SessionInfoCache(CachePrefix);
            CompanyInfo = new CompanyInfoCache(dataSource, CachePrefix);
            CompanyRolePermissions = new CompanyRolePermissionsCache(dataSource, CachePrefix);
            DepartmentTree = new DepartmentTreeCache(dataSource, CachePrefix);
        }

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

    }
}
