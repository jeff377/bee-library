using Bee.Base;
using Bee.Definition;
using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;

namespace Bee.Db.Manager
{
    /// <summary>
    /// Default <see cref="IDbConnectionManager"/> implementation: resolves cached
    /// <see cref="DbConnectionInfo"/> values from an injected
    /// <see cref="IDatabaseSettingsProvider"/> and refreshes them when the underlying
    /// database settings change.
    /// </summary>
    /// <remarks>
    /// Registered as a Singleton by <c>AddBeeFramework</c>. The legacy static
    /// <see cref="DbConnectionManager"/> facade delegates to a process-wide instance of
    /// this class for transitional callers that have not yet adopted ctor injection.
    /// </remarks>
    public sealed class DbConnectionManagerService : IDbConnectionManager
    {
        private readonly IDatabaseSettingsProvider _provider;
        private readonly ConcurrentDictionary<string, DbConnectionInfo> _cache = new();

        /// <summary>
        /// Initializes a new <see cref="DbConnectionManagerService"/> bound to the supplied
        /// <see cref="IDatabaseSettingsProvider"/>.
        /// </summary>
        /// <param name="provider">The database settings provider.</param>
        public DbConnectionManagerService(IDatabaseSettingsProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            GlobalEvents.DatabaseSettingsChanged += OnDatabaseSettingsChanged;
        }

        private void OnDatabaseSettingsChanged(object? sender, EventArgs e) => Clear();

        /// <inheritdoc/>
        public DbConnectionInfo GetConnectionInfo(string databaseId)
        {
            if (string.IsNullOrWhiteSpace(databaseId))
                throw new ArgumentNullException(nameof(databaseId), "Database ID cannot be null or empty.");

            return _cache.GetOrAdd(databaseId, CreateConnectionInfo);
        }

        private DbConnectionInfo CreateConnectionInfo(string databaseId)
        {
            var settings = _provider.Get();

            var databaseItem = settings.Items![databaseId];
            if (databaseItem == null)
                throw new InvalidOperationException($"DatabaseItem for id '{databaseId}' was not found.");

            var databaseType = databaseItem.DatabaseType;
            string connectionString = databaseItem.ConnectionString;
            string userId = databaseItem.UserId;
            string password = databaseItem.Password;
            string dbName = databaseItem.DbName;

            if (StringUtilities.IsNotEmpty(databaseItem.ServerId))
            {
                var server = settings.Servers![databaseItem.ServerId];
                if (server == null)
                {
                    throw new InvalidOperationException(
                        $"DatabaseServer '{databaseItem.ServerId}' referenced by DatabaseItem '{databaseId}' was not found.");
                }

                connectionString = server.ConnectionString;
                databaseType = server.DatabaseType;

                if (StringUtilities.IsEmpty(userId))
                    userId = server.UserId;
                if (StringUtilities.IsEmpty(password))
                    password = server.Password;
            }

            if (StringUtilities.IsNotEmpty(dbName))
                connectionString = StringUtilities.Replace(connectionString, "{@DbName}", dbName);
            if (StringUtilities.IsNotEmpty(userId))
                connectionString = StringUtilities.Replace(connectionString, "{@UserId}", userId);
            if (StringUtilities.IsNotEmpty(password))
                connectionString = StringUtilities.Replace(connectionString, "{@Password}", password);

            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException($"Connection string for database '{databaseId}' is null or empty.");

            var provider = DbProviderRegistry.Get(databaseType)
                ?? throw new InvalidOperationException($"Unknown database type: {databaseType}.");

            return new DbConnectionInfo(databaseType, provider, connectionString);
        }

        /// <inheritdoc/>
        public DbConnection CreateConnection(string databaseId)
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

        /// <inheritdoc/>
        public bool Remove(string databaseId) => _cache.TryRemove(databaseId, out _);

        /// <inheritdoc/>
        public void Clear() => _cache.Clear();

        /// <inheritdoc/>
        public bool Contains(string databaseId) => _cache.ContainsKey(databaseId);

        /// <inheritdoc/>
        public int Count => _cache.Count;
    }
}
