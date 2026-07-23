using Bee.Api.Contracts;
using Bee.Definition.Logging;
using Bee.Definition.Paging;
using MessagePack;

namespace Bee.Api.Core.Messages.AuditLog
{
    /// <summary>
    /// API request for the login-log list operation.
    /// </summary>
    [MessagePackObject(keyAsPropertyName: true)]
    public class GetLoginLogRequest : ApiRequest, IGetLoginLogRequest
    {
        /// <summary>Gets or sets the inclusive lower bound on the event time (UTC).</summary>
        public DateTime? FromUtc { get; set; }

        /// <summary>Gets or sets the inclusive upper bound on the event time (UTC).</summary>
        public DateTime? ToUtc { get; set; }

        /// <summary>Gets or sets the acting user's login id filter.</summary>
        public string? UserId { get; set; }

        /// <summary>Gets or sets the login-event filter.</summary>
        public LoginEvent? Event { get; set; }

        /// <summary>Gets or sets the paging request; <c>null</c> applies the server default page.</summary>
        public PagingOptions? Paging { get; set; }

        // Add new fields starting from Key(105).
    }
}
