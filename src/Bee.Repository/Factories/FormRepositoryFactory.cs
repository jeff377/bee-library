using Bee.Repository.Abstractions.Factories;
using Bee.Repository.Abstractions.Form;
using Bee.Repository.Form;

namespace Bee.Repository.Factories
{
    /// <summary>
    /// Default implementation of <see cref="IFormRepositoryFactory"/>.
    /// </summary>
    public class FormRepositoryFactory : IFormRepositoryFactory
    {
        /// <summary>
        /// Creates an <see cref="IDataFormRepository"/> for the specified ProgId.
        /// </summary>
        /// <param name="progId">The program identifier.</param>
        public IDataFormRepository CreateDataFormRepository(string progId)
        {
            return new DataFormRepository(progId);
        }

        /// <summary>
        /// Creates an <see cref="IReportFormRepository"/> for the specified ProgId.
        /// </summary>
        /// <param name="progId">The program identifier.</param>
        public IReportFormRepository CreateReportFormRepository(string progId)
        {
            return new ReportFormRepository(progId);
        }
    }
}
