using System.Data;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Bee.Definition;
using Bee.Definition.Layouts;

namespace Bee.UI.Avalonia.Controls
{
    /// <summary>
    /// Avalonia <see cref="UserControl"/> that renders a <see cref="LayoutGrid"/> over a
    /// <see cref="DataTable"/> using the built-in <see cref="DataGrid"/> control.
    /// Raises <see cref="RowSelected"/> with the row's <see cref="SysFields.RowId"/>
    /// Guid when the user selects a row. Mirrors the MAUI / Blazor <c>DynamicGrid</c>
    /// structure for cross-family parity.
    /// </summary>
    /// <remarks>
    /// The grid is intentionally <em>presentation-only</em>: the host (typically
    /// <see cref="FormView"/>) owns the call to <c>FormApiConnector.GetListAsync</c>
    /// and passes the resulting <see cref="DataTable"/> in via <see cref="Rows"/>.
    /// Keeping the fetch outside the grid lets the host coordinate refresh with
    /// the master form (e.g. re-load the list after Save / Delete).
    /// <para>
    /// Each column uses a <see cref="DataGridTemplateColumn"/> with a
    /// <see cref="FuncDataTemplate{T}"/> that fetches the cell value from
    /// <see cref="DataRowView"/>'s indexer at render time. The straightforward
    /// <c>new Binding("[FieldName]")</c> path that WPF uses does <em>not</em>
    /// resolve against <see cref="DataRowView"/>'s string indexer in Avalonia 12 —
    /// the binding engine looks up CLR properties / typed indexers and never
    /// reaches <c>DataRowView.this[string]</c> — so cells silently render empty
    /// even though <see cref="DataGrid.ItemsSource"/> iterates the rows.
    /// </para>
    /// </remarks>
    public class DynamicGrid : UserControl
    {
        private readonly DataGrid _dataGrid;
        private readonly TextBlock _emptyLabel;

        /// <summary>
        /// Identifies the <see cref="ListLayout"/> styled property.
        /// </summary>
        public static readonly StyledProperty<LayoutGrid?> ListLayoutProperty =
            AvaloniaProperty.Register<DynamicGrid, LayoutGrid?>(nameof(ListLayout));

        /// <summary>
        /// Identifies the <see cref="Rows"/> styled property.
        /// </summary>
        public static readonly StyledProperty<DataTable?> RowsProperty =
            AvaloniaProperty.Register<DynamicGrid, DataTable?>(nameof(Rows));

        /// <summary>
        /// Identifies the <see cref="EmptyText"/> styled property.
        /// </summary>
        public static readonly StyledProperty<string> EmptyTextProperty =
            AvaloniaProperty.Register<DynamicGrid, string>(nameof(EmptyText), defaultValue: "No data.");

        static DynamicGrid()
        {
            ListLayoutProperty.Changed.AddClassHandler<DynamicGrid>((d, _) => d.Rebuild());
            RowsProperty.Changed.AddClassHandler<DynamicGrid>((d, _) => d.Rebuild());
            EmptyTextProperty.Changed.AddClassHandler<DynamicGrid>((d, _) => d.Rebuild());
        }

        /// <summary>
        /// Initializes a new instance of <see cref="DynamicGrid"/> with an empty placeholder.
        /// </summary>
        public DynamicGrid()
        {
            _emptyLabel = new TextBlock { Text = EmptyText };
            _dataGrid = new DataGrid
            {
                IsReadOnly = true,
                CanUserResizeColumns = true,
                SelectionMode = DataGridSelectionMode.Single,
                AutoGenerateColumns = false,
            };
            _dataGrid.SelectionChanged += OnDataGridSelectionChanged;
            Content = _emptyLabel;
        }

        /// <summary>
        /// Gets or sets the list layout that defines the visible columns.
        /// </summary>
        public LayoutGrid? ListLayout
        {
            get => GetValue(ListLayoutProperty);
            set => SetValue(ListLayoutProperty, value);
        }

        /// <summary>
        /// Gets or sets the data rows to render.
        /// </summary>
        public DataTable? Rows
        {
            get => GetValue(RowsProperty);
            set => SetValue(RowsProperty, value);
        }

        /// <summary>
        /// Gets or sets the placeholder text shown when there is no data.
        /// </summary>
        public string EmptyText
        {
            get => GetValue(EmptyTextProperty);
            set => SetValue(EmptyTextProperty, value);
        }

        /// <summary>
        /// Raised when the user selects a row; carries the row's
        /// <see cref="SysFields.RowId"/> Guid. Rows without a parseable
        /// <c>sys_rowid</c> are silently ignored.
        /// </summary>
        public event EventHandler<Guid>? RowSelected;

        private void Rebuild()
        {
            if (ListLayout is null || Rows is null || Rows.Rows.Count == 0)
            {
                _emptyLabel.Text = EmptyText ?? string.Empty;
                Content = _emptyLabel;
                return;
            }

            _dataGrid.Columns.Clear();
            foreach (var column in EnumerateVisibleColumns(ListLayout))
            {
                _dataGrid.Columns.Add(BuildColumn(column));
            }
            _dataGrid.ItemsSource = Rows.DefaultView;
            Content = _dataGrid;
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

        private void OnDataGridSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            var handler = RowSelected;
            if (handler is null) return;
            if (_dataGrid.SelectedItem is not DataRowView rowView) return;
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
