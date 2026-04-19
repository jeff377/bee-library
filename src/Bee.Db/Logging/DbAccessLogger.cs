using Bee.Definition;
using Bee.Definition.Logging;
using System;

namespace Bee.Db.Logging
{
    /// <summary>
    /// Provides logging functionality for database access operations.
    /// Covers command start, end, anomaly, and error logging,
    /// useful for tracking SQL performance, anomalies, and error analysis.
    /// </summary>
    public static class DbAccessLogger
    {
        /// <summary>
        /// Starts logging a database command execution.
        /// </summary>
        /// <param name="command">The database command specification.</param>
        /// <param name="databaseId">The database identifier.</param>
        public static DbLogContext LogStart(DbCommandSpec command, string databaseId = "")
        {
            return new DbLogContext(command.CommandText, databaseId: databaseId);
        }

        /// <summary>
        /// Ends logging for a database command execution.
        /// Stops the timer and logs a slow query or large update warning based on configured thresholds.
        /// </summary>
        /// <param name="context">The database access log context.</param>
        /// <param name="affectedRows">The number of rows affected; defaults to -1 (unknown).</param>
        public static void LogEnd(DbLogContext context, int affectedRows = -1)
        {
            if (context == null) throw new ArgumentNullException(nameof(context), "context cannot be null.");
            context.Stopwatch.Stop();

            var opts = BackendInfo.LogOptions == null ? null : BackendInfo.LogOptions.DbAccess;
            if (opts == null) return;

            if (ShouldWarn(opts, context.Stopwatch.Elapsed.TotalSeconds, affectedRows))
            {
                WriteWarning();
            }
        }

        /// <summary>
        /// Determines whether an anomaly warning should be logged for the given options and runtime metrics.
        /// </summary>
        /// <param name="opts">The database access anomaly log options.</param>
        /// <param name="elapsedSeconds">The command execution elapsed time in seconds.</param>
        /// <param name="affectedRows">The number of rows affected.</param>
        /// <returns>True if a warning should be written; otherwise, false.</returns>
        internal static bool ShouldWarn(DbAccessAnomalyLogOptions opts, double elapsedSeconds, int affectedRows)
        {
            if (opts == null) return false;
            bool isSlow = (opts.ExecutionTimeThreshold > 0) && (elapsedSeconds >= opts.ExecutionTimeThreshold);
            bool isLarge = (opts.AffectedRowThreshold > 0) && (affectedRows >= opts.AffectedRowThreshold);
            return opts.Level == DbAccessAnomalyLogLevel.Warning && (isSlow || isLarge);
        }

        /// <summary>
        /// Logs a database access error.
        /// Stops the timer and records the exception, elapsed time, database identifier, and SQL command text.
        /// </summary>
        /// <param name="context">The database access log context.</param>
        /// <param name="exception">The exception that occurred.</param>
        public static void LogError(DbLogContext context, Exception exception)
        {
            if (context == null) throw new ArgumentNullException(nameof(context), "context cannot be null.");
            if (exception == null) throw new ArgumentNullException(nameof(exception), "exception cannot be null.");

            try { context.Stopwatch.Stop(); } catch { /* ignore */ }

            // Placeholder: no-op until an external logger is injected into BackendInfo.
        }

        /// <summary>
        /// Writes an anomaly warning log entry.
        /// </summary>
        private static void WriteWarning()
        {
            // Placeholder: no-op until an external logger is injected into BackendInfo.
        }
    }
}
