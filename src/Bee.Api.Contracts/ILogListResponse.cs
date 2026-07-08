using System.Data;
using Bee.Definition.Paging;

namespace Bee.Api.Contracts
{
    /// <summary>
    /// Shared contract for a paged audit-log list response: a page of event-header rows plus paging
    /// metadata. Used by every <c>st_log_*</c> list query (login / access / anomaly); the
    /// <see cref="Table"/> carries whichever columns that axis projects.
    /// </summary>
    public interface ILogListResponse
    {
        /// <summary>Gets the event header rows for this page.</summary>
        DataTable? Table { get; }

        /// <summary>Gets the paging metadata for this page.</summary>
        PagingInfo? Paging { get; }
    }
}
