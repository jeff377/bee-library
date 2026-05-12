using Bee.Definition;
using System.Data.Common;

namespace Bee.Db.Manager
{
    /// <summary>
    /// Transitional static facade over <see cref="IDbConnectionManager"/>. Phase 5 PR 5.3b
    /// moved the canonical implementation to <see cref="DbConnectionManagerService"/> and
    /// reserved <see cref="IDbConnectionManager"/> for ctor injection. This shim continues
    /// to serve direct <c>new DbAccess("id")</c> test call sites until the test fixture
    /// rewrite in PR 5.4 finishes migrating them.
    /// </summary>
    public static class DbConnectionManager
    {
        private static IDbConnectionManager? _instance;

        /// <summary>
        /// Installs a process-wide <see cref="IDbConnectionManager"/> instance. Called by
        /// the framework bootstrapper at host startup (or by test fixtures bootstrapping
        /// the legacy static path).
        /// </summary>
        /// <param name="instance">The instance to install. Replaces the previous one if any.</param>
        public static void Initialize(IDbConnectionManager instance)
        {
            _instance = instance ?? throw new ArgumentNullException(nameof(instance));
        }

        /// <summary>
        /// Installs a process-wide instance backed by the supplied database settings provider.
        /// Convenience overload that preserves the pre-PR-5.3b API shape used by tests.
        /// </summary>
        /// <param name="provider">The database settings provider.</param>
        public static void Initialize(IDatabaseSettingsProvider provider)
        {
            Initialize(new DbConnectionManagerService(provider));
        }

        private static IDbConnectionManager Instance
            => _instance ?? throw new InvalidOperationException(
                "DbConnectionManager has not been initialized. Call DbConnectionManager.Initialize(provider) before use.");

        /// <summary>
        /// Gets or creates the connection information for the specified database (cached).
        /// </summary>
        public static DbConnectionInfo GetConnectionInfo(string databaseId) => Instance.GetConnectionInfo(databaseId);

        /// <summary>
        /// Creates a database connection for the specified database identifier.
        /// </summary>
        public static DbConnection CreateConnection(string databaseId) => Instance.CreateConnection(databaseId);

        /// <summary>
        /// Removes the cached connection information for the specified database.
        /// </summary>
        public static bool Remove(string databaseId) => Instance.Remove(databaseId);

        /// <summary>
        /// Clears all cached connection information.
        /// </summary>
        public static void Clear() => Instance.Clear();

        /// <summary>
        /// Determines whether the connection information for the specified database is cached.
        /// </summary>
        public static bool Contains(string databaseId) => Instance.Contains(databaseId);

        /// <summary>
        /// Gets the number of cached connection information entries.
        /// </summary>
        public static int Count => Instance.Count;
    }
}
