using Bee.Api.Contracts;
using Bee.Definition.Logging;
using Bee.Definition.Paging;
using MessagePack;

namespace Bee.Api.Core.Messages.AuditLog
{
    /// <summary>
    /// API request for the DB-anomaly list operation.
    /// </summary>
    [MessagePackObject(keyAsPropertyName: true)]
    public class GetDbAnomalyLogRequest : ApiRequest, IGetDbAnomalyLogRequest
    {
        /// <summary>Gets or sets the inclusive lower bound on the event time (UTC).</summary>
        public DateTime? FromUtc { get; set; }

        /// <summary>Gets or sets the inclusive upper bound on the event time (UTC).</summary>
        public DateTime? ToUtc { get; set; }

        /// <summary>Gets or sets the database id filter (which physical database deviated).</summary>
        public string? DatabaseId { get; set; }

        /// <summary>Gets or sets the anomaly-kind filter.</summary>
        public AnomalyKind? Kind { get; set; }

        /// <summary>Gets or sets the paging request; <c>null</c> applies the server default page.</summary>
        public PagingOptions? Paging { get; set; }

        // Add new fields starting from Key(105).
    }
}
