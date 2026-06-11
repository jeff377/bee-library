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
            var phoneType = detail.Fields.Add("type", "Type", FieldDbType.String);
            phoneType.ListItems!.Add("OF", "Office");
            phoneType.ListItems.Add("MB", "Mobile");

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

        private static StackPanel GetToolbar(GridControl grid)
            => Assert.IsType<StackPanel>(Assert.IsType<DockPanel>(grid.Content).Children[0]);

        [Fact]
        [DisplayName("GridControl 為 ContentControl 組合，內含工具列與 DataGrid")]
        public void Type_IsContentControlCompositeWithBaseStyleKey()
        {
            var grid = new GridControl();

            Assert.IsAssignableFrom<ContentControl>(grid);
            var styleKey = typeof(global::Avalonia.StyledElement)
                .GetProperty("StyleKeyOverride", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(grid);
            Assert.Equal(typeof(ContentControl), styleKey);

            var host = Assert.IsType<DockPanel>(grid.Content);
            Assert.IsType<StackPanel>(host.Children[0]);
            Assert.Same(grid.InnerGrid, host.Children[1]);
        }

        [Fact]
        [DisplayName("預設為唯讀、單選、不自動產生欄位、工具列隱藏")]
        public void Defaults_AreReadOnlySingleSelection()
        {
            var grid = new GridControl();

            Assert.True(grid.InnerGrid.IsReadOnly);
            Assert.False(grid.InnerGrid.AutoGenerateColumns);
            Assert.Equal(DataGridSelectionMode.Single, grid.InnerGrid.SelectionMode);
            Assert.False(grid.AllowEdit);
            Assert.False(GetToolbar(grid).IsVisible);
        }

        [Fact]
        [DisplayName("Bind(layout, rows) 僅產生可見欄位並掛上資料")]
        public void Bind_LayoutAndRows_BuildsVisibleColumns()
        {
            var grid = new GridControl();

            grid.Bind(BuildEmployeeListLayout(), BuildEmployeeRows());

            // Three visible columns (sys_id, sys_name, hire_date); internal_notes hidden.
            Assert.Equal(3, grid.InnerGrid.Columns.Count);
            Assert.Equal("Employee ID", grid.InnerGrid.Columns[0].Header);
            Assert.Equal("Name", grid.InnerGrid.Columns[1].Header);
            Assert.Equal("Hire Date", grid.InnerGrid.Columns[2].Header);
            Assert.NotNull(grid.InnerGrid.ItemsSource);
            Assert.Equal("Employee", grid.TableName);
        }

        [Fact]
        [DisplayName("LayoutColumn.Width 大於 0 時轉為固定欄寬")]
        public void Bind_ColumnWithWidth_AppliesPixelWidth()
        {
            var grid = new GridControl();

            grid.Bind(BuildEmployeeListLayout(), BuildEmployeeRows());

            Assert.Equal(120d, grid.InnerGrid.Columns[1].Width.Value);
            Assert.Equal(DataGridLengthUnitType.Pixel, grid.InnerGrid.Columns[1].Width.UnitType);
            Assert.Equal(DataGridLengthUnitType.Star, grid.InnerGrid.Columns[0].Width.UnitType);
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
            Assert.Single(grid.InnerGrid.Columns);
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
            Assert.Null(grid.InnerGrid.ItemsSource);
            Assert.Single(grid.InnerGrid.Columns);
        }

        [Fact]
        [DisplayName("設定 DataTable 屬性重建資料列、欄位沿用既有 layout")]
        public void DataTable_Setter_RebuildsRowsKeepsColumns()
        {
            var grid = new GridControl();
            grid.Bind(BuildEmployeeListLayout(), null);
            Assert.Null(grid.InnerGrid.ItemsSource);

            grid.DataTable = BuildEmployeeRows();

            Assert.NotNull(grid.InnerGrid.ItemsSource);
            Assert.Equal(3, grid.InnerGrid.Columns.Count);
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
        [DisplayName("DataSet 置換後依 TableName 重解析新的明細表實例")]
        public async Task Bind_DataObjectDetail_DataSetReplaced_ReResolvesTable()
        {
            var schema = new FormSchema("Employee", "Employee");
            var master = schema.Tables!.Add("Employee", "Employee");
            master.Fields!.Add("emp_name", "Name", FieldDbType.String);
            var detail = schema.Tables.Add("EmployeePhone", "Phones");
            detail.Fields!.Add("phone", "Phone", FieldDbType.String);

            var refreshed = new DataSet("Employee");
            refreshed.Tables.Add(new DataTable("Employee"));
            var refreshedDetail = new DataTable("EmployeePhone");
            refreshedDetail.Columns.Add("phone", typeof(string));
            refreshedDetail.Rows.Add("02-1234-5678");
            refreshed.Tables.Add(refreshedDetail);

            var connector = new FakeFormApiConnector
            {
                GetNewDataHandler = () => new Bee.Api.Core.Messages.Form.GetNewDataResponse { DataSet = refreshed },
            };
            var dataObject = new FormDataObject(schema, connector);

            var layout = new LayoutGrid("EmployeePhone", "Phones");
            layout.Columns!.Add(new LayoutColumn { FieldName = "phone", Caption = "Phone", Visible = true });
            var grid = new GridControl();
            grid.Bind(dataObject, layout);
            var originalTable = grid.DataTable;

            await dataObject.NewAsync();

            Assert.NotSame(originalTable, grid.DataTable);
            Assert.Same(refreshedDetail, grid.DataTable);
        }

        [Fact]
        [DisplayName("明細綁定 + AllowActions.Edit 時 Edit 模式可編輯、View 模式唯讀")]
        public void SetControlState_DetailBoundEditMode_TogglesReadOnly()
        {
            var dataObject = BuildDataObjectWithDetail();
            var layout = new LayoutGrid("EmployeePhone", "Phones");
            layout.Columns!.Add(new LayoutColumn { FieldName = "phone", Caption = "Phone", Visible = true });

            var grid = new GridControl();
            grid.Bind(dataObject, layout);

            grid.SetControlState(SingleFormMode.Edit);
            Assert.False(grid.InnerGrid.IsReadOnly);

            grid.SetControlState(SingleFormMode.View);
            Assert.True(grid.InnerGrid.IsReadOnly);
        }

        [Fact]
        [DisplayName("AllowActions 不含 Edit 時任何模式皆唯讀")]
        public void SetControlState_AllowActionsWithoutEdit_StaysReadOnly()
        {
            var dataObject = BuildDataObjectWithDetail();
            var layout = new LayoutGrid("EmployeePhone", "Phones")
            {
                AllowActions = GridControlAllowActions.Add | GridControlAllowActions.Delete,
            };
            layout.Columns!.Add(new LayoutColumn { FieldName = "phone", Caption = "Phone", Visible = true });

            var grid = new GridControl();
            grid.Bind(dataObject, layout);
            grid.SetControlState(SingleFormMode.Edit);

            Assert.True(grid.InnerGrid.IsReadOnly);
        }

        [Fact]
        [DisplayName("列表模式（無 FormDataObject）維持唯讀")]
        public void SetControlState_ListMode_StaysReadOnly()
        {
            var grid = new GridControl();
            grid.Bind(BuildEmployeeListLayout(), BuildEmployeeRows());

            grid.SetControlState(SingleFormMode.Edit);

            Assert.True(grid.InnerGrid.IsReadOnly);
        }

        [Fact]
        [DisplayName("SetControlState 映射 AllowEdit：View 關閉、Add/Edit 開啟")]
        public void SetControlState_MapsFormModeToAllowEdit()
        {
            var dataObject = BuildDataObjectWithDetail();
            var layout = new LayoutGrid("EmployeePhone", "Phones");
            layout.Columns!.Add(new LayoutColumn { FieldName = "phone", Caption = "Phone", Visible = true });
            var grid = new GridControl();
            grid.Bind(dataObject, layout);

            grid.SetControlState(SingleFormMode.View);
            Assert.False(grid.AllowEdit);

            grid.SetControlState(SingleFormMode.Add);
            Assert.True(grid.AllowEdit);

            grid.SetControlState(SingleFormMode.Edit);
            Assert.True(grid.AllowEdit);
        }

        [Fact]
        [DisplayName("明細綁定時工具列隨 AllowEdit 顯示/隱藏")]
        public void AllowEdit_DetailBound_TogglesToolbarVisibility()
        {
            var dataObject = BuildDataObjectWithDetail();
            var layout = new LayoutGrid("EmployeePhone", "Phones");
            layout.Columns!.Add(new LayoutColumn { FieldName = "phone", Caption = "Phone", Visible = true });
            var grid = new GridControl();

            // The ambient form mode defaults to Edit, so the explicit bind leaves
            // the grid editable with the toolbar shown.
            grid.Bind(dataObject, layout);
            Assert.True(grid.AllowEdit);
            Assert.True(GetToolbar(grid).IsVisible);
            Assert.False(grid.InnerGrid.IsReadOnly);

            grid.AllowEdit = false;
            Assert.False(GetToolbar(grid).IsVisible);
            Assert.True(grid.InnerGrid.IsReadOnly);
        }

        [Fact]
        [DisplayName("list-mode 綁定即使 AllowEdit 開啟仍隱藏工具列且唯讀")]
        public void AllowEdit_ListMode_KeepsToolbarHiddenAndReadOnly()
        {
            var grid = new GridControl();
            grid.Bind(BuildEmployeeListLayout(), BuildEmployeeRows());

            grid.AllowEdit = true;

            Assert.False(GetToolbar(grid).IsVisible);
            Assert.True(grid.InnerGrid.IsReadOnly);
        }

        [Fact]
        [DisplayName("工具列按鈕依 AllowActions 與 EditMode 顯示")]
        public void Toolbar_ButtonVisibility_FollowsAllowActionsAndEditMode()
        {
            var dataObject = BuildDataObjectWithDetail();
            var layout = new LayoutGrid("EmployeePhone", "Phones");
            layout.Columns!.Add(new LayoutColumn { FieldName = "phone", Caption = "Phone", Visible = true });
            var grid = new GridControl();
            grid.Bind(dataObject, layout);

            var toolbar = GetToolbar(grid);
            var addButton = Assert.IsType<Button>(toolbar.Children[0]);
            var editButton = Assert.IsType<Button>(toolbar.Children[1]);
            var deleteButton = Assert.IsType<Button>(toolbar.Children[2]);

            // In-cell editing needs no Edit button (cells edit in place).
            Assert.True(addButton.IsVisible);
            Assert.False(editButton.IsVisible);
            Assert.True(deleteButton.IsVisible);

            grid.EditMode = GridEditMode.EditForm;
            Assert.True(editButton.IsVisible);

            var restricted = new LayoutGrid("EmployeePhone", "Phones")
            {
                AllowActions = GridControlAllowActions.Add | GridControlAllowActions.Delete,
            };
            restricted.Columns!.Add(new LayoutColumn { FieldName = "phone", Caption = "Phone", Visible = true });
            var restrictedGrid = new GridControl { EditMode = GridEditMode.EditForm };
            restrictedGrid.Bind(dataObject, restricted);

            var restrictedToolbar = GetToolbar(restrictedGrid);
            Assert.True(restrictedToolbar.IsVisible);
            Assert.True(Assert.IsType<Button>(restrictedToolbar.Children[0]).IsVisible);
            Assert.False(Assert.IsType<Button>(restrictedToolbar.Children[1]).IsVisible);
            Assert.True(Assert.IsType<Button>(restrictedToolbar.Children[2]).IsVisible);
        }

        [Fact]
        [DisplayName("AllowActions=None 時工具列不顯示")]
        public void Toolbar_NoAllowedActions_StaysHidden()
        {
            var dataObject = BuildDataObjectWithDetail();
            var layout = new LayoutGrid("EmployeePhone", "Phones")
            {
                AllowActions = GridControlAllowActions.None,
            };
            layout.Columns!.Add(new LayoutColumn { FieldName = "phone", Caption = "Phone", Visible = true });
            var grid = new GridControl();
            grid.Bind(dataObject, layout);

            Assert.True(grid.AllowEdit);
            Assert.False(GetToolbar(grid).IsVisible);
            Assert.True(grid.InnerGrid.IsReadOnly);
        }

        [Fact]
        [DisplayName("AddRow 新增列、補齊 NOT NULL 預設並標記 dirty")]
        public void AddRow_AppendsRowAndMarksDirty()
        {
            var dataObject = BuildDataObjectWithDetail();
            var layout = new LayoutGrid("EmployeePhone", "Phones");
            layout.Columns!.Add(new LayoutColumn { FieldName = "phone", Caption = "Phone", Visible = true });
            var grid = new GridControl();
            grid.Bind(dataObject, layout);

            grid.AddRow();

            Assert.Equal(1, grid.DataTable!.Rows.Count);
            Assert.True(dataObject.IsDirty);
        }

        [Fact]
        [DisplayName("AddRow 對 wire 形態（無 DefaultValue 的 NOT NULL 欄）補型別空值")]
        public void AddRow_WireShapedTable_SeedsNonNullDefaults()
        {
            var table = new DataTable("Items");
            table.Columns.Add(new DataColumn("name", typeof(string)) { AllowDBNull = false });
            table.Columns.Add(new DataColumn("qty", typeof(int)) { AllowDBNull = false });
            var layout = new LayoutGrid("Items", "Items");
            layout.Columns!.Add(new LayoutColumn { FieldName = "name", Caption = "Name", Visible = true });

            var grid = new GridControl();
            grid.Bind(layout, table);
            grid.AddRow();

            var row = table.Rows[0];
            Assert.Equal(string.Empty, row["name"]);
            Assert.Equal(0, row["qty"]);
        }

        [Fact]
        [DisplayName("DeleteSelectedRow 將選取列標記 Deleted 並標記 dirty")]
        public void DeleteSelectedRow_MarksRowDeletedAndDirty()
        {
            var dataObject = BuildDataObjectWithDetail();
            var layout = new LayoutGrid("EmployeePhone", "Phones");
            layout.Columns!.Add(new LayoutColumn { FieldName = "phone", Caption = "Phone", Visible = true });
            var grid = new GridControl();
            grid.Bind(dataObject, layout);
            grid.AddRow();
            grid.DataTable!.AcceptChanges();

            grid.SelectedItem = grid.DataTable.DefaultView[0];
            grid.DeleteSelectedRow();

            Assert.Equal(DataRowState.Deleted, grid.DataTable.Rows[0].RowState);
            Assert.True(dataObject.IsDirty);
        }

        [Fact]
        [DisplayName("文字 cell editor 寫回 DataRow，無效輸入保留原值")]
        public void BuildCellEditor_TextEditor_WritesBackAndIgnoresInvalid()
        {
            var table = new DataTable("Items");
            table.Columns.Add("name", typeof(string));
            table.Columns.Add("qty", typeof(int));
            table.Rows.Add("Widget", 5);
            var grid = new GridControl();
            grid.Bind(new LayoutGrid("Items", "Items"), table);

            var method = typeof(GridControl).GetMethod(
                "BuildCellEditor", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);
            var rowView = table.DefaultView[0];

            var nameEditor = Assert.IsType<TextBox>(method!.Invoke(
                grid, new object?[] { rowView, new LayoutColumn { FieldName = "name" } }));
            nameEditor.Text = "Gadget";
            Assert.Equal("Gadget", table.Rows[0]["name"]);

            var qtyEditor = Assert.IsType<TextBox>(method.Invoke(
                grid, new object?[] { rowView, new LayoutColumn { FieldName = "qty" } }));
            qtyEditor.Text = "abc";
            Assert.Equal(5, table.Rows[0]["qty"]);
            qtyEditor.Text = "12";
            Assert.Equal(12, table.Rows[0]["qty"]);
        }

        [Fact]
        [DisplayName("CheckEdit cell editor 以 CheckBox 寫回布林")]
        public void BuildCellEditor_CheckEditor_WritesBoolean()
        {
            var table = new DataTable("Items");
            table.Columns.Add("ok", typeof(bool));
            table.Rows.Add(false);
            var grid = new GridControl();
            grid.Bind(new LayoutGrid("Items", "Items"), table);

            var method = typeof(GridControl).GetMethod(
                "BuildCellEditor", BindingFlags.NonPublic | BindingFlags.Instance);
            var editor = Assert.IsType<CheckBox>(method!.Invoke(
                grid, new object?[] { table.DefaultView[0], new LayoutColumn { FieldName = "ok", ControlType = ControlType.CheckEdit } }));

            editor.IsChecked = true;

            Assert.Equal(true, table.Rows[0]["ok"]);
        }

        [Fact]
        [DisplayName("LayoutColumn.ReadOnly 反映到 DataGrid 欄位唯讀（可編輯 grid 上驗證）")]
        public void Bind_ReadOnlyColumn_SetsColumnReadOnly()
        {
            // The DataGridColumn.IsReadOnly getter coerces with the owning grid's
            // IsReadOnly, so the per-column flag is only observable on an editable
            // (detail-bound, Edit-mode) grid.
            var dataObject = BuildDataObjectWithDetail();
            var layout = new LayoutGrid("EmployeePhone", "Phones");
            layout.Columns!.Add(new LayoutColumn { FieldName = "phone", Caption = "Phone", Visible = true, ReadOnly = true });
            layout.Columns.Add(new LayoutColumn { FieldName = "type", Caption = "Type", Visible = true });

            var grid = new GridControl();
            grid.Bind(dataObject, layout);
            grid.SetControlState(SingleFormMode.Edit);

            Assert.False(grid.InnerGrid.IsReadOnly);
            Assert.True(grid.InnerGrid.Columns[0].IsReadOnly);
            Assert.False(grid.InnerGrid.Columns[1].IsReadOnly);
        }

        [Fact]
        [DisplayName("Popup 型欄位（Check/DropDown/Date/YearMonth）繞過編輯管線改走常駐編輯器")]
        public void BuildColumn_PopupEditorTypes_BypassEditPipeline()
        {
            var dataObject = BuildDataObjectWithDetail();
            var layout = new LayoutGrid("EmployeePhone", "Phones");
            layout.Columns!.Add(new LayoutColumn("phone", "Phone", ControlType.TextEdit));
            layout.Columns.Add(new LayoutColumn("type", "Type", ControlType.DropDownEdit));
            layout.Columns.Add(new LayoutColumn("ok", "OK", ControlType.CheckEdit));

            var grid = new GridControl();
            grid.Bind(dataObject, layout);

            var textColumn = Assert.IsType<DataGridTemplateColumn>(grid.InnerGrid.Columns[0]);
            Assert.NotNull(textColumn.CellEditingTemplate);

            var dropDownColumn = Assert.IsType<DataGridTemplateColumn>(grid.InnerGrid.Columns[1]);
            Assert.Null(dropDownColumn.CellEditingTemplate);
            Assert.True(dropDownColumn.IsReadOnly);

            var checkColumn = Assert.IsType<DataGridTemplateColumn>(grid.InnerGrid.Columns[2]);
            Assert.Null(checkColumn.CellEditingTemplate);
            Assert.True(checkColumn.IsReadOnly);
        }

        [Fact]
        [DisplayName("互動 cell：布林為置中勾選框；popup 型唯讀為文字、可編輯為點擊置換 host")]
        public void BuildInteractiveCell_StateVariants_BuildExpectedControls()
        {
            var table = new DataTable("Items");
            table.Columns.Add("ok", typeof(bool));
            table.Rows.Add(true);
            var rowView = table.DefaultView[0];
            var column = new LayoutColumn("ok", "OK", ControlType.CheckEdit);

            var method = typeof(GridControl).GetMethod(
                "BuildInteractiveCell", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);

            // Boolean cells render a centred checkbox in every state: disabled on a
            // read-only (list-mode) grid, interactive on an editable one.
            var readOnlyGrid = new GridControl();
            readOnlyGrid.Bind(new LayoutGrid("Items", "Items"), table);
            var readOnlyCell = Assert.IsType<CheckBox>(method!.Invoke(readOnlyGrid, new object?[] { rowView, column }));
            Assert.False(readOnlyCell.IsEnabled);
            Assert.True(readOnlyCell.IsChecked);
            Assert.Null(readOnlyCell.Content);
            Assert.Equal(global::Avalonia.Layout.VerticalAlignment.Center, readOnlyCell.VerticalAlignment);

            var dataObject = BuildDataObjectWithDetail();
            var editableLayout = new LayoutGrid("EmployeePhone", "Phones");
            editableLayout.Columns!.Add(column);
            var editableGrid = new GridControl();
            editableGrid.Bind(dataObject, editableLayout);
            Assert.False(editableGrid.InnerGrid.IsReadOnly);
            var editableCell = Assert.IsType<CheckBox>(method.Invoke(editableGrid, new object?[] { rowView, column }));
            Assert.True(editableCell.IsEnabled);

            // Popup columns: text on a read-only grid, click-to-edit host on an
            // editable one (resting content is still the plain text).
            var dateColumn = new LayoutColumn("ok", "OK", ControlType.DateEdit);
            Assert.IsType<TextBlock>(method.Invoke(readOnlyGrid, new object?[] { rowView, dateColumn }));
            var swapHost = Assert.IsType<ContentControl>(
                method.Invoke(editableGrid, new object?[] { rowView, dateColumn }), exactMatch: false);
            Assert.IsType<TextBlock>(swapHost.Content);
        }

        [Fact]
        [DisplayName("Date 系 cell editor 使用三段式 DatePicker 並依格式寫回")]
        public void BuildCellEditor_DateEditor_UsesSegmentedDatePicker()
        {
            var table = new DataTable("Items");
            table.Columns.Add("d", typeof(DateTime));
            table.Columns.Add("ym", typeof(string));
            table.Rows.Add(new DateTime(2026, 1, 15), "2026-06");
            var rowView = table.DefaultView[0];
            var grid = new GridControl();
            grid.Bind(new LayoutGrid("Items", "Items"), table);

            var method = typeof(GridControl).GetMethod(
                "BuildCellEditor", BindingFlags.NonPublic | BindingFlags.Instance);

            var datePicker = Assert.IsType<DatePicker>(method!.Invoke(
                grid, new object?[] { rowView, new LayoutColumn("d", "D", ControlType.DateEdit) }));
            Assert.True(datePicker.DayVisible);
            Assert.Equal(new DateTime(2026, 1, 15), datePicker.SelectedDate!.Value.DateTime);
            datePicker.SelectedDate = new DateTimeOffset(
                new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Unspecified), TimeSpan.Zero);
            Assert.Equal(new DateTime(2026, 5, 1), (DateTime)table.Rows[0]["d"]);

            var monthPicker = Assert.IsType<DatePicker>(method.Invoke(
                grid, new object?[] { rowView, new LayoutColumn("ym", "YM", ControlType.YearMonthEdit) }));
            Assert.False(monthPicker.DayVisible);
            monthPicker.SelectedDate = new DateTimeOffset(
                new DateTime(2026, 7, 20, 0, 0, 0, DateTimeKind.Unspecified), TimeSpan.Zero);
            Assert.Equal("2026-07", table.Rows[0]["ym"]);
        }

        [Fact]
        [DisplayName("DropDown cell editor 選取後以 ListItem.Value 寫回 DataRow")]
        public void BuildCellEditor_DropDownEditor_WritesBackValue()
        {
            var dataObject = BuildDataObjectWithDetail();
            var layout = new LayoutGrid("EmployeePhone", "Phones");
            layout.Columns!.Add(new LayoutColumn("type", "Type", ControlType.DropDownEdit));
            var grid = new GridControl();
            grid.Bind(dataObject, layout);

            var detailTable = grid.DataTable!;
            detailTable.Rows.Add("0912-345-678", "OF");
            var rowView = detailTable.DefaultView[0];

            var method = typeof(GridControl).GetMethod(
                "BuildCellEditor", BindingFlags.NonPublic | BindingFlags.Instance);
            var combo = Assert.IsType<ComboBox>(method!.Invoke(
                grid, new object?[] { rowView, new LayoutColumn("type", "Type", ControlType.DropDownEdit) }));

            var selected = Assert.IsType<Bee.Definition.Collections.ListItem>(combo.SelectedItem);
            Assert.Equal("OF", selected.Value);

            combo.SelectedIndex = 1;

            Assert.Equal("MB", detailTable.Rows[0]["type"]);
        }

        [Fact]
        [DisplayName("EndEdit 在無編輯狀態下不拋例外")]
        public void EndEdit_NoActiveEdit_DoesNotThrow()
        {
            var grid = new GridControl();
            grid.Bind(BuildEmployeeListLayout(), BuildEmployeeRows());

            var exception = Record.Exception(grid.EndEdit);

            Assert.Null(exception);
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

        /// <summary>
        /// Test double that bypasses the real JSON-RPC pipeline by overriding the
        /// virtual CRUD methods used here. Mirrors the fake in
        /// <c>FormDataObjectTests</c>.
        /// </summary>
        private sealed class FakeFormApiConnector : Bee.Api.Client.Connectors.FormApiConnector
        {
            public FakeFormApiConnector() : base(Guid.NewGuid(), "Employee") { }

            public Func<Bee.Api.Core.Messages.Form.GetNewDataResponse>? GetNewDataHandler { get; set; }

            public override Task<Bee.Api.Core.Messages.Form.GetNewDataResponse> GetNewDataAsync()
                => Task.FromResult((GetNewDataHandler ?? (() => new Bee.Api.Core.Messages.Form.GetNewDataResponse()))());
        }
    }
}
