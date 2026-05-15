using Bee.Definition;
using Bee.Definition.Database;
using Bee.Definition.Identity;
using Bee.Repository.Abstractions;

namespace Bee.Repository
{
    /// <summary>
    /// Default <see cref="IRepositoryDatabaseRouter"/> implementation. Routes
    /// <see cref="DbScope"/> values to a physical databaseId by combining fixed
    /// rules (Common / Log) with session lookup (Company).
    /// </summary>
    public sealed class RepositoryDatabaseRouter : IRepositoryDatabaseRouter
    {
        private readonly ISessionInfoService _sessionService;
        private readonly ICompanyInfoService _companyService;

        /// <summary>
        /// Initializes a new <see cref="RepositoryDatabaseRouter"/>.
        /// </summary>
        /// <param name="sessionService">Session lookup service.</param>
        /// <param name="companyService">Company info lookup service.</param>
        public RepositoryDatabaseRouter(
            ISessionInfoService sessionService,
            ICompanyInfoService companyService)
        {
            _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
            _companyService = companyService ?? throw new ArgumentNullException(nameof(companyService));
        }

        /// <inheritdoc/>
        public string Resolve(DbScope scope, Guid accessToken)
        {
            // Common / Log share fixed databaseIds across all sessions and do not
            // require an access token. This lets pre-EnterCompany methods (Login,
            // Logout, etc.) write audit logs without resolving a company context.
            switch (scope)
            {
                case DbScope.Common: return DbCategoryIds.Common;
                case DbScope.Log:    return DbCategoryIds.Log;
            }

            if (scope != DbScope.Company)
                throw new InvalidOperationException($"Unsupported DbScope value: {scope}.");

            var session = _sessionService.Get(accessToken)
                ?? throw new UnauthorizedAccessException("Session not found or has expired.");

            if (string.IsNullOrEmpty(session.CompanyId))
                throw new InvalidOperationException("CompanyNotEntered");

            var company = _companyService.Get(session.CompanyId)
                ?? throw new InvalidOperationException(
                    "Company information unavailable; please re-enter the company.");

            return company.CompanyDatabaseId;
        }
    }
}
