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

        /// <summary>
        /// Initializes a new <see cref="SystemRepositoryFactory"/>.
        /// </summary>
        /// <param name="defineAccess">The define access service used by repositories that need to read
        /// the defined table schema (e.g., schema upgrade).</param>
        /// <param name="connectionManager">The DI-resolved connection manager.</param>
        public SystemRepositoryFactory(IDefineAccess defineAccess, IDbConnectionManager connectionManager)
        {
            _defineAccess = defineAccess ?? throw new ArgumentNullException(nameof(defineAccess));
            _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
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
        /// Creates an <see cref="IDepartmentRepository"/> (per-company <c>ft_department</c> reader).
        /// </summary>
        public IDepartmentRepository CreateDepartmentRepository()
        {
            return new DepartmentRepository(_connectionManager);
        }
    }
}
