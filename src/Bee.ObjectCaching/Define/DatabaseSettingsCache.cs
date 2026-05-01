using Bee.Base.Serialization;
using Bee.Definition;
using Bee.Definition.Settings;

namespace Bee.ObjectCaching.Define
{
    /// <summary>
    /// Database settings cache.
    /// </summary>
    public class DatabaseSettingsCache : ObjectCache<DatabaseSettings>
    {
        /// <summary>
        /// Gets the cache item expiration policy.
        /// </summary>
        protected override CacheItemPolicy GetPolicy()
        {
            var policy = new CacheItemPolicy(CacheTimeKind.SlidingTime, 20);
            policy.ChangeMonitorFilePaths = new string[] { DefinePathInfo.GetDatabaseSettingsFilePath() };
            return policy;
        }

        /// <summary>
        /// Creates an instance of the database settings.
        /// </summary>
        protected override DatabaseSettings? CreateInstance()
        {
            string filePath = DefinePathInfo.GetDatabaseSettingsFilePath();
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"The file {filePath} does not exist.");

            var settings =  XmlCodec.DeserializeFromFile<DatabaseSettings>(filePath);

            // Raise the global database settings changed event
            GlobalEvents.RaiseDatabaseSettingsChanged();

            return settings;
        }
    }
}
