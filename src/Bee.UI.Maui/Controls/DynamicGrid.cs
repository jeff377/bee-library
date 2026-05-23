using System.Data;
using System.Globalization;
using Bee.Definition;
using Bee.Definition.Layouts;

namespace Bee.UI.Maui.Controls
{
    /// <summary>
    /// MAUI <see cref="ContentView"/> that renders a <see cref="LayoutGrid"/> over a
    /// <see cref="DataTable"/> and raises <see cref="RowSelected"/> with the row's
    /// <see cref="SysFields.RowId"/> Guid when the user taps a row. Mirrors the
    /// Blazor <c>DynamicGrid</c> component structure for cross-family parity.
    /// </summary>
    /// <remarks>
    /// The grid is intentionally <em>presentation-only</em>: the host (typically
    /// <see cref="FormPage"/>) owns the call to <c>FormApiConnector.GetListAsync</c>
    /// and passes the resulting <see cref="DataTable"/> in via <see cref="Rows"/>.
    /// Keeping the fetch outside the grid lets the host coordinate refresh with
    /// the master form (e.g. re-load the list after Save / Delete).
    /// </remarks>
    public class DynamicGrid : ContentView
    {
        /// <summary>
        /// Identifies the <see cref="ListLayout"/> bindable property.
        /// </summary>
        // The property is named ListLayout rather than the Blazor-side "Layout" because
        // VisualElement already exposes a public Layout(Rect) method; reusing the name
        // would force a `new` modifier and shadow MAUI's layout-pass entry point.
        public static readonly BindableProperty ListLayoutProperty = BindableProperty.Create(
            nameof(ListLayout),
            typeof(LayoutGrid),
            typeof(DynamicGrid),
            propertyChanged: (b, _, _) => ((DynamicGrid)b).Rebuild());

        /// <summary>
        /// Identifies the <see cref="Rows"/> bindable property.
        /// </summary>
        public static readonly BindableProperty RowsProperty = BindableProperty.Create(
            nameof(Rows),
            typeof(DataTable),
            typeof(DynamicGrid),
            propertyChanged: (b, _, _) => ((DynamicGrid)b).Rebuild());

        /// <summary>
        /// Identifies the <see cref="EmptyText"/> bindable property.
        /// </summary>
        public static readonly BindableProperty EmptyTextProperty = BindableProperty.Create(
            nameof(EmptyText),
            typeof(string),
            typeof(DynamicGrid),
            defaultValue: "No data.",
            propertyChanged: (b, _, _) => ((DynamicGrid)b).Rebuild());

        /// <summary>
        /// Initializes a new instance of <see cref="DynamicGrid"/> with an empty placeholder.
        /// </summary>
        public DynamicGrid()
        {
            Content = new Label { Text = EmptyText ?? "No data." };
        }

        /// <summary>
        /// Gets or sets the list layout that defines the visible columns.
        /// </summary>
        public LayoutGrid? ListLayout
        {
            get => (LayoutGrid?)GetValue(ListLayoutProperty);
            set => SetValue(ListLayoutProperty, value);
        }

        /// <summary>
        /// Gets or sets the data rows to render.
        /// </summary>
        public DataTable? Rows
        {
            get => (DataTable?)GetValue(RowsProperty);
            set => SetValue(RowsProperty, value);
        }

        /// <summary>
        /// Gets or sets the placeholder text shown when there is no data.
        /// </summary>
        public string EmptyText
        {
            get => (string)GetValue(EmptyTextProperty);
            set => SetValue(EmptyTextProperty, value);
        }

        /// <summary>
        /// Raised when the user taps a row; carries the row's
        /// <see cref="SysFields.RowId"/> Guid. Rows without a parseable
        /// <c>sys_rowid</c> are silently ignored.
        /// </summary>
        public event EventHandler<Guid>? RowSelected;

        private void Rebuild()
        {
            if (ListLayout is null || Rows is null || Rows.Rows.Count == 0)
            {
                Content = new Label { Text = EmptyText ?? string.Empty };
                return;
            }

            var visibleColumns = EnumerateVisibleColumns(ListLayout).ToList();
            var grid = new Grid
            {
                ColumnSpacing = 0,
                RowSpacing = 0,
                ColumnDefinitions = BuildColumnDefinitions(visibleColumns),
            };

            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            for (int i = 0; i < visibleColumns.Count; i++)
            {
                var header = BuildHeaderCell(visibleColumns[i].Caption);
                Grid.SetRow(header, 0);
                Grid.SetColumn(header, i);
                grid.Add(header);
            }

            int rowIndex = 1;
            foreach (DataRow dataRow in Rows.Rows)
            {
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                for (int colIndex = 0; colIndex < visibleColumns.Count; colIndex++)
                {
                    var cell = BuildBodyCell(FormatCell(dataRow, visibleColumns[colIndex]));
                    Grid.SetRow(cell, rowIndex);
                    Grid.SetColumn(cell, colIndex);
                    AttachRowTap(cell, dataRow);
                    grid.Add(cell);
                }
                rowIndex++;
            }

            Content = grid;
        }

        private void AttachRowTap(View cell, DataRow row)
        {
            var tap = new TapGestureRecognizer();
            tap.Tapped += (_, _) => OnRowTapped(row);
            cell.GestureRecognizers.Add(tap);
        }

        private void OnRowTapped(DataRow row)
        {
            var handler = RowSelected;
            if (handler is null) return;
            if (!TryGetRowId(row, out var rowId)) return;
            handler(this, rowId);
        }

        private static IEnumerable<LayoutColumn> EnumerateVisibleColumns(LayoutGrid layout)
            => layout.Columns?.Where(c => c.Visible) ?? Enumerable.Empty<LayoutColumn>();

        private static ColumnDefinitionCollection BuildColumnDefinitions(IList<LayoutColumn> columns)
        {
            var defs = new ColumnDefinitionCollection();
            foreach (var column in columns)
            {
                // LayoutColumn.Width is in CSS pixels on the Blazor side; treating it as
                // device-independent units gives an equivalent column-width hint on MAUI.
                var width = column.Width > 0
                    ? new GridLength(column.Width, GridUnitType.Absolute)
                    : GridLength.Star;
                defs.Add(new ColumnDefinition { Width = width });
            }
            return defs;
        }

        private static Label BuildHeaderCell(string caption)
            => new()
            {
                Text = caption,
                FontAttributes = FontAttributes.Bold,
                Padding = new Thickness(8, 4),
            };

        private static Label BuildBodyCell(string text)
            => new()
            {
                Text = text,
                Padding = new Thickness(8, 4),
            };

        private static bool TryGetRowId(DataRow row, out Guid rowId)
        {
            rowId = Guid.Empty;
            if (!row.Table.Columns.Contains(SysFields.RowId)) return false;
            var raw = row[SysFields.RowId];
            if (raw is null || raw == DBNull.Value) return false;
            if (raw is Guid g) { rowId = g; return true; }
            return Guid.TryParse(raw.ToString(), out rowId);
        }

        private static string FormatCell(DataRow row, LayoutColumn column)
        {
            if (!row.Table.Columns.Contains(column.FieldName)) return string.Empty;
            var raw = row[column.FieldName];
            if (raw is null || raw == DBNull.Value) return string.Empty;

            if (!string.IsNullOrEmpty(column.DisplayFormat) && raw is IFormattable formattableDisplay)
                return formattableDisplay.ToString(column.DisplayFormat, CultureInfo.InvariantCulture);
            if (!string.IsNullOrEmpty(column.NumberFormat) && raw is IFormattable formattableNumber)
                return formattableNumber.ToString(column.NumberFormat, CultureInfo.InvariantCulture);

            return raw switch
            {
                DateTime dt => dt.TimeOfDay == TimeSpan.Zero
                    ? dt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                    : dt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
                _ => raw.ToString() ?? string.Empty,
            };
        }
    }
}
