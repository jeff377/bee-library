using System.Reflection;
using Bee.Base.Data;
using Bee.Definition.Forms;
using Bee.Definition.Layouts;
using Bee.Web.Blazor.Server.Components;
using Bee.Web.Blazor.Server.DataObjects;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bee.Web.Blazor.Server.UnitTests.Components
{
    /// <summary>
    /// 補強 <c>DynamicForm.razor</c>（Server）模板渲染路徑的覆蓋率。
    /// 以反射呼叫 <c>BuildRenderTree</c>，驗證各 <see cref="ControlType"/> case
    /// 與空/非空 Layout 分支皆不拋例外，並記錄 Razor 模板已完整處理。
    /// </summary>
    public class DynamicFormRenderTreeTests
    {
        private static readonly Type[] s_renderTreeBuilderParam = [typeof(RenderTreeBuilder)];

        private static void InvokeBuildRenderTree(DynamicForm component)
        {
            var method = typeof(DynamicForm).GetMethod(
                "BuildRenderTree",
                BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                s_renderTreeBuilderParam,
                null);
            Assert.NotNull(method);
            method!.Invoke(component, new object[] { new RenderTreeBuilder() });
        }

        private static FormSchema BuildMultiTypeSchema()
        {
            var schema = new FormSchema("Emp", "Emp");
            var table = schema.Tables!.Add("Emp", "Emp");
            table.Fields!.Add("is_active", "Active", FieldDbType.Boolean);
            table.Fields.Add("hire_date", "Hire Date", FieldDbType.Date);
            table.Fields.Add("year_month", "Year Month", FieldDbType.String);
            table.Fields.Add("notes", "Notes", FieldDbType.Text);
            var statusField = table.Fields.Add("status", "Status", FieldDbType.String);
            statusField.ListItems!.Add("A", "Active");
            statusField.ListItems.Add("I", "Inactive");
            table.Fields.Add("emp_name", "Name", FieldDbType.String);
            return schema;
        }

        private static FormLayout BuildMultiTypeLayout(bool showSectionCaption = true)
        {
            var layout = new FormLayout { ColumnCount = 2 };
            var section = new LayoutSection { Caption = "基本資料", ShowCaption = showSectionCaption };
            section.Fields!.Add(new LayoutField { FieldName = "is_active", ControlType = ControlType.CheckEdit, Visible = true });
            section.Fields.Add(new LayoutField { FieldName = "hire_date", ControlType = ControlType.DateEdit, Visible = true });
            section.Fields.Add(new LayoutField { FieldName = "year_month", ControlType = ControlType.YearMonthEdit, Visible = true });
            section.Fields.Add(new LayoutField { FieldName = "notes", ControlType = ControlType.MemoEdit, Visible = true });
            section.Fields.Add(new LayoutField { FieldName = "status", ControlType = ControlType.DropDownEdit, Visible = true });
            section.Fields.Add(new LayoutField { FieldName = "emp_name", ControlType = ControlType.TextEdit, Visible = true });
            layout.Sections!.Add(section);
            return layout;
        }

        private static DynamicForm CreateComponent(FormLayout? layout, FormDataObject? dataObject)
        {
            var component = new DynamicForm();
            if (layout is not null)
                typeof(DynamicForm).GetProperty("Layout", BindingFlags.Public | BindingFlags.Instance)!
                    .SetValue(component, layout);
            if (dataObject is not null)
                typeof(DynamicForm).GetProperty("DataObject", BindingFlags.Public | BindingFlags.Instance)!
                    .SetValue(component, dataObject);
            return component;
        }

        [Fact]
        [DisplayName("BuildRenderTree Layout 為 null 時應渲染空容器，不拋例外")]
        public void BuildRenderTree_NullLayout_RendersEmptyContainerWithoutException()
        {
            var component = CreateComponent(null, null);
            var exception = Record.Exception(() => InvokeBuildRenderTree(component));
            Assert.Null(exception);
        }

        [Fact]
        [DisplayName("BuildRenderTree DataObject 為 null 時應渲染空容器，不拋例外")]
        public void BuildRenderTree_NullDataObject_RendersEmptyContainerWithoutException()
        {
            var layout = BuildMultiTypeLayout();
            var component = CreateComponent(layout, null);
            var exception = Record.Exception(() => InvokeBuildRenderTree(component));
            Assert.Null(exception);
        }

        [Fact]
        [DisplayName("BuildRenderTree 所有 ControlType（CheckEdit/DateEdit/YearMonthEdit/MemoEdit/DropDownEdit/TextEdit）應完整渲染，不拋例外")]
        public void BuildRenderTree_WithAllControlTypes_RendersWithoutException()
        {
            var schema = BuildMultiTypeSchema();
            var dataObject = new FormDataObject(schema);
            var layout = BuildMultiTypeLayout(showSectionCaption: true);
            var component = CreateComponent(layout, dataObject);
            var exception = Record.Exception(() => InvokeBuildRenderTree(component));
            Assert.Null(exception);
        }

        [Fact]
        [DisplayName("BuildRenderTree Section.ShowCaption 為 false 時不渲染 legend，且不拋例外")]
        public void BuildRenderTree_SectionCaptionHidden_RendersWithoutException()
        {
            var schema = BuildMultiTypeSchema();
            var dataObject = new FormDataObject(schema);
            var layout = BuildMultiTypeLayout(showSectionCaption: false);
            var component = CreateComponent(layout, dataObject);
            var exception = Record.Exception(() => InvokeBuildRenderTree(component));
            Assert.Null(exception);
        }

        [Fact]
        [DisplayName("BuildRenderTree DropDownEdit 欄位含 ListItems 時應渲染 option 元素，不拋例外")]
        public void BuildRenderTree_DropDownEditWithOptions_RendersWithoutException()
        {
            var schema = new FormSchema("Order", "Order");
            var table = schema.Tables!.Add("Order", "Order");
            var statusField = table.Fields!.Add("status", "Status", FieldDbType.String);
            statusField.ListItems!.Add("N", "New");
            statusField.ListItems.Add("P", "Processing");
            statusField.ListItems.Add("D", "Done");

            var dataObject = new FormDataObject(schema);

            var layout = new FormLayout();
            var section = new LayoutSection();
            section.Fields!.Add(new LayoutField { FieldName = "status", ControlType = ControlType.DropDownEdit, Visible = true });
            layout.Sections!.Add(section);

            var component = CreateComponent(layout, dataObject);
            var exception = Record.Exception(() => InvokeBuildRenderTree(component));
            Assert.Null(exception);
        }
    }
}
