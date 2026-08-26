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

        /// <summary>
        /// Per-form audit rule maintenance (the <c>st_audit_rule</c> form). Doubles as the
        /// permission model id gating who may change the audit policy.
        /// </summary>
        /// <remarks>
        /// Distinct from <see cref="AuditLog"/>, and the pair is easy to mix up: that one reads
        /// what was recorded, this one decides what gets recorded.
        /// </remarks>
        public const string AuditRule = "AuditRule";
    }
}
