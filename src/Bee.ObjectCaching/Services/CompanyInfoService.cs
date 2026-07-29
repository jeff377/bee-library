using Bee.Definition.Identity;

namespace Bee.ObjectCaching.Services
{
    /// <summary>
    /// Company information access service. Ctor-injects <see cref="ICacheContainer"/> so per-host
    /// (or per-test-fixture) DI containers own their own company cache. Loading from
    /// <c>st_company</c> is the cache's own read-through path, not this service's.
    /// </summary>
    /// <remarks>
    /// Cross-process / multi-node invalidation: this service does not watch <c>st_company</c> for
    /// changes. A writer that modifies a company in a way that matters to the cache must, in the
    /// same transaction, bump the notification row via
    /// <c>ICacheNotifyService.Touch("CompanyInfo:{companyId}", tx)</c> — the key must match exactly,
    /// since each cached entry carries it as its <c>ChangeNotifyKey</c>. The cache-notify poller
    /// publishes the observed version, which expires the entry on its next read so the following
    /// <see cref="Get(string)"/> reloads from <c>st_company</c>. There is no in-framework
    /// <c>st_company</c> writer today; company master data is maintained externally.
    /// </remarks>
    public class CompanyInfoService : ICompanyInfoService
    {
        private readonly ICacheContainer _cache;

        /// <summary>
        /// Initializes a new <see cref="CompanyInfoService"/>.
        /// </summary>
        /// <param name="cache">The cache container hosting the company cache.</param>
        public CompanyInfoService(ICacheContainer cache)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        /// <summary>
        /// Gets the company information; the cache reads through to <c>st_company</c> on a miss.
        /// Returns <c>null</c> when the company doesn't exist (or is disabled — disabled companies
        /// are filtered at the repository layer).
        /// </summary>
        public CompanyInfo? Get(string companyId) => _cache.CompanyInfo.Get(companyId);

        /// <summary>
        /// Stores the company information in the cache, persisting it if necessary.
        /// </summary>
        public void Set(CompanyInfo companyInfo)
        {
            _cache.CompanyInfo.Set(companyInfo);
        }

        /// <summary>
        /// Removes the specified company information from the cache, invalidating any persisted state if necessary.
        /// </summary>
        public void Remove(string companyId)
        {
            _cache.CompanyInfo.Remove(companyId);
        }
    }
}
