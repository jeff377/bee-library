using Bee.Repository.Abstractions.Form;

namespace Bee.Repository.Abstractions.Provider
{
    /// <summary>
    /// Interface for the form repository provider.
    /// </summary>
    public interface IFormRepositoryProvider
    {
        /// <summary>
        /// Gets the <see cref="IDataFormRepository"/> corresponding to the specified ProgId.
        /// </summary>
        /// <param name="progId">The program identifier.</param>
        IDataFormRepository GetDataFormRepository(string progId);

        /// <summary>
        /// Gets the <see cref="IReportFormRepository"/> corresponding to the specified ProgId.
        /// </summary>
        /// <param name="progId">The program identifier.</param>
        IReportFormRepository GetReportFormRepository(string progId);
    }
}
