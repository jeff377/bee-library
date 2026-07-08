using Bee.Api.Contracts;
using MessagePack;

namespace Bee.Api.Core.Messages.AuditLog
{
    /// <summary>
    /// API response for the get-record-history operation.
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
        /// Gets or sets the change events for the record, ordered by <c>log_time</c> descending.
        /// </summary>
        [Key(102)]
        public List<RecordHistoryEntry> Changes { get; set; } = [];

        /// <inheritdoc/>
        IReadOnlyList<RecordHistoryEntry> IGetRecordHistoryResponse.Changes => Changes;

        // Add new fields starting from Key(103).
    }
}
