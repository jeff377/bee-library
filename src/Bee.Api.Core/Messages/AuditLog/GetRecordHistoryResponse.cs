using System.Data;
using Bee.Api.Contracts;
using Bee.Definition.Paging;
using MessagePack;

namespace Bee.Api.Core.Messages.AuditLog
{
    /// <summary>
    /// API response for the get-record-history operation: a page of change-event headers for one record.
    /// </summary>
    [MessagePackObject]
    public class GetRecordHistoryResponse : ApiResponse, IGetRecordHistoryResponse
    {
        /// <summary>
        /// Gets or sets the business object (program) id the history belongs to.
        /// </summary>
        [Key(100)]
        public string ProgId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the master record key (its <c>sys_rowid</c>) the history belongs to.
        /// </summary>
        [Key(101)]
        public string RowKey { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the change-event header rows for the record, ordered by <c>log_time</c> descending.
        /// </summary>
        [Key(102)]
        public DataTable? Table { get; set; }

        /// <summary>
        /// Gets or sets the paging metadata for this page.
        /// </summary>
        [Key(103)]
        public PagingInfo? Paging { get; set; }

        // Add new fields starting from Key(104).
    }
}
