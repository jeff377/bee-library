using System.ComponentModel;
using System.Data;
using System.Reflection;
using Bee.Definition;
using Bee.Definition.Layouts;
using Bee.UI.Maui.Controls;

namespace Bee.UI.Maui.UnitTests.Controls
{
    /// <summary>
    /// Structural + behaviour checks for <see cref="DynamicGrid"/>. Mirrors the
    /// <see cref="DynamicFormTests"/> pattern so a reader can pair the two and
    /// verify cross-family parity at a glance.
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

        [Fact]
        [DisplayName("DynamicGrid 為 MAUI ContentView 子類別")]
        public void Type_IsContentViewSubclass()
        {
            Assert.True(typeof(ContentView).IsAssignableFrom(typeof(DynamicGrid)));
        }

        [Theory]
        [InlineData(nameof(DynamicGrid.ListLayout), "ListLayoutProperty")]
        [InlineData(nameof(DynamicGrid.Rows), "RowsProperty")]
        [InlineData(nameof(DynamicGrid.EmptyText), "EmptyTextProperty")]
        [DisplayName("公開屬性皆有對應的 BindableProperty 註冊")]
        public void PublicProperties_HaveMatchingBindableProperty(string propertyName, string bindablePropertyFieldName)
        {
            var property = typeof(DynamicGrid).GetProperty(
                propertyName, BindingFlags.Public | BindingFlags.Instance);
            Assert.NotNull(property);

            var bindable = typeof(DynamicGrid).GetField(
                bindablePropertyFieldName,
                BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(bindable);
            Assert.Equal(typeof(BindableProperty), bindable!.FieldType);
            Assert.NotNull(bindable.GetValue(null));
        }

        [Fact]
        [DisplayName("初始狀態 Content 為 Label,顯示 EmptyText 預設值")]
        public void Default_ContentIsEmptyPlaceholder()
        {
            var grid = new DynamicGrid();

            var label = Assert.IsType<Label>(grid.Content);
            Assert.False(string.IsNullOrEmpty(label.Text));
        }

        [Fact]
        [DisplayName("指派 ListLayout + Rows 後 Content 改為含表頭 + 列的 Grid")]
        public void AssignedLayoutAndRows_BuildsGridWithHeaderAndBody()
        {
            var grid = new DynamicGrid
            {
                ListLayout = BuildEmployeeListLayout(),
                Rows = BuildEmployeeRows(),
            };

            var built = Assert.IsType<Grid>(grid.Content);
            // 1 header row + 2 data rows
            Assert.Equal(3, built.RowDefinitions.Count);
            // 3 visible columns (sys_id, sys_name, hire_date); internal_notes hidden
            Assert.Equal(3, built.ColumnDefinitions.Count);
        }

        [Fact]
        [DisplayName("Rows 為空時 Content 退回 EmptyText 預設訊息")]
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

            var label = Assert.IsType<Label>(grid.Content);
            Assert.Equal("目前沒有員工", label.Text);
        }

        [Fact]
        [DisplayName("RowSelected 事件帶回 sys_rowid Guid")]
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

            // The grid attaches TapGestureRecognizers per cell; simulate the tap by
            // invoking the private OnRowTapped(row) method directly to avoid pulling
            // in the MAUI gesture-pipeline test infrastructure.
            var onRowTapped = typeof(DynamicGrid).GetMethod(
                "OnRowTapped", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(onRowTapped);

            onRowTapped!.Invoke(grid, new object[] { rows.Rows[1] });

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

            var onRowTapped = typeof(DynamicGrid).GetMethod(
                "OnRowTapped", BindingFlags.NonPublic | BindingFlags.Instance);
            onRowTapped!.Invoke(grid, new object[] { table.Rows[0] });

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
            var ok2 = (bool)method!.Invoke(null, args2)!;
            Assert.True(ok2);
            Assert.Equal(expectedGuid, (Guid)args2[1]!);

            var nullTable = new DataTable();
            nullTable.Columns.Add(SysFields.RowId, typeof(Guid));
            nullTable.Rows.Add(DBNull.Value);
            var args3 = new object?[] { nullTable.Rows[0], Guid.Empty };
            var ok3 = (bool)method!.Invoke(null, args3)!;
            Assert.False(ok3);

            var noColumnTable = new DataTable();
            noColumnTable.Columns.Add("other", typeof(string));
            noColumnTable.Rows.Add("x");
            var args4 = new object?[] { noColumnTable.Rows[0], Guid.Empty };
            var ok4 = (bool)method!.Invoke(null, args4)!;
            Assert.False(ok4);
        }

        [Fact]
        [DisplayName("FormatCell 對日期、null、缺失欄位、DisplayFormat 各情境回傳預期字串")]
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
                1234.5m,
                DBNull.Value);

            var row = table.Rows[0];

            var dateOnly = (string)method!.Invoke(null, new object[] {
                row, new LayoutColumn { FieldName = "date_only" } })!;
            Assert.Equal("2026-05-23", dateOnly);

            var ts = (string)method!.Invoke(null, new object[] {
                row, new LayoutColumn { FieldName = "ts" } })!;
            Assert.Equal("2026-05-23 09:30:15", ts);

            var formatted = (string)method!.Invoke(null, new object[] {
                row, new LayoutColumn { FieldName = "amount", DisplayFormat = "N2" } })!;
            Assert.Equal("1,234.50", formatted);

            var nullValue = (string)method!.Invoke(null, new object[] {
                row, new LayoutColumn { FieldName = "nullable" } })!;
            Assert.Equal(string.Empty, nullValue);

            var missingColumn = (string)method!.Invoke(null, new object[] {
                row, new LayoutColumn { FieldName = "not_a_column" } })!;
            Assert.Equal(string.Empty, missingColumn);
        }
    }
}
