using System.Globalization;
using Bee.Definition.Collections;
using Bee.Definition.Layouts;
using Bee.UI.Maui.DataObjects;

namespace Bee.UI.Maui.Controls
{
    /// <summary>
    /// MAUI <see cref="ContentView"/> that renders the master section(s) of a
    /// <see cref="FormLayout"/> by dispatching each <see cref="LayoutField"/> to the MAUI
    /// control appropriate to its <see cref="ControlType"/>. Mirrors the Blazor
    /// <c>DynamicForm</c> component structure for cross-family parity.
    /// </summary>
    /// <remarks>
    /// Phase 1a is layout-only and renders the master area only. Detail grids
    /// (<see cref="FormLayout.Details"/>) are wired up in Phase 1b together with
    /// <c>DynamicGrid</c>.
    /// </remarks>
    public class DynamicForm : ContentView
    {
        private static readonly ListItem[] _emptyOptions = Array.Empty<ListItem>();

        /// <summary>
        /// Identifies the <see cref="FormLayout"/> bindable property.
        /// </summary>
        // The property is named FormLayout rather than the Blazor-side "Layout" because
        // VisualElement already exposes a public Layout(Rect) method; reusing the name
        // would force a `new` modifier and shadow MAUI's layout-pass entry point.
        public static readonly BindableProperty FormLayoutProperty = BindableProperty.Create(
            nameof(FormLayout),
            typeof(FormLayout),
            typeof(DynamicForm),
            propertyChanged: (b, _, _) => ((DynamicForm)b).Rebuild());

        /// <summary>
        /// Identifies the <see cref="DataObject"/> bindable property.
        /// </summary>
        public static readonly BindableProperty DataObjectProperty = BindableProperty.Create(
            nameof(DataObject),
            typeof(FormDataObject),
            typeof(DynamicForm),
            propertyChanged: (b, _, _) => ((DynamicForm)b).Rebuild());

        /// <summary>
        /// Initializes a new instance of <see cref="DynamicForm"/> with an empty content host.
        /// </summary>
        public DynamicForm()
        {
            Content = new VerticalStackLayout();
        }

        /// <summary>
        /// Gets or sets the form layout that drives the rendering loop.
        /// </summary>
        public FormLayout? FormLayout
        {
            get => (FormLayout?)GetValue(FormLayoutProperty);
            set => SetValue(FormLayoutProperty, value);
        }

        /// <summary>
        /// Gets or sets the data object that backs two-way binding for each input.
        /// </summary>
        public FormDataObject? DataObject
        {
            get => (FormDataObject?)GetValue(DataObjectProperty);
            set => SetValue(DataObjectProperty, value);
        }

        private void Rebuild()
        {
            var host = new VerticalStackLayout { Spacing = 8 };
            if (FormLayout is not null && DataObject is not null)
            {
                foreach (var section in EnumerateSections())
                    host.Add(BuildSection(section));
            }
            Content = host;
        }

        private Border BuildSection(LayoutSection section)
        {
            var stack = new VerticalStackLayout { Spacing = 4 };
            if (section.ShowCaption && !string.IsNullOrEmpty(section.Caption))
                stack.Add(new Label { Text = section.Caption, FontAttributes = FontAttributes.Bold });

            stack.Add(BuildFieldGrid(section));

            return new Border
            {
                Padding = new Thickness(8),
                StrokeThickness = 1,
                Content = stack
            };
        }

        private Grid BuildFieldGrid(LayoutSection section)
        {
            var columnCount = NormalizeColumnCount(FormLayout?.ColumnCount);
            var grid = new Grid
            {
                ColumnSpacing = 8,
                RowSpacing = 8,
                ColumnDefinitions = BuildColumnDefinitions(columnCount)
            };

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
                    grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                var cell = BuildFieldCell(field);
                Grid.SetRow(cell, row);
                Grid.SetColumn(cell, col);
                Grid.SetRowSpan(cell, rowSpan);
                Grid.SetColumnSpan(cell, colSpan);
                grid.Add(cell);

                col += colSpan;
                if (col >= columnCount)
                {
                    row++;
                    col = 0;
                }
            }

            return grid;
        }

        private VerticalStackLayout BuildFieldCell(LayoutField field)
        {
            var rawValue = DataObject?.GetField(field.FieldName) ?? string.Empty;
            var stack = new VerticalStackLayout { Spacing = 2 };
            stack.Add(new Label { Text = field.Caption });
            stack.Add(BuildInputControl(field, rawValue));
            return stack;
        }

        // Dispatches LayoutField.ControlType to the corresponding MAUI control, mirroring
        // the Blazor DynamicForm switch over <input type="..."> / <select> / <textarea>.
        private View BuildInputControl(LayoutField field, string rawValue)
        {
            switch (field.ControlType)
            {
                case ControlType.CheckEdit:
                    {
                        var control = new CheckBox
                        {
                            IsChecked = string.Equals(rawValue, bool.TrueString, StringComparison.OrdinalIgnoreCase),
                            IsEnabled = !field.ReadOnly
                        };
                        control.CheckedChanged += (_, e) =>
                            DataObject?.SetField(field.FieldName, e.Value.ToString());
                        return control;
                    }
                case ControlType.DateEdit:
                    return BuildDatePicker(field, rawValue, "yyyy-MM-dd");
                case ControlType.YearMonthEdit:
                    return BuildDatePicker(field, rawValue, "yyyy-MM");
                case ControlType.MemoEdit:
                    {
                        var control = new Editor
                        {
                            Text = rawValue,
                            IsReadOnly = field.ReadOnly,
                            AutoSize = EditorAutoSizeOption.TextChanges
                        };
                        control.TextChanged += (_, e) =>
                            DataObject?.SetField(field.FieldName, e.NewTextValue);
                        return control;
                    }
                case ControlType.DropDownEdit:
                    {
                        var items = EnumerateOptions(field).ToList();
                        var picker = new Picker
                        {
                            IsEnabled = !field.ReadOnly,
                            ItemsSource = items,
                            ItemDisplayBinding = new Binding(nameof(ListItem.Text))
                        };
                        var selected = items.FirstOrDefault(i =>
                            string.Equals(i.Value, rawValue, StringComparison.Ordinal));
                        if (selected is not null)
                            picker.SelectedItem = selected;
                        picker.SelectedIndexChanged += (_, _) =>
                        {
                            var item = picker.SelectedItem as ListItem;
                            DataObject?.SetField(field.FieldName, item?.Value);
                        };
                        return picker;
                    }
                default:
                    {
                        var control = new Entry
                        {
                            Text = rawValue,
                            IsReadOnly = field.ReadOnly
                        };
                        control.TextChanged += (_, e) =>
                            DataObject?.SetField(field.FieldName, e.NewTextValue);
                        return control;
                    }
            }
        }

        private DatePicker BuildDatePicker(LayoutField field, string rawValue, string format)
        {
            var picker = new DatePicker
            {
                IsEnabled = !field.ReadOnly,
                Format = format
            };
            if (DateTime.TryParse(
                    rawValue,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeLocal,
                    out var parsed))
            {
                picker.Date = parsed;
            }
            picker.DateSelected += (_, e) =>
            {
                if (e.NewDate is { } date)
                    DataObject?.SetField(
                        field.FieldName,
                        date.ToString(format, CultureInfo.InvariantCulture));
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

        private static ColumnDefinitionCollection BuildColumnDefinitions(int columnCount)
        {
            var defs = new ColumnDefinitionCollection();
            for (int i = 0; i < columnCount; i++)
                defs.Add(new ColumnDefinition { Width = GridLength.Star });
            return defs;
        }
    }
}
