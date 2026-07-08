using System.Data;
using Bee.Definition.Paging;

namespace Bee.Api.Contracts
{
    /// <summary>
    /// Contract interface for the get-record-history response: the queried record identity plus a page
    /// of its change-event headers (newest first). Field-level before/after detail is fetched per event
    /// via get-change-detail — the list itself does not restore the DiffGram.
    /// </summary>
    public interface IGetRecordHistoryResponse
    {
        /// <summary>
        /// Gets the business object (program) id the history belongs to.
        /// </summary>
        string ProgId { get; }

        /// <summary>
        /// Gets the master record key (its <c>sys_rowid</c>) the history belongs to.
        /// </summary>
        string RowKey { get; }

        /// <summary>
        /// Gets the change-event header rows for the record, ordered by <c>log_time</c> descending.
        /// </summary>
        DataTable? Table { get; }

        /// <summary>
        /// Gets the paging metadata for this page.
        /// </summary>
        PagingInfo? Paging { get; }
    }
}
