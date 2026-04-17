using Bee.ObjectCaching.Database;
using Bee.ObjectCaching.Define;
using Bee.ObjectCaching.Runtime;
using System;

namespace Bee.ObjectCaching
{
    /// <summary>
    /// Cache instance container that centrally manages singleton instances of all cache classes, using <see cref="Lazy{T}"/> for deferred initialization.
    /// </summary>
    internal static class CacheContainer
    {
        private static readonly Lazy<SystemSettingsCache> _systemSettings = new Lazy<SystemSettingsCache>(() => new SystemSettingsCache());
        internal static SystemSettingsCache SystemSettings => _systemSettings.Value;

        private static readonly Lazy<DatabaseSettingsCache> _databaseSettings = new Lazy<DatabaseSettingsCache>(() => new DatabaseSettingsCache());
        internal static DatabaseSettingsCache DatabaseSettings => _databaseSettings.Value;

        private static readonly Lazy<ProgramSettingsCache> _programSettings = new Lazy<ProgramSettingsCache>(() => new ProgramSettingsCache());
        internal static ProgramSettingsCache ProgramSettings => _programSettings.Value;

        private static readonly Lazy<DbSchemaSettingsCache> _dbSchemaSettings = new Lazy<DbSchemaSettingsCache>(() => new DbSchemaSettingsCache());
        internal static DbSchemaSettingsCache DbSchemaSettings => _dbSchemaSettings.Value;

        private static readonly Lazy<TableSchemaCache> _tableSchema = new Lazy<TableSchemaCache>(() => new TableSchemaCache());
        internal static TableSchemaCache TableSchema => _tableSchema.Value;

        private static readonly Lazy<FormSchemaCache> _formSchema = new Lazy<FormSchemaCache>(() => new FormSchemaCache());
        internal static FormSchemaCache FormSchema => _formSchema.Value;

        private static readonly Lazy<FormLayoutCache> _formLayout = new Lazy<FormLayoutCache>(() => new FormLayoutCache());
        internal static FormLayoutCache FormLayout => _formLayout.Value;

        private static readonly Lazy<SessionInfoCache> _sessionInfo = new Lazy<SessionInfoCache>(() => new SessionInfoCache());
        internal static SessionInfoCache SessionInfo => _sessionInfo.Value;

        private static readonly Lazy<ViewStateCache> viewState = new Lazy<ViewStateCache>(() => new ViewStateCache());
        internal static ViewStateCache ViewState => viewState.Value;
    }
}
