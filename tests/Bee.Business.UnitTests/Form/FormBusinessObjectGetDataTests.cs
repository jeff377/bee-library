using System.ComponentModel;
using System.Data;
using Bee.Business.Form;
using Bee.Db.Dml;
using Bee.Definition;
using Bee.Definition.Database;
using Bee.Definition.Filters;
using Bee.Tests.Shared;

namespace Bee.Business.UnitTests.Form
{
    /// <summary>
    /// <see cref="FormBusinessObject.GetData"/> 的 <c>[DbFact]</c> 整合測試:
    /// 種子一筆 Employee → GetData(rowId) → 比對欄位值;並確認回傳的
    /// DataSet 維持 <c>DataSetName == ProgId</c> 與 <c>Tables[ProgId]</c> 即
    /// Master 的框架不變式,所有 row state 為 <see cref="DataRowState.Unchanged"/>。
    /// </summary>
    public class FormBusinessObjectGetDataTests : IClassFixture<SharedDbFixture>
    {
        private readonly SharedDbFixture _fx;

        public FormBusinessObjectGetDataTests(SharedDbFixture fx) { _fx = fx; }

        [Fact]
        [DisplayName("GetData 傳入 null 應拋 ArgumentNullException")]
        public void GetData_NullArgs_Throws()
        {
            var bo = new FormBusinessObject(TestBeeContext.Create(_fx), Guid.NewGuid(),
                CrudTestContext.ProgId);
            Assert.Throws<ArgumentNullException>(() => bo.GetData(null!));
        }

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("SQLite:GetData 應回傳已存在 Employee 並維持 DataSetName / Master TableName 慣例")]
        public void GetData_Sqlite_ReturnsExistingRow()
            => RunReturnsExistingRow(DatabaseType.SQLite);

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("SQL Server:GetData 應回傳已存在 Employee 並維持 DataSetName / Master TableName 慣例")]
        public void GetData_SqlServer_ReturnsExistingRow()
            => RunReturnsExistingRow(DatabaseType.SQLServer);

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("SQLite:GetData 對不存在的 RowId 應回傳 null")]
        public void GetData_Sqlite_NonExistentRowId_ReturnsNull()
            => RunNonExistentRowReturnsNull(DatabaseType.SQLite);

        private void RunReturnsExistingRow(DatabaseType dbType)
        {
            var ctx = new CrudTestContext(_fx, dbType);
            string runId = Guid.NewGuid().ToString("N")[..8];
            var employeeRowId = Guid.NewGuid();

            try
            {
                InsertEmployee(ctx, employeeRowId, $"E{runId}", "員工甲", Guid.Empty);

                var bo = ctx.CreateBo();
                var result = bo.GetData(new GetDataArgs { RowId = employeeRowId });

                Assert.NotNull(result.DataSet);
                // 框架不變式
                Assert.Equal(CrudTestContext.ProgId, result.DataSet!.DataSetName);
                Assert.True(result.DataSet.Tables.Contains(CrudTestContext.ProgId));

                var master = result.DataSet.Tables[CrudTestContext.ProgId]!;
                Assert.Single(master.Rows);
                // SQLite stores GUID as TEXT — compare via string round-trip so
                // the same assertion passes on every provider.
                Assert.Equal(employeeRowId, Guid.Parse(master.Rows[0][SysFields.RowId].ToString()!));
                Assert.Equal($"E{runId}", master.Rows[0]["sys_id"]);
                Assert.Equal("員工甲", master.Rows[0][SysFields.Name]);

                Assert.Equal(DataRowState.Unchanged, master.Rows[0].RowState);
            }
            finally
            {
                TryDelete(ctx, employeeRowId);
            }
        }

        private void RunNonExistentRowReturnsNull(DatabaseType dbType)
        {
            var ctx = new CrudTestContext(_fx, dbType);
            var bo = ctx.CreateBo();

            var result = bo.GetData(new GetDataArgs { RowId = Guid.NewGuid() });

            Assert.Null(result.DataSet);
        }

        private static void InsertEmployee(CrudTestContext ctx, Guid rowId, string sysId, string sysName, Guid deptRowId)
        {
            var dt = new DataTable();
            dt.Columns.Add(SysFields.RowId, typeof(Guid));
            dt.Columns.Add("sys_id", typeof(string));
            dt.Columns.Add(SysFields.Name, typeof(string));
            dt.Columns.Add("dept_rowid", typeof(Guid));
            var row = dt.NewRow();
            row[SysFields.RowId] = rowId;
            row["sys_id"] = sysId;
            row[SysFields.Name] = sysName;
            row["dept_rowid"] = deptRowId;
            var spec = new InsertCommandBuilder(ctx.EmployeeSchema, ctx.DbType).Build(CrudTestContext.ProgId, row);
            ctx.DbAccess.Execute(spec);
        }

        private static void TryDelete(CrudTestContext ctx, Guid rowId)
        {
            try
            {
                var spec = new DeleteCommandBuilder(ctx.EmployeeSchema, ctx.DbType)
                    .Build(CrudTestContext.ProgId, FilterCondition.Equal(SysFields.RowId, rowId));
                ctx.DbAccess.Execute(spec);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetDataTests cleanup of Employee#{rowId} failed — {ex.GetType().Name}: {ex.Message}");
            }
        }
    }
}
