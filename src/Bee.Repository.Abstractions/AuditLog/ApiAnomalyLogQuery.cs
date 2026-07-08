using Bee.Definition.Logging;

namespace Bee.Repository.Abstractions.AuditLog
{
    /// <summary>
    /// Typed filter for an API-anomaly (<c>st_log_anomaly_api</c>) list query. All fields optional,
    /// AND-combined, each mapping to an indexed column.
    /// </summary>
    public sealed class ApiAnomalyLogQuery
    {
        /// <summary>Gets the inclusive lower bound on <c>log_time</c> (UTC); null means no lower bound.</summary>
        public DateTime? FromUtc { get; init; }

        /// <summary>Gets the inclusive upper bound on <c>log_time</c> (UTC); null means no upper bound.</summary>
        public DateTime? ToUtc { get; init; }

        /// <summary>Gets the acting user's login id filter; null means any user.</summary>
        public string? UserId { get; init; }

        /// <summary>Gets the API method filter (e.g. <c>"Order.Save"</c>); null means any method.</summary>
        public string? Method { get; init; }

        /// <summary>Gets the anomaly-kind filter (Error / Timeout / Slow); null means any kind.</summary>
        public AnomalyKind? Kind { get; init; }

        /// <summary>Gets the company scope; when non-empty, restricts to that company's rows.</summary>
        public string? CompanyId { get; init; }
    }
}
