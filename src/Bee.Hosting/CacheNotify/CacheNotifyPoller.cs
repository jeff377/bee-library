using System.Data.Common;
using Bee.Db;
using Bee.Definition.Settings;
using Bee.ObjectCaching;
using Bee.ObjectCaching.CacheNotify;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Bee.Hosting.CacheNotify
{
    /// <summary>
    /// Hosted service that periodically polls the <c>st_cache_notify</c> table and evicts cached
    /// objects whose source data changed, propagating invalidations across processes / nodes that
    /// each poll the same table. Registered by <c>AddBeeFramework</c> when
    /// <see cref="CacheNotifyOptions.Enabled"/> is set.
    /// </summary>
    /// <remarks>
    /// A thin timer shell around <see cref="CacheNotifyPollSession"/>: it owns one session for the
    /// configured database and ticks it on <see cref="CacheNotifyOptions.IntervalSeconds"/>. A
    /// failed poll is logged and the loop continues, so a transient database error does not stop
    /// invalidation permanently.
    /// </remarks>
    public sealed class CacheNotifyPoller : BackgroundService
    {
        private readonly IDbAccessFactory _dbAccessFactory;
        private readonly ICacheContainer _container;
        private readonly ICacheNotifyRouter _router;
        private readonly CacheNotifyOptions _options;
        private readonly ILogger<CacheNotifyPoller> _logger;

        /// <summary>
        /// Initializes a new <see cref="CacheNotifyPoller"/>.
        /// </summary>
        public CacheNotifyPoller(
            IDbAccessFactory dbAccessFactory,
            ICacheContainer container,
            ICacheNotifyRouter router,
            CacheNotifyOptions options,
            ILogger<CacheNotifyPoller> logger)
        {
            _dbAccessFactory = dbAccessFactory ?? throw new ArgumentNullException(nameof(dbAccessFactory));
            _container = container ?? throw new ArgumentNullException(nameof(container));
            _router = router ?? throw new ArgumentNullException(nameof(router));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var session = new CacheNotifyPollSession(
                _options.DatabaseId, _dbAccessFactory, _container, _router, _options.MarginSeconds);

            int intervalSeconds = _options.IntervalSeconds > 0 ? _options.IntervalSeconds : 5;
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(intervalSeconds));

            // Take the baseline cursor immediately so the first real tick already polls a delta.
            SafePoll(session);

            try
            {
                while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
                {
                    SafePoll(session);
                }
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown.
            }
        }

        private void SafePoll(CacheNotifyPollSession session)
        {
            try
            {
                session.Poll();
            }
            catch (DbException ex)
            {
                // Resilience: a transient DB error must not terminate the poll loop, or cache
                // invalidation would stop permanently. DbException covers every provider's
                // exception type (SqlException / NpgsqlException / MySqlException / OracleException).
                _logger.LogWarning(ex, "Cache-notify poll failed against database '{DatabaseId}'.", _options.DatabaseId);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Cache-notify poll failed against database '{DatabaseId}'.", _options.DatabaseId);
            }
        }
    }
}
