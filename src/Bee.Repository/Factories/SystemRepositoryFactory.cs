using Bee.Repository.Abstractions.Factories;
using Bee.Repository.Abstractions.System;

namespace Bee.Repository.Factories
{
    /// <summary>
    /// Default implementation of <see cref="ISystemRepositoryFactory"/>, now a thin adapter over
    /// <see cref="IRepositoryFactory"/>.
    /// </summary>
    /// <remarks>
    /// Kept only so consumers can move across one at a time; every method forwards to
    /// <see cref="IRepositoryFactory.Create{T}"/>. The interface it implements is what this work
    /// set out to remove — it grew a method per system table — so nothing new should be added here.
    /// </remarks>
    public class SystemRepositoryFactory : ISystemRepositoryFactory
    {
        private readonly IRepositoryFactory _factory;

        /// <summary>
        /// Initializes a new <see cref="SystemRepositoryFactory"/>.
        /// </summary>
        /// <param name="factory">The unified repository factory every method delegates to.</param>
        public SystemRepositoryFactory(IRepositoryFactory factory)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        /// <inheritdoc/>
        public IDatabaseRepository CreateDatabaseRepository() => _factory.Create<IDatabaseRepository>();

        /// <inheritdoc/>
        public ISessionRepository CreateSessionRepository() => _factory.Create<ISessionRepository>();

        /// <inheritdoc/>
        public ICompanyRepository CreateCompanyRepository() => _factory.Create<ICompanyRepository>();

        /// <inheritdoc/>
        public IUserCompanyRepository CreateUserCompanyRepository() => _factory.Create<IUserCompanyRepository>();

        /// <inheritdoc/>
        public IRolePermissionRepository CreateRolePermissionRepository() => _factory.Create<IRolePermissionRepository>();

        /// <inheritdoc/>
        public IDepartmentRepository CreateDepartmentRepository() => _factory.Create<IDepartmentRepository>();

        /// <inheritdoc/>
        public IUserRepository CreateUserRepository() => _factory.Create<IUserRepository>();

        /// <inheritdoc/>
        public IEmployeeRepository CreateEmployeeRepository() => _factory.Create<IEmployeeRepository>();

        /// <inheritdoc/>
        public IApiKeyRepository CreateApiKeyRepository() => _factory.Create<IApiKeyRepository>();
    }
}
