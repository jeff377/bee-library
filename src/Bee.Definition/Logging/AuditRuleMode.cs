namespace Bee.Definition.Logging
{
    /// <summary>
    /// Per-form switch for one audit axis (data change or record view), stored as an integer in
    /// <c>st_audit_rule</c> and resolved against the deployment-wide default for that axis.
    /// </summary>
    /// <remarks>
    /// <see cref="Inherit"/> is the persisted default (<c>0</c>), so a deployment whose rule table
    /// is empty behaves exactly as it did before per-form rules existed. Only
    /// <c>AuditLogOptions.Enabled</c> gates this resolution; the per-axis switches supply the value
    /// <see cref="Inherit"/> defers to rather than acting as a second gate.
    /// </remarks>
    public enum AuditRuleMode
    {
        /// <summary>Defer to the deployment-wide default for this axis.</summary>
        Inherit = 0,

        /// <summary>Record this form's activity on this axis, whatever the deployment default is.</summary>
        On = 1,

        /// <summary>Do not record this form's activity on this axis, whatever the deployment default is.</summary>
        Off = 2,
    }
}
