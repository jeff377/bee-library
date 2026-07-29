using Bee.Definition;
using Bee.Definition.Identity;

namespace Bee.ObjectCaching.Database
{
    /// <summary>
    /// Per-company role-permission snapshot cache, keyed by company id. Reads through to
    /// <see cref="ICacheDataSourceProvider.GetCompanyRolePermissions"/> on a miss, which resolves
    /// the company database and reads the permission tables; invalidation goes through the common
    /// cache-notify table (cache group <c>CompanyRolePermissions</c>).
    /// </summary>
    public class CompanyRolePermissionsCache : KeyObjectCache<CompanyRolePermissions>
    {
        private readonly Func<ICacheDataSourceProvider>? _dataSource;

        /// <summary>
        /// Initializes a new <see cref="CompanyRolePermissionsCache"/> without a data source,
        /// leaving <see cref="KeyObjectCache{T}.Set(T)"/> as the only way in.
        /// </summary>
        /// <param name="cachePrefix">Per-owner cache namespace (see <see cref="KeyObjectCache{T}"/>).</param>
        public CompanyRolePermissionsCache(string cachePrefix = "") : this(null, cachePrefix) { }

        /// <summary>
        /// Initializes a new <see cref="CompanyRolePermissionsCache"/> bound to a data source.
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
        internal CompanyRolePermissionsCache(Func<ICacheDataSourceProvider>? dataSource, string cachePrefix)
            : base(cachePrefix)
        {
            _dataSource = dataSource;
        }

        /// <summary>
        /// Creates the role-permission snapshot for the specified company id by reading it from the
        /// data source.
        /// </summary>
        /// <param name="key">The company id.</param>
        protected override CompanyRolePermissions? CreateInstance(string key)
        {
            return _dataSource?.Invoke().GetCompanyRolePermissions(key);
        }
    }
}
