namespace Bee.Api.Contracts
{
    /// <summary>
    /// Contract interface for the DB-anomaly summary request: counts <c>st_log_anomaly_db</c> events by
    /// anomaly kind over an optional time window. A cross-company infrastructure summary.
    /// </summary>
    public interface IGetDbAnomalySummaryRequest
    {
        /// <summary>Gets the inclusive lower bound on the event time (UTC); <c>null</c> means no lower bound.</summary>
        DateTime? FromUtc { get; }

        /// <summary>Gets the inclusive upper bound on the event time (UTC); <c>null</c> means no upper bound.</summary>
        DateTime? ToUtc { get; }
    }
}
