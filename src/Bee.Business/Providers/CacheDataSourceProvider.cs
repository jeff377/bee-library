using Bee.Definition;
using Bee.Definition.Identity;
using Bee.Definition.Organization;
using Bee.Repository.Abstractions.System;

namespace Bee.Business.Providers
{
    /// <summary>
    /// Default <see cref="ICacheDataSourceProvider"/>: reads each cache's data from the system
    /// repositories and shapes it into the definition-layer type the cache stores.
    /// </summary>
    /// <remarks>
    /// The per-company snapshots resolve the company database themselves — the permission and
    /// department tables live in a company database, so the company record must be read first to
    /// obtain its <c>CompanyDatabaseId</c>.
    /// </remarks>
    public class CacheDataSourceProvider : ICacheDataSourceProvider
    {
        private readonly ISessionRepository _sessionRepository;
        private readonly ICompanyRepository _companyRepository;
        private readonly IRolePermissionRepository _rolePermissionRepository;
        private readonly IDepartmentRepository _departmentRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="CacheDataSourceProvider"/> class.
        /// </summary>
        /// <param name="sessionRepository">The session reader (<c>st_session</c>).</param>
        /// <param name="companyRepository">The company master reader (<c>st_company</c>).</param>
        /// <param name="rolePermissionRepository">The per-company permission table reader.</param>
        /// <param name="departmentRepository">The per-company department reader.</param>
        public CacheDataSourceProvider(
            ISessionRepository sessionRepository,
            ICompanyRepository companyRepository,
            IRolePermissionRepository rolePermissionRepository,
            IDepartmentRepository departmentRepository)
        {
            _sessionRepository = sessionRepository ?? throw new ArgumentNullException(nameof(sessionRepository));
            _companyRepository = companyRepository ?? throw new ArgumentNullException(nameof(companyRepository));
            _rolePermissionRepository = rolePermissionRepository ?? throw new ArgumentNullException(nameof(rolePermissionRepository));
            _departmentRepository = departmentRepository ?? throw new ArgumentNullException(nameof(departmentRepository));
        }

        /// <inheritdoc/>
        public SessionUser? GetSessionUser(Guid accessToken)
        {
            return _sessionRepository.GetSession(accessToken);
        }

        /// <inheritdoc/>
        public CompanyInfo? GetCompanyInfo(string companyId)
        {
            return _companyRepository.GetById(companyId);
        }

        /// <inheritdoc/>
        public CompanyRolePermissions? GetCompanyRolePermissions(string companyId)
        {
            var company = _companyRepository.GetById(companyId);
            if (company == null) { return null; }

            string databaseId = company.CompanyDatabaseId;
            var grants = _rolePermissionRepository.GetRoleGrants(databaseId);
            var userRoles = _rolePermissionRepository.GetUserRoles(databaseId);
            return new CompanyRolePermissions(companyId, grants, userRoles);
        }

        /// <inheritdoc/>
        public DepartmentTree? GetDepartmentTree(string companyId)
        {
            var company = _companyRepository.GetById(companyId);
            if (company == null) { return null; }

            var rows = _departmentRepository.GetDepartments(company.CompanyDatabaseId);
            return new DepartmentTree(companyId, rows);
        }
    }
}
