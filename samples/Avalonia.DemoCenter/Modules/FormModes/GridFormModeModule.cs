using Avalonia.Controls;
using Avalonia.Media;
using Bee.Definition.Layouts;
using Bee.UI.Avalonia.Controls;
using Bee.UI.Avalonia.Controls.Editors;
using Avalonia.DemoCenter.Modules.Views;

namespace Avalonia.DemoCenter.Modules.FormModes
{
    /// <summary>
    /// Grid × FormMode: the same detail <see cref="GridControl"/> shown under View / Add /
    /// Edit (each pinned via <see cref="FormScope.SetFormMode"/>). In View the grid is
    /// read-only and its add/delete toolbar is hidden; in Add / Edit it is editable with the
    /// toolbar shown.
    /// </summary>
    public sealed class GridFormModeModule : DemoModuleBase
    {
        /// <inheritdoc/>
        public override string Category => "FormMode 顯示狀態";

        /// <inheritdoc/>
        public override string Title => "Grid × FormMode";

        /// <inheritdoc/>
        public override string Description =>
            "同一明細 GridControl 在三態下的差異：View 唯讀且工具列隱藏；Add / Edit 可編輯且顯示新增/刪除工具列"
            + "（AllowEdit + AllowEditModes 合成）。各區段以 FormScope 釘住模式。";

        /// <inheritdoc/>
        public override Control BuildView()
        {
            var stack = new StackPanel { Spacing = 16, Margin = new Thickness(4) };
            stack.Children.Add(Section(SingleFormMode.View, "View（唯讀、工具列隱藏）"));
            stack.Children.Add(Section(SingleFormMode.Add, "Add（可編輯、顯示工具列）"));
            stack.Children.Add(Section(SingleFormMode.Edit, "Edit（可編輯、顯示工具列）"));
            return new ScrollViewer { Content = stack };
        }

        private static Border Section(SingleFormMode mode, string title)
        {
            var data = SampleFormData.BuildMasterDetail(SampleFormData.BuildSchema());

            var layout = new LayoutGrid("Phones", "電話");
            layout.Columns!.Add(new LayoutColumn("phone", "號碼", ControlType.TextEdit));
            layout.Columns.Add(new LayoutColumn("type", "類型", ControlType.DropDownEdit));
            layout.Columns.Add(new LayoutColumn("is_primary", "主要", ControlType.CheckEdit));
            layout.Columns.Add(new LayoutColumn("valid_from", "生效日", ControlType.DateEdit));

            var grid = new GridControl { MinHeight = 130 };
            grid.Bind(data, layout);

            var stack = new StackPanel { Spacing = 8 };
            stack.Children.Add(new TextBlock { Text = title, FontSize = 15, FontWeight = FontWeight.Bold });
            stack.Children.Add(grid);

            var card = new Border
            {
                Padding = new Thickness(12),
                BorderThickness = new Thickness(1),
                BorderBrush = Brushes.Gray,
                CornerRadius = new CornerRadius(4),
                Child = stack,
            };
            // Pin this section to its mode; the grid inherits it and ignores the toolbar.
            FormScope.SetFormMode(card, mode);
            return card;
        }
    }
}
