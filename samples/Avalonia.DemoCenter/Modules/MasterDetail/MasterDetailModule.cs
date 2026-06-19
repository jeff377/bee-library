using Avalonia.Controls;
using Bee.UI.Avalonia.Controls;
using Avalonia.DemoCenter.Modules.Views;

namespace Avalonia.DemoCenter.Modules.MasterDetail
{
    /// <summary>
    /// Master-detail: the generated <c>FormLayout</c> for the Employee + Phones schema,
    /// rendering the master section above the Phones detail grid, all bound to one local
    /// data object (the same composition the production <c>FormView</c> produces).
    /// </summary>
    public sealed class MasterDetailModule : DemoModuleBase
    {
        /// <inheritdoc/>
        public override string Category => "Master-Detail";

        /// <inheritdoc/>
        public override string Title => "主檔 + 明細";

        /// <inheritdoc/>
        public override string Description =>
            "Employee 主檔區段 + Phones 明細 grid，綁定同一個 FormDataObject（預設 Edit 可編輯）。"
            + "生產的後端載入/存檔流程見 Avalonia.Demo。";

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
