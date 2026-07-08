using Bee.Api.Contracts;

namespace Bee.Business.AuditLog
{
    /// <summary>
    /// Output result for retrieving a record's change history.
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
        /// Gets or sets the change events for the record, newest first, each with restored before/after
        /// field values.
        /// </summary>
        public List<RecordHistoryEntry> Changes { get; set; } = [];

        /// <inheritdoc/>
        IReadOnlyList<RecordHistoryEntry> IGetRecordHistoryResponse.Changes => Changes;
    }
}
