using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.DemoCenter.Modules.Views;
using Bee.Definition.Layouts;
using Bee.UI.Avalonia.Controls;
using Bee.UI.Avalonia.Controls.Editors;

namespace Avalonia.DemoCenter.Modules.Grids
{
    /// <summary>
    /// <see cref="GridControl"/> scenarios on the Phones detail table: layout binding with
    /// in-cell editing, ambient binding via <c>TableName</c>, and the EditForm popup mode
    /// (editing strategy in <c>ADR-021</c>). Each section uses its own data object so edits
    /// stay isolated.
    /// </summary>
    public sealed class GridControlModule : DemoModuleBase
    {
        /// <inheritdoc/>
        public override string Category => "Grid";

        /// <inheritdoc/>
        public override string ControlName => "GridControl";

        /// <inheritdoc/>
        public override string ScenarioTitle => "綁定 · in-cell · EditForm";

        /// <inheritdoc/>
        public override string Description =>
            "明細資料的編輯網格。示範 Layout 綁定 + in-cell 編輯、只設 TableName 的 ambient 綁定、"
            + "以及 EditForm 彈窗編輯模式（編輯策略見 ADR-021）。";

        /// <inheritdoc/>
        public override Control BuildView()
        {
            var stack = new StackPanel { Orientation = Orientation.Vertical, Spacing = 16, Margin = new Thickness(4) };
            stack.Children.Add(InCellSection());
            stack.Children.Add(AmbientSection());
            stack.Children.Add(EditFormSection());
            return new ScrollViewer { Content = stack };
        }

        private static LayoutGrid BuildPhonesLayout()
        {
            var layout = new LayoutGrid("Phones", "電話");
            layout.Columns!.Add(new LayoutColumn("phone", "號碼", ControlType.TextEdit));
            layout.Columns.Add(new LayoutColumn("type", "類型", ControlType.DropDownEdit));
            layout.Columns.Add(new LayoutColumn("is_primary", "主要", ControlType.CheckEdit));
            layout.Columns.Add(new LayoutColumn("valid_from", "生效日", ControlType.DateEdit));
            return layout;
        }

        private static Border InCellSection()
        {
            var data = SampleFormData.BuildMasterDetail(SampleFormData.BuildSchema());
            var grid = new GridControl { MinHeight = 140, EditMode = GridEditMode.InCell };
            grid.Bind(data, BuildPhonesLayout());
            return Card(
                "Layout 綁定 + in-cell 編輯",
                "雙擊 cell（或 F2）進入編輯；下拉 / 日期 / 勾選為單擊置換編輯器。grid 內建新增 / 刪除工具列。",
                grid);
        }

        private static Border AmbientSection()
        {
            var data = SampleFormData.BuildMasterDetail(SampleFormData.BuildSchema());
            // TableName only: the grid binds through the ambient FormScope on attach and
            // generates plain columns from the table when no layout is supplied.
            var grid = new GridControl { TableName = "Phones", MinHeight = 140 };
            var card = Card(
                "Ambient 綁定（只設 TableName）",
                "未給 Layout，grid 由 FormScope 自動綁定明細表、欄位由表自動產生。",
                grid);
            FormScope.SetDataObject(card, data);
            return card;
        }

        private static Border EditFormSection()
        {
            var data = SampleFormData.BuildMasterDetail(SampleFormData.BuildSchema());
            var grid = new GridControl { MinHeight = 140, EditMode = GridEditMode.EditForm };
            grid.Bind(data, BuildPhonesLayout());
            return Card(
                "EditForm 模式（彈窗編輯整列）",
                "grid 唯讀；雙擊列或工具列 Edit 圖示開彈窗編輯整列，Add 開彈窗編輯新列、取消移除空列。",
                grid);
        }

        private static Border Card(string title, string note, Control child)
        {
            var stack = new StackPanel { Orientation = Orientation.Vertical, Spacing = 8 };
            stack.Children.Add(new TextBlock { Text = title, FontSize = 15, FontWeight = FontWeight.Bold });
            stack.Children.Add(new TextBlock { Text = note, FontSize = 12, Opacity = 0.7, TextWrapping = TextWrapping.Wrap });
            stack.Children.Add(child);
            return new Border
            {
                Padding = new Thickness(12),
                BorderThickness = new Thickness(1),
                BorderBrush = Brushes.Gray,
                CornerRadius = new CornerRadius(4),
                Child = stack,
            };
        }
    }
}
