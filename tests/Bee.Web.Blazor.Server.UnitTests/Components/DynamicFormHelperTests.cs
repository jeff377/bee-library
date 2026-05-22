using System.ComponentModel;
using System.Reflection;
using Bee.Definition.Collections;
using Bee.Definition.Layouts;
using Bee.Web.Blazor.Server.Components;

namespace Bee.Web.Blazor.Server.UnitTests.Components
{
    public class DynamicFormHelperTests
    {
        private static readonly Type[] s_layoutFieldParam = [typeof(LayoutField)];
        private static readonly Type[] s_layoutSectionParam = [typeof(LayoutSection)];

        [Fact]
        [DisplayName("BuildGridStyle Layout 為 null 時應預設回傳 1 欄 CSS grid 樣式")]
        public void BuildGridStyle_NullLayout_DefaultsToOneColumn()
        {
            var component = new DynamicForm();
            var method = typeof(DynamicForm).GetMethod("BuildGridStyle",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);
            var result = method!.Invoke(component, null) as string;
            Assert.Equal("display:grid;grid-template-columns:repeat(1,minmax(0,1fr));gap:8px", result);
        }

        [Fact]
        [DisplayName("BuildGridStyle ColumnCount 為 3 時應回傳 3 欄 CSS grid 樣式")]
        public void BuildGridStyle_ThreeColumns_ReturnsThreeColumnStyle()
        {
            var component = new DynamicForm();
            typeof(DynamicForm)
                .GetProperty("Layout", BindingFlags.Public | BindingFlags.Instance)!
                .SetValue(component, new FormLayout { ColumnCount = 3 });
            var method = typeof(DynamicForm).GetMethod("BuildGridStyle",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);
            var result = method!.Invoke(component, null) as string;
            Assert.Equal("display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:8px", result);
        }

        [Fact]
        [DisplayName("BuildFieldStyle 預設 span 應回傳 grid-row:span 1;grid-column:span 1 樣式")]
        public void BuildFieldStyle_DefaultSpans_ReturnsSpanOneOne()
        {
            var field = new LayoutField();
            var method = typeof(DynamicForm).GetMethod("BuildFieldStyle",
                BindingFlags.NonPublic | BindingFlags.Static, null, s_layoutFieldParam, null);
            Assert.NotNull(method);
            var result = method!.Invoke(null, new object[] { field }) as string;
            Assert.Equal("grid-row:span 1;grid-column:span 1", result);
        }

        [Theory]
        [InlineData(2, 3, "grid-row:span 2;grid-column:span 3")]
        [InlineData(1, 4, "grid-row:span 1;grid-column:span 4")]
        [DisplayName("BuildFieldStyle 指定 span 應回傳對應 CSS grid 樣式")]
        public void BuildFieldStyle_CustomSpans_ReturnsCorrectStyle(int rowSpan, int colSpan, string expected)
        {
            var field = new LayoutField { RowSpan = rowSpan, ColumnSpan = colSpan };
            var method = typeof(DynamicForm).GetMethod("BuildFieldStyle",
                BindingFlags.NonPublic | BindingFlags.Static, null, s_layoutFieldParam, null);
            Assert.NotNull(method);
            var result = method!.Invoke(null, new object[] { field }) as string;
            Assert.Equal(expected, result);
        }

        [Fact]
        [DisplayName("FieldInputId 應回傳 IdPrefix 與 FieldName 組合的 HTML id 字串")]
        public void FieldInputId_WithPrefix_ReturnsPrefixedId()
        {
            var component = new DynamicForm();
            typeof(DynamicForm)
                .GetProperty("IdPrefix", BindingFlags.Public | BindingFlags.Instance)!
                .SetValue(component, "frm");
            var field = new LayoutField { FieldName = "emp_name" };
            var method = typeof(DynamicForm).GetMethod("FieldInputId",
                BindingFlags.NonPublic | BindingFlags.Instance, null, s_layoutFieldParam, null);
            Assert.NotNull(method);
            var result = method!.Invoke(component, new object[] { field }) as string;
            Assert.Equal("frm-emp_name", result);
        }

        [Fact]
        [DisplayName("EnumerateSections Layout 為 null 時應回傳空序列")]
        public void EnumerateSections_NullLayout_ReturnsEmpty()
        {
            var component = new DynamicForm();
            var method = typeof(DynamicForm).GetMethod("EnumerateSections",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);
            var result = method!.Invoke(component, null) as IEnumerable<LayoutSection>;
            Assert.NotNull(result);
            Assert.Empty(result!);
        }

        [Fact]
        [DisplayName("EnumerateSections Layout 含一個 Section 時應回傳該 Section")]
        public void EnumerateSections_WithOneSection_ReturnsSingleSection()
        {
            var component = new DynamicForm();
            var layout = new FormLayout();
            layout.Sections!.Add(new LayoutSection { Name = "Main" });
            typeof(DynamicForm)
                .GetProperty("Layout", BindingFlags.Public | BindingFlags.Instance)!
                .SetValue(component, layout);
            var method = typeof(DynamicForm).GetMethod("EnumerateSections",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);
            var result = method!.Invoke(component, null) as IEnumerable<LayoutSection>;
            Assert.NotNull(result);
            Assert.Single(result!);
        }

        [Fact]
        [DisplayName("EnumerateFields 應只回傳 Visible 為 true 的欄位")]
        public void EnumerateFields_MixedVisibility_ReturnsOnlyVisibleFields()
        {
            var section = new LayoutSection();
            section.Fields!.Add(new LayoutField { FieldName = "visible_field", Visible = true });
            section.Fields.Add(new LayoutField { FieldName = "hidden_field", Visible = false });
            var method = typeof(DynamicForm).GetMethod("EnumerateFields",
                BindingFlags.NonPublic | BindingFlags.Static, null, s_layoutSectionParam, null);
            Assert.NotNull(method);
            var result = method!.Invoke(null, new object[] { section }) as IEnumerable<LayoutField>;
            Assert.NotNull(result);
            var fields = result!.ToList();
            Assert.Single(fields);
            Assert.Equal("visible_field", fields[0].FieldName);
        }

        [Fact]
        [DisplayName("EnumerateOptions DataObject 為 null 時應回傳空集合")]
        public void EnumerateOptions_NullDataObject_ReturnsEmpty()
        {
            var component = new DynamicForm();
            var field = new LayoutField { FieldName = "status" };
            var method = typeof(DynamicForm).GetMethod("EnumerateOptions",
                BindingFlags.NonPublic | BindingFlags.Instance, null, s_layoutFieldParam, null);
            Assert.NotNull(method);
            var result = method!.Invoke(component, new object[] { field }) as IEnumerable<ListItem>;
            Assert.NotNull(result);
            Assert.Empty(result!);
        }
    }
}
