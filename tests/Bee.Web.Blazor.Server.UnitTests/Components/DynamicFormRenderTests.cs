using System.ComponentModel;
using Bee.Base.Data;
using Bee.Definition.Forms;
using Bee.Web.Blazor.Server.Components;
using Bee.Web.Blazor.Server.DataObjects;
using Bunit;

namespace Bee.Web.Blazor.Server.UnitTests.Components
{
    /// <summary>
    /// 渲染測試：觸發 DynamicForm.razor 的 BuildRenderTree，補強 .razor 模板行覆蓋率。
    /// </summary>
    public class DynamicFormRenderTests : BunitContext
    {
        private static FormSchema BuildSchema()
        {
            var schema = new FormSchema("Employee", "Employee");
            var master = schema.Tables!.Add("Employee", "Employee");
            master.Fields!.Add("emp_id", "ID", FieldDbType.String);
            master.Fields.Add("is_active", "Active", FieldDbType.Boolean);
            master.Fields.Add("created_at", "Created", FieldDbType.DateTime);
            return schema;
        }

        [Fact]
        [DisplayName("DynamicForm Layout 為 null 時應渲染空狀態 div")]
        public void DynamicForm_NullLayout_RendersEmptyDiv()
        {
            var cut = Render<DynamicForm>();
            Assert.NotNull(cut.Find("div.bee-dynamic-form--empty"));
        }

        [Fact]
        [DisplayName("DynamicForm 傳入 Layout 與 DataObject 後應渲染表單容器")]
        public void DynamicForm_WithLayoutAndDataObject_RendersFormContainer()
        {
            var schema = BuildSchema();
            var layout = schema.GetFormLayout();
            var dataObject = new FormDataObject(schema);

            var cut = Render<DynamicForm>(p => p
                .Add(c => c.Layout, layout)
                .Add(c => c.DataObject, dataObject));

            Assert.NotNull(cut.Find("div.bee-dynamic-form"));
        }

        [Fact]
        [DisplayName("DynamicForm Section ShowCaption 為 false 時不應渲染 legend 元素")]
        public void DynamicForm_SectionShowCaptionFalse_DoesNotRenderLegend()
        {
            var schema = BuildSchema();
            var layout = schema.GetFormLayout();
            foreach (var section in layout.Sections!)
                section.ShowCaption = false;
            var dataObject = new FormDataObject(schema);

            var cut = Render<DynamicForm>(p => p
                .Add(c => c.Layout, layout)
                .Add(c => c.DataObject, dataObject));

            Assert.Empty(cut.FindAll("legend.bee-dynamic-form__section-caption"));
        }
    }
}
