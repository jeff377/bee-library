using Bee.Api.Contracts.AuditLog;
using Bee.Definition.Logging;
using Bee.Definition.Paging;

namespace Bee.Business.AuditLog
{
    /// <summary>
    /// Input arguments for the login-log list query.
    /// </summary>
    public class GetLoginLogArgs : BusinessArgs, IGetLoginLogRequest
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
    }
}
