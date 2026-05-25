using System.ComponentModel;
using System.Reflection;
using Bee.Base.Data;
using Bee.Definition.Forms;
using Bee.Definition.Layouts;
using Bee.UI.Maui.Controls;
using Bee.UI.Maui.DataObjects;

namespace Bee.UI.Maui.UnitTests.Controls
{
    /// <summary>
    /// 補強 <see cref="DynamicForm.BuildInputControl"/> 中尚未覆蓋的控制項類型：
    /// <see cref="ControlType.YearMonthEdit"/> 與 <see cref="ControlType.DropDownEdit"/>。
    /// </summary>
    public class DynamicFormCoverageTests
    {
        private static readonly Type[] s_buildInputParams = [typeof(LayoutField), typeof(string)];

        private static FormSchema BuildSchema()
        {
            var schema = new FormSchema("Employee", "Employee");
            var master = schema.Tables!.Add("Employee", "Employee");
            master.Fields!.Add("emp_id", "ID", FieldDbType.String);
            return schema;
        }

        private static MethodInfo GetBuildInputControlMethod()
        {
            var method = typeof(DynamicForm).GetMethod(
                "BuildInputControl",
                BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                s_buildInputParams,
                null);
            Assert.NotNull(method);
            return method!;
        }

        [Fact]
        [DisplayName("BuildInputControl YearMonthEdit 欄位應建立 DatePicker 且不拋例外")]
        public void BuildInputControl_YearMonthEditField_DoesNotThrow()
        {
            var schema = BuildSchema();
            var component = new DynamicForm();
            component.DataObject = new FormDataObject(schema);

            var method = GetBuildInputControlMethod();
            var field = new LayoutField { FieldName = "hire_month", ControlType = ControlType.YearMonthEdit };
            string rawValue = "2024-03";
            var ex = Record.Exception(() => method.Invoke(component, new object[] { field, rawValue }));
            Assert.Null(ex);
        }

        [Fact]
        [DisplayName("BuildInputControl DropDownEdit 欄位應建立 Picker 且不拋例外")]
        public void BuildInputControl_DropDownEditField_DoesNotThrow()
        {
            var schema = BuildSchema();
            var component = new DynamicForm();
            component.DataObject = new FormDataObject(schema);

            var method = GetBuildInputControlMethod();
            var field = new LayoutField { FieldName = "status", ControlType = ControlType.DropDownEdit };
            string rawValue = string.Empty;
            var ex = Record.Exception(() => method.Invoke(component, new object[] { field, rawValue }));
            Assert.Null(ex);
        }
    }
}
