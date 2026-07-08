using Bee.Api.Contracts;
using MessagePack;

namespace Bee.Api.Core.Messages.AuditLog
{
    /// <summary>
    /// API request for the top-API-methods operation.
    /// </summary>
    [MessagePackObject]
    public class GetTopApiMethodsRequest : ApiRequest, IGetTopApiMethodsRequest
    {
        /// <summary>Gets or sets the inclusive lower bound on the event time (UTC).</summary>
        [Key(100)]
        public DateTime? FromUtc { get; set; }

        /// <summary>Gets or sets the inclusive upper bound on the event time (UTC).</summary>
        [Key(101)]
        public DateTime? ToUtc { get; set; }

        /// <summary>Gets or sets how many top methods to return; the server clamps it to a sane range.</summary>
        [Key(102)]
        public int TopN { get; set; } = 10;

        // Add new fields starting from Key(103).
    }
}
