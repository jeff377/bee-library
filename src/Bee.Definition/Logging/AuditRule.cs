namespace Bee.Definition.Logging
{
    /// <summary>
    /// One form's audit rule as read from <c>st_audit_rule</c>: which axes are recorded for the
    /// form, and whether its entries are marked sensitive.
    /// </summary>
    /// <param name="ProgId">The form's program id (<c>st_audit_rule.sys_id</c>).</param>
    /// <param name="ChangeMode">Whether data changes to this form are recorded.</param>
    /// <param name="AccessMode">Whether record views of this form are recorded.</param>
    /// <param name="IsSensitive">
    /// Whether entries written for this form are flagged sensitive, filling in the value the
    /// change axis previously hard-coded.
    /// </param>
    public sealed record AuditRule(
        string ProgId,
        AuditRuleMode ChangeMode,
        AuditRuleMode AccessMode,
        bool IsSensitive);
}
