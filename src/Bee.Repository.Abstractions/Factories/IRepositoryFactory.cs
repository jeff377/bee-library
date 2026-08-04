using Bee.Repository.Abstractions.Form;

namespace Bee.Repository.Abstractions.Factories
{
    /// <summary>
    /// The single entry point for obtaining a repository, on either of the two axes the framework
    /// has.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The progId axis</b> (<see cref="CreateFormRepository{T}"/>): the type varies per progId
    /// and is resolved through the registry, mirroring
    /// <c>IBusinessObjectFactory.CreateBusinessObject(token, progId)</c>. This is where one business
    /// object owns one repository.
    /// </para>
    /// <para>
    /// <b>The framework axis</b> (<see cref="Create{T}"/>): the type is fixed and there is no progId
    /// to speak of. Its repositories are shared infrastructure — the session store, the key store,
    /// the per-company organisation tables — read by background services and cache providers that
    /// are not business objects and have no request to belong to. Naming the interface is the honest
    /// way to ask for one; inventing a progId for it would not be.
    /// </para>
    /// <para>
    /// Both methods are generic, so adding a repository never widens this interface. That is the
    /// point: the factory it replaces grew a method per system table.
    /// </para>
    /// </remarks>
    public interface IRepositoryFactory
    {
        /// <summary>
        /// Creates the repository registered for the supplied progId, routed to the database its
        /// form schema's category resolves to.
        /// </summary>
        /// <typeparam name="T">
        /// The repository interface to return. Use <see cref="IDataFormRepository"/> for the
        /// framework's own CRUD surface, or a form's own <c>IXxxRepository</c> extension of it.
        /// </typeparam>
        /// <param name="accessToken">The current request's access token; required when the schema's category resolves to a company database.</param>
        /// <param name="progId">The program identifier.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the resolved repository is not assignable to <typeparamref name="T"/>, which
        /// means the registry binds this progId to a repository that does not implement the
        /// interface the caller expects.
        /// </exception>
        T CreateFormRepository<T>(Guid accessToken, string progId) where T : class, IDataFormRepository;

        /// <summary>
        /// Creates a framework repository by its interface.
        /// </summary>
        /// <typeparam name="T">The repository interface (for example <c>ISessionRepository</c>).</typeparam>
        /// <param name="accessToken">
        /// The access token, for the repositories that route to a company database.
        /// <see cref="Guid.Empty"/> is the correct value outside a request — background services and
        /// cache providers have no session — and is accepted by every repository that does not need
        /// one.
        /// </param>
        /// <exception cref="NotSupportedException">Thrown when no implementation is registered for <typeparamref name="T"/>.</exception>
        T Create<T>(Guid accessToken = default) where T : class;
    }
}
