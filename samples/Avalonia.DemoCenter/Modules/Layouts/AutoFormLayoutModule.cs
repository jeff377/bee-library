using Avalonia.Controls;
using Bee.Definition.Layouts;
using Bee.UI.Avalonia.Controls;
using Avalonia.DemoCenter.Modules.Views;

namespace Avalonia.DemoCenter.Modules.Layouts
{
    /// <summary>
    /// Design-time layout generation: <c>FormLayoutGenerator.Generate</c> derives the form's sections
    /// and field placement from the schema, which is how a definition editor produces the starting
    /// point for a <c>FormLayout</c> definition file. The result is then rendered with the same
    /// primitives the production <c>FormView</c> uses — which, at runtime, reads the stored
    /// definition rather than generating one.
    /// </summary>
    public sealed class AutoFormLayoutModule : DemoModuleBase
    {
        /// <inheritdoc/>
        public override string Category => "Layout 排版";

        /// <inheritdoc/>
        public override string Title => "FormLayout 設計階段產生";

        /// <inheritdoc/>
        public override string Description =>
            "FormLayoutGenerator.Generate 由 schema 產生表單 layout（區段 + 欄位擺放），作為設計階段的起點；"
            + "執行階段一律讀已存檔的 FormLayout 定義，不會即時推導。";

        /// <inheritdoc/>
        public override Control BuildView()
        {
            var schema = SampleFormData.BuildMasterFormSchema();
            var data = SampleFormData.BuildMasterForm(schema);
            var layout = FormLayoutGenerator.Generate(schema, "default");
            return FormLayoutRenderer.Render(data, layout, GridEditMode.InCell);
        }
    }
}
