using Avalonia.Controls;
using Bee.UI.Avalonia.Controls;
using Avalonia.DemoCenter.Modules.Views;

namespace Avalonia.DemoCenter.Modules.Layouts
{
    /// <summary>
    /// Auto-generated FormLayout: <c>FormSchema.GetFormLayout()</c> derives the form's
    /// sections and field placement from the schema; the layout is then rendered with the
    /// same primitives the production <c>FormView</c> uses.
    /// </summary>
    public sealed class AutoFormLayoutModule : DemoModuleBase
    {
        /// <inheritdoc/>
        public override string Category => "Layout 排版";

        /// <inheritdoc/>
        public override string Title => "FormLayout 自動產生";

        /// <inheritdoc/>
        public override string Description =>
            "FormSchema.GetFormLayout() 由 schema 自動產生表單 layout（區段 + 欄位擺放），免手繪版面。";

        /// <inheritdoc/>
        public override Control BuildView()
        {
            var schema = SampleFormData.BuildMasterFormSchema();
            var data = SampleFormData.BuildMasterForm(schema);
            var layout = schema.GetFormLayout();
            return FormLayoutRenderer.Render(data, layout, GridEditMode.InCell);
        }
    }
}
