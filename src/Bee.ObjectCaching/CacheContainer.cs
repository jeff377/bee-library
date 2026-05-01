using Bee.ObjectCaching.Database;
using Bee.ObjectCaching.Define;

namespace Bee.ObjectCaching
{
    /// <summary>
    /// Cache instance container that centrally manages singleton instances of all cache classes,
    /// using <see cref="Lazy{T}"/> for deferred initialization.
    /// </summary>
    public static class CacheContainer
    {
        private static readonly Lazy<SystemSettingsCache> _systemSettings = new Lazy<SystemSettingsCache>(() => new SystemSettingsCache());
        /// <summary>Gets the system settings cache.</summary>
        public static SystemSettingsCache SystemSettings => _systemSettings.Value;

        private static readonly Lazy<DatabaseSettingsCache> _databaseSettings = new Lazy<DatabaseSettingsCache>(() => new DatabaseSettingsCache());
        /// <summary>Gets the database settings cache.</summary>
        public static DatabaseSettingsCache DatabaseSettings => _databaseSettings.Value;

        private static readonly Lazy<ProgramSettingsCache> _programSettings = new Lazy<ProgramSettingsCache>(() => new ProgramSettingsCache());
        /// <summary>Gets the program settings cache.</summary>
        public static ProgramSettingsCache ProgramSettings => _programSettings.Value;

        private static readonly Lazy<DbSchemaSettingsCache> _dbSchemaSettings = new Lazy<DbSchemaSettingsCache>(() => new DbSchemaSettingsCache());
        /// <summary>Gets the database schema settings cache.</summary>
        public static DbSchemaSettingsCache DbSchemaSettings => _dbSchemaSettings.Value;

        private static readonly Lazy<TableSchemaCache> _tableSchema = new Lazy<TableSchemaCache>(() => new TableSchemaCache());
        /// <summary>Gets the table schema cache, keyed by database name and table name.</summary>
        public static TableSchemaCache TableSchema => _tableSchema.Value;

        private static readonly Lazy<FormSchemaCache> _formSchema = new Lazy<FormSchemaCache>(() => new FormSchemaCache());
        /// <summary>Gets the form schema cache, keyed by program identifier.</summary>
        public static FormSchemaCache FormSchema => _formSchema.Value;

        private static readonly Lazy<FormLayoutCache> _formLayout = new Lazy<FormLayoutCache>(() => new FormLayoutCache());
        /// <summary>Gets the form layout cache, keyed by layout identifier.</summary>
        public static FormLayoutCache FormLayout => _formLayout.Value;

        private static readonly Lazy<SessionInfoCache> _sessionInfo = new Lazy<SessionInfoCache>(() => new SessionInfoCache());
        /// <summary>Gets the session information cache, keyed by access token.</summary>
        public static SessionInfoCache SessionInfo => _sessionInfo.Value;
    }
}
