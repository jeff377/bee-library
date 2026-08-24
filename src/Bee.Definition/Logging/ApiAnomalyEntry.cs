namespace Bee.Definition.Logging
{
    /// <summary>
    /// Anomaly entry for the API layer (row in <c>st_log_anomaly_api</c>). Records which action
    /// (<c>method</c>) hit an anomaly, from the caller's perspective — the API call has session
    /// context, so the common who / company columns apply.
    /// </summary>
    public sealed class ApiAnomalyEntry : AnomalyEntry
    {
        /// <inheritdoc/>
        public override string TableName => "st_log_anomaly_api";

        /// <summary>Gets the API method that hit the anomaly (<c>"ProgId.Action"</c>).</summary>
        public string Method { get; init; } = string.Empty;

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
