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
    /// <see cref="FormBusinessObject.Save"/> 的 <c>[DbFact]</c> 整合測試:
    /// 涵蓋 Added → INSERT、Modified → UPDATE、Deleted → DELETE 三種 row state
    /// 派發,以及無變更時拋出 <see cref="InvalidOperationException"/>。
    /// </summary>
    public class FormBusinessObjectSaveTests : IClassFixture<SharedDbFixture>
    {
        private readonly SharedDbFixture _fx;

        public FormBusinessObjectSaveTests(SharedDbFixture fx) { _fx = fx; }

        [Fact]
        [DisplayName("Save 傳入 null args 應拋 ArgumentNullException")]
        public void Save_NullArgs_Throws()
        {
            var bo = new FormBusinessObject(TestBeeContext.Create(_fx), Guid.NewGuid(),
                CrudTestContext.ProgId);
            Assert.Throws<ArgumentNullException>(() => bo.Save(null!));
        }

        [Fact]
        [DisplayName("Save args.DataSet = null 應拋 ArgumentException")]
        public void Save_NullDataSet_Throws()
        {
            var bo = new FormBusinessObject(TestBeeContext.Create(_fx), Guid.NewGuid(),
                CrudTestContext.ProgId);
            Assert.Throws<ArgumentException>(() => bo.Save(new SaveArgs { DataSet = null }));
        }

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("SQLite:Save 帶 Added row 應 INSERT 並回傳 refreshed DataSet")]
        public void Save_Sqlite_AddedRow_Inserts()
            => RunSaveAddedRow(DatabaseType.SQLite);

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("SQL Server:Save 帶 Added row 應 INSERT 並回傳 refreshed DataSet")]
        public void Save_SqlServer_AddedRow_Inserts()
            => RunSaveAddedRow(DatabaseType.SQLServer);

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("SQLite:Save 帶 Modified row 應 UPDATE 並回傳 refreshed DataSet")]
        public void Save_Sqlite_ModifiedRow_Updates()
            => RunSaveModifiedRow(DatabaseType.SQLite);

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("SQLite:Save 帶 Deleted row 應 DELETE")]
        public void Save_Sqlite_DeletedRow_Deletes()
            => RunSaveDeletedRow(DatabaseType.SQLite);

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("SQLite:Save DataSet 無任何變更應拋 InvalidOperationException")]
        public void Save_Sqlite_NoChanges_Throws()
            => RunSaveNoChangesThrows(DatabaseType.SQLite);

        private void RunSaveAddedRow(DatabaseType dbType)
        {
            var ctx = new CrudTestContext(_fx, dbType);
            string runId = Guid.NewGuid().ToString("N")[..8];
            var rowId = Guid.NewGuid();

            try
            {
                var dataSet = ctx.Repository.GetNewData();
                var master = dataSet.Tables[CrudTestContext.ProgId]!;
                // GetNewData 已預填 sys_rowid + Added,我們覆寫 rowId 與其他欄位。
                master.Rows[0][SysFields.RowId] = rowId;
                master.Rows[0]["sys_id"] = $"S{runId}";
                master.Rows[0][SysFields.Name] = "新增員工";

                var result = ctx.CreateBo().Save(new SaveArgs { DataSet = dataSet });

                Assert.NotNull(result.DataSet);
                Assert.Equal(CrudTestContext.ProgId, result.DataSet!.DataSetName);
                Assert.Equal(1, result.AffectedRows[CrudTestContext.ProgId]);

                // 直接 GetData 重新讀回,確認資料已寫入
                var reloaded = ctx.CreateBo().GetData(new GetDataArgs { RowId = rowId });
                Assert.NotNull(reloaded.DataSet);
                Assert.Equal("新增員工",
                    reloaded.DataSet!.Tables[CrudTestContext.ProgId]!.Rows[0][SysFields.Name]);
            }
            finally
            {
                TryDelete(ctx, rowId);
            }
        }

        private void RunSaveModifiedRow(DatabaseType dbType)
        {
            var ctx = new CrudTestContext(_fx, dbType);
            string runId = Guid.NewGuid().ToString("N")[..8];
            var rowId = Guid.NewGuid();

            try
            {
                InsertEmployee(ctx, rowId, $"M{runId}", "原始名稱", Guid.Empty);

                var loaded = ctx.CreateBo().GetData(new GetDataArgs { RowId = rowId });
                Assert.NotNull(loaded.DataSet);

                loaded.DataSet!.Tables[CrudTestContext.ProgId]!.Rows[0][SysFields.Name] = "已修改名稱";

                var result = ctx.CreateBo().Save(new SaveArgs { DataSet = loaded.DataSet });

                Assert.Equal(1, result.AffectedRows[CrudTestContext.ProgId]);

                var reloaded = ctx.CreateBo().GetData(new GetDataArgs { RowId = rowId });
                Assert.Equal("已修改名稱",
                    reloaded.DataSet!.Tables[CrudTestContext.ProgId]!.Rows[0][SysFields.Name]);
            }
            finally
            {
                TryDelete(ctx, rowId);
            }
        }

        private void RunSaveDeletedRow(DatabaseType dbType)
        {
            var ctx = new CrudTestContext(_fx, dbType);
            string runId = Guid.NewGuid().ToString("N")[..8];
            var rowId = Guid.NewGuid();

            try
            {
                InsertEmployee(ctx, rowId, $"D{runId}", "待刪除", Guid.Empty);

                var loaded = ctx.CreateBo().GetData(new GetDataArgs { RowId = rowId });
                Assert.NotNull(loaded.DataSet);

                loaded.DataSet!.Tables[CrudTestContext.ProgId]!.Rows[0].Delete();

                var result = ctx.CreateBo().Save(new SaveArgs { DataSet = loaded.DataSet });
                Assert.Equal(1, result.AffectedRows[CrudTestContext.ProgId]);

                // 重新讀:已不存在
                var reloaded = ctx.CreateBo().GetData(new GetDataArgs { RowId = rowId });
                Assert.Null(reloaded.DataSet);
            }
            finally
            {
                TryDelete(ctx, rowId);
            }
        }

        private void RunSaveNoChangesThrows(DatabaseType dbType)
        {
            var ctx = new CrudTestContext(_fx, dbType);
            string runId = Guid.NewGuid().ToString("N")[..8];
            var rowId = Guid.NewGuid();

            try
            {
                InsertEmployee(ctx, rowId, $"N{runId}", "未動", Guid.Empty);

                var loaded = ctx.CreateBo().GetData(new GetDataArgs { RowId = rowId });
                Assert.NotNull(loaded.DataSet);

                // 沒有任何變更
                Assert.Throws<InvalidOperationException>(() =>
                    ctx.CreateBo().Save(new SaveArgs { DataSet = loaded.DataSet }));
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
                Console.WriteLine($"SaveTests cleanup of Employee#{rowId} failed — {ex.GetType().Name}: {ex.Message}");
            }
        }
    }
}
