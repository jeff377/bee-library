using Avalonia.Controls;
using Bee.UI.Avalonia.Controls;
using Avalonia.DemoCenter.Modules.DataEditors;
using Avalonia.DemoCenter.Modules.Views;

namespace Avalonia.DemoCenter.Modules.Grids
{
    /// <summary>
    /// EditForm mode: a <see cref="GridControl"/> in <see cref="GridEditMode.EditForm"/> —
    /// the grid stays read-only and editing a row opens a popup form (editing strategy in
    /// <c>ADR-021</c>).
    /// </summary>
    public sealed class EditFormModule : DemoModuleBase
    {
        /// <inheritdoc/>
        public override string Category => "Grid";

        /// <inheritdoc/>
        public override string Title => "EditForm 彈窗";

        /// <inheritdoc/>
        public override string Description =>
            "GridControl EditForm 模式：grid 唯讀，雙擊列或工具列 Edit 圖示開彈窗編輯整列；Add 開彈窗編輯新列、取消移除空列。";

        /// <inheritdoc/>
        public override Control BuildView()
        {
            var data = SampleFormData.BuildMasterDetail(SampleFormData.BuildSchema());
            var grid = new GridControl { MinHeight = 240, EditMode = GridEditMode.EditForm };
            grid.Bind(data, SampleFormData.BuildPhonesLayout());

            return new ScrollViewer
            {
                Content = DataEditorParts.Section(
                    "EditForm 彈窗編輯",
                    "雙擊列或 Edit 圖示開彈窗；Cancel 完整還原、OK 落實並捲回該列。",
                    grid),
            };
        }
    }
}
