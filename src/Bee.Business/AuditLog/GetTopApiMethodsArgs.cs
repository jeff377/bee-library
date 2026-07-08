using Bee.Api.Contracts;

namespace Bee.Business.AuditLog
{
    /// <summary>
    /// Input arguments for the top-API-methods query.
    /// </summary>
    public class GetTopApiMethodsArgs : BusinessArgs, IGetTopApiMethodsRequest
    {
        /// <summary>Gets or sets the inclusive lower bound on the event time (UTC).</summary>
        public DateTime? FromUtc { get; set; }

        /// <summary>Gets or sets the inclusive upper bound on the event time (UTC).</summary>
        public DateTime? ToUtc { get; set; }

        /// <summary>Gets or sets how many top methods to return; the server clamps it to a sane range.</summary>
        public int TopN { get; set; } = 10;
    }
}
