using Bee.Definition.Logging;
using Bee.Definition.Paging;

namespace Bee.Api.Contracts
{
    /// <summary>
    /// Contract interface for the change-log list request: a typed, AND-combined filter over the
    /// <c>st_log_change</c> event headers plus optional paging. All filter fields are optional; the
    /// query is scoped to the caller's current company server-side.
    /// </summary>
    public interface IGetChangeLogRequest
    {
        /// <summary>Gets the inclusive lower bound on the event time (UTC); <c>null</c> means no lower bound.</summary>
        DateTime? FromUtc { get; }

        /// <summary>Gets the inclusive upper bound on the event time (UTC); <c>null</c> means no upper bound.</summary>
        DateTime? ToUtc { get; }

        /// <summary>Gets the acting user's login id filter; <c>null</c> means any user.</summary>
        string? UserId { get; }

        /// <summary>Gets the business object (program) id filter; <c>null</c> means any program.</summary>
        string? ProgId { get; }

        /// <summary>Gets the master record key filter; <c>null</c> means any record.</summary>
        string? RowKey { get; }

        /// <summary>Gets the change-kind filter; <c>null</c> means any kind.</summary>
        ChangeKind? ChangeKind { get; }

        /// <summary>Gets the paging request; <c>null</c> applies the server default page.</summary>
        PagingOptions? Paging { get; }
    }
}
