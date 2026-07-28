namespace Bee.Definition.Logging
{
    /// <summary>
    /// No-op <see cref="IAuditLogWriter"/> used when audit logging is disabled. Registering it
    /// unconditionally lets consumers ctor-inject <see cref="IAuditLogWriter"/> without a null
    /// check. Audit writing is best-effort by design: a failure to record must not fail the operation being recorded.
    /// </summary>
    public sealed class NullAuditLogWriter : IAuditLogWriter
    {
        /// <summary>Gets the shared singleton instance.</summary>
        public static NullAuditLogWriter Instance { get; } = new();

        private NullAuditLogWriter() { }

        /// <inheritdoc/>
        public void Write(AuditEntry entry)
        {
            // Intentionally does nothing: audit logging is disabled.
        }
    }
}
