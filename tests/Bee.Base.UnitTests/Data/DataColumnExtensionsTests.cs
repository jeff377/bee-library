using System.ComponentModel;
using System.Data;
using System.Text.Json;
using Bee.Base.Data;
using Bee.Base.Serialization;

namespace Bee.Base.UnitTests.Data
{
    /// <summary>
    /// 驗證欄位語意標記（<see cref="DataColumnExtensions"/>）與 JSON wire 路徑的承接。
    /// 多個 FieldDbType 共用同一個 CLR 型別（Date/DateTime → DateTime、Text/String → string、
    /// Currency/Decimal → decimal），標記存在的目的就是補回這段被抹平的資訊。
    /// </summary>
    public class DataColumnExtensionsTests
    {
        private static JsonSerializerOptions Options()
        {
            var opts = new JsonSerializerOptions();
            opts.Converters.Add(new DataTableJsonConverter());
            return opts;
        }

        [Fact]
        [DisplayName("未標記的欄位 ResolveFieldDbType 應回退為由 CLR 型別反推")]
        public void ResolveFieldDbType_NoMarker_FallsBackToClrType()
        {
            var column = new DataColumn("d", typeof(DateTime));

            Assert.Null(column.GetDeclaredFieldDbType());
            Assert.Equal(FieldDbType.DateTime, column.ResolveFieldDbType());
        }

        [Fact]
        [DisplayName("已標記的欄位 ResolveFieldDbType 應回傳標記值而非反推值")]
        public void ResolveFieldDbType_WithMarker_PrefersMarker()
        {
            var column = new DataColumn("d", typeof(DateTime));
            column.ApplyFieldDbType(FieldDbType.Date);

            Assert.Equal(FieldDbType.Date, column.GetDeclaredFieldDbType());
            Assert.Equal(FieldDbType.Date, column.ResolveFieldDbType());
        }

        [Theory]
        [InlineData(FieldDbType.Date, typeof(DateTime))]
        [InlineData(FieldDbType.Text, typeof(string))]
        [InlineData(FieldDbType.Currency, typeof(decimal))]
        [InlineData(FieldDbType.AutoIncrement, typeof(int))]
        [DisplayName("AddColumn 應在共用 CLR 型別的 FieldDbType 上留下標記")]
        public void AddColumn_SharedClrType_RecordsDeclaredType(FieldDbType dbType, Type expectedClrType)
        {
            var table = new DataTable("t");
            var column = table.AddColumn("f", dbType);

            Assert.Equal(expectedClrType, column.DataType);
            Assert.Equal(dbType, column.ResolveFieldDbType());
        }

        [Fact]
        [DisplayName("AddColumn 的三個 FieldDbType 多載都應留下標記")]
        public void AddColumn_AllFieldDbTypeOverloads_RecordDeclaredType()
        {
            var table = new DataTable("t");

            var byType = table.AddColumn("a", FieldDbType.Date);
            var byTypeAndDefault = table.AddColumn("b", FieldDbType.Date, DateTime.Today);
            var byCaption = table.AddColumn("c", "訂單日期", FieldDbType.Date, DateTime.Today);

            Assert.Equal(FieldDbType.Date, byType.ResolveFieldDbType());
            Assert.Equal(FieldDbType.Date, byTypeAndDefault.ResolveFieldDbType());
            Assert.Equal(FieldDbType.Date, byCaption.ResolveFieldDbType());
        }

        [Fact]
        [DisplayName("SetDateColumns 應把指定欄位標記為 Date，其餘欄位不受影響")]
        public void SetDateColumns_MarksOnlyNamedColumns()
        {
            var table = new DataTable("t");
            table.Columns.Add("order_date", typeof(DateTime));
            table.Columns.Add("created_at", typeof(DateTime));

            table.SetDateColumns("order_date");

            Assert.Equal(FieldDbType.Date, table.Columns["order_date"]!.ResolveFieldDbType());
            Assert.Equal(FieldDbType.DateTime, table.Columns["created_at"]!.ResolveFieldDbType());
        }

        [Fact]
        [DisplayName("SetDateColumns 的欄名比對應不區分大小寫")]
        public void SetDateColumns_ColumnNameMatchIsCaseInsensitive()
        {
            var table = new DataTable("t");
            table.Columns.Add("order_date", typeof(DateTime));

            table.SetDateColumns("ORDER_DATE");

            Assert.Equal(FieldDbType.Date, table.Columns["order_date"]!.ResolveFieldDbType());
        }

        [Fact]
        [DisplayName("SetDateColumns 指定不存在的欄名應擲例外，不可靜默略過")]
        public void SetDateColumns_UnknownColumn_Throws()
        {
            var table = new DataTable("t");
            table.Columns.Add("order_date", typeof(DateTime));

            // 打錯字時「看起來宣告了、實際沒作用」正是標記機制要消除的靜默失敗模式。
            var ex = Assert.Throws<ArgumentException>(() => table.SetDateColumns("oder_date"));
            Assert.Contains("oder_date", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        [DisplayName("JSON round-trip 應保留 Date 標記而非退回 DateTime")]
        public void JsonRoundTrip_PreservesDateMarker()
        {
            var table = new DataTable("t");
            table.AddColumn("order_date", FieldDbType.Date, DateTime.Today);
            table.AddColumn("created_at", FieldDbType.DateTime, DateTime.Now);

            var json = JsonSerializer.Serialize(table, Options());
            var restored = JsonSerializer.Deserialize<DataTable>(json, Options());

            Assert.NotNull(restored);
            Assert.Equal(FieldDbType.Date, restored!.Columns["order_date"]!.ResolveFieldDbType());
            Assert.Equal(FieldDbType.DateTime, restored.Columns["created_at"]!.ResolveFieldDbType());
        }

        [Fact]
        [DisplayName("JSON payload 的 type 欄位應寫出 Date 而非 DateTime")]
        public void JsonPayload_TypeFieldCarriesDate()
        {
            var table = new DataTable("t");
            table.AddColumn("order_date", FieldDbType.Date, DateTime.Today);

            var json = JsonSerializer.Serialize(table, Options());

            Assert.Contains("\"type\":\"Date\"", json, StringComparison.Ordinal);
        }

        [Fact]
        [DisplayName("JSON round-trip 後日曆日欄位的 CLR 型別仍為 DateTime")]
        public void JsonRoundTrip_DateColumnStaysDateTimeClrType()
        {
            var table = new DataTable("t");
            table.AddColumn("order_date", FieldDbType.Date, DateTime.Today);
            var row = table.NewRow();
            row["order_date"] = new DateTime(2026, 7, 25, 0, 0, 0, DateTimeKind.Unspecified);
            table.Rows.Add(row);

            var json = JsonSerializer.Serialize(table, Options());
            var restored = JsonSerializer.Deserialize<DataTable>(json, Options());

            // 標記方案刻意不改 CLR 型別：DataColumn 的字串寫回、RowFilter、Compute
            // 全部依賴 DataType 為 DateTime，改成 DateOnly 會打斷這些既有路徑。
            Assert.Equal(typeof(DateTime), restored!.Columns["order_date"]!.DataType);
            Assert.Equal(new DateTime(2026, 7, 25, 0, 0, 0, DateTimeKind.Unspecified), restored.Rows[0]["order_date"]);
        }

        [Fact]
        [DisplayName("未標記的 DataTable 經 JSON round-trip 行為不變")]
        public void JsonRoundTrip_UnmarkedTable_BehaviourUnchanged()
        {
            var table = new DataTable("t");
            table.Columns.Add("created_at", typeof(DateTime));
            table.Columns.Add("name", typeof(string));

            var json = JsonSerializer.Serialize(table, Options());
            var restored = JsonSerializer.Deserialize<DataTable>(json, Options());

            Assert.Equal(FieldDbType.DateTime, restored!.Columns["created_at"]!.ResolveFieldDbType());
            Assert.Equal(FieldDbType.String, restored.Columns["name"]!.ResolveFieldDbType());
        }
    }
}
