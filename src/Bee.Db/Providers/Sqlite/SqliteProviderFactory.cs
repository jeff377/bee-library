using System.Data.Common;

namespace Bee.Db.Providers.Sqlite
{
    /// <summary>
    /// Wraps an inner Sqlite <see cref="DbProviderFactory"/> (typically
    /// <c>Microsoft.Data.Sqlite.SqliteFactory.Instance</c>) to add the
    /// <see cref="DbDataAdapter"/> that Microsoft.Data.Sqlite omits. Every member delegates to the
    /// inner factory; only the adapter hooks are overridden. Registering this wrapper (instead of
    /// the raw Sqlite factory) lets the framework drive SQLite through the same adapter-based
    /// read/write path as the other providers, so no bespoke "no adapter" fallback is needed.
    /// </summary>
    /// <remarks>
    /// The inner factory is taken as the abstract <see cref="DbProviderFactory"/> so this type lives
    /// in <c>Bee.Db</c> without forcing a Microsoft.Data.Sqlite package reference on every consumer;
    /// the host supplies the concrete Sqlite factory at registration time.
    /// </remarks>
    public sealed class SqliteProviderFactory : DbProviderFactory
    {
        private readonly DbProviderFactory _inner;

        /// <summary>
        /// Initializes a new instance wrapping the given Sqlite provider factory.
        /// </summary>
        /// <param name="inner">The concrete Sqlite factory (e.g. <c>SqliteFactory.Instance</c>).</param>
        public SqliteProviderFactory(DbProviderFactory inner)
            => _inner = inner ?? throw new ArgumentNullException(nameof(inner));

        /// <inheritdoc/>
        public override bool CanCreateDataAdapter => true;

        /// <inheritdoc/>
        public override DbDataAdapter CreateDataAdapter() => new SqliteDataAdapter();

        /// <inheritdoc/>
        public override DbConnection? CreateConnection() => _inner.CreateConnection();

        /// <inheritdoc/>
        public override DbCommand? CreateCommand() => _inner.CreateCommand();

        /// <inheritdoc/>
        public override DbParameter? CreateParameter() => _inner.CreateParameter();

        /// <inheritdoc/>
        public override DbCommandBuilder? CreateCommandBuilder() => _inner.CreateCommandBuilder();

        /// <inheritdoc/>
        public override DbConnectionStringBuilder? CreateConnectionStringBuilder() => _inner.CreateConnectionStringBuilder();
    }
}
