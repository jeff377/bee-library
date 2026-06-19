using Avalonia.Controls;
using Bee.UI.Avalonia.Controls;
using Avalonia.DemoCenter.Modules.DataEditors;
using Avalonia.DemoCenter.Modules.Views;

namespace Avalonia.DemoCenter.Modules.Grids
{
    /// <summary>
    /// In-cell editing: a <see cref="GridControl"/> bound to the Phones detail in
    /// <see cref="GridEditMode.InCell"/> mode — double-click a cell to edit it in place.
    /// </summary>
    public sealed class InCellEditModule : DemoModuleBase
    {
        /// <inheritdoc/>
        public override string Category => "Grid";

        /// <inheritdoc/>
        public override string Title => "In-cell 編輯";

        /// <inheritdoc/>
        public override string Description =>
            "GridControl InCell 模式：雙擊 cell（或 F2）就地編輯；下拉 / 日期 / 勾選為單擊置換編輯器。內建新增 / 刪除工具列。";

        /// <inheritdoc/>
        public override Control BuildView()
        {
            var data = SampleFormData.BuildMasterDetail(SampleFormData.BuildSchema());
            var grid = new GridControl { MinHeight = 240, EditMode = GridEditMode.InCell };
            grid.Bind(data, SampleFormData.BuildPhonesLayout());

            return new ScrollViewer
            {
                Content = DataEditorParts.Section(
                    "In-cell 編輯",
                    "雙擊 cell（或 F2）進入編輯，Enter / 點別處 commit、Esc 取消。",
                    grid),
            };
        }
    }
}
