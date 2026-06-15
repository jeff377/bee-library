using System.Data;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Bee.Definition;
using Bee.Definition.Forms;
using Bee.Definition.Layouts;
using Bee.UI.Avalonia.DataObjects;

namespace Bee.UI.Avalonia.Controls.Editors
{
    /// <summary>
    /// Tabular control that renders a <see cref="LayoutGrid"/> definition over a
    /// <see cref="System.Data.DataTable"/>. Composes a built-in icon toolbar
    /// (Add / Edit / Delete, shown only while <see cref="AllowEdit"/> grants
    /// editing) above an inner native <see cref="DataGrid"/> exposed as
    /// <see cref="InnerGrid"/>. Implements the definition-layer
    /// <see cref="IBindTableControl"/> / <see cref="IUIControl"/> contracts.
    /// Raises <see cref="RowSelected"/> with the row's <see cref="SysFields.RowId"/>
    /// Guid when the user selects a row.
    /// </summary>
    /// <remarks>
    /// <see cref="AllowEdit"/> is the single editing switch: the form host flips it
    /// through <see cref="SetControlState"/> (only single-record forms carry a form
    /// mode), other hosts set the property directly. Toolbar visibility, in-cell
    /// editing and the edit-form flow all derive from it inside the control;
    /// list-mode binds (rows without a <see cref="FormDataObject"/>) never edit.
    /// <para>
    /// Each column uses a <see cref="DataGridTemplateColumn"/> with a
    /// <see cref="FuncDataTemplate{T}"/> that fetches the cell value from
    /// <see cref="DataRowView"/>'s indexer at render time. The straightforward
    /// <c>new Binding("[FieldName]")</c> path that WPF uses does <em>not</em>
    /// resolve against <see cref="DataRowView"/>'s string indexer in Avalonia 12 —
    /// the binding engine looks up CLR properties / typed indexers and never
    /// reaches <c>DataRowView.this[string]</c> — so cells silently render empty
    /// even though <see cref="DataGrid.ItemsSource"/> iterates the rows.
    /// See docs/adr/adr-020-avalonia-datagrid-binding-strategy.md for the
    /// full reasoning and trade-offs.
    /// </para>
    /// <para>
    /// In-grid editing is available when the grid is bound to a
    /// <see cref="FormDataObject"/> detail table, <see cref="AllowEdit"/> is set,
    /// and the layout grants <see cref="GridControlAllowActions.Edit"/>; list-mode
    /// binds stay read-only. An empty table renders headers only — hosts that need
    /// an "empty" hint overlay their own placeholder.
    /// </para>
    /// </remarks>
    public class GridControl : ContentControl, IBindTableControl, IUIControl
    {
        /// <summary>
        /// Identifies the <see cref="TableName"/> styled property.
        /// </summary>
        public static readonly StyledProperty<string> TableNameProperty =
            AvaloniaProperty.Register<GridControl, string>(nameof(TableName), string.Empty);

        /// <summary>
        /// Identifies the <see cref="EditMode"/> styled property.
        /// </summary>
        public static readonly StyledProperty<GridEditMode> EditModeProperty =
            AvaloniaProperty.Register<GridControl, GridEditMode>(nameof(EditMode), GridEditMode.InCell);

        /// <summary>
        /// Identifies the <see cref="AllowEdit"/> styled property.
        /// </summary>
        public static readonly StyledProperty<bool> AllowEditProperty =
            AvaloniaProperty.Register<GridControl, bool>(nameof(AllowEdit), false);

        // Simple geometric glyphs (24 grid) drawn for the toolbar, embedded so the
        // control renders the same under any application theme.
        private const string AddIconGeometry =
            "M12 4a1 1 0 0 1 1 1v6h6a1 1 0 1 1 0 2h-6v6a1 1 0 1 1-2 0v-6H5a1 1 0 1 1 0-2h6V5a1 1 0 0 1 1-1Z";
        private const string EditIconGeometry =
            "M16.77 3.16a2.5 2.5 0 0 1 3.54 0l.53.53a2.5 2.5 0 0 1 0 3.54l-1.42 1.41-4.06-4.06 1.41-1.42Z" +
            "M14.3 5.64l4.06 4.06-9.53 9.53a2 2 0 0 1-.94.53l-3.6.9a.75.75 0 0 1-.91-.9l.9-3.61a2 2 0 0 1 .53-.94L14.3 5.64Z";
        private const string DeleteIconGeometry =
            "M10 2.5h4A1.5 1.5 0 0 1 15.5 4v.5H20a1 1 0 1 1 0 2h-.55l-.88 13.2A2.5 2.5 0 0 1 16.08 22H7.92" +
            "a2.5 2.5 0 0 1-2.49-2.3L4.55 6.5H4a1 1 0 0 1 0-2h4.5V4A1.5 1.5 0 0 1 10 2.5Z" +
            "M6.56 6.5l.86 12.97a.5.5 0 0 0 .5.46h8.16a.5.5 0 0 0 .5-.46l.86-12.97H6.56Z" +
            "M10 9.5a1 1 0 0 1 1 1v6a1 1 0 1 1-2 0v-6a1 1 0 0 1 1-1Zm4 0a1 1 0 0 1 1 1v6a1 1 0 1 1-2 0v-6a1 1 0 0 1 1-1Z";

        private readonly GridControlBinder _binder;
        private readonly DataGrid _grid;
        private readonly StackPanel _toolbar;
        private readonly Button _addButton;
        private readonly Button _editButton;
        private readonly Button _deleteButton;
        private readonly PathIcon _addIcon;
        private readonly PathIcon _editIcon;
        private readonly PathIcon _deleteIcon;
        private LayoutGrid? _layout;
        private DataTable? _dataTable;
        private Action? _endActiveInlineEdit;

        static GridControl()
        {
            TableNameProperty.Changed.AddClassHandler<GridControl>((o, _) => o._binder.OnBindingContextChanged());
            EditModeProperty.Changed.AddClassHandler<GridControl>((o, _) => o.UpdateControlState());
            AllowEditProperty.Changed.AddClassHandler<GridControl>((o, _) => o.UpdateControlState());
            FormScope.DataObjectProperty.Changed.AddClassHandler<GridControl>((o, _) => o._binder.OnBindingContextChanged());
            FormScope.FormModeProperty.Changed.AddClassHandler<GridControl>((o, e) => o.SetControlState((SingleFormMode)e.NewValue!));
        }

        /// <summary>
        /// Initializes a new instance of <see cref="GridControl"/> with a hidden
        /// toolbar and a read-only, single-selection inner grid.
        /// </summary>
        public GridControl()
        {
            _binder = new GridControlBinder(this);

            _grid = new DataGrid
            {
                IsReadOnly = true,
                AutoGenerateColumns = false,
                CanUserResizeColumns = true,
                SelectionMode = DataGridSelectionMode.Single,
            };
            _grid.SelectionChanged += OnSelectionChangedCore;
            _grid.DoubleTapped += async (_, _) =>
            {
                if (!CanUseEditForm) return;
                await EditSelectedRowAsync().ConfigureAwait(true);
            };

            _addIcon = BuildToolbarIcon();
            _editIcon = BuildToolbarIcon();
            _deleteIcon = BuildToolbarIcon();
            _addButton = BuildToolbarButton(_addIcon, "Add");
            _editButton = BuildToolbarButton(_editIcon, "Edit");
            _deleteButton = BuildToolbarButton(_deleteIcon, "Delete");
            _addButton.Click += async (_, _) => await AddRowAsync().ConfigureAwait(true);
            _editButton.Click += async (_, _) => await EditSelectedRowAsync().ConfigureAwait(true);
            _deleteButton.Click += (_, _) =>
            {
                EndEdit();
                DeleteSelectedRow();
            };

            _toolbar = new StackPanel
            {
                Orientation = global::Avalonia.Layout.Orientation.Horizontal,
                Spacing = 4,
                Margin = new Thickness(0, 0, 0, 4),
                IsVisible = false,
            };
            _toolbar.Children.Add(_addButton);
            _toolbar.Children.Add(_editButton);
            _toolbar.Children.Add(_deleteButton);

            var host = new DockPanel();
            DockPanel.SetDock(_toolbar, Dock.Top);
            host.Children.Add(_toolbar);
            host.Children.Add(_grid);
            Content = host;
        }

        /// <inheritdoc />
        // WARNING: Without this override the subclass looks up a ControlTheme keyed by
        // its own type, which the application theme does not provide, and the control
        // renders with no visual at all.
        protected override Type StyleKeyOverride => typeof(ContentControl);

        /// <summary>
        /// Gets the inner native <see cref="DataGrid"/>. Columns, items and
        /// selection internals live here; the composite keeps only the bound-grid
        /// surface (<see cref="Bind(FormDataObject, LayoutGrid)"/>,
        /// <see cref="AllowEdit"/>, row actions) on itself.
        /// </summary>
        public DataGrid InnerGrid => _grid;

        /// <summary>
        /// Gets or sets the bound table name.
        /// </summary>
        public string TableName
        {
            get => GetValue(TableNameProperty);
            set => SetValue(TableNameProperty, value);
        }

        /// <summary>
        /// Gets or sets the editing model. <see cref="GridEditMode.EditForm"/> keeps
        /// the inner grid read-only; rows are edited in a popup edit form opened by
        /// the toolbar Edit button or a double tap.
        /// </summary>
        public GridEditMode EditMode
        {
            get => GetValue(EditModeProperty);
            set => SetValue(EditModeProperty, value);
        }

        /// <summary>
        /// Gets or sets whether editing is allowed. This is the single switch the
        /// host controls: single-record forms flip it through
        /// <see cref="SetControlState"/> on form-mode changes, other hosts set it
        /// directly. The toolbar, in-cell editing and the edit-form flow all follow
        /// it, further gated by the layout's <see cref="LayoutGrid.AllowActions"/>
        /// and the presence of a bound <see cref="FormDataObject"/>.
        /// </summary>
        public bool AllowEdit
        {
            get => GetValue(AllowEditProperty);
            set => SetValue(AllowEditProperty, value);
        }

        /// <summary>
        /// Gets or sets the bound data table. Setting the table rebuilds the rows;
        /// the columns keep following the layout supplied to <c>Bind</c>.
        /// </summary>
        public DataTable? DataTable
        {
            get => _dataTable;
            set
            {
                _dataTable = value;
                RebuildRows();
            }
        }

        /// <summary>
        /// Gets the layout definition supplied to <c>Bind</c>, or <c>null</c>.
        /// </summary>
        public LayoutGrid? Layout => _layout;

        /// <summary>
        /// Gets or sets the selected item of the inner grid.
        /// </summary>
        public object? SelectedItem
        {
            get => _grid.SelectedItem;
            set => _grid.SelectedItem = value;
        }

        /// <summary>
        /// Raised when the user selects a row; carries the row's
        /// <see cref="SysFields.RowId"/> Guid. Rows without a parseable
        /// <c>sys_rowid</c> are silently ignored.
        /// </summary>
        public event EventHandler<Guid>? RowSelected;

        /// <summary>
        /// Binds a detail table: resolves the <see cref="System.Data.DataTable"/> named
        /// <see cref="LayoutGrid.TableName"/> from the data object's <c>DataSet</c>.
        /// A missing table binds as empty (headers only) rather than throwing, because
        /// server responses may omit detail tables that carry no rows.
        /// </summary>
        /// <param name="dataObject">The data object whose dataset holds the detail table.</param>
        /// <param name="layout">The grid layout that defines the columns.</param>
        public void Bind(FormDataObject dataObject, LayoutGrid layout)
        {
            ArgumentNullException.ThrowIfNull(dataObject);
            ArgumentNullException.ThrowIfNull(layout);
            _layout = layout;
            TableName = layout.TableName;
            // Bind before building columns: lookup-column detection resolves the
            // FormField metadata through the bound data object.
            _binder.BindExplicit(dataObject);
            RebuildColumns();
            RefreshFromDataObject();
        }

        /// <summary>
        /// Binds a caller-supplied table (list mode): the rows of a
        /// <c>GetListAsync</c> response live outside any <see cref="FormDataObject"/>,
        /// so the grid never edits them and the toolbar stays hidden.
        /// </summary>
        /// <param name="layout">The grid layout that defines the columns.</param>
        /// <param name="rows">The data rows to render, or <c>null</c> for headers only.</param>
        public void Bind(LayoutGrid layout, DataTable? rows)
        {
            ArgumentNullException.ThrowIfNull(layout);
            // List-mode rows live outside any data object; drop a previous detail
            // subscription so a stale DataSetReplaced cannot overwrite these rows.
            _binder.Unbind();
            _layout = layout;
            TableName = layout.TableName;
            _dataTable = rows;
            RebuildColumns();
            RebuildRows();
            SetControlState(GetValue(FormScope.FormModeProperty));
        }

        /// <summary>
        /// Releases the data object subscription (if any). The current columns and
        /// rows stay rendered.
        /// </summary>
        public void Unbind()
        {
            _binder.Unbind();
        }

        /// <summary>
        /// Re-realizes the rows. Hosts call this after mutating row values outside
        /// the grid (for example after a popup edit form commits) — realized text
        /// cells capture their value when their template builds and do not track
        /// later <see cref="DataRow"/> writes.
        /// </summary>
        public void RefreshRows()
        {
            _grid.ItemsSource = null;
            RebuildRows();
        }

        /// <summary>
        /// Commits the in-progress cell and row edit (if any) so the underlying
        /// <see cref="DataRow"/> leaves its edit state before the host inspects or
        /// persists the dataset.
        /// </summary>
        public void EndEdit()
        {
            _endActiveInlineEdit?.Invoke();
            _grid.CommitEdit();
            if (_grid.SelectedItem is DataRowView { IsEdit: true } rowView)
                rowView.EndEdit();
        }

        /// <inheritdoc />
        public void SetControlState(SingleFormMode formMode)
        {
            // The layout's AllowEditModes narrows which form modes may edit; without
            // a layout the mode alone cannot grant editing.
            AllowEdit = formMode != SingleFormMode.View
                && (_layout?.AllowEditModes.Allows(formMode) ?? false);
            // The property handler skips unchanged values, but bind-time calls still
            // need the effective state re-evaluated against the (possibly new)
            // layout and data object.
            UpdateControlState();
        }

        /// <summary>
        /// Appends a new row to the bound table. The owning <see cref="FormDataObject"/> seeds the
        /// row's non-null columns from the FormSchema (a fresh <c>sys_rowid</c>, the
        /// <c>sys_master_rowid</c> master link, and type-appropriate defaults) through its
        /// <see cref="DataTable.TableNewRow"/> hook, so the row is insert-ready on creation.
        /// </summary>
        public void AddRow()
        {
            if (_dataTable is null) return;

            var row = _dataTable.NewRow();
            // Form-backed binds: the FormDataObject seeds the row's non-null columns (a fresh
            // sys_rowid, the sys_master_rowid link, and type-appropriate defaults) from the
            // FormSchema via its TableNewRow hook. Raw-table (list-mode) binds have no such hook,
            // so the grid fills non-nullable columns itself with type-appropriate empty values.
            if (_binder.DataObject is null)
            {
                foreach (DataColumn column in _dataTable.Columns)
                {
                    if (!column.AllowDBNull
                        && (column.DefaultValue is null || column.DefaultValue == DBNull.Value))
                    {
                        row[column] = FormDataObject.ResolveEmptyValueForType(column.DataType);
                    }
                }
            }
            // Attaching the row marks the data object dirty through its DataTable
            // event bridge; no explicit notification is needed here.
            _dataTable.Rows.Add(row);
            // The DataGrid does not observe DataView changes — re-realize so the
            // new row shows up.
            RefreshRows();
        }

        /// <summary>
        /// Deletes the selected row (marks it <see cref="DataRowState.Deleted"/> so the
        /// save pipeline can translate the change). No-op when nothing is selected.
        /// </summary>
        public void DeleteSelectedRow()
        {
            if (_grid.SelectedItem is not DataRowView rowView) return;
            rowView.Row.Delete();
            // The DataGrid does not observe DataView changes — re-realize so the
            // deleted row disappears.
            RefreshRows();
        }

        /// <inheritdoc />
        protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            base.OnAttachedToLogicalTree(e);
            _binder.NotifyAttached();
        }

        /// <inheritdoc />
        protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromLogicalTree(e);
            _binder.NotifyDetached();
        }

        /// <inheritdoc />
        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            // `Geometry.Parse` needs platform services that are absent when unit
            // tests construct the control without an Avalonia platform, so the icon
            // data is created on first visual attach instead of in the constructor.
            _addIcon.Data ??= Geometry.Parse(AddIconGeometry);
            _editIcon.Data ??= Geometry.Parse(EditIconGeometry);
            _deleteIcon.Data ??= Geometry.Parse(DeleteIconGeometry);
        }

        /// <summary>
        /// Re-resolves the bound table from the data object by <see cref="TableName"/>
        /// and refreshes the rows. Called after binding and whenever the underlying
        /// <c>DataSet</c> is replaced. Without a layout (ambient, table-name-only
        /// binds), plain text columns are generated from the table's columns.
        /// </summary>
        internal void RefreshFromDataObject()
        {
            var dataObject = _binder.DataObject;
            if (dataObject is null) return;

            var tables = dataObject.DataSet.Tables;
            var tableName = TableName;
            _dataTable = tableName.Length > 0 && tables.Contains(tableName) ? tables[tableName] : null;
            if (_layout is null)
                RebuildFallbackColumns();
            RebuildRows();
        }

        private bool CanUseEditForm
            => AllowEdit
                && EditMode == GridEditMode.EditForm
                && _binder.DataObject is not null
                && (_layout?.AllowActions.HasFlag(GridControlAllowActions.Edit) ?? false);

        // Recomputes the effective editing state after AllowEdit / EditMode / bind
        // changes: toolbar visibility per action flag, in-cell editability of the
        // inner grid.
        private void UpdateControlState()
        {
            var actions = _layout?.AllowActions ?? GridControlAllowActions.None;
            var canEdit = AllowEdit && _binder.DataObject is not null;

            _addButton.IsVisible = actions.HasFlag(GridControlAllowActions.Add);
            _deleteButton.IsVisible = actions.HasFlag(GridControlAllowActions.Delete);
            // In-cell editing needs no Edit button (cells edit in place); EditForm
            // surfaces one beside the double-tap gesture.
            _editButton.IsVisible = EditMode == GridEditMode.EditForm
                && actions.HasFlag(GridControlAllowActions.Edit);
            _toolbar.IsVisible = canEdit && actions != GridControlAllowActions.None;

            var inCellEdit = canEdit
                && EditMode == GridEditMode.InCell
                && actions.HasFlag(GridControlAllowActions.Edit);
            var readOnly = !inCellEdit;
            if (_grid.IsReadOnly == readOnly) return;

            _grid.IsReadOnly = readOnly;
            // Always-on editor cells capture the enabled state when their template
            // builds, so an editability switch must re-realize the rows.
            _grid.ItemsSource = null;
            RebuildRows();
        }

        private async Task AddRowAsync()
        {
            if (_binder.DataObject is null || _layout is null || _dataTable is null) return;
            AddRow();
            if (EditMode != GridEditMode.EditForm) return;

            var table = _dataTable;
            var row = table.Rows[table.Rows.Count - 1];
            var committed = await RowEditDialog.ShowAsync(this, _binder.DataObject, _layout, row).ConfigureAwait(true);
            if (committed)
            {
                RefreshAndFocusRow(row);
            }
            else
            {
                // A cancelled Add removes the blank row again instead of leaving an
                // empty line in the detail table.
                table.Rows.Remove(row);
                RefreshRows();
            }
        }

        private async Task EditSelectedRowAsync()
        {
            if (_binder.DataObject is null || _layout is null) return;
            if (_grid.SelectedItem is not DataRowView rowView) return;

            var row = rowView.Row;
            var committed = await RowEditDialog.ShowAsync(this, _binder.DataObject, _layout, row).ConfigureAwait(true);
            if (committed)
                RefreshAndFocusRow(row);
        }

        // Realized text cells capture their value at template build, so a committed
        // edit form re-realizes the rows and scrolls back to the affected row.
        private void RefreshAndFocusRow(DataRow row)
        {
            RefreshRows();
            var rowView = _dataTable?.DefaultView
                .Cast<DataRowView>()
                .FirstOrDefault(v => ReferenceEquals(v.Row, row));
            if (rowView is null) return;
            _grid.SelectedItem = rowView;
            _grid.ScrollIntoView(rowView, null);
        }

        private static PathIcon BuildToolbarIcon()
            => new()
            {
                Width = 14,
                Height = 14,
            };

        private static Button BuildToolbarButton(PathIcon icon, string toolTip)
        {
            var button = new Button
            {
                Content = icon,
                Focusable = false,
                Padding = new Thickness(6, 4),
                MinWidth = 0,
                MinHeight = 0,
            };
            ToolTip.SetTip(button, toolTip);
            return button;
        }

        private void RebuildFallbackColumns()
        {
            _grid.Columns.Clear();
            if (_dataTable is null) return;
            foreach (DataColumn column in _dataTable.Columns)
                _grid.Columns.Add(BuildColumn(new LayoutColumn(column.ColumnName, column.ColumnName, ControlType.TextEdit)));
        }

        private void RebuildColumns()
        {
            _grid.Columns.Clear();
            if (_layout is null) return;
            foreach (var column in EnumerateVisibleColumns(_layout))
                _grid.Columns.Add(BuildColumn(column));
        }

        private void RebuildRows()
        {
            // Re-realizing discards any cell that hosted an inline editor.
            _endActiveInlineEdit = null;
            _grid.ItemsSource = _dataTable?.DefaultView;
        }

        private DataGridTemplateColumn BuildColumn(LayoutColumn column)
        {
            // Capture per-column metadata into locals so each cell template closure
            // resolves the correct field name / display format regardless of when
            // the template fires.
            var fieldName = column.FieldName;
            var displayFormat = column.DisplayFormat;
            var numberFormat = column.NumberFormat;

            var templateColumn = new DataGridTemplateColumn
            {
                Header = column.Caption,
                IsReadOnly = column.ReadOnly,
            };

            if (TryGetLookupFormField(column) is { } lookupField)
            {
                // The lookup editor is a modal dialog: it takes focus out of the cell,
                // which would tear a CellEditingTemplate down mid-edit, so lookup
                // columns bypass the DataGrid edit pipeline entirely and open the
                // dialog from the display template instead.
                // See docs/adr/adr-021-avalonia-datagrid-editing-strategy.md.
                templateColumn.IsReadOnly = true;
                templateColumn.CellTemplate = new FuncDataTemplate<DataRowView>(
                    (row, _) => BuildLookupCell(row, column, lookupField),
                    supportsRecycling: false);
            }
            else if (IsAlwaysOnEditor(column.ControlType))
            {
                // Popup-based editors (ComboBox dropdown, DatePicker flyout) break
                // inside the DataGrid edit pipeline: opening the popup moves focus out
                // of the cell and the grid tears the editing template down. These
                // columns manage their own click-to-edit swap inside the display
                // template instead and bypass the edit pipeline entirely.
                // See docs/adr/adr-021-avalonia-datagrid-editing-strategy.md.
                templateColumn.IsReadOnly = true;
                templateColumn.CellTemplate = new FuncDataTemplate<DataRowView>(
                    (row, _) => BuildInteractiveCell(row, column),
                    supportsRecycling: false);
            }
            else
            {
                // List-mode lookup columns (no data object → no lookup flow) still
                // render the display fields instead of the raw row id.
                var textFields = SplitDisplayFields(column.DisplayFields);
                // Recycling MUST stay off: the cell Text is computed once at build time
                // (it is not a binding to the row's DataContext). With recycling on, the
                // DataGrid reuses a presenter across rows and the stale Text no longer
                // matches the underlying DataRowView — so the displayed value diverges
                // from the row, and a lookup pick returns a different row than shown.
                templateColumn.CellTemplate = new FuncDataTemplate<DataRowView>(
                    (row, _) => new TextBlock
                    {
                        Text = textFields.Length == 0
                            ? FormatCell(row, fieldName, displayFormat, numberFormat)
                            : ComposeDisplayText(row, textFields, displayFormat, numberFormat),
                        Margin = new Thickness(8, 4),
                    },
                    supportsRecycling: false);
                // Recycling is off: each edit session gets a fresh editor whose change
                // handlers close over the row being edited.
                templateColumn.CellEditingTemplate = new FuncDataTemplate<DataRowView>(
                    (row, _) => BuildCellEditor(row, column),
                    supportsRecycling: false);
            }

            if (column.Width > 0)
            {
                // LayoutColumn.Width is in CSS pixels on the Blazor side; treating it as
                // device-independent units gives an equivalent column-width hint here.
                templateColumn.Width = new DataGridLength(column.Width);
            }
            else
            {
                templateColumn.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
            }

            return templateColumn;
        }

        private static bool IsAlwaysOnEditor(ControlType controlType)
            => controlType is ControlType.CheckEdit or ControlType.DropDownEdit
                or ControlType.DateEdit or ControlType.YearMonthEdit;

        /// <summary>
        /// Resolves the relation metadata for a lookup column: a
        /// <see cref="ControlType.ButtonEdit"/> column whose schema field carries
        /// <c>RelationProgId</c> on the bound table. Returns <c>null</c> in list mode
        /// (no data object) — lookup editing needs the data object write path.
        /// </summary>
        private FormField? TryGetLookupFormField(LayoutColumn column)
        {
            if (column.ControlType != ControlType.ButtonEdit) return null;
            var dataObject = _binder.DataObject;
            var tableName = TableName;
            if (dataObject is null || string.IsNullOrEmpty(tableName)) return null;
            var field = dataObject.GetFormField(tableName, column.FieldName);
            return field is not null && !string.IsNullOrEmpty(field.RelationProgId) ? field : null;
        }

        // Lookup cells rest as the composed display-field text — typically
        // "<id> <name>", id only for transactional targets, empty when no display
        // field resolves (never the raw Guid); a click on an editable cell opens
        // the lookup dialog and the selection writes back through the data object.
        private Control BuildLookupCell(DataRowView? rowView, LayoutColumn column, FormField lookupField)
        {
            var displayFields = SplitDisplayFields(column.DisplayFields);
            if (displayFields.Length == 0)
                displayFields = [.. lookupField.GetDisplayFields()];
            var text = new TextBlock
            {
                Text = ComposeDisplayText(rowView, displayFields, column.DisplayFormat, column.NumberFormat),
                Margin = new Thickness(8, 4),
                VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Center,
            };

            var canEdit = !_grid.IsReadOnly && !column.ReadOnly && rowView is not null;
            if (!canEdit) return text;

            // Transparent (not null) background: a null background excludes the empty
            // area beside the text from hit-testing.
            var host = new Border
            {
                Background = Brushes.Transparent,
                Child = text,
            };
            host.PointerPressed += async (_, e) =>
            {
                e.Handled = true;
                // Only one inline editor at a time: opening the dialog closes a
                // lingering editor in another cell.
                _endActiveInlineEdit?.Invoke();
                await OpenLookupCellAsync(rowView!.Row, lookupField).ConfigureAwait(true);
            };
            return host;
        }

        private static string[] SplitDisplayFields(string displayFields)
            => string.IsNullOrEmpty(displayFields)
                ? []
                : displayFields.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        // Joins the non-empty display-field values (e.g. "D001 - Engineering").
        private static string ComposeDisplayText(
            DataRowView? rowView, string[] displayFields, string displayFormat, string numberFormat)
        {
            if (displayFields.Length == 0) return string.Empty;
            return LookupDisplay.Compose(displayFields
                .Select(f => FormatCell(rowView, f, displayFormat, numberFormat)));
        }

        private async Task OpenLookupCellAsync(DataRow row, FormField lookupField)
        {
            var dataObject = _binder.DataObject;
            if (dataObject is null) return;

            var progId = string.IsNullOrEmpty(lookupField.LookupProgId)
                ? lookupField.RelationProgId
                : lookupField.LookupProgId;
            try
            {
                var selected = await LookupDialog.ShowAsync(this, progId).ConfigureAwait(true);
                if (selected is null) return;
                dataObject.ApplyLookupSelection(lookupField, selected, row);
                // Realized text cells capture their value at template build;
                // re-realize and scroll back to the affected row (same as a
                // committed edit form).
                RefreshAndFocusRow(row);
            }
            catch (Exception ex)
            {
                // UI boundary: an async pointer handler must not crash the app;
                // surface the failure as the grid's tooltip.
                ToolTip.SetTip(this, ex.Message);
            }
        }

        // Display template for the popup-based editor columns. Boolean cells render a
        // centred checkbox in every state (a disabled checkbox reads better than
        // "True"/"False" text); the other types rest as plain formatted text and swap
        // in their editor on click — the swap is managed here, not by the DataGrid
        // edit pipeline, so opening the editor's popup cannot tear the editor down.
        private Control BuildInteractiveCell(DataRowView? rowView, LayoutColumn column)
        {
            var canEdit = !_grid.IsReadOnly && !column.ReadOnly && rowView is not null;

            if (column.ControlType == ControlType.CheckEdit)
            {
                CheckBox checkBox;
                if (canEdit)
                {
                    checkBox = (CheckBox)BuildCellEditor(rowView, column);
                }
                else
                {
                    var value = string.Equals(
                        FormatCell(rowView, column.FieldName, string.Empty, string.Empty),
                        bool.TrueString, StringComparison.OrdinalIgnoreCase);
                    checkBox = new CheckBox { IsChecked = value, IsEnabled = false };
                }
                checkBox.HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Center;
                checkBox.VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Center;
                return checkBox;
            }

            if (!canEdit)
            {
                return new TextBlock
                {
                    Text = FormatCell(rowView, column.FieldName, column.DisplayFormat, column.NumberFormat),
                    Margin = new Thickness(8, 4),
                    VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Center,
                };
            }

            // canEdit is true only when rowView is not null (see its definition above),
            // so the swap-cell branch always has a non-null row.
            return BuildSwapCell(rowView!, column);
        }

        // Click-to-edit host: rests as the original text rendering, swaps to the
        // editor on pointer press and swaps back when the edit session ends, re-reading
        // the row so the committed value is what gets displayed.
        private Control BuildSwapCell(DataRowView rowView, LayoutColumn column)
        {
            var host = new ContentControl
            {
                HorizontalContentAlignment = global::Avalonia.Layout.HorizontalAlignment.Stretch,
                VerticalContentAlignment = global::Avalonia.Layout.VerticalAlignment.Center,
                // Transparent (not null) background: a null background excludes the
                // empty area beside the text from hit-testing, so only the characters
                // themselves would react to the click.
                Background = Brushes.Transparent,
            };

            void ShowDisplay() => host.Content = new TextBlock
            {
                Text = FormatCell(rowView, column.FieldName, column.DisplayFormat, column.NumberFormat),
                Margin = new Thickness(8, 4),
                VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Center,
            };

            host.PointerPressed += (_, _) =>
            {
                if (host.Content is not TextBlock) return;
                // Only one inline editor at a time: starting an edit here closes a
                // lingering editor in another cell (e.g. a dismissed date pick).
                _endActiveInlineEdit?.Invoke();

                var editor = BuildCellEditor(rowView, column);
                editor.HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Stretch;
                Action end = null!;
                end = () =>
                {
                    if (host.Content is TextBlock) return;
                    ShowDisplay();
                    if (ReferenceEquals(_endActiveInlineEdit, end))
                        _endActiveInlineEdit = null;
                };
                WireInlineEditEnd(editor, end);
                host.Content = editor;
                _endActiveInlineEdit = end;
            };

            ShowDisplay();
            return host;
        }

        private static void WireInlineEditEnd(Control editor, Action endEdit)
        {
            switch (editor)
            {
                case ComboBox combo:
                    // Open the dropdown so a single click on the cell goes straight to
                    // picking an option — deferred past the click that swapped the
                    // editor in, otherwise the same pointer interaction closes the
                    // dropdown again immediately. Only a close that follows a real
                    // open ends the edit (guards the same race on the way out).
                    var opened = false;
                    combo.AttachedToVisualTree += (_, _) =>
                        global::Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                        {
                            opened = true;
                            combo.IsDropDownOpen = true;
                        });
                    combo.PropertyChanged += (_, e) =>
                    {
                        if (e.Property != ComboBox.IsDropDownOpenProperty) return;
                        if (combo.IsDropDownOpen)
                            opened = true;
                        else if (opened)
                            endEdit();
                    };
                    break;
                case DatePicker picker:
                    // Pop the spinner flyout right away so a single click on the cell
                    // goes straight to picking. DatePicker has no public open API;
                    // raising Click on the template's flyout button is the supported
                    // route in. Background priority defers past the layout pass so the
                    // flyout positions against the realized picker.
                    picker.AttachedToVisualTree += (_, _) =>
                        global::Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                        {
                            picker.ApplyTemplate();
                            var flyoutButton = global::Avalonia.VisualTree.VisualExtensions
                                .FindDescendantOfType<Button>(picker);
                            flyoutButton?.RaiseEvent(new global::Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
                        }, global::Avalonia.Threading.DispatcherPriority.Background);
                    // Commit-driven swap-back only: confirming a date changes
                    // SelectedDate (the write-back hook subscribed first, so the row
                    // is updated before the swap-back re-reads it). LostFocus is NOT
                    // wired — the spinner flyout takes focus and would tear the editor
                    // down mid-pick. A dismissed flyout leaves the editor in place
                    // until the next inline edit or EndEdit closes it.
                    picker.PropertyChanged += (_, e) =>
                    {
                        if (e.Property == DatePicker.SelectedDateProperty)
                            endEdit();
                    };
                    break;
                default:
                    editor.LostFocus += (_, _) => endEdit();
                    break;
            }
        }

        // Builds the in-cell editor for the column's ControlType. The writes go
        // straight to the DataRow (the ADR-020 binding limitation applies to editing
        // templates too); invalid partial input keeps the last valid value.
        private Control BuildCellEditor(DataRowView? rowView, LayoutColumn column)
        {
            if (rowView is null) return new TextBlock();
            var fieldName = column.FieldName;
            var dataColumn = rowView.Row.Table.Columns.Contains(fieldName)
                ? rowView.Row.Table.Columns[fieldName]
                : null;
            if (dataColumn is null) return new TextBlock();

            var current = FormatCell(rowView, fieldName, string.Empty, string.Empty);

            if (column.ControlType == ControlType.CheckEdit)
            {
                var checkBox = new CheckBox
                {
                    IsChecked = string.Equals(current, bool.TrueString, StringComparison.OrdinalIgnoreCase),
                };
                checkBox.IsCheckedChanged += (_, _) =>
                    WriteCell(rowView, dataColumn, (checkBox.IsChecked ?? false).ToString());
                return checkBox;
            }

            if (column.ControlType is ControlType.DateEdit or ControlType.YearMonthEdit)
            {
                var format = column.ControlType == ControlType.YearMonthEdit ? "yyyy-MM" : "yyyy-MM-dd";
                var picker = new DatePicker
                {
                    DayVisible = column.ControlType != ControlType.YearMonthEdit,
                    SelectedDate = DateEdit.ParseToOffset(current),
                };
                // `SelectedDateChanged` is not raised reliably for programmatic
                // writes; hook the property change instead (same approach as the
                // TextBox editor).
                picker.PropertyChanged += (_, e) =>
                {
                    if (e.Property == DatePicker.SelectedDateProperty)
                        WriteCell(rowView, dataColumn, picker.SelectedDate?.DateTime.ToString(format, CultureInfo.InvariantCulture));
                };
                return picker;
            }

            if (column.ControlType == ControlType.DropDownEdit
                && _binder.DataObject?.GetFormField(TableName, fieldName)?.ListItems is { Count: > 0 } items)
            {
                var options = items.ToList();
                var combo = new ComboBox
                {
                    ItemsSource = options,
                    // Same selection-box pitfall as DropDownEdit: a recycling template
                    // starves the collapsed combo of its content instance.
                    DisplayMemberBinding = new global::Avalonia.Data.Binding(
                        nameof(Bee.Definition.Collections.ListItem.Text)),
                    SelectedItem = options.FirstOrDefault(i => string.Equals(i.Value, current, StringComparison.Ordinal)),
                };
                combo.SelectionChanged += (_, _) =>
                    WriteCell(rowView, dataColumn, (combo.SelectedItem as Bee.Definition.Collections.ListItem)?.Value);
                return combo;
            }

            var textBox = new TextBox { Text = current };
            // `TextChanged` is not raised reliably for programmatic writes; hook the
            // property change instead (same approach as `TextEdit`).
            textBox.PropertyChanged += (_, e) =>
            {
                if (e.Property == TextBox.TextProperty)
                    WriteCell(rowView, dataColumn, textBox.Text);
            };
            return textBox;
        }

        private static void WriteCell(DataRowView rowView, DataColumn column, string? value)
        {
            if (!TryConvertCellValue(value, column, out var converted)) return;
            if (Equals(converted, rowView.Row[column])) return;
            // The write raises FieldValueChanged and marks dirty through the data
            // object's DataTable event bridge.
            rowView.Row[column] = converted;
        }

        private static bool TryConvertCellValue(string? value, DataColumn column, out object converted)
        {
            try
            {
                converted = FormDataObject.ConvertToColumnValue(value, column);
                return true;
            }
            catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException)
            {
                // Partial keystroke input (for example "12." in a decimal column)
                // cannot convert yet; the cell keeps its last valid value until the
                // input parses.
                converted = DBNull.Value;
                return false;
            }
        }

        private static string FormatCell(DataRowView? row, string fieldName, string displayFormat, string numberFormat)
        {
            if (row is null) return string.Empty;
            var dataRow = row.Row;
            if (!dataRow.Table.Columns.Contains(fieldName)) return string.Empty;
            var raw = dataRow[fieldName];
            if (raw is null || raw == DBNull.Value) return string.Empty;

            if (!string.IsNullOrEmpty(displayFormat) && raw is IFormattable formattableDisplay)
                return formattableDisplay.ToString(displayFormat, CultureInfo.InvariantCulture);
            if (!string.IsNullOrEmpty(numberFormat) && raw is IFormattable formattableNumber)
                return formattableNumber.ToString(numberFormat, CultureInfo.InvariantCulture);

            return raw switch
            {
                DateTime dt => dt.TimeOfDay == TimeSpan.Zero
                    ? dt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                    : dt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
                _ => raw.ToString() ?? string.Empty,
            };
        }

        private void OnSelectionChangedCore(object? sender, SelectionChangedEventArgs e)
        {
            var handler = RowSelected;
            if (handler is null) return;
            if (_grid.SelectedItem is not DataRowView rowView) return;
            if (!TryGetRowId(rowView.Row, out var rowId)) return;
            handler(this, rowId);
        }

        private static IEnumerable<LayoutColumn> EnumerateVisibleColumns(LayoutGrid layout)
            => layout.Columns?.Where(c => c.Visible) ?? Enumerable.Empty<LayoutColumn>();

        private static bool TryGetRowId(DataRow row, out Guid rowId)
        {
            rowId = Guid.Empty;
            if (!row.Table.Columns.Contains(SysFields.RowId)) return false;
            var raw = row[SysFields.RowId];
            if (raw is null || raw == DBNull.Value) return false;
            if (raw is Guid g) { rowId = g; return true; }
            return Guid.TryParse(raw.ToString(), out rowId);
        }
    }
}
