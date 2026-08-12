using System.Data.Common;
using System.Diagnostics;

using Bee.Definition.Database;
using Bee.Definition.Logging;

namespace Bee.Db
{
    /// <summary>
    /// Anomaly detection around command execution: what counts as slow or oversized, and how a failure is recorded.
    /// </summary>
    /// <remarks>
    /// Kept apart from the execution path because it is entirely best-effort: nothing here may alter a
    /// command's outcome. `DbAnomalyDetail` lives with it — it has no other caller.
    /// </remarks>
    public partial class DbAccess
    {
        /// <summary>
        /// Runs <paramref name="exec"/>, detecting and recording anomalies (Error / Timeout on
        /// failure, Slow / LargeAffected / LargeResult on success). A no-op wrapper when anomaly
        /// logging is disabled. Anomaly writes are best-effort and never alter the command outcome.
        /// </summary>
        private DbCommandResult RunWithAnomalyDetection(DbCommandSpec command, Func<DbCommandResult> exec)
        {
            if (_anomalyWriter == null || _anomalyOptions == null
                || _anomalyOptions.Level == DbAccessAnomalyLogLevel.None)
            {
                return exec();
            }

            var stopwatch = Stopwatch.StartNew();
            try
            {
                var result = exec();
                stopwatch.Stop();
                LogSuccessAnomalies(command, result, stopwatch.ElapsedMilliseconds);
                return result;
            }
            catch (DbException ex)
            {
                stopwatch.Stop();
                LogFailureAnomaly(command, ex, stopwatch.ElapsedMilliseconds);
                throw;
            }
        }

        private void LogSuccessAnomalies(DbCommandSpec command, DbCommandResult result, long elapsedMs)
        {
            // Slow / large-row are "abnormal but succeeded" — only recorded at Warning level.
            if (_anomalyOptions!.Level != DbAccessAnomalyLogLevel.Warning) { return; }

            int slowThresholdMs = _anomalyOptions.ExecutionTimeThreshold > 0
                ? _anomalyOptions.ExecutionTimeThreshold * 1000 : 0;
            if (slowThresholdMs > 0 && elapsedMs > slowThresholdMs)
                WriteDbAnomaly(command, AnomalyKind.Slow, elapsedMs, new DbAnomalyDetail { ThresholdMs = slowThresholdMs });

            if (_anomalyOptions.AffectedRowThreshold > 0 && result.RowsAffected > _anomalyOptions.AffectedRowThreshold)
                WriteDbAnomaly(command, AnomalyKind.LargeAffected, elapsedMs, new DbAnomalyDetail { AffectedRows = result.RowsAffected });

            int resultRows = result.Table?.Rows.Count ?? 0;
            if (_anomalyOptions.ResultRowThreshold > 0 && resultRows > _anomalyOptions.ResultRowThreshold)
                WriteDbAnomaly(command, AnomalyKind.LargeResult, elapsedMs, new DbAnomalyDetail { ResultRows = resultRows });
        }

        private void LogFailureAnomaly(DbCommandSpec command, DbException ex, long elapsedMs)
        {
            var kind = IsTimeout(ex, elapsedMs, command) ? AnomalyKind.Timeout : AnomalyKind.Error;
            WriteDbAnomaly(command, kind, elapsedMs,
                new DbAnomalyDetail { ErrorType = ex.GetType().Name, ErrorMessage = SanitizeMessage(ex.Message) });
        }

        private bool IsTimeout(DbException ex, long elapsedMs, DbCommandSpec command)
        {
            if (ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase)) { return true; }
            int timeoutSec = ResolveTimeout(command.CommandTimeout);
            return timeoutSec > 0 && elapsedMs >= (long)timeoutSec * 1000 * 9 / 10;
        }

        private void WriteDbAnomaly(DbCommandSpec command, AnomalyKind kind, long elapsedMs, DbAnomalyDetail detail = default)
        {
            _anomalyWriter!.Write(new DbAnomalyEntry
            {
                DatabaseId = _databaseId,
                Command = command.CommandText,   // {0} template only — never the parameter values
                Kind = kind,
                ElapsedMs = elapsedMs > int.MaxValue ? int.MaxValue : (int)elapsedMs,
                ThresholdMs = detail.ThresholdMs,
                AffectedRows = detail.AffectedRows,
                ResultRows = detail.ResultRows,
                ErrorType = detail.ErrorType,
                ErrorMessage = detail.ErrorMessage,
            });
        }

        /// <summary>
        /// Optional per-kind detail for a <see cref="DbAnomalyEntry"/>: each anomaly kind sets only the
        /// fields it carries (threshold for Slow, affected/result rows for the large-* kinds, error
        /// type/message for failures), leaving the rest null.
        /// </summary>
        private readonly struct DbAnomalyDetail
        {
            public int? ThresholdMs { get; init; }
            public int? AffectedRows { get; init; }
            public int? ResultRows { get; init; }
            public string? ErrorType { get; init; }
            public string? ErrorMessage { get; init; }
        }

        private static string SanitizeMessage(string message)
        {
            // Provider error text only (no stack trace, no parameter values); flattened and capped.
            var oneLine = message.Replace('\r', ' ').Replace('\n', ' ');
            return oneLine.Length <= MaxLoggedMessageLength ? oneLine : oneLine[..MaxLoggedMessageLength];
        }
    }
}
