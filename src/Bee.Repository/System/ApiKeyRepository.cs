using Bee.Base;
using Bee.Base.Data;
using Bee.Db;
using Bee.Db.Manager;
using Bee.Definition;
using Bee.Definition.Database;
using Bee.Definition.Security;
using Bee.Repository.Abstractions.System;

namespace Bee.Repository.System
{
    /// <summary>
    /// Data access object for issued API keys on the <c>st_api_key</c> table (common database).
    /// </summary>
    /// <remarks>
    /// Disabled keys are excluded at the query layer — to callers they look exactly like keys that
    /// never existed, which is what keeps the API from reporting which identifiers are real.
    /// </remarks>
    public class ApiKeyRepository : RepositoryBase, IApiKeyRepository
    {
        private const string TableName = "st_api_key";
        private const string SysIdColumn = "sys_id";
        private const string SysNameColumn = "sys_name";
        private const string KeyTypeColumn = "key_type";
        private const string ContactColumn = "contact";
        private const string ExpiredAtColumn = "expired_at";
        private const string EnabledColumn = "enabled";

        /// <summary>
        /// Initializes a new <see cref="ApiKeyRepository"/>.
        /// </summary>
        /// <param name="ctx">
        /// The shared repository context. Its <see cref="IRepositoryContext.CacheNotify"/> is the
        /// cross-process invalidation channel this repository announces writes on; <c>null</c> means
        /// other processes pick a change up when their cached entry lapses.
        /// </param>
        /// <param name="accessToken">The current request's access token.</param>
        /// <param name="progId">Unused on the framework axis; accepted for signature uniformity.</param>
        public ApiKeyRepository(IRepositoryContext ctx, Guid accessToken, string progId)
            : base(ctx, accessToken, progId, DbScope.Common)
        {
        }

        /// <inheritdoc/>
        public ApiKeyInfo? GetEnabledById(string sysId)
        {
            if (StringUtilities.IsEmpty(sysId)) { return null; }

            var dbType = Context.ConnectionManager.GetConnectionInfo(DbCategoryIds.Common).DatabaseType;
            string tbl = dbType.QuoteIdentifier(TableName);
            string colId = dbType.QuoteIdentifier(SysIdColumn);
            string colName = dbType.QuoteIdentifier(SysNameColumn);
            string colHashed = dbType.QuoteIdentifier("hashed_key");
            string colKeyType = dbType.QuoteIdentifier(KeyTypeColumn);
            string colContact = dbType.QuoteIdentifier(ContactColumn);
            string colExpired = dbType.QuoteIdentifier(ExpiredAtColumn);
            string colEnabled = dbType.QuoteIdentifier(EnabledColumn);

            string sql = $"SELECT {colId}, {colName}, {colHashed}, {colKeyType}, {colContact}, {colExpired} \n" +
                         $"FROM {tbl} \n" +
                         $"WHERE {colId} = {{0}} AND {colEnabled} = {{1}}";
            var command = new DbCommandSpec(DbCommandKind.DataTable, sql, sysId, true);
            var dbAccess = CreateDbAccess();
            var result = dbAccess.Execute(command);
            var table = result.Table!;
            if (table.IsEmpty()) { return null; }

            var row = table.Rows[0];
            object expiredAt = row[ExpiredAtColumn];
            return new ApiKeyInfo
            {
                SysId = ValueUtilities.CStr(row[SysIdColumn]),
                SysName = ValueUtilities.CStr(row[SysNameColumn]),
                HashedKey = ValueUtilities.CStr(row["hashed_key"]),
                KeyType = (ApiKeyType)ValueUtilities.CInt(row[KeyTypeColumn], (int)ApiKeyType.Internal),
                Contact = ValueUtilities.CStr(row[ContactColumn]),
                // `expired_at` is a naive column holding UTC (ADR-032 D1), matching the
                // `DateTime.UtcNow` the validator compares it against.
                ExpiredAt = expiredAt is DateTime dt ? dt : null,
            };
        }

        /// <inheritdoc/>
        /// <remarks>
        /// Table presence is asked of the schema provider rather than inferred from a failed query.
        /// That distinction is the whole compatibility gate: an absent table is a definitive
        /// "this deployment has not issued keys yet" and relaxes the check, whereas a query that
        /// throws is an outage and must not.
        /// </remarks>
        public ApiKeyGateState GetGateState()
        {
            var connInfo = Context.ConnectionManager.GetConnectionInfo(DbCategoryIds.Common);
            var schemaProvider = DbDialectRegistry.Get(connInfo.DatabaseType)
                .CreateTableSchemaProvider(DbCategoryIds.Common, Context.ConnectionManager);
            if (schemaProvider.GetTableSchema(TableName) == null)
            {
                return new ApiKeyGateState { InForce = false };
            }

            return new ApiKeyGateState { InForce = CountEnabled() > 0 };
        }

        /// <inheritdoc/>
        public bool Exists(string sysId)
        {
            if (StringUtilities.IsEmpty(sysId)) { return false; }

            var dbType = Context.ConnectionManager.GetConnectionInfo(DbCategoryIds.Common).DatabaseType;
            string tbl = dbType.QuoteIdentifier(TableName);
            string colId = dbType.QuoteIdentifier(SysIdColumn);

            string sql = $"SELECT COUNT(*) FROM {tbl} WHERE {colId} = {{0}}";
            var command = new DbCommandSpec(DbCommandKind.Scalar, sql, sysId);
            var dbAccess = CreateDbAccess();
            return ValueUtilities.CInt(dbAccess.Execute(command).Scalar!) > 0;
        }

        /// <inheritdoc/>
        /// <remarks>
        /// The insert and the two cache-notify bumps share one transaction, so the announcement can
        /// never be visible without the row (or survive a rolled-back write). Both keys matter: the
        /// key's own entry may hold a cached miss from an earlier probe, and the gate entry is what
        /// tells other processes the gate has just come into force. Missing the gate bump is the
        /// bootstrap trap — the first key gets issued and the deployment keeps accepting any
        /// non-empty key until the entry lapses.
        /// </remarks>
        public void Insert(ApiKeyInfo apiKey)
        {
            ArgumentNullException.ThrowIfNull(apiKey);

            var dbType = Context.ConnectionManager.GetConnectionInfo(DbCategoryIds.Common).DatabaseType;
            string tbl = dbType.QuoteIdentifier(TableName);
            string colRowId = dbType.QuoteIdentifier("sys_rowid");
            string colId = dbType.QuoteIdentifier(SysIdColumn);
            string colName = dbType.QuoteIdentifier(SysNameColumn);
            string colHashed = dbType.QuoteIdentifier("hashed_key");
            string colKeyType = dbType.QuoteIdentifier(KeyTypeColumn);
            string colContact = dbType.QuoteIdentifier(ContactColumn);
            string colEnabled = dbType.QuoteIdentifier(EnabledColumn);
            string colExpired = dbType.QuoteIdentifier(ExpiredAtColumn);
            string colInsert = dbType.QuoteIdentifier("sys_insert_time");

            string sql = $"INSERT INTO {tbl} \n" +
                         $"({colRowId}, {colId}, {colName}, {colHashed}, {colKeyType}, {colContact}, {colEnabled}, {colExpired}, {colInsert}) \n" +
                         "VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8})";
            // `DBNull.Value` rather than a null element, matching AuditLogWriteRepository: the
            // parameter array is not nullable-annotated, and this is the form already exercised
            // against every dialect for nullable columns.
            object expiredAt = apiKey.ExpiredAt.HasValue ? apiKey.ExpiredAt.Value : DBNull.Value;
            var command = new DbCommandSpec(DbCommandKind.NonQuery, sql,
                Guid.NewGuid(), apiKey.SysId, apiKey.SysName, apiKey.HashedKey, (int)apiKey.KeyType,
                apiKey.Contact, true, expiredAt, DateTime.UtcNow);

            using var connection = Context.ConnectionManager.CreateConnection(DbCategoryIds.Common);
            connection.Open();
            using var transaction = connection.BeginTransaction();

            new DbAccess(connection, dbType).Execute(command, transaction);
            if (Context.CacheNotify != null)
            {
                Context.CacheNotify.Touch(NotifyKey(apiKey.SysId), transaction, dbType);
                Context.CacheNotify.Touch(NotifyKey(ApiKeyGateState.CacheKey), transaction, dbType);
            }

            transaction.Commit();
        }

        /// <inheritdoc/>
        public IReadOnlyList<ApiKeySummary> GetList()
        {
            var dbType = Context.ConnectionManager.GetConnectionInfo(DbCategoryIds.Common).DatabaseType;
            string tbl = dbType.QuoteIdentifier(TableName);
            string colId = dbType.QuoteIdentifier(SysIdColumn);
            string colName = dbType.QuoteIdentifier(SysNameColumn);
            string colKeyType = dbType.QuoteIdentifier(KeyTypeColumn);
            string colContact = dbType.QuoteIdentifier(ContactColumn);
            string colEnabled = dbType.QuoteIdentifier(EnabledColumn);
            string colExpired = dbType.QuoteIdentifier(ExpiredAtColumn);
            string colInsert = dbType.QuoteIdentifier("sys_insert_time");

            // `hashed_key` is deliberately absent from the projection, not merely unmapped: the
            // credential hash never travels beyond the validation path.
            string sql = $"SELECT {colId}, {colName}, {colKeyType}, {colContact}, {colEnabled}, {colExpired}, {colInsert} \n" +
                         $"FROM {tbl} \n" +
                         $"ORDER BY {colId}";
            var command = new DbCommandSpec(DbCommandKind.DataTable, sql);
            var result = CreateDbAccess().Execute(command);

            var list = new List<ApiKeySummary>();
            foreach (global::System.Data.DataRow row in result.Table!.Rows)
            {
                object expiredAt = row[ExpiredAtColumn];
                object issuedAt = row["sys_insert_time"];
                list.Add(new ApiKeySummary
                {
                    SysId = ValueUtilities.CStr(row[SysIdColumn]),
                    SysName = ValueUtilities.CStr(row[SysNameColumn]),
                    KeyType = (ApiKeyType)ValueUtilities.CInt(row[KeyTypeColumn], (int)ApiKeyType.Internal),
                    Contact = ValueUtilities.CStr(row[ContactColumn]),
                    Enabled = ValueUtilities.CBool(row[EnabledColumn]),
                    ExpiredAt = expiredAt is DateTime expiry ? expiry : null,
                    IssuedAt = issuedAt is DateTime issued ? issued : null,
                });
            }
            return list;
        }

        /// <inheritdoc/>
        public bool SetEnabled(string sysId, bool enabled)
        {
            return UpdateColumn(sysId, EnabledColumn, enabled);
        }

        /// <inheritdoc/>
        public bool SetExpiry(string sysId, DateTime? expiredAt)
        {
            return UpdateColumn(sysId, ExpiredAtColumn, expiredAt.HasValue ? expiredAt.Value : DBNull.Value);
        }

        /// <summary>
        /// Updates one column of one key row and announces the change, in a single transaction.
        /// </summary>
        /// <param name="sysId">The key identifier.</param>
        /// <param name="columnName">The column to write.</param>
        /// <param name="value">The value to store.</param>
        /// <returns><c>true</c> when a row was updated; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// WARNING: the update and both cache-notify bumps share one transaction, exactly as in
        /// <see cref="Insert"/>. The gate bump is not optional even for a single-key change —
        /// disabling the last enabled key takes the gate out of force, and a deployment left
        /// believing the gate is still closed would keep rejecting callers it should now accept
        /// (and the reverse when re-enabling).
        /// </remarks>
        private bool UpdateColumn(string sysId, string columnName, object value)
        {
            if (StringUtilities.IsEmpty(sysId)) { return false; }

            var dbType = Context.ConnectionManager.GetConnectionInfo(DbCategoryIds.Common).DatabaseType;
            string tbl = dbType.QuoteIdentifier(TableName);
            string colId = dbType.QuoteIdentifier(SysIdColumn);
            string col = dbType.QuoteIdentifier(columnName);

            string sql = $"UPDATE {tbl} SET {col} = {{0}} WHERE {colId} = {{1}}";
            var command = new DbCommandSpec(DbCommandKind.NonQuery, sql, value, sysId);

            using var connection = Context.ConnectionManager.CreateConnection(DbCategoryIds.Common);
            connection.Open();
            using var transaction = connection.BeginTransaction();

            var dbAccess = new DbAccess(connection, dbType);
            if (dbAccess.Execute(command, transaction).RowsAffected == 0)
            {
                transaction.Rollback();
                return false;
            }

            if (Context.CacheNotify != null)
            {
                Context.CacheNotify.Touch(NotifyKey(sysId), transaction, dbType);
                Context.CacheNotify.Touch(NotifyKey(ApiKeyGateState.CacheKey), transaction, dbType);
            }

            transaction.Commit();
            return true;
        }

        /// <summary>
        /// Builds the cache-notify key for an API key entry.
        /// </summary>
        /// <param name="key">The key identifier, or <see cref="ApiKeyGateState.CacheKey"/> for the gate.</param>
        /// <remarks>
        /// WARNING: the group must stay <see cref="ApiKeyInfo"/> for both. It is the group the key cache
        /// derives its notify keys from, and the gate cache overrides its own group to match — that
        /// shared group is what lets one write invalidate both.
        /// </remarks>
        private static string NotifyKey(string key)
        {
            return nameof(ApiKeyInfo) + ":" + key;
        }

        /// <summary>
        /// Counts the enabled key rows.
        /// </summary>
        private int CountEnabled()
        {
            var dbType = Context.ConnectionManager.GetConnectionInfo(DbCategoryIds.Common).DatabaseType;
            string tbl = dbType.QuoteIdentifier(TableName);
            string colEnabled = dbType.QuoteIdentifier(EnabledColumn);

            string sql = $"SELECT COUNT(*) FROM {tbl} WHERE {colEnabled} = {{0}}";
            var command = new DbCommandSpec(DbCommandKind.Scalar, sql, true);
            var dbAccess = CreateDbAccess();
            return ValueUtilities.CInt(dbAccess.Execute(command).Scalar!);
        }
    }
}
