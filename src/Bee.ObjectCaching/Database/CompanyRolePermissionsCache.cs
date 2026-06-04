using Bee.Definition.Identity;

namespace Bee.ObjectCaching.Database
{
    /// <summary>
    /// Per-company role-permission snapshot cache, keyed by company id. DB-sourced — the snapshot
    /// is loaded by <c>RolePermissionService</c> from the company database and stored via
    /// <see cref="KeyObjectCache{T}.Set(T)"/>; invalidation goes through the common cache-notify
    /// table (cache group <c>CompanyRolePermissions</c>).
    /// </summary>
    /// <remarks>
    /// <see cref="CreateInstance"/> returns <c>null</c> (no self-loading): entries are populated
    /// exclusively by the service after it resolves the company database and reads the permission
    /// tables, matching the <c>CompanyInfoCache</c> pattern.
    /// </remarks>
    public class CompanyRolePermissionsCache : KeyObjectCache<CompanyRolePermissions>
    {
        /// <summary>
        /// Initializes a new <see cref="CompanyRolePermissionsCache"/>.
        /// </summary>
        /// <param name="cachePrefix">Per-owner cache namespace (see <see cref="KeyObjectCache{T}"/>).</param>
        public CompanyRolePermissionsCache(string cachePrefix = "") : base(cachePrefix) { }

        /// <summary>
        /// Creates an instance for the specified company id. Always returns <c>null</c> — the
        /// snapshot is populated by the service layer, not self-loaded here.
        /// </summary>
        /// <param name="key">The company id.</param>
        protected override CompanyRolePermissions? CreateInstance(string key) => null;
    }
}
