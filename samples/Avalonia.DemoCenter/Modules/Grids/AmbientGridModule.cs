using Avalonia.Controls;
using Bee.UI.Avalonia.Controls;
using Bee.UI.Avalonia.Controls.Editors;
using Avalonia.DemoCenter.Modules.DataEditors;
using Avalonia.DemoCenter.Modules.Views;

namespace Avalonia.DemoCenter.Modules.Grids
{
    /// <summary>
    /// Ambient grid binding: a <see cref="GridControl"/> with only <c>TableName</c> set binds
    /// itself through the ambient <see cref="FormScope"/> on attach and generates plain
    /// columns from the table.
    /// </summary>
    public sealed class AmbientGridModule : DemoModuleBase
    {
        /// <inheritdoc/>
        public override string Category => "Grid";

        /// <inheritdoc/>
        public override string Title => "Ambient 綁定";

        /// <inheritdoc/>
        public override string Description =>
            "只設 TableName，grid 經 FormScope 自動綁定明細表、欄位由表自動產生——免給 Layout。";

        /// <inheritdoc/>
        public override Control BuildView()
        {
            var data = SampleFormData.BuildMasterDetail(SampleFormData.BuildSchema());
            var grid = new GridControl { TableName = "Phones", MinHeight = 240 };

            var root = new ScrollViewer
            {
                Content = DataEditorParts.Section(
                    "Ambient 綁定（只設 TableName）",
                    "未給 Layout；欄位由 Phones 表自動產生。",
                    grid),
            };
            FormScope.SetDataObject(root, data);
            return root;
        }
    }
}
