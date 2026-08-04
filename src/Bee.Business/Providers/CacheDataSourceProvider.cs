using Bee.Base;
using Bee.Business.Session;
using Bee.Definition;
using Bee.Definition.Database;
using Bee.Definition.Identity;
using Bee.Definition.Organization;
using Bee.Definition.Security;
using Bee.Definition.Storage;
using Bee.Repository.Abstractions.Factories;
using Bee.Repository.Abstractions.System;

namespace Bee.Business.Providers
{
    /// <summary>
    /// Default <see cref="ICacheDataSourceProvider"/>: reads each cache's data from the system
    /// repositories and shapes it into the definition-layer type the cache stores.
    /// </summary>
    /// <remarks>
    /// Repositories are obtained from <see cref="IRepositoryFactory"/> per call rather than
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
        private readonly IRepositoryFactory _repositoryFactory;
        private readonly IServiceProvider _services;

        /// <summary>
        /// Initializes a new instance of the <see cref="CacheDataSourceProvider"/> class.
        /// </summary>
        /// <param name="repositoryFactory">Factory that builds framework repositories on demand.</param>
        /// <param name="services">
        /// Service provider used to resolve session-rebuild collaborators on first use.
        /// </param>
        public CacheDataSourceProvider(IRepositoryFactory repositoryFactory, IServiceProvider services)
        {
            _repositoryFactory = repositoryFactory ?? throw new ArgumentNullException(nameof(repositoryFactory));
            _services = services ?? throw new ArgumentNullException(nameof(services));
        }

        /// <inheritdoc/>
        /// <remarks>
        /// Rebuilding re-runs the derivations sign-in performed rather than restoring a snapshot:
        /// the seed carries only the token, user, expiry and company, and roles, customization code
        /// and record-scope identity are recomputed here. That is what keeps a permission revoked
        /// after sign-in from surviving in the rebuilt session.
        /// </remarks>
        public SessionInfo? GetSessionInfo(Guid accessToken)
        {
            // Resolved per call, not in the constructor: this provider is itself built lazily on a
            // cache miss, and the key provider can depend on the session service, which would close
            // a construction cycle back onto the cache container.
            var keyProvider = _services.GetRequiredService<IApiEncryptionKeyProvider>();
            // A provider whose key lives only inside the session cannot supply one for a session
            // that is no longer cached. Rebuilding anyway would produce a session that looks signed
            // in but fails every encrypted call, so the token is treated as dead instead.
            if (!keyProvider.SupportsSessionRebuild) { return null; }

            // No `common` database configured means there is no seed store to read, which is the
            // normal state for an in-process host that never wired one up. A session lookup must
            // answer "not a session" there rather than surfacing a connection-manager failure —
            // the caller is `AccessTokenValidator`, whose only question is whether the token
            // authenticates.
            if (!HasCommonDatabase()) { return null; }

            var seed = _repositoryFactory.Create<ISessionRepository>().GetSession(accessToken);
            if (seed == null) { return null; }

            var sessionInfo = new SessionInfo
            {
                AccessToken = seed.AccessToken,
                UserId = seed.UserID,
                UserName = seed.UserName,
                ExpiredAt = seed.EndTime,
                ApiEncryptionKey = keyProvider.GetKey(seed.AccessToken),
            };

            var locale = _repositoryFactory.Create<IUserRepository>().GetLocale(seed.UserID);
            var backend = _services.GetRequiredService<IDefineAccess>().GetSystemSettings().BackendConfiguration;
            sessionInfo.TimeZone = StringUtilities.IsNotEmpty(locale.TimeZone) ? locale.TimeZone : backend.DefaultTimeZone;
            sessionInfo.Culture = StringUtilities.IsNotEmpty(locale.Culture) ? locale.Culture : backend.DefaultLanguage;

            if (StringUtilities.IsNotEmpty(seed.CompanyId))
            {
                // Access is re-checked here, so a user whose company permission was revoked does
                // not come back into that company on the next rebuild.
                var binding = _services.GetRequiredService<SessionCompanyBinder>().Bind(sessionInfo, seed.CompanyId!);
                if (binding == null) { return null; }
            }

            return sessionInfo;
        }

        /// <inheritdoc/>
        public CompanyInfo? GetCompanyInfo(string companyId)
        {
            return _repositoryFactory.Create<ICompanyRepository>().GetById(companyId);
        }

        /// <inheritdoc/>
        public CompanyRolePermissions? GetCompanyRolePermissions(string companyId)
        {
            var company = GetCompanyInfo(companyId);
            if (company == null) { return null; }

            string databaseId = company.CompanyDatabaseId;
            var repository = _repositoryFactory.Create<IRolePermissionRepository>();
            var grants = repository.GetRoleGrants(databaseId);
            var userRoles = repository.GetUserRoles(databaseId);
            return new CompanyRolePermissions(companyId, grants, userRoles);
        }

        /// <inheritdoc/>
        public DepartmentTree? GetDepartmentTree(string companyId)
        {
            var company = GetCompanyInfo(companyId);
            if (company == null) { return null; }

            var rows = _repositoryFactory.Create<IDepartmentRepository>().GetDepartments(company.CompanyDatabaseId);
            return new DepartmentTree(companyId, rows);
        }

        /// <inheritdoc/>
        public ApiKeyInfo? GetApiKey(string sysId)
        {
            // Same guard as the session lookup: an in-process host with no `common` database has no
            // key store, and "no store" has to read as "no such key" rather than a connection-manager
            // failure.
            if (!HasCommonDatabase()) { return null; }

            return _repositoryFactory.Create<IApiKeyRepository>().GetEnabledById(sysId);
        }

        /// <inheritdoc/>
        /// <remarks>
        /// WARNING: no try/catch here, and none in the caller. A database failure must reach the
        /// validator so it can reject the call; turning it into a not-in-force state would silently
        /// reopen the key gate for the duration of an outage. Only the two definitive answers —
        /// no configured store, and a store whose table is absent or empty — report not-in-force.
        /// </remarks>
        public ApiKeyGateState GetApiKeyGateState()
        {
            if (!HasCommonDatabase()) { return new ApiKeyGateState { InForce = false }; }

            return _repositoryFactory.Create<IApiKeyRepository>().GetGateState();
        }

        /// <summary>
        /// Returns whether a <c>common</c> database is configured for this host.
        /// </summary>
        private bool HasCommonDatabase()
        {
            var databaseItems = _services.GetRequiredService<IDatabaseSettingsProvider>().Get().Items;
            return databaseItems?.GetOrDefault(DbCategoryIds.Common) != null;
        }
    }
}
