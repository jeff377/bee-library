using System.Data;
using System.Globalization;
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

        // Header column lists per axis — deliberately exclude any heavy payload (e.g. the change axis'
        // changes_xml). All are compile-time constants (no user input), safe to inline into SELECTs.
        private const string ChangeHeaderColumns =
            "sys_rowid, log_time, user_id, user_name, company_id, company_name, prog_id, row_key, change_kind, is_sensitive, source";

        private const string ChangeDetailColumns = ChangeHeaderColumns + ", changes_xml";

        private const string LoginHeaderColumns =
            "sys_rowid, log_time, user_id, user_name, company_id, company_name, client_ip, source, event, fail_reason";

        private const string AccessHeaderColumns =
            "sys_rowid, log_time, user_id, user_name, company_id, company_name, client_ip, source, prog_id, row_key";

        private const string ApiAnomalyHeaderColumns =
            "sys_rowid, log_time, user_id, user_name, company_id, company_name, client_ip, source, method, anomaly_kind, elapsed_ms, threshold_ms, error_type, error_message";

        private const string DbAnomalyHeaderColumns =
            "sys_rowid, log_time, database_id, command, anomaly_kind, elapsed_ms, threshold_ms, affected_rows, result_rows, error_type, error_message";

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
            var where = new WhereBuilder()
                .Eq("company_id", query.CompanyId)
                .Eq("prog_id", query.ProgId)
                .Eq("row_key", query.RowKey)
                .Eq("user_id", query.UserId)
                .Eq("change_kind", (int?)query.ChangeKind)
                .Gte("log_time", query.FromUtc)
                .Lte("log_time", query.ToUtc);
            return QueryPage("st_log_change", ChangeHeaderColumns, where, paging);
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

        /// <inheritdoc/>
        public AuditLogPage GetLoginLog(LoginLogQuery query, PagingOptions paging)
        {
            ArgumentNullException.ThrowIfNull(query);
            var where = new WhereBuilder()
                .Eq("company_id", query.CompanyId)
                .Eq("user_id", query.UserId)
                .Eq("event", (int?)query.Event)
                .Gte("log_time", query.FromUtc)
                .Lte("log_time", query.ToUtc);
            return QueryPage("st_log_login", LoginHeaderColumns, where, paging);
        }

        /// <inheritdoc/>
        public AuditLogPage GetAccessLog(AccessLogQuery query, PagingOptions paging)
        {
            ArgumentNullException.ThrowIfNull(query);
            var where = new WhereBuilder()
                .Eq("company_id", query.CompanyId)
                .Eq("prog_id", query.ProgId)
                .Eq("row_key", query.RowKey)
                .Eq("user_id", query.UserId)
                .Gte("log_time", query.FromUtc)
                .Lte("log_time", query.ToUtc);
            return QueryPage("st_log_access", AccessHeaderColumns, where, paging);
        }

        /// <inheritdoc/>
        public AuditLogPage GetApiAnomalyLog(ApiAnomalyLogQuery query, PagingOptions paging)
        {
            ArgumentNullException.ThrowIfNull(query);
            var where = new WhereBuilder()
                .Eq("company_id", query.CompanyId)
                .Eq("user_id", query.UserId)
                .Eq("method", query.Method)
                .Eq("anomaly_kind", (int?)query.Kind)
                .Gte("log_time", query.FromUtc)
                .Lte("log_time", query.ToUtc);
            return QueryPage("st_log_anomaly_api", ApiAnomalyHeaderColumns, where, paging);
        }

        /// <inheritdoc/>
        public AuditLogPage GetDbAnomalyLog(DbAnomalyLogQuery query, PagingOptions paging)
        {
            ArgumentNullException.ThrowIfNull(query);
            // st_log_anomaly_db carries no who / company (DbAccess has no session context), so this is a
            // cross-company infrastructure view — no company scope filter is possible or applied.
            var where = new WhereBuilder()
                .Eq("database_id", query.DatabaseId)
                .Eq("anomaly_kind", (int?)query.Kind)
                .Gte("log_time", query.FromUtc)
                .Lte("log_time", query.ToUtc);
            return QueryPage("st_log_anomaly_db", DbAnomalyHeaderColumns, where, paging);
        }

        /// <summary>The upper bound for a top-N aggregate request.</summary>
        private const int MaxTopN = 100;

        /// <inheritdoc/>
        public DataTable GetApiAnomalySummary(DateTime? fromUtc, DateTime? toUtc, string? companyId)
        {
            var where = new WhereBuilder()
                .Eq("company_id", companyId)
                .Gte("log_time", fromUtc)
                .Lte("log_time", toUtc);
            var (whereSql, values) = where.Build();
            string sql = "SELECT anomaly_kind, COUNT(*) AS event_count FROM st_log_anomaly_api" + whereSql +
                " GROUP BY anomaly_kind ORDER BY COUNT(*) DESC";
            return ExecuteQuery(sql, values);
        }

        /// <inheritdoc/>
        public DataTable GetDbAnomalySummary(DateTime? fromUtc, DateTime? toUtc)
        {
            // st_log_anomaly_db carries no company — a cross-company infrastructure summary.
            var where = new WhereBuilder()
                .Gte("log_time", fromUtc)
                .Lte("log_time", toUtc);
            var (whereSql, values) = where.Build();
            string sql = "SELECT anomaly_kind, COUNT(*) AS event_count FROM st_log_anomaly_db" + whereSql +
                " GROUP BY anomaly_kind ORDER BY COUNT(*) DESC";
            return ExecuteQuery(sql, values);
        }

        /// <inheritdoc/>
        public DataTable GetTopApiMethods(DateTime? fromUtc, DateTime? toUtc, int topN, string? companyId)
        {
            int take = Math.Clamp(topN, 1, MaxTopN);
            var dbType = _connectionManager.GetConnectionInfo(_databaseId).DatabaseType;
            var where = new WhereBuilder()
                .Eq("company_id", companyId)
                .Gte("log_time", fromUtc)
                .Lte("log_time", toUtc);
            var (whereSql, values) = where.Build();
            // ORDER BY the COUNT(*) expression (not the alias) so every dialect accepts it; the top-N is
            // the dialect LIMIT/FETCH, which requires the ORDER BY that is already present.
            string limit = new LimitBuilder(dbType).Build(null, take);
            string sql = "SELECT method, COUNT(*) AS event_count, MAX(elapsed_ms) AS max_elapsed_ms FROM st_log_anomaly_api" +
                whereSql + " GROUP BY method ORDER BY COUNT(*) DESC" + (limit.Length > 0 ? " " + limit : string.Empty);
            return ExecuteQuery(sql, values);
        }

        /// <summary>Executes a read-only DataTable query against the log database.</summary>
        private DataTable ExecuteQuery(string sql, object[] values)
        {
            var dbAccess = new DbAccess(_databaseId, _connectionManager);
            return dbAccess.Execute(new DbCommandSpec(DbCommandKind.DataTable, sql, values)).Table ?? new DataTable();
        }

        /// <summary>
        /// Runs a paged, <c>log_time DESC, sys_no DESC</c>-ordered SELECT of <paramref name="columns"/>
        /// from <paramref name="table"/> filtered by <paramref name="where"/>. Table and column names are
        /// compile-time constants; filter values bind through <c>{n}</c> placeholders. Paging reuses the
        /// dialect <see cref="LimitBuilder"/>; when the total count is not requested a probe row computes
        /// <c>HasMore</c> without an extra round-trip.
        /// </summary>
        private AuditLogPage QueryPage(string table, string columns, WhereBuilder where, PagingOptions paging)
        {
            ArgumentNullException.ThrowIfNull(paging);

            var dbType = _connectionManager.GetConnectionInfo(_databaseId).DatabaseType;
            var dbAccess = new DbAccess(_databaseId, _connectionManager);
            var (whereSql, values) = where.Build();

            int pageSize = Math.Clamp(paging.PageSize, 1, MaxPageSize);
            int page = Math.Max(paging.Page, 1);
            int skip = (page - 1) * pageSize;

            int? totalCount = null;
            if (paging.IncludeTotalCount)
            {
                var countSql = "SELECT COUNT(*) FROM " + table + whereSql;
                var scalar = dbAccess.Execute(new DbCommandSpec(DbCommandKind.Scalar, countSql, values)).Scalar;
                totalCount = Convert.ToInt32(scalar, CultureInfo.InvariantCulture);
            }

            // Probe one extra row when no COUNT is requested, so HasMore is known without a round-trip.
            // OFFSET/FETCH (SQL Server / Oracle) requires a deterministic ORDER BY; log_time DESC with the
            // auto-increment sys_no as tiebreak is stable even when timestamps collide.
            int take = paging.IncludeTotalCount ? pageSize : pageSize + 1;
            string limit = new LimitBuilder(dbType).Build(skip, take);
            string sql = "SELECT " + columns + " FROM " + table + whereSql +
                " ORDER BY log_time DESC, sys_no DESC" + (limit.Length > 0 ? " " + limit : string.Empty);
            var resultTable = dbAccess.Execute(new DbCommandSpec(DbCommandKind.DataTable, sql, values)).Table ?? new DataTable();

            bool hasMore;
            if (paging.IncludeTotalCount)
            {
                hasMore = totalCount > skip + resultTable.Rows.Count;
            }
            else
            {
                hasMore = resultTable.Rows.Count > pageSize;
                if (hasMore)
                {
                    // Trim the probe row so the caller never sees the extra record.
                    resultTable.Rows.RemoveAt(resultTable.Rows.Count - 1);
                }
            }

            return new AuditLogPage
            {
                Table = resultTable,
                Paging = new PagingInfo
                {
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    HasMore = hasMore,
                },
            };
        }

        /// <summary>
        /// Accumulates AND-combined SQL predicates over compile-time-constant column names, binding each
        /// value to a positional <c>{n}</c> placeholder so nothing is string-formatted into the SQL.
        /// Fluent <see cref="Eq(string, string)"/> / <see cref="Gte"/> / <see cref="Lte"/> skip null
        /// (or empty-string) values, so an unset filter simply adds no clause.
        /// </summary>
        private sealed class WhereBuilder
        {
            private readonly List<string> _clauses = [];
            private readonly List<object> _values = [];

            public WhereBuilder Eq(string column, string? value)
            {
                if (!string.IsNullOrEmpty(value)) { Add(column, "=", value); }
                return this;
            }

            public WhereBuilder Eq(string column, int? value)
            {
                if (value.HasValue) { Add(column, "=", value.Value); }
                return this;
            }

            public WhereBuilder Gte(string column, DateTime? value)
            {
                if (value.HasValue) { Add(column, ">=", value.Value); }
                return this;
            }

            public WhereBuilder Lte(string column, DateTime? value)
            {
                if (value.HasValue) { Add(column, "<=", value.Value); }
                return this;
            }

            private void Add(string column, string op, object value)
            {
                _clauses.Add(column + " " + op + " {" + _values.Count.ToString(CultureInfo.InvariantCulture) + "}");
                _values.Add(value);
            }

            public (string Where, object[] Values) Build()
                => _clauses.Count == 0
                    ? (string.Empty, Array.Empty<object>())
                    : (" WHERE " + string.Join(" AND ", _clauses), _values.ToArray());
        }
    }
}
