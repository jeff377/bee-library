using Bee.Db.CacheNotify;
using Bee.Db.Manager;
using Bee.Definition.Storage;
using Bee.Repository.Abstractions.Factories;
using Bee.Repository.Abstractions.System;
using Bee.Repository.System;

namespace Bee.Repository.Factories
{
    /// <summary>
    /// Default implementation of <see cref="ISystemRepositoryFactory"/>.
    /// </summary>
    public class SystemRepositoryFactory : ISystemRepositoryFactory
    {
        private readonly IDefineAccess _defineAccess;
        private readonly IDbConnectionManager _connectionManager;
        private readonly ICacheNotifyService? _cacheNotify;

        /// <summary>
        /// Initializes a new <see cref="SystemRepositoryFactory"/> without a cache invalidation
        /// channel.
        /// </summary>
        /// <param name="defineAccess">The define access service used by repositories that need to read
        /// the defined table schema (e.g., schema upgrade).</param>
        /// <param name="connectionManager">The DI-resolved connection manager.</param>
        /// <remarks>
        /// NOTE: kept as a separate overload rather than giving the channel parameter a default value.
        /// Adding an optional parameter to a shipped public constructor keeps source compatibility but
        /// breaks binary compatibility for already-compiled consumers, and this type ships in a NuGet
        /// package.
        /// </remarks>
        public SystemRepositoryFactory(IDefineAccess defineAccess, IDbConnectionManager connectionManager)
            : this(defineAccess, connectionManager, null) { }

        /// <summary>
        /// Initializes a new <see cref="SystemRepositoryFactory"/>.
        /// </summary>
        /// <param name="defineAccess">The define access service used by repositories that need to read
        /// the defined table schema (e.g., schema upgrade).</param>
        /// <param name="connectionManager">The DI-resolved connection manager.</param>
        /// <param name="cacheNotify">
        /// Cross-process cache invalidation channel, required only by repositories that write data a
        /// cache holds. <c>null</c> leaves those writes without a notification, which is the correct
        /// shape for hosts and tests that never poll the channel.
        /// </param>
        public SystemRepositoryFactory(IDefineAccess defineAccess, IDbConnectionManager connectionManager,
            ICacheNotifyService? cacheNotify)
        {
            _defineAccess = defineAccess ?? throw new ArgumentNullException(nameof(defineAccess));
            _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
            _cacheNotify = cacheNotify;
        }

        /// <summary>
        /// Creates an <see cref="IDatabaseRepository"/>.
        /// </summary>
        public IDatabaseRepository CreateDatabaseRepository()
        {
            return new DatabaseRepository(_defineAccess, _connectionManager);
        }

        /// <summary>
        /// Creates an <see cref="ISessionRepository"/>.
        /// </summary>
        public ISessionRepository CreateSessionRepository()
        {
            return new SessionRepository(_connectionManager);
        }

        /// <summary>
        /// Creates an <see cref="ICompanyRepository"/>.
        /// </summary>
        public ICompanyRepository CreateCompanyRepository()
        {
            return new CompanyRepository(_connectionManager);
        }

        /// <summary>
        /// Creates an <see cref="IUserCompanyRepository"/>.
        /// </summary>
        public IUserCompanyRepository CreateUserCompanyRepository()
        {
            return new UserCompanyRepository(_connectionManager);
        }

        /// <summary>
        /// Creates an <see cref="IRolePermissionRepository"/> (per-company permission tables).
        /// </summary>
        public IRolePermissionRepository CreateRolePermissionRepository()
        {
            return new RolePermissionRepository(_connectionManager);
        }

        /// <summary>
        /// Creates an <see cref="IDepartmentRepository"/> (per-company <c>st_department</c> reader).
        /// </summary>
        public IDepartmentRepository CreateDepartmentRepository()
        {
            return new DepartmentRepository(_connectionManager);
        }

        /// <summary>
        /// Creates an <see cref="IUserRepository"/> (common <c>st_user</c> reader).
        /// </summary>
        public IUserRepository CreateUserRepository()
        {
            return new UserRepository(_connectionManager);
        }

        /// <summary>
        /// Creates an <see cref="IEmployeeRepository"/> (per-company <c>st_employee</c> reader).
        /// </summary>
        public IEmployeeRepository CreateEmployeeRepository()
        {
            return new EmployeeRepository(_connectionManager);
        }

        /// <summary>
        /// Creates an <see cref="IApiKeyRepository"/> (common <c>st_api_key</c> access).
        /// </summary>
        public IApiKeyRepository CreateApiKeyRepository()
        {
            return new ApiKeyRepository(_connectionManager, _cacheNotify);
        }
    }
}
