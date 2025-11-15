using System.IO;
using Bee.Base;
using Bee.Define;

namespace Bee.Cache
{
    /// <summary>
    /// 資料庫設定快取。
    /// </summary>
    internal class DatabaseSettingsCache : ObjectCache<DatabaseSettings>
    {
        /// <summary>
        /// 取得快取項目到期條件。
        /// </summary>
        protected override CacheItemPolicy GetPolicy()
        {
            var policy = new CacheItemPolicy(CacheTimeKind.SlidingTime, 20);
            policy.ChangeMonitorFilePaths = new string[] { DefinePathInfo.GetDatabaseSettingsFilePath() };
            return policy;
        }

        /// <summary>
        /// 建立執行個體。
        /// </summary>
        protected override DatabaseSettings CreateInstance()
        {
            string filePath = DefinePathInfo.GetDatabaseSettingsFilePath();
            if (!FileFunc.FileExists(filePath))
                throw new FileNotFoundException($"The file {filePath} does not exist.");

            var settings =  SerializeFunc.XmlFileToObject<DatabaseSettings>(filePath);

            // 觸發全域事件
            GlobalEvents.RaiseDatabaseSettingsChanged();

            return settings;
        }
    }
}
