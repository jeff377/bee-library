using System.ComponentModel;
using Bee.Api.Core.Transformers;
using Bee.Definition.Filters;
using Bee.Definition.Sorting;

namespace Bee.Api.Core.UnitTests.Filters
{
    public class FilterNodeMessagePackSpike
    {
        [Fact]
        [DisplayName("SPIKE：FilterNode MessagePack round-trip 含巢狀 group 應保留型別與值")]
        public void RoundTrip_NestedGroup_PreservesUnionAndValues()
        {
            FilterNode original = FilterGroup.All(
                FilterCondition.Equal("sys_id", "E001"),
                FilterCondition.Contains("sys_name", "張"),
                FilterGroup.Any(
                    FilterCondition.StartsWith("ref_dept_id", "D"),
                    FilterCondition.Between("salary", 30000, 80000)
                )
            );

            var serializer = new MessagePackPayloadSerializer();
            byte[] bytes = serializer.Serialize(original, typeof(FilterNode));
            var restored = (FilterNode)serializer.Deserialize(bytes, typeof(FilterNode))!;

            var rg = Assert.IsType<FilterGroup>(restored);
            Assert.Equal(LogicalOperator.And, rg.Operator);
            Assert.Equal(3, rg.Nodes.Count);

            var c0 = Assert.IsType<FilterCondition>(rg.Nodes[0]);
            Assert.Equal("sys_id", c0.FieldName);
            Assert.Equal(ComparisonOperator.Equal, c0.Operator);
            Assert.Equal("E001", c0.Value);

            var c1 = Assert.IsType<FilterCondition>(rg.Nodes[1]);
            Assert.Equal(ComparisonOperator.Contains, c1.Operator);
            Assert.Equal("張", c1.Value);

            var inner = Assert.IsType<FilterGroup>(rg.Nodes[2]);
            Assert.Equal(LogicalOperator.Or, inner.Operator);
            Assert.Equal(2, inner.Nodes.Count);

            var c2 = Assert.IsType<FilterCondition>(inner.Nodes[0]);
            Assert.Equal(ComparisonOperator.StartsWith, c2.Operator);
            Assert.Equal("D", c2.Value);

            var c3 = Assert.IsType<FilterCondition>(inner.Nodes[1]);
            Assert.Equal(ComparisonOperator.Between, c3.Operator);
            Assert.Equal(30000, c3.Value);
            Assert.Equal(80000, c3.SecondValue);
        }

        [Fact]
        [DisplayName("SPIKE：FilterCondition.In MessagePack round-trip 應保留清單值")]
        public void RoundTrip_InCondition_PreservesValues()
        {
            FilterNode original = FilterCondition.In("sys_id", new object[] { "E001", "E002", "E003" });

            var serializer = new MessagePackPayloadSerializer();
            byte[] bytes = serializer.Serialize(original, typeof(FilterNode));
            var restored = (FilterCondition)serializer.Deserialize(bytes, typeof(FilterNode))!;

            Assert.Equal("sys_id", restored.FieldName);
            Assert.Equal(ComparisonOperator.In, restored.Operator);
            var values = Assert.IsAssignableFrom<IEnumerable<object>>(restored.Value).ToList();
            Assert.Equal(new object[] { "E001", "E002", "E003" }, values);
        }

        [Fact]
        [DisplayName("SPIKE：SortFieldCollection MessagePack round-trip 應保留欄位與方向")]
        public void RoundTrip_SortFieldCollection_PreservesValues()
        {
            var original = new SortFieldCollection
            {
                new SortField("sys_id", SortDirection.Asc),
                new SortField("ref_dept_name", SortDirection.Desc),
            };

            var serializer = new MessagePackPayloadSerializer();
            byte[] bytes = serializer.Serialize(original, typeof(SortFieldCollection));
            var restored = (SortFieldCollection)serializer.Deserialize(bytes, typeof(SortFieldCollection))!;

            Assert.Equal(2, restored.Count);
            Assert.Equal("sys_id", restored[0].FieldName);
            Assert.Equal(SortDirection.Asc, restored[0].Direction);
            Assert.Equal("ref_dept_name", restored[1].FieldName);
            Assert.Equal(SortDirection.Desc, restored[1].Direction);
        }
    }
}
