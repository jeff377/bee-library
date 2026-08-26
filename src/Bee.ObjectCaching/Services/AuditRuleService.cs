using Bee.Definition.Logging;

namespace Bee.ObjectCaching.Services
{
    /// <summary>
    /// Audit-rule snapshot service. The snapshot is built by the cache's read-through path, which
    /// resolves the company database and reads <c>st_audit_rule</c> — so subsequent rule lookups
    /// run entirely from memory.
    /// </summary>
    /// <remarks>
    /// Cross-process invalidation: a writer that changes rules in a company database must bump the
    /// common cache-notify row <c>"CompanyAuditRules:{companyId}"</c> — the key must match exactly,
    /// since the cached entry carries it as its <c>ChangeNotifyKey</c>. The poller publishes the
    /// observed version, expiring the entry on its next read.
    /// </remarks>
    public class AuditRuleService : IAuditRuleService
    {
        private readonly ICacheContainer _cache;

        /// <summary>
        /// Initializes a new <see cref="AuditRuleService"/>.
        /// </summary>
        /// <param name="cache">The cache container hosting the audit-rule cache.</param>
        public AuditRuleService(ICacheContainer cache)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        /// <inheritdoc/>
        public CompanyAuditRules? Get(string companyId) => _cache.CompanyAuditRules.Get(companyId);

        /// <inheritdoc/>
        public void Remove(string companyId) => _cache.CompanyAuditRules.Remove(companyId);
    }
}
