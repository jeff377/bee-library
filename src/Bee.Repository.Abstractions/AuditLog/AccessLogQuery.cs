namespace Bee.Repository.Abstractions.AuditLog
{
    /// <summary>
    /// Typed filter for a record-view access-log (<c>st_log_access</c>) list query. All fields optional,
    /// AND-combined, each mapping to an indexed column.
    /// </summary>
    public sealed class AccessLogQuery
    {
        /// <summary>Gets the inclusive lower bound on <c>log_time</c> (UTC); null means no lower bound.</summary>
        public DateTime? FromUtc { get; init; }

        /// <summary>Gets the inclusive upper bound on <c>log_time</c> (UTC); null means no upper bound.</summary>
        public DateTime? ToUtc { get; init; }

        /// <summary>Gets the acting user's login id filter; null means any user.</summary>
        public string? UserId { get; init; }

        /// <summary>Gets the business object (program) id filter; null means any program.</summary>
        public string? ProgId { get; init; }

        /// <summary>Gets the viewed record key filter; null means any record.</summary>
        public string? RowKey { get; init; }

        /// <summary>Gets the company scope; when non-empty, restricts to that company's rows.</summary>
        public string? CompanyId { get; init; }
    }
}
