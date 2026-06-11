using System.Data;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
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
    /// The grid is read-only in this phase; in-grid editing and the
    /// <see cref="LayoutGrid.AllowActions"/> row actions arrive in a later stage
    /// of plan-avalonia-grid-control. An empty table renders headers only — hosts
    /// that need an "empty" hint overlay their own placeholder.
    /// </para>
    /// </remarks>
    public class GridControl : DataGrid, IBindTableControl, IUIControl
    {
        /// <summary>
        /// Identifies the <see cref="TableName"/> styled property.
        /// </summary>
        public static readonly StyledProperty<string> TableNameProperty =
            AvaloniaProperty.Register<GridControl, string>(nameof(TableName), string.Empty);

        private LayoutGrid? _layout;
        private DataTable? _dataTable;

        /// <summary>
        /// Initializes a new instance of <see cref="GridControl"/> with read-only,
        /// single-selection defaults.
        /// </summary>
        public GridControl()
        {
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
            var tables = dataObject.DataSet.Tables;
            var table = tables.Contains(layout.TableName) ? tables[layout.TableName] : null;
            Bind(layout, table);
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
            _layout = layout;
            TableName = layout.TableName;
            _dataTable = rows;
            RebuildColumns();
            RebuildRows();
        }

        /// <summary>
        /// Ends the current edit operation.
        /// </summary>
        /// <remarks>
        /// No-op in the read-only phase: there is no in-grid editing surface yet.
        /// A later stage of plan-avalonia-grid-control implements commit semantics.
        /// </remarks>
        public void EndEdit()
        {
        }

        /// <inheritdoc />
        public void SetControlState(SingleFormMode formMode)
        {
            // Editing arrives in a later stage; the grid stays read-only in every mode.
            IsReadOnly = true;
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
            ItemsSource = _dataTable?.DefaultView;
        }

        private static DataGridTemplateColumn BuildColumn(LayoutColumn column)
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
                CellTemplate = new FuncDataTemplate<DataRowView>(
                    (row, _) => new TextBlock
                    {
                        Text = FormatCell(row, fieldName, displayFormat, numberFormat),
                        Margin = new Thickness(8, 4),
                    },
                    supportsRecycling: true),
            };

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
