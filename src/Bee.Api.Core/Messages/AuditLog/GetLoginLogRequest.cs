using Bee.Api.Contracts;
using Bee.Definition.Logging;
using Bee.Definition.Paging;
using MessagePack;

namespace Bee.Api.Core.Messages.AuditLog
{
    /// <summary>
    /// API request for the login-log list operation.
    /// </summary>
    [MessagePackObject]
    public class GetLoginLogRequest : ApiRequest, IGetLoginLogRequest
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

        /// <summary>Gets or sets the login-event filter.</summary>
        [Key(103)]
        public LoginEvent? Event { get; set; }

        /// <summary>Gets or sets the paging request; <c>null</c> applies the server default page.</summary>
        [Key(104)]
        public PagingOptions? Paging { get; set; }

        // Add new fields starting from Key(105).
    }
}
