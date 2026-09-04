using System.Threading.Channels;
using Bee.Definition.Logging;
using Bee.Definition.Settings;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Bee.Hosting.Audit
{
    /// <summary>
    /// Background writer: entries are enqueued onto a bounded in-memory channel and drained in
    /// batches by the hosted service, keeping the log-database write off the business request's
    /// critical path. When the queue is saturated the write degrades to synchronous rather than
    /// dropping the entry.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The saturation fallback is what keeps a busy process from dropping records. It is not a
    /// guarantee that nothing is ever lost: if the sink itself fails, the batch is reported at error
    /// level and discarded, because the alternative — letting the exception escape — stops the whole
    /// application. See <see cref="SafeDrain"/>.
    /// </para>
    /// <para>
    /// Serves both writer interfaces from one instance: the queue, the batch drain and the
    /// saturation fallback are identical for an audit record and an anomaly record, so splitting
    /// the implementation would duplicate all three.
    /// </para>
    /// </remarks>
    internal sealed class AuditLogWriterService : BackgroundService, IAuditLogWriter, IAnomalyLogWriter
    {
        private readonly IAuditLogSink _sink;
        private readonly ILogger<AuditLogWriterService> _logger;
        private readonly Channel<AuditEntry> _channel;
        private readonly int _batchSize;

        /// <summary>
        /// Initializes a new <see cref="AuditLogWriterService"/>.
        /// </summary>
        public AuditLogWriterService(
            IAuditLogSink sink, AuditLogOptions options, ILogger<AuditLogWriterService> logger)
        {
            _sink = sink ?? throw new ArgumentNullException(nameof(sink));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
                    SafeDrain(reader, batch);
                }
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown.
            }

            // Flush anything still buffered at shutdown (best effort).
            SafeDrain(reader, batch);
        }

        /// <summary>
        /// Runs one drain and keeps the loop alive whatever the sink does.
        /// </summary>
        /// <param name="reader">The channel reader.</param>
        /// <param name="batch">The reusable batch buffer.</param>
        /// <remarks>
        /// <para>
        /// WARNING: an exception escaping <see cref="ExecuteAsync"/> faults the
        /// <see cref="BackgroundService"/>, and .NET's default
        /// <c>BackgroundServiceExceptionBehavior.StopHost</c> then stops the whole application —
        /// <b>a failed log write would take the deployment down with it</b>. ADR-017 established this
        /// for <c>CacheNotifyPoller</c>; this service was the one that never got it.
        /// </para>
        /// <para>
        /// The catch is deliberately unfiltered, and deliberately unlike its sibling services.
        /// <c>CacheNotifyPoller</c> and <c>ExpiredSessionCleanupService</c> call framework-internal
        /// code, so they can enumerate what it throws and let anything else through as a bug —
        /// <c>ExpiredSessionCleanupService</c> has a test pinning exactly that. Here the call goes to
        /// <see cref="IAuditLogSink"/>, a public DI seam: a deployment's own sink can throw anything
        /// at all, and the framework has no list to write. Losing a batch of log records is strictly
        /// better than stopping the application over one, and the failure is reported at error level
        /// rather than swallowed.
        /// </para>
        /// <para>
        /// <see cref="OperationCanceledException"/> is left to the caller, which treats it as normal
        /// shutdown.
        /// </para>
        /// </remarks>
        private void SafeDrain(ChannelReader<AuditEntry> reader, List<AuditEntry> batch)
        {
            try
            {
                DrainAndWrite(reader, batch);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Audit log drain failed; the batch is lost and the writer continues.");
            }
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
