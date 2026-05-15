using Bee.Definition.Identity;

namespace Bee.ObjectCaching.Database
{
    /// <summary>
    /// Company information cache, keyed by company id.
    /// </summary>
    /// <remarks>
    /// Inherits the default negative caching policy from <see cref="KeyObjectCache{T}"/>:
    /// a missing company id is cached as a sentinel for the negative TTL, so repeated
    /// lookups of unknown company ids do not re-invoke <see cref="CreateInstance"/>.
    /// <see cref="CreateInstance"/> currently returns <c>null</c> — until the company
    /// data source is wired (separate plan), entries are populated exclusively by
    /// <see cref="KeyObjectCache{T}.Set(T)"/> from <c>EnterCompany</c>.
    /// </remarks>
    public class CompanyInfoCache : KeyObjectCache<CompanyInfo>
    {
        /// <summary>
        /// Initializes a new <see cref="CompanyInfoCache"/>.
        /// </summary>
        /// <param name="cachePrefix">Per-owner cache namespace (see <see cref="KeyObjectCache{T}"/>).</param>
        public CompanyInfoCache(string cachePrefix = "") : base(cachePrefix) { }

        /// <summary>
        /// Creates an instance of the company information.
        /// </summary>
        /// <param name="key">The company id.</param>
        protected override CompanyInfo? CreateInstance(string key)
        {
            return null; // Loading CompanyInfo from the database is not yet implemented
        }
    }
}
