using Bee.Api.Contracts.AuditLog;

namespace Bee.Business.AuditLog
{
    /// <summary>
    /// Input arguments for the API-anomaly summary query.
    /// </summary>
    public class GetApiAnomalySummaryArgs : BusinessArgs, IGetApiAnomalySummaryRequest
    {
        /// <summary>Gets or sets the inclusive lower bound on the event time (UTC).</summary>
        public DateTime? FromUtc { get; set; }

        /// <summary>Gets or sets the inclusive upper bound on the event time (UTC).</summary>
        public DateTime? ToUtc { get; set; }
    }
}
