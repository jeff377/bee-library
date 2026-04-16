using Bee.Definition;
using System;
using System.Globalization;
using System.Reflection;
using System.Text;

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
        /// Maximum length of CommandText included in log output. Longer values are truncated.
        /// </summary>
        private const int MaxCommandTextLogLength = 500;
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

            var elapsedSeconds = context.Stopwatch.Elapsed.TotalSeconds;
            bool isSlow = (opts.ExecutionTimeThreshold > 0) && (elapsedSeconds >= opts.ExecutionTimeThreshold);
            bool isLarge = (opts.AffectedRowThreshold > 0) && (affectedRows >= opts.AffectedRowThreshold);

            if (opts.Level == DbAccessAnomalyLogLevel.Warning && (isSlow || isLarge))
            {
                WriteWarning(context, affectedRows, elapsedSeconds, isSlow, isLarge);
            }
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

            var elapsedSeconds = context.Stopwatch.Elapsed.TotalSeconds;
            var (errCode, errNumber) = GetDbErrorInfo(exception);

            var sb = new StringBuilder(300);
            sb.Append("SQL execution error. ");
            if (!string.IsNullOrEmpty(context.DatabaseId)) sb.Append("DatabaseId=").Append(context.DatabaseId).Append("; ");
            sb.Append("Elapsed=").Append(elapsedSeconds.ToString("0.###", CultureInfo.InvariantCulture)).Append(" s; ");
            if (errCode.HasValue) sb.Append("ErrorCode=").Append(errCode.Value).Append("; ");
            if (errNumber.HasValue) sb.Append("Number=").Append(errNumber.Value).Append("; ");
            sb.Append("Exception=").Append(exception.GetType().FullName).Append("; ");
            sb.Append("CommandText=").Append(TruncateCommandText(context.CommandText));

            // Write error log
            // SysInfo.LogWriter?.WriteError(sb.ToString());
        }

        /// <summary>
        /// Writes an anomaly warning log entry.
        /// </summary>
        /// <param name="ctx">The database access log context.</param>
        /// <param name="affectedRows">The number of rows affected.</param>
        /// <param name="elapsedSeconds">The elapsed execution time in seconds.</param>
        /// <param name="isSlow">Whether the query was considered slow.</param>
        /// <param name="isLarge">Whether the operation affected a large number of rows.</param>
        private static void WriteWarning(DbLogContext ctx, int affectedRows, double elapsedSeconds, bool isSlow, bool isLarge)
        {
            var sb = new StringBuilder(300);
            sb.Append("SQL anomaly detected (");
            if (isSlow) sb.Append("Slow");
            if (isSlow && isLarge) sb.Append(", ");
            if (isLarge) sb.Append("LargeUpdate");
            sb.Append("). ");

            if (!string.IsNullOrEmpty(ctx.DatabaseId)) sb.Append("DbId=").Append(ctx.DatabaseId).Append("; ");
            sb.Append("Elapsed=").Append(elapsedSeconds.ToString("0.###", CultureInfo.InvariantCulture)).Append(" s; ");
            if (affectedRows >= 0) sb.Append("Rows=").Append(affectedRows).Append("; ");
            sb.Append("CommandText=").Append(TruncateCommandText(ctx.CommandText));

            // TODO: Write warning log
            // SysInfo.LogWriter?.WriteError(sb.ToString());
        }

        /// <summary>
        /// Truncates command text to <see cref="MaxCommandTextLogLength"/> to avoid exposing excessive SQL details in logs.
        /// </summary>
        /// <param name="commandText">The original command text.</param>
        private static string TruncateCommandText(string commandText)
        {
            if (string.IsNullOrEmpty(commandText) || commandText.Length <= MaxCommandTextLogLength)
                return commandText;
            return commandText.Substring(0, MaxCommandTextLogLength) + "...(truncated)";
        }

        /// <summary>
        /// Extracts the error code and error number from a database exception (if available).
        /// </summary>
        /// <param name="ex">The exception object.</param>
        /// <returns>A tuple where Item1 is the ErrorCode and Item2 is the Number (if available).</returns>
        private static Tuple<int?, int?> GetDbErrorInfo(Exception ex)
        {
            int? errorCode = null;
            if (ex is System.Data.Common.DbException dbex) errorCode = dbex.ErrorCode;

            int? number = null;
            var prop = ex.GetType().GetProperty("Number", BindingFlags.Instance | BindingFlags.Public);
            if (prop != null && prop.PropertyType == typeof(int))
            {
                try { number = (int)prop.GetValue(ex, null); } catch { /* ignore */ }
            }
            return Tuple.Create(errorCode, number);
        }
    }
}
