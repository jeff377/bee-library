using System.Data;
using Bee.Api.Contracts;
using Bee.Definition.Paging;

namespace Bee.Business.AuditLog
{
    /// <summary>
    /// Output result for retrieving a page of a record's change-event headers.
    /// </summary>
    public class GetRecordHistoryResult : BusinessResult, IGetRecordHistoryResponse
    {
        /// <summary>
        /// Gets or sets the business object (program) id the history belongs to.
        /// </summary>
        public string ProgId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the master record key (its <c>sys_rowid</c>) the history belongs to.
        /// </summary>
        public string RowKey { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the change-event header rows for the record, ordered by <c>log_time</c> descending.
        /// </summary>
        public DataTable? Table { get; set; }

        /// <summary>
        /// Gets or sets the paging metadata for this page.
        /// </summary>
        public PagingInfo? Paging { get; set; }
    }
}
