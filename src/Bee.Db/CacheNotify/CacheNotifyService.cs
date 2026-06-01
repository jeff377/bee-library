using System.Data.Common;
using Bee.Base.Data;
using Bee.Db.Manager;
using Bee.Definition.Database;

namespace Bee.Db.CacheNotify
{
    /// <summary>
    /// Default <see cref="ICacheNotifyService"/> implementation. Issues a single atomic UPSERT
    /// against <c>st_cache_notify</c> using the target dialect's native upsert construct
    /// (<c>ON CONFLICT</c> / <c>ON DUPLICATE KEY</c> / <c>MERGE</c>), so the version increment is
    /// evaluated under a row lock and concurrent touches of the same key cannot lose updates.
    /// </summary>
    /// <remarks>
    /// Stateless: every call builds its statement from the supplied <see cref="DatabaseType"/> and
    /// runs it on the caller's <see cref="DbTransaction"/>. The server-time expression for
    /// <c>sys_update_time</c> is sourced from the registered dialect factory so it stays in lock-step
    /// with the column's CREATE-TABLE default (e.g. <c>getdate()</c>, <c>SYSTIMESTAMP</c>).
    /// </remarks>
    public sealed class CacheNotifyService : ICacheNotifyService
    {
        private const string TableName = "st_cache_notify";
        private const string KeyColumn = "cache_key";
        private const string VersionColumn = "cache_version";
        private const string UpdateTimeColumn = "sys_update_time";

        /// <inheritdoc/>
        public void Touch(string cacheKey, DbTransaction transaction, DatabaseType databaseType)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(cacheKey);
            ArgumentNullException.ThrowIfNull(transaction);

            var spec = BuildUpsertSpec(cacheKey, databaseType);
            CreateDbAccess(transaction, databaseType).Execute(spec, transaction);
        }

        /// <inheritdoc/>
        public Task TouchAsync(string cacheKey, DbTransaction transaction, DatabaseType databaseType,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(cacheKey);
            ArgumentNullException.ThrowIfNull(transaction);

            var spec = BuildUpsertSpec(cacheKey, databaseType);
            return CreateDbAccess(transaction, databaseType).ExecuteAsync(spec, transaction, cancellationToken);
        }

        private static DbAccess CreateDbAccess(DbTransaction transaction, DatabaseType databaseType)
        {
            var connection = transaction.Connection
                ?? throw new InvalidOperationException("Transaction has no associated connection.");
            return new DbAccess(connection, databaseType);
        }

        /// <summary>
        /// Builds the dialect-specific UPSERT command. The version increment is computed by the
        /// database (existing value + 1) rather than read-then-written by the application, so the
        /// row lock acquired by the statement serializes concurrent bumps.
        /// </summary>
        private static DbCommandSpec BuildUpsertSpec(string cacheKey, DatabaseType databaseType)
        {
            // Server-time expression for sys_update_time, taken from the same dialect source the
            // CREATE TABLE default uses, so a touched row's time matches a freshly inserted one.
            string now = DbDialectRegistry.Get(databaseType).GetDefaultValueExpression(FieldDbType.DateTime);

            string key = databaseType.QuoteIdentifier(KeyColumn);
            string ver = databaseType.QuoteIdentifier(VersionColumn);
            string upd = databaseType.QuoteIdentifier(UpdateTimeColumn);
            string tbl = databaseType.QuoteIdentifier(TableName);

            string commandText = databaseType switch
            {
                // PostgreSQL / SQLite share the same INSERT ... ON CONFLICT upsert; unqualified
                // column names in the DO UPDATE SET clause refer to the existing target row.
                DatabaseType.PostgreSQL or DatabaseType.SQLite =>
                    $"INSERT INTO {tbl} ({key}, {ver}, {upd}) VALUES ({{0}}, 1, {now}) " +
                    $"ON CONFLICT ({key}) DO UPDATE SET {ver} = {tbl}.{ver} + 1, {upd} = {now}",

                DatabaseType.MySQL =>
                    $"INSERT INTO {tbl} ({key}, {ver}, {upd}) VALUES ({{0}}, 1, {now}) " +
                    $"ON DUPLICATE KEY UPDATE {ver} = {ver} + 1, {upd} = {now}",

                // HOLDLOCK closes the MERGE upsert race (a concurrent insert of the same new key);
                // the statement must terminate with a semicolon.
                DatabaseType.SQLServer =>
                    $"MERGE {tbl} WITH (HOLDLOCK) AS t USING (VALUES ({{0}})) AS s ({key}) ON t.{key} = s.{key} " +
                    $"WHEN MATCHED THEN UPDATE SET t.{ver} = t.{ver} + 1, t.{upd} = {now} " +
                    $"WHEN NOT MATCHED THEN INSERT ({key}, {ver}, {upd}) VALUES (s.{key}, 1, {now});",

                DatabaseType.Oracle =>
                    $"MERGE INTO {tbl} t USING (SELECT {{0}} AS {key} FROM dual) s ON (t.{key} = s.{key}) " +
                    $"WHEN MATCHED THEN UPDATE SET t.{ver} = t.{ver} + 1, t.{upd} = {now} " +
                    $"WHEN NOT MATCHED THEN INSERT ({key}, {ver}, {upd}) VALUES (s.{key}, 1, {now})",

                _ => throw new NotSupportedException($"Cache-notify upsert is not defined for {databaseType}.")
            };

            return new DbCommandSpec(DbCommandKind.NonQuery, commandText, cacheKey);
        }
    }
}
