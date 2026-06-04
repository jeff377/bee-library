using Bee.Definition.Identity;
using Bee.Repository.Abstractions.System;

namespace Bee.ObjectCaching.Services
{
    /// <summary>
    /// Role-permission snapshot service. On a cache miss, resolves the company database via
    /// <see cref="ICompanyInfoService"/>, reads the permission tables via
    /// <see cref="IRolePermissionRepository"/>, builds a <see cref="CompanyRolePermissions"/>
    /// snapshot and caches it — so subsequent permission checks run entirely from memory.
    /// </summary>
    /// <remarks>
    /// Cross-process invalidation: a writer that changes role/grant/user-role config in a company
    /// database must bump the common cache-notify row <c>"CompanyRolePermissions:{companyId}"</c>;
    /// the poller then dispatches it through <see cref="ICacheContainer.TryEvict(string)"/>.
    /// </remarks>
    public class RolePermissionService : IRolePermissionService
    {
        private readonly ICacheContainer _cache;
        private readonly ICompanyInfoService _companyInfoService;
        private readonly IRolePermissionRepository _repository;

        /// <summary>
        /// Initializes a new <see cref="RolePermissionService"/>.
        /// </summary>
        /// <param name="cache">The cache container hosting the role-permission cache.</param>
        /// <param name="companyInfoService">Resolves the company database id from the company id.</param>
        /// <param name="repository">The DB-backed permission-table reader.</param>
        public RolePermissionService(ICacheContainer cache, ICompanyInfoService companyInfoService, IRolePermissionRepository repository)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _companyInfoService = companyInfoService ?? throw new ArgumentNullException(nameof(companyInfoService));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        /// <inheritdoc/>
        public CompanyRolePermissions? Get(string companyId)
        {
            var cached = _cache.CompanyRolePermissions.Get(companyId);
            if (cached != null) { return cached; }

            var company = _companyInfoService.Get(companyId);
            if (company == null) { return null; }

            var databaseId = company.CompanyDatabaseId;
            var grants = _repository.GetRoleGrants(databaseId);
            var userRoles = _repository.GetUserRoles(databaseId);
            var snapshot = new CompanyRolePermissions(companyId, grants, userRoles);
            _cache.CompanyRolePermissions.Set(snapshot);
            return snapshot;
        }

        /// <inheritdoc/>
        public void Remove(string companyId) => _cache.CompanyRolePermissions.Remove(companyId);
    }
}
