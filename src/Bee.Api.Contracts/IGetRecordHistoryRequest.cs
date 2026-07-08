using Bee.Definition.Paging;

namespace Bee.Api.Contracts
{
    /// <summary>
    /// Contract interface for the get-record-history request: a single record (a <c>progId</c> plus its
    /// master <c>sys_rowid</c>) whose change-event headers are requested, with optional paging.
    /// </summary>
    public interface IGetRecordHistoryRequest
    {
        /// <summary>
        /// Gets the business object (program) id whose record history is requested.
        /// </summary>
        string ProgId { get; }

        /// <summary>
        /// Gets the master record key (its <c>sys_rowid</c>).
        /// </summary>
        string RowKey { get; }

        /// <summary>
        /// Gets the paging request; <c>null</c> applies the server default page.
        /// </summary>
        PagingOptions? Paging { get; }
    }
}
