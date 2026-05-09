using Bee.Definition;
using Bee.Definition.Settings;
using Bee.Definition.Storage;

namespace Bee.ObjectCaching.Define
{
    /// <summary>
    /// Database category settings cache.
    /// </summary>
    public class DbCategorySettingsCache : ObjectCache<DbCategorySettings>
    {
        /// <summary>
        /// Gets the cache item expiration policy.
        /// </summary>
        protected override CacheItemPolicy GetPolicy()
        {
            var policy = new CacheItemPolicy(CacheTimeKind.SlidingTime, 20);
            if (BackendInfo.DefineStorage is FileDefineStorage)
                policy.ChangeMonitorFilePaths = new string[] { DefinePathInfo.GetDbCategorySettingsFilePath() };
            return policy;
        }

        /// <summary>
        /// Creates an instance of the database category settings.
        /// </summary>
        protected override DbCategorySettings? CreateInstance()
        {
            return BackendInfo.DefineStorage.GetDbCategorySettings();
        }
    }
}
