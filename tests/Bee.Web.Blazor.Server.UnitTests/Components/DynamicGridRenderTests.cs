using System.ComponentModel;
using System.Data;
using Bee.Definition.Layouts;
using Bee.Web.Blazor.Server.Components;
using Bunit;

namespace Bee.Web.Blazor.Server.UnitTests.Components
{
    /// <summary>
    /// 渲染測試：觸發 DynamicGrid.razor 的 BuildRenderTree，補強 .razor 模板行覆蓋率。
    /// </summary>
    public class DynamicGridRenderTests : BunitContext
    {
        [Fact]
        [DisplayName("DynamicGrid Layout 為 null 時應渲染空狀態 div 並包含預設文字")]
        public void DynamicGrid_NullLayout_RendersEmptyDiv()
        {
            var cut = Render<DynamicGrid>();
            var emptyDiv = cut.Find("div.bee-dynamic-grid--empty");
            Assert.Contains("No data.", emptyDiv.TextContent);
        }

        [Fact]
        [DisplayName("DynamicGrid 傳入有資料的 Layout 與 Rows 後應渲染表格")]
        public void DynamicGrid_WithLayoutAndRows_RendersTable()
        {
            var layout = new LayoutGrid();
            layout.Columns!.Add(new LayoutColumn { FieldName = "name", Caption = "Name", Visible = true });

            var table = new DataTable();
            table.Columns.Add("name", typeof(string));
            var row = table.NewRow();
            row["name"] = "Alice";
            table.Rows.Add(row);

            var cut = Render<DynamicGrid>(p => p
                .Add(c => c.Layout, layout)
                .Add(c => c.Rows, table));

            Assert.NotNull(cut.Find("table.bee-dynamic-grid"));
        }
    }
}
