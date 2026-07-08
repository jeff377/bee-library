namespace Bee.Definition
{
    /// <summary>
    /// Action name constants for the audit-log business object (<c>AuditLog</c> axis).
    /// All actions are read-only queries over the <c>st_log_*</c> audit tables.
    /// </summary>
    public static class LogActions
    {
        /// <summary>
        /// Gets the change history of a single record (all <c>st_log_change</c> rows for a
        /// <c>progId</c> + <c>rowKey</c>), with each row's <c>changes_xml</c> DiffGram restored
        /// into structured before/after field values.
        /// </summary>
        public const string GetRecordHistory = "GetRecordHistory";
    }
}
