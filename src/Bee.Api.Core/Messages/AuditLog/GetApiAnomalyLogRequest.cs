using Bee.Api.Contracts;
using Bee.Definition.Logging;
using Bee.Definition.Paging;
using MessagePack;

namespace Bee.Api.Core.Messages.AuditLog
{
    /// <summary>
    /// API request for the API-anomaly list operation.
    /// </summary>
    [MessagePackObject]
    public class GetApiAnomalyLogRequest : ApiRequest, IGetApiAnomalyLogRequest
    {
        /// <summary>Gets or sets the inclusive lower bound on the event time (UTC).</summary>
        [Key(100)]
        public DateTime? FromUtc { get; set; }

        /// <summary>Gets or sets the inclusive upper bound on the event time (UTC).</summary>
        [Key(101)]
        public DateTime? ToUtc { get; set; }

        /// <summary>Gets or sets the acting user's login id filter.</summary>
        [Key(102)]
        public string? UserId { get; set; }

        /// <summary>Gets or sets the API method filter (e.g. <c>"Order.Save"</c>).</summary>
        [Key(103)]
        public string? Method { get; set; }

        /// <summary>Gets or sets the anomaly-kind filter.</summary>
        [Key(104)]
        public AnomalyKind? Kind { get; set; }

        /// <summary>Gets or sets the paging request; <c>null</c> applies the server default page.</summary>
        [Key(105)]
        public PagingOptions? Paging { get; set; }

        // Add new fields starting from Key(106).
    }
}
