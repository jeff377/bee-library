using Bee.Api.Contracts;
using Bee.Definition.Paging;

namespace Bee.Business.AuditLog
{
    /// <summary>
    /// Input arguments for retrieving a page of a record's change-event headers.
    /// </summary>
    public class GetRecordHistoryArgs : BusinessArgs, IGetRecordHistoryRequest
    {
        /// <summary>
        /// Gets or sets the business object (program) id whose record history is requested.
        /// </summary>
        public string ProgId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the master record key (its <c>sys_rowid</c>).
        /// </summary>
        public string RowKey { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the paging request; <c>null</c> applies the server default page.
        /// </summary>
        public PagingOptions? Paging { get; set; }
    }
}
