using Bee.Definition;
using Bee.Definition.Settings;
using Bee.Definition.Storage;

namespace Bee.ObjectCaching.Define
{
    /// <summary>
    /// Database schema settings cache.
    /// </summary>
    internal class DbSchemaSettingsCache : ObjectCache<DbSchemaSettings>
    {
        /// <summary>
        /// Gets the cache item expiration policy.
        /// </summary>
        protected override CacheItemPolicy GetPolicy()
        {
            var policy = new CacheItemPolicy(CacheTimeKind.SlidingTime, 20);
            if (BackendInfo.DefineStorage is FileDefineStorage)
                policy.ChangeMonitorFilePaths = new string[] { DefinePathInfo.GetDbTableSettingsFilePath() };
            return policy;
        }

        /// <summary>
        /// Creates an instance of the database schema settings.
        /// </summary>
        protected override DbSchemaSettings? CreateInstance()
        {
            return BackendInfo.DefineStorage.GetDbSchemaSettings();
        }
    }
}
