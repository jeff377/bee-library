using System.ComponentModel;
using System.Data;
using Bee.Business.Form;
using Bee.Definition;
using Bee.Definition.Database;
using Bee.Tests.Shared;

namespace Bee.Business.UnitTests.Form
{
    /// <summary>
    /// <see cref="FormBusinessObject.GetNewData"/> 的 <c>[DbFact]</c> 整合測試:
    /// 確認 skeleton DataSet 含 server-issued <c>sys_rowid</c>、master row state
    /// 為 <see cref="DataRowState.Added"/>,並維持 <c>DataSetName == ProgId</c>
    /// 與 <c>Tables[ProgId]</c> 即 Master 的框架不變式。
    /// </summary>
    public class FormBusinessObjectGetNewDataTests : IClassFixture<SharedDbFixture>
    {
        private readonly SharedDbFixture _fx;

        public FormBusinessObjectGetNewDataTests(SharedDbFixture fx) { _fx = fx; }

        [Fact]
        [DisplayName("GetNewData 傳入 null 應拋 ArgumentNullException")]
        public void GetNewData_NullArgs_Throws()
        {
            var bo = new FormBusinessObject(TestBeeContext.Create(_fx), Guid.NewGuid(),
                CrudTestContext.ProgId);
            Assert.Throws<ArgumentNullException>(() => bo.GetNewData(null!));
        }

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("SQLite:GetNewData 應回傳 skeleton DataSet 並 server-side 預填 sys_rowid")]
        public void GetNewData_Sqlite_ReturnsSkeletonWithServerRowId()
            => RunSkeletonShape(DatabaseType.SQLite);

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("SQL Server:GetNewData 應回傳 skeleton DataSet 並 server-side 預填 sys_rowid")]
        public void GetNewData_SqlServer_ReturnsSkeletonWithServerRowId()
            => RunSkeletonShape(DatabaseType.SQLServer);

        private void RunSkeletonShape(DatabaseType dbType)
        {
            var ctx = new CrudTestContext(_fx, dbType);
            var bo = ctx.CreateBo();

            var result = bo.GetNewData(new GetNewDataArgs());

            Assert.NotNull(result.DataSet);

            // 框架不變式:DataSet.DataSetName == ProgId,Tables[ProgId] 即 Master。
            Assert.Equal(CrudTestContext.ProgId, result.DataSet!.DataSetName);
            Assert.True(result.DataSet.Tables.Contains(CrudTestContext.ProgId));

            var master = result.DataSet.Tables[CrudTestContext.ProgId]!;
            Assert.Single(master.Rows);

            // server-issued sys_rowid 不應為 Guid.Empty,且 row state 為 Added
            var rowId = (Guid)master.Rows[0][SysFields.RowId];
            Assert.NotEqual(Guid.Empty, rowId);
            Assert.Equal(DataRowState.Added, master.Rows[0].RowState);
        }
    }
}
