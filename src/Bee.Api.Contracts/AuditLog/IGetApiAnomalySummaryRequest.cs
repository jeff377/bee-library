namespace Bee.Api.Contracts.AuditLog
{
    /// <summary>
    /// Contract interface for the API-anomaly summary request: counts <c>st_log_anomaly_api</c> events
    /// by anomaly kind over an optional time window (scoped to the caller's company).
    /// </summary>
    public interface IGetApiAnomalySummaryRequest
    {
        /// <summary>Gets the inclusive lower bound on the event time (UTC); <c>null</c> means no lower bound.</summary>
        DateTime? FromUtc { get; }

        /// <summary>Gets the inclusive upper bound on the event time (UTC); <c>null</c> means no upper bound.</summary>
        DateTime? ToUtc { get; }
    }
}
