using Bee.Definition.Identity;
using Bee.Repository.Abstractions.Factories;
using Bee.Repository.Abstractions.System;

namespace Bee.Business.Session
{
    /// <summary>
    /// Applies a company context onto a session: checks the user's access, then snapshots the
    /// customization code, roles and record-scope row ids onto the session.
    /// </summary>
    /// <remarks>
    /// Shared by <c>SystemBusinessObject.EnterCompany</c> and by session rebuild, which must
    /// arrive at exactly the same session state — a rebuild that derived company context
    /// differently would silently hand the user a different set of permissions than the one they
    /// signed in with. Re-running the access check on rebuild is also what makes a revoked company
    /// permission take effect: the session fails to come back rather than living on as a snapshot.
    /// </remarks>
    public class SessionCompanyBinder
    {
        private readonly ICompanyInfoService _companyInfoService;
        private readonly IRepositoryFactory _repositoryFactory;
        private readonly IRolePermissionService _rolePermissionService;
        private readonly IEmployeeContextResolver _employeeContextResolver;

        /// <summary>
        /// Initializes a new <see cref="SessionCompanyBinder"/>.
        /// </summary>
        /// <param name="companyInfoService">Company record lookup.</param>
        /// <param name="repositoryFactory">Factory for the user-company grant repository.</param>
        /// <param name="rolePermissionService">Per-company role permission snapshot.</param>
        /// <param name="employeeContextResolver">Record-scope identity resolver.</param>
        public SessionCompanyBinder(
            ICompanyInfoService companyInfoService,
            IRepositoryFactory repositoryFactory,
            IRolePermissionService rolePermissionService,
            IEmployeeContextResolver employeeContextResolver)
        {
            _companyInfoService = companyInfoService ?? throw new ArgumentNullException(nameof(companyInfoService));
            _repositoryFactory = repositoryFactory ?? throw new ArgumentNullException(nameof(repositoryFactory));
            _rolePermissionService = rolePermissionService ?? throw new ArgumentNullException(nameof(rolePermissionService));
            _employeeContextResolver = employeeContextResolver ?? throw new ArgumentNullException(nameof(employeeContextResolver));
        }

        /// <summary>
        /// Binds <paramref name="sessionInfo"/> to the given company, or returns <c>null</c> when
        /// the company is unknown, disabled, or the user has no access to it.
        /// </summary>
        /// <param name="sessionInfo">The session to mutate.</param>
        /// <param name="companyId">The target company id.</param>
        /// <returns>The binding, or <c>null</c> when access is denied.</returns>
        /// <remarks>
        /// The three failure modes collapse into a single <c>null</c> on purpose: distinguishing
        /// them would let a caller enumerate companies by probing which error comes back.
        /// </remarks>
        public SessionCompanyBinding? Bind(SessionInfo sessionInfo, string companyId)
        {
            ArgumentNullException.ThrowIfNull(sessionInfo);
            ArgumentException.ThrowIfNullOrWhiteSpace(companyId);

            var companyInfo = _companyInfoService.Get(companyId);
            if (companyInfo == null) { return null; }

            var userCompanyRepository = _repositoryFactory.Create<IUserCompanyRepository>();
            if (!userCompanyRepository.HasAccess(sessionInfo.UserId, companyId)) { return null; }

            sessionInfo.CompanyId = companyId;
            // Derive the session's customization code from the company (empty when the company
            // ships no customization). The session-level overlay reads this value downstream.
            sessionInfo.CustomizeId = companyInfo.CustomizeId;
            // Snapshot the user's roles for this company so the layer-1 Can check runs from
            // SessionInfo.Roles (sys_id) without re-hitting the database on every request.
            var snapshot = _rolePermissionService.Get(companyId);
            sessionInfo.Roles = snapshot?.GetUserRoleIds(sessionInfo.UserId).ToList() ?? [];
            // Compute the per-model capability snapshot once, here, where the roles and the
            // permission snapshot are both in hand — the client caches it to degrade UI elements
            // without any extra round-trip.
            var capabilities = snapshot?.GetAllowedByModel(sessionInfo.Roles) ?? [];
            // Snapshot the user's record-scope identity (user/employee/department row ids) so
            // layer-2 scope filtering runs from the session without re-hitting the database.
            var employeeContext = _employeeContextResolver.Resolve(sessionInfo.UserId, companyInfo.CompanyDatabaseId);
            sessionInfo.UserRowId = employeeContext.UserRowId;
            sessionInfo.EmployeeRowId = employeeContext.EmployeeRowId;
            sessionInfo.DeptRowId = employeeContext.DeptRowId;

            return new SessionCompanyBinding(companyInfo, capabilities);
        }
    }
}
