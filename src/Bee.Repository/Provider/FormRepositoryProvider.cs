using Bee.Repository.Abstractions.Form;
using Bee.Repository.Abstractions.Provider;
using Bee.Repository.Form;

namespace Bee.Repository.Provider
{
    /// <summary>
    /// Default implementation of the form repository provider.
    /// </summary>
    public class FormRepositoryProvider : IFormRepositoryProvider
    {
        /// <summary>
        /// Gets the <see cref="IDataFormRepository"/> corresponding to the specified ProgId.
        /// </summary>
        /// <param name="progId">The program identifier.</param>
        public IDataFormRepository GetDataFormRepository(string progId)
        {
            return new DataFormRepository(progId);
        }

        /// <summary>
        /// Gets the <see cref="IReportFormRepository"/> corresponding to the specified ProgId.
        /// </summary>
        /// <param name="progId">The program identifier.</param>
        public IReportFormRepository GetReportFormRepository(string progId)
        {
            return new ReportFormRepository(progId);
        }
    }
}
