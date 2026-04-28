using Bee.Definition.Settings;
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
        /// <summary>
        /// Tests the database connection and throws an exception on failure.
        /// </summary>
        /// <param name="item">The database configuration item.</param>
        public void TestConnection(DatabaseItem item)
        {
            var provider = DbProviderRegistry.Get(item.DatabaseType);

            var connectionString = item.ConnectionString;
            if (StrFunc.IsNotEmpty(item.DbName))
                connectionString = StrFunc.Replace(connectionString, "{@DbName}", item.DbName);
            if (StrFunc.IsNotEmpty(item.UserId))
                connectionString = StrFunc.Replace(connectionString, "{@UserId}", item.UserId);
            if (StrFunc.IsNotEmpty(item.Password))
                connectionString = StrFunc.Replace(connectionString, "{@Password}", item.Password);

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
        /// <param name="dbName">The database name.</param>
        /// <param name="tableName">The table name.</param>
        /// <remarks>Returns whether the schema was upgraded.</remarks>
        public bool UpgradeTableSchema(string databaseId, string dbName, string tableName)
        {
            // Ensure required parameters are not empty
            BaseFunc.EnsureNotNullOrWhiteSpace(
                (databaseId, nameof(databaseId)),
                (dbName, nameof(dbName)),
                (tableName, nameof(tableName))
            );
            var builder = new TableSchemaBuilder(databaseId);
            return builder.Execute(dbName, tableName);
        }
    }
}
