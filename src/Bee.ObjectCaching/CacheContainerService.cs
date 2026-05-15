using Bee.Definition;
using Bee.Definition.Storage;
using Bee.ObjectCaching.Database;
using Bee.ObjectCaching.Define;

namespace Bee.ObjectCaching
{
    /// <summary>
    /// Default <see cref="ICacheContainer"/> implementation: holds singleton instances of
    /// the nine framework caches. Constructed once per host (registered as a Singleton in
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
            ProgramSettings = new ProgramSettingsCache(paths, CachePrefix);
            DbCategorySettings = new DbCategorySettingsCache(storage, paths, CachePrefix);
            TableSchema = new TableSchemaCache(storage, paths, CachePrefix);
            FormSchema = new FormSchemaCache(storage, paths, CachePrefix);
            FormLayout = new FormLayoutCache(storage, paths, CachePrefix);
            SessionInfo = new SessionInfoCache(CachePrefix);
            CompanyInfo = new CompanyInfoCache(CachePrefix);
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
        public DbCategorySettingsCache DbCategorySettings { get; }

        /// <inheritdoc/>
        public TableSchemaCache TableSchema { get; }

        /// <inheritdoc/>
        public FormSchemaCache FormSchema { get; }

        /// <inheritdoc/>
        public FormLayoutCache FormLayout { get; }

        /// <inheritdoc/>
        public SessionInfoCache SessionInfo { get; }

        /// <inheritdoc/>
        public CompanyInfoCache CompanyInfo { get; }
    }
}
