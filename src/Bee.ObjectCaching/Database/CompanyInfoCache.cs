using Bee.Definition;
using Bee.Definition.Identity;

namespace Bee.ObjectCaching.Database
{
    /// <summary>
    /// Company information cache, keyed by company id. Reads through to
    /// <see cref="ICacheDataSourceProvider.GetCompanyInfo"/> on a miss.
    /// </summary>
    /// <remarks>
    /// Inherits the default negative caching policy from <see cref="KeyObjectCache{T}"/>:
    /// a missing company id is cached as a sentinel for the negative TTL, so repeated
    /// lookups of unknown company ids do not re-invoke <see cref="CreateInstance"/>.
    /// </remarks>
    public class CompanyInfoCache : KeyObjectCache<CompanyInfo>
    {
        private readonly Func<ICacheDataSourceProvider>? _dataSource;

        /// <summary>
        /// Initializes a new <see cref="CompanyInfoCache"/> without a data source, leaving
        /// <see cref="KeyObjectCache{T}.Set(T)"/> as the only way in.
        /// </summary>
        /// <param name="cachePrefix">Per-owner cache namespace (see <see cref="KeyObjectCache{T}"/>).</param>
        public CompanyInfoCache(string cachePrefix = "") : this(null, cachePrefix) { }

        /// <summary>
        /// Initializes a new <see cref="CompanyInfoCache"/> bound to a data source.
        /// </summary>
        /// <param name="dataSource">
        /// Lazy accessor for the cache data source; <c>null</c> disables read-through, leaving
        /// <see cref="KeyObjectCache{T}.Set(T)"/> as the only way in.
        /// </param>
        /// <param name="cachePrefix">Per-owner cache namespace (see <see cref="KeyObjectCache{T}"/>).</param>
        /// <remarks>
        /// WARNING: <paramref name="dataSource"/> must stay a factory. Resolving the provider while
        /// <see cref="CacheContainerService"/> is under construction closes the dependency cycle
        /// <see cref="ICacheContainer"/> to <see cref="ICacheDataSourceProvider"/> to the repository factory to
        /// <see cref="Bee.Definition.Storage.IDefineAccess"/> and back to <see cref="Bee.ObjectCaching.ICacheContainer"/>. Deferring the call to the first
        /// cache miss breaks that cycle, because the container singleton is fully constructed by then.
        /// </remarks>
        internal CompanyInfoCache(Func<ICacheDataSourceProvider>? dataSource, string cachePrefix)
            : base(cachePrefix)
        {
            _dataSource = dataSource;
        }

        /// <summary>
        /// Creates an instance of the company information by reading it from the data source.
        /// </summary>
        /// <param name="key">The company id.</param>
        protected override CompanyInfo? CreateInstance(string key)
        {
            return _dataSource?.Invoke().GetCompanyInfo(key);
        }
    }
}
