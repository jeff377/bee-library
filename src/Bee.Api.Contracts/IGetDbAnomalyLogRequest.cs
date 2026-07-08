using Bee.Definition.Logging;
using Bee.Definition.Paging;

namespace Bee.Api.Contracts
{
    /// <summary>
    /// Contract interface for the DB-anomaly list request (typed, AND-combined filter over
    /// <c>st_log_anomaly_db</c> headers). All filter fields optional.
    /// </summary>
    /// <remarks>
    /// <c>st_log_anomaly_db</c> carries no acting user / company, so this is a cross-company
    /// infrastructure view (still gated behind the <c>AuditLog</c> read permission).
    /// </remarks>
    public interface IGetDbAnomalyLogRequest
    {
        /// <summary>Gets the inclusive lower bound on the event time (UTC); <c>null</c> means no lower bound.</summary>
        DateTime? FromUtc { get; }

        /// <summary>Gets the inclusive upper bound on the event time (UTC); <c>null</c> means no upper bound.</summary>
        DateTime? ToUtc { get; }

        /// <summary>Gets the database id filter (which physical database deviated); <c>null</c> means any.</summary>
        string? DatabaseId { get; }

        /// <summary>Gets the anomaly-kind filter (Error / Timeout / Slow / large-row); <c>null</c> means any kind.</summary>
        AnomalyKind? Kind { get; }

        /// <summary>Gets the paging request; <c>null</c> applies the server default page.</summary>
        PagingOptions? Paging { get; }
    }
}
