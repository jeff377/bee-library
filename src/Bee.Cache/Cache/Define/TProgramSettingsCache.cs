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
            var policy = new TCacheItemPolicy(ECacheTimeKind.SlidingTime, 20);
            policy.ChangeMonitorFilePaths = new string[] { DefinePathInfo.GetProgramSettingsFilePath() };
            return policy;
        }

        /// <summary>
        /// 建立執行個體。
        /// </summary>
        protected override TProgramSettings CreateInstance()
        {
            string filePath = DefinePathInfo.GetProgramSettingsFilePath();
            if (!FileFunc.FileExists(filePath))
                throw new FileNotFoundException($"The file {filePath} does not exist.");

            return SerializeFunc.XmlFileToObject<TProgramSettings>(filePath);
        }
    }
}
