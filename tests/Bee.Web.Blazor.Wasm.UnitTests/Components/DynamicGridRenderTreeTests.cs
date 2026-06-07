using System.ComponentModel;
using System.Data;
using System.Reflection;
using Bee.Definition.Layouts;
using Bee.Web.Blazor.Wasm.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bee.Web.Blazor.Wasm.UnitTests.Components
{
    public class DynamicGridRenderTreeTests
    {
        private static readonly MethodInfo s_buildRenderTree =
            typeof(DynamicGrid).GetMethod("BuildRenderTree",
                BindingFlags.Instance | BindingFlags.NonPublic)!;

        [Fact]
        [DisplayName("BuildRenderTree Layout 為 null 時應渲染空白區塊，不拋例外")]
        public void BuildRenderTree_NullLayout_RendersEmptyState()
        {
            var component = new DynamicGrid();
            var builder = new RenderTreeBuilder();
            var exception = Record.Exception(() => s_buildRenderTree.Invoke(component, new object[] { builder }));
            Assert.Null(exception);
        }

        [Fact]
        [DisplayName("BuildRenderTree Rows 為 null 時應渲染空白區塊，不拋例外")]
        public void BuildRenderTree_NullRows_RendersEmptyState()
        {
            var component = new DynamicGrid();
            typeof(DynamicGrid)
                .GetProperty("Layout", BindingFlags.Public | BindingFlags.Instance)!
                .SetValue(component, new LayoutGrid());
            var builder = new RenderTreeBuilder();
            var exception = Record.Exception(() => s_buildRenderTree.Invoke(component, new object[] { builder }));
            Assert.Null(exception);
        }

        [Fact]
        [DisplayName("BuildRenderTree Rows 為空 DataTable 時應渲染空白區塊，不拋例外")]
        public void BuildRenderTree_EmptyRows_RendersEmptyState()
        {
            var component = new DynamicGrid();
            typeof(DynamicGrid)
                .GetProperty("Layout", BindingFlags.Public | BindingFlags.Instance)!
                .SetValue(component, new LayoutGrid());
            typeof(DynamicGrid)
                .GetProperty("Rows", BindingFlags.Public | BindingFlags.Instance)!
                .SetValue(component, new DataTable());
            var builder = new RenderTreeBuilder();
            var exception = Record.Exception(() => s_buildRenderTree.Invoke(component, new object[] { builder }));
            Assert.Null(exception);
        }

        [Fact]
        [DisplayName("BuildRenderTree 設定 Layout 與含資料列的 Rows 時應渲染表格，不拋例外")]
        public void BuildRenderTree_WithLayoutAndRows_RendersDataTable()
        {
            var layout = new LayoutGrid();
            layout.Columns!.Add(new LayoutColumn { FieldName = "name", Caption = "Name", Visible = true, Width = 120 });
            layout.Columns.Add(new LayoutColumn { FieldName = "amount", Caption = "Amount", Visible = true });

            var table = new DataTable();
            table.Columns.Add("name", typeof(string));
            table.Columns.Add("amount", typeof(double));
            var row = table.NewRow();
            row["name"] = "Alice";
            row["amount"] = 1234.56;
            table.Rows.Add(row);

            var component = new DynamicGrid();
            typeof(DynamicGrid)
                .GetProperty("Layout", BindingFlags.Public | BindingFlags.Instance)!
                .SetValue(component, layout);
            typeof(DynamicGrid)
                .GetProperty("Rows", BindingFlags.Public | BindingFlags.Instance)!
                .SetValue(component, table);

            var builder = new RenderTreeBuilder();
            var exception = Record.Exception(() => s_buildRenderTree.Invoke(component, new object[] { builder }));
            Assert.Null(exception);
        }
    }
}
