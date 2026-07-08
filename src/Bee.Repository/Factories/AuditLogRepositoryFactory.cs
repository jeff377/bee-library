using Bee.Db.Manager;
using Bee.Definition.Database;
using Bee.Repository.Abstractions.Factories;
using Bee.Repository.Abstractions.AuditLog;
using Bee.Repository.AuditLog;

namespace Bee.Repository.Factories
{
    /// <summary>
    /// Default <see cref="IAuditLogRepositoryFactory"/>. Binds the created repository to the
    /// conventional <c>log</c> database (<see cref="DbCategoryIds.Log"/>), matching the write side.
    /// </summary>
    public class AuditLogRepositoryFactory : IAuditLogRepositoryFactory
    {
        private readonly IDbConnectionManager _connectionManager;

        /// <summary>
        /// Initializes a new <see cref="AuditLogRepositoryFactory"/>.
        /// </summary>
        /// <param name="connectionManager">The DI-resolved connection manager.</param>
        public AuditLogRepositoryFactory(IDbConnectionManager connectionManager)
        {
            _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
        }

        /// <inheritdoc/>
        public IAuditLogRepository CreateAuditLogRepository()
            => new AuditLogRepository(_connectionManager, DbCategoryIds.Log);
    }
}
