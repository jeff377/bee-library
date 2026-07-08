using Bee.Api.Contracts;
using MessagePack;

namespace Bee.Api.Core.Messages.AuditLog
{
    /// <summary>
    /// API request for the API-anomaly summary operation.
    /// </summary>
    [MessagePackObject]
    public class GetApiAnomalySummaryRequest : ApiRequest, IGetApiAnomalySummaryRequest
    {
        /// <summary>Gets or sets the inclusive lower bound on the event time (UTC).</summary>
        [Key(100)]
        public DateTime? FromUtc { get; set; }

        /// <summary>Gets or sets the inclusive upper bound on the event time (UTC).</summary>
        [Key(101)]
        public DateTime? ToUtc { get; set; }

        // Add new fields starting from Key(102).
    }
}
