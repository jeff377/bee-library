using Bee.Api.Contracts;

namespace Bee.Business.AuditLog
{
    /// <summary>
    /// Input arguments for the DB-anomaly summary query.
    /// </summary>
    public class GetDbAnomalySummaryArgs : BusinessArgs, IGetDbAnomalySummaryRequest
    {
        /// <summary>Gets or sets the inclusive lower bound on the event time (UTC).</summary>
        public DateTime? FromUtc { get; set; }

        /// <summary>Gets or sets the inclusive upper bound on the event time (UTC).</summary>
        public DateTime? ToUtc { get; set; }
    }
}
