using Bee.Definition;
using Bee.Definition.Identity;
using Bee.Definition.Organization;
using Bee.Repository.Abstractions.Factories;

namespace Bee.Business.Providers
{
    /// <summary>
    /// Default <see cref="ICacheDataSourceProvider"/>: reads each cache's data from the system
    /// repositories and shapes it into the definition-layer type the cache stores.
    /// </summary>
    /// <remarks>
    /// Repositories are obtained from <see cref="ISystemRepositoryFactory"/> per call rather than
    /// injected one by one, mirroring how <c>FormBusinessObject</c> obtains its form repository.
    /// A new database-backed cache therefore adds a method here and leaves this constructor alone.
    /// <para>
    /// The per-company snapshots resolve the company database themselves — the permission and
    /// department tables live in a company database, so the company record must be read first to
    /// obtain its <c>CompanyDatabaseId</c>.
    /// </para>
    /// </remarks>
    public class CacheDataSourceProvider : ICacheDataSourceProvider
    {
        private readonly ISystemRepositoryFactory _systemFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="CacheDataSourceProvider"/> class.
        /// </summary>
        /// <param name="systemFactory">Factory that builds system-level repositories on demand.</param>
        public CacheDataSourceProvider(ISystemRepositoryFactory systemFactory)
        {
            _systemFactory = systemFactory ?? throw new ArgumentNullException(nameof(systemFactory));
        }

        /// <inheritdoc/>
        public SessionUser? GetSessionUser(Guid accessToken)
        {
            return _systemFactory.CreateSessionRepository().GetSession(accessToken);
        }

        /// <inheritdoc/>
        public CompanyInfo? GetCompanyInfo(string companyId)
        {
            return _systemFactory.CreateCompanyRepository().GetById(companyId);
        }

        /// <inheritdoc/>
        public CompanyRolePermissions? GetCompanyRolePermissions(string companyId)
        {
            var company = GetCompanyInfo(companyId);
            if (company == null) { return null; }

            string databaseId = company.CompanyDatabaseId;
            var repository = _systemFactory.CreateRolePermissionRepository();
            var grants = repository.GetRoleGrants(databaseId);
            var userRoles = repository.GetUserRoles(databaseId);
            return new CompanyRolePermissions(companyId, grants, userRoles);
        }

        /// <inheritdoc/>
        public DepartmentTree? GetDepartmentTree(string companyId)
        {
            var company = GetCompanyInfo(companyId);
            if (company == null) { return null; }

            var rows = _systemFactory.CreateDepartmentRepository().GetDepartments(company.CompanyDatabaseId);
            return new DepartmentTree(companyId, rows);
        }
    }
}
