namespace Bee.Definition.Logging
{
    /// <summary>
    /// Writes execution-anomaly entries to the log database. This is the entry point the API and
    /// data-access layers use to record executions that deviated from the normal envelope; the
    /// implementation owns the choice of synchronous versus background writing, batching, and
    /// durability fallback.
    /// </summary>
    /// <remarks>
    /// Separate from <see cref="IAuditLogWriter"/> because the two answer different questions: the
    /// audit trail records who did what to which record, an anomaly records which execution went
    /// wrong. No producer writes both, so depending on the narrower one keeps a layer that only
    /// observes anomalies from taking a dependency on the audit trail.
    /// <para>
    /// IMPORTANT: the separation is one-directional. <see cref="AnomalyEntry"/> derives from
    /// <c>AuditEntry</c> so the two share one write pipeline, which means
    /// <see cref="IAuditLogWriter"/> still accepts an anomaly entry. What the type system prevents
    /// is the reverse — an anomaly producer cannot write a login, change or access record.
    /// </para>
    /// </remarks>
    public interface IAnomalyLogWriter
    {
        /// <summary>
        /// Records an anomaly entry. Non-blocking on the default background implementation (the
        /// entry is enqueued); when the bounded queue is saturated the write degrades to
        /// synchronous so entries are never silently dropped.
        /// </summary>
        /// <param name="entry">The anomaly entry to record.</param>
        void Write(AnomalyEntry entry);
    }
}
