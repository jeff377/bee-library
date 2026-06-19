using Avalonia.Controls;
using Bee.Base.Data;
using Bee.Definition.Layouts;
using Bee.UI.Avalonia.Controls.Editors;
using Avalonia.DemoCenter.Modules.DataEditors;

namespace Avalonia.DemoCenter.Modules.DataBinding
{
    /// <summary>
    /// Explicit binding: the host calls <c>editor.Bind(dataObject, layoutField)</c> directly,
    /// without relying on an ambient <see cref="FormScope"/>. Useful when a control sits
    /// outside a scoped container or the host wants full control of the binding.
    /// </summary>
    public sealed class ExplicitBindingModule : DemoModuleBase
    {
        /// <inheritdoc/>
        public override string Category => "資料繫結";

        /// <inheritdoc/>
        public override string Title => "明確繫結";

        /// <inheritdoc/>
        public override string Description =>
            "editor.Bind(dataObject, layoutField) 直接綁定，不靠容器 ambient scope；適合控件在 scope 外或 host 自管綁定。";

        /// <inheritdoc/>
        public override Control BuildView()
        {
            var data = DataEditorParts.SingleField("name", "Name", FieldDbType.String, initialValue: "Alice Chen");

            var editor = new TextEdit();
            editor.Bind(data, new LayoutField { FieldName = "name" });

            // No FormScope.SetDataObject on the root: the binding is explicit. FormMode still
            // flows from the shell's ambient scope, so the editor follows View/Add/Edit.
            var stack = new StackPanel { Spacing = 16, Margin = new Thickness(4) };
            stack.Children.Add(DataEditorParts.Section(
                "editor.Bind(dataObject, layoutField)",
                "根節點未設 ambient DataObject；編輯器由 Bind(...) 明確綁定。",
                DataEditorParts.LabeledRow("name", editor)));
            stack.Children.Add(DataEditorParts.Section(
                "即時值",
                null,
                DataEditorParts.LiveValue(data, "name")));
            return new ScrollViewer { Content = stack };
        }
    }
}
