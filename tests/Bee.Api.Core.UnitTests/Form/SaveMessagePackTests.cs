using System.ComponentModel;
using System.Data;
using Bee.Api.Core.MessagePack;
using Bee.Api.Core.Messages.Form;
using Bee.Definition;

namespace Bee.Api.Core.UnitTests.Form
{
    /// <summary>
    /// <see cref="SaveRequest"/> / <see cref="SaveResponse"/> 的 MessagePack
    /// round-trip 驗證。重點:三種 <see cref="DataRowState"/>(Added /
    /// Modified / Deleted)混合的 DataSet 還原與 AffectedRows 字典。
    /// </summary>
    public class SaveMessagePackTests
    {
        [Fact]
        [DisplayName("SaveRequest DataSet 混合 Added/Modified/Deleted row state 應完整還原")]
        public void SaveRequest_DataSet_PreservesMixedRowStates()
        {
            var dataSet = new DataSet("Employee");

            var master = new DataTable("Employee");
            var rowIdColumn = master.Columns.Add(SysFields.RowId, typeof(Guid));
            master.Columns.Add(SysFields.Name, typeof(string));
            master.PrimaryKey = new[] { rowIdColumn };

            var unchangedId = Guid.NewGuid();
            var modifiedId = Guid.NewGuid();
            var deletedId = Guid.NewGuid();
            var addedId = Guid.NewGuid();

            master.Rows.Add(unchangedId, "保持不變");
            master.Rows.Add(modifiedId, "原始名稱");
            master.Rows.Add(deletedId, "待刪除");
            master.AcceptChanges();

            // 一個 Modified row(改名稱),一個 Deleted row,一個 Added row
            master.Rows.Find(modifiedId)![SysFields.Name] = "已修改名稱";
            master.Rows.Find(deletedId)!.Delete();
            master.Rows.Add(addedId, "全新一筆");

            dataSet.Tables.Add(master);

            var request = new SaveRequest { DataSet = dataSet };

            var bytes = MessagePackCodec.Serialize(request);
            var restored = MessagePackCodec.Deserialize<SaveRequest>(bytes);

            Assert.NotNull(restored);
            Assert.NotNull(restored!.DataSet);
            var restoredMaster = restored.DataSet!.Tables["Employee"]!;

            // 還原後應該保有所有 row(含 Deleted),且 RowState 對齊
            DataRow? FindByCurrentOrOriginal(Guid id)
            {
                foreach (DataRow r in restoredMaster.Rows)
                {
                    var version = r.RowState == DataRowState.Deleted
                        ? DataRowVersion.Original
                        : DataRowVersion.Current;
                    if (r[SysFields.RowId, version] is Guid g && g == id)
                        return r;
                }
                return null;
            }

            Assert.Equal(DataRowState.Unchanged, FindByCurrentOrOriginal(unchangedId)!.RowState);
            Assert.Equal(DataRowState.Modified, FindByCurrentOrOriginal(modifiedId)!.RowState);
            Assert.Equal(DataRowState.Deleted, FindByCurrentOrOriginal(deletedId)!.RowState);
            Assert.Equal(DataRowState.Added, FindByCurrentOrOriginal(addedId)!.RowState);
        }

        [Fact]
        [DisplayName("SaveResponse 帶 AffectedRows 與 refreshed DataSet 應 round-trip 還原")]
        public void SaveResponse_RoundTrip_PreservesAffectedRowsAndDataSet()
        {
            var dataSet = new DataSet("Employee");
            var master = new DataTable("Employee");
            master.Columns.Add(SysFields.RowId, typeof(Guid));
            master.Columns.Add(SysFields.Name, typeof(string));
            master.Rows.Add(Guid.NewGuid(), "回寫後資料");
            dataSet.Tables.Add(master);
            dataSet.AcceptChanges();

            var response = new SaveResponse
            {
                DataSet = dataSet,
                AffectedRows = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Employee"] = 2,
                    ["EmployeeDept"] = 3,
                },
            };

            var bytes = MessagePackCodec.Serialize(response);
            var restored = MessagePackCodec.Deserialize<SaveResponse>(bytes);

            Assert.NotNull(restored);
            Assert.NotNull(restored!.DataSet);
            Assert.Single(restored.DataSet!.Tables["Employee"]!.Rows);
            Assert.Equal(2, restored.AffectedRows["Employee"]);
            Assert.Equal(3, restored.AffectedRows["EmployeeDept"]);
        }

        [Fact]
        [DisplayName("SaveResponse AffectedRows 為空字典應 round-trip 為空字典")]
        public void SaveResponse_EmptyAffectedRows_RoundTrip()
        {
            var response = new SaveResponse();

            var bytes = MessagePackCodec.Serialize(response);
            var restored = MessagePackCodec.Deserialize<SaveResponse>(bytes);

            Assert.NotNull(restored);
            Assert.NotNull(restored!.AffectedRows);
            Assert.Empty(restored.AffectedRows);
            Assert.Null(restored.DataSet);
        }
    }
}
