using Bee.Definition.Logging;

namespace Bee.Repository.Abstractions.AuditLog
{
    /// <summary>
    /// Data access for a company's per-form audit rules (<c>st_audit_rule</c>). Lives in a company
    /// database, so the method takes the company database id explicitly (resolved by the caller via
    /// the company-DB router).
    /// </summary>
    public interface IAuditRuleRepository
    {
        /// <summary>
        /// Reads every rule row from the company database's <c>st_audit_rule</c> table.
        /// </summary>
        /// <param name="databaseId">The company database id.</param>
        /// <returns>
        /// The rules, or an empty list when the table holds none. An empty list is also returned
        /// when the table does not exist at all: a deployment upgraded before the table was
        /// introduced must keep auditing on its previous deployment-wide settings rather than
        /// failing every save.
        /// </returns>
        IReadOnlyList<AuditRule> GetRules(string databaseId);

        /// <summary>
        /// Announces that the specified company's rules changed, so caches in this and other
        /// processes reload them.
        /// </summary>
        /// <param name="companyId">The company business id whose rules changed.</param>
        /// <remarks>
        /// The announcement is a version bump on the <b>common</b> database's cache-notify table,
        /// while the rules themselves live in a company database — the same split
        /// <see cref="Bee.Definition.Identity.CompanyRolePermissions"/> already uses, because the notify poller watches exactly one
        /// database.
        /// <para>
        /// WARNING: this necessarily runs in its own transaction, after the rule change has
        /// committed — a form save owns its transaction internally and does not hand one out. The
        /// ordering is the safe one (the data is already visible when the bump lands, so a reloading
        /// process cannot cache a stale value and mark it fresh); what it gives up is atomicity, so
        /// a crash between the two leaves the announcement unsent and other processes keep their
        /// cached rules until the next change. Rule edits are rare enough for that to be the right
        /// trade.
        /// </para>
        /// </remarks>
        void NotifyRulesChanged(string companyId);
    }
}
