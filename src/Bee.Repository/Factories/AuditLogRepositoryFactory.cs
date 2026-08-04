using Bee.Repository.Abstractions.AuditLog;
using Bee.Repository.Abstractions.Factories;

namespace Bee.Repository.Factories
{
    /// <summary>
    /// Default <see cref="IAuditLogRepositoryFactory"/>, now a thin adapter over
    /// <see cref="IRepositoryFactory"/>.
    /// </summary>
    /// <remarks>
    /// Kept only so consumers can move across one at a time. The log database is no longer named
    /// here: <c>AuditLogRepository</c> declares its own scope, which is where the routing of every
    /// other repository now lives too.
    /// </remarks>
    public class AuditLogRepositoryFactory : IAuditLogRepositoryFactory
    {
        private readonly IRepositoryFactory _factory;

        /// <summary>
        /// Initializes a new <see cref="AuditLogRepositoryFactory"/>.
        /// </summary>
        /// <param name="factory">The unified repository factory this delegates to.</param>
        public AuditLogRepositoryFactory(IRepositoryFactory factory)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        /// <inheritdoc/>
        public IAuditLogRepository CreateAuditLogRepository() => _factory.Create<IAuditLogRepository>();
    }
}
