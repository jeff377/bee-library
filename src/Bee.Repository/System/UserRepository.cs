using Bee.Base;
using Bee.Db;
using Bee.Db.Manager;
using Bee.Definition.Database;
using Bee.Repository.Abstractions.System;

namespace Bee.Repository.System
{
    /// <summary>
    /// Reads the common <c>st_user</c> table. Resolves a user's <c>sys_rowid</c> from its
    /// <c>sys_id</c> so company-scoped lookups (e.g. the employee link) can be keyed by row id.
    /// </summary>
    public class UserRepository : IUserRepository
    {
        private readonly IDbConnectionManager _connectionManager;

        /// <summary>
        /// Initializes a new <see cref="UserRepository"/>.
        /// </summary>
        /// <param name="connectionManager">The DI-resolved connection manager.</param>
        public UserRepository(IDbConnectionManager connectionManager)
        {
            _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
        }

        /// <inheritdoc/>
        public Guid GetRowIdBySysId(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) { return Guid.Empty; }

            var dbType = _connectionManager.GetConnectionInfo(DbCategoryIds.Common).DatabaseType;
            string tbl = dbType.QuoteIdentifier("st_user");
            string colRowId = dbType.QuoteIdentifier("sys_rowid");
            string colId = dbType.QuoteIdentifier("sys_id");

            string sql = $"SELECT {colRowId} FROM {tbl} WHERE {colId} = {{0}}";
            var dbAccess = new DbAccess(DbCategoryIds.Common, _connectionManager);
            var result = dbAccess.Execute(new DbCommandSpec(DbCommandKind.Scalar, sql, userId));
            // Scalar is null when the user id matches no row → no user, empty row id.
            return result.Scalar == null ? Guid.Empty : ValueUtilities.CGuid(result.Scalar);
        }

        /// <inheritdoc/>
        public UserLocale GetLocale(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) { return UserLocale.Empty; }

            var dbType = _connectionManager.GetConnectionInfo(DbCategoryIds.Common).DatabaseType;
            string tbl = dbType.QuoteIdentifier("st_user");
            string colTimeZone = dbType.QuoteIdentifier("time_zone");
            string colCulture = dbType.QuoteIdentifier("culture");
            string colId = dbType.QuoteIdentifier("sys_id");

            string sql = $"SELECT {colTimeZone}, {colCulture} FROM {tbl} WHERE {colId} = {{0}}";
            var dbAccess = new DbAccess(DbCategoryIds.Common, _connectionManager);
            var result = dbAccess.Execute(new DbCommandSpec(DbCommandKind.DataTable, sql, userId));
            var table = result.Table;
            if (table == null || table.Rows.Count == 0) { return UserLocale.Empty; }

            // Null covers both "column never populated" and, on Oracle, a stored empty string.
            // Both mean the same thing to the caller: no preference, use the deployment default.
            var row = table.Rows[0];
            return new UserLocale(ReadText(row[0]), ReadText(row[1]));

            static string ReadText(object? value)
                => value == null || value == DBNull.Value ? string.Empty : ValueUtilities.CStr(value).Trim();
        }

        /// <inheritdoc/>
        public string? GetName(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) { return null; }

            var dbType = _connectionManager.GetConnectionInfo(DbCategoryIds.Common).DatabaseType;
            string tbl = dbType.QuoteIdentifier("st_user");
            string colName = dbType.QuoteIdentifier("sys_name");
            string colId = dbType.QuoteIdentifier("sys_id");

            string sql = $"SELECT {colName} FROM {tbl} WHERE {colId} = {{0}}";
            var dbAccess = new DbAccess(DbCategoryIds.Common, _connectionManager);
            var result = dbAccess.Execute(new DbCommandSpec(DbCommandKind.DataTable, sql, userId));
            var table = result.Table;
            // No row means no such user. A row carrying a null name is a user with a blank name,
            // which is a different answer and must not collapse into the same one.
            if (table == null || table.Rows.Count == 0) { return null; }

            var value = table.Rows[0][0];
            return value == null || value == DBNull.Value ? string.Empty : ValueUtilities.CStr(value).Trim();
        }

        /// <inheritdoc/>
        public bool IsDeploymentAdmin(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) { return false; }

            var dbType = _connectionManager.GetConnectionInfo(DbCategoryIds.Common).DatabaseType;
            string tbl = dbType.QuoteIdentifier("st_user");
            string colFlag = dbType.QuoteIdentifier("deployment_admin");
            string colId = dbType.QuoteIdentifier("sys_id");

            string sql = $"SELECT {colFlag} FROM {tbl} WHERE {colId} = {{0}}";
            var dbAccess = new DbAccess(DbCategoryIds.Common, _connectionManager);
            var result = dbAccess.Execute(new DbCommandSpec(DbCommandKind.Scalar, sql, userId));
            // Null covers "no such user" and a column not yet populated by an older row. Both deny.
            return result.Scalar != null && result.Scalar != DBNull.Value && ValueUtilities.CBool(result.Scalar);
        }

        /// <inheritdoc/>
        public bool SetDeploymentAdmin(string userId, bool isDeploymentAdmin)
        {
            if (string.IsNullOrWhiteSpace(userId)) { return false; }

            var dbType = _connectionManager.GetConnectionInfo(DbCategoryIds.Common).DatabaseType;
            string tbl = dbType.QuoteIdentifier("st_user");
            string colFlag = dbType.QuoteIdentifier("deployment_admin");
            string colId = dbType.QuoteIdentifier("sys_id");

            string sql = $"UPDATE {tbl} SET {colFlag} = {{0}} WHERE {colId} = {{1}}";
            var dbAccess = new DbAccess(DbCategoryIds.Common, _connectionManager);
            var spec = new DbCommandSpec(DbCommandKind.NonQuery, sql, isDeploymentAdmin, userId);
            return dbAccess.Execute(spec).RowsAffected > 0;
        }
    }
}
