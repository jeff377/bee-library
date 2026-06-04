namespace Bee.Definition.Identity
{
    /// <summary>
    /// Access service for per-company role-permission snapshots: a cache fronting the company
    /// database's permission tables (<c>st_role</c> / <c>st_role_grant</c> / <c>st_user_role</c>).
    /// </summary>
    public interface IRolePermissionService
    {
        /// <summary>
        /// Gets the company's role-permission snapshot from cache; on a cache miss, loads it from
        /// the company database and populates the cache before returning. Returns <c>null</c> when
        /// the company doesn't exist.
        /// </summary>
        /// <param name="companyId">The company id.</param>
        CompanyRolePermissions? Get(string companyId);

        /// <summary>
        /// Removes the company's snapshot from the cache.
        /// </summary>
        /// <param name="companyId">The company id.</param>
        void Remove(string companyId);
    }
}
