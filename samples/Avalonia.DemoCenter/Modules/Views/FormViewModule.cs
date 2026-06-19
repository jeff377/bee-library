using Avalonia.Controls;
using Bee.Definition.Forms;
using Bee.UI.Avalonia.Controls;

namespace Avalonia.DemoCenter.Modules.Views
{
    /// <summary>
    /// FormView-style master-detail scenario: renders the generated <c>FormLayout</c> for
    /// the Employee + Phones schema against a locally-seeded data object. The toolbar's
    /// FormMode (View / Add / Edit) drives the whole form.
    /// </summary>
    /// <remarks>
    /// The production <c>FormView</c> wraps this same layout / binding with a Save / Cancel
    /// toolbar and backend load / save; that end-to-end flow is shown in <c>Avalonia.Demo</c>.
    /// Here the focus is the control-layer behaviour, driven by a fake-data view-model.
    /// </remarks>
    public sealed class FormViewModule : DemoModuleBase
    {
        /// <inheritdoc/>
        public override string Category => "Views";

        /// <inheritdoc/>
        public override string ControlName => "FormView";

        /// <inheritdoc/>
        public override string ScenarioTitle => "Master-Detail · FormMode 三態";

        /// <inheritdoc/>
        public override string Description =>
            "由 FormSchema 產生的 FormLayout（master 區段 + Phones 明細 grid），綁定本機假資料 FormDataObject。"
            + "切工具列 FormMode（View / Add / Edit）驅動整張表單；生產的後端載入/存檔流程見 Avalonia.Demo。";

        /// <inheritdoc/>
        public override Control BuildView()
        {
            var schema = SampleFormData.BuildSchema();
            var data = SampleFormData.BuildMasterDetail(schema);
            var layout = schema.GetFormLayout();
            return FormLayoutRenderer.Render(data, layout, GridEditMode.InCell);
        }
    }
}
