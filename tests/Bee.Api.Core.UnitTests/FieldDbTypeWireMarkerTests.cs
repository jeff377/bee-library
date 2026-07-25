using System.ComponentModel;
using System.Data;
using System.Text.Json;
using Bee.Api.Core.MessagePack;
using Bee.Base.Data;
using Bee.Base.Serialization;

namespace Bee.Api.Core.UnitTests
{
    /// <summary>
    /// 驗證欄位語意標記在 MessagePack wire 路徑的承接，以及與 JSON 路徑的一致性。
    /// wire 序列化有 MessagePack 與 JSON 兩份平行實作（分居 Bee.Api.Core 與 Bee.Base），
    /// 改一份忘一份是最可能的失誤，且部署上通常只跑一種 PayloadFormat、切換時才炸——
    /// 故格式間一致性必須有測試釘住。
    /// </summary>
    public class FieldDbTypeWireMarkerTests
    {
        private static JsonSerializerOptions JsonOptions()
        {
            var opts = new JsonSerializerOptions();
            opts.Converters.Add(new DataTableJsonConverter());
            return opts;
        }

        private static DataTable BuildTable()
        {
            var table = new DataTable("orders");
            table.AddColumn("order_date", FieldDbType.Date, DateTime.Today);
            table.AddColumn("created_at", FieldDbType.DateTime, DateTime.Now);
            table.AddColumn("remark", FieldDbType.Text, string.Empty);
            table.AddColumn("amount", FieldDbType.Currency, 0m);
            return table;
        }

        [Fact]
        [DisplayName("MessagePack round-trip 應保留 Date 標記而非退回 DateTime")]
        public void MessagePackRoundTrip_PreservesDateMarker()
        {
            var table = BuildTable();

            var bytes = MessagePackCodec.Serialize(table);
            var restored = MessagePackCodec.Deserialize<DataTable>(bytes);

            Assert.NotNull(restored);
            Assert.Equal(FieldDbType.Date, restored!.Columns["order_date"]!.ResolveFieldDbType());
            Assert.Equal(FieldDbType.DateTime, restored.Columns["created_at"]!.ResolveFieldDbType());
        }

        [Fact]
        [DisplayName("SerializableDataTable 的 wire 欄位型別應為 Date")]
        public void SerializableDataTable_CarriesDateOnWire()
        {
            var table = BuildTable();

            var sdt = SerializableDataTable.FromDataTable(table);

            var column = Assert.Single(sdt.Columns, c => c.ColumnName == "order_date");
            Assert.Equal(FieldDbType.Date, column.DataType);
        }

        [Fact]
        [DisplayName("MessagePack round-trip 後日曆日欄位的 CLR 型別仍為 DateTime")]
        public void MessagePackRoundTrip_DateColumnStaysDateTimeClrType()
        {
            var table = BuildTable();
            var row = table.NewRow();
            row["order_date"] = new DateTime(2026, 7, 25, 0, 0, 0, DateTimeKind.Unspecified);
            table.Rows.Add(row);

            var bytes = MessagePackCodec.Serialize(table);
            var restored = MessagePackCodec.Deserialize<DataTable>(bytes);

            Assert.Equal(typeof(DateTime), restored!.Columns["order_date"]!.DataType);
            Assert.Equal(new DateTime(2026, 7, 25, 0, 0, 0, DateTimeKind.Unspecified), restored.Rows[0]["order_date"]);
        }

        [Fact]
        [DisplayName("兩種 wire 格式 round-trip 後的欄位標記應完全相同")]
        public void BothWireFormats_ProduceIdenticalMarkers()
        {
            var table = BuildTable();

            var viaMessagePack = MessagePackCodec.Deserialize<DataTable>(MessagePackCodec.Serialize(table));
            var viaJson = JsonSerializer.Deserialize<DataTable>(
                JsonSerializer.Serialize(table, JsonOptions()), JsonOptions());

            Assert.NotNull(viaMessagePack);
            Assert.NotNull(viaJson);

            foreach (DataColumn source in table.Columns)
            {
                var expected = source.ResolveFieldDbType();
                Assert.Equal(expected, viaMessagePack!.Columns[source.ColumnName]!.ResolveFieldDbType());
                Assert.Equal(expected, viaJson!.Columns[source.ColumnName]!.ResolveFieldDbType());
            }
        }

        [Fact]
        [DisplayName("未標記的 DataTable 經 MessagePack round-trip 行為不變")]
        public void MessagePackRoundTrip_UnmarkedTable_BehaviourUnchanged()
        {
            var table = new DataTable("t");
            table.Columns.Add("created_at", typeof(DateTime));
            table.Columns.Add("name", typeof(string));

            var restored = MessagePackCodec.Deserialize<DataTable>(MessagePackCodec.Serialize(table));

            Assert.Equal(FieldDbType.DateTime, restored!.Columns["created_at"]!.ResolveFieldDbType());
            Assert.Equal(FieldDbType.String, restored.Columns["name"]!.ResolveFieldDbType());
        }

        [Fact]
        [DisplayName("DataSet 經 MessagePack round-trip 應逐表保留欄位標記")]
        public void DataSetRoundTrip_PreservesMarkersPerTable()
        {
            var dataSet = new DataSet("ds");
            dataSet.Tables.Add(BuildTable());

            var restored = MessagePackCodec.Deserialize<DataSet>(MessagePackCodec.Serialize(dataSet));

            Assert.NotNull(restored);
            Assert.Equal(FieldDbType.Date, restored!.Tables["orders"]!.Columns["order_date"]!.ResolveFieldDbType());
        }
    }
}
