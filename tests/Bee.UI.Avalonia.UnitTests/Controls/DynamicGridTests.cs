using System.ComponentModel;
using System.Data;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Bee.Definition;
using Bee.Definition.Layouts;
using Bee.UI.Avalonia.Controls;

namespace Bee.UI.Avalonia.UnitTests.Controls
{
    /// <summary>
    /// Structural + behaviour checks for <see cref="DynamicGrid"/>. Mirrors the MAUI
    /// <c>DynamicGridTests</c> pattern so a reader can pair the two and verify
    /// cross-family parity at a glance.
    /// </summary>
    public class DynamicGridTests
    {
        private static LayoutGrid BuildEmployeeListLayout()
        {
            var layout = new LayoutGrid();
            layout.Columns!.Add(new LayoutColumn { FieldName = "sys_id", Caption = "Employee ID", Visible = true });
            layout.Columns.Add(new LayoutColumn { FieldName = "sys_name", Caption = "Name", Visible = true });
            layout.Columns.Add(new LayoutColumn { FieldName = "hire_date", Caption = "Hire Date", Visible = true });
            layout.Columns.Add(new LayoutColumn { FieldName = "internal_notes", Caption = "Notes", Visible = false });
            return layout;
        }

        private static DataTable BuildEmployeeRows()
        {
            var table = new DataTable("Employee");
            table.Columns.Add(SysFields.RowId, typeof(Guid));
            table.Columns.Add("sys_id", typeof(string));
            table.Columns.Add("sys_name", typeof(string));
            table.Columns.Add("hire_date", typeof(DateTime));
            table.Rows.Add(Guid.NewGuid(), "E001", "Alice Chen", new DateTime(2024, 3, 1));
            table.Rows.Add(Guid.NewGuid(), "E002", "Bob Liu", new DateTime(2025, 1, 15));
            return table;
        }

        private static void InvokeSelectionChangedHandler(DynamicGrid grid)
        {
            var method = typeof(DynamicGrid).GetMethod(
                "OnDataGridSelectionChanged", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);
            method!.Invoke(grid, new object?[] { null, null });
        }

        [Fact]
        [DisplayName("DynamicGrid 為 Avalonia UserControl 子類別")]
        public void Type_IsUserControlSubclass()
        {
            Assert.True(typeof(UserControl).IsAssignableFrom(typeof(DynamicGrid)));
        }

        [Theory]
        [InlineData(nameof(DynamicGrid.ListLayout), "ListLayoutProperty")]
        [InlineData(nameof(DynamicGrid.Rows), "RowsProperty")]
        [InlineData(nameof(DynamicGrid.EmptyText), "EmptyTextProperty")]
        [DisplayName("公開屬性皆有對應的 StyledProperty 註冊")]
        public void PublicProperties_HaveMatchingStyledProperty(string propertyName, string styledPropertyFieldName)
        {
            var property = typeof(DynamicGrid).GetProperty(
                propertyName, BindingFlags.Public | BindingFlags.Instance);
            Assert.NotNull(property);

            var styled = typeof(DynamicGrid).GetField(
                styledPropertyFieldName,
                BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(styled);
            Assert.True(typeof(AvaloniaProperty).IsAssignableFrom(styled!.FieldType));
            Assert.NotNull(styled.GetValue(null));
        }

        [Fact]
        [DisplayName("初始狀態 Content 為 TextBlock,顯示 EmptyText 預設值")]
        public void Default_ContentIsEmptyPlaceholder()
        {
            var grid = new DynamicGrid();

            var label = Assert.IsType<TextBlock>(grid.Content);
            Assert.Equal("No data.", label.Text);
        }

        [Fact]
        [DisplayName("指派 ListLayout + Rows 後 Content 改為 DataGrid,僅含可見欄位")]
        public void AssignedLayoutAndRows_BuildsDataGridWithVisibleColumns()
        {
            var grid = new DynamicGrid
            {
                ListLayout = BuildEmployeeListLayout(),
                Rows = BuildEmployeeRows(),
            };

            var dataGrid = Assert.IsType<DataGrid>(grid.Content);
            // Three visible columns (sys_id, sys_name, hire_date); internal_notes hidden.
            Assert.Equal(3, dataGrid.Columns.Count);
            Assert.Equal("Employee ID", dataGrid.Columns[0].Header);
            Assert.Equal("Name", dataGrid.Columns[1].Header);
            Assert.Equal("Hire Date", dataGrid.Columns[2].Header);
            Assert.NotNull(dataGrid.ItemsSource);
        }

        [Fact]
        [DisplayName("Rows 為空時 Content 退回 EmptyText 訊息")]
        public void EmptyRows_RevertsToEmptyPlaceholder()
        {
            var emptyTable = new DataTable("Employee");
            emptyTable.Columns.Add(SysFields.RowId, typeof(Guid));

            var grid = new DynamicGrid
            {
                ListLayout = BuildEmployeeListLayout(),
                Rows = emptyTable,
                EmptyText = "目前沒有員工",
            };

            var label = Assert.IsType<TextBlock>(grid.Content);
            Assert.Equal("目前沒有員工", label.Text);
        }

        [Fact]
        [DisplayName("RowSelected 事件帶回選取列的 sys_rowid Guid")]
        public void RowSelected_InvokesHandlerWithSysRowId()
        {
            var rows = BuildEmployeeRows();
            var expectedRowId = (Guid)rows.Rows[1][SysFields.RowId];

            var grid = new DynamicGrid
            {
                ListLayout = BuildEmployeeListLayout(),
                Rows = rows,
            };

            Guid? received = null;
            grid.RowSelected += (_, rowId) => received = rowId;

            var dataGrid = Assert.IsType<DataGrid>(grid.Content);
            dataGrid.SelectedItem = rows.DefaultView[1];
            // Selection without a realized visual tree may not raise SelectionChanged,
            // so drive the private handler directly; a duplicate invocation is harmless
            // because the assertion compares the (idempotent) payload.
            InvokeSelectionChangedHandler(grid);

            Assert.Equal(expectedRowId, received);
        }

        [Fact]
        [DisplayName("Row 缺少 sys_rowid 時 RowSelected 不會觸發")]
        public void RowSelected_NoSysRowIdColumn_DoesNotInvokeHandler()
        {
            var table = new DataTable("Employee");
            table.Columns.Add("sys_id", typeof(string));
            table.Rows.Add("E999");
            var layout = new LayoutGrid();
            layout.Columns!.Add(new LayoutColumn { FieldName = "sys_id", Caption = "Employee ID", Visible = true });

            var grid = new DynamicGrid { ListLayout = layout, Rows = table };

            var invoked = false;
            grid.RowSelected += (_, _) => invoked = true;

            var dataGrid = Assert.IsType<DataGrid>(grid.Content);
            dataGrid.SelectedItem = table.DefaultView[0];
            InvokeSelectionChangedHandler(grid);

            Assert.False(invoked);
        }

        [Fact]
        [DisplayName("TryGetRowId 接受 Guid 欄位、字串可解析的 Guid、DBNull 時回傳 false")]
        public void TryGetRowId_VariantInputs_Behaviour()
        {
            var method = typeof(DynamicGrid).GetMethod(
                "TryGetRowId", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);

            var guidTable = new DataTable();
            guidTable.Columns.Add(SysFields.RowId, typeof(Guid));
            var expectedGuid = Guid.NewGuid();
            guidTable.Rows.Add(expectedGuid);
            var args1 = new object?[] { guidTable.Rows[0], Guid.Empty };
            var ok1 = (bool)method!.Invoke(null, args1)!;
            Assert.True(ok1);
            Assert.Equal(expectedGuid, (Guid)args1[1]!);

            var stringTable = new DataTable();
            stringTable.Columns.Add(SysFields.RowId, typeof(string));
            stringTable.Rows.Add(expectedGuid.ToString());
            var args2 = new object?[] { stringTable.Rows[0], Guid.Empty };
            var ok2 = (bool)method.Invoke(null, args2)!;
            Assert.True(ok2);
            Assert.Equal(expectedGuid, (Guid)args2[1]!);

            var nullTable = new DataTable();
            nullTable.Columns.Add(SysFields.RowId, typeof(Guid));
            nullTable.Rows.Add(DBNull.Value);
            var args3 = new object?[] { nullTable.Rows[0], Guid.Empty };
            var ok3 = (bool)method.Invoke(null, args3)!;
            Assert.False(ok3);

            var noColumnTable = new DataTable();
            noColumnTable.Columns.Add("other", typeof(string));
            noColumnTable.Rows.Add("x");
            var args4 = new object?[] { noColumnTable.Rows[0], Guid.Empty };
            var ok4 = (bool)method.Invoke(null, args4)!;
            Assert.False(ok4);
        }

        [Fact]
        [DisplayName("FormatCell 對日期、格式字串、null、缺失欄位各情境回傳預期字串")]
        public void FormatCell_VariantInputs_FormatsExpectedString()
        {
            var method = typeof(DynamicGrid).GetMethod(
                "FormatCell", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);

            var table = new DataTable();
            table.Columns.Add("date_only", typeof(DateTime));
            table.Columns.Add("ts", typeof(DateTime));
            table.Columns.Add("amount", typeof(decimal));
            table.Columns.Add("nullable", typeof(string));
            table.Rows.Add(
                new DateTime(2026, 5, 23),
                new DateTime(2026, 5, 23, 9, 30, 15),
                1234.56m,
                DBNull.Value);
            var row = table.DefaultView[0];

            string Format(string fieldName, string displayFormat = "", string numberFormat = "")
                => (string)method!.Invoke(null, new object?[] { row, fieldName, displayFormat, numberFormat })!;

            Assert.Equal("2026-05-23", Format("date_only"));
            Assert.Equal("2026-05-23 09:30:15", Format("ts"));
            Assert.Equal("1,234.56", Format("amount", displayFormat: "N2"));
            Assert.Equal("1234.6", Format("amount", numberFormat: "F1"));
            Assert.Equal(string.Empty, Format("nullable"));
            Assert.Equal(string.Empty, Format("not_a_column"));

            var nullRow = (string)method!.Invoke(null, new object?[] { null, "date_only", "", "" })!;
            Assert.Equal(string.Empty, nullRow);
        }
    }
}
