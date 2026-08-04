using System.Data;
using Bee.Base;
using Bee.Db;
using Bee.Definition.Identity;
using Bee.Definition.Settings;
using Bee.Repository.Abstractions.System;

namespace Bee.Repository.System
{
    /// <summary>
    /// Data access for the per-company permission tables (<c>st_role</c> / <c>st_role_grant</c>
    /// / <c>st_user_role</c>). These tables live in a company database, so every method takes the
    /// company database id explicitly (resolved by the caller via the company-DB router). All
    /// relations use <c>sys_id</c> business keys (role / user), matching the sys_id-only permission
    /// cache; row ids are reserved for single-record access.
    /// </summary>
    public class RolePermissionRepository : RepositoryBase, IRolePermissionRepository
    {
        private const string ColRoleId = "role_id";

        /// <summary>
        /// Initializes a new <see cref="RolePermissionRepository"/>.
        /// </summary>
        /// <param name="ctx">The shared repository context.</param>
        /// <param name="accessToken">The current request's access token.</param>
        /// <param name="progId">Unused on the framework axis; accepted for signature uniformity.</param>
        /// <remarks>
        /// Declares no scope: its tables live in a company database, but its callers are cache
        /// providers and session bootstrap, which are told which company to read and hold no token
        /// to route with. Routing by session here would read the caller's company instead of the
        /// requested one.
        /// </remarks>
        public RolePermissionRepository(IRepositoryContext ctx, Guid accessToken, string progId)
            : base(ctx, accessToken, progId, null)
        {
        }

        /// <inheritdoc/>
        public IReadOnlyList<RoleGrantRow> GetRoleGrants(string databaseId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(databaseId);

            var dbType = Context.ConnectionManager.GetConnectionInfo(databaseId).DatabaseType;
            string tbl = dbType.QuoteIdentifier("st_role_grant");
            string colRoleId = dbType.QuoteIdentifier(ColRoleId);
            string colModelId = dbType.QuoteIdentifier("model_id");
            string colAction = dbType.QuoteIdentifier("action");
            string colScope = dbType.QuoteIdentifier("scope");

            string sql = $"SELECT {colRoleId}, {colModelId}, {colAction}, {colScope} FROM {tbl}";
            var dbAccess = CreateDbAccess(databaseId);
            var table = dbAccess.Execute(new DbCommandSpec(DbCommandKind.DataTable, sql)).Table!;

            var list = new List<RoleGrantRow>(table.Rows.Count);
            foreach (DataRow row in table.Rows)
            {
                list.Add(new RoleGrantRow(
                    ValueUtilities.CStr(row[ColRoleId]),
                    ValueUtilities.CStr(row["model_id"]),
                    (PermissionAction)ValueUtilities.CInt(row["action"]),
                    (ScopeStrategy)ValueUtilities.CInt(row["scope"])));
            }
            return list;
        }

        /// <inheritdoc/>
        public IReadOnlyList<UserRoleRow> GetUserRoles(string databaseId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(databaseId);

            var dbType = Context.ConnectionManager.GetConnectionInfo(databaseId).DatabaseType;
            string tbl = dbType.QuoteIdentifier("st_user_role");
            string colUserId = dbType.QuoteIdentifier("user_id");
            string colRoleId = dbType.QuoteIdentifier(ColRoleId);

            string sql = $"SELECT {colUserId}, {colRoleId} FROM {tbl}";
            var dbAccess = CreateDbAccess(databaseId);
            var table = dbAccess.Execute(new DbCommandSpec(DbCommandKind.DataTable, sql)).Table!;

            var list = new List<UserRoleRow>(table.Rows.Count);
            foreach (DataRow row in table.Rows)
            {
                list.Add(new UserRoleRow(
                    ValueUtilities.CStr(row["user_id"]),
                    ValueUtilities.CStr(row[ColRoleId])));
            }
            return list;
        }
    }
}
