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
    /// <see cref="FormBusinessObject.Delete"/> 的 <c>[DbFact]</c> 整合測試:
    /// 種子一筆 → Delete(rowId) → 確認回傳 RowsAffected,並以 SELECT 驗證
    /// 已不存在;對不存在 RowId 呼叫應回 0。
    /// </summary>
    public class FormBusinessObjectDeleteTests : IClassFixture<SharedDbFixture>
    {
        private readonly SharedDbFixture _fx;

        public FormBusinessObjectDeleteTests(SharedDbFixture fx) { _fx = fx; }

        [Fact]
        [DisplayName("Delete 傳入 null 應拋 ArgumentNullException")]
        public void Delete_NullArgs_Throws()
        {
            var bo = new FormBusinessObject(TestBeeContext.Create(_fx), Guid.NewGuid(),
                CrudTestContext.ProgId);
            Assert.Throws<ArgumentNullException>(() => bo.Delete(null!));
        }

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("SQLite:Delete 已存在的 Employee 應回傳 RowsAffected=1 並從 DB 移除")]
        public void Delete_Sqlite_ExistingRow_Removes()
            => RunDeleteExistingRow(DatabaseType.SQLite);

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("SQL Server:Delete 已存在的 Employee 應回傳 RowsAffected=1 並從 DB 移除")]
        public void Delete_SqlServer_ExistingRow_Removes()
            => RunDeleteExistingRow(DatabaseType.SQLServer);

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("SQLite:Delete 不存在的 RowId 應回傳 RowsAffected=0")]
        public void Delete_Sqlite_NonExistentRow_ReturnsZero()
            => RunDeleteNonExistentRow(DatabaseType.SQLite);

        private void RunDeleteExistingRow(DatabaseType dbType)
        {
            var ctx = new CrudTestContext(_fx, dbType);
            string runId = Guid.NewGuid().ToString("N")[..8];
            var rowId = Guid.NewGuid();

            try
            {
                InsertEmployee(ctx, rowId, $"X{runId}", "待刪", Guid.Empty);

                var result = ctx.CreateBo().Delete(new DeleteArgs { RowId = rowId });
                Assert.Equal(1, result.RowsAffected);

                var reloaded = ctx.CreateBo().GetData(new GetDataArgs { RowId = rowId });
                Assert.Null(reloaded.DataSet);
            }
            finally
            {
                TryDelete(ctx, rowId);
            }
        }

        private void RunDeleteNonExistentRow(DatabaseType dbType)
        {
            var ctx = new CrudTestContext(_fx, dbType);

            var result = ctx.CreateBo().Delete(new DeleteArgs { RowId = Guid.NewGuid() });
            Assert.Equal(0, result.RowsAffected);
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
                Console.WriteLine($"DeleteTests cleanup of Employee#{rowId} failed — {ex.GetType().Name}: {ex.Message}");
            }
        }
    }
}
