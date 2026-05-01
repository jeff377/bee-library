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
        /// <summary>
        /// Gets the cache item expiration policy.
        /// </summary>
        /// <param name="key">The member key.</param>
        protected override CacheItemPolicy GetPolicy(string key)
        {
            // Parse the member key to extract the database name and table name
            StrFunc.SplitLeft(key, ".", out string dbName, out string tableName);

            // Default: sliding expiration of 20 minutes
            var policy = new CacheItemPolicy(CacheTimeKind.SlidingTime, 20);
            if (BackendInfo.DefineStorage is FileDefineStorage)
                policy.ChangeMonitorFilePaths = new string[] { DefinePathInfo.GetTableSchemaFilePath(dbName, tableName) };
            return policy;
        }

        /// <summary>
        /// Creates an instance of the table schema.
        /// </summary>
        /// <param name="key">The member key, in the format [database name].[table name].</param>
        protected override TableSchema? CreateInstance(string key)
        {
            // Parse the member key to extract the database name and table name
            StrFunc.SplitLeft(key, ".", out string dbName, out string tableName);
            return BackendInfo.DefineStorage.GetTableSchema(dbName, tableName);
        }

        /// <summary>
        /// Gets the table schema for the specified database and table.
        /// </summary>
        /// <param name="dbName">The database name.</param>
        /// <param name="tableName">The table name.</param>
        public TableSchema? Get(string dbName, string tableName)
        {
            string key = $"{dbName}.{tableName}";
            return base.Get(key);
        }

        /// <summary>
        /// Removes the table schema entry from the cache.
        /// </summary>
        /// <param name="categoryID">The database category identifier.</param>
        /// <param name="tableName">The table name.</param>
        public void Remove(string categoryID, string tableName)
        {
            string key = $"{categoryID}.{tableName}";
            base.Remove(key);
        }
    }
}
