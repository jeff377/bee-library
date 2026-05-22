using System.ComponentModel;
using System.Reflection;
using Bee.Base.Data;
using Bee.Definition.Forms;
using Bee.UI.Maui.Controls;
using Bee.UI.Maui.DataObjects;

namespace Bee.UI.Maui.UnitTests.Controls
{
    /// <summary>
    /// Structural checks for <see cref="DynamicForm"/>. Phase 1a verifies the public
    /// component surface and that wiring through <see cref="FormSchema.GetFormLayout"/>
    /// produces a layout the component is willing to consume; deeper render assertions
    /// land alongside the host-app sample in Phase 1b.
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
        [DisplayName("DynamicForm 為 MAUI ContentView 子類別")]
        public void Type_IsContentViewSubclass()
        {
            Assert.True(typeof(ContentView).IsAssignableFrom(typeof(DynamicForm)));
        }

        [Theory]
        [InlineData(nameof(DynamicForm.FormLayout), "FormLayoutProperty")]
        [InlineData(nameof(DynamicForm.DataObject), "DataObjectProperty")]
        [DisplayName("公開屬性皆有對應的 BindableProperty 註冊")]
        public void PublicProperties_HaveMatchingBindableProperty(string propertyName, string bindablePropertyFieldName)
        {
            var property = GetProperty(propertyName);
            Assert.NotNull(property);

            var bindable = typeof(DynamicForm).GetField(
                bindablePropertyFieldName,
                BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(bindable);
            Assert.Equal(typeof(BindableProperty), bindable!.FieldType);
            Assert.NotNull(bindable.GetValue(null));
        }

        [Fact]
        [DisplayName("可實例化 DynamicForm 並透過 BindableProperty 指派 FormLayout/DataObject")]
        public void CanInstantiateAndAssignParameters()
        {
            var schema = BuildSchema();
            var layout = schema.GetFormLayout();
            var dataObject = new FormDataObject(schema);

            var component = new DynamicForm();
            component.FormLayout = layout;
            component.DataObject = dataObject;

            Assert.Same(layout, component.FormLayout);
            Assert.Same(dataObject, component.DataObject);
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
