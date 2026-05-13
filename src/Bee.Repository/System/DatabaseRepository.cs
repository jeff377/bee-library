using Bee.Definition.Settings;
using Bee.Definition.Storage;
using Bee.Base;
using Bee.Db.Manager;
using Bee.Db.Schema;
using Bee.Repository.Abstractions.System;

namespace Bee.Repository.System
{
    /// <summary>
    /// Default implementation of database operations.
    /// </summary>
    internal class DatabaseRepository : IDatabaseRepository
    {
        private readonly IDefineAccess _defineAccess;
        private readonly IDbConnectionManager _connectionManager;

        public DatabaseRepository(IDefineAccess defineAccess, IDbConnectionManager connectionManager)
        {
            _defineAccess = defineAccess ?? throw new ArgumentNullException(nameof(defineAccess));
            _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
        }

        /// <summary>
        /// Tests the database connection and throws an exception on failure.
        /// </summary>
        /// <param name="item">The database configuration item.</param>
        public void TestConnection(DatabaseItem item)
        {
            var provider = DbProviderRegistry.Get(item.DatabaseType);

            var connectionString = item.ConnectionString;
            if (StringUtilities.IsNotEmpty(item.DbName))
                connectionString = StringUtilities.Replace(connectionString, "{@DbName}", item.DbName);
            if (StringUtilities.IsNotEmpty(item.UserId))
                connectionString = StringUtilities.Replace(connectionString, "{@UserId}", item.UserId);
            if (StringUtilities.IsNotEmpty(item.Password))
                connectionString = StringUtilities.Replace(connectionString, "{@Password}", item.Password);

            using (var connection = provider.CreateConnection()!)
            {
                connection.ConnectionString = connectionString;
                connection.Open();
            }
        }

        /// <summary>
        /// Upgrades the table schema for the specified table.
        /// </summary>
        /// <param name="databaseId">The database identifier.</param>
        /// <param name="categoryId">The database category id.</param>
        /// <param name="tableName">The table name.</param>
        /// <remarks>Returns whether the schema was upgraded.</remarks>
        public bool UpgradeTableSchema(string databaseId, string categoryId, string tableName)
        {
            // Ensure required parameters are not empty
            ArgumentException.ThrowIfNullOrWhiteSpace(databaseId);
            ArgumentException.ThrowIfNullOrWhiteSpace(categoryId);
            ArgumentException.ThrowIfNullOrWhiteSpace(tableName);
            var builder = new TableSchemaBuilder(databaseId, _defineAccess, _connectionManager);
            return builder.Execute(categoryId, tableName);
        }
    }
}
