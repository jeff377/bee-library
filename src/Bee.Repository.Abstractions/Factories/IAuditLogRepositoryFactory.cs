using Bee.Repository.Abstractions.AuditLog;

namespace Bee.Repository.Abstractions.Factories
{
    /// <summary>
    /// Factory for creating the log-scoped <see cref="IAuditLogRepository"/>. Separate from
    /// <see cref="ISystemRepositoryFactory"/> because audit reads target the log database
    /// (a distinct <see cref="Bee.Definition.DbScope.Log"/> scope), not the common / company databases.
    /// </summary>
    public interface IAuditLogRepositoryFactory
    {
        /// <summary>
        /// Creates an <see cref="IAuditLogRepository"/> bound to the log database.
        /// </summary>
        IAuditLogRepository CreateAuditLogRepository();
    }
}
