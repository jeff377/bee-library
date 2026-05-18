using System.ComponentModel;
using Bee.Db;
using Bee.Db.Manager;
using Bee.Definition.Database;
using Bee.Repository.System;
using Bee.Tests.Shared;

namespace Bee.Repository.UnitTests
{
    /// <summary>
    /// 5 DB round-trip 測試：seed user '001' ↔ company 'C001' 對照於
    /// <see cref="SharedDatabaseState"/> 已建好；測試覆蓋三種情境 — granted+enabled、
    /// not-granted、granted+disabled。
    /// </summary>
    public class UserCompanyRepositoryTests : IClassFixture<SharedDbFixture>
    {
        private readonly SharedDbFixture _fx;
        public UserCompanyRepositoryTests(SharedDbFixture fx) { _fx = fx; }

        private UserCompanyRepository CreateRepo()
            => new UserCompanyRepository(_fx.GetRequiredService<IDbConnectionManager>());

        #region HasAccess — Granted + Enabled

        private void RunHasAccessGranted(DatabaseType _)
        {
            var repo = CreateRepo();
            Assert.True(repo.HasAccess("001", "C001"));
        }

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("HasAccess('001','C001') on SQL Server seed 對照存在 → true")]
        public void HasAccess_Granted_SqlServer() => RunHasAccessGranted(DatabaseType.SQLServer);

        [DbFact(DatabaseType.PostgreSQL)]
        [DisplayName("HasAccess('001','C001') on PostgreSQL seed 對照存在 → true")]
        public void HasAccess_Granted_PostgreSql() => RunHasAccessGranted(DatabaseType.PostgreSQL);

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("HasAccess('001','C001') on SQLite seed 對照存在 → true")]
        public void HasAccess_Granted_Sqlite() => RunHasAccessGranted(DatabaseType.SQLite);

        [DbFact(DatabaseType.MySQL)]
        [DisplayName("HasAccess('001','C001') on MySQL seed 對照存在 → true")]
        public void HasAccess_Granted_MySql() => RunHasAccessGranted(DatabaseType.MySQL);

        [DbFact(DatabaseType.Oracle)]
        [DisplayName("HasAccess('001','C001') on Oracle seed 對照存在 → true")]
        public void HasAccess_Granted_Oracle() => RunHasAccessGranted(DatabaseType.Oracle);

        #endregion

        #region HasAccess — Not Granted (nonexistent company)

        private void RunHasAccessNotGranted(DatabaseType _)
        {
            var repo = CreateRepo();
            Assert.False(repo.HasAccess("001", "__nonexistent_company_xyz__"));
        }

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("HasAccess 不存在公司 on SQL Server → false")]
        public void HasAccess_NotGranted_SqlServer() => RunHasAccessNotGranted(DatabaseType.SQLServer);

        [DbFact(DatabaseType.PostgreSQL)]
        [DisplayName("HasAccess 不存在公司 on PostgreSQL → false")]
        public void HasAccess_NotGranted_PostgreSql() => RunHasAccessNotGranted(DatabaseType.PostgreSQL);

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("HasAccess 不存在公司 on SQLite → false")]
        public void HasAccess_NotGranted_Sqlite() => RunHasAccessNotGranted(DatabaseType.SQLite);

        [DbFact(DatabaseType.MySQL)]
        [DisplayName("HasAccess 不存在公司 on MySQL → false")]
        public void HasAccess_NotGranted_MySql() => RunHasAccessNotGranted(DatabaseType.MySQL);

        [DbFact(DatabaseType.Oracle)]
        [DisplayName("HasAccess 不存在公司 on Oracle → false")]
        public void HasAccess_NotGranted_Oracle() => RunHasAccessNotGranted(DatabaseType.Oracle);

        #endregion

        #region HasAccess — Granted but Company Disabled

        private void RunHasAccessDisabledCompany(DatabaseType dbType)
        {
            // Seed: 建 disabled company + 對照 user '001' → 該公司；HasAccess 應為 false。
            var dbAccess = _fx.NewDbAccess(TestDbConventions.GetDatabaseId(dbType));
            var companyId = string.Concat("DIS_", Guid.NewGuid().ToString("N").AsSpan(0, 6));
            var companyRowId = Guid.NewGuid();
            var linkRowId = Guid.NewGuid();

            string tblCompany = dbType.QuoteIdentifier("st_company");
            string tblUc = dbType.QuoteIdentifier("st_user_company");
            string tblUser = dbType.QuoteIdentifier("st_user");
            string colRowId = dbType.QuoteIdentifier("sys_rowid");
            string colSysId = dbType.QuoteIdentifier("sys_id");
            string colName = dbType.QuoteIdentifier("sys_name");
            string colDbId = dbType.QuoteIdentifier("company_database_id");
            string colEnabled = dbType.QuoteIdentifier("enabled");
            string colUserRowId = dbType.QuoteIdentifier("user_rowid");
            string colCompanyRowId = dbType.QuoteIdentifier("company_rowid");
            string colInsTime = dbType.QuoteIdentifier("sys_insert_time");

            string nowExpr = dbType == DatabaseType.PostgreSQL || dbType == DatabaseType.SQLite ? "CURRENT_TIMESTAMP"
                           : dbType == DatabaseType.SQLServer ? "GETDATE()"
                           : dbType == DatabaseType.MySQL ? "CURRENT_TIMESTAMP(6)"
                           : "SYSTIMESTAMP";
            string disabledLiteral = dbType == DatabaseType.PostgreSQL ? "FALSE" : "0";

            var insertCompany = new DbCommandSpec(DbCommandKind.NonQuery,
                $"INSERT INTO {tblCompany} ({colRowId}, {colSysId}, {colName}, {colDbId}, {colEnabled}, {colInsTime}) " +
                $"VALUES ({{0}}, {{1}}, {{2}}, {{3}}, {disabledLiteral}, {nowExpr})",
                companyRowId, companyId, "停用公司", "common");
            dbAccess.Execute(insertCompany);

            // 取 user '001' rowid
            var lookupUser = new DbCommandSpec(DbCommandKind.Scalar,
                $"SELECT {colRowId} FROM {tblUser} WHERE {colSysId} = {{0}}", "001");
            var userResult = dbAccess.Execute(lookupUser);
            var userRowId = ToGuid(userResult.Scalar!);

            var insertLink = new DbCommandSpec(DbCommandKind.NonQuery,
                $"INSERT INTO {tblUc} ({colRowId}, {colUserRowId}, {colCompanyRowId}, {colInsTime}) " +
                $"VALUES ({{0}}, {{1}}, {{2}}, {nowExpr})",
                linkRowId, userRowId, companyRowId);
            dbAccess.Execute(insertLink);

            try
            {
                var repo = CreateRepo();
                Assert.False(repo.HasAccess("001", companyId));
            }
            finally
            {
                var cleanupLink = new DbCommandSpec(DbCommandKind.NonQuery,
                    $"DELETE FROM {tblUc} WHERE {colRowId} = {{0}}", linkRowId);
                dbAccess.Execute(cleanupLink);
                var cleanupCompany = new DbCommandSpec(DbCommandKind.NonQuery,
                    $"DELETE FROM {tblCompany} WHERE {colRowId} = {{0}}", companyRowId);
                dbAccess.Execute(cleanupCompany);
            }
        }

        private static Guid ToGuid(object value)
        {
            if (value is Guid g) return g;
            if (value is byte[] b && b.Length == 16) return new Guid(b);
            if (value is string s && Guid.TryParse(s, out var parsed)) return parsed;
            throw new InvalidOperationException($"Cannot convert {value?.GetType().Name} to Guid.");
        }

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("HasAccess 已授權但公司停用 on SQL Server → false")]
        public void HasAccess_GrantedDisabled_SqlServer() => RunHasAccessDisabledCompany(DatabaseType.SQLServer);

        [DbFact(DatabaseType.PostgreSQL)]
        [DisplayName("HasAccess 已授權但公司停用 on PostgreSQL → false")]
        public void HasAccess_GrantedDisabled_PostgreSql() => RunHasAccessDisabledCompany(DatabaseType.PostgreSQL);

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("HasAccess 已授權但公司停用 on SQLite → false")]
        public void HasAccess_GrantedDisabled_Sqlite() => RunHasAccessDisabledCompany(DatabaseType.SQLite);

        [DbFact(DatabaseType.MySQL)]
        [DisplayName("HasAccess 已授權但公司停用 on MySQL → false")]
        public void HasAccess_GrantedDisabled_MySql() => RunHasAccessDisabledCompany(DatabaseType.MySQL);

        [DbFact(DatabaseType.Oracle)]
        [DisplayName("HasAccess 已授權但公司停用 on Oracle → false")]
        public void HasAccess_GrantedDisabled_Oracle() => RunHasAccessDisabledCompany(DatabaseType.Oracle);

        #endregion
    }
}
