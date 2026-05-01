using Bee.Base;
using Bee.Definition;
using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;

namespace Bee.Db.Manager
{
    /// <summary>
    /// Resolves and caches database connection information.
    /// Combines DatabaseItem, DatabaseServer, and <see cref="DbProviderRegistry"/>
    /// data into ready-to-use <see cref="DbConnectionInfo"/> instances,
    /// then caches the result and refreshes when database settings change.
    /// </summary>
    public static class DbConnectionManager
    {
        /// <summary>
        /// Static constructor; runs when the class is first referenced.
        /// </summary>
        static DbConnectionManager()
        {
            // Subscribe to database settings changed events
            GlobalEvents.DatabaseSettingsChanged += OnDatabaseSettingsChanged;
        }

        private static void OnDatabaseSettingsChanged(object? sender, EventArgs e)
        {
            Clear();
        }

        /// <summary>
        /// Thread-safe cache of connection information.
        /// </summary>
        private static readonly ConcurrentDictionary<string, DbConnectionInfo> _cache
            = new ConcurrentDictionary<string, DbConnectionInfo>();

        /// <summary>
        /// Gets or creates the connection information for the specified database (with caching).
        /// The first call creates and caches the connection info; subsequent calls return the cached result.
        /// </summary>
        /// <param name="databaseId">The database identifier.</param>
        /// <returns>A <see cref="DbConnectionInfo"/> containing the database type, provider, and connection string.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="databaseId"/> is null or empty.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the database item is not found, the provider is not registered, or the connection string is invalid.</exception>
        public static DbConnectionInfo GetConnectionInfo(string databaseId)
        {
            if (string.IsNullOrWhiteSpace(databaseId))
                throw new ArgumentNullException(nameof(databaseId), "Database ID cannot be null or empty.");

            return _cache.GetOrAdd(databaseId, CreateConnectionInfo);
        }

        /// <summary>
        /// Creates a new <see cref="DbConnectionInfo"/> for the specified database identifier.
        /// </summary>
        /// <param name="databaseId">The database identifier.</param>
        /// <returns>The newly created connection information object.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the database item is not found, the provider is not registered, or the connection string is invalid.</exception>
        private static DbConnectionInfo CreateConnectionInfo(string databaseId)
        {
            // Retrieve database settings via DefineAccess (internally cached)
            var settings = BackendInfo.DefineAccess.GetDatabaseSettings();

            // Get the database item
            var databaseItem = settings.Items![databaseId];
            if (databaseItem == null)
                throw new InvalidOperationException($"DatabaseItem for id '{databaseId}' was not found.");

            // Default to the DatabaseItem settings
            var databaseType = databaseItem.DatabaseType;
            string connectionString = databaseItem.ConnectionString;
            string userId = databaseItem.UserId;
            string password = databaseItem.Password;
            string dbName = databaseItem.DbName;

            // If a ServerId is specified, retrieve the connection string template from the corresponding server
            if (StrFunc.IsNotEmpty(databaseItem.ServerId))
            {
                var server = settings.Servers![databaseItem.ServerId];
                if (server == null)
                {
                    throw new InvalidOperationException(
                        $"DatabaseServer '{databaseItem.ServerId}' referenced by DatabaseItem '{databaseId}' was not found.");
                }

                // Use the server settings as the base
                connectionString = server.ConnectionString;
                databaseType = server.DatabaseType;

                // DatabaseItem can override the server's UserId/Password if specified
                if (StrFunc.IsEmpty(userId))
                    userId = server.UserId;
                if (StrFunc.IsEmpty(password))
                    password = server.Password;
            }

            // Substitute placeholders in the connection string
            if (StrFunc.IsNotEmpty(dbName))
                connectionString = StrFunc.Replace(connectionString, "{@DbName}", dbName);
            if (StrFunc.IsNotEmpty(userId))
                connectionString = StrFunc.Replace(connectionString, "{@UserId}", userId);
            if (StrFunc.IsNotEmpty(password))
                connectionString = StrFunc.Replace(connectionString, "{@Password}", password);

            // Validate the connection string
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException($"Connection string for database '{databaseId}' is null or empty.");

            // Retrieve the database provider factory
            var provider = DbProviderRegistry.Get(databaseType)
                ?? throw new InvalidOperationException($"Unknown database type: {databaseType}.");

            return new DbConnectionInfo(databaseType, provider, connectionString);
        }

        /// <summary>
        /// Creates a database connection for the specified database identifier.
        /// If a connection initializer is registered for the underlying database type
        /// (see <see cref="DbProviderRegistry.GetConnectionInitializer"/>), it is wired to
        /// the connection's <see cref="DbConnection.StateChange"/> event so that it runs
        /// automatically each time the connection transitions from <c>Closed</c> to <c>Open</c>.
        /// </summary>
        /// <param name="databaseId">The database identifier.</param>
        public static DbConnection CreateConnection(string databaseId)
        {
            var connInfo = GetConnectionInfo(databaseId);

            var provider = DbProviderRegistry.Get(connInfo.DatabaseType)
                    ?? throw new InvalidOperationException($"Unknown database type: {connInfo.DatabaseType}.");
            var connection = provider.CreateConnection()
                    ?? throw new InvalidOperationException("Failed to create a database connection: DbProviderFactory.CreateConnection() returned null.");
            connection.ConnectionString = connInfo.ConnectionString;

            var initializer = DbProviderRegistry.GetConnectionInitializer(connInfo.DatabaseType);
            if (initializer != null)
            {
                connection.StateChange += (sender, e) =>
                {
                    if (e.OriginalState == ConnectionState.Closed && e.CurrentState == ConnectionState.Open)
                        initializer((DbConnection)sender!);
                };
            }
            return connection;
        }

        /// <summary>
        /// Removes the cached connection information for the specified database (used when settings change).
        /// </summary>
        /// <param name="databaseId">The database identifier.</param>
        /// <returns><c>true</c> if the cache entry was successfully removed; <c>false</c> if it did not exist.</returns>
        public static bool Remove(string databaseId)
        {
            return _cache.TryRemove(databaseId, out _);
        }

        /// <summary>
        /// Clears all cached connection information.
        /// </summary>
        public static void Clear()
        {
            _cache.Clear();
        }

        /// <summary>
        /// Determines whether the connection information for the specified database is cached.
        /// </summary>
        /// <param name="databaseId">The database identifier.</param>
        /// <returns><c>true</c> if the entry is cached; otherwise, <c>false</c>.</returns>
        public static bool Contains(string databaseId)
        {
            return _cache.ContainsKey(databaseId);
        }

        /// <summary>
        /// Gets the number of connection information entries currently in the cache.
        /// </summary>
        public static int Count => _cache.Count;
    }
}
