using Bee.Base;
using Bee.Definition;
using Bee.Definition.Database;
using Bee.Definition.Storage;

namespace Bee.ObjectCaching.Define
{
    /// <summary>
    /// Table schema cache.
    /// </summary>
    public class TableSchemaCache : KeyObjectCache<TableSchema>
    {
        private readonly IDefineStorage _storage;

        /// <summary>
        /// Initializes a new instance of <see cref="TableSchemaCache"/>.
        /// </summary>
        /// <param name="storage">The define storage backing this cache.</param>
        /// <param name="paths">Retained for constructor compatibility; the monitored file paths now come from <paramref name="storage"/>. Still validated as non-null.</param>
        /// <param name="cachePrefix">Per-owner cache namespace (see <see cref="KeyObjectCache{T}"/>).</param>
        public TableSchemaCache(IDefineStorage storage, PathOptions paths, string cachePrefix = "") : base(cachePrefix)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            ArgumentNullException.ThrowIfNull(paths);
        }

        /// <summary>
        /// Gets the cache item expiration policy.
        /// </summary>
        /// <param name="key">The member key.</param>
        protected override CacheItemPolicy GetPolicy(string key)
        {
            // Parse the member key to extract the category id and table name
            key.SplitLeft(".", out string categoryId, out string tableName);

            // Default: sliding expiration of 20 minutes
            var policy = new CacheItemPolicy(CacheTimeKind.SlidingTime, 20);
            policy.ChangeMonitorFilePaths = _storage.GetChangeSource(DefineType.TableSchema, categoryId, tableName).FilePaths;
            return policy;
        }

        /// <summary>
        /// Creates an instance of the table schema.
        /// </summary>
        /// <param name="key">The member key, in the format [category id].[table name].</param>
        protected override TableSchema? CreateInstance(string key)
        {
            // Parse the member key to extract the category id and table name
            key.SplitLeft(".", out string categoryId, out string tableName);
            return _storage.GetTableSchema(categoryId, tableName);
        }

        /// <summary>
        /// Gets the table schema for the specified category and table.
        /// </summary>
        /// <param name="categoryId">The database category id.</param>
        /// <param name="tableName">The table name.</param>
        public TableSchema? Get(string categoryId, string tableName)
        {
            string key = $"{categoryId}.{tableName}";
            return base.Get(key);
        }

        /// <summary>
        /// Removes the table schema entry from the cache.
        /// </summary>
        /// <param name="categoryId">The database category id.</param>
        /// <param name="tableName">The table name.</param>
        public void Remove(string categoryId, string tableName)
        {
            string key = $"{categoryId}.{tableName}";
            base.Remove(key);
        }
    }
}
