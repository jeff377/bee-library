using Bee.Db;
using Bee.Db.CacheNotify;
using Bee.Db.Manager;
using Bee.Definition.Storage;
using Bee.Repository.Abstractions;

namespace Bee.Repository
{
    /// <summary>
    /// Default <see cref="IRepositoryContext"/>. Built once by <see cref="Factories.RepositoryFactory"/>
    /// from its own injected services and handed to every repository it creates.
    /// </summary>
    /// <remarks>
    /// Holds application-lifetime services only. Per-call state — the access token, the progId —
    /// stays in the constructor parameters beside it, so one context instance is safely shared by
    /// every repository the factory builds.
    /// </remarks>
    public sealed class RepositoryContext : IRepositoryContext
    {
        /// <inheritdoc/>
        public required IDefineAccess DefineAccess { get; init; }

        /// <inheritdoc/>
        public required IDbConnectionManager ConnectionManager { get; init; }

        /// <inheritdoc/>
        public required IDbAccessFactory DbAccessFactory { get; init; }

        /// <inheritdoc/>
        public required IRepositoryDatabaseRouter Router { get; init; }

        /// <inheritdoc/>
        public ICacheNotifyService? CacheNotify { get; init; }

        /// <inheritdoc/>
        public required IServiceProvider Services { get; init; }
    }
}
