namespace Bee.Definition.Logging
{
    /// <summary>
    /// No-op writer used when audit logging is disabled, and when anomaly logging alone is
    /// disabled. Registering it unconditionally lets consumers ctor-inject
    /// <see cref="IAuditLogWriter"/> or <see cref="IAnomalyLogWriter"/> without a null check.
    /// Both are best-effort by design: a failure to record must not fail the operation being recorded.
    /// </summary>
    /// <remarks>
    /// The name predates the split into two writer interfaces and is kept: renaming a public type
    /// would be a breaking change that buys nothing but a more fitting name.
    /// </remarks>
    public sealed class NullAuditLogWriter : IAuditLogWriter, IAnomalyLogWriter
    {
        /// <summary>Gets the shared singleton instance.</summary>
        public static NullAuditLogWriter Instance { get; } = new();

        private NullAuditLogWriter() { }

        /// <inheritdoc/>
        public void Write(AuditEntry entry)
        {
            // Intentionally does nothing: logging is disabled.
        }

        /// <inheritdoc cref="IAnomalyLogWriter.Write(AnomalyEntry)"/>
        public void Write(AnomalyEntry entry) => Write((AuditEntry)entry);
    }
}
