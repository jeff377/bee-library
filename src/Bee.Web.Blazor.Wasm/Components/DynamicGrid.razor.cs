using System.Data;
using System.Globalization;
using Bee.Definition;
using Bee.Definition.Layouts;
using Microsoft.AspNetCore.Components;

namespace Bee.Web.Blazor.Wasm.Components
{
    /// <summary>
    /// Code-behind for <c>DynamicGrid.razor</c>. Renders a <see cref="LayoutGrid"/>
    /// over a <see cref="DataTable"/> and raises <see cref="OnRowSelected"/> with
    /// the <see cref="SysFields.RowId"/> Guid when a row is clicked.
    /// </summary>
    /// <remarks>
    /// The grid is intentionally <em>presentation-only</em>: the host (typically
    /// <c>FormPage</c>) owns the call to <c>FormApiConnector.GetListAsync</c> and
    /// passes the resulting <see cref="DataTable"/> in via <see cref="Rows"/>.
    /// Keeping the fetch outside the grid lets the host coordinate refresh with
    /// the master form (e.g. re-load the list after Save / Delete).
    /// </remarks>
    public partial class DynamicGrid : ComponentBase
    {
        /// <summary>
        /// Gets or sets the list layout that defines the visible columns.
        /// </summary>
        [Parameter]
        public LayoutGrid? Layout { get; set; }

        /// <summary>
        /// Gets or sets the data rows to render.
        /// </summary>
        [Parameter]
        public DataTable? Rows { get; set; }

        /// <summary>
        /// Invoked when the user clicks a row; receives the row's
        /// <see cref="SysFields.RowId"/> Guid. Rows without a parseable
        /// <c>sys_rowid</c> are silently ignored.
        /// </summary>
        [Parameter]
        public EventCallback<Guid> OnRowSelected { get; set; }

        /// <summary>
        /// Gets or sets the placeholder text shown when there is no data.
        /// </summary>
        [Parameter]
        public string EmptyText { get; set; } = "No data.";

        private IEnumerable<LayoutColumn> VisibleColumns
            => Layout?.Columns?.Where(c => c.Visible) ?? Enumerable.Empty<LayoutColumn>();

        private async Task OnRowClickAsync(DataRow row)
        {
            if (!OnRowSelected.HasDelegate) return;
            if (!TryGetRowId(row, out var rowId)) return;
            await OnRowSelected.InvokeAsync(rowId);
        }

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

        private static string BuildColumnStyle(LayoutColumn column)
            => column.Width > 0
                ? string.Create(CultureInfo.InvariantCulture, $"width:{column.Width}px")
                : string.Empty;
    }
}
