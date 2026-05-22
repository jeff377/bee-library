using System.ComponentModel;
using System.Reflection;
using Bee.Definition.Layouts;
using Bee.UI.Maui.Controls;

namespace Bee.UI.Maui.UnitTests.Controls
{
    /// <summary>
    /// Reflection-based checks for the private helper methods that drive
    /// <see cref="DynamicForm"/> layout placement. Mirrors the Blazor
    /// <c>DynamicFormHelperTests</c> structure so a reader can pair the two and
    /// verify cross-family parity at a glance.
    /// </summary>
    public class DynamicFormHelperTests
    {
        private static readonly Type[] s_intOrNullParam = [typeof(int?)];
        private static readonly Type[] s_layoutFieldParam = [typeof(LayoutField)];
        private static readonly Type[] s_intParam = [typeof(int)];

        [Theory]
        [InlineData(null, 1)]
        [InlineData(0, 1)]
        [InlineData(-3, 1)]
        [InlineData(1, 1)]
        [InlineData(3, 3)]
        [DisplayName("NormalizeColumnCount 對 null/負值/0 一律回傳 1,正值原樣回傳")]
        public void NormalizeColumnCount_ClampsToMinimumOne(int? input, int expected)
        {
            var method = typeof(DynamicForm).GetMethod(
                "NormalizeColumnCount",
                BindingFlags.NonPublic | BindingFlags.Static,
                null,
                s_intOrNullParam,
                null);
            Assert.NotNull(method);

            var result = (int)method!.Invoke(null, new object?[] { input })!;
            Assert.Equal(expected, result);
        }

        [Fact]
        [DisplayName("NormalizeSpans 預設值（RowSpan/ColumnSpan = 0）應 clamp 為 (1, 1)")]
        public void NormalizeSpans_DefaultField_ReturnsOneOne()
        {
            var field = new LayoutField();

            var method = typeof(DynamicForm).GetMethod(
                "NormalizeSpans",
                BindingFlags.NonPublic | BindingFlags.Static,
                null,
                s_layoutFieldParam,
                null);
            Assert.NotNull(method);

            var result = ((int rowSpan, int columnSpan))method!.Invoke(null, new object[] { field })!;
            Assert.Equal(1, result.rowSpan);
            Assert.Equal(1, result.columnSpan);
        }

        [Fact]
        [DisplayName("NormalizeSpans 正值原樣回傳")]
        public void NormalizeSpans_PositiveSpans_ReturnsAsIs()
        {
            var field = new LayoutField { RowSpan = 2, ColumnSpan = 3 };

            var method = typeof(DynamicForm).GetMethod(
                "NormalizeSpans",
                BindingFlags.NonPublic | BindingFlags.Static,
                null,
                s_layoutFieldParam,
                null);
            Assert.NotNull(method);

            var result = ((int rowSpan, int columnSpan))method!.Invoke(null, new object[] { field })!;
            Assert.Equal(2, result.rowSpan);
            Assert.Equal(3, result.columnSpan);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(5)]
        [DisplayName("BuildColumnDefinitions 回傳指定欄數,每欄寬度為 Star")]
        public void BuildColumnDefinitions_ProducesStarColumnsMatchingCount(int columnCount)
        {
            var method = typeof(DynamicForm).GetMethod(
                "BuildColumnDefinitions",
                BindingFlags.NonPublic | BindingFlags.Static,
                null,
                s_intParam,
                null);
            Assert.NotNull(method);

            var defs = (ColumnDefinitionCollection)method!.Invoke(null, new object[] { columnCount })!;
            Assert.Equal(columnCount, defs.Count);
            Assert.All(defs, def => Assert.Equal(GridLength.Star, def.Width));
        }
    }
}
