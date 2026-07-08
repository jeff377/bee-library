namespace Bee.Definition
{
    /// <summary>
    /// Action name constants for the audit-log business object (<c>AuditLog</c> axis).
    /// All actions are read-only queries over the <c>st_log_*</c> audit tables.
    /// </summary>
    public static class LogActions
    {
        /// <summary>
        /// Gets a page of a single record's change-event headers (all <c>st_log_change</c> rows for a
        /// <c>progId</c> + <c>rowKey</c>, newest first). Field-level detail is fetched per event via
        /// <see cref="GetChangeDetail"/>.
        /// </summary>
        public const string GetRecordHistory = "GetRecordHistory";

        /// <summary>
        /// Gets a filtered, paged list of <c>st_log_change</c> event headers across records.
        /// </summary>
        public const string GetChangeLog = "GetChangeLog";

        /// <summary>
        /// Gets one change event's restored field-level before/after detail (by its <c>sys_rowid</c>).
        /// </summary>
        public const string GetChangeDetail = "GetChangeDetail";
    }
}
