using Bee.Definition;
using System.Data.Common;

namespace Bee.Db.Manager
{
    /// <summary>
    /// Holds database connection information.
    /// </summary>
    public class DbConnectionInfo
    {
        /// <summary>
        /// Initializes a new instance of <see cref="DbConnectionInfo"/>.
        /// </summary>
        /// <param name="databaseType">The database type.</param>
        /// <param name="provider">The database provider factory.</param>
        /// <param name="connectionString">The connection string.</param>
        internal DbConnectionInfo(DatabaseType databaseType, DbProviderFactory provider, string connectionString)
        {
            DatabaseType = databaseType;
            Provider = provider;
            ConnectionString = connectionString;
        }

        /// <summary>
        /// Gets the database type.
        /// </summary>
        public DatabaseType DatabaseType { get; }

        /// <summary>
        /// Gets the database provider factory.
        /// </summary>
        public DbProviderFactory Provider { get; }

        /// <summary>
        /// Gets the database connection string.
        /// </summary>
        /// <remarks>
        /// Design note: The connection string (which may contain embedded credentials) is held
        /// as a plain <see cref="string"/> and cached for the application lifetime in
        /// <see cref="DbConnectionManager"/>. This follows standard ADO.NET practice.
        /// Use <c>PersistSecurityInfo=false</c> in connection strings to limit credential
        /// exposure after the connection is opened.
        /// </remarks>
        public string ConnectionString { get; }
    }
}
