using Avalonia.Controls;
using Bee.UI.Avalonia.Controls;
using Avalonia.DemoCenter.Modules.DataEditors;
using Avalonia.DemoCenter.Modules.Views;

namespace Avalonia.DemoCenter.Modules.Grids
{
    /// <summary>
    /// List mode: a <see cref="GridControl"/> bound to a standalone <c>DataTable</c> (outside
    /// any data object), so the grid is read-only and the edit toolbar stays hidden.
    /// </summary>
    public sealed class ListModeModule : DemoModuleBase
    {
        /// <inheritdoc/>
        public override string Category => "Grid";

        /// <inheritdoc/>
        public override string Title => "List mode（唯讀清單）";

        /// <inheritdoc/>
        public override string Description =>
            "GridControl 以 list mode 綁定獨立 DataTable（不屬任何 FormDataObject）：唯讀、無新增/刪除工具列；欄位由 LayoutGrid 定義。";

        /// <inheritdoc/>
        public override Control BuildView()
        {
            var grid = new GridControl { MinHeight = 240 };
            grid.Bind(SampleFormData.BuildEmployeeListLayout(), SampleFormData.BuildEmployeeListTable());

            return new ScrollViewer
            {
                Content = DataEditorParts.Section(
                    "員工清單（list mode）",
                    "list-mode 綁定的 grid 不可編輯、無工具列；生產的 ListView 另加後端 reload 與列事件（見 Avalonia.Demo）。",
                    grid),
            };
        }
    }
}
