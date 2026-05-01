using System.ComponentModel;
using Bee.Base.Serialization;
using Bee.Definition.Filters;

namespace Bee.Definition.UnitTests.Filters
{
    /// <summary>
    /// FilterCondition / FilterGroup / FilterNode 邏輯與序列化測試。
    /// </summary>
    public class FilterGroupTests
    {
        [Fact]
        [DisplayName("FilterCondition 建構子應正確設定欄位、運算子與值")]
        public void FilterCondition_Constructor_SetsProperties()
        {
            // Act
            var cond = new FilterCondition("Age", ComparisonOperator.GreaterThan, 18);

            // Assert
            Assert.Equal("Age", cond.FieldName);
            Assert.Equal(ComparisonOperator.GreaterThan, cond.Operator);
            Assert.Equal(18, cond.Value);
            Assert.Null(cond.SecondValue);
            Assert.Equal(FilterNodeKind.Condition, cond.Kind);
        }

        [Fact]
        [DisplayName("FilterCondition Between 工廠方法應設定兩個值")]
        public void FilterCondition_Between_SetsBothValues()
        {
            // Arrange
            var from = new DateTime(2026, 1, 1);
            var to = new DateTime(2026, 12, 31);

            // Act
            var cond = FilterCondition.Between("HireDate", from, to);

            // Assert
            Assert.Equal(ComparisonOperator.Between, cond.Operator);
            Assert.Equal(from, cond.Value);
            Assert.Equal(to, cond.SecondValue);
        }

        [Theory]
        [InlineData("Equal")]
        [InlineData("NotEqual")]
        [InlineData("Contains")]
        [InlineData("StartsWith")]
        [InlineData("EndsWith")]
        [DisplayName("FilterCondition 工廠方法應產生對應運算子")]
        public void FilterCondition_FactoryMethods_ProduceExpectedOperator(string factory)
        {
            // Act
            FilterCondition cond = factory switch
            {
                "Equal" => FilterCondition.Equal("F", 1),
                "NotEqual" => FilterCondition.NotEqual("F", 1),
                "Contains" => FilterCondition.Contains("F", "x"),
                "StartsWith" => FilterCondition.StartsWith("F", "x"),
                "EndsWith" => FilterCondition.EndsWith("F", "x"),
                _ => throw new ArgumentOutOfRangeException(nameof(factory))
            };

            // Assert
            var expected = factory switch
            {
                "Equal" => ComparisonOperator.Equal,
                "NotEqual" => ComparisonOperator.NotEqual,
                "Contains" => ComparisonOperator.Contains,
                "StartsWith" => ComparisonOperator.StartsWith,
                "EndsWith" => ComparisonOperator.EndsWith,
                _ => throw new ArgumentOutOfRangeException(nameof(factory))
            };
            Assert.Equal(expected, cond.Operator);
        }

        [Fact]
        [DisplayName("FilterCondition In 工廠方法應儲存值集合")]
        public void FilterCondition_In_StoresEnumerable()
        {
            // Arrange
            var values = new object[] { 1, 2, 3 };

            // Act
            var cond = FilterCondition.In("Id", values);

            // Assert
            Assert.Equal(ComparisonOperator.In, cond.Operator);
            Assert.Same(values, cond.Value);
        }

        [Fact]
        [DisplayName("FilterGroup.All 應建立 AND 群組並包含子節點")]
        public void FilterGroup_All_CreatesAndGroupWithNodes()
        {
            // Arrange
            var c1 = FilterCondition.Equal("A", 1);
            var c2 = FilterCondition.Equal("B", 2);

            // Act
            var group = FilterGroup.All(c1, c2);

            // Assert
            Assert.Equal(LogicalOperator.And, group.Operator);
            Assert.Equal(2, group.Nodes.Count);
            Assert.Equal(FilterNodeKind.Group, group.Kind);
        }

        [Fact]
        [DisplayName("FilterGroup.Any 應建立 OR 群組並包含子節點")]
        public void FilterGroup_Any_CreatesOrGroupWithNodes()
        {
            // Act
            var group = FilterGroup.Any(FilterCondition.Equal("A", 1));

            // Assert
            Assert.Equal(LogicalOperator.Or, group.Operator);
            Assert.Single(group.Nodes);
        }

        [Fact]
        [DisplayName("FilterGroup 建構子帶 LogicalOperator 應設定運算子")]
        public void FilterGroup_Constructor_WithOperator_SetsOperator()
        {
            // Act
            var group = new FilterGroup(LogicalOperator.Or);

            // Assert
            Assert.Equal(LogicalOperator.Or, group.Operator);
            Assert.NotNull(group.Nodes);
            Assert.Empty(group.Nodes);
        }

        [Fact]
        [DisplayName("FilterGroup ShouldSerializeNodes 空集合應回傳 false")]
        public void FilterGroup_ShouldSerializeNodes_EmptyCollection_ReturnsFalse()
        {
            // Arrange
            var group = new FilterGroup();

            // Act & Assert
            Assert.False(group.ShouldSerializeNodes());
        }

        [Fact]
        [DisplayName("FilterGroup ShouldSerializeNodes 非空集合應回傳 true")]
        public void FilterGroup_ShouldSerializeNodes_NonEmpty_ReturnsTrue()
        {
            // Arrange
            var group = FilterGroup.All(FilterCondition.Equal("A", 1));

            // Act & Assert
            Assert.True(group.ShouldSerializeNodes());
        }

        [Fact]
        [DisplayName("FilterGroup 三層巢狀 XML 序列化應正確還原結構")]
        public void FilterGroup_DeepNested_XmlRoundtrip_PreservesStructure()
        {
            // Arrange
            var root = FilterGroup.All(
                FilterCondition.Equal("A", 1),
                FilterGroup.Any(
                    FilterCondition.Equal("B", 2),
                    FilterGroup.All(
                        FilterCondition.Equal("C", 3),
                        FilterCondition.Equal("D", 4)
                    )
                )
            );

            // Act
            var xml = XmlCodec.Serialize(root);
            var restored = XmlCodec.Deserialize<FilterGroup>(xml);

            // Assert
            Assert.NotNull(restored);
            Assert.Equal(LogicalOperator.And, restored!.Operator);
            Assert.Equal(2, restored.Nodes.Count);
            Assert.IsType<FilterCondition>(restored.Nodes[0]);
            Assert.IsType<FilterGroup>(restored.Nodes[1]);
            var inner = (FilterGroup)restored.Nodes[1];
            Assert.Equal(LogicalOperator.Or, inner.Operator);
            Assert.IsType<FilterGroup>(inner.Nodes[1]);
        }

        [Fact]
        [DisplayName("FilterCondition ToString Between 應包含 BETWEEN ... AND ...")]
        public void FilterCondition_ToString_Between_FormatsBothValues()
        {
            // Arrange
            var cond = FilterCondition.Between("Age", 10, 20);

            // Act
            var text = cond.ToString();

            // Assert
            Assert.Contains("BETWEEN", text);
            Assert.Contains("AND", text);
            Assert.Contains("10", text);
            Assert.Contains("20", text);
        }

        [Theory]
        [InlineData(ComparisonOperator.Equal, "=")]
        [InlineData(ComparisonOperator.NotEqual, "<>")]
        [InlineData(ComparisonOperator.GreaterThan, ">")]
        [InlineData(ComparisonOperator.GreaterThanOrEqual, ">=")]
        [InlineData(ComparisonOperator.LessThan, "<")]
        [InlineData(ComparisonOperator.LessThanOrEqual, "<=")]
        [InlineData(ComparisonOperator.Like, "LIKE")]
        [InlineData(ComparisonOperator.In, "IN")]
        [DisplayName("FilterCondition ToString 應依運算子輸出對應符號")]
        public void FilterCondition_ToString_ReturnsExpectedOperatorSymbol(ComparisonOperator op, string expectedSymbol)
        {
            // Arrange
            object value = op == ComparisonOperator.In ? (object)new object[] { 1, 2 } : 5;
            var cond = new FilterCondition("F", op, value);

            // Act
            var text = cond.ToString();

            // Assert
            Assert.Contains(expectedSymbol, text);
        }

        [Fact]
        [DisplayName("FilterCondition ToString Contains 應包含 %value%")]
        public void FilterCondition_ToString_Contains_WrapsValueInPercent()
        {
            // Arrange
            var cond = FilterCondition.Contains("Name", "Lee");

            // Act
            var text = cond.ToString();

            // Assert
            Assert.Contains("'%Lee%'", text);
        }

        [Fact]
        [DisplayName("FilterCondition ToString StartsWith 應以 'value%' 結尾")]
        public void FilterCondition_ToString_StartsWith_EndsWithPercent()
        {
            // Arrange
            var cond = FilterCondition.StartsWith("Name", "Lee");

            // Act
            var text = cond.ToString();

            // Assert
            Assert.Contains("'Lee%'", text);
        }

        [Fact]
        [DisplayName("FilterCondition ToString EndsWith 應以 '%value' 開頭")]
        public void FilterCondition_ToString_EndsWith_StartsWithPercent()
        {
            // Arrange
            var cond = FilterCondition.EndsWith("Name", "Lee");

            // Act
            var text = cond.ToString();

            // Assert
            Assert.Contains("'%Lee'", text);
        }
    }
}
