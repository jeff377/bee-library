namespace Bee.Definition.Logging
{
    /// <summary>
    /// Access service for per-company audit-rule snapshots: a cache fronting the company database's
    /// <c>st_audit_rule</c> table.
    /// </summary>
    public interface IAuditRuleService
    {
        /// <summary>
        /// Gets the company's audit-rule snapshot from cache; on a cache miss, loads it from the
        /// company database and populates the cache before returning. Returns <c>null</c> when the
        /// company does not exist, which callers treat the same as an empty snapshot.
        /// </summary>
        /// <param name="companyId">The company business id.</param>
        CompanyAuditRules? Get(string companyId);

        /// <summary>
        /// Removes the company's snapshot from the cache.
        /// </summary>
        /// <param name="companyId">The company business id.</param>
        void Remove(string companyId);
    }
}
