using System.ComponentModel;
using Bee.Db;
using Bee.Db.Manager;
using Bee.Definition.Database;
using Bee.Definition.Settings;
using Bee.Repository.System;
using Bee.Tests.Shared;

namespace Bee.Repository.UnitTests
{
    /// <summary>
    /// RolePermissionRepository 的 5 DB round-trip 測試：在 company DB insert
    /// st_role / st_role_grant / st_user_role，驗證 GetRoleGrants / GetUserRoles 查回正確
    /// （role 以 sys_id 識別、grant 的 allowed_actions 還原為 PermissionAction mask）。
    /// </summary>
    public class RolePermissionRepositoryTests : IClassFixture<SharedDbFixture>
    {
        private readonly SharedDbFixture _fx;
        public RolePermissionRepositoryTests(SharedDbFixture fx) { _fx = fx; }

        private RolePermissionRepository CreateRepo()
            => new RolePermissionRepository(_fx.GetRequiredService<IDbConnectionManager>());

        private void RunRoundTrip(DatabaseType dbType)
        {
            var databaseId = TestDbConventions.GetDatabaseId(dbType, "company");
            var dbAccess = _fx.NewDbAccess(databaseId);

            var roleId = string.Concat("ROLE_", Guid.NewGuid().ToString("N").AsSpan(0, 6));
            var roleRowId = Guid.NewGuid();
            var grantRowId = Guid.NewGuid();
            var userRoleRowId = Guid.NewGuid();
            var userRowId = Guid.NewGuid();
            var allowed = (int)(PermissionAction.Read | PermissionAction.Update);

            string tblRole = dbType.QuoteIdentifier("st_role");
            string tblGrant = dbType.QuoteIdentifier("st_role_grant");
            string tblUserRole = dbType.QuoteIdentifier("st_user_role");
            string colRowId = dbType.QuoteIdentifier("sys_rowid");
            string colSysId = dbType.QuoteIdentifier("sys_id");
            string colName = dbType.QuoteIdentifier("sys_name");
            string colRoleRowId = dbType.QuoteIdentifier("role_rowid");
            string colUserRowId = dbType.QuoteIdentifier("user_rowid");
            string colModelId = dbType.QuoteIdentifier("model_id");
            string colActions = dbType.QuoteIdentifier("allowed_actions");
            string colInsTime = dbType.QuoteIdentifier("sys_insert_time");

            string nowExpr = dbType == DatabaseType.PostgreSQL || dbType == DatabaseType.SQLite ? "CURRENT_TIMESTAMP"
                           : dbType == DatabaseType.SQLServer ? "GETDATE()"
                           : dbType == DatabaseType.MySQL ? "CURRENT_TIMESTAMP(6)"
                           : "SYSTIMESTAMP";

            dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                $"INSERT INTO {tblRole} ({colRowId}, {colSysId}, {colName}, {colInsTime}) " +
                $"VALUES ({{0}}, {{1}}, {{2}}, {nowExpr})",
                roleRowId, roleId, "測試角色"));
            dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                $"INSERT INTO {tblGrant} ({colRowId}, {colRoleRowId}, {colModelId}, {colActions}, {colInsTime}) " +
                $"VALUES ({{0}}, {{1}}, {{2}}, {{3}}, {nowExpr})",
                grantRowId, roleRowId, "PurchaseOrder", allowed));
            dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                $"INSERT INTO {tblUserRole} ({colRowId}, {colUserRowId}, {colRoleRowId}, {colInsTime}) " +
                $"VALUES ({{0}}, {{1}}, {{2}}, {nowExpr})",
                userRoleRowId, userRowId, roleRowId));

            try
            {
                var repo = CreateRepo();

                var grant = repo.GetRoleGrants(databaseId).Single(g => g.RoleId == roleId);
                Assert.Equal("PurchaseOrder", grant.ModelId);
                Assert.Equal(PermissionAction.Read | PermissionAction.Update, grant.AllowedActions);

                var userRole = repo.GetUserRoles(databaseId).Single(u => u.RoleId == roleId);
                Assert.Equal(userRowId.ToString(), userRole.UserRowId);
            }
            finally
            {
                dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                    $"DELETE FROM {tblUserRole} WHERE {colRowId} = {{0}}", userRoleRowId));
                dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                    $"DELETE FROM {tblGrant} WHERE {colRowId} = {{0}}", grantRowId));
                dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                    $"DELETE FROM {tblRole} WHERE {colRowId} = {{0}}", roleRowId));
            }
        }

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("RolePermission round-trip on SQL Server")]
        public void RoundTrip_SqlServer() => RunRoundTrip(DatabaseType.SQLServer);

        [DbFact(DatabaseType.PostgreSQL)]
        [DisplayName("RolePermission round-trip on PostgreSQL")]
        public void RoundTrip_PostgreSql() => RunRoundTrip(DatabaseType.PostgreSQL);

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("RolePermission round-trip on SQLite")]
        public void RoundTrip_Sqlite() => RunRoundTrip(DatabaseType.SQLite);

        [DbFact(DatabaseType.MySQL)]
        [DisplayName("RolePermission round-trip on MySQL")]
        public void RoundTrip_MySql() => RunRoundTrip(DatabaseType.MySQL);

        [DbFact(DatabaseType.Oracle)]
        [DisplayName("RolePermission round-trip on Oracle")]
        public void RoundTrip_Oracle() => RunRoundTrip(DatabaseType.Oracle);
    }
}
