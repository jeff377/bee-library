using Bee.Define;

namespace Bee.Cache
{
    /// <summary>
    /// 資料庫結構設定快取。
    /// </summary>
    internal class TDbSchemaSettingsCache : TObjectCache<TDbSchemaSettings>
    {
        /// <summary>
        /// 取得快取項目到期條件。
        /// </summary>
        protected override TCacheItemPolicy GetPolicy()
        {
            var policy = new TCacheItemPolicy(ECacheTimeKind.SlidingTime, 20);
            if (BackendInfo.DefineProvider is TFileDefineProvider)
                policy.ChangeMonitorFilePaths = new string[] { DefinePathInfo.GetDbTableSettingsFilePath() };
            return policy;
        }

        /// <summary>
        /// 建立執行個體。
        /// </summary>
        protected override TDbSchemaSettings CreateInstance()
        {
            return BackendInfo.DefineProvider.GetDbSchemaSettings();
        }
    }
}
