using System.ComponentModel;
using Bee.Db;
using Bee.Db.Manager;
using Bee.Definition.Database;
using Bee.Repository.System;
using Bee.Tests.Shared;

namespace Bee.Repository.UnitTests
{
    /// <summary>
    /// DepartmentRepository 的 5 DB round-trip 測試：在 company DB insert st_department
    /// （含 parent_rowid 連結），驗證 GetDepartments 查回節點與父子關係正確。
    /// </summary>
    public class DepartmentRepositoryTests : IClassFixture<SharedDbFixture>
    {
        private readonly SharedDbFixture _fx;
        public DepartmentRepositoryTests(SharedDbFixture fx) { _fx = fx; }

        private DepartmentRepository CreateRepo()
            => new DepartmentRepository(_fx.GetRequiredService<IDbConnectionManager>());

        private void RunRoundTrip(DatabaseType dbType)
        {
            var databaseId = TestDbConventions.GetDatabaseId(dbType, "company");
            var dbAccess = _fx.NewDbAccess(databaseId);

            var parentRowId = Guid.NewGuid();
            var childRowId = Guid.NewGuid();
            var parentId = string.Concat("DEP_", Guid.NewGuid().ToString("N").AsSpan(0, 6));
            var childId = string.Concat("DEP_", Guid.NewGuid().ToString("N").AsSpan(0, 6));

            string tbl = dbType.QuoteIdentifier("st_department");
            string colRowId = dbType.QuoteIdentifier("sys_rowid");
            string colId = dbType.QuoteIdentifier("sys_id");
            string colName = dbType.QuoteIdentifier("sys_name");
            string colManager = dbType.QuoteIdentifier("manager_rowid");
            string colParent = dbType.QuoteIdentifier("parent_rowid");
            string cols = $"({colRowId}, {colId}, {colName}, {colManager}, {colParent})";

            dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                $"INSERT INTO {tbl} {cols} VALUES ({{0}}, {{1}}, {{2}}, {{3}}, {{4}})",
                parentRowId, parentId, "母部門", Guid.Empty, Guid.Empty));
            dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                $"INSERT INTO {tbl} {cols} VALUES ({{0}}, {{1}}, {{2}}, {{3}}, {{4}})",
                childRowId, childId, "子部門", Guid.Empty, parentRowId));

            try
            {
                var all = CreateRepo().GetDepartments(databaseId);

                var parent = all.Single(d => d.RowId == parentRowId);
                Assert.Equal(parentId, parent.DeptId);
                Assert.Equal(Guid.Empty, parent.ParentRowId);

                var child = all.Single(d => d.RowId == childRowId);
                Assert.Equal(childId, child.DeptId);
                Assert.Equal(parentRowId, child.ParentRowId);
            }
            finally
            {
                dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                    $"DELETE FROM {tbl} WHERE {colRowId} = {{0}}", childRowId));
                dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                    $"DELETE FROM {tbl} WHERE {colRowId} = {{0}}", parentRowId));
            }
        }

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("Department round-trip on SQL Server")]
        public void RoundTrip_SqlServer() => RunRoundTrip(DatabaseType.SQLServer);

        [DbFact(DatabaseType.PostgreSQL)]
        [DisplayName("Department round-trip on PostgreSQL")]
        public void RoundTrip_PostgreSql() => RunRoundTrip(DatabaseType.PostgreSQL);

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("Department round-trip on SQLite")]
        public void RoundTrip_Sqlite() => RunRoundTrip(DatabaseType.SQLite);

        [DbFact(DatabaseType.MySQL)]
        [DisplayName("Department round-trip on MySQL")]
        public void RoundTrip_MySql() => RunRoundTrip(DatabaseType.MySQL);

        [DbFact(DatabaseType.Oracle)]
        [DisplayName("Department round-trip on Oracle")]
        public void RoundTrip_Oracle() => RunRoundTrip(DatabaseType.Oracle);
    }
}
