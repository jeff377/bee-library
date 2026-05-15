using Bee.Definition.Identity;

namespace Bee.ObjectCaching.Services
{
    /// <summary>
    /// Company information access service. Ctor-injects <see cref="ICacheContainer"/>
    /// so per-host (or per-test-fixture) DI containers own their own company cache.
    /// </summary>
    public class CompanyInfoService : ICompanyInfoService
    {
        private readonly ICacheContainer _cache;

        /// <summary>
        /// Initializes a new <see cref="CompanyInfoService"/> backed by the supplied cache container.
        /// </summary>
        /// <param name="cache">The cache container hosting the company cache.</param>
        public CompanyInfoService(ICacheContainer cache)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        /// <summary>
        /// Gets the company information from the cache, falling back to the database on a cache miss.
        /// </summary>
        public CompanyInfo? Get(string companyId)
        {
            return _cache.CompanyInfo.Get(companyId);
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
