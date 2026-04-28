using Bee.Repository.Abstractions.Form;

namespace Bee.Repository.Abstractions.Factories
{
    /// <summary>
    /// Factory for creating form-level repositories. Each form is identified by its ProgId.
    /// </summary>
    public interface IFormRepositoryFactory
    {
        /// <summary>
        /// Creates an <see cref="IDataFormRepository"/> for the specified ProgId.
        /// </summary>
        /// <param name="progId">The program identifier.</param>
        IDataFormRepository CreateDataFormRepository(string progId);

        /// <summary>
        /// Creates an <see cref="IReportFormRepository"/> for the specified ProgId.
        /// </summary>
        /// <param name="progId">The program identifier.</param>
        IReportFormRepository CreateReportFormRepository(string progId);
    }
}
