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
        /// Gets a filtered, paged list of <c>st_log_change</c> event headers across records (e.g. a form's
        /// changes over a period, a user's changes over a period, or one record's history via
        /// <c>ProgId</c> + <c>RowKey</c>).
        /// </summary>
        /// <param name="args">The input arguments carrying the typed filter and optional paging.</param>
        GetChangeLogResult GetChangeLog(GetChangeLogArgs args);

        /// <summary>
        /// Gets one change event's restored field-level before/after detail, by its log row id.
        /// </summary>
        /// <param name="args">The input arguments carrying the event's <c>SysRowId</c>.</param>
        GetChangeDetailResult GetChangeDetail(GetChangeDetailArgs args);

        /// <summary>Gets a filtered, paged list of <c>st_log_login</c> event headers.</summary>
        /// <param name="args">The input arguments carrying the typed filter and optional paging.</param>
        LogListResult GetLoginLog(GetLoginLogArgs args);

        /// <summary>Gets a filtered, paged list of <c>st_log_access</c> record-view headers.</summary>
        /// <param name="args">The input arguments carrying the typed filter and optional paging.</param>
        LogListResult GetAccessLog(GetAccessLogArgs args);

        /// <summary>Gets a filtered, paged list of <c>st_log_anomaly_api</c> API-anomaly headers.</summary>
        /// <param name="args">The input arguments carrying the typed filter and optional paging.</param>
        LogListResult GetApiAnomalyLog(GetApiAnomalyLogArgs args);

        /// <summary>Gets a filtered, paged list of <c>st_log_anomaly_db</c> DB-anomaly headers.</summary>
        /// <param name="args">The input arguments carrying the typed filter and optional paging.</param>
        LogListResult GetDbAnomalyLog(GetDbAnomalyLogArgs args);

        /// <summary>Gets API-anomaly counts grouped by anomaly kind (monitoring summary).</summary>
        /// <param name="args">The input arguments carrying the optional time window.</param>
        LogAggregateResult GetApiAnomalySummary(GetApiAnomalySummaryArgs args);

        /// <summary>Gets DB-anomaly counts grouped by anomaly kind (monitoring summary).</summary>
        /// <param name="args">The input arguments carrying the optional time window.</param>
        LogAggregateResult GetDbAnomalySummary(GetDbAnomalySummaryArgs args);

        /// <summary>Gets the top API methods by anomaly count (monitoring hot-spots).</summary>
        /// <param name="args">The input arguments carrying the optional time window and top-N.</param>
        LogAggregateResult GetTopApiMethods(GetTopApiMethodsArgs args);
    }
}
