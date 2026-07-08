namespace Bee.Api.Contracts
{
    /// <summary>
    /// Contract interface for the get-record-history response: the queried record identity plus its
    /// change events, newest first, each with restored before/after field values.
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
        /// Gets the change events for the record, ordered by <c>log_time</c> descending (newest first).
        /// </summary>
        IReadOnlyList<RecordHistoryEntry> Changes { get; }
    }
}
