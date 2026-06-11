using System.ComponentModel;
using System.Data;
using System.Reflection;
using Avalonia.Controls;
using Bee.Base.Data;
using Bee.Definition;
using Bee.Definition.Forms;
using Bee.Definition.Layouts;
using Bee.UI.Avalonia.Controls.Editors;
using Bee.UI.Avalonia.DataObjects;

namespace Bee.UI.Avalonia.UnitTests.Controls.Editors
{
    /// <summary>
    /// Structural + behaviour checks for <see cref="GridControl"/>: layout-driven
    /// column generation, the two explicit bind paths, row selection and the
    /// formatting helpers carried over from the retired <c>DynamicGrid</c>.
    /// </summary>
    public class GridControlTests
    {
        private static LayoutGrid BuildEmployeeListLayout()
        {
            var layout = new LayoutGrid("Employee", "Employees");
            layout.Columns!.Add(new LayoutColumn { FieldName = "sys_id", Caption = "Employee ID", Visible = true });
            layout.Columns.Add(new LayoutColumn { FieldName = "sys_name", Caption = "Name", Visible = true, Width = 120 });
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

        private static FormDataObject BuildDataObjectWithDetail()
        {
            var schema = new FormSchema("Employee", "Employee");
            var master = schema.Tables!.Add("Employee", "Employee");
            master.Fields!.Add("emp_name", "Name", FieldDbType.String);
            var detail = schema.Tables.Add("EmployeePhone", "Phones");
            detail.Fields!.Add("phone", "Phone", FieldDbType.String);

            var dataObject = new FormDataObject(schema);
            dataObject.InitializeNewMaster();
            return dataObject;
        }

        private static void InvokeSelectionChangedHandler(GridControl grid)
        {
            var method = typeof(GridControl).GetMethod(
                "OnSelectionChangedCore", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);
            method!.Invoke(grid, new object?[] { null, null });
        }

        [Fact]
        [DisplayName("GridControl 為 DataGrid 子類別且 StyleKeyOverride 指向 DataGrid")]
        public void Type_IsDataGridSubclassWithBaseStyleKey()
        {
            var grid = new GridControl();

            Assert.IsAssignableFrom<DataGrid>(grid);
            var styleKey = typeof(global::Avalonia.StyledElement)
                .GetProperty("StyleKeyOverride", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(grid);
            Assert.Equal(typeof(DataGrid), styleKey);
        }

        [Fact]
        [DisplayName("預設為唯讀、單選、不自動產生欄位")]
        public void Defaults_AreReadOnlySingleSelection()
        {
            var grid = new GridControl();

            Assert.True(grid.IsReadOnly);
            Assert.False(grid.AutoGenerateColumns);
            Assert.Equal(DataGridSelectionMode.Single, grid.SelectionMode);
        }

        [Fact]
        [DisplayName("Bind(layout, rows) 僅產生可見欄位並掛上資料")]
        public void Bind_LayoutAndRows_BuildsVisibleColumns()
        {
            var grid = new GridControl();

            grid.Bind(BuildEmployeeListLayout(), BuildEmployeeRows());

            // Three visible columns (sys_id, sys_name, hire_date); internal_notes hidden.
            Assert.Equal(3, grid.Columns.Count);
            Assert.Equal("Employee ID", grid.Columns[0].Header);
            Assert.Equal("Name", grid.Columns[1].Header);
            Assert.Equal("Hire Date", grid.Columns[2].Header);
            Assert.NotNull(grid.ItemsSource);
            Assert.Equal("Employee", grid.TableName);
        }

        [Fact]
        [DisplayName("LayoutColumn.Width 大於 0 時轉為固定欄寬")]
        public void Bind_ColumnWithWidth_AppliesPixelWidth()
        {
            var grid = new GridControl();

            grid.Bind(BuildEmployeeListLayout(), BuildEmployeeRows());

            Assert.Equal(120d, grid.Columns[1].Width.Value);
            Assert.Equal(DataGridLengthUnitType.Pixel, grid.Columns[1].Width.UnitType);
            Assert.Equal(DataGridLengthUnitType.Star, grid.Columns[0].Width.UnitType);
        }

        [Fact]
        [DisplayName("Bind(dataObject, layout) 依 TableName 解析明細表")]
        public void Bind_DataObjectDetail_ResolvesTable()
        {
            var dataObject = BuildDataObjectWithDetail();
            var layout = new LayoutGrid("EmployeePhone", "Phones");
            layout.Columns!.Add(new LayoutColumn { FieldName = "phone", Caption = "Phone", Visible = true });

            var grid = new GridControl();
            grid.Bind(dataObject, layout);

            Assert.Same(dataObject.DataSet.Tables["EmployeePhone"], grid.DataTable);
            Assert.Single(grid.Columns);
        }

        [Fact]
        [DisplayName("Bind(dataObject, layout) 找不到表時綁定為空（只剩表頭）")]
        public void Bind_DataObjectMissingTable_BindsEmpty()
        {
            var dataObject = BuildDataObjectWithDetail();
            var layout = new LayoutGrid("NoSuchTable", "Missing");
            layout.Columns!.Add(new LayoutColumn { FieldName = "x", Caption = "X", Visible = true });

            var grid = new GridControl();
            grid.Bind(dataObject, layout);

            Assert.Null(grid.DataTable);
            Assert.Null(grid.ItemsSource);
            Assert.Single(grid.Columns);
        }

        [Fact]
        [DisplayName("設定 DataTable 屬性重建資料列、欄位沿用既有 layout")]
        public void DataTable_Setter_RebuildsRowsKeepsColumns()
        {
            var grid = new GridControl();
            grid.Bind(BuildEmployeeListLayout(), null);
            Assert.Null(grid.ItemsSource);

            grid.DataTable = BuildEmployeeRows();

            Assert.NotNull(grid.ItemsSource);
            Assert.Equal(3, grid.Columns.Count);
        }

        [Fact]
        [DisplayName("RowSelected 事件帶回選取列的 sys_rowid Guid")]
        public void RowSelected_InvokesHandlerWithSysRowId()
        {
            var rows = BuildEmployeeRows();
            var expectedRowId = (Guid)rows.Rows[1][SysFields.RowId];

            var grid = new GridControl();
            grid.Bind(BuildEmployeeListLayout(), rows);

            Guid? received = null;
            grid.RowSelected += (_, rowId) => received = rowId;

            grid.SelectedItem = rows.DefaultView[1];
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
            var layout = new LayoutGrid("Employee", "Employees");
            layout.Columns!.Add(new LayoutColumn { FieldName = "sys_id", Caption = "Employee ID", Visible = true });

            var grid = new GridControl();
            grid.Bind(layout, table);

            var invoked = false;
            grid.RowSelected += (_, _) => invoked = true;

            grid.SelectedItem = table.DefaultView[0];
            InvokeSelectionChangedHandler(grid);

            Assert.False(invoked);
        }

        [Fact]
        [DisplayName("SetControlState 在任何模式下維持唯讀（編輯屬後續階段）")]
        public void SetControlState_AnyMode_StaysReadOnly()
        {
            var grid = new GridControl();

            grid.SetControlState(SingleFormMode.Edit);
            Assert.True(grid.IsReadOnly);

            grid.SetControlState(SingleFormMode.View);
            Assert.True(grid.IsReadOnly);
        }

        [Fact]
        [DisplayName("TryGetRowId 接受 Guid 欄位、字串可解析的 Guid、DBNull 時回傳 false")]
        public void TryGetRowId_VariantInputs_Behaviour()
        {
            var method = typeof(GridControl).GetMethod(
                "TryGetRowId", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);

            var guidTable = new DataTable();
            guidTable.Columns.Add(SysFields.RowId, typeof(Guid));
            var expectedGuid = Guid.NewGuid();
            guidTable.Rows.Add(expectedGuid);
            var args1 = new object?[] { guidTable.Rows[0], Guid.Empty };
            Assert.True((bool)method!.Invoke(null, args1)!);
            Assert.Equal(expectedGuid, (Guid)args1[1]!);

            var stringTable = new DataTable();
            stringTable.Columns.Add(SysFields.RowId, typeof(string));
            stringTable.Rows.Add(expectedGuid.ToString());
            var args2 = new object?[] { stringTable.Rows[0], Guid.Empty };
            Assert.True((bool)method.Invoke(null, args2)!);
            Assert.Equal(expectedGuid, (Guid)args2[1]!);

            var nullTable = new DataTable();
            nullTable.Columns.Add(SysFields.RowId, typeof(Guid));
            nullTable.Rows.Add(DBNull.Value);
            var args3 = new object?[] { nullTable.Rows[0], Guid.Empty };
            Assert.False((bool)method.Invoke(null, args3)!);

            var noColumnTable = new DataTable();
            noColumnTable.Columns.Add("other", typeof(string));
            noColumnTable.Rows.Add("x");
            var args4 = new object?[] { noColumnTable.Rows[0], Guid.Empty };
            Assert.False((bool)method.Invoke(null, args4)!);
        }

        [Fact]
        [DisplayName("FormatCell 對日期、格式字串、null、缺失欄位各情境回傳預期字串")]
        public void FormatCell_VariantInputs_FormatsExpectedString()
        {
            var method = typeof(GridControl).GetMethod(
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
