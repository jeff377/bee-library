using Bee.ObjectCaching;
using Bee.Definition;
using Bee.Definition.Forms;
using Bee.Definition.Storage;

namespace Bee.ObjectCaching.Define
{
    /// <summary>
    /// Form schema definition cache.
    /// </summary>
    internal class FormSchemaCache : KeyObjectCache<FormSchema>
    {
        /// <summary>
        /// Gets the cache item expiration policy.
        /// </summary>
        /// <param name="key">The member key.</param>
        protected override CacheItemPolicy GetPolicy(string key)
        {
            // Program identifier
            string progId = key;
            // Default: sliding expiration of 20 minutes
            var policy = new CacheItemPolicy(CacheTimeKind.SlidingTime, 20);
            if (BackendInfo.DefineStorage is FileDefineStorage)
                policy.ChangeMonitorFilePaths = new string[] { DefinePathInfo.GetFormSchemaFilePath(progId) };
            return policy;
        }

        /// <summary>
        /// Creates an instance of the form schema.
        /// </summary>
        /// <param name="key">The member key, which is the program identifier.</param>
        protected override FormSchema? CreateInstance(string key)
        {
            // Program identifier
            string progId = key;
            return BackendInfo.DefineStorage.GetFormSchema(progId);
        }
    }
}
