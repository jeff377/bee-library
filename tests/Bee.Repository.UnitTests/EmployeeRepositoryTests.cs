using System.ComponentModel;
using Bee.Db;
using Bee.Db.Manager;
using Bee.Definition.Database;
using Bee.Repository.System;
using Bee.Tests.Shared;

namespace Bee.Repository.UnitTests
{
    /// <summary>
    /// EmployeeRepository 的 5 DB round-trip 測試：在 company DB insert st_employee
    /// （含 user_rowid / dept_rowid 連結），驗證 GetByUserRowId 查回對應員工、未知 user 回 null。
    /// </summary>
    public class EmployeeRepositoryTests : IClassFixture<SharedDbFixture>
    {
        private readonly SharedDbFixture _fx;
        public EmployeeRepositoryTests(SharedDbFixture fx) { _fx = fx; }

        private EmployeeRepository CreateRepo()
            => new EmployeeRepository(_fx.GetRequiredService<IDbConnectionManager>());

        private void RunRoundTrip(DatabaseType dbType)
        {
            var databaseId = TestDbConventions.GetDatabaseId(dbType, "company");
            var dbAccess = _fx.NewDbAccess(databaseId);

            var empRowId = Guid.NewGuid();
            var userRowId = Guid.NewGuid();
            var deptRowId = Guid.NewGuid();
            var empId = string.Concat("EMP_", Guid.NewGuid().ToString("N").AsSpan(0, 6));

            string tbl = dbType.QuoteIdentifier("st_employee");
            string colRowId = dbType.QuoteIdentifier("sys_rowid");
            string colId = dbType.QuoteIdentifier("sys_id");
            string colName = dbType.QuoteIdentifier("sys_name");
            string colDept = dbType.QuoteIdentifier("dept_rowid");
            string colUser = dbType.QuoteIdentifier("user_rowid");
            string cols = $"({colRowId}, {colId}, {colName}, {colDept}, {colUser})";

            dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                $"INSERT INTO {tbl} {cols} VALUES ({{0}}, {{1}}, {{2}}, {{3}}, {{4}})",
                empRowId, empId, "測試員工", deptRowId, userRowId));

            try
            {
                var employee = CreateRepo().GetByUserRowId(databaseId, userRowId);

                Assert.NotNull(employee);
                Assert.Equal(empRowId, employee!.RowId);
                Assert.Equal(empId, employee.EmployeeId);
                Assert.Equal(deptRowId, employee.DeptRowId);
                Assert.Equal(userRowId, employee.UserRowId);

                // 未知 user → null
                Assert.Null(CreateRepo().GetByUserRowId(databaseId, Guid.NewGuid()));
            }
            finally
            {
                dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                    $"DELETE FROM {tbl} WHERE {colRowId} = {{0}}", empRowId));
            }
        }

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("Employee round-trip on SQL Server")]
        public void RoundTrip_SqlServer() => RunRoundTrip(DatabaseType.SQLServer);

        [DbFact(DatabaseType.PostgreSQL)]
        [DisplayName("Employee round-trip on PostgreSQL")]
        public void RoundTrip_PostgreSql() => RunRoundTrip(DatabaseType.PostgreSQL);

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("Employee round-trip on SQLite")]
        public void RoundTrip_Sqlite() => RunRoundTrip(DatabaseType.SQLite);

        [DbFact(DatabaseType.MySQL)]
        [DisplayName("Employee round-trip on MySQL")]
        public void RoundTrip_MySql() => RunRoundTrip(DatabaseType.MySQL);

        [DbFact(DatabaseType.Oracle)]
        [DisplayName("Employee round-trip on Oracle")]
        public void RoundTrip_Oracle() => RunRoundTrip(DatabaseType.Oracle);
    }
}
