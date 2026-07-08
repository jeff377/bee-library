using Bee.Definition.Logging;

namespace Bee.Repository.Abstractions.AuditLog
{
    /// <summary>
    /// Typed filter for a DB-anomaly (<c>st_log_anomaly_db</c>) list query. All fields optional,
    /// AND-combined, each mapping to an indexed column.
    /// </summary>
    /// <remarks>
    /// <c>st_log_anomaly_db</c> carries no acting user / company (the DB layer has no session context),
    /// so this is a cross-company infrastructure view — there is no company scope to filter on.
    /// </remarks>
    public sealed class DbAnomalyLogQuery
    {
        /// <summary>Gets the inclusive lower bound on <c>log_time</c> (UTC); null means no lower bound.</summary>
        public DateTime? FromUtc { get; init; }

        /// <summary>Gets the inclusive upper bound on <c>log_time</c> (UTC); null means no upper bound.</summary>
        public DateTime? ToUtc { get; init; }

        /// <summary>Gets the database id filter (which physical database deviated); null means any.</summary>
        public string? DatabaseId { get; init; }

        /// <summary>Gets the anomaly-kind filter (Error / Timeout / Slow / large-row); null means any kind.</summary>
        public AnomalyKind? Kind { get; init; }
    }
}
