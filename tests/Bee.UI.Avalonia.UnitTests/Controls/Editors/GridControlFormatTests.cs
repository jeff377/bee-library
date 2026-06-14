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
    /// 補強 <see cref="GridControl"/> 覆蓋率：FormatCell 各資料型別路徑、
    /// TryConvertCellValue 成功與例外路徑、TryGetRowId 各欄位狀態、
    /// SetControlState 依版面模式切換 AllowEdit、AddRow、
    /// BuildCellEditor CheckEdit/DateEdit、BuildInteractiveCell 唯讀路徑。
    /// </summary>
    public class GridControlFormatTests
    {
        private static string InvokeFormatCell(
            DataRowView? rowView, string fieldName, string displayFormat, string numberFormat)
        {
            var method = typeof(GridControl).GetMethod(
                "FormatCell", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);
            return (string)method!.Invoke(null,
                new object?[] { rowView, fieldName, displayFormat, numberFormat })!;
        }

        private static bool InvokeTryConvertCellValue(string? value, DataColumn column)
        {
            var method = typeof(GridControl).GetMethod(
                "TryConvertCellValue", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);
            var args = new object?[] { value, column, null };
            return (bool)method!.Invoke(null, args)!;
        }

        private static bool InvokeTryGetRowId(DataRow row, out Guid rowId)
        {
            var method = typeof(GridControl).GetMethod(
                "TryGetRowId", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);
            var args = new object?[] { row, Guid.Empty };
            var result = (bool)method!.Invoke(null, args)!;
            rowId = (Guid)args[1]!;
            return result;
        }

        private static Control InvokeBuildCellEditor(GridControl grid, DataRowView? rowView, LayoutColumn column)
        {
            var method = typeof(GridControl).GetMethod(
                "BuildCellEditor", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);
            return (Control)method!.Invoke(grid, new object?[] { rowView, column })!;
        }

        private static Control InvokeBuildInteractiveCell(GridControl grid, DataRowView? rowView, LayoutColumn column)
        {
            var method = typeof(GridControl).GetMethod(
                "BuildInteractiveCell", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);
            return (Control)method!.Invoke(grid, new object?[] { rowView, column })!;
        }

        private static (GridControl grid, DataTable table) BindSimpleGrid(string colName, Type colType)
        {
            var table = new DataTable("T");
            table.Columns.Add(colName, colType);
            var layout = new LayoutGrid("T", "T");
            layout.Columns!.Add(new LayoutColumn { FieldName = colName, Caption = colName, Visible = true });
            var grid = new GridControl();
            grid.Bind(layout, table);
            return (grid, table);
        }

        [Fact]
        [DisplayName("FormatCell：rowView 為 null 時回傳空字串")]
        public void FormatCell_NullRowView_ReturnsEmptyString()
        {
            var result = InvokeFormatCell(null, "col", string.Empty, string.Empty);

            Assert.Equal(string.Empty, result);
        }

        [Fact]
        [DisplayName("FormatCell：欄位不存在時回傳空字串")]
        public void FormatCell_MissingColumn_ReturnsEmptyString()
        {
            var table = new DataTable("T");
            table.Columns.Add("name", typeof(string));
            table.Rows.Add("Alice");

            var result = InvokeFormatCell(table.DefaultView[0], "nonexistent", string.Empty, string.Empty);

            Assert.Equal(string.Empty, result);
        }

        [Fact]
        [DisplayName("FormatCell：值為 DBNull 時回傳空字串")]
        public void FormatCell_DBNullValue_ReturnsEmptyString()
        {
            var table = new DataTable("T");
            table.Columns.Add("col", typeof(string));
            table.Rows.Add(DBNull.Value);

            var result = InvokeFormatCell(table.DefaultView[0], "col", string.Empty, string.Empty);

            Assert.Equal(string.Empty, result);
        }

        [Fact]
        [DisplayName("FormatCell：DateTime 含時間部分時格式化為 yyyy-MM-dd HH:mm:ss")]
        public void FormatCell_DateTimeWithTime_FormatsWithTimePart()
        {
            var dt = new DateTime(2026, 1, 15, 14, 30, 0, DateTimeKind.Unspecified);
            var table = new DataTable("T");
            table.Columns.Add("ts", typeof(DateTime));
            table.Rows.Add(dt);

            var result = InvokeFormatCell(table.DefaultView[0], "ts", string.Empty, string.Empty);

            Assert.Equal("2026-01-15 14:30:00", result);
        }

        [Fact]
        [DisplayName("FormatCell：DateTime 無時間部分時格式化為 yyyy-MM-dd")]
        public void FormatCell_DateTimeWithNoTime_FormatsDateOnly()
        {
            var dt = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Unspecified);
            var table = new DataTable("T");
            table.Columns.Add("d", typeof(DateTime));
            table.Rows.Add(dt);

            var result = InvokeFormatCell(table.DefaultView[0], "d", string.Empty, string.Empty);

            Assert.Equal("2026-06-01", result);
        }

        [Fact]
        [DisplayName("FormatCell：指定 displayFormat 時套用顯示格式")]
        public void FormatCell_WithDisplayFormat_AppliesDisplayFormat()
        {
            var table = new DataTable("T");
            table.Columns.Add("amount", typeof(decimal));
            table.Rows.Add(100.5m);

            var result = InvokeFormatCell(table.DefaultView[0], "amount", "F2", string.Empty);

            Assert.Equal("100.50", result);
        }

        [Fact]
        [DisplayName("FormatCell：指定 numberFormat 時套用數字格式")]
        public void FormatCell_WithNumberFormat_AppliesNumberFormat()
        {
            var table = new DataTable("T");
            table.Columns.Add("qty", typeof(int));
            table.Rows.Add(42);

            var result = InvokeFormatCell(table.DefaultView[0], "qty", string.Empty, "D5");

            Assert.Equal("00042", result);
        }

        [Fact]
        [DisplayName("TryConvertCellValue：合法整數字串轉換成功回傳 true")]
        public void TryConvertCellValue_ValidIntString_ReturnsTrue()
        {
            var column = new DataColumn("qty", typeof(int));

            var ok = InvokeTryConvertCellValue("5", column);

            Assert.True(ok);
        }

        [Fact]
        [DisplayName("TryConvertCellValue：無法解析字串時回傳 false，不拋例外")]
        public void TryConvertCellValue_InvalidString_ReturnsFalse()
        {
            var column = new DataColumn("qty", typeof(int));

            var ok = InvokeTryConvertCellValue("not-a-number", column);

            Assert.False(ok);
        }

        [Fact]
        [DisplayName("TryGetRowId：無 sys_rowid 欄位時回傳 false")]
        public void TryGetRowId_NoRowIdColumn_ReturnsFalse()
        {
            var table = new DataTable("T");
            table.Columns.Add("name", typeof(string));
            table.Rows.Add("Alice");

            var result = InvokeTryGetRowId(table.Rows[0], out _);

            Assert.False(result);
        }

        [Fact]
        [DisplayName("TryGetRowId：sys_rowid 為 DBNull 時回傳 false")]
        public void TryGetRowId_DBNullValue_ReturnsFalse()
        {
            var table = new DataTable("T");
            table.Columns.Add(SysFields.RowId, typeof(Guid));
            table.Rows.Add(DBNull.Value);

            var result = InvokeTryGetRowId(table.Rows[0], out _);

            Assert.False(result);
        }

        [Fact]
        [DisplayName("TryGetRowId：sys_rowid 為 Guid 類型時回傳 true 並輸出該 Guid")]
        public void TryGetRowId_GuidValue_ReturnsTrueWithCorrectGuid()
        {
            var expected = Guid.NewGuid();
            var table = new DataTable("T");
            table.Columns.Add(SysFields.RowId, typeof(Guid));
            table.Rows.Add(expected);

            var result = InvokeTryGetRowId(table.Rows[0], out var rowId);

            Assert.True(result);
            Assert.Equal(expected, rowId);
        }

        [Fact]
        [DisplayName("TryGetRowId：sys_rowid 為可解析字串時回傳 true 並輸出對應 Guid")]
        public void TryGetRowId_StringGuidValue_ReturnsTrueWithCorrectGuid()
        {
            var expected = Guid.NewGuid();
            var table = new DataTable("T");
            table.Columns.Add(SysFields.RowId, typeof(string));
            table.Rows.Add(expected.ToString());

            var result = InvokeTryGetRowId(table.Rows[0], out var rowId);

            Assert.True(result);
            Assert.Equal(expected, rowId);
        }

        [Fact]
        [DisplayName("SetControlState View 模式：AllowEdit 設為 false")]
        public void SetControlState_ViewMode_SetsAllowEditFalse()
        {
            var layout = new LayoutGrid("T", "T");
            layout.AllowEditModes = FormEditModes.All;
            layout.Columns!.Add(new LayoutColumn { FieldName = "name", Caption = "Name", Visible = true });
            var grid = new GridControl();
            grid.Bind(layout, null);
            grid.AllowEdit = true;

            grid.SetControlState(SingleFormMode.View);

            Assert.False(grid.AllowEdit);
        }

        [Fact]
        [DisplayName("SetControlState Edit 模式且版面允許 All 編輯模式：AllowEdit 設為 true")]
        public void SetControlState_EditModeWithAllowingLayout_SetsAllowEditTrue()
        {
            var layout = new LayoutGrid("T", "T");
            layout.AllowEditModes = FormEditModes.All;
            layout.Columns!.Add(new LayoutColumn { FieldName = "name", Caption = "Name", Visible = true });
            var grid = new GridControl();
            grid.Bind(layout, null);

            grid.SetControlState(SingleFormMode.Edit);

            Assert.True(grid.AllowEdit);
        }

        [Fact]
        [DisplayName("AddRow 有 DataTable 時新增一筆列")]
        public void AddRow_WithNonNullableColumn_AddsRow()
        {
            var (grid, table) = BindSimpleGrid("name", typeof(string));
            table.Columns["name"]!.AllowDBNull = false;

            grid.AddRow();

            Assert.Equal(1, table.Rows.Count);
        }

        [Fact]
        [DisplayName("BuildCellEditor CheckEdit 欄位回傳 CheckBox")]
        public void BuildCellEditor_CheckEdit_ReturnsCheckBox()
        {
            var (grid, table) = BindSimpleGrid("active", typeof(bool));
            table.Rows.Add(true);
            var rowView = table.DefaultView[0];

            var result = InvokeBuildCellEditor(grid, rowView,
                new LayoutColumn("active", "Active", ControlType.CheckEdit));

            Assert.IsType<CheckBox>(result);
        }

        [Fact]
        [DisplayName("BuildCellEditor DateEdit 欄位回傳 DatePicker")]
        public void BuildCellEditor_DateEdit_ReturnsDatePicker()
        {
            var (grid, table) = BindSimpleGrid("hire_date", typeof(DateTime));
            table.Rows.Add(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Unspecified));
            var rowView = table.DefaultView[0];

            var result = InvokeBuildCellEditor(grid, rowView,
                new LayoutColumn("hire_date", "Date", ControlType.DateEdit));

            Assert.IsType<DatePicker>(result);
        }

        [Fact]
        [DisplayName("BuildInteractiveCell CheckEdit 唯讀時回傳已停用的 CheckBox")]
        public void BuildInteractiveCell_CheckEditReadOnly_ReturnsDisabledCheckBox()
        {
            var (grid, table) = BindSimpleGrid("active", typeof(bool));
            table.Rows.Add(false);
            var rowView = table.DefaultView[0];

            var result = InvokeBuildInteractiveCell(grid, rowView,
                new LayoutColumn("active", "Active", ControlType.CheckEdit));

            var checkBox = Assert.IsType<CheckBox>(result);
            Assert.False(checkBox.IsEnabled);
        }

        [Fact]
        [DisplayName("BuildInteractiveCell 非 CheckEdit 唯讀時回傳 TextBlock")]
        public void BuildInteractiveCell_DateEditReadOnly_ReturnsTextBlock()
        {
            var (grid, table) = BindSimpleGrid("hire_date", typeof(DateTime));
            table.Rows.Add(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Unspecified));
            var rowView = table.DefaultView[0];

            var result = InvokeBuildInteractiveCell(grid, rowView,
                new LayoutColumn("hire_date", "Date", ControlType.DateEdit));

            Assert.IsType<TextBlock>(result);
        }
    }
}
