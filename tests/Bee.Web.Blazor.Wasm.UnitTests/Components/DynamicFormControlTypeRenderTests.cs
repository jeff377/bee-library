using Bee.Definition.Collections;
using System.ComponentModel;
using Bee.Base.Data;
using Bee.Definition.Forms;
using Bee.Definition.Layouts;
using Bee.Web.Blazor.Wasm.Components;
using Bee.Web.Blazor.Wasm.DataObjects;
using Bunit;

namespace Bee.Web.Blazor.Wasm.UnitTests.Components
{
    /// <summary>
    /// 補強 DynamicForm.razor switch 中 YearMonthEdit / MemoEdit / DropDownEdit 分支的渲染覆蓋率。
    /// </summary>
    public class DynamicFormControlTypeRenderTests : BunitContext
    {
        private static void SetFirstFieldControlType(FormLayout layout, ControlType controlType)
        {
            layout.Sections![0].Fields![0].ControlType = controlType;
        }

        [Fact]
        [DisplayName("DynamicForm YearMonthEdit 欄位應渲染 input[type=month] 元素")]
        public void DynamicForm_YearMonthEditField_RendersMonthInput()
        {
            var schema = new FormSchema("T", "T");
            schema.Tables!.Add("T", "T").Fields!.Add("report_month", "Month", FieldDbType.String);
            var layout = schema.GetFormLayout();
            SetFirstFieldControlType(layout, ControlType.YearMonthEdit);
            var dataObject = new FormDataObject(schema);

            var cut = Render<DynamicForm>(p => p
                .Add(c => c.Layout, layout)
                .Add(c => c.DataObject, dataObject));

            Assert.NotNull(cut.Find("input[type='month']"));
        }

        [Fact]
        [DisplayName("DynamicForm MemoEdit 欄位應渲染 textarea 元素")]
        public void DynamicForm_MemoEditField_RendersTextarea()
        {
            var schema = new FormSchema("T", "T");
            schema.Tables!.Add("T", "T").Fields!.Add("remark", "Remark", FieldDbType.String);
            var layout = schema.GetFormLayout();
            SetFirstFieldControlType(layout, ControlType.MemoEdit);
            var dataObject = new FormDataObject(schema);

            var cut = Render<DynamicForm>(p => p
                .Add(c => c.Layout, layout)
                .Add(c => c.DataObject, dataObject));

            Assert.NotNull(cut.Find("textarea.bee-dynamic-form__input--memo"));
        }

        [Fact]
        [DisplayName("DynamicForm DropDownEdit 欄位應渲染 select 元素並包含選項")]
        public void DynamicForm_DropDownEditField_RendersSelectWithOptions()
        {
            var schema = new FormSchema("T", "T");
            var master = schema.Tables!.Add("T", "T");
            var schemaField = master.Fields!.Add("status", "Status", FieldDbType.String);
            schemaField.ListItems!.Add("A", "Active");
            schemaField.ListItems.Add("I", "Inactive");
            var layout = schema.GetFormLayout();
            SetFirstFieldControlType(layout, ControlType.DropDownEdit);
            var dataObject = new FormDataObject(schema);

            var cut = Render<DynamicForm>(p => p
                .Add(c => c.Layout, layout)
                .Add(c => c.DataObject, dataObject));

            Assert.NotNull(cut.Find("select.bee-dynamic-form__input--select"));
            Assert.Equal(2, cut.FindAll("option").Count);
        }
    }
}
