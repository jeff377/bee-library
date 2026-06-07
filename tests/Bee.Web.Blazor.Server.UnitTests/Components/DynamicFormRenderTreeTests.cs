using System.ComponentModel;
using System.Reflection;
using Bee.Definition.Forms;
using Bee.Definition.Layouts;
using Bee.Web.Blazor.Server.Components;
using Bee.Web.Blazor.Server.DataObjects;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bee.Web.Blazor.Server.UnitTests.Components
{
    public class DynamicFormRenderTreeTests
    {
        private static readonly MethodInfo s_buildRenderTree =
            typeof(DynamicForm).GetMethod("BuildRenderTree",
                BindingFlags.Instance | BindingFlags.NonPublic)!;

        private static FormDataObject CreateMinimalDataObject()
        {
            var schema = new FormSchema("Test", "Test");
            schema.Tables!.Add("Test", "Test");
            return new FormDataObject(schema);
        }

        [Fact]
        [DisplayName("BuildRenderTree Layout 為 null 時應渲染空白區塊，不拋例外")]
        public void BuildRenderTree_NullLayout_RendersEmptyState()
        {
            var component = new DynamicForm();
            var builder = new RenderTreeBuilder();
            var exception = Record.Exception(() => s_buildRenderTree.Invoke(component, new object[] { builder }));
            Assert.Null(exception);
        }

        [Fact]
        [DisplayName("BuildRenderTree DataObject 為 null 時應渲染空白區塊，不拋例外")]
        public void BuildRenderTree_NullDataObject_RendersEmptyState()
        {
            var component = new DynamicForm();
            typeof(DynamicForm)
                .GetProperty("Layout", BindingFlags.Public | BindingFlags.Instance)!
                .SetValue(component, new FormLayout());
            var builder = new RenderTreeBuilder();
            var exception = Record.Exception(() => s_buildRenderTree.Invoke(component, new object[] { builder }));
            Assert.Null(exception);
        }

        [Fact]
        [DisplayName("BuildRenderTree 設定 Layout 與 DataObject 並含所有 ControlType 欄位時應渲染完整表單，不拋例外")]
        public void BuildRenderTree_WithAllControlTypes_RendersFormWithoutThrowing()
        {
            var layout = new FormLayout { ColumnCount = 3 };
            var section = new LayoutSection { Name = "Main", Caption = "Section", ShowCaption = true };
            section.Fields!.Add(new LayoutField { FieldName = "text_f", Caption = "Text", ControlType = ControlType.TextEdit, Visible = true });
            section.Fields.Add(new LayoutField { FieldName = "check_f", Caption = "Check", ControlType = ControlType.CheckEdit, Visible = true });
            section.Fields.Add(new LayoutField { FieldName = "date_f", Caption = "Date", ControlType = ControlType.DateEdit, Visible = true });
            section.Fields.Add(new LayoutField { FieldName = "month_f", Caption = "Month", ControlType = ControlType.YearMonthEdit, Visible = true });
            section.Fields.Add(new LayoutField { FieldName = "memo_f", Caption = "Memo", ControlType = ControlType.MemoEdit, Visible = true });
            section.Fields.Add(new LayoutField { FieldName = "drop_f", Caption = "Dropdown", ControlType = ControlType.DropDownEdit, Visible = true });
            layout.Sections!.Add(section);

            var component = new DynamicForm();
            typeof(DynamicForm)
                .GetProperty("Layout", BindingFlags.Public | BindingFlags.Instance)!
                .SetValue(component, layout);
            typeof(DynamicForm)
                .GetProperty("DataObject", BindingFlags.Public | BindingFlags.Instance)!
                .SetValue(component, CreateMinimalDataObject());

            var builder = new RenderTreeBuilder();
            var exception = Record.Exception(() => s_buildRenderTree.Invoke(component, new object[] { builder }));
            Assert.Null(exception);
        }

        [Fact]
        [DisplayName("BuildRenderTree Section ShowCaption 為 false 時應略過 legend 渲染，不拋例外")]
        public void BuildRenderTree_SectionShowCaptionFalse_SkipsCaption()
        {
            var layout = new FormLayout();
            var section = new LayoutSection { Name = "NoCaption", ShowCaption = false };
            section.Fields!.Add(new LayoutField { FieldName = "name_f", Caption = "Name", ControlType = ControlType.TextEdit, Visible = true });
            layout.Sections!.Add(section);

            var component = new DynamicForm();
            typeof(DynamicForm)
                .GetProperty("Layout", BindingFlags.Public | BindingFlags.Instance)!
                .SetValue(component, layout);
            typeof(DynamicForm)
                .GetProperty("DataObject", BindingFlags.Public | BindingFlags.Instance)!
                .SetValue(component, CreateMinimalDataObject());

            var builder = new RenderTreeBuilder();
            var exception = Record.Exception(() => s_buildRenderTree.Invoke(component, new object[] { builder }));
            Assert.Null(exception);
        }
    }
}
