namespace Bee.Business.AuditLog
{
    /// <summary>
    /// Cross-BO interface for the audit-log business object (read-only queries over the
    /// <c>st_log_*</c> tables). Exists so other BOs can resolve it through
    /// <c>IBusinessObjectFactory</c> and query the audit trail without binding to the concrete class.
    /// </summary>
    public interface ILogBusinessObject : IBusinessObject
    {
        /// <summary>
        /// Gets a page of one record's change-event headers (all <c>st_log_change</c> events for a
        /// <c>progId</c> + <c>rowKey</c>, newest first).
        /// </summary>
        /// <param name="args">The input arguments carrying <c>ProgId</c>, <c>RowKey</c> and optional paging.</param>
        GetRecordHistoryResult GetRecordHistory(GetRecordHistoryArgs args);

        /// <summary>
        /// Gets a filtered, paged list of <c>st_log_change</c> event headers across records.
        /// </summary>
        /// <param name="args">The input arguments carrying the typed filter and optional paging.</param>
        GetChangeLogResult GetChangeLog(GetChangeLogArgs args);

        /// <summary>
        /// Gets one change event's restored field-level before/after detail, by its log row id.
        /// </summary>
        /// <param name="args">The input arguments carrying the event's <c>SysRowId</c>.</param>
        GetChangeDetailResult GetChangeDetail(GetChangeDetailArgs args);
    }
}
