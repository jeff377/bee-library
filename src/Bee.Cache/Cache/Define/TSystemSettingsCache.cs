using System.IO;
using Bee.Base;
using Bee.Define;

namespace Bee.Cache
{
    /// <summary>
    /// 系統設定快取。
    /// </summary>
    internal class TSystemSettingsCache : TObjectCache<TSystemSettings>
    {
        /// <summary>
        /// 取得快取項目到期條件。
        /// </summary>
        protected override TCacheItemPolicy GetPolicy()
        {
            TCacheItemPolicy oPolicy;

            oPolicy = new TCacheItemPolicy(ECacheTimeKind.SlidingTime, 20);
            oPolicy.ChangeMonitorFilePaths = new string[] { DefinePathInfo.GetSystemSettingsFilePath() };
            return oPolicy;
        }

        /// <summary>
        /// 建立執行個體。
        /// </summary>
        /// <returns></returns>
        protected override TSystemSettings CreateInstance()
        {
            TSystemSettings oValue;
            string sFilePath;

            sFilePath = DefinePathInfo.GetSystemSettingsFilePath();
            if (!FileFunc.FileExists(sFilePath))
                throw new FileNotFoundException($"The file {sFilePath} does not exist.");
            oValue = SerializeFunc.XmlFileToObject<TSystemSettings>(sFilePath);
            return oValue;
        }
    }
}
