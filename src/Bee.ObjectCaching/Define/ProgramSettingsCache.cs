using Bee.Base.Serialization;
using Bee.Definition;
using Bee.Definition.Settings;

namespace Bee.ObjectCaching.Define
{
    /// <summary>
    /// Program settings cache.
    /// </summary>
    public class ProgramSettingsCache : ObjectCache<ProgramSettings>
    {
        /// <summary>
        /// Gets the cache item expiration policy.
        /// </summary>
        protected override CacheItemPolicy GetPolicy()
        {
            var policy = new CacheItemPolicy(CacheTimeKind.SlidingTime, 20);
            policy.ChangeMonitorFilePaths = new string[] { DefinePathInfo.GetProgramSettingsFilePath() };
            return policy;
        }

        /// <summary>
        /// Creates an instance of the program settings.
        /// </summary>
        protected override ProgramSettings? CreateInstance()
        {
            string filePath = DefinePathInfo.GetProgramSettingsFilePath();
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"The file {filePath} does not exist.");

            return XmlCodec.DeserializeFromFile<ProgramSettings>(filePath);
        }
    }
}
