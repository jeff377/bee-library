using Bee.Definition.Logging;
using Bee.Definition.Paging;

namespace Bee.Api.Contracts
{
    /// <summary>
    /// Contract interface for the login-log list request (typed, AND-combined filter over
    /// <c>st_log_login</c> headers). All filter fields optional; scoped to the caller's company.
    /// </summary>
    public interface IGetLoginLogRequest
    {
        /// <summary>Gets the inclusive lower bound on the event time (UTC); <c>null</c> means no lower bound.</summary>
        DateTime? FromUtc { get; }

        /// <summary>Gets the inclusive upper bound on the event time (UTC); <c>null</c> means no upper bound.</summary>
        DateTime? ToUtc { get; }

        /// <summary>Gets the acting user's login id filter; <c>null</c> means any user.</summary>
        string? UserId { get; }

        /// <summary>Gets the login-event filter (success / failure / lockout / logout); <c>null</c> means any.</summary>
        LoginEvent? Event { get; }

        /// <summary>Gets the paging request; <c>null</c> applies the server default page.</summary>
        PagingOptions? Paging { get; }
    }
}
