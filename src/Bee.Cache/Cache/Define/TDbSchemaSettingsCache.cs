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
            TCacheItemPolicy oPolicy;

            oPolicy = new TCacheItemPolicy(ECacheTimeKind.SlidingTime, 20);
            if (BackendInfo.DefineProvider is TFileDefineProvider)
                oPolicy.ChangeMonitorFilePaths = new string[] { DefinePathInfo.GetDbTableSettingsFilePath() };
            return oPolicy;
        }

        /// <summary>
        /// 建立執行個體。
        /// </summary>
        protected override TDbSchemaSettings CreateInstance()
        {
            TDbSchemaSettings oValue;

            oValue = BackendInfo.DefineProvider.GetDbSchemaSettings();
            return oValue;
        }
    }
}
