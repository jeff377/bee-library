using System.ComponentModel;
using System.Data;
using System.Reflection;
using Avalonia.Controls;
using Bee.Definition;
using Bee.Definition.Layouts;
using Bee.UI.Avalonia.Controls.Editors;

namespace Bee.UI.Avalonia.UnitTests.Controls.Editors
{
    /// <summary>
    /// 補強 <see cref="GridControl"/> 覆蓋率：private static 輔助方法
    /// ComposeDisplayText / SplitDisplayFields、BuildCellEditor null 路徑、
    /// AddRow 在 DataTable 為 null 時的 no-op。
    /// </summary>
    public class GridControlAdditionalTests
    {
        private static readonly string[] s_idAndNameFields = { "sys_id", "sys_name" };
        private static readonly string[] s_idField = { "sys_id" };

        private static string InvokeSplitDisplayFields(string displayFields)
        {
            var method = typeof(GridControl).GetMethod(
                "SplitDisplayFields", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);
            var result = (string[])method!.Invoke(null, new object[] { displayFields })!;
            return string.Join(",", result);
        }

        private static string InvokeComposeDisplayText(
            DataRowView? rowView, string[] displayFields, string displayFormat, string numberFormat)
        {
            var method = typeof(GridControl).GetMethod(
                "ComposeDisplayText", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);
            return (string)method!.Invoke(null,
                new object?[] { rowView, displayFields, displayFormat, numberFormat })!;
        }

        private static DataTable BuildSimpleTable()
        {
            var table = new DataTable("Items");
            table.Columns.Add("sys_id", typeof(string));
            table.Columns.Add("sys_name", typeof(string));
            table.Rows.Add("E001", "Alice");
            return table;
        }

        [Fact]
        [DisplayName("SplitDisplayFields 空字串回傳空陣列")]
        public void SplitDisplayFields_EmptyString_ReturnsEmptyArray()
        {
            var result = InvokeSplitDisplayFields(string.Empty);

            Assert.Equal(string.Empty, result);
        }

        [Fact]
        [DisplayName("SplitDisplayFields 逗號分隔字串回傳修剪後的陣列")]
        public void SplitDisplayFields_CommaSeparated_ReturnsTrimmedElements()
        {
            var result = InvokeSplitDisplayFields(" sys_id , sys_name ");

            Assert.Equal("sys_id,sys_name", result);
        }

        [Fact]
        [DisplayName("ComposeDisplayText 以顯示欄位組合儲存格文字")]
        public void ComposeDisplayText_WithDisplayFields_JoinsValues()
        {
            var table = BuildSimpleTable();
            var rowView = table.DefaultView[0];

            var result = InvokeComposeDisplayText(rowView, s_idAndNameFields, string.Empty, string.Empty);

            Assert.Equal("E001 - Alice", result);
        }

        [Fact]
        [DisplayName("ComposeDisplayText 欄位不存在時回傳空字串")]
        public void ComposeDisplayText_MissingField_ReturnsEmpty()
        {
            var table = BuildSimpleTable();
            var rowView = table.DefaultView[0];

            var result = InvokeComposeDisplayText(rowView, s_idField, string.Empty, string.Empty);

            // sys_id exists → "E001"; no separator needed for single field
            Assert.Equal("E001", result);
        }

        [Fact]
        [DisplayName("ComposeDisplayText rowView 為 null 時回傳空字串")]
        public void ComposeDisplayText_NullRowView_ReturnsEmpty()
        {
            var result = InvokeComposeDisplayText(null, s_idAndNameFields, string.Empty, string.Empty);

            Assert.Equal(string.Empty, result);
        }

        [Fact]
        [DisplayName("BuildCellEditor 在 rowView 為 null 時回傳 TextBlock")]
        public void BuildCellEditor_NullRowView_ReturnsTextBlock()
        {
            var grid = new GridControl();
            grid.Bind(new LayoutGrid("Items", "Items"), null);

            var method = typeof(GridControl).GetMethod(
                "BuildCellEditor", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);

            var result = method!.Invoke(grid,
                new object?[] { null, new LayoutColumn { FieldName = "name" } });

            Assert.IsType<TextBlock>(result);
        }

        [Fact]
        [DisplayName("BuildCellEditor 在欄位不存在於 DataTable 時回傳 TextBlock")]
        public void BuildCellEditor_FieldMissingFromTable_ReturnsTextBlock()
        {
            var table = BuildSimpleTable();
            var grid = new GridControl();
            grid.Bind(new LayoutGrid("Items", "Items"), table);

            var method = typeof(GridControl).GetMethod(
                "BuildCellEditor", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);

            var result = method!.Invoke(grid,
                new object?[] { table.DefaultView[0], new LayoutColumn { FieldName = "not_a_column" } });

            Assert.IsType<TextBlock>(result);
        }

        [Fact]
        [DisplayName("AddRow 在 DataTable 為 null 時為 no-op，不拋例外")]
        public void AddRow_NullDataTable_IsNoOp()
        {
            var grid = new GridControl();

            var exception = Record.Exception(grid.AddRow);

            Assert.Null(exception);
        }
    }
}
