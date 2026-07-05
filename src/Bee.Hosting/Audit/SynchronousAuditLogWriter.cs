using Bee.Definition.Logging;

namespace Bee.Hosting.Audit
{
    /// <summary>
    /// <see cref="IAuditLogWriter"/> that persists each entry synchronously on the calling thread.
    /// Used when the background writer is disabled — notably for hosts without an <c>IHost</c>
    /// (an in-process local deployment), where a hosted service would never start.
    /// </summary>
    internal sealed class SynchronousAuditLogWriter : IAuditLogWriter
    {
        private readonly IAuditLogSink _sink;

        /// <summary>
        /// Initializes a new <see cref="SynchronousAuditLogWriter"/>.
        /// </summary>
        public SynchronousAuditLogWriter(IAuditLogSink sink)
        {
            _sink = sink ?? throw new ArgumentNullException(nameof(sink));
        }

        /// <inheritdoc/>
        public void Write(AuditEntry entry)
        {
            ArgumentNullException.ThrowIfNull(entry);
            _sink.WriteBatch(new[] { entry });
        }
    }
}
