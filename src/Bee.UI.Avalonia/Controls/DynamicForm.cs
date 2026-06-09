using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using Bee.Definition.Collections;
using Bee.Definition.Layouts;
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
    /// This phase is layout-only and renders the master area only. Detail grids
    /// (<see cref="FormLayout.Details"/>) are wired up alongside <c>DynamicGrid</c>.
    /// </remarks>
    public class DynamicForm : UserControl
    {
        private static readonly ListItem[] _emptyOptions = Array.Empty<ListItem>();

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

        static DynamicForm()
        {
            FormLayoutProperty.Changed.AddClassHandler<DynamicForm>((d, _) => d.Rebuild());
            DataObjectProperty.Changed.AddClassHandler<DynamicForm>((d, _) => d.Rebuild());
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
            var rawValue = DataObject?.GetField(field.FieldName) ?? string.Empty;
            var stack = new StackPanel { Orientation = Orientation.Vertical, Spacing = 2 };
            stack.Children.Add(new TextBlock { Text = field.Caption });
            stack.Children.Add(BuildInputControl(field, rawValue));
            return stack;
        }

        // Dispatches LayoutField.ControlType to the corresponding Avalonia control, mirroring
        // the MAUI / Blazor DynamicForm switch.
        private Control BuildInputControl(LayoutField field, string rawValue)
        {
            switch (field.ControlType)
            {
                case ControlType.CheckEdit:
                    {
                        var control = new CheckBox
                        {
                            IsChecked = string.Equals(rawValue, bool.TrueString, StringComparison.OrdinalIgnoreCase),
                            IsEnabled = !field.ReadOnly,
                        };
                        control.IsCheckedChanged += (_, _) =>
                            DataObject?.SetField(field.FieldName, (control.IsChecked ?? false).ToString());
                        return control;
                    }
                case ControlType.DateEdit:
                    return BuildDatePicker(field, rawValue, "yyyy-MM-dd");
                case ControlType.YearMonthEdit:
                    return BuildDatePicker(field, rawValue, "yyyy-MM");
                case ControlType.MemoEdit:
                    {
                        var control = new TextBox
                        {
                            Text = rawValue,
                            IsReadOnly = field.ReadOnly,
                            AcceptsReturn = true,
                            TextWrapping = TextWrapping.Wrap,
                            MinHeight = 60,
                        };
                        control.TextChanged += (_, _) =>
                            DataObject?.SetField(field.FieldName, control.Text);
                        return control;
                    }
                case ControlType.DropDownEdit:
                    {
                        var items = EnumerateOptions(field).ToList();
                        var combo = new ComboBox
                        {
                            IsEnabled = !field.ReadOnly,
                            ItemsSource = items,
                            ItemTemplate = new FuncDataTemplate<ListItem>(
                                (item, _) => new TextBlock { Text = item?.Text ?? string.Empty },
                                supportsRecycling: true),
                        };
                        var selected = items.FirstOrDefault(i =>
                            string.Equals(i.Value, rawValue, StringComparison.Ordinal));
                        if (selected is not null)
                            combo.SelectedItem = selected;
                        combo.SelectionChanged += (_, _) =>
                        {
                            var item = combo.SelectedItem as ListItem;
                            DataObject?.SetField(field.FieldName, item?.Value);
                        };
                        return combo;
                    }
                default:
                    {
                        var control = new TextBox
                        {
                            Text = rawValue,
                            IsReadOnly = field.ReadOnly,
                        };
                        control.TextChanged += (_, _) =>
                            DataObject?.SetField(field.FieldName, control.Text);
                        return control;
                    }
            }
        }

        private DatePicker BuildDatePicker(LayoutField field, string rawValue, string format)
        {
            var picker = new DatePicker
            {
                IsEnabled = !field.ReadOnly,
            };
            if (DateTime.TryParse(
                    rawValue,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeLocal,
                    out var parsed))
            {
                picker.SelectedDate = new DateTimeOffset(parsed.Date, TimeSpan.Zero);
            }
            picker.SelectedDateChanged += (_, e) =>
            {
                if (e.NewDate is { } date)
                    DataObject?.SetField(
                        field.FieldName,
                        date.DateTime.ToString(format, CultureInfo.InvariantCulture));
                else
                    DataObject?.SetField(field.FieldName, null);
            };
            return picker;
        }

        private IEnumerable<LayoutSection> EnumerateSections()
            => FormLayout?.Sections ?? Enumerable.Empty<LayoutSection>();

        private static IEnumerable<LayoutField> EnumerateFields(LayoutSection section)
            => section.Fields?.Where(f => f.Visible) ?? Enumerable.Empty<LayoutField>();

        private IEnumerable<ListItem> EnumerateOptions(LayoutField field)
        {
            var formField = DataObject?.GetFormField(field.FieldName);
            return formField?.ListItems ?? (IEnumerable<ListItem>)_emptyOptions;
        }

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
