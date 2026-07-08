using System.Data;
using Bee.Db;
using Bee.Db.Manager;
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

        // Column list is a compile-time constant — no user input — so it is safe to inline into the
        // command text. Row values bind through the {0}/{1}/{2} placeholders, never string-formatted.
        private const string ChangeColumns =
            "sys_rowid, log_time, user_id, user_name, change_kind, is_sensitive, source, changes_xml";

        private const string ChangeHistorySql =
            "SELECT " + ChangeColumns + " FROM st_log_change " +
            "WHERE prog_id = {0} AND row_key = {1} ORDER BY log_time DESC";

        private const string ChangeHistoryByCompanySql =
            "SELECT " + ChangeColumns + " FROM st_log_change " +
            "WHERE prog_id = {0} AND row_key = {1} AND company_id = {2} ORDER BY log_time DESC";

        /// <inheritdoc/>
        public DataTable GetRecordChangeHistory(string progId, string rowKey, string? companyId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(progId);
            ArgumentException.ThrowIfNullOrWhiteSpace(rowKey);

            var dbAccess = new DbAccess(_databaseId, _connectionManager);
            var spec = string.IsNullOrEmpty(companyId)
                ? new DbCommandSpec(DbCommandKind.DataTable, ChangeHistorySql, progId, rowKey)
                : new DbCommandSpec(DbCommandKind.DataTable, ChangeHistoryByCompanySql, progId, rowKey, companyId);

            var result = dbAccess.Execute(spec);
            return result.Table ?? new DataTable();
        }
    }
}
