using Bee.ObjectCaching;
using Bee.Definition;
using Bee.Definition.Layouts;
using Bee.Definition.Storage;

namespace Bee.ObjectCaching.Define
{
    /// <summary>
    /// Form layout cache.
    /// </summary>
    internal class FormLayoutCache : KeyObjectCache<FormLayout>
    {
        /// <summary>
        /// Gets the cache item expiration policy.
        /// </summary>
        /// <param name="key">The member key.</param>
        protected override CacheItemPolicy GetPolicy(string key)
        {
            // Layout identifier
            string layoutId = key;
            // Default: sliding expiration of 20 minutes
            var policy = new CacheItemPolicy(CacheTimeKind.SlidingTime, 20);
            if (BackendInfo.DefineStorage is FileDefineStorage)
                policy.ChangeMonitorFilePaths = new string[] { DefinePathInfo.GetFormLayoutFilePath(layoutId) };
            return policy;
        }

        /// <summary>
        /// Creates an instance of the form layout.
        /// </summary>
        /// <param name="key">The member key, which is the layout identifier.</param>
        protected override FormLayout CreateInstance(string key)
        {
            // Layout identifier
            string layoutId = key;
            return BackendInfo.DefineStorage.GetFormLayout(layoutId);
        }
    }
}
