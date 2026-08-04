using Bee.Repository.Abstractions.Form;

namespace Bee.Repository.Form
{
    /// <summary>
    /// Repository implementation for report forms.
    /// </summary>
    public class ReportFormRepository : RepositoryBase, IReportFormRepository
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReportFormRepository"/> class.
        /// </summary>
        /// <param name="ctx">The shared repository context.</param>
        /// <param name="accessToken">The current request's access token.</param>
        /// <param name="progId">The program identifier.</param>
        /// <remarks>
        /// Declares no scope: a report form carries no schema-driven CRUD, so it has no database of
        /// its own to resolve. Reports reach their data through the business object's own queries.
        /// </remarks>
        public ReportFormRepository(IRepositoryContext ctx, Guid accessToken, string progId)
            : base(ctx, accessToken, progId, null)
        {
        }
    }
}
