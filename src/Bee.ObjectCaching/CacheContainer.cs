using Bee.Definition.Storage;
using Bee.ObjectCaching.Database;
using Bee.ObjectCaching.Define;

namespace Bee.ObjectCaching
{
    /// <summary>
    /// Cache instance container that centrally manages singleton instances of all cache classes.
    /// </summary>
    /// <remarks>
    /// Must be initialized via <see cref="Initialize"/> at host startup. Storage-backed caches
    /// receive the supplied <see cref="IDefineStorage"/>; file-backed caches (SystemSettings,
    /// DatabaseSettings, ProgramSettings) read XML directly and do not require the storage instance.
    /// </remarks>
    public static class CacheContainer
    {
        private static SystemSettingsCache? _systemSettings;
        private static DatabaseSettingsCache? _databaseSettings;
        private static ProgramSettingsCache? _programSettings;
        private static DbCategorySettingsCache? _dbCategorySettings;
        private static TableSchemaCache? _tableSchema;
        private static FormSchemaCache? _formSchema;
        private static FormLayoutCache? _formLayout;
        private static SessionInfoCache? _sessionInfo;

        /// <summary>
        /// Installs the define storage and constructs the cache instances. Must be called once
        /// at host startup before any cache accessor is read.
        /// </summary>
        /// <param name="storage">The define storage shared by storage-backed caches.</param>
        public static void Initialize(IDefineStorage storage)
        {
            if (storage == null) throw new ArgumentNullException(nameof(storage));

            _systemSettings = new SystemSettingsCache();
            _databaseSettings = new DatabaseSettingsCache();
            _programSettings = new ProgramSettingsCache();
            _dbCategorySettings = new DbCategorySettingsCache(storage);
            _tableSchema = new TableSchemaCache(storage);
            _formSchema = new FormSchemaCache(storage);
            _formLayout = new FormLayoutCache(storage);
            _sessionInfo = new SessionInfoCache();
        }

        private static T EnsureReady<T>(T? instance, string name) where T : class
            => instance ?? throw new InvalidOperationException(
                $"CacheContainer.{name} accessed before CacheContainer.Initialize was called.");

        /// <summary>Gets the system settings cache.</summary>
        public static SystemSettingsCache SystemSettings => EnsureReady(_systemSettings, nameof(SystemSettings));

        /// <summary>Gets the database settings cache.</summary>
        public static DatabaseSettingsCache DatabaseSettings => EnsureReady(_databaseSettings, nameof(DatabaseSettings));

        /// <summary>Gets the program settings cache.</summary>
        public static ProgramSettingsCache ProgramSettings => EnsureReady(_programSettings, nameof(ProgramSettings));

        /// <summary>Gets the database category settings cache.</summary>
        public static DbCategorySettingsCache DbCategorySettings => EnsureReady(_dbCategorySettings, nameof(DbCategorySettings));

        /// <summary>Gets the table schema cache, keyed by category id and table name.</summary>
        public static TableSchemaCache TableSchema => EnsureReady(_tableSchema, nameof(TableSchema));

        /// <summary>Gets the form schema cache, keyed by program identifier.</summary>
        public static FormSchemaCache FormSchema => EnsureReady(_formSchema, nameof(FormSchema));

        /// <summary>Gets the form layout cache, keyed by layout identifier.</summary>
        public static FormLayoutCache FormLayout => EnsureReady(_formLayout, nameof(FormLayout));

        /// <summary>Gets the session information cache, keyed by access token.</summary>
        public static SessionInfoCache SessionInfo => EnsureReady(_sessionInfo, nameof(SessionInfo));
    }
}
