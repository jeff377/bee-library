using System.Threading.Channels;
using Bee.Definition.Logging;
using Bee.Definition.Settings;
using Microsoft.Extensions.Hosting;

namespace Bee.Hosting.Audit
{
    /// <summary>
    /// Background writer: entries are enqueued onto a bounded in-memory channel and drained in
    /// batches by the hosted service, keeping the log-database write off the business request's
    /// critical path. When the queue is saturated the write degrades to synchronous rather than
    /// dropping the entry, so records are never silently lost.
    /// </summary>
    /// <remarks>
    /// Serves both writer interfaces from one instance: the queue, the batch drain and the
    /// saturation fallback are identical for an audit record and an anomaly record, so splitting
    /// the implementation would duplicate all three.
    /// </remarks>
    internal sealed class AuditLogWriterService : BackgroundService, IAuditLogWriter, IAnomalyLogWriter
    {
        private readonly IAuditLogSink _sink;
        private readonly Channel<AuditEntry> _channel;
        private readonly int _batchSize;

        /// <summary>
        /// Initializes a new <see cref="AuditLogWriterService"/>.
        /// </summary>
        public AuditLogWriterService(IAuditLogSink sink, AuditLogOptions options)
        {
            _sink = sink ?? throw new ArgumentNullException(nameof(sink));
            ArgumentNullException.ThrowIfNull(options);

            int capacity = options.QueueCapacity > 0 ? options.QueueCapacity : 10000;
            _batchSize = options.BatchSize > 0 ? options.BatchSize : 100;
            _channel = Channel.CreateBounded<AuditEntry>(new BoundedChannelOptions(capacity)
            {
                // Wait mode makes TryWrite return false (without blocking) when full, so the caller
                // can fall back to a synchronous write instead of dropping the entry.
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
            });
        }

        /// <inheritdoc/>
        public void Write(AuditEntry entry)
        {
            ArgumentNullException.ThrowIfNull(entry);
            if (!_channel.Writer.TryWrite(entry))
            {
                // Queue saturated — persist synchronously so the entry is not lost.
                _sink.WriteBatch(new[] { entry });
            }
        }

        /// <inheritdoc cref="IAnomalyLogWriter.Write(AnomalyEntry)"/>
        public void Write(AnomalyEntry entry) => Write((AuditEntry)entry);

        /// <inheritdoc/>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var reader = _channel.Reader;
            var batch = new List<AuditEntry>(_batchSize);
            try
            {
                while (await reader.WaitToReadAsync(stoppingToken).ConfigureAwait(false))
                {
                    DrainAndWrite(reader, batch);
                }
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown.
            }

            // Flush anything still buffered at shutdown (best effort).
            DrainAndWrite(reader, batch);
        }

        /// <summary>
        /// Drains up to <see cref="_batchSize"/> entries currently available and writes them as one
        /// batch; repeats until the reader is momentarily empty.
        /// </summary>
        private void DrainAndWrite(ChannelReader<AuditEntry> reader, List<AuditEntry> batch)
        {
            while (true)
            {
                batch.Clear();
                while (batch.Count < _batchSize && reader.TryRead(out var entry))
                {
                    batch.Add(entry);
                }
                if (batch.Count == 0) { break; }
                _sink.WriteBatch(batch);
            }
        }
    }
}
