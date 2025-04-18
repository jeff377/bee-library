using System;

namespace Bee.Cache
{
    /// <summary>  
    /// 快取實體容器，集中管理所有快取類別的單一實例，使用 <see cref="Lazy{T}"/> 實現延遲初始化。   
    /// </summary>
    internal class CacheContainer
    {
        private static readonly Lazy<TSystemSettingsCache> _systemSettings = new Lazy<TSystemSettingsCache>(() => new TSystemSettingsCache());
        internal static TSystemSettingsCache SystemSettings => _systemSettings.Value;

        private static readonly Lazy<TDatabaseSettingsCache> _databaseSettings = new Lazy<TDatabaseSettingsCache>(() => new TDatabaseSettingsCache());
        internal static TDatabaseSettingsCache DatabaseSettings => _databaseSettings.Value;

        private static readonly Lazy<TProgramSettingsCache> _programSettings = new Lazy<TProgramSettingsCache>(() => new TProgramSettingsCache());
        internal static TProgramSettingsCache ProgramSettings => _programSettings.Value;

        private static readonly Lazy<TDbSchemaSettingsCache> _dbSchemaSettings = new Lazy<TDbSchemaSettingsCache>(() => new TDbSchemaSettingsCache());
        internal static TDbSchemaSettingsCache DbSchemaSettings => _dbSchemaSettings.Value;

        private static readonly Lazy<TDbTableCache> _dbTable = new Lazy<TDbTableCache>(() => new TDbTableCache());
        internal static TDbTableCache DbTable => _dbTable.Value;

        private static readonly Lazy<TFormDefineCache> _formDefine = new Lazy<TFormDefineCache>(() => new TFormDefineCache());
        internal static TFormDefineCache FormDefine => _formDefine.Value;

        private static readonly Lazy<TFormLayoutCache> _formLayout = new Lazy<TFormLayoutCache>(() => new TFormLayoutCache());
        internal static TFormLayoutCache FormLayout => _formLayout.Value;

        private static readonly Lazy<TSessionInfoCache> _sessionInfo = new Lazy<TSessionInfoCache>(() => new TSessionInfoCache());
        internal static TSessionInfoCache SessionInfo => _sessionInfo.Value;

        private static readonly Lazy<TViewStateCache> viewState = new Lazy<TViewStateCache>(() => new TViewStateCache());
        internal static TViewStateCache ViewState => viewState.Value;
    }
}
