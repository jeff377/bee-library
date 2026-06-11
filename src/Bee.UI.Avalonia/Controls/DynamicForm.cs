using System.Data;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Bee.Definition.Layouts;
using Bee.UI.Avalonia.Controls.Editors;
using Bee.UI.Avalonia.DataObjects;

namespace Bee.UI.Avalonia.Controls
{
    /// <summary>
    /// Avalonia <see cref="UserControl"/> that renders the master section(s) of a
    /// <see cref="FormLayout"/> by dispatching each <see cref="LayoutField"/> to the
    /// Avalonia control appropriate to its <see cref="ControlType"/>. Mirrors the MAUI
    /// <c>DynamicForm</c> structure and behaviour for cross-family parity.
    /// </summary>
    /// <remarks>
    /// Renders the master sections followed by the detail grids
    /// (<see cref="FormLayout.Details"/>), each as a bound <see cref="GridControl"/>.
    /// </remarks>
    public class DynamicForm : UserControl
    {
        /// <summary>
        /// Identifies the <see cref="FormLayout"/> styled property.
        /// </summary>
        public static readonly StyledProperty<FormLayout?> FormLayoutProperty =
            AvaloniaProperty.Register<DynamicForm, FormLayout?>(nameof(FormLayout));

        /// <summary>
        /// Identifies the <see cref="DataObject"/> styled property.
        /// </summary>
        public static readonly StyledProperty<FormDataObject?> DataObjectProperty =
            AvaloniaProperty.Register<DynamicForm, FormDataObject?>(nameof(DataObject));

        /// <summary>
        /// Identifies the <see cref="DetailEditMode"/> styled property.
        /// </summary>
        public static readonly StyledProperty<GridEditMode> DetailEditModeProperty =
            AvaloniaProperty.Register<DynamicForm, GridEditMode>(nameof(DetailEditMode), GridEditMode.InCell);

        static DynamicForm()
        {
            FormLayoutProperty.Changed.AddClassHandler<DynamicForm>((d, _) => d.Rebuild());
            DataObjectProperty.Changed.AddClassHandler<DynamicForm>((d, _) => d.Rebuild());
            DetailEditModeProperty.Changed.AddClassHandler<DynamicForm>((d, _) => d.Rebuild());
        }

        /// <summary>
        /// Initializes a new instance of <see cref="DynamicForm"/> with an empty content host.
        /// </summary>
        public DynamicForm()
        {
            Content = new StackPanel { Orientation = Orientation.Vertical };
        }

        /// <summary>
        /// Gets or sets the form layout that drives the rendering loop.
        /// </summary>
        public FormLayout? FormLayout
        {
            get => GetValue(FormLayoutProperty);
            set => SetValue(FormLayoutProperty, value);
        }

        /// <summary>
        /// Gets or sets the data object that backs two-way binding for each input.
        /// </summary>
        public FormDataObject? DataObject
        {
            get => GetValue(DataObjectProperty);
            set => SetValue(DataObjectProperty, value);
        }

        /// <summary>
        /// Gets or sets the editing model applied to every detail grid this form
        /// renders. The editing model is a UI-layer decision — the shared layout
        /// definitions stay framework-neutral — so the host sets it here once.
        /// </summary>
        public GridEditMode DetailEditMode
        {
            get => GetValue(DetailEditModeProperty);
            set => SetValue(DetailEditModeProperty, value);
        }

        /// <summary>
        /// Rebuilds the form against the current <see cref="DataObject"/> and
        /// <see cref="FormLayout"/>. Call this after mutating the
        /// <see cref="FormDataObject"/>'s internal <c>DataSet</c> in place — Avalonia's
        /// <see cref="StyledProperty{T}"/> only fires on reference changes, so an in-place
        /// mutation does not trigger the change callback that would otherwise rebuild
        /// automatically.
        /// </summary>
        public void Refresh() => Rebuild();

        private void Rebuild()
        {
            var host = new StackPanel { Orientation = Orientation.Vertical, Spacing = 8 };
            if (FormLayout is not null && DataObject is not null)
            {
                foreach (var section in EnumerateSections())
                    host.Children.Add(BuildSection(section));
                foreach (var detail in EnumerateDetails())
                    host.Children.Add(BuildDetailSection(detail));
            }
            Content = host;
        }

        private Border BuildSection(LayoutSection section)
        {
            var stack = new StackPanel { Orientation = Orientation.Vertical, Spacing = 4 };
            if (section.ShowCaption && !string.IsNullOrEmpty(section.Caption))
            {
                stack.Children.Add(new TextBlock
                {
                    Text = section.Caption,
                    FontWeight = FontWeight.Bold,
                });
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
            var columnCount = NormalizeColumnCount(FormLayout?.ColumnCount);
            var grid = new Grid
            {
                ColumnSpacing = 8,
                RowSpacing = 8,
            };
            for (int i = 0; i < columnCount; i++)
                grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

            int row = 0, col = 0;
            foreach (var field in EnumerateFields(section))
            {
                var (rowSpan, colSpan) = NormalizeSpans(field);

                // CSS-grid-like wrap: if the field would overflow the row, advance to next row first.
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
            stack.Children.Add(new TextBlock { Text = field.Caption });
            stack.Children.Add(BuildInputControl(field));
            return stack;
        }

        // Dispatches LayoutField.ControlType to the corresponding field editor; the
        // editor pulls its value, applies FormField metadata (MaxLength, ListItems)
        // and refreshes itself through the FormDataObject events.
        private Control BuildInputControl(LayoutField field)
        {
            var editor = FieldEditorFactory.Create(field.ControlType);
            ((IFieldEditor)editor).Bind(DataObject!, field);
            return editor;
        }

        private Border BuildDetailSection(LayoutGrid layout)
        {
            var stack = new StackPanel { Orientation = Orientation.Vertical, Spacing = 4 };
            if (!string.IsNullOrEmpty(layout.Caption))
            {
                stack.Children.Add(new TextBlock
                {
                    Text = layout.Caption,
                    FontWeight = FontWeight.Bold,
                });
            }

            var grid = new GridControl { MinHeight = 120, EditMode = DetailEditMode };
            grid.Bind(DataObject!, layout);
            if (DetailEditMode == GridEditMode.EditForm
                && layout.AllowActions.HasFlag(GridControlAllowActions.Edit))
            {
                grid.DoubleTapped += async (_, _) =>
                    await EditSelectedRowAsync(grid, layout).ConfigureAwait(true);
            }
            if (BuildDetailToolbar(layout, grid) is { } toolbar)
                stack.Children.Add(toolbar);
            stack.Children.Add(grid);

            return new Border
            {
                Padding = new Thickness(8),
                BorderThickness = new Thickness(1),
                BorderBrush = Brushes.Gray,
                Child = stack,
            };
        }

        private StackPanel? BuildDetailToolbar(LayoutGrid layout, GridControl grid)
        {
            var allowAdd = layout.AllowActions.HasFlag(GridControlAllowActions.Add);
            var allowDelete = layout.AllowActions.HasFlag(GridControlAllowActions.Delete);
            // In-cell editing needs no Edit button (cells edit in place); EditForm
            // surfaces one beside the double-tap gesture.
            var allowEdit = DetailEditMode == GridEditMode.EditForm
                && layout.AllowActions.HasFlag(GridControlAllowActions.Edit);
            if (!allowAdd && !allowEdit && !allowDelete) return null;

            var toolbar = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            if (allowAdd)
            {
                var addButton = new Button { Content = "Add" };
                addButton.Click += async (_, _) =>
                    await AddDetailRowAsync(grid, layout).ConfigureAwait(true);
                toolbar.Children.Add(addButton);
            }
            if (allowEdit)
            {
                var editButton = new Button { Content = "Edit" };
                editButton.Click += async (_, _) =>
                    await EditSelectedRowAsync(grid, layout).ConfigureAwait(true);
                toolbar.Children.Add(editButton);
            }
            if (allowDelete)
            {
                var deleteButton = new Button { Content = "Delete" };
                deleteButton.Click += (_, _) =>
                {
                    grid.EndEdit();
                    grid.DeleteSelectedRow();
                };
                toolbar.Children.Add(deleteButton);
            }
            return toolbar;
        }

        private async Task AddDetailRowAsync(GridControl grid, LayoutGrid layout)
        {
            grid.AddRow();
            if (DetailEditMode != GridEditMode.EditForm) return;

            var table = grid.DataTable;
            if (table is null || DataObject is null) return;
            var row = table.Rows[table.Rows.Count - 1];

            var committed = await RowEditDialog.ShowAsync(this, DataObject, layout, row).ConfigureAwait(true);
            if (committed)
            {
                RefreshAndFocusRow(grid, row);
            }
            else
            {
                // A cancelled Add removes the blank row again instead of leaving an
                // empty line in the detail table.
                table.Rows.Remove(row);
                grid.RefreshRows();
            }
        }

        private async Task EditSelectedRowAsync(GridControl grid, LayoutGrid layout)
        {
            if (DataObject is null) return;
            if (grid.SelectedItem is not DataRowView rowView) return;

            var row = rowView.Row;
            var committed = await RowEditDialog.ShowAsync(this, DataObject, layout, row).ConfigureAwait(true);
            if (committed)
                RefreshAndFocusRow(grid, row);
        }

        // Realized text cells capture their value at template build, so a committed
        // edit form re-realizes the rows and scrolls back to the affected row.
        private static void RefreshAndFocusRow(GridControl grid, DataRow row)
        {
            grid.RefreshRows();
            var rowView = grid.DataTable?.DefaultView
                .Cast<DataRowView>()
                .FirstOrDefault(v => ReferenceEquals(v.Row, row));
            if (rowView is null) return;
            grid.SelectedItem = rowView;
            grid.ScrollIntoView(rowView, null);
        }

        private IEnumerable<LayoutSection> EnumerateSections()
            => FormLayout?.Sections ?? Enumerable.Empty<LayoutSection>();

        private IEnumerable<LayoutGrid> EnumerateDetails()
            => FormLayout?.Details ?? Enumerable.Empty<LayoutGrid>();

        private static IEnumerable<LayoutField> EnumerateFields(LayoutSection section)
            => section.Fields?.Where(f => f.Visible) ?? Enumerable.Empty<LayoutField>();

        private static int NormalizeColumnCount(int? columnCount)
        {
            var n = columnCount ?? 1;
            return n < 1 ? 1 : n;
        }

        private static (int rowSpan, int columnSpan) NormalizeSpans(LayoutField field)
        {
            var rowSpan = field.RowSpan < 1 ? 1 : field.RowSpan;
            var colSpan = field.ColumnSpan < 1 ? 1 : field.ColumnSpan;
            return (rowSpan, colSpan);
        }
    }
}
