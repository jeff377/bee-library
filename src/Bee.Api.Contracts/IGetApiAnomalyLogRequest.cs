using Bee.Definition.Logging;
using Bee.Definition.Paging;

namespace Bee.Api.Contracts
{
    /// <summary>
    /// Contract interface for the API-anomaly list request (typed, AND-combined filter over
    /// <c>st_log_anomaly_api</c> headers). All filter fields optional; scoped to the caller's company.
    /// </summary>
    public interface IGetApiAnomalyLogRequest
    {
        /// <summary>Gets the inclusive lower bound on the event time (UTC); <c>null</c> means no lower bound.</summary>
        DateTime? FromUtc { get; }

        /// <summary>Gets the inclusive upper bound on the event time (UTC); <c>null</c> means no upper bound.</summary>
        DateTime? ToUtc { get; }

        /// <summary>Gets the acting user's login id filter; <c>null</c> means any user.</summary>
        string? UserId { get; }

        /// <summary>Gets the API method filter (e.g. <c>"Order.Save"</c>); <c>null</c> means any method.</summary>
        string? Method { get; }

        /// <summary>Gets the anomaly-kind filter (Error / Timeout / Slow); <c>null</c> means any kind.</summary>
        AnomalyKind? Kind { get; }

        /// <summary>Gets the paging request; <c>null</c> applies the server default page.</summary>
        PagingOptions? Paging { get; }
    }
}
