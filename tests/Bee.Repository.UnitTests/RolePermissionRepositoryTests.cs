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
    /// RolePermissionRepository 的 5 DB round-trip 測試：在 company DB insert st_role_grant /
    /// st_user_role（關聯欄一律 sys_id 業務鍵），驗證 GetRoleGrants / GetUserRoles 查回正確
    /// （allowed_actions 還原為 PermissionAction mask、user→role 以 sys_id 配對）。
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
            var userId = string.Concat("USER_", Guid.NewGuid().ToString("N").AsSpan(0, 6));
            var grantRowId = Guid.NewGuid();
            var userRoleRowId = Guid.NewGuid();
            var action = (int)PermissionAction.Read;
            var scope = (int)ScopeStrategy.Dept;

            string tblGrant = dbType.QuoteIdentifier("st_role_grant");
            string tblUserRole = dbType.QuoteIdentifier("st_user_role");
            string colRowId = dbType.QuoteIdentifier("sys_rowid");
            string colRoleId = dbType.QuoteIdentifier("role_id");
            string colUserId = dbType.QuoteIdentifier("user_id");
            string colModelId = dbType.QuoteIdentifier("model_id");
            string colAction = dbType.QuoteIdentifier("action");
            string colScope = dbType.QuoteIdentifier("scope");
            string colInsTime = dbType.QuoteIdentifier("sys_insert_time");

            string nowExpr = dbType == DatabaseType.PostgreSQL || dbType == DatabaseType.SQLite ? "CURRENT_TIMESTAMP"
                           : dbType == DatabaseType.SQLServer ? "GETDATE()"
                           : dbType == DatabaseType.MySQL ? "CURRENT_TIMESTAMP(6)"
                           : "SYSTIMESTAMP";

            dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                $"INSERT INTO {tblGrant} ({colRowId}, {colRoleId}, {colModelId}, {colAction}, {colScope}, {colInsTime}) " +
                $"VALUES ({{0}}, {{1}}, {{2}}, {{3}}, {{4}}, {nowExpr})",
                grantRowId, roleId, "PurchaseOrder", action, scope));
            dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                $"INSERT INTO {tblUserRole} ({colRowId}, {colUserId}, {colRoleId}, {colInsTime}) " +
                $"VALUES ({{0}}, {{1}}, {{2}}, {nowExpr})",
                userRoleRowId, userId, roleId));

            try
            {
                var repo = CreateRepo();

                var grant = repo.GetRoleGrants(databaseId).Single(g => g.RoleId == roleId);
                Assert.Equal("PurchaseOrder", grant.ModelId);
                Assert.Equal(PermissionAction.Read, grant.Action);
                Assert.Equal(ScopeStrategy.Dept, grant.Scope);

                var userRole = repo.GetUserRoles(databaseId).Single(u => u.RoleId == roleId);
                Assert.Equal(userId, userRole.UserId);
            }
            finally
            {
                dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                    $"DELETE FROM {tblUserRole} WHERE {colRowId} = {{0}}", userRoleRowId));
                dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                    $"DELETE FROM {tblGrant} WHERE {colRowId} = {{0}}", grantRowId));
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
