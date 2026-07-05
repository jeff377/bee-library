using Bee.Definition.Logging;

namespace Bee.Hosting.Audit
{
    /// <summary>
    /// Terminal writer that persists audit entries. Abstracted from the writers (background /
    /// synchronous) so the durability target (database + file fallback) can be swapped or faked
    /// in tests without touching the queueing logic.
    /// </summary>
    internal interface IAuditLogSink
    {
        /// <summary>
        /// Persists a batch of entries. Implementations must not throw for expected persistence
        /// failures — a failed write is logged and (optionally) spilled to a file, so a log-store
        /// outage never propagates into the business flow.
        /// </summary>
        /// <param name="entries">The entries to persist.</param>
        void WriteBatch(IReadOnlyList<AuditEntry> entries);
    }
}
