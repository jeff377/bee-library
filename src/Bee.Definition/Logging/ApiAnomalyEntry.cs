namespace Bee.Definition.Logging
{
    /// <summary>
    /// Audit entry for an API-layer anomaly (row in <c>st_log_anomaly_api</c>). Records which action
    /// (<c>method</c>) hit an anomaly, from the caller's perspective — the API call has session
    /// context, so the common who / company columns apply.
    /// </summary>
    public sealed class ApiAnomalyEntry : AuditEntry
    {
        /// <inheritdoc/>
        public override string TableName => "st_log_anomaly_api";

        /// <summary>Gets the API method that hit the anomaly (<c>"ProgId.Action"</c>).</summary>
        public string Method { get; init; } = string.Empty;

        /// <summary>Gets the anomaly classification.</summary>
        public AnomalyKind Kind { get; init; }

        /// <summary>Gets the elapsed time in milliseconds.</summary>
        public int ElapsedMs { get; init; }

        /// <summary>Gets the threshold that triggered a <see cref="AnomalyKind.Slow"/> anomaly, if any.</summary>
        public int? ThresholdMs { get; init; }

        /// <summary>Gets the exception type name for an Error / Timeout anomaly.</summary>
        public string? ErrorType { get; init; }

        /// <summary>Gets the sanitised error message (no stack trace, no internal paths).</summary>
        public string? ErrorMessage { get; init; }

        /// <inheritdoc/>
        protected override void AddColumns(IList<AuditColumn> columns)
        {
            columns.Add(new AuditColumn("method", Method));
            columns.Add(new AuditColumn("anomaly_kind", (int)Kind));
            columns.Add(new AuditColumn("elapsed_ms", ElapsedMs));
            columns.Add(new AuditColumn("threshold_ms", ThresholdMs));
            columns.Add(new AuditColumn("error_type", ErrorType));
            columns.Add(new AuditColumn("error_message", ErrorMessage));
        }
    }
}
