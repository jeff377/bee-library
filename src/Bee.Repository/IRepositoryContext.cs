using Bee.Db;
using Bee.Db.CacheNotify;
using Bee.Db.Manager;
using Bee.Definition.Storage;
using Bee.Repository.Abstractions;

namespace Bee.Repository
{
    /// <summary>
    /// Construction-time context handed to every repository. The data-access counterpart of
    /// <see cref="Bee.Definition.IBeeContext"/>: it aggregates the cross-cutting services a repository needs so that
    /// every repository can share one constructor signature.
    /// </summary>
    /// <remarks>
    /// Lives in <c>Bee.Repository</c> rather than <c>Bee.Repository.Abstractions</c> because its
    /// members are <c>Bee.Db</c> types. Consumers only ever name <see cref="Bee.Repository.Abstractions.Factories.IRepositoryFactory"/>, which
    /// does stay in the abstractions package, so <c>Bee.Business</c> and <c>Bee.ObjectCaching</c>
    /// keep their present dependencies. Anything that names this interface — a repository, or a
    /// host writing its own — already references <c>Bee.Repository</c> for the base class.
    /// </remarks>
    public interface IRepositoryContext
    {
        /// <summary>The definition data access service (FormSchema / TableSchema lookups).</summary>
        IDefineAccess DefineAccess { get; }

        /// <summary>The connection manager (dialect + connection resolution).</summary>
        IDbConnectionManager ConnectionManager { get; }

        /// <summary>The database access factory.</summary>
        IDbAccessFactory DbAccessFactory { get; }

        /// <summary>Resolves a logical scope to a physical database id.</summary>
        IRepositoryDatabaseRouter Router { get; }

        /// <summary>
        /// Cross-process cache invalidation channel; <c>null</c> when the host does not poll it.
        /// </summary>
        /// <remarks>
        /// Held here rather than injected only into the one repository that writes cache-backed data.
        /// It is nullable and unused by default, so carrying it costs nothing, whereas a special
        /// constructor for its single consumer would defeat the uniform signature this context
        /// exists to enable. Which repositories actually use it is a grep away.
        /// </remarks>
        ICacheNotifyService? CacheNotify { get; }

        /// <summary>Escape hatch for services not in the typed core members. Use sparingly.</summary>
        IServiceProvider Services { get; }
    }
}
