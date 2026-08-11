using System.ComponentModel;
using System.Data;
using Bee.Api.Core.JsonRpc;
using Bee.Api.Core.Messages.Form;
using Bee.Base.Data;
using Bee.Definition.Filters;

namespace Bee.Api.Core.UnitTests
{
    /// <summary>
    /// 驗證 ADR-032 D6 的兩條 wire 不變式：`DataSet` 查 `DateTimeMode`、鬆散 `DateTime` 查 `Kind`。
    /// </summary>
    public class DateTimeWireGuardTests
    {
        private static readonly DateTime s_sample = new DateTime(2026, 1, 1, 9, 0, 0, DateTimeKind.Unspecified);

        private static DataTable AdoNetShapedTable()
        {
            // What DbDataAdapter.Fill / DataTable.Load leave behind: DateTimeMode is the .NET default.
            var table = new DataTable("orders");
            table.Columns.Add(new DataColumn("created_at", typeof(DateTime)));
            table.Rows.Add(s_sample);
            return table;
        }

        [Fact]
        [DisplayName("DataTable 帶 UnspecifiedLocal 欄位時 guard 應擲例外")]
        public void Validate_DataTableWithUnspecifiedLocalColumn_Throws()
        {
            var exception = Assert.Throws<InvalidOperationException>(
                () => DateTimeWireGuard.Validate(AdoNetShapedTable()));

            Assert.Contains("created_at", exception.Message, StringComparison.Ordinal);
            Assert.Contains("UnspecifiedLocal", exception.Message, StringComparison.Ordinal);
        }

        [Fact]
        [DisplayName("正規化後的 DataTable 應通過 guard")]
        public void Validate_NormalizedDataTable_Passes()
        {
            var table = AdoNetShapedTable();
            table.NormalizeDateTimeMode();

            Assert.Null(Record.Exception(() => DateTimeWireGuard.Validate(table)));
        }

        [Fact]
        [DisplayName("AddColumn 建立的 DataTable 應通過 guard")]
        public void Validate_FrameworkBuiltDataTable_Passes()
        {
            var table = new DataTable("orders");
            table.AddColumn("created_at", FieldDbType.DateTime);
            table.AddColumn("order_date", FieldDbType.Date);

            Assert.Null(Record.Exception(() => DateTimeWireGuard.Validate(table)));
        }

        [Fact]
        [DisplayName("DataSet 內任一表違規即應擲例外")]
        public void Validate_DataSetWithOneOffendingTable_Throws()
        {
            using var dataSet = new DataSet("s");
            var clean = new DataTable("clean");
            clean.AddColumn("created_at", FieldDbType.DateTime);
            dataSet.Tables.Add(clean);
            dataSet.Tables.Add(AdoNetShapedTable());

            Assert.Throws<InvalidOperationException>(() => DateTimeWireGuard.Validate(dataSet));
        }

        [Fact]
        [DisplayName("非 DateTime 欄位不受 DateTimeMode 檢查影響")]
        public void Validate_TableWithoutDateTimeColumns_Passes()
        {
            var table = new DataTable("orders");
            table.Columns.Add(new DataColumn("remark", typeof(string)));

            Assert.Null(Record.Exception(() => DateTimeWireGuard.Validate(table)));
        }

        [Theory]
        [InlineData(DateTimeKind.Unspecified)]
        [InlineData(DateTimeKind.Utc)]
        [DisplayName("FilterCondition 值為 Unspecified 或 Utc 時應通過 guard")]
        public void Validate_FilterConditionWithNonLocalKind_Passes(DateTimeKind kind)
        {
            var filter = FilterCondition.Equal("created_at", DateTime.SpecifyKind(s_sample, kind));

            Assert.Null(Record.Exception(() => DateTimeWireGuard.Validate(Request(filter))));
        }

        [Fact]
        [DisplayName("FilterCondition 值為 Kind=Local 時 guard 應擲例外")]
        public void Validate_FilterConditionWithLocalKind_Throws()
        {
            var filter = FilterCondition.Equal("created_at", DateTime.SpecifyKind(s_sample, DateTimeKind.Local));

            var exception = Assert.Throws<InvalidOperationException>(
                () => DateTimeWireGuard.Validate(Request(filter)));

            Assert.Contains("created_at", exception.Message, StringComparison.Ordinal);
        }

        [Fact]
        [DisplayName("巢狀 FilterGroup 內的違規值同樣應被攔下")]
        public void Validate_NestedFilterGroupWithLocalKind_Throws()
        {
            var nested = FilterGroup.All(
                FilterCondition.Equal("status", "open"),
                FilterGroup.All(
                    FilterCondition.Equal("created_at", DateTime.SpecifyKind(s_sample, DateTimeKind.Local))));

            Assert.Throws<InvalidOperationException>(
                () => DateTimeWireGuard.Validate(Request(nested)));
        }

        [Fact]
        [DisplayName("Between 條件的 SecondValue 同樣受檢查")]
        public void Validate_FilterConditionSecondValueWithLocalKind_Throws()
        {
            var filter = new FilterCondition(
                "created_at",
                ComparisonOperator.Between,
                DateTime.SpecifyKind(s_sample, DateTimeKind.Utc),
                DateTime.SpecifyKind(s_sample, DateTimeKind.Local));

            Assert.Throws<InvalidOperationException>(
                () => DateTimeWireGuard.Validate(Request(filter)));
        }

        [Fact]
        [DisplayName("DateOnly 條件值不受 Kind 檢查影響")]
        public void Validate_FilterConditionWithDateOnly_Passes()
        {
            var filter = FilterCondition.Equal("order_date", new DateOnly(2026, 1, 1));

            Assert.Null(Record.Exception(() => DateTimeWireGuard.Validate(Request(filter))));
        }

        [Fact]
        [DisplayName("null 與未涵蓋的型別一律放行")]
        public void Validate_NullOrUnknownValue_Passes()
        {
            Assert.Null(Record.Exception(() => DateTimeWireGuard.Validate(null)));
            Assert.Null(Record.Exception(() => DateTimeWireGuard.Validate("plain string")));
        }

        private static GetListRequest Request(FilterNode? filter) => new GetListRequest { Filter = filter };
    }
}
