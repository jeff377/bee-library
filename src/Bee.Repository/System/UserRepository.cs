using Bee.Base;
using Bee.Base.Security;
using Bee.Db;
using Bee.Definition;
using Bee.Definition.Database;
using Bee.Repository.Abstractions.System;

namespace Bee.Repository.System
{
    /// <summary>
    /// Reads the common <c>st_user</c> table. Resolves a user's <c>sys_rowid</c> from its
    /// <c>sys_id</c> so company-scoped lookups (e.g. the employee link) can be keyed by row id.
    /// </summary>
    public class UserRepository : RepositoryBase, IUserRepository
    {
        private const string TableName = "st_user";
        private const string SysIdColumn = "sys_id";

        /// <summary>
        /// Initializes a new <see cref="UserRepository"/>.
        /// </summary>
        /// <param name="ctx">The shared repository context.</param>
        /// <param name="accessToken">The current request's access token.</param>
        /// <param name="progId">Unused on the framework axis; accepted for signature uniformity.</param>
        public UserRepository(IRepositoryContext ctx, Guid accessToken, string progId)
            : base(ctx, accessToken, progId, DbScope.Common)
        {
        }

        /// <inheritdoc/>
        public Guid GetRowIdBySysId(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) { return Guid.Empty; }

            var dbType = Context.ConnectionManager.GetConnectionInfo(DbCategoryIds.Common).DatabaseType;
            string tbl = dbType.QuoteIdentifier(TableName);
            string colRowId = dbType.QuoteIdentifier("sys_rowid");
            string colId = dbType.QuoteIdentifier(SysIdColumn);

            string sql = $"SELECT {colRowId} FROM {tbl} WHERE {colId} = {{0}}";
            var dbAccess = CreateDbAccess();
            var result = dbAccess.Execute(new DbCommandSpec(DbCommandKind.Scalar, sql, userId));
            // Scalar is null when the user id matches no row → no user, empty row id.
            return result.Scalar == null ? Guid.Empty : ValueUtilities.CGuid(result.Scalar);
        }

        /// <inheritdoc/>
        public bool VerifyPassword(string userId, string password)
        {
            if (string.IsNullOrWhiteSpace(userId)) { return false; }

            var dbType = Context.ConnectionManager.GetConnectionInfo(DbCategoryIds.Common).DatabaseType;
            string tbl = dbType.QuoteIdentifier(TableName);
            string colPassword = dbType.QuoteIdentifier("password");
            string colId = dbType.QuoteIdentifier(SysIdColumn);

            string sql = $"SELECT {colPassword} FROM {tbl} WHERE {colId} = {{0}}";
            var dbAccess = CreateDbAccess();
            var result = dbAccess.Execute(new DbCommandSpec(DbCommandKind.Scalar, sql, userId));
            if (result.Scalar == null || result.Scalar == DBNull.Value) { return false; }

            var hash = ValueUtilities.CStr(result.Scalar);
            // A blank stored hash is an account with no password set, not an account that accepts
            // any password. `VerifyPassword` would return false for it anyway; short-circuiting
            // keeps that intent explicit rather than incidental.
            if (string.IsNullOrEmpty(hash)) { return false; }

            return PasswordHasher.VerifyPassword(password, hash);
        }

        /// <inheritdoc/>
        public UserLocale GetLocale(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) { return UserLocale.Empty; }

            var dbType = Context.ConnectionManager.GetConnectionInfo(DbCategoryIds.Common).DatabaseType;
            string tbl = dbType.QuoteIdentifier(TableName);
            string colTimeZone = dbType.QuoteIdentifier("time_zone");
            string colCulture = dbType.QuoteIdentifier("culture");
            string colId = dbType.QuoteIdentifier(SysIdColumn);

            string sql = $"SELECT {colTimeZone}, {colCulture} FROM {tbl} WHERE {colId} = {{0}}";
            var dbAccess = CreateDbAccess();
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

            var dbType = Context.ConnectionManager.GetConnectionInfo(DbCategoryIds.Common).DatabaseType;
            string tbl = dbType.QuoteIdentifier(TableName);
            string colName = dbType.QuoteIdentifier("sys_name");
            string colId = dbType.QuoteIdentifier(SysIdColumn);

            string sql = $"SELECT {colName} FROM {tbl} WHERE {colId} = {{0}}";
            var dbAccess = CreateDbAccess();
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

            var dbType = Context.ConnectionManager.GetConnectionInfo(DbCategoryIds.Common).DatabaseType;
            string tbl = dbType.QuoteIdentifier(TableName);
            string colFlag = dbType.QuoteIdentifier("deployment_admin");
            string colId = dbType.QuoteIdentifier(SysIdColumn);

            string sql = $"SELECT {colFlag} FROM {tbl} WHERE {colId} = {{0}}";
            var dbAccess = CreateDbAccess();
            var result = dbAccess.Execute(new DbCommandSpec(DbCommandKind.Scalar, sql, userId));
            // Null covers "no such user" and a column not yet populated by an older row. Both deny.
            return result.Scalar != null && result.Scalar != DBNull.Value && ValueUtilities.CBool(result.Scalar);
        }

        /// <inheritdoc/>
        public bool SetDeploymentAdmin(string userId, bool isDeploymentAdmin)
        {
            if (string.IsNullOrWhiteSpace(userId)) { return false; }

            var dbType = Context.ConnectionManager.GetConnectionInfo(DbCategoryIds.Common).DatabaseType;
            string tbl = dbType.QuoteIdentifier(TableName);
            string colFlag = dbType.QuoteIdentifier("deployment_admin");
            string colId = dbType.QuoteIdentifier(SysIdColumn);

            string sql = $"UPDATE {tbl} SET {colFlag} = {{0}} WHERE {colId} = {{1}}";
            var dbAccess = CreateDbAccess();
            var spec = new DbCommandSpec(DbCommandKind.NonQuery, sql, isDeploymentAdmin, userId);
            return dbAccess.Execute(spec).RowsAffected > 0;
        }
    }
}
