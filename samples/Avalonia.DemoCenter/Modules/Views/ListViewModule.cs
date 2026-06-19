using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Bee.UI.Avalonia.Controls;

namespace Avalonia.DemoCenter.Modules.Views
{
    /// <summary>
    /// ListView-style scenario: a read-only employee list rendered by a
    /// <see cref="GridControl"/> in list mode (rows supplied as a standalone
    /// <c>DataTable</c>, outside any data object, so the grid never edits them).
    /// </summary>
    /// <remarks>
    /// The production <c>ListView</c> wraps this with backend reload and row View / Edit /
    /// Add events; that end-to-end flow is shown in <c>Avalonia.Demo</c>. Here the focus is
    /// the list-mode rendering, driven by a fake-data table.
    /// </remarks>
    public sealed class ListViewModule : DemoModuleBase
    {
        /// <inheritdoc/>
        public override string Category => "Views";

        /// <inheritdoc/>
        public override string ControlName => "ListView";

        /// <inheritdoc/>
        public override string ScenarioTitle => "清單呈現（list mode）";

        /// <inheritdoc/>
        public override string Description =>
            "GridControl 以 list mode 綁定獨立 DataTable（不屬任何 FormDataObject，故唯讀、工具列隱藏）。"
            + "生產的 ListView 另加後端 reload 與列 View/Edit/Add 事件，見 Avalonia.Demo。";

        /// <inheritdoc/>
        public override Control BuildView()
        {
            var grid = new GridControl { MinHeight = 200 };
            grid.Bind(SampleFormData.BuildEmployeeListLayout(), SampleFormData.BuildEmployeeListTable());

            var stack = new StackPanel { Orientation = Orientation.Vertical, Spacing = 8, Margin = new Thickness(4) };
            stack.Children.Add(new TextBlock
            {
                Text = "員工清單（唯讀 list mode）",
                FontWeight = FontWeight.Bold,
            });
            stack.Children.Add(new TextBlock
            {
                Text = "list-mode 綁定的 grid 不可編輯、無新增/刪除工具列；欄位由 LayoutGrid 定義。",
                FontSize = 12,
                Opacity = 0.7,
                TextWrapping = TextWrapping.Wrap,
            });
            stack.Children.Add(grid);
            return stack;
        }
    }
}
