using System.IO;
using Bee.Base;
using Bee.Define;

namespace Bee.Cache
{
    /// <summary>
    /// 程式清單快取。
    /// </summary>
    internal class TProgramSettingsCache : TObjectCache<TProgramSettings>
    {
        /// <summary>
        /// 取得快取項目到期條件。
        /// </summary>
        protected override TCacheItemPolicy GetPolicy()
        {
            TCacheItemPolicy oPolicy;

            oPolicy = new TCacheItemPolicy(ECacheTimeKind.SlidingTime, 20);
            oPolicy.ChangeMonitorFilePaths = new string[] { DefinePathInfo.GetProgramSettingsFilePath() };
            return oPolicy;
        }

        /// <summary>
        /// 建立執行個體。
        /// </summary>
        protected override TProgramSettings CreateInstance()
        {
            TProgramSettings oValue;
            string sFilePath;

            sFilePath = DefinePathInfo.GetProgramSettingsFilePath();
            if (!FileFunc.FileExists(sFilePath))
                throw new FileNotFoundException($"The file {sFilePath} does not exist.");
            oValue = SerializeFunc.XmlFileToObject<TProgramSettings>(sFilePath);
            return oValue;
        }
    }
}
