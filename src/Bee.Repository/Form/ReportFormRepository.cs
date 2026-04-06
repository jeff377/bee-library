using Bee.Repository.Abstractions.Form;

namespace Bee.Repository.Form
{
    /// <summary>
    /// Repository implementation for report forms.
    /// </summary>
    public class ReportFormRepository : IReportFormRepository
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReportFormRepository"/> class.
        /// </summary>
        /// <param name="progId">The program identifier.</param>
        public ReportFormRepository(string progId)
        {
            ProgId = progId;
        }

        /// <summary>
        /// Gets the program identifier.
        /// </summary>
        public string ProgId { get; }
    }
}
