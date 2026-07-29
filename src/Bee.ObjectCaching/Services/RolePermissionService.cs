using Bee.Definition.Identity;

namespace Bee.ObjectCaching.Services
{
    /// <summary>
    /// Role-permission snapshot service. The snapshot is built by the cache's read-through path,
    /// which resolves the company database and reads the permission tables — so subsequent
    /// permission checks run entirely from memory.
    /// </summary>
    /// <remarks>
    /// Cross-process invalidation: a writer that changes role/grant/user-role config in a company
    /// database must bump the common cache-notify row <c>"CompanyRolePermissions:{companyId}"</c> —
    /// the key must match exactly, since the cached entry carries it as its <c>ChangeNotifyKey</c>.
    /// The poller publishes the observed version, expiring the entry on its next read.
    /// </remarks>
    public class RolePermissionService : IRolePermissionService
    {
        private readonly ICacheContainer _cache;

        /// <summary>
        /// Initializes a new <see cref="RolePermissionService"/>.
        /// </summary>
        /// <param name="cache">The cache container hosting the role-permission cache.</param>
        public RolePermissionService(ICacheContainer cache)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        /// <inheritdoc/>
        public CompanyRolePermissions? Get(string companyId) => _cache.CompanyRolePermissions.Get(companyId);

        /// <inheritdoc/>
        public void Remove(string companyId) => _cache.CompanyRolePermissions.Remove(companyId);
    }
}
