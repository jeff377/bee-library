using Bee.Base;
using Bee.Base.Serialization;
using Bee.Cache;
using Bee.Define;
using Bee.Define.Settings;
using System.IO;

namespace Bee.Cache.Define
{
    /// <summary>
    /// System settings cache.
    /// </summary>
    internal class SystemSettingsCache : ObjectCache<SystemSettings>
    {
        /// <summary>
        /// Gets the cache item expiration policy.
        /// </summary>
        protected override CacheItemPolicy GetPolicy()
        {
            var policy = new CacheItemPolicy(CacheTimeKind.SlidingTime, 20);
            policy.ChangeMonitorFilePaths = new string[] { DefinePathInfo.GetSystemSettingsFilePath() };
            return policy;
        }

        /// <summary>
        /// Creates an instance of the system settings.
        /// </summary>
        /// <returns>The system settings instance.</returns>
        protected override SystemSettings CreateInstance()
        {
            string sFilePath = DefinePathInfo.GetSystemSettingsFilePath();
            if (!FileFunc.FileExists(sFilePath))
                throw new FileNotFoundException($"The file {sFilePath} does not exist.");

            return SerializeFunc.XmlFileToObject<SystemSettings>(sFilePath);
        }
    }
}
