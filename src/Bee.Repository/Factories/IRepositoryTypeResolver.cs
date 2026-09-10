using Bee.Repository.Form;

namespace Bee.Repository.Factories
{
    /// <summary>
    /// Resolves the concrete form repository type bound to a progId.
    /// </summary>
    /// <remarks>
    /// The repository-axis counterpart of the business-object type resolver (<c>IBoTypeResolver</c>).
    /// <see cref="RepositoryFactory"/> asks it which type to build and then builds it, so a host
    /// that only wants to change a binding registers its own implementation rather than
    /// subclassing the factory.
    /// <para>
    /// It takes the access token rather than a customization code. The code selects which
    /// tenant's definitions are read, so it has to come from the session and never from the
    /// caller; an implementation reads it from the session itself, and the signature leaves no
    /// parameter through which a caller could supply one.
    /// </para>
    /// <para>
    /// The returned type must be <see cref="DataFormRepository"/> or derive from it.
    /// <see cref="RepositoryFactory"/> checks that for whatever resolver it was given and throws
    /// <see cref="InvalidOperationException"/> when it does not hold.
    /// </para>
    /// </remarks>
    public interface IRepositoryTypeResolver
    {
        /// <summary>
        /// Returns the repository type to build for a progId in the given session.
        /// </summary>
        /// <param name="accessToken">The current request's access token.</param>
        /// <param name="progId">The program identifier.</param>
        /// <returns><see cref="DataFormRepository"/> itself, or a type derived from it.</returns>
        Type Resolve(Guid accessToken, string progId);
    }
}
