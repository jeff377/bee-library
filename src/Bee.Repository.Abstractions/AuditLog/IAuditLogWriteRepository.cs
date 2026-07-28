using Bee.Definition.Logging;

namespace Bee.Repository.Abstractions.AuditLog
{
    /// <summary>
    /// Write side of the audit log: persists <see cref="AuditEntry"/> rows into the log database.
    /// Counterpart to <see cref="IAuditLogRepository"/>, which reads them back.
    /// </summary>
    /// <remarks>
    /// Deliberately separate from <see cref="IAuditLogRepository"/>: the read side is queried
    /// per-database by <c>IAuditLogRepositoryFactory</c>, whereas writes always target the
    /// conventional log database and are driven by the hosting layer's writer pipeline.
    /// </remarks>
    public interface IAuditLogWriteRepository
    {
        /// <summary>
        /// Persists a batch of audit entries. Implementations do not swallow database failures —
        /// the caller decides whether to retry, spill to a fallback store, or drop the batch.
        /// </summary>
        /// <param name="entries">The entries to persist; an empty batch is a no-op.</param>
        void WriteBatch(IReadOnlyList<AuditEntry> entries);
    }
}
