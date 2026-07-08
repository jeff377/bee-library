namespace Bee.Definition.Logging
{
    /// <summary>
    /// Audit entry for a DB-layer anomaly (row in <c>st_log_anomaly_db</c>). Records which database
    /// and command hit an anomaly, from the technical perspective. <c>DbAccess</c> has no
    /// session context, so the common who / company columns are intentionally omitted.
    /// </summary>
    public sealed class DbAnomalyEntry : AuditEntry
    {
        /// <inheritdoc/>
        public override string TableName => "st_log_anomaly_db";

        /// <summary>Gets the database identifier the command ran against.</summary>
        public string DatabaseId { get; init; } = string.Empty;

        /// <summary>
        /// Gets the SQL command template (<c>{0}</c> placeholders, never the parameter values).
        /// </summary>
        public string Command { get; init; } = string.Empty;

        /// <summary>Gets the anomaly classification.</summary>
        public AnomalyKind Kind { get; init; }

        /// <summary>Gets the elapsed time in milliseconds.</summary>
        public int ElapsedMs { get; init; }

        /// <summary>Gets the threshold (ms for Slow) that triggered the anomaly, if any.</summary>
        public int? ThresholdMs { get; init; }

        /// <summary>Gets the affected row count (for a <see cref="AnomalyKind.LargeAffected"/> anomaly).</summary>
        public int? AffectedRows { get; init; }

        /// <summary>Gets the returned row count (for a <see cref="AnomalyKind.LargeResult"/> anomaly).</summary>
        public int? ResultRows { get; init; }

        /// <summary>Gets the exception type name for an Error / Timeout anomaly.</summary>
        public string? ErrorType { get; init; }

        /// <summary>Gets the sanitised error message (no stack trace, no internal paths).</summary>
        public string? ErrorMessage { get; init; }

        /// <summary>Omits the common who / company columns — a DB command has no session context.</summary>
        protected override void AddCommonColumns(IList<AuditColumn> columns)
        {
            // Intentionally empty: the DB anomaly perspective is database_id + command, not who.
        }

        /// <inheritdoc/>
        protected override void AddColumns(IList<AuditColumn> columns)
        {
            columns.Add(new AuditColumn("database_id", DatabaseId));
            columns.Add(new AuditColumn("command", Command));
            columns.Add(new AuditColumn("anomaly_kind", (int)Kind));
            columns.Add(new AuditColumn("elapsed_ms", ElapsedMs));
            columns.Add(new AuditColumn("threshold_ms", ThresholdMs));
            columns.Add(new AuditColumn("affected_rows", AffectedRows));
            columns.Add(new AuditColumn("result_rows", ResultRows));
            columns.Add(new AuditColumn("error_type", ErrorType));
            columns.Add(new AuditColumn("error_message", ErrorMessage));
        }
    }
}
