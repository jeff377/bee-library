using System.ComponentModel;
using System.Reflection;
using Bee.Base.Data;
using Bee.Definition.Collections;
using Bee.Definition.Forms;
using Bee.Definition.Layouts;
using Bee.Web.Blazor.Server.Components;
using Bee.Web.Blazor.Server.DataObjects;
using Microsoft.AspNetCore.Components;

namespace Bee.Web.Blazor.Server.UnitTests.Components
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

        private static MethodInfo GetPrivateInstanceMethod(string name)
        {
            var method = typeof(DynamicForm).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);
            return method!;
        }

        private static MethodInfo GetPrivateStaticMethod(string name)
        {
            var method = typeof(DynamicForm).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);
            return method!;
        }

        [Fact]
        [DisplayName("EnumerateSections 在 Layout 為 null 時回傳空列舉")]
        public void EnumerateSections_NullLayout_ReturnsEmpty()
        {
            var component = new DynamicForm();
            var method = GetPrivateInstanceMethod("EnumerateSections");
            var result = (IEnumerable<LayoutSection>)method.Invoke(component, null)!;
            Assert.Empty(result);
        }

        [Fact]
        [DisplayName("EnumerateSections 在 Layout 含有 Section 時回傳對應集合")]
        public void EnumerateSections_WithSections_ReturnsSections()
        {
            var schema = BuildSchema();
            var layout = schema.GetFormLayout();
            var component = new DynamicForm();
            GetProperty(nameof(DynamicForm.Layout)).SetValue(component, layout);
            var method = GetPrivateInstanceMethod("EnumerateSections");
            var result = ((IEnumerable<LayoutSection>)method.Invoke(component, null)!).ToList();
            Assert.NotEmpty(result);
        }

        [Fact]
        [DisplayName("EnumerateFields 篩選後只回傳 Visible=true 的欄位")]
        public void EnumerateFields_MixedVisibility_ReturnsOnlyVisible()
        {
            var section = new LayoutSection();
            section.Fields!.Add(new LayoutField { FieldName = "visible_col", Visible = true });
            section.Fields.Add(new LayoutField { FieldName = "hidden_col", Visible = false });
            var method = GetPrivateStaticMethod("EnumerateFields");
            var result = ((IEnumerable<LayoutField>)method.Invoke(null, new object[] { section })!).ToList();
            Assert.Single(result);
            Assert.Equal("visible_col", result[0].FieldName);
        }

        [Fact]
        [DisplayName("EnumerateFields 所有欄位 Visible=true 時全數回傳")]
        public void EnumerateFields_AllVisible_ReturnsAllFields()
        {
            var section = new LayoutSection();
            section.Fields!.Add(new LayoutField { FieldName = "field_a", Visible = true });
            section.Fields.Add(new LayoutField { FieldName = "field_b", Visible = true });
            var method = GetPrivateStaticMethod("EnumerateFields");
            var result = ((IEnumerable<LayoutField>)method.Invoke(null, new object[] { section })!).ToList();
            Assert.Equal(2, result.Count);
        }

        [Fact]
        [DisplayName("EnumerateOptions 在 DataObject 為 null 時回傳空列舉")]
        public void EnumerateOptions_NullDataObject_ReturnsEmpty()
        {
            var component = new DynamicForm();
            var field = new LayoutField { FieldName = "status" };
            var method = GetPrivateInstanceMethod("EnumerateOptions");
            var result = (IEnumerable<ListItem>)method.Invoke(component, new object[] { field })!;
            Assert.Empty(result);
        }

        [Fact]
        [DisplayName("EnumerateOptions 欄位名稱不在 Schema 時回傳空列舉")]
        public void EnumerateOptions_FieldNotInSchema_ReturnsEmpty()
        {
            var schema = BuildSchema();
            var dataObject = new FormDataObject(schema);
            var component = new DynamicForm();
            GetProperty(nameof(DynamicForm.DataObject)).SetValue(component, dataObject);
            var field = new LayoutField { FieldName = "nonexistent_col" };
            var method = GetPrivateInstanceMethod("EnumerateOptions");
            var result = (IEnumerable<ListItem>)method.Invoke(component, new object[] { field })!;
            Assert.Empty(result);
        }

        [Fact]
        [DisplayName("FieldInputId 回傳 IdPrefix 與 FieldName 組合的格式化字串")]
        public void FieldInputId_ValidField_ReturnsFormattedId()
        {
            var component = new DynamicForm();
            GetProperty(nameof(DynamicForm.IdPrefix)).SetValue(component, "my-form");
            var field = new LayoutField { FieldName = "emp_name" };
            var method = GetPrivateInstanceMethod("FieldInputId");
            var result = (string)method.Invoke(component, new object[] { field })!;
            Assert.Equal("my-form-emp_name", result);
        }

        [Fact]
        [DisplayName("BuildGridStyle 在 Layout 為 null 時產生單欄格線樣式")]
        public void BuildGridStyle_NullLayout_UsesOneColumnStyle()
        {
            var component = new DynamicForm();
            var method = GetPrivateInstanceMethod("BuildGridStyle");
            var result = (string)method.Invoke(component, null)!;
            Assert.Equal("display:grid;grid-template-columns:repeat(1,minmax(0,1fr));gap:8px", result);
        }

        [Fact]
        [DisplayName("BuildGridStyle 依 ColumnCount 產生對應欄數的格線樣式")]
        public void BuildGridStyle_WithColumnCount_ReturnsCorrectStyle()
        {
            var component = new DynamicForm();
            var layout = new FormLayout { ColumnCount = 3 };
            GetProperty(nameof(DynamicForm.Layout)).SetValue(component, layout);
            var method = GetPrivateInstanceMethod("BuildGridStyle");
            var result = (string)method.Invoke(component, null)!;
            Assert.Equal("display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:8px", result);
        }

        [Fact]
        [DisplayName("BuildGridStyle 在 ColumnCount 小於 1 時夾住為單欄格線樣式")]
        public void BuildGridStyle_ZeroColumnCount_ClampsToOneColumn()
        {
            var component = new DynamicForm();
            var layout = new FormLayout { ColumnCount = 0 };
            GetProperty(nameof(DynamicForm.Layout)).SetValue(component, layout);
            var method = GetPrivateInstanceMethod("BuildGridStyle");
            var result = (string)method.Invoke(component, null)!;
            Assert.Equal("display:grid;grid-template-columns:repeat(1,minmax(0,1fr));gap:8px", result);
        }

        [Fact]
        [DisplayName("BuildFieldStyle 在預設 Span 時回傳 grid-row:span 1;grid-column:span 1")]
        public void BuildFieldStyle_DefaultSpans_ReturnsSpanOne()
        {
            var field = new LayoutField { FieldName = "test_col" };
            var method = GetPrivateStaticMethod("BuildFieldStyle");
            var result = (string)method.Invoke(null, new object[] { field })!;
            Assert.Equal("grid-row:span 1;grid-column:span 1", result);
        }

        [Fact]
        [DisplayName("BuildFieldStyle 在自訂 Span 時回傳對應的 CSS Grid 樣式字串")]
        public void BuildFieldStyle_CustomSpans_ReturnsCorrectStyle()
        {
            var field = new LayoutField { FieldName = "desc_col", RowSpan = 2, ColumnSpan = 3 };
            var method = GetPrivateStaticMethod("BuildFieldStyle");
            var result = (string)method.Invoke(null, new object[] { field })!;
            Assert.Equal("grid-row:span 2;grid-column:span 3", result);
        }
    }
}
