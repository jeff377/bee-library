using System.Data;
using Bee.Base;
using Bee.Db;
using Bee.Db.Manager;
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
    public class RolePermissionRepository : IRolePermissionRepository
    {
        private const string ColRoleId = "role_id";

        private readonly IDbConnectionManager _connectionManager;

        /// <summary>
        /// Initializes a new <see cref="RolePermissionRepository"/>.
        /// </summary>
        /// <param name="connectionManager">The DI-resolved connection manager.</param>
        public RolePermissionRepository(IDbConnectionManager connectionManager)
        {
            _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
        }

        /// <inheritdoc/>
        public IReadOnlyList<RoleGrantRow> GetRoleGrants(string databaseId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(databaseId);

            var dbType = _connectionManager.GetConnectionInfo(databaseId).DatabaseType;
            string tbl = dbType.QuoteIdentifier("st_role_grant");
            string colRoleId = dbType.QuoteIdentifier(ColRoleId);
            string colModelId = dbType.QuoteIdentifier("model_id");
            string colActions = dbType.QuoteIdentifier("allowed_actions");

            string sql = $"SELECT {colRoleId}, {colModelId}, {colActions} FROM {tbl}";
            var dbAccess = new DbAccess(databaseId, _connectionManager);
            var table = dbAccess.Execute(new DbCommandSpec(DbCommandKind.DataTable, sql)).Table!;

            var list = new List<RoleGrantRow>(table.Rows.Count);
            foreach (DataRow row in table.Rows)
            {
                list.Add(new RoleGrantRow(
                    ValueUtilities.CStr(row[ColRoleId]),
                    ValueUtilities.CStr(row["model_id"]),
                    (PermissionActions)ValueUtilities.CInt(row["allowed_actions"])));
            }
            return list;
        }

        /// <inheritdoc/>
        public IReadOnlyList<UserRoleRow> GetUserRoles(string databaseId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(databaseId);

            var dbType = _connectionManager.GetConnectionInfo(databaseId).DatabaseType;
            string tbl = dbType.QuoteIdentifier("st_user_role");
            string colUserId = dbType.QuoteIdentifier("user_id");
            string colRoleId = dbType.QuoteIdentifier(ColRoleId);

            string sql = $"SELECT {colUserId}, {colRoleId} FROM {tbl}";
            var dbAccess = new DbAccess(databaseId, _connectionManager);
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
