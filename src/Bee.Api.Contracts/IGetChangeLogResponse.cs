using System.Data;
using Bee.Definition.Paging;

namespace Bee.Api.Contracts
{
    /// <summary>
    /// Contract interface for the change-log list response: a page of <c>st_log_change</c> event
    /// headers (newest first) plus paging metadata. The <c>changes_xml</c> DiffGram is not included;
    /// fetch a single event's before/after detail via get-change-detail.
    /// </summary>
    public interface IGetChangeLogResponse
    {
        /// <summary>Gets the change-event header rows.</summary>
        DataTable? Table { get; }

        /// <summary>Gets the paging metadata for this page.</summary>
        PagingInfo? Paging { get; }
    }
}
