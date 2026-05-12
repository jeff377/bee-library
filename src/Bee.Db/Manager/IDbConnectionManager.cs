using System.Data.Common;

namespace Bee.Db.Manager
{
    /// <summary>
    /// Resolves cached <see cref="DbConnectionInfo"/> instances and creates new
    /// <see cref="DbConnection"/> objects for a given database identifier.
    /// </summary>
    /// <remarks>
    /// Phase 5 PR 5.3b introduced this interface alongside the existing
    /// <see cref="DbConnectionManager"/> static facade. Consumers should ctor-inject
    /// <see cref="IDbConnectionManager"/>; the static class remains as a transitional
    /// shim for tests and is removed once test fixtures finish migrating.
    /// </remarks>
    public interface IDbConnectionManager
    {
        /// <summary>
        /// Gets or creates the connection information for the specified database (cached).
        /// </summary>
        /// <param name="databaseId">The database identifier.</param>
        DbConnectionInfo GetConnectionInfo(string databaseId);

        /// <summary>
        /// Creates a fresh <see cref="DbConnection"/> for the specified database identifier;
        /// any connection-open initializer registered for the underlying provider is wired
        /// to the connection's <see cref="DbConnection.StateChange"/> event.
        /// </summary>
        /// <param name="databaseId">The database identifier.</param>
        DbConnection CreateConnection(string databaseId);

        /// <summary>
        /// Removes the cached connection information for the specified database.
        /// </summary>
        /// <param name="databaseId">The database identifier.</param>
        bool Remove(string databaseId);

        /// <summary>
        /// Clears all cached connection information.
        /// </summary>
        void Clear();

        /// <summary>
        /// Determines whether the connection information for the specified database is cached.
        /// </summary>
        /// <param name="databaseId">The database identifier.</param>
        bool Contains(string databaseId);

        /// <summary>
        /// Gets the number of cached connection information entries.
        /// </summary>
        int Count { get; }
    }
}
