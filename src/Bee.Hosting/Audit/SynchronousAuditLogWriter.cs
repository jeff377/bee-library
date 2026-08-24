using Bee.Definition.Logging;

namespace Bee.Hosting.Audit
{
    /// <summary>
    /// Writer that persists each entry synchronously on the calling thread. Used when the
    /// background writer is disabled — notably for hosts without an <c>IHost</c> (an in-process
    /// local deployment), where a hosted service would never start.
    /// </summary>
    /// <remarks>
    /// Serves both writer interfaces from one instance: the queueing, batching and durability
    /// behaviour is identical for an audit record and an anomaly record, so splitting the
    /// implementation would duplicate it.
    /// </remarks>
    internal sealed class SynchronousAuditLogWriter : IAuditLogWriter, IAnomalyLogWriter
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

        /// <inheritdoc cref="IAnomalyLogWriter.Write(AnomalyEntry)"/>
        public void Write(AnomalyEntry entry) => Write((AuditEntry)entry);
    }
}
