using System;

namespace Bee.Cache
{
    /// <summary>  
    /// 快取實體容器，集中管理所有快取類別的單一實例，使用 <see cref="Lazy{T}"/> 實現延遲初始化。   
    /// </summary>
    internal class CacheContainer
    {
        private static readonly Lazy<SystemSettingsCache> _systemSettings = new Lazy<SystemSettingsCache>(() => new SystemSettingsCache());
        internal static SystemSettingsCache SystemSettings => _systemSettings.Value;

        private static readonly Lazy<DatabaseSettingsCache> _databaseSettings = new Lazy<DatabaseSettingsCache>(() => new DatabaseSettingsCache());
        internal static DatabaseSettingsCache DatabaseSettings => _databaseSettings.Value;

        private static readonly Lazy<ProgramSettingsCache> _programSettings = new Lazy<ProgramSettingsCache>(() => new ProgramSettingsCache());
        internal static ProgramSettingsCache ProgramSettings => _programSettings.Value;

        private static readonly Lazy<DbSchemaSettingsCache> _dbSchemaSettings = new Lazy<DbSchemaSettingsCache>(() => new DbSchemaSettingsCache());
        internal static DbSchemaSettingsCache DbSchemaSettings => _dbSchemaSettings.Value;

        private static readonly Lazy<DbTableCache> _dbTable = new Lazy<DbTableCache>(() => new DbTableCache());
        internal static DbTableCache DbTable => _dbTable.Value;

        private static readonly Lazy<FormDefineCache> _formDefine = new Lazy<FormDefineCache>(() => new FormDefineCache());
        internal static FormDefineCache FormDefine => _formDefine.Value;

        private static readonly Lazy<FormLayoutCache> _formLayout = new Lazy<FormLayoutCache>(() => new FormLayoutCache());
        internal static FormLayoutCache FormLayout => _formLayout.Value;

        private static readonly Lazy<SessionInfoCache> _sessionInfo = new Lazy<SessionInfoCache>(() => new SessionInfoCache());
        internal static SessionInfoCache SessionInfo => _sessionInfo.Value;

        private static readonly Lazy<ViewStateCache> viewState = new Lazy<ViewStateCache>(() => new ViewStateCache());
        internal static ViewStateCache ViewState => viewState.Value;
    }
}
