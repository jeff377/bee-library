using System.ComponentModel;
using System.Reflection;
using Bee.Base.Data;
using Bee.Definition.Forms;
using Bee.Web.Blazor.Wasm.Components;
using Bee.Web.Blazor.Wasm.DataObjects;
using Microsoft.AspNetCore.Components;

namespace Bee.Web.Blazor.Wasm.UnitTests.Components
{
    /// <summary>
    /// Structural checks for <see cref="DynamicForm"/>. Phase 1a verifies the public
    /// component surface and that wiring through <see cref="FormSchema.GetFormLayout"/>
    /// produces a layout the component is willing to consume; deeper render assertions
    /// (with bUnit) land alongside the host-app sample in Phase 1b.
    /// </summary>
    public class DynamicFormTests
    {
        private static FormSchema BuildSchema()
        {
            var schema = new FormSchema("Employee", "Employee");
            var master = schema.Tables!.Add("Employee", "Employee");
            master.Fields!.Add("emp_id", "ID", FieldDbType.String);
            master.Fields.Add("is_active", "Active", FieldDbType.Boolean);
            master.Fields.Add("hire_date", "Hire Date", FieldDbType.Date);
            return schema;
        }

        private static PropertyInfo GetProperty(string name)
        {
            var property = typeof(DynamicForm).GetProperty(
                name, BindingFlags.Public | BindingFlags.Instance);
            Assert.NotNull(property);
            return property!;
        }

        [Fact]
        [DisplayName("DynamicForm 為 Blazor ComponentBase 子類別")]
        public void Type_IsComponentBaseSubclass()
        {
            Assert.True(typeof(ComponentBase).IsAssignableFrom(typeof(DynamicForm)));
        }

        [Theory]
        [InlineData(nameof(DynamicForm.Layout))]
        [InlineData(nameof(DynamicForm.DataObject))]
        [InlineData(nameof(DynamicForm.IdPrefix))]
        [DisplayName("公開屬性皆標註 [Parameter]")]
        public void PublicProperties_AreMarkedAsParameters(string name)
        {
            var property = GetProperty(name);
            Assert.NotNull(property.GetCustomAttribute<ParameterAttribute>());
        }

        [Fact]
        [DisplayName("可實例化 DynamicForm 並透過 reflection 指派 Layout/DataObject 參數")]
        public void CanInstantiateAndAssignParameters()
        {
            var schema = BuildSchema();
            var layout = schema.GetFormLayout();
            var dataObject = new FormDataObject(schema);

            var component = new DynamicForm();

            // Use reflection to bypass BL0005 — production callers assign parameters
            // through the Blazor renderer (SetParametersAsync), which is hard to drive
            // outside bUnit; this is a structural smoke check only.
            GetProperty(nameof(DynamicForm.Layout)).SetValue(component, layout);
            GetProperty(nameof(DynamicForm.DataObject)).SetValue(component, dataObject);
            GetProperty(nameof(DynamicForm.IdPrefix)).SetValue(component, "test-prefix");

            Assert.Same(layout, component.Layout);
            Assert.Same(dataObject, component.DataObject);
            Assert.Equal("test-prefix", component.IdPrefix);
        }

        [Fact]
        [DisplayName("FormLayoutGenerator 產出的 FormLayout 至少含一個 Section,可被 DynamicForm 消費")]
        public void GeneratedLayout_HasAtLeastOneSection()
        {
            var schema = BuildSchema();
            var layout = schema.GetFormLayout();

            Assert.NotNull(layout.Sections);
            Assert.NotEmpty(layout.Sections!);
            Assert.All(layout.Sections!, section =>
            {
                Assert.NotNull(section.Fields);
                Assert.NotEmpty(section.Fields!);
            });
        }
    }
}
