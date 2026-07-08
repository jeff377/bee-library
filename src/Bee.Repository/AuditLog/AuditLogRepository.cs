using System.Data;
using System.Globalization;
using System.Text;
using Bee.Db;
using Bee.Db.Dml;
using Bee.Db.Manager;
using Bee.Definition.Paging;
using Bee.Repository.Abstractions.AuditLog;

namespace Bee.Repository.AuditLog
{
    /// <summary>
    /// Default <see cref="IAuditLogRepository"/>. Runs parameterised, read-only SELECTs against the
    /// log database via <see cref="DbAccess"/>. Table and column names match the write-side sink
    /// (unquoted lower-case snake_case) so reads line up with writes across every provider.
    /// </summary>
    public class AuditLogRepository : IAuditLogRepository
    {
        /// <summary>The framework-wide upper bound for <see cref="PagingOptions.PageSize"/>.</summary>
        private const int MaxPageSize = 1000;

        // Header columns for list queries — deliberately excludes changes_xml (heavy; fetched per row
        // via GetChangeById). Compile-time constant, no user input, safe to inline.
        private const string ChangeHeaderColumns =
            "sys_rowid, log_time, user_id, user_name, company_id, company_name, prog_id, row_key, change_kind, is_sensitive, source";

        // Detail columns — header plus the raw DiffGram payload for a single-row fetch.
        private const string ChangeDetailColumns = ChangeHeaderColumns + ", changes_xml";

        private readonly IDbConnectionManager _connectionManager;
        private readonly string _databaseId;

        /// <summary>
        /// Initializes a new <see cref="AuditLogRepository"/>.
        /// </summary>
        /// <param name="connectionManager">The DI-resolved connection manager.</param>
        /// <param name="databaseId">
        /// The log database id to target (production resolves this to <c>DbCategoryIds.Log</c>; tests
        /// pass a per-dialect id such as <c>log_sqlserver</c>).
        /// </param>
        public AuditLogRepository(IDbConnectionManager connectionManager, string databaseId)
        {
            _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
            ArgumentException.ThrowIfNullOrWhiteSpace(databaseId);
            _databaseId = databaseId;
        }

        /// <inheritdoc/>
        public AuditLogPage GetChangeLog(ChangeLogQuery query, PagingOptions paging)
        {
            ArgumentNullException.ThrowIfNull(query);
            ArgumentNullException.ThrowIfNull(paging);

            var dbType = _connectionManager.GetConnectionInfo(_databaseId).DatabaseType;
            var dbAccess = new DbAccess(_databaseId, _connectionManager);
            var (where, values) = BuildChangeWhere(query);

            int pageSize = Math.Clamp(paging.PageSize, 1, MaxPageSize);
            int page = Math.Max(paging.Page, 1);
            int skip = (page - 1) * pageSize;

            int? totalCount = null;
            if (paging.IncludeTotalCount)
            {
                var countSql = "SELECT COUNT(*) FROM st_log_change" + where;
                var scalar = dbAccess.Execute(new DbCommandSpec(DbCommandKind.Scalar, countSql, values)).Scalar;
                totalCount = Convert.ToInt32(scalar, CultureInfo.InvariantCulture);
            }

            // Probe one extra row when no COUNT is requested, so HasMore is known without a round-trip.
            int take = paging.IncludeTotalCount ? pageSize : pageSize + 1;
            // OFFSET/FETCH (SQL Server / Oracle) requires a deterministic ORDER BY; log_time DESC with
            // the auto-increment sys_no as tiebreak is stable even when timestamps collide.
            string limit = new LimitBuilder(dbType).Build(skip, take);
            string sql = "SELECT " + ChangeHeaderColumns + " FROM st_log_change" + where +
                " ORDER BY log_time DESC, sys_no DESC" + (limit.Length > 0 ? " " + limit : string.Empty);
            var table = dbAccess.Execute(new DbCommandSpec(DbCommandKind.DataTable, sql, values)).Table ?? new DataTable();

            bool hasMore;
            if (paging.IncludeTotalCount)
            {
                hasMore = totalCount > skip + table.Rows.Count;
            }
            else
            {
                hasMore = table.Rows.Count > pageSize;
                if (hasMore)
                {
                    // Trim the probe row so the caller never sees the extra record.
                    table.Rows.RemoveAt(table.Rows.Count - 1);
                }
            }

            return new AuditLogPage
            {
                Table = table,
                Paging = new PagingInfo
                {
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    HasMore = hasMore,
                },
            };
        }

        /// <inheritdoc/>
        public DataTable? GetChangeById(Guid sysRowId, string? companyId)
        {
            var dbAccess = new DbAccess(_databaseId, _connectionManager);
            var spec = string.IsNullOrEmpty(companyId)
                ? new DbCommandSpec(DbCommandKind.DataTable,
                    "SELECT " + ChangeDetailColumns + " FROM st_log_change WHERE sys_rowid = {0}", sysRowId)
                : new DbCommandSpec(DbCommandKind.DataTable,
                    "SELECT " + ChangeDetailColumns + " FROM st_log_change WHERE sys_rowid = {0} AND company_id = {1}", sysRowId, companyId);

            var table = dbAccess.Execute(spec).Table;
            return table != null && table.Rows.Count > 0 ? table : null;
        }

        /// <summary>
        /// Builds the parameterised WHERE clause for a change-log query. Column names and operators are
        /// compile-time constants; every value binds through a positional <c>{n}</c> placeholder, never
        /// string-formatted into the SQL.
        /// </summary>
        private static (string Where, object[] Values) BuildChangeWhere(ChangeLogQuery query)
        {
            var clauses = new List<string>();
            var values = new List<object>();

            void Add(string columnAndOperator, object value)
            {
                clauses.Add(columnAndOperator + " {" + values.Count.ToString(CultureInfo.InvariantCulture) + "}");
                values.Add(value);
            }

            if (!string.IsNullOrEmpty(query.CompanyId)) { Add("company_id =", query.CompanyId); }
            if (!string.IsNullOrEmpty(query.ProgId)) { Add("prog_id =", query.ProgId); }
            if (!string.IsNullOrEmpty(query.RowKey)) { Add("row_key =", query.RowKey); }
            if (!string.IsNullOrEmpty(query.UserId)) { Add("user_id =", query.UserId); }
            if (query.ChangeKind.HasValue) { Add("change_kind =", (int)query.ChangeKind.Value); }
            if (query.FromUtc.HasValue) { Add("log_time >=", query.FromUtc.Value); }
            if (query.ToUtc.HasValue) { Add("log_time <=", query.ToUtc.Value); }

            if (clauses.Count == 0) { return (string.Empty, Array.Empty<object>()); }

            var sb = new StringBuilder(" WHERE ");
            sb.Append(string.Join(" AND ", clauses));
            return (sb.ToString(), values.ToArray());
        }
    }
}
