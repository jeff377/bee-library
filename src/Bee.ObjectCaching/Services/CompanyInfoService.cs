using Bee.Definition.Identity;
using Bee.Repository.Abstractions.System;

namespace Bee.ObjectCaching.Services
{
    /// <summary>
    /// Company information access service. Ctor-injects <see cref="ICacheContainer"/>
    /// so per-host (or per-test-fixture) DI containers own their own company cache,
    /// and <see cref="ICompanyRepository"/> so cache misses fall back to <c>st_company</c>.
    /// </summary>
    /// <remarks>
    /// Cross-process / multi-node invalidation: this service does not watch <c>st_company</c> for
    /// changes. A writer that modifies a company in a way that matters to the cache must, in the
    /// same transaction, bump the notification row via
    /// <c>ICacheNotifyService.Touch("CompanyInfo:{companyId}", tx)</c>. The cache-notify poller then
    /// dispatches that key through <see cref="ICacheContainer.TryEvict(string)"/> (cache group
    /// <c>CompanyInfo</c> → <see cref="ICacheContainer.CompanyInfo"/>), evicting the stale entry so
    /// the next <see cref="Get(string)"/> reloads from <c>st_company</c>. There is no in-framework
    /// <c>st_company</c> writer today; company master data is maintained externally.
    /// </remarks>
    public class CompanyInfoService : ICompanyInfoService
    {
        private readonly ICacheContainer _cache;
        private readonly ICompanyRepository _companyRepository;

        /// <summary>
        /// Initializes a new <see cref="CompanyInfoService"/>.
        /// </summary>
        /// <param name="cache">The cache container hosting the company cache.</param>
        /// <param name="companyRepository">The DB-backed fallback for cache misses.</param>
        public CompanyInfoService(ICacheContainer cache, ICompanyRepository companyRepository)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _companyRepository = companyRepository ?? throw new ArgumentNullException(nameof(companyRepository));
        }

        /// <summary>
        /// Gets the company information from the cache; on a cache miss, loads the row from
        /// <c>st_company</c> via <see cref="ICompanyRepository.GetById"/> and populates the
        /// cache before returning. Returns <c>null</c> when the company doesn't exist (or is
        /// disabled — disabled companies are filtered at the repository layer).
        /// </summary>
        public CompanyInfo? Get(string companyId)
        {
            var cached = _cache.CompanyInfo.Get(companyId);
            if (cached != null) return cached;

            var loaded = _companyRepository.GetById(companyId);
            if (loaded != null)
            {
                _cache.CompanyInfo.Set(loaded);
                return loaded;
            }
            return null;
        }

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
