using Bee.Definition.Storage;
using Bee.ObjectCaching.Database;
using Bee.ObjectCaching.Define;

namespace Bee.ObjectCaching
{
    /// <summary>
    /// Default <see cref="ICacheContainer"/> implementation: holds singleton instances of
    /// the eight framework caches. Constructed once per host (registered as a Singleton in
    /// <c>AddBeeFramework</c>).
    /// </summary>
    /// <remarks>
    /// The file-backed caches (SystemSettings, DatabaseSettings, ProgramSettings) read XML
    /// directly via <see cref="Bee.Definition.DefinePathInfo"/>; the storage-backed caches
    /// receive the supplied <see cref="IDefineStorage"/>.
    /// </remarks>
    public sealed class CacheContainerService : ICacheContainer
    {
        /// <summary>
        /// Initializes a new <see cref="CacheContainerService"/> bound to the supplied storage.
        /// Each instance generates a unique <see cref="CachePrefix"/> so multiple containers
        /// can coexist over the shared <see cref="CacheInfo.Provider"/> without colliding —
        /// essential for per-test-fixture isolation.
        /// </summary>
        /// <param name="storage">The define storage shared by storage-backed caches.</param>
        public CacheContainerService(IDefineStorage storage)
        {
            ArgumentNullException.ThrowIfNull(storage);

            CachePrefix = "cc_" + Guid.NewGuid().ToString("N");
            SystemSettings = new SystemSettingsCache(CachePrefix);
            DatabaseSettings = new DatabaseSettingsCache(CachePrefix);
            ProgramSettings = new ProgramSettingsCache(CachePrefix);
            DbCategorySettings = new DbCategorySettingsCache(storage, CachePrefix);
            TableSchema = new TableSchemaCache(storage, CachePrefix);
            FormSchema = new FormSchemaCache(storage, CachePrefix);
            FormLayout = new FormLayoutCache(storage, CachePrefix);
            SessionInfo = new SessionInfoCache(CachePrefix);
        }

        /// <summary>
        /// The unique namespace prefix used by every cache instance this container owns.
        /// Surfaced primarily for diagnostics — consumers should not rely on its format.
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
    }
}
