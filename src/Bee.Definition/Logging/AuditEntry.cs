namespace Bee.Definition.Logging
{
    /// <summary>
    /// Base type for a single audit-trail entry (one row in a <c>st_log_*</c> table). Subclasses
    /// declare their target table and axis-specific columns (login / change / exec / access); the
    /// common columns shared by every axis live here.
    /// </summary>
    /// <remarks>
    /// Log rows are self-sufficient: log tables are queried without joining other tables (the log
    /// database is physically separate from the common / company databases, so a cross-database
    /// join is not even possible). Identifiers are therefore stored denormalised — <c>user_id</c> +
    /// <c>user_name</c>, <c>company_id</c> + <c>company_name</c> — rather than a bare row id that
    /// would require a join to resolve.
    /// </remarks>
    public abstract class AuditEntry
    {
        /// <summary>Gets the unique row id. Defaults to a new <see cref="Guid"/>.</summary>
        public Guid SysRowId { get; init; } = Guid.NewGuid();

        /// <summary>
        /// Gets the event time in UTC. Defaults to the moment the entry is created, which for
        /// synchronous capture equals the event time.
        /// </summary>
        public DateTime LogTimeUtc { get; init; } = DateTime.UtcNow;

        /// <summary>
        /// Gets the acting user's login id (self-sufficient identifier). Retained even when the
        /// display name is unknown (for example a failed login), aligning with the SAP
        /// <c>USERNAME</c> convention.
        /// </summary>
        public string? UserId { get; init; }

        /// <summary>
        /// Gets the acting user's denormalised display name, so the log row is readable without
        /// joining the common-database <c>st_user</c> table.
        /// </summary>
        public string? UserName { get; init; }

        /// <summary>
        /// Gets the tenant code (self-sufficient). Null for pre-company events such as a login
        /// before <c>EnterCompany</c>.
        /// </summary>
        public string? CompanyId { get; init; }

        /// <summary>
        /// Gets the denormalised company display name, so the log row is readable without a
        /// cross-database join to the common-database <c>st_company</c> table.
        /// </summary>
        public string? CompanyName { get; init; }

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
        /// subclass-specific columns appended by <see cref="AddColumns"/>. The auto-increment
        /// <c>sys_no</c> is database-generated and intentionally omitted.
        /// </summary>
        /// <returns>The columns to insert, in a stable order.</returns>
        public IReadOnlyList<AuditColumn> GetColumns()
        {
            var columns = new List<AuditColumn>(16)
            {
                new("sys_rowid", SysRowId),
                new("log_time", LogTimeUtc),
                new("user_id", UserId),
                new("user_name", UserName),
                new("company_id", CompanyId),
                new("company_name", CompanyName),
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
