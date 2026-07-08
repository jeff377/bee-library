namespace Bee.Definition
{
    /// <summary>
    /// Program ID constants used by the system.
    /// </summary>
    public static class SysProgIds
    {
        /// <summary>
        /// System-level business object.
        /// </summary>
        public const string System = "System";

        /// <summary>
        /// Audit-log business object (read-only queries over the <c>st_log_*</c> audit tables).
        /// Doubles as the permission model id gating audit-trail reads.
        /// </summary>
        public const string AuditLog = "AuditLog";
    }
}
