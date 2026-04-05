using Bee.Base;
using Bee.Base.Serialization;
using Bee.Cache;
using Bee.Define;
using Bee.Define.Settings;
using System.IO;

namespace Bee.Cache.Define
{
    /// <summary>
    /// 系統設定快取。
    /// </summary>
    internal class SystemSettingsCache : ObjectCache<SystemSettings>
    {
        /// <summary>
        /// 取得快取項目到期條件。
        /// </summary>
        protected override CacheItemPolicy GetPolicy()
        {
            var policy = new CacheItemPolicy(CacheTimeKind.SlidingTime, 20);
            policy.ChangeMonitorFilePaths = new string[] { DefinePathInfo.GetSystemSettingsFilePath() };
            return policy;
        }

        /// <summary>
        /// 建立執行個體。
        /// </summary>
        /// <returns></returns>
        protected override SystemSettings CreateInstance()
        {
            string sFilePath = DefinePathInfo.GetSystemSettingsFilePath();
            if (!FileFunc.FileExists(sFilePath))
                throw new FileNotFoundException($"The file {sFilePath} does not exist.");

            return SerializeFunc.XmlFileToObject<SystemSettings>(sFilePath);
        }
    }
}
