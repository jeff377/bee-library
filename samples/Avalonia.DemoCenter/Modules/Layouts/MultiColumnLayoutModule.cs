using Avalonia.Controls;
using Bee.UI.Avalonia.Controls;
using Avalonia.DemoCenter.Modules.Views;

namespace Avalonia.DemoCenter.Modules.Layouts
{
    /// <summary>
    /// Multi-column layout: the generated <c>FormLayout</c> is set to two columns and one
    /// field is given a column span of two, showing CSS-grid-like field placement.
    /// </summary>
    public sealed class MultiColumnLayoutModule : DemoModuleBase
    {
        /// <inheritdoc/>
        public override string Category => "Layout 排版";

        /// <inheritdoc/>
        public override string Title => "多欄排版（ColumnCount / ColumnSpan）";

        /// <inheritdoc/>
        public override string Description =>
            "設 FormLayout.ColumnCount=2 兩欄排列，並讓 notes 欄 ColumnSpan=2 跨整列；欄位依跨欄自動換行擺放。";

        /// <inheritdoc/>
        public override Control BuildView()
        {
            var schema = SampleFormData.BuildMasterFormSchema();
            var data = SampleFormData.BuildMasterForm(schema);

            var layout = schema.GetFormLayout();
            layout.ColumnCount = 2;
            // Let the memo field span the full width of the two-column grid.
            foreach (var section in layout.Sections ?? [])
            {
                foreach (var field in section.Fields ?? [])
                {
                    if (field.FieldName == "notes")
                        field.ColumnSpan = 2;
                }
            }

            return FormLayoutRenderer.Render(data, layout, GridEditMode.InCell);
        }
    }
}
