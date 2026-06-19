using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Bee.Base.Data;
using Bee.Definition.Forms;
using Bee.UI.Avalonia.Controls.Editors;
using Bee.UI.Avalonia.DataObjects;

namespace Avalonia.DemoCenter.Modules.DataEditors
{
    /// <summary>
    /// Shared building blocks for the per-editor Data Editors scenarios: single-field
    /// data objects, titled section cards, a live value readout, and a scrollable root
    /// that wires the ambient <see cref="FormScope"/> data object. Keeps each editor
    /// module small and consistent.
    /// </summary>
    internal static class DataEditorParts
    {
        /// <summary>
        /// Builds a data object with one master table carrying a single field.
        /// </summary>
        public static FormDataObject SingleField(
            string fieldName,
            string caption,
            FieldDbType dbType,
            Action<FormField>? configure = null,
            string? initialValue = null)
        {
            var schema = new FormSchema("Demo", "Demo");
            var master = schema.Tables!.Add("Demo", "Demo");
            var field = master.Fields!.Add(fieldName, caption, dbType);
            configure?.Invoke(field);

            var dataObject = new FormDataObject(schema);
            dataObject.InitializeNewMaster();
            if (initialValue is not null)
                dataObject.SetField(fieldName, initialValue);
            return dataObject;
        }

        /// <summary>
        /// Wraps content in a titled card with an optional explanatory note.
        /// </summary>
        public static Border Section(string title, string? note, params Control[] children)
        {
            var stack = new StackPanel { Spacing = 8 };
            stack.Children.Add(new TextBlock { Text = title, FontSize = 15, FontWeight = FontWeight.Bold });
            if (note is not null)
                stack.Children.Add(new TextBlock { Text = note, FontSize = 12, Opacity = 0.7, TextWrapping = TextWrapping.Wrap });
            foreach (var child in children)
                stack.Children.Add(child);

            return new Border
            {
                Padding = new Thickness(12),
                BorderThickness = new Thickness(1),
                BorderBrush = Brushes.Gray,
                CornerRadius = new CornerRadius(4),
                Child = stack,
            };
        }

        /// <summary>
        /// A monospace text block that reflects the given fields and refreshes whenever
        /// any field value changes.
        /// </summary>
        public static TextBlock LiveValue(FormDataObject dataObject, params string[] fieldNames)
        {
            var text = new TextBlock
            {
                FontFamily = new FontFamily("Menlo,Consolas,monospace"),
                FontSize = 12,
                Opacity = 0.85,
            };
            void Refresh() => text.Text = string.Join(
                Environment.NewLine,
                fieldNames.Select(f => $"{f,-10} = {dataObject.GetField(f)}"));
            Refresh();
            dataObject.FieldValueChanged += (_, _) => Refresh();
            return text;
        }

        /// <summary>
        /// A caption label paired with its editor in a horizontal row.
        /// </summary>
        public static Control LabeledRow(string label, Control editor)
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };
            row.Children.Add(new TextBlock
            {
                Text = label,
                Width = 110,
                Opacity = 0.8,
                VerticalAlignment = VerticalAlignment.Center,
            });
            editor.MinWidth = 220;
            row.Children.Add(editor);
            return row;
        }

        /// <summary>
        /// Composes the module root: a scrollable vertical stack of sections with the
        /// ambient <see cref="FormScope"/> data object set so descendant editors bind on
        /// attach. The shell owns FormMode; this root owns the data object.
        /// </summary>
        public static Control Compose(FormDataObject dataObject, params Control[] sections)
        {
            var stack = new StackPanel { Spacing = 16, Margin = new Thickness(4) };
            foreach (var section in sections)
                stack.Children.Add(section);

            var root = new ScrollViewer { Content = stack };
            FormScope.SetDataObject(root, dataObject);
            return root;
        }
    }
}
