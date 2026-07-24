using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Bee.Definition.Layouts;
using Bee.UI.Avalonia.Controls;
using Bee.UI.Avalonia.Controls.Editors;

namespace Bee.UI.Avalonia.Views
{
    /// <summary>
    /// Record-rendering half of <see cref="FormView"/> (master sections, field grids, input controls
    /// and detail sections). Split out for file size only; behaviour is unchanged.
    /// </summary>
    public partial class FormView
    {
        // ---- Record rendering (master sections + detail grids) ----

        private void Rebuild()
        {
            _formHost.Children.Clear();
            // Grids are recreated below; drop the stale references before repopulating.
            _detailGrids.Clear();
            if (_formLayout is null || _dataObject is null) return;

            // Build against the current width so the first render (which may run before any Bounds
            // notification) already reflects the compact state; resize crossings re-enter via
            // ApplyResponsiveState.
            _isCompact = IsCompactWidth(GetViewportWidth(), CompactWidthThreshold);

            foreach (var section in EnumerateSections())
                _formHost.Children.Add(BuildSection(section));
            foreach (var detail in EnumerateDetails())
                _formHost.Children.Add(BuildDetailSection(detail));
        }

        private Border BuildSection(LayoutSection section)
        {
            var stack = new StackPanel { Orientation = Orientation.Vertical, Spacing = 4 };
            if (section.ShowCaption && !string.IsNullOrEmpty(section.Caption))
            {
                stack.Children.Add(new TextBlock { Text = section.Caption, FontWeight = FontWeight.Bold });
            }

            stack.Children.Add(BuildFieldGrid(section));

            return new Border
            {
                Padding = new Thickness(8),
                BorderThickness = new Thickness(1),
                BorderBrush = Brushes.Gray,
                Child = stack,
            };
        }

        private Grid BuildFieldGrid(LayoutSection section)
        {
            var columnCount = EffectiveColumnCount();
            var grid = new Grid { ColumnSpacing = 8, RowSpacing = 8 };
            for (int i = 0; i < columnCount; i++)
                grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

            int row = 0, col = 0;
            foreach (var field in EnumerateFields(section))
            {
                var (rowSpan, colSpan) = NormalizeSpans(field);

                // CSS-grid-like wrap: if the field would overflow the row, advance first.
                if (col + colSpan > columnCount)
                {
                    row++;
                    col = 0;
                }

                while (grid.RowDefinitions.Count < row + rowSpan)
                    grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

                var cell = BuildFieldCell(field);
                Grid.SetRow(cell, row);
                Grid.SetColumn(cell, col);
                Grid.SetRowSpan(cell, rowSpan);
                Grid.SetColumnSpan(cell, colSpan);
                grid.Children.Add(cell);

                col += colSpan;
                if (col >= columnCount)
                {
                    row++;
                    col = 0;
                }
            }

            return grid;
        }

        private StackPanel BuildFieldCell(LayoutField field)
        {
            var stack = new StackPanel { Orientation = Orientation.Vertical, Spacing = 2 };
            var caption = new TextBlock { Text = field.Caption };
            // A required field's caption is blue; a read-only field's caption stays the theme
            // default — its cue is the editor's read-only underline (the detail grid, which has no
            // such per-cell visual, parenthesises the header instead). See FieldCaptionStyle.
            if (Controls.FieldCaptionStyle.GetCaptionForeground(field.ReadOnly, field.Required) is { } brush)
                caption.Foreground = brush;
            stack.Children.Add(caption);
            stack.Children.Add(BuildInputControl(field));
            return stack;
        }

        // Dispatches LayoutField.ControlType to the corresponding field editor; the editor
        // pulls its value, applies FormField metadata and refreshes through FormDataObject.
        private Control BuildInputControl(LayoutField field)
        {
            var editor = FieldEditorFactory.Create(field.ControlType);
            // Currency/unit-aware cell decimals (Tier 2), set before Bind so the first render uses them.
            if (editor is NumericEdit numeric)
            {
                numeric.CurrencySettings = _roundingContext.CurrencySettings;
                numeric.UnitSettings = _roundingContext.UnitSettings;
                numeric.DefaultCurrencyCode = _roundingContext.Company?.DefaultCurrency ?? string.Empty;
            }
            ((IFieldEditor)editor).Bind(_dataObject!, field);
            return editor;
        }

        private Border BuildDetailSection(LayoutGrid layout)
        {
            var stack = new StackPanel { Orientation = Orientation.Vertical, Spacing = 4 };
            if (!string.IsNullOrEmpty(layout.Caption))
            {
                stack.Children.Add(new TextBlock { Text = layout.Caption, FontWeight = FontWeight.Bold });
            }

            // The grid carries its own icon toolbar and edit-form flow; the form only supplies the
            // layout, the data object and the editing model (responsive — see EffectiveDetailEditMode).
            var grid = new GridControl { MinHeight = 120, EditMode = EffectiveDetailEditMode() };
            // Currency/unit-aware cell decimals (Tier 2), set before Bind so the first render uses them.
            grid.CurrencySettings = _roundingContext.CurrencySettings;
            grid.UnitSettings = _roundingContext.UnitSettings;
            grid.DefaultCurrencyCode = _roundingContext.Company?.DefaultCurrency ?? string.Empty;
            grid.Bind(_dataObject!, layout);
            // Track by table name so a live recompute of a detail row can refresh this grid.
            _detailGrids[layout.TableName] = grid;
            stack.Children.Add(grid);

            return new Border
            {
                Padding = new Thickness(8),
                BorderThickness = new Thickness(1),
                BorderBrush = Brushes.Gray,
                Child = stack,
            };
        }
    }
}
