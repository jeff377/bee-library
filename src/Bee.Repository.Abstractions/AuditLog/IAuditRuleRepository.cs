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
    }
}
