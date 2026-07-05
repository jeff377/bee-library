namespace Bee.Definition.Logging
{
    /// <summary>
    /// Writes audit-trail entries to the log database. This is the single entry point business
    /// code uses to record data-history events; the implementation owns the choice of synchronous
    /// versus background writing, batching, and durability fallback.
    /// </summary>
    public interface IAuditLogWriter
    {
        /// <summary>
        /// Records an audit entry. Non-blocking on the default background implementation (the entry
        /// is enqueued); when the bounded queue is saturated the write degrades to synchronous so
        /// entries are never silently dropped.
        /// </summary>
        /// <param name="entry">The audit entry to record.</param>
        void Write(AuditEntry entry);
    }
}
