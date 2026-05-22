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
    /// 串接 4 個 CRUD action 的整合測試:覆蓋「新增存檔」與「修改存檔」
    /// 兩個完整流程,證明 <c>GetNewData → Save → GetData</c> 與
    /// <c>GetData → 修改 → Save → GetData</c> 在實體 DB 上串得起來。
    /// </summary>
    public class FormBusinessObjectCrudFlowTests : IClassFixture<SharedDbFixture>
    {
        private readonly SharedDbFixture _fx;

        public FormBusinessObjectCrudFlowTests(SharedDbFixture fx) { _fx = fx; }

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("SQLite:新增存檔流程 GetNewData → 填欄位 → Save → GetData 應比對相符")]
        public void NewAndSaveFlow_Sqlite()
            => RunNewAndSaveFlow(DatabaseType.SQLite);

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("SQL Server:新增存檔流程 GetNewData → 填欄位 → Save → GetData 應比對相符")]
        public void NewAndSaveFlow_SqlServer()
            => RunNewAndSaveFlow(DatabaseType.SQLServer);

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("SQLite:修改存檔流程 GetData → 修改 → Save → GetData 應比對相符")]
        public void LoadAndSaveFlow_Sqlite()
            => RunLoadAndSaveFlow(DatabaseType.SQLite);

        private void RunNewAndSaveFlow(DatabaseType dbType)
        {
            var ctx = new CrudTestContext(_fx, dbType);
            string runId = Guid.NewGuid().ToString("N")[..8];
            var bo = ctx.CreateBo();

            // 1. GetNewData
            var skeleton = bo.GetNewData(new GetNewDataArgs()).DataSet;
            Assert.NotNull(skeleton);
            var master = skeleton!.Tables[CrudTestContext.ProgId]!;
            var rowId = (Guid)master.Rows[0][SysFields.RowId];

            try
            {
                // 2. 填欄位
                master.Rows[0]["sys_id"] = $"F{runId}";
                master.Rows[0][SysFields.Name] = "整合流程員工";

                // 3. Save
                var saveResult = bo.Save(new SaveArgs { DataSet = skeleton });
                Assert.Equal(1, saveResult.AffectedRows[CrudTestContext.ProgId]);

                // 4. GetData 重新讀回,確認欄位一致
                var reloaded = bo.GetData(new GetDataArgs { RowId = rowId }).DataSet;
                Assert.NotNull(reloaded);
                Assert.Equal($"F{runId}", reloaded!.Tables[CrudTestContext.ProgId]!.Rows[0]["sys_id"]);
                Assert.Equal("整合流程員工", reloaded.Tables[CrudTestContext.ProgId]!.Rows[0][SysFields.Name]);
            }
            finally
            {
                TryDelete(ctx, rowId);
            }
        }

        private void RunLoadAndSaveFlow(DatabaseType dbType)
        {
            var ctx = new CrudTestContext(_fx, dbType);
            string runId = Guid.NewGuid().ToString("N")[..8];
            var rowId = Guid.NewGuid();
            var bo = ctx.CreateBo();

            try
            {
                // 先以底層 INSERT 種子一筆。
                InsertEmployee(ctx, rowId, $"L{runId}", "原始名稱", Guid.Empty);

                // 1. GetData
                var loaded = bo.GetData(new GetDataArgs { RowId = rowId }).DataSet;
                Assert.NotNull(loaded);

                // 2. 修改
                loaded!.Tables[CrudTestContext.ProgId]!.Rows[0][SysFields.Name] = "修改後名稱";

                // 3. Save
                var saveResult = bo.Save(new SaveArgs { DataSet = loaded });
                Assert.Equal(1, saveResult.AffectedRows[CrudTestContext.ProgId]);

                // 4. GetData 重新讀回
                var reloaded = bo.GetData(new GetDataArgs { RowId = rowId }).DataSet;
                Assert.Equal("修改後名稱",
                    reloaded!.Tables[CrudTestContext.ProgId]!.Rows[0][SysFields.Name]);
            }
            finally
            {
                TryDelete(ctx, rowId);
            }
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
                Console.WriteLine($"CrudFlowTests cleanup of Employee#{rowId} failed — {ex.GetType().Name}: {ex.Message}");
            }
        }
    }
}
