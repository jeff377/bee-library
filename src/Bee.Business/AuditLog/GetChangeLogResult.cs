using System.Data;
using Bee.Api.Contracts;
using Bee.Definition.Paging;

namespace Bee.Business.AuditLog
{
    /// <summary>
    /// Output result for the change-log list query: a page of <c>st_log_change</c> event headers.
    /// </summary>
    public class GetChangeLogResult : BusinessResult, IGetChangeLogResponse
    {
        /// <summary>Gets or sets the change-event header rows.</summary>
        public DataTable? Table { get; set; }

        /// <summary>Gets or sets the paging metadata for this page.</summary>
        public PagingInfo? Paging { get; set; }
    }
}
