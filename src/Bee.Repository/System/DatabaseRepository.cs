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
        /// <remarks>
        /// When <see cref="DatabaseItem.ServerId"/> is set, the connection string and
        /// <see cref="DatabaseItem.DatabaseType"/> are taken from the referenced
        /// <see cref="DatabaseServer"/>; the item's <c>UserId</c>/<c>Password</c> override
        /// the server's when non-empty (mirrors <c>DbConnectionManagerService</c>).
        /// </remarks>
        public void TestConnection(DatabaseItem item)
        {
            var databaseType = item.DatabaseType;
            var connectionString = item.ConnectionString;
            var userId = item.UserId;
            var password = item.Password;

            if (StringUtilities.IsNotEmpty(item.ServerId))
            {
                var settings = _defineAccess.GetDatabaseSettings();
                if (settings.Servers == null || !settings.Servers.Contains(item.ServerId))
                {
                    throw new InvalidOperationException(
                        $"DatabaseServer '{item.ServerId}' referenced by DatabaseItem '{item.Id}' was not found.");
                }
                var server = settings.Servers[item.ServerId];
                connectionString = server.ConnectionString;
                databaseType = server.DatabaseType;
                if (StringUtilities.IsEmpty(userId))
                    userId = server.UserId;
                if (StringUtilities.IsEmpty(password))
                    password = server.Password;
            }

            if (StringUtilities.IsNotEmpty(item.DbName))
                connectionString = StringUtilities.Replace(connectionString, "{@DbName}", item.DbName);
            if (StringUtilities.IsNotEmpty(userId))
                connectionString = StringUtilities.Replace(connectionString, "{@UserId}", userId);
            if (StringUtilities.IsNotEmpty(password))
                connectionString = StringUtilities.Replace(connectionString, "{@Password}", password);

            var provider = DbProviderRegistry.Get(databaseType);
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
