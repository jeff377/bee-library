namespace Bee.Definition.Logging
{
    /// <summary>
    /// Base type for a single audit-trail entry (one row in a <c>st_log_*</c> table). Subclasses
    /// declare their target table and axis-specific columns (login / change / exec / access); the
    /// common columns shared by every axis — who, when, where, and the request correlation id —
    /// live here.
    /// </summary>
    public abstract class AuditEntry
    {
        /// <summary>
        /// Gets the event time in UTC. Defaults to the moment the entry is created, which for
        /// synchronous capture equals the event time.
        /// </summary>
        public DateTime LogTimeUtc { get; init; } = DateTime.UtcNow;

        /// <summary>Gets the acting user's row id (common-database <c>st_user</c>), when known.</summary>
        public Guid? UserRowId { get; init; }

        /// <summary>
        /// Gets the acting user's login id. Retained even when <see cref="UserRowId"/> is unknown
        /// (for example a failed login attempt), aligning with the SAP <c>USERNAME</c> convention.
        /// </summary>
        public string? UserId { get; init; }

        /// <summary>
        /// Gets the tenant context. Null for pre-company events such as a login before
        /// <c>EnterCompany</c>; used to distinguish companies when all logs share one log database.
        /// </summary>
        public string? CompanyId { get; init; }

        /// <summary>Gets the session correlation token, when the event occurs within a session.</summary>
        public Guid? AccessToken { get; init; }

        /// <summary>
        /// Gets the correlation id shared by every entry produced within one API call, so an
        /// execution record and the change / access records it triggered can be traced together.
        /// </summary>
        public Guid? TraceId { get; init; }

        /// <summary>Gets the source IP when available; null for in-process local calls.</summary>
        public string? ClientIp { get; init; }

        /// <summary>Gets the origin marker, such as <c>"ProgId.Action"</c> or a channel name.</summary>
        public string? Source { get; init; }

        /// <summary>Gets the physical log table this entry is written to (e.g. <c>st_log_login</c>).</summary>
        public abstract string TableName { get; }

        /// <summary>
        /// Builds the ordered column list for the INSERT: the common columns first, then the
        /// subclass-specific columns appended by <see cref="AddColumns"/>.
        /// </summary>
        /// <returns>The columns to insert, in a stable order.</returns>
        public IReadOnlyList<AuditColumn> GetColumns()
        {
            var columns = new List<AuditColumn>(16)
            {
                new("log_time", LogTimeUtc),
                new("user_rowid", UserRowId),
                new("user_id", UserId),
                new("company_id", CompanyId),
                new("access_token", AccessToken),
                new("trace_id", TraceId),
                new("client_ip", ClientIp),
                new("source", Source),
            };
            AddColumns(columns);
            return columns;
        }

        /// <summary>
        /// Appends the subclass-specific columns to <paramref name="columns"/>. Called after the
        /// common columns have been added.
        /// </summary>
        /// <param name="columns">The column list to append to.</param>
        protected abstract void AddColumns(IList<AuditColumn> columns);
    }
}
