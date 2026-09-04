using System.Data;
using Bee.Base;
using Bee.Db;
using Bee.Db.Manager;
using Bee.Definition.Database;
using Bee.Definition.Logging;
using Bee.Repository.Abstractions.AuditLog;

namespace Bee.Repository.AuditLog
{
    /// <summary>
    /// Reads a company's per-form audit rules from <c>st_audit_rule</c> (a company-database table).
    /// </summary>
    /// <remarks>
    /// Declares no scope: its table lives in a company database, but its caller is the cache data
    /// source, which is told which company to read and holds no token to route with. Routing by
    /// session here would read the caller's company instead of the requested one — the same
    /// reasoning as <see cref="Bee.Repository.System.DepartmentRepository"/>.
    /// </remarks>
    public class AuditRuleRepository : RepositoryBase, IAuditRuleRepository
    {
        private const string TableName = "st_audit_rule";

        /// <summary>
        /// Initializes a new <see cref="AuditRuleRepository"/>.
        /// </summary>
        /// <param name="ctx">The shared repository context.</param>
        /// <param name="accessToken">The current request's access token.</param>
        /// <param name="progId">Unused on the framework axis; accepted for signature uniformity.</param>
        public AuditRuleRepository(IRepositoryContext ctx, Guid accessToken, string progId)
            : base(ctx, accessToken, progId, null)
        {
        }

        /// <inheritdoc/>
        public IReadOnlyList<AuditRule> GetRules(string databaseId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(databaseId);

            var dbType = Context.ConnectionManager.GetConnectionInfo(databaseId).DatabaseType;

            // A deployment that predates this table must keep working on its deployment-wide
            // settings. Probing the schema rather than catching a provider exception keeps the
            // answer definitive: every provider words a missing table differently, and swallowing
            // exceptions here would hide a genuine connection failure as "no rules".
            var schemaProvider = DbDialectRegistry.Get(dbType)
                .CreateTableSchemaProvider(databaseId, Context.ConnectionManager);
            if (schemaProvider.GetTableSchema(TableName) == null)
            {
                return [];
            }

            string tbl = dbType.QuoteIdentifier(TableName);
            string colId = dbType.QuoteIdentifier("sys_id");
            string colChange = dbType.QuoteIdentifier("change_mode");
            string colAccess = dbType.QuoteIdentifier("access_mode");
            string colSensitive = dbType.QuoteIdentifier("is_sensitive");

            string sql = $"SELECT {colId}, {colChange}, {colAccess}, {colSensitive} FROM {tbl}";
            var dbAccess = CreateDbAccess(databaseId);
            var table = dbAccess.Execute(new DbCommandSpec(DbCommandKind.DataTable, sql)).Table!;

            var list = new List<AuditRule>(table.Rows.Count);
            foreach (DataRow row in table.Rows)
            {
                list.Add(new AuditRule(
                    ValueUtilities.CStr(row["sys_id"]),
                    (AuditRuleMode)ValueUtilities.CInt(row["change_mode"]),
                    (AuditRuleMode)ValueUtilities.CInt(row["access_mode"]),
                    ValueUtilities.CBool(row["is_sensitive"])));
            }
            return list;
        }

        /// <inheritdoc/>
        public void NotifyRulesChanged(string companyId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(companyId);
            if (Context.CacheNotify == null) { return; }

            var dbType = Context.ConnectionManager.GetConnectionInfo(DbCategoryIds.Common).DatabaseType;
            using var connection = Context.ConnectionManager.CreateConnection(DbCategoryIds.Common);
            connection.Open();
            using var transaction = connection.BeginTransaction();

            Context.CacheNotify.Touch(NotifyKey(companyId), transaction, dbType);

            transaction.Commit();
        }

        /// <summary>
        /// Builds the cache-notify key for a company's rule snapshot.
        /// </summary>
        /// <param name="companyId">The company business id.</param>
        /// <remarks>
        /// WARNING: must match the key the cached entry carries, which
        /// <c>CacheGroup</c> derives from the cached type's name. Renaming
        /// <see cref="CompanyAuditRules"/> silently breaks invalidation — the bump lands on a key
        /// nothing depends on, and every process keeps serving stale rules.
        /// </remarks>
        private static string NotifyKey(string companyId)
            => $"{nameof(CompanyAuditRules)}:{companyId}";
    }
}
