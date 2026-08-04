using System.Data.Common;
using Bee.Definition.Settings;
using Bee.Repository.Abstractions.Factories;
using Bee.Repository.Abstractions.System;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Bee.Hosting.Session
{
    /// <summary>
    /// Hosted service that periodically deletes expired rows from <c>st_session</c>. Registered by
    /// <c>AddBeeFramework</c> when <see cref="SessionCleanupOptions.Enabled"/> is set.
    /// </summary>
    /// <remarks>
    /// Session reads have no side effects, so nothing reclaims an expired row on the request path
    /// while every sign-in inserts one — without this the table only grows. Deleting by expiry time
    /// is idempotent, so several nodes running this at once is safe and needs no coordination.
    /// </remarks>
    public sealed class ExpiredSessionCleanupService : BackgroundService
    {
        private readonly IRepositoryFactory _repositoryFactory;
        private readonly SessionCleanupOptions _options;
        private readonly ILogger<ExpiredSessionCleanupService> _logger;

        /// <summary>
        /// Initializes a new <see cref="ExpiredSessionCleanupService"/>.
        /// </summary>
        /// <param name="repositoryFactory">Factory that builds the session repository on demand.</param>
        /// <param name="options">Cleanup settings.</param>
        /// <param name="logger">Logger.</param>
        public ExpiredSessionCleanupService(
            IRepositoryFactory repositoryFactory,
            SessionCleanupOptions options,
            ILogger<ExpiredSessionCleanupService> logger)
        {
            _repositoryFactory = repositoryFactory ?? throw new ArgumentNullException(nameof(repositoryFactory));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            int intervalSeconds = _options.IntervalSeconds > 0 ? _options.IntervalSeconds : 3600;
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(intervalSeconds));

            // Sweep once at startup: a process that has been down for a while comes back to a table
            // holding every session that expired meanwhile.
            SafeCleanup();

            try
            {
                while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
                {
                    SafeCleanup();
                }
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown.
            }
        }

        private void SafeCleanup()
        {
            try
            {
                int deleted = _repositoryFactory.Create<ISessionRepository>().DeleteExpiredSessions();
                if (deleted > 0 && _logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation("Deleted {Count} expired session row(s).", deleted);
                }
            }
            catch (DbException ex)
            {
                // Resilience: a transient database error must not end the loop, or the table would
                // grow unchecked for the lifetime of the process. DbException covers every
                // provider's failure type.
                _logger.LogWarning(ex, "Expired session cleanup failed; will retry on the next tick.");
            }
        }
    }
}
