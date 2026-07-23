using Bee.Api.Contracts;
using MessagePack;

namespace Bee.Api.Core.Messages.AuditLog
{
    /// <summary>
    /// API request for the top-API-methods operation.
    /// </summary>
    [MessagePackObject(keyAsPropertyName: true)]
    public class GetTopApiMethodsRequest : ApiRequest, IGetTopApiMethodsRequest
    {
        /// <summary>Gets or sets the inclusive lower bound on the event time (UTC).</summary>
        public DateTime? FromUtc { get; set; }

        /// <summary>Gets or sets the inclusive upper bound on the event time (UTC).</summary>
        public DateTime? ToUtc { get; set; }

        /// <summary>Gets or sets how many top methods to return; the server clamps it to a sane range.</summary>
        public int TopN { get; set; } = 10;

        // Add new fields starting from Key(103).
    }
}
