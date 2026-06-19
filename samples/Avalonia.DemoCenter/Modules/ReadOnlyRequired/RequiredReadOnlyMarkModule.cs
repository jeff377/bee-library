using Avalonia.Controls;
using Avalonia.Media;
using Bee.Definition.Layouts;
using Bee.UI.Avalonia.Controls;
using Avalonia.DemoCenter.Modules.Views;

namespace Avalonia.DemoCenter.Modules.ReadOnlyRequired
{
    /// <summary>
    /// Required / read-only marking: a <see cref="GridControl"/> whose column headers are
    /// coloured by field state — read-only brown, required blue (read-only wins) — via the
    /// library's shared caption-colour convention.
    /// </summary>
    public sealed class RequiredReadOnlyMarkModule : DemoModuleBase
    {
        /// <inheritdoc/>
        public override string Category => "唯讀與必填";

        /// <inheritdoc/>
        public override string Title => "必填 / 唯讀標示";

        /// <inheritdoc/>
        public override string Description =>
            "GridControl 欄位以表頭文字色標示狀態：唯讀=棕、必填=藍（唯讀優先）。掃一眼表頭即知欄位狀態。";

        /// <inheritdoc/>
        public override Control BuildView()
        {
            var data = SampleFormData.BuildMasterDetail(SampleFormData.BuildSchema());

            var layout = new LayoutGrid("Phones", "電話");
            layout.Columns!.Add(new LayoutColumn("phone", "號碼（唯讀）", ControlType.TextEdit) { ReadOnly = true });
            layout.Columns.Add(new LayoutColumn("type", "類型（必填）", ControlType.DropDownEdit) { Required = true });
            layout.Columns.Add(new LayoutColumn("is_primary", "主要", ControlType.CheckEdit));
            layout.Columns.Add(new LayoutColumn("valid_from", "生效日", ControlType.DateEdit));

            var grid = new GridControl { MinHeight = 180 };
            grid.Bind(data, layout);

            var stack = new StackPanel { Spacing = 8, Margin = new Thickness(4) };
            stack.Children.Add(new TextBlock { Text = "欄位狀態以表頭色標示", FontSize = 15, FontWeight = FontWeight.Bold });
            stack.Children.Add(new TextBlock
            {
                Text = "「號碼」唯讀 → 棕色表頭；「類型」必填 → 藍色表頭；其餘預設色。",
                FontSize = 12,
                Opacity = 0.7,
                TextWrapping = TextWrapping.Wrap,
            });
            stack.Children.Add(grid);
            return new ScrollViewer { Content = stack };
        }
    }
}
