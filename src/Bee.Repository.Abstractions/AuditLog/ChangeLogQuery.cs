using Bee.Definition.Logging;

namespace Bee.Repository.Abstractions.AuditLog
{
    /// <summary>
    /// Typed filter for a change-log (<c>st_log_change</c>) list query. Every field is optional and
    /// AND-combined; each maps directly to an indexed column so the query stays parameterised and
    /// avoids full-table scans. A generic <c>FilterNode</c> is intentionally not used — the log tables
    /// are not FormSchema-driven, so there is no filter-to-SQL builder to reuse and an explicit column
    /// set keeps the query surface (and injection surface) closed.
    /// </summary>
    public sealed class ChangeLogQuery
    {
        /// <summary>Gets the inclusive lower bound on <c>log_time</c> (UTC); null means no lower bound.</summary>
        public DateTime? FromUtc { get; init; }

        /// <summary>Gets the inclusive upper bound on <c>log_time</c> (UTC); null means no upper bound.</summary>
        public DateTime? ToUtc { get; init; }

        /// <summary>Gets the acting user's login id filter; null means any user.</summary>
        public string? UserId { get; init; }

        /// <summary>Gets the business object (program) id filter; null means any program.</summary>
        public string? ProgId { get; init; }

        /// <summary>Gets the master record key filter; null means any record.</summary>
        public string? RowKey { get; init; }

        /// <summary>Gets the change-kind filter; null means any kind.</summary>
        public ChangeKind? ChangeKind { get; init; }

        /// <summary>
        /// Gets the company scope. When non-empty, restricts the result to rows whose denormalised
        /// <c>company_id</c> matches, so a caller only reads their own company's trail.
        /// </summary>
        public string? CompanyId { get; init; }
    }
}
