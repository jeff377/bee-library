using Bee.Definition;
using Bee.Definition.Logging;

namespace Bee.ObjectCaching.Database
{
    /// <summary>
    /// Per-company audit-rule snapshot cache, keyed by company id. Reads through to
    /// <see cref="ICacheDataSourceProvider.GetCompanyAuditRules"/> on a miss, which resolves the
    /// company database and reads <c>st_audit_rule</c>; invalidation goes through the common
    /// cache-notify table (cache group <c>CompanyAuditRules</c>).
    /// </summary>
    /// <remarks>
    /// Keyed by company rather than by program id on purpose: most forms carry no rule at all, so
    /// per-form keying would turn each of them into a miss, a query and a negative entry. See
    /// <see cref="CompanyAuditRules"/>.
    /// </remarks>
    public class CompanyAuditRulesCache : KeyObjectCache<CompanyAuditRules>
    {
        private readonly Func<ICacheDataSourceProvider>? _dataSource;

        /// <summary>
        /// Initializes a new <see cref="CompanyAuditRulesCache"/> without a data source, leaving
        /// <see cref="KeyObjectCache{T}.Set(T)"/> as the only way in.
        /// </summary>
        /// <param name="cachePrefix">Per-owner cache namespace (see <see cref="KeyObjectCache{T}"/>).</param>
        public CompanyAuditRulesCache(string cachePrefix = "") : this(null, cachePrefix) { }

        /// <summary>
        /// Initializes a new <see cref="CompanyAuditRulesCache"/> bound to a data source.
        /// </summary>
        /// <param name="dataSource">
        /// Lazy accessor for the cache data source; <c>null</c> disables read-through.
        /// </param>
        /// <param name="cachePrefix">Per-owner cache namespace (see <see cref="KeyObjectCache{T}"/>).</param>
        /// <remarks>
        /// WARNING: <paramref name="dataSource"/> must stay a factory — see
        /// <see cref="CompanyInfoCache(Func{ICacheDataSourceProvider}, string)"/> for the dependency
        /// cycle it avoids.
        /// </remarks>
        internal CompanyAuditRulesCache(Func<ICacheDataSourceProvider>? dataSource, string cachePrefix)
            : base(cachePrefix)
        {
            _dataSource = dataSource;
        }

        /// <summary>
        /// Creates the audit-rule snapshot for the specified company id by reading it from the data
        /// source.
        /// </summary>
        /// <param name="key">The company id.</param>
        protected override CompanyAuditRules? CreateInstance(string key)
        {
            return _dataSource?.Invoke().GetCompanyAuditRules(key);
        }
    }
}
