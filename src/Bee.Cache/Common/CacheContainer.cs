using System;

namespace Bee.Cache
{
    /// <summary>  
    /// 快取實體容器，集中管理所有快取類別的單一實例，使用 <see cref="Lazy{T}"/> 實現延遲初始化。   
    /// </summary>
    internal class CacheContainer
    {
        private static readonly Lazy<TSystemSettingsCache> _systemSettings = new Lazy<TSystemSettingsCache>(() => new TSystemSettingsCache());
        public static TSystemSettingsCache SystemSettings => _systemSettings.Value;

        private static readonly Lazy<TDatabaseSettingsCache> _databaseSettings = new Lazy<TDatabaseSettingsCache>(() => new TDatabaseSettingsCache());
        public static TDatabaseSettingsCache DatabaseSettings => _databaseSettings.Value;

        private static readonly Lazy<TProgramSettingsCache> _programSettings = new Lazy<TProgramSettingsCache>(() => new TProgramSettingsCache());
        public static TProgramSettingsCache ProgramSettings => _programSettings.Value;
    }
}
