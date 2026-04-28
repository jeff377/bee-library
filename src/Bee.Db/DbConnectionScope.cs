using System.Data;
using System.Data.Common;

namespace Bee.Db
{
    /// <summary>
    /// Manages the lifetime of a database connection within a scoped context.
    /// If the connection is created by this class, it is closed on Dispose();
    /// if an external connection is supplied, it is not closed — only opened if necessary.
    /// </summary>
    public sealed class DbConnectionScope : IDisposable
    {
        /// <summary>
        /// Gets the current database connection.
        /// </summary>
        public DbConnection? Connection { get; private set; }

        private readonly bool _ownsConnection;

        private DbConnectionScope(DbConnection connection, bool ownsConnection)
        {
            Connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _ownsConnection = ownsConnection;
        }

        /// <summary>
        /// Creates a connection scope synchronously.
        /// </summary>
        /// <param name="externalConnection">An external connection to reuse; if null, a new connection will be created.</param>
        /// <param name="factory">The database provider factory.</param>
        /// <param name="connectionString">The connection string (used only when creating a new connection).</param>
        /// <param name="onConnectionOpened">
        /// Optional callback invoked after a newly created connection is opened. Not invoked for an
        /// external connection — its initialization is the caller's responsibility.
        /// </param>
        public static DbConnectionScope Create(
            DbConnection? externalConnection,
            DbProviderFactory factory,
            string connectionString,
            Action<DbConnection>? onConnectionOpened = null)
        {
            if (externalConnection != null)
            {
                EnsureOpenSync(externalConnection);
                return new DbConnectionScope(externalConnection, false);
            }

            if (factory == null) throw new ArgumentNullException(nameof(factory), "Factory cannot be null.");

            var conn = factory.CreateConnection()
                       ?? throw new InvalidOperationException("Failed to create database connection: DbProviderFactory.CreateConnection() returned null.");
            conn.ConnectionString = connectionString;
            try
            {
                conn.Open();
                onConnectionOpened?.Invoke(conn);
            }
            catch
            {
                conn.Dispose();
                throw;
            }
            return new DbConnectionScope(conn, true);
        }

        /// <summary>
        /// Creates a connection scope asynchronously.
        /// </summary>
        /// <param name="externalConnection">An external connection to reuse; if null, a new connection will be created.</param>
        /// <param name="factory">The database provider factory.</param>
        /// <param name="connectionString">The connection string (used only when creating a new connection).</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <param name="onConnectionOpened">
        /// Optional callback invoked after a newly created connection is opened. Not invoked for an
        /// external connection — its initialization is the caller's responsibility.
        /// </param>
        public static async Task<DbConnectionScope> CreateAsync(
            DbConnection? externalConnection,
            DbProviderFactory factory,
            string connectionString,
            CancellationToken cancellationToken = default,
            Action<DbConnection>? onConnectionOpened = null)
        {
            if (externalConnection != null)
            {
                await EnsureOpenAsync(externalConnection, cancellationToken).ConfigureAwait(false);
                return new DbConnectionScope(externalConnection, false);
            }

            if (factory == null) throw new ArgumentNullException(nameof(factory), "Factory cannot be null.");

            var conn = factory.CreateConnection()
                       ?? throw new InvalidOperationException("Failed to create database connection: DbProviderFactory.CreateConnection() returned null.");
            conn.ConnectionString = connectionString;
            try
            {
                await conn.OpenAsync(cancellationToken).ConfigureAwait(false);
                onConnectionOpened?.Invoke(conn);
            }
            catch
            {
                await conn.DisposeAsync().ConfigureAwait(false);
                throw;
            }
            return new DbConnectionScope(conn, true);
        }

        /// <summary>
        /// Releases resources. The connection is closed only if it was created by this scope.
        /// </summary>
        public void Dispose()
        {
            if (_ownsConnection)
            {
                Connection?.Dispose();
            }
            Connection = null;
        }

        private static void EnsureOpenSync(DbConnection connection)
        {
            // Treat Broken the same as Closed; re-open if needed
            if (connection.State == ConnectionState.Closed || connection.State == ConnectionState.Broken)
            {
                connection.Open();
            }
        }

        private static async Task EnsureOpenAsync(DbConnection connection, CancellationToken ct)
        {
            if (connection.State == ConnectionState.Closed || connection.State == ConnectionState.Broken)
            {
                await connection.OpenAsync(ct).ConfigureAwait(false);
            }
        }
    }

}
