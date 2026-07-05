namespace Bee.Definition.Logging
{
    /// <summary>
    /// No-op <see cref="IAuditLogWriter"/> used when audit logging is disabled. Registering it
    /// unconditionally lets consumers ctor-inject <see cref="IAuditLogWriter"/> without a null
    /// check, following the same pattern as <c>NullLogWriter</c>.
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
