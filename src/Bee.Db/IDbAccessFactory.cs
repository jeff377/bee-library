using Bee.Db.Manager;

namespace Bee.Db
{
    /// <summary>
    /// Creates <see cref="DbAccess"/> instances bound to the per-app configuration
    /// (such as the <see cref="System.Data.Common.DbCommand.CommandTimeout"/> cap).
    /// </summary>
    public interface IDbAccessFactory
    {
        /// <summary>
        /// Creates a <see cref="DbAccess"/> instance for the specified database identifier.
        /// </summary>
        /// <param name="databaseId">The database identifier (e.g. <c>common</c>, <c>company</c>).</param>
        DbAccess Create(string databaseId);
    }

    /// <summary>
    /// Default <see cref="IDbAccessFactory"/> implementation. Holds the per-app
    /// <see cref="System.Data.Common.DbCommand.CommandTimeout"/> cap and propagates it to each
    /// <see cref="DbAccess"/> instance it creates.
    /// </summary>
    /// <remarks>
    /// Typical host registration (per-app cap differs by deployment):
    /// <list type="bullet">
    /// <item>Mobile API backend: 30 sec</item>
    /// <item>Web backend: 60 sec</item>
    /// <item>Scheduled batch service: 120 sec</item>
    /// </list>
    /// </remarks>
    public sealed class DbAccessFactory : IDbAccessFactory
    {
        private readonly IDbConnectionManager _connectionManager;
        private readonly int _maxCommandTimeout;

        /// <summary>
        /// Initializes a new <see cref="DbAccessFactory"/>.
        /// </summary>
        /// <param name="connectionManager">The DI-resolved connection manager.</param>
        /// <param name="maxCommandTimeout">
        /// Per-app upper bound applied to each <see cref="System.Data.Common.DbCommand.CommandTimeout"/>;
        /// 0 (default) disables the cap.
        /// </param>
        public DbAccessFactory(IDbConnectionManager connectionManager, int maxCommandTimeout = 0)
        {
            _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
            _maxCommandTimeout = maxCommandTimeout;
        }

        /// <inheritdoc/>
        public DbAccess Create(string databaseId)
            => new DbAccess(databaseId, _connectionManager, _maxCommandTimeout);
    }
}
