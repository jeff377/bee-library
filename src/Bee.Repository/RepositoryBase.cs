using Bee.Db;
using Bee.Definition;

namespace Bee.Repository
{
    /// <summary>
    /// Base class for every repository. Carries the shared construction context and resolves the
    /// target database once, so the nine framework repositories no longer each repeat the same
    /// wiring.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The uniform constructor is what makes the registry work.</b> A repository can be created
    /// from a type name only if every repository is created the same way — the same property that
    /// lets <c>BusinessObject</c> be built by the business object factory.
    /// </para>
    /// <para>
    /// <b>Routing lives here, not in the factory.</b> Before this, the target database was reached
    /// three different ways: injected by the factory (form repositories), hard-coded per method
    /// (the common-scope ones), or handed in by the caller. Declaring the scope on the type makes
    /// it one thing, and one that can be read off the class rather than hunted for among its
    /// method bodies.
    /// </para>
    /// </remarks>
    public abstract class RepositoryBase
    {
        /// <summary>
        /// Initializes a new <see cref="RepositoryBase"/> and eagerly resolves
        /// <see cref="DatabaseId"/> when the scope calls for it.
        /// </summary>
        /// <param name="ctx">The shared construction context.</param>
        /// <param name="accessToken">The current request's access token; <see cref="Guid.Empty"/> outside a request.</param>
        /// <param name="progId">The program identifier; empty for the framework axis.</param>
        /// <param name="scope">
        /// The logical database this repository reads and writes, or <c>null</c> when it has none of
        /// its own and every method is told which database to use.
        /// </param>
        /// <exception cref="UnauthorizedAccessException">Thrown when the scope is company-bound and the session is missing or expired.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the scope is company-bound and the session has not entered a company.</exception>
        /// <remarks>
        /// The resolution is eager so a bad route fails while the repository is being built, before
        /// any data has been touched — the timing the form factory already had. Scope is a
        /// constructor parameter rather than an overridable property so nothing virtual is called
        /// during construction.
        /// </remarks>
        protected RepositoryBase(IRepositoryContext ctx, Guid accessToken, string progId, DbScope? scope)
        {
            Context = ctx ?? throw new ArgumentNullException(nameof(ctx));
            AccessToken = accessToken;
            ProgId = progId ?? string.Empty;
            Scope = scope;
            DatabaseId = scope is { } value ? ctx.Router.Resolve(value, accessToken) : string.Empty;
        }

        /// <summary>
        /// Initializes a new <see cref="RepositoryBase"/> bound to an explicit database, skipping
        /// the router entirely.
        /// </summary>
        /// <param name="ctx">The shared construction context.</param>
        /// <param name="progId">The program identifier; empty for the framework axis.</param>
        /// <param name="databaseId">The database this repository reads and writes.</param>
        /// <remarks>
        /// For callers that already know the target database and have no token to route with, and
        /// for tests that would otherwise need a working router to build a repository at all. A
        /// separate constructor rather than an optional parameter, so the two construction paths are
        /// distinguishable at the call site.
        /// </remarks>
        protected RepositoryBase(IRepositoryContext ctx, string progId, string databaseId)
        {
            Context = ctx ?? throw new ArgumentNullException(nameof(ctx));
            ArgumentException.ThrowIfNullOrWhiteSpace(databaseId);
            AccessToken = Guid.Empty;
            ProgId = progId ?? string.Empty;
            Scope = null;
            DatabaseId = databaseId;
        }

        /// <summary>Gets the shared construction context.</summary>
        protected IRepositoryContext Context { get; }

        /// <summary>Gets the access token this repository was built for.</summary>
        protected Guid AccessToken { get; }

        /// <summary>Gets the program identifier; empty on the framework axis.</summary>
        public string ProgId { get; }

        /// <summary>
        /// Gets the logical database this repository is bound to, or <c>null</c> when its methods
        /// each take a database id instead.
        /// </summary>
        protected DbScope? Scope { get; }

        /// <summary>
        /// Gets the resolved physical database id, or an empty string when <see cref="Scope"/> is
        /// <c>null</c> and the caller supplies one per method.
        /// </summary>
        protected string DatabaseId { get; }

        /// <summary>
        /// Creates a <see cref="DbAccess"/> against this repository's resolved database.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when this repository has no database of its own.</exception>
        protected DbAccess CreateDbAccess()
        {
            if (string.IsNullOrEmpty(DatabaseId))
            {
                throw new InvalidOperationException(
                    $"{GetType().Name} is not bound to a database; call the overload that takes a databaseId.");
            }
            return new DbAccess(DatabaseId, Context.ConnectionManager);
        }

        /// <summary>
        /// Creates a <see cref="DbAccess"/> against the supplied database.
        /// </summary>
        /// <param name="databaseId">The target database id.</param>
        protected DbAccess CreateDbAccess(string databaseId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(databaseId);
            return new DbAccess(databaseId, Context.ConnectionManager);
        }
    }
}
