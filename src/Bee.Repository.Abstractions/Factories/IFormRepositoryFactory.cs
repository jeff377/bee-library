using Bee.Repository.Abstractions.Form;

namespace Bee.Repository.Abstractions.Factories
{
    /// <summary>
    /// Factory for creating form-level repositories. Each form is identified by its ProgId.
    /// </summary>
    public interface IFormRepositoryFactory
    {
        /// <summary>
        /// Creates an <see cref="IDataFormRepository"/> for the specified ProgId,
        /// routing to the physical database via <c>IRepositoryDatabaseRouter</c>
        /// using the supplied access token.
        /// </summary>
        /// <param name="progId">The program identifier.</param>
        /// <param name="accessToken">The current request's access token; required
        /// when the form schema's <c>CategoryId</c> resolves to
        /// <c>DbScope.Company</c>.</param>
        IDataFormRepository CreateDataFormRepository(string progId, Guid accessToken);

        /// <summary>
        /// Creates an <see cref="IReportFormRepository"/> for the specified ProgId.
        /// </summary>
        /// <param name="progId">The program identifier.</param>
        IReportFormRepository CreateReportFormRepository(string progId);
    }
}
