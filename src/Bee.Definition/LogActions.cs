namespace Bee.Definition
{
    /// <summary>
    /// Action name constants for the audit-log business object (<c>AuditLog</c> axis).
    /// All actions are read-only queries over the <c>st_log_*</c> audit tables.
    /// </summary>
    public static class LogActions
    {
        /// <summary>
        /// Gets a filtered, paged list of <c>st_log_change</c> event headers across records (form + period,
        /// user + period, or one record via progId + rowKey). Field-level detail per event is fetched via
        /// <see cref="GetChangeDetail"/>.
        /// </summary>
        public const string GetChangeLog = "GetChangeLog";

        /// <summary>
        /// Gets one change event's restored field-level before/after detail (by its <c>sys_rowid</c>).
        /// </summary>
        public const string GetChangeDetail = "GetChangeDetail";

        /// <summary>
        /// Gets a filtered, paged list of <c>st_log_login</c> event headers.
        /// </summary>
        public const string GetLoginLog = "GetLoginLog";

        /// <summary>
        /// Gets a filtered, paged list of <c>st_log_access</c> record-view headers.
        /// </summary>
        public const string GetAccessLog = "GetAccessLog";

        /// <summary>
        /// Gets a filtered, paged list of <c>st_log_anomaly_api</c> API-anomaly headers.
        /// </summary>
        public const string GetApiAnomalyLog = "GetApiAnomalyLog";

        /// <summary>
        /// Gets a filtered, paged list of <c>st_log_anomaly_db</c> DB-anomaly headers.
        /// </summary>
        public const string GetDbAnomalyLog = "GetDbAnomalyLog";

        /// <summary>
        /// Gets API-anomaly counts grouped by anomaly kind (monitoring summary).
        /// </summary>
        public const string GetApiAnomalySummary = "GetApiAnomalySummary";

        /// <summary>
        /// Gets DB-anomaly counts grouped by anomaly kind (monitoring summary).
        /// </summary>
        public const string GetDbAnomalySummary = "GetDbAnomalySummary";

        /// <summary>
        /// Gets the top API methods by anomaly count (monitoring hot-spots).
        /// </summary>
        public const string GetTopApiMethods = "GetTopApiMethods";
    }
}
