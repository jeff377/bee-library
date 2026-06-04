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
    /// / <c>st_user_role</c>). Unlike the common-DB system repositories, these tables live in a
    /// company database, so every method takes the company database id explicitly
    /// (resolved by the caller via the company-DB router).
    /// </summary>
    public class RolePermissionRepository : IRolePermissionRepository
    {
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
            string tblGrant = dbType.QuoteIdentifier("st_role_grant");
            string tblRole = dbType.QuoteIdentifier("st_role");
            string colRowId = dbType.QuoteIdentifier("sys_rowid");
            string colSysId = dbType.QuoteIdentifier("sys_id");
            string colRoleRowId = dbType.QuoteIdentifier("role_rowid");
            string colModelId = dbType.QuoteIdentifier("model_id");
            string colActions = dbType.QuoteIdentifier("allowed_actions");

            string sql =
                $"SELECT r.{colSysId}, g.{colModelId}, g.{colActions} \n" +
                $"FROM {tblGrant} g \n" +
                $"INNER JOIN {tblRole} r ON r.{colRowId} = g.{colRoleRowId}";
            var dbAccess = new DbAccess(databaseId, _connectionManager);
            var table = dbAccess.Execute(new DbCommandSpec(DbCommandKind.DataTable, sql)).Table!;

            var list = new List<RoleGrantRow>(table.Rows.Count);
            foreach (DataRow row in table.Rows)
            {
                list.Add(new RoleGrantRow(
                    ValueUtilities.CStr(row["sys_id"]),
                    ValueUtilities.CStr(row["model_id"]),
                    (PermissionAction)ValueUtilities.CInt(row["allowed_actions"])));
            }
            return list;
        }

        /// <inheritdoc/>
        public IReadOnlyList<UserRoleRow> GetUserRoles(string databaseId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(databaseId);

            var dbType = _connectionManager.GetConnectionInfo(databaseId).DatabaseType;
            string tblUserRole = dbType.QuoteIdentifier("st_user_role");
            string tblRole = dbType.QuoteIdentifier("st_role");
            string colRowId = dbType.QuoteIdentifier("sys_rowid");
            string colSysId = dbType.QuoteIdentifier("sys_id");
            string colUserRowId = dbType.QuoteIdentifier("user_rowid");
            string colRoleRowId = dbType.QuoteIdentifier("role_rowid");

            string sql =
                $"SELECT ur.{colUserRowId}, r.{colSysId} \n" +
                $"FROM {tblUserRole} ur \n" +
                $"INNER JOIN {tblRole} r ON r.{colRowId} = ur.{colRoleRowId}";
            var dbAccess = new DbAccess(databaseId, _connectionManager);
            var table = dbAccess.Execute(new DbCommandSpec(DbCommandKind.DataTable, sql)).Table!;

            var list = new List<UserRoleRow>(table.Rows.Count);
            foreach (DataRow row in table.Rows)
            {
                list.Add(new UserRoleRow(
                    ToGuid(row["user_rowid"]).ToString(),
                    ValueUtilities.CStr(row["sys_id"])));
            }
            return list;
        }

        /// <summary>
        /// Converts a database value to a <see cref="Guid"/>, tolerating the per-dialect storage
        /// forms (native Guid, 16-byte binary for Oracle RAW(16), or string for SQLite).
        /// </summary>
        private static Guid ToGuid(object value)
        {
            if (value is Guid g) { return g; }
            if (value is byte[] b && b.Length == 16) { return new Guid(b); }
            if (value is string s && Guid.TryParse(s, out var parsed)) { return parsed; }
            throw new InvalidOperationException($"Cannot convert '{value?.GetType().Name ?? "null"}' to Guid.");
        }
    }
}
