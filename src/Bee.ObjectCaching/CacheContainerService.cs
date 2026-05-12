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
        /// </summary>
        /// <param name="storage">The define storage shared by storage-backed caches.</param>
        public CacheContainerService(IDefineStorage storage)
        {
            ArgumentNullException.ThrowIfNull(storage);

            SystemSettings = new SystemSettingsCache();
            DatabaseSettings = new DatabaseSettingsCache();
            ProgramSettings = new ProgramSettingsCache();
            DbCategorySettings = new DbCategorySettingsCache(storage);
            TableSchema = new TableSchemaCache(storage);
            FormSchema = new FormSchemaCache(storage);
            FormLayout = new FormLayoutCache(storage);
            SessionInfo = new SessionInfoCache();
        }

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
