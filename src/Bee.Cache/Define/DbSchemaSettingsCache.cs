using Bee.Cache;
using Bee.Define;
using Bee.Define.Settings;
using Bee.Define.Storage;

namespace Bee.Cache.Define
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
        protected override DbSchemaSettings CreateInstance()
        {
            return BackendInfo.DefineStorage.GetDbSchemaSettings();
        }
    }
}
