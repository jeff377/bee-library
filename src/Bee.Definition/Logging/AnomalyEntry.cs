namespace Bee.Definition.Logging
{
    /// <summary>
    /// Base type for an execution-anomaly entry — an execution that deviated from the normal
    /// envelope, whether it failed or merely ran outside a threshold. Subclasses add the columns
    /// specific to the layer that observed it (API method, or database and command).
    /// </summary>
    /// <remarks>
    /// Anomaly records are an operational signal (a bug to fix, a query to tune, a caller to
    /// investigate), not a business audit trail: they answer "which execution went wrong", not
    /// "who did what to which record". They share the audit tables' write pipeline, storage
    /// location and query entry point, which is why they derive from <see cref="AuditEntry"/> —
    /// but <see cref="IAnomalyLogWriter"/> is what a producer of these should depend on, so the
    /// layers that only observe anomalies do not take a dependency on the audit trail.
    /// </remarks>
    public abstract class AnomalyEntry : AuditEntry
    {
        /// <summary>Gets the anomaly classification.</summary>
        public AnomalyKind Kind { get; init; }

        /// <summary>Gets the elapsed time in milliseconds.</summary>
        public int ElapsedMs { get; init; }

        /// <summary>
        /// Gets the threshold that triggered the anomaly, if any — milliseconds for a
        /// <see cref="AnomalyKind.Slow"/> anomaly, a row count for the large-result kinds.
        /// </summary>
        public int? ThresholdMs { get; init; }

        /// <summary>Gets the exception type name for an Error / Timeout anomaly.</summary>
        public string? ErrorType { get; init; }

        /// <summary>Gets the sanitised error message (no stack trace, no internal paths).</summary>
        public string? ErrorMessage { get; init; }
    }
}
