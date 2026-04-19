using System.ComponentModel;
using System.Data;
using Bee.Api.Core.MessagePack;

namespace Bee.Api.Core.UnitTests
{
    /// <summary>
    /// DataSetFormatter / DataTableFormatter 的 null round-trip 測試，涵蓋 WriteNil/TryReadNil 路徑。
    /// </summary>
    public class MessagePackNullFormatterTests
    {
        [Fact]
        [DisplayName("DataSet null 序列化應可反序列化回 null")]
        public void DataSet_Null_Serialize_RoundTrip_ReturnsNull()
        {
            DataSet? original = null;

            var bytes = MessagePackHelper.Serialize(original);
            var restored = MessagePackHelper.Deserialize<DataSet?>(bytes);

            Assert.Null(restored);
        }

        [Fact]
        [DisplayName("DataTable null 序列化應可反序列化回 null")]
        public void DataTable_Null_Serialize_RoundTrip_ReturnsNull()
        {
            DataTable? original = null;

            var bytes = MessagePackHelper.Serialize(original);
            var restored = MessagePackHelper.Deserialize<DataTable?>(bytes);

            Assert.Null(restored);
        }

        [Fact]
        [DisplayName("空 DataSet 序列化應可還原為空集合")]
        public void DataSet_Empty_Serialize_RoundTrip_ReturnsEmpty()
        {
            var original = new DataSet("Empty");

            var bytes = MessagePackHelper.Serialize(original);
            var restored = MessagePackHelper.Deserialize<DataSet>(bytes);

            Assert.NotNull(restored);
            Assert.Empty(restored.Tables);
        }

        [Fact]
        [DisplayName("空 DataTable 序列化應可還原為空資料表")]
        public void DataTable_Empty_Serialize_RoundTrip_ReturnsEmpty()
        {
            var original = new DataTable("Empty");
            original.Columns.Add("Id", typeof(int));

            var bytes = MessagePackHelper.Serialize(original);
            var restored = MessagePackHelper.Deserialize<DataTable>(bytes);

            Assert.NotNull(restored);
            Assert.Empty(restored.Rows);
            Assert.Single(restored.Columns.Cast<DataColumn>());
        }
    }
}
