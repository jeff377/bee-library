namespace Bee.Api.Contracts
{
    /// <summary>
    /// Contract interface for the top-API-methods request: the busiest API methods by
    /// <c>st_log_anomaly_api</c> count over an optional time window (scoped to the caller's company).
    /// </summary>
    public interface IGetTopApiMethodsRequest
    {
        /// <summary>Gets the inclusive lower bound on the event time (UTC); <c>null</c> means no lower bound.</summary>
        DateTime? FromUtc { get; }

        /// <summary>Gets the inclusive upper bound on the event time (UTC); <c>null</c> means no upper bound.</summary>
        DateTime? ToUtc { get; }

        /// <summary>Gets how many top methods to return; the server clamps it to a sane range.</summary>
        int TopN { get; }
    }
}
