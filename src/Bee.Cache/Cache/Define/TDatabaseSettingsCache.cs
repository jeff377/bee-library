using System.IO;
using Bee.Base;
using Bee.Define;

namespace Bee.Cache
{
    /// <summary>
    /// 資料庫設定快取。
    /// </summary>
    internal class TDatabaseSettingsCache : TObjectCache<TDatabaseSettings>
    {
        /// <summary>
        /// 取得快取項目到期條件。
        /// </summary>
        protected override TCacheItemPolicy GetPolicy()
        {
            TCacheItemPolicy oPolicy;

            oPolicy = new TCacheItemPolicy(ECacheTimeKind.SlidingTime, 20);
            oPolicy.ChangeMonitorFilePaths = new string[] { DefinePathInfo.GetDatabaseSettingsFilePath() };
            return oPolicy;
        }

        /// <summary>
        /// 建立執行個體。
        /// </summary>
        protected override TDatabaseSettings CreateInstance()
        {
            TDatabaseSettings oValue;
            string sFilePath;

            sFilePath = DefinePathInfo.GetDatabaseSettingsFilePath();
            if (!FileFunc.FileExists(sFilePath))
                throw new FileNotFoundException($"The file {sFilePath} does not exist.");
            oValue = SerializeFunc.XmlFileToObject<TDatabaseSettings>(sFilePath);
            return oValue;
        }
    }
}
