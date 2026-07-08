using System.Data;
using Bee.Api.Contracts;
using Bee.Definition.Paging;

namespace Bee.Business.AuditLog
{
    /// <summary>
    /// Shared output result for the audit-log list queries (login / access / anomaly): a page of
    /// event-header rows plus paging metadata.
    /// </summary>
    public class LogListResult : BusinessResult, ILogListResponse
    {
        /// <summary>Gets or sets the event header rows for this page.</summary>
        public DataTable? Table { get; set; }

        /// <summary>Gets or sets the paging metadata for this page.</summary>
        public PagingInfo? Paging { get; set; }
    }
}
