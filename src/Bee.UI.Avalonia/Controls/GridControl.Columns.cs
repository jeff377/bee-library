using System.Data;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Bee.Definition.Layouts;

namespace Bee.UI.Avalonia.Controls
{
    /// <summary>
    /// Column-building half of <see cref="GridControl"/> (rebuilding the DataGrid columns and
    /// their headers). Split out for file size only; behaviour is unchanged.
    /// </summary>
    public partial class GridControl
    {
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
            // resolves the correct display format regardless of when the template fires.
            var displayFormat = column.DisplayFormat;
            var numberFormat = column.NumberFormat;

            var templateColumn = new DataGridTemplateColumn
            {
                Header = BuildColumnHeader(column),
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
                            ? FormatCellForColumn(row, column)
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

        // Read-only / required columns look identical to plain ones at rest, so the header marks
        // the cue: a read-only column's caption is parenthesised (e.g. "(Amount)"), a required
        // column's caption is blue (see FieldCaptionStyle). Keyed on the layout ReadOnly flag, NOT
        // the DataGrid column IsReadOnly: lookup and popup-editor columns set IsReadOnly to bypass
        // the edit pipeline while staying editable through click-to-swap, so they must not show
        // the read-only cue.
        private static object BuildColumnHeader(LayoutColumn column)
        {
            var caption = FieldCaptionStyle.FormatCaption(column.Caption, column.ReadOnly);
            var brush = FieldCaptionStyle.GetCaptionForeground(column.ReadOnly, column.Required);
            if (brush is null)
                return caption;
            return new TextBlock
            {
                Text = caption,
                Foreground = brush,
            };
        }
    }
}
