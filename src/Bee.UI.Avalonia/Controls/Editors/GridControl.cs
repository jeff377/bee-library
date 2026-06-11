using System.Data;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Bee.Definition;
using Bee.Definition.Layouts;
using Bee.UI.Avalonia.DataObjects;

namespace Bee.UI.Avalonia.Controls.Editors
{
    /// <summary>
    /// Tabular control that renders a <see cref="LayoutGrid"/> definition over a
    /// <see cref="System.Data.DataTable"/> by inheriting the native
    /// <see cref="DataGrid"/>. Implements the definition-layer
    /// <see cref="IBindTableControl"/> / <see cref="IUIControl"/> contracts.
    /// Raises <see cref="RowSelected"/> with the row's <see cref="SysFields.RowId"/>
    /// Guid when the user selects a row.
    /// </summary>
    /// <remarks>
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
    /// <para>
    /// In-grid editing is available when the grid is bound to a
    /// <see cref="FormDataObject"/> detail table, the form mode is not
    /// <see cref="SingleFormMode.View"/>, and the layout grants
    /// <see cref="GridControlAllowActions.Edit"/>; list-mode binds stay read-only.
    /// An empty table renders headers only — hosts that need an "empty" hint
    /// overlay their own placeholder.
    /// </para>
    /// </remarks>
    public class GridControl : DataGrid, IBindTableControl, IUIControl
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

        private readonly GridControlBinder _binder;
        private LayoutGrid? _layout;
        private DataTable? _dataTable;
        private Action? _endActiveInlineEdit;

        static GridControl()
        {
            TableNameProperty.Changed.AddClassHandler<GridControl>((o, _) => o._binder.OnBindingContextChanged());
            EditModeProperty.Changed.AddClassHandler<GridControl>((o, _) => o.SetControlState(o.GetValue(FormScope.FormModeProperty)));
            FormScope.DataObjectProperty.Changed.AddClassHandler<GridControl>((o, _) => o._binder.OnBindingContextChanged());
            FormScope.FormModeProperty.Changed.AddClassHandler<GridControl>((o, e) => o.SetControlState((SingleFormMode)e.NewValue!));
        }

        /// <summary>
        /// Initializes a new instance of <see cref="GridControl"/> with read-only,
        /// single-selection defaults.
        /// </summary>
        public GridControl()
        {
            _binder = new GridControlBinder(this);
            IsReadOnly = true;
            AutoGenerateColumns = false;
            CanUserResizeColumns = true;
            SelectionMode = DataGridSelectionMode.Single;
            SelectionChanged += OnSelectionChangedCore;
        }

        /// <inheritdoc />
        // WARNING: Without this override the subclass looks up a ControlTheme keyed by
        // its own type, which the application theme does not provide, and the control
        // renders with no visual at all.
        protected override Type StyleKeyOverride => typeof(DataGrid);

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
        /// the grid read-only; the host opens a popup edit form per row instead.
        /// </summary>
        public GridEditMode EditMode
        {
            get => GetValue(EditModeProperty);
            set => SetValue(EditModeProperty, value);
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
            RebuildColumns();
            _binder.BindExplicit(dataObject);
            RefreshFromDataObject();
        }

        /// <summary>
        /// Binds a caller-supplied table (list mode): the rows of a
        /// <c>GetListAsync</c> response live outside any <see cref="FormDataObject"/>.
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
            ItemsSource = null;
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
            CommitEdit();
            if (SelectedItem is DataRowView { IsEdit: true } rowView)
                rowView.EndEdit();
        }

        /// <inheritdoc />
        public void SetControlState(SingleFormMode formMode)
        {
            // In-cell editing only makes sense against a FormDataObject detail table
            // whose layout grants the Edit action; list-mode rows stay read-only, and
            // EditForm mode keeps the grid itself read-only (rows are edited in the
            // popup edit form).
            var allowEdit = formMode != SingleFormMode.View
                && EditMode == GridEditMode.InCell
                && _binder.DataObject is not null
                && (_layout?.AllowActions.HasFlag(GridControlAllowActions.Edit) ?? false);
            var readOnly = !allowEdit;
            if (IsReadOnly == readOnly) return;

            IsReadOnly = readOnly;
            // Always-on editor cells capture the enabled state when their template
            // builds, so a mode switch must re-realize the rows.
            ItemsSource = null;
            RebuildRows();
        }

        /// <summary>
        /// Appends a new row to the bound table, seeding non-nullable columns whose
        /// <see cref="DataColumn.DefaultValue"/> is still <see cref="DBNull"/> with a
        /// type-appropriate empty value (wire-deserialized tables often lack the
        /// pinned defaults that schema-derived tables carry).
        /// </summary>
        public void AddRow()
        {
            if (_dataTable is null) return;

            var row = _dataTable.NewRow();
            foreach (DataColumn column in _dataTable.Columns)
            {
                if (!column.AllowDBNull
                    && (column.DefaultValue is null || column.DefaultValue == DBNull.Value))
                {
                    row[column] = FormDataObject.ResolveEmptyValueForType(column.DataType);
                }
            }
            // Attaching the row marks the data object dirty through its DataTable
            // event bridge; no explicit notification is needed here.
            _dataTable.Rows.Add(row);
        }

        /// <summary>
        /// Deletes the selected row (marks it <see cref="DataRowState.Deleted"/> so the
        /// save pipeline can translate the change). No-op when nothing is selected.
        /// </summary>
        public void DeleteSelectedRow()
        {
            if (SelectedItem is not DataRowView rowView) return;
            rowView.Row.Delete();
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

        private void RebuildFallbackColumns()
        {
            Columns.Clear();
            if (_dataTable is null) return;
            foreach (DataColumn column in _dataTable.Columns)
                Columns.Add(BuildColumn(new LayoutColumn(column.ColumnName, column.ColumnName, ControlType.TextEdit)));
        }

        private void RebuildColumns()
        {
            Columns.Clear();
            if (_layout is null) return;
            foreach (var column in EnumerateVisibleColumns(_layout))
                Columns.Add(BuildColumn(column));
        }

        private void RebuildRows()
        {
            // Re-realizing discards any cell that hosted an inline editor.
            _endActiveInlineEdit = null;
            ItemsSource = _dataTable?.DefaultView;
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

            if (IsAlwaysOnEditor(column.ControlType))
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
                templateColumn.CellTemplate = new FuncDataTemplate<DataRowView>(
                    (row, _) => new TextBlock
                    {
                        Text = FormatCell(row, fieldName, displayFormat, numberFormat),
                        Margin = new Thickness(8, 4),
                    },
                    supportsRecycling: true);
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

        // Display template for the popup-based editor columns. Boolean cells render a
        // centred checkbox in every state (a disabled checkbox reads better than
        // "True"/"False" text); the other types rest as plain formatted text and swap
        // in their editor on click — the swap is managed here, not by the DataGrid
        // edit pipeline, so opening the editor's popup cannot tear the editor down.
        private Control BuildInteractiveCell(DataRowView? rowView, LayoutColumn column)
        {
            var canEdit = !IsReadOnly && !column.ReadOnly && rowView is not null;

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

            if (!canEdit || rowView is null)
            {
                return new TextBlock
                {
                    Text = FormatCell(rowView, column.FieldName, column.DisplayFormat, column.NumberFormat),
                    Margin = new Thickness(8, 4),
                    VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Center,
                };
            }

            return BuildSwapCell(rowView, column);
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
                    ItemTemplate = new FuncDataTemplate<Bee.Definition.Collections.ListItem>(
                        (item, _) => new TextBlock { Text = item?.Text ?? string.Empty },
                        supportsRecycling: true),
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
            if (SelectedItem is not DataRowView rowView) return;
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
