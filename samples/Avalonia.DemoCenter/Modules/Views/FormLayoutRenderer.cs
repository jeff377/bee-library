using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Bee.Definition.Layouts;
using Bee.UI.Avalonia.Controls;
using Bee.UI.Avalonia.Controls.Editors;
using Bee.UI.Avalonia.DataObjects;

namespace Avalonia.DemoCenter.Modules.Views
{
    /// <summary>
    /// Renders a <see cref="FormLayout"/> against a local <see cref="FormDataObject"/>
    /// using the same public primitives the production <c>FormView</c> uses internally
    /// (<see cref="FieldEditorFactory"/> for master fields, <see cref="GridControl"/> for
    /// details). This shows the FormView layout / binding / FormMode behaviour at the
    /// control layer, without the backend load / save that <c>FormView</c> wraps.
    /// </summary>
    internal static class FormLayoutRenderer
    {
        /// <summary>
        /// Builds a scrollable master-sections + detail-grids view bound to
        /// <paramref name="dataObject"/>. FormMode comes from the ambient
        /// <see cref="FormScope"/> set by the shell.
        /// </summary>
        public static Control Render(FormDataObject dataObject, FormLayout layout, GridEditMode detailEditMode)
        {
            var host = new StackPanel { Orientation = Orientation.Vertical, Spacing = 12, Margin = new Thickness(4) };

            foreach (var section in layout.Sections ?? [])
                host.Children.Add(BuildSection(dataObject, section));

            foreach (var detail in layout.Details ?? [])
                host.Children.Add(BuildDetailSection(dataObject, detail, detailEditMode));

            var root = new ScrollViewer { Content = host };
            FormScope.SetDataObject(root, dataObject);
            return root;
        }

        private static Border BuildSection(FormDataObject dataObject, LayoutSection section)
        {
            var stack = new StackPanel { Orientation = Orientation.Vertical, Spacing = 6 };
            if (section.ShowCaption && !string.IsNullOrEmpty(section.Caption))
                stack.Children.Add(new TextBlock { Text = section.Caption, FontWeight = FontWeight.Bold });

            foreach (var field in (section.Fields ?? []).Where(f => f.Visible))
                stack.Children.Add(BuildFieldCell(dataObject, field));

            return Card(stack);
        }

        private static StackPanel BuildFieldCell(FormDataObject dataObject, LayoutField field)
        {
            var stack = new StackPanel { Orientation = Orientation.Vertical, Spacing = 2 };
            // The library's read-only/required caption colouring (FieldCaptionStyle) is
            // internal; this demo renders plain captions. The read-only/required header
            // cue is showcased on the GridControl overview instead.
            stack.Children.Add(new TextBlock { Text = field.Caption });

            var editor = FieldEditorFactory.Create(field.ControlType);
            editor.MinWidth = 240;
            editor.HorizontalAlignment = HorizontalAlignment.Left;
            ((IFieldEditor)editor).Bind(dataObject, field);
            stack.Children.Add(editor);
            return stack;
        }

        private static Border BuildDetailSection(FormDataObject dataObject, LayoutGrid layout, GridEditMode detailEditMode)
        {
            var stack = new StackPanel { Orientation = Orientation.Vertical, Spacing = 6 };
            if (!string.IsNullOrEmpty(layout.Caption))
                stack.Children.Add(new TextBlock { Text = layout.Caption, FontWeight = FontWeight.Bold });

            var grid = new GridControl { MinHeight = 120, EditMode = detailEditMode };
            grid.Bind(dataObject, layout);
            stack.Children.Add(grid);
            return Card(stack);
        }

        private static Border Card(Control child) => new()
        {
            Padding = new Thickness(12),
            BorderThickness = new Thickness(1),
            BorderBrush = Brushes.Gray,
            CornerRadius = new CornerRadius(4),
            Child = child,
        };
    }
}
