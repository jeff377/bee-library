using System.ComponentModel;
using System.Data;
using Bee.Api.Core.MessagePack;
using Bee.Api.Core.Messages.Form;
using Bee.Definition;

namespace Bee.Api.Core.UnitTests.Form
{
    /// <summary>
    /// <see cref="GetNewDataRequest"/> / <see cref="GetNewDataResponse"/> 的
    /// MessagePack round-trip 驗證。重點:DataSet skeleton(空 detail
    /// table、master 1 row Added 狀態)經 wire 還原後結構與 RowState 一致。
    /// </summary>
    public class GetNewDataMessagePackTests
    {
        [Fact]
        [DisplayName("GetNewDataRequest 屬性為空,應可正常 round-trip")]
        public void GetNewDataRequest_Empty_RoundTrips()
        {
            var request = new GetNewDataRequest();

            var bytes = MessagePackCodec.Serialize(request);
            var restored = MessagePackCodec.Deserialize<GetNewDataRequest>(bytes);

            Assert.NotNull(restored);
        }

        [Fact]
        [DisplayName("GetNewDataResponse 帶 master Added row + 空 detail table 應完整還原 RowState 與 schema")]
        public void GetNewDataResponse_SkeletonDataSet_RoundTripPreservesAddedRowState()
        {
            var dataSet = new DataSet("Employee");

            var master = new DataTable("Employee");
            master.Columns.Add(SysFields.RowId, typeof(Guid));
            master.Columns.Add(SysFields.Name, typeof(string));
            var masterRowId = Guid.NewGuid();
            master.Rows.Add(masterRowId, "預設員工");
            dataSet.Tables.Add(master);

            var detail = new DataTable("EmployeeDept");
            detail.Columns.Add(SysFields.RowId, typeof(Guid));
            detail.Columns.Add(SysFields.MasterRowId, typeof(Guid));
            dataSet.Tables.Add(detail);

            var response = new GetNewDataResponse { DataSet = dataSet };

            var bytes = MessagePackCodec.Serialize(response);
            var restored = MessagePackCodec.Deserialize<GetNewDataResponse>(bytes);

            Assert.NotNull(restored);
            Assert.NotNull(restored!.DataSet);
            Assert.Equal(2, restored.DataSet!.Tables.Count);

            var restoredMaster = restored.DataSet.Tables["Employee"]!;
            Assert.Single(restoredMaster.Rows);
            Assert.Equal(masterRowId, (Guid)restoredMaster.Rows[0][SysFields.RowId]);
            Assert.Equal("預設員工", restoredMaster.Rows[0][SysFields.Name]);
            Assert.Equal(DataRowState.Added, restoredMaster.Rows[0].RowState);

            var restoredDetail = restored.DataSet.Tables["EmployeeDept"]!;
            Assert.Empty(restoredDetail.Rows);
            Assert.Equal(2, restoredDetail.Columns.Count);
        }

        [Fact]
        [DisplayName("GetNewDataResponse.DataSet = null 應 round-trip 為 null")]
        public void GetNewDataResponse_NullDataSet_RoundTrip()
        {
            var response = new GetNewDataResponse { DataSet = null };

            var bytes = MessagePackCodec.Serialize(response);
            var restored = MessagePackCodec.Deserialize<GetNewDataResponse>(bytes);

            Assert.NotNull(restored);
            Assert.Null(restored!.DataSet);
        }
    }
}
