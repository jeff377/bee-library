using Bee.Cache;
using Bee.Define;
using Bee.Define.Settings;
using Bee.Define.Storage;

namespace Bee.Cache.Define
{
    /// <summary>
    /// 資料庫結構設定快取。
    /// </summary>
    internal class DbSchemaSettingsCache : ObjectCache<DbSchemaSettings>
    {
        /// <summary>
        /// 取得快取項目到期條件。
        /// </summary>
        protected override CacheItemPolicy GetPolicy()
        {
            var policy = new CacheItemPolicy(CacheTimeKind.SlidingTime, 20);
            if (BackendInfo.DefineStorage is FileDefineStorage)
                policy.ChangeMonitorFilePaths = new string[] { DefinePathInfo.GetDbTableSettingsFilePath() };
            return policy;
        }

        /// <summary>
        /// 建立執行個體。
        /// </summary>
        protected override DbSchemaSettings CreateInstance()
        {
            return BackendInfo.DefineStorage.GetDbSchemaSettings();
        }
    }
}
