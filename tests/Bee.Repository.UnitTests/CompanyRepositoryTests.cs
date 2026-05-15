using System.ComponentModel;
using Bee.Db;
using Bee.Db.Manager;
using Bee.Definition.Database;
using Bee.Repository.System;
using Bee.Tests.Shared;

namespace Bee.Repository.UnitTests
{
    /// <summary>
    /// 5 DB round-trip 測試：seed company 'C001' 於 <see cref="SharedDatabaseState"/> 建好，
    /// 測試覆蓋 enabled / nonexistent / disabled 三種情境。
    /// </summary>
    public class CompanyRepositoryTests : IClassFixture<SharedDbFixture>
    {
        private readonly SharedDbFixture _fx;
        public CompanyRepositoryTests(SharedDbFixture fx) { _fx = fx; }

        private CompanyRepository CreateRepo()
            => new CompanyRepository(_fx.GetRequiredService<IDbConnectionManager>());

        #region GetById — Enabled (seed company 'C001')

        private void RunGetByIdEnabled(DatabaseType _)
        {
            var repo = CreateRepo();
            var result = repo.GetById("C001");
            Assert.NotNull(result);
            Assert.Equal("C001", result.CompanyId);
            Assert.Equal("測試公司", result.CompanyName);
            Assert.False(string.IsNullOrEmpty(result.CompanyDatabaseId));
        }

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("GetById 'C001' on SQL Server 應回傳啟用中的 seed company")]
        public void GetById_Enabled_SqlServer() => RunGetByIdEnabled(DatabaseType.SQLServer);

        [DbFact(DatabaseType.PostgreSQL)]
        [DisplayName("GetById 'C001' on PostgreSQL 應回傳啟用中的 seed company")]
        public void GetById_Enabled_PostgreSql() => RunGetByIdEnabled(DatabaseType.PostgreSQL);

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("GetById 'C001' on SQLite 應回傳啟用中的 seed company")]
        public void GetById_Enabled_Sqlite() => RunGetByIdEnabled(DatabaseType.SQLite);

        [DbFact(DatabaseType.MySQL)]
        [DisplayName("GetById 'C001' on MySQL 應回傳啟用中的 seed company")]
        public void GetById_Enabled_MySql() => RunGetByIdEnabled(DatabaseType.MySQL);

        [DbFact(DatabaseType.Oracle)]
        [DisplayName("GetById 'C001' on Oracle 應回傳啟用中的 seed company")]
        public void GetById_Enabled_Oracle() => RunGetByIdEnabled(DatabaseType.Oracle);

        #endregion

        #region GetById — NonExistent

        private void RunGetByIdNotFound(DatabaseType _)
        {
            var repo = CreateRepo();
            var result = repo.GetById("__nonexistent_company_xyz__");
            Assert.Null(result);
        }

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("GetById 不存在公司 on SQL Server 應回傳 null")]
        public void GetById_NotFound_SqlServer() => RunGetByIdNotFound(DatabaseType.SQLServer);

        [DbFact(DatabaseType.PostgreSQL)]
        [DisplayName("GetById 不存在公司 on PostgreSQL 應回傳 null")]
        public void GetById_NotFound_PostgreSql() => RunGetByIdNotFound(DatabaseType.PostgreSQL);

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("GetById 不存在公司 on SQLite 應回傳 null")]
        public void GetById_NotFound_Sqlite() => RunGetByIdNotFound(DatabaseType.SQLite);

        [DbFact(DatabaseType.MySQL)]
        [DisplayName("GetById 不存在公司 on MySQL 應回傳 null")]
        public void GetById_NotFound_MySql() => RunGetByIdNotFound(DatabaseType.MySQL);

        [DbFact(DatabaseType.Oracle)]
        [DisplayName("GetById 不存在公司 on Oracle 應回傳 null")]
        public void GetById_NotFound_Oracle() => RunGetByIdNotFound(DatabaseType.Oracle);

        #endregion

        #region GetById — Disabled (filtered at query layer)

        private void RunGetByIdDisabled(DatabaseType dbType)
        {
            // 建一筆 disabled company；查詢應回 null。本 sub-test 透過原生 SQL 寫 seed
            // 以避免依賴尚未實作的 admin API。company id 用隨機後綴避免 5 DB 殘留汙染。
            var dbAccess = _fx.NewDbAccess(TestDbConventions.GetDatabaseId(dbType));
            var companyId = "DISABLED_" + Guid.NewGuid().ToString("N").Substring(0, 8);
            string tbl = dbType.QuoteIdentifier("st_company");
            string colRowId = dbType.QuoteIdentifier("sys_rowid");
            string colId = dbType.QuoteIdentifier("sys_id");
            string colName = dbType.QuoteIdentifier("sys_name");
            string colDbId = dbType.QuoteIdentifier("company_database_id");
            string colEnabled = dbType.QuoteIdentifier("enabled");
            string colInsTime = dbType.QuoteIdentifier("sys_insert_time");
            string nowExpr = dbType == DatabaseType.PostgreSQL || dbType == DatabaseType.SQLite ? "CURRENT_TIMESTAMP"
                           : dbType == DatabaseType.SQLServer ? "GETDATE()"
                           : dbType == DatabaseType.MySQL ? "CURRENT_TIMESTAMP(6)"
                           : "SYSTIMESTAMP";
            string disabledLiteral = dbType == DatabaseType.PostgreSQL ? "FALSE" : "0";

            var insert = new DbCommandSpec(DbCommandKind.NonQuery,
                $"INSERT INTO {tbl} ({colRowId}, {colId}, {colName}, {colDbId}, {colEnabled}, {colInsTime}) " +
                $"VALUES ({{0}}, {{1}}, {{2}}, {{3}}, {disabledLiteral}, {nowExpr})",
                Guid.NewGuid(), companyId, "停用測試公司", "common");
            dbAccess.Execute(insert);

            try
            {
                var repo = CreateRepo();
                var result = repo.GetById(companyId);
                Assert.Null(result);
            }
            finally
            {
                var cleanup = new DbCommandSpec(DbCommandKind.NonQuery,
                    $"DELETE FROM {tbl} WHERE {colId} = {{0}}", companyId);
                dbAccess.Execute(cleanup);
            }
        }

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("GetById 停用公司 on SQL Server 應回傳 null（query 層過濾）")]
        public void GetById_Disabled_SqlServer() => RunGetByIdDisabled(DatabaseType.SQLServer);

        [DbFact(DatabaseType.PostgreSQL)]
        [DisplayName("GetById 停用公司 on PostgreSQL 應回傳 null（query 層過濾）")]
        public void GetById_Disabled_PostgreSql() => RunGetByIdDisabled(DatabaseType.PostgreSQL);

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("GetById 停用公司 on SQLite 應回傳 null（query 層過濾）")]
        public void GetById_Disabled_Sqlite() => RunGetByIdDisabled(DatabaseType.SQLite);

        [DbFact(DatabaseType.MySQL)]
        [DisplayName("GetById 停用公司 on MySQL 應回傳 null（query 層過濾）")]
        public void GetById_Disabled_MySql() => RunGetByIdDisabled(DatabaseType.MySQL);

        [DbFact(DatabaseType.Oracle)]
        [DisplayName("GetById 停用公司 on Oracle 應回傳 null（query 層過濾）")]
        public void GetById_Disabled_Oracle() => RunGetByIdDisabled(DatabaseType.Oracle);

        #endregion
    }
}
