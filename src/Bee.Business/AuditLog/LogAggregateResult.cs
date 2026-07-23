using System.Data;
using Bee.Api.Contracts.AuditLog;

namespace Bee.Business.AuditLog
{
    /// <summary>
    /// Shared output result for the audit-log aggregate queries (anomaly summary / top-N): a bounded,
    /// unpaged summary result set.
    /// </summary>
    public class LogAggregateResult : BusinessResult, ILogAggregateResponse
    {
        /// <summary>Gets or sets the aggregate result rows.</summary>
        public DataTable? Table { get; set; }
    }
}
