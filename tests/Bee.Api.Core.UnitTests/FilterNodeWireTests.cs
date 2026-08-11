using System.ComponentModel;
using Bee.Api.Core.MessagePack;
using Bee.Definition.Filters;

namespace Bee.Api.Core.UnitTests
{
    /// <summary>
    /// <c>FilterNodeFormatter</c> 的多型 round-trip 測試。
    /// </summary>
    /// <remarks>
    /// 多型判別由 formatter 手寫，編譯器不會把型別與 formatter 綁在一起。漂移守衛是
    /// <c>WireContractDriftTests</c>，經由 <c>FilterConditionFormatter</c> /
    /// <c>FilterGroupFormatter</c> 上的 <c>IWireContract</c> 實作；這裡驗的是 round-trip 保真度。
    /// </remarks>
    public class FilterNodeWireTests
    {
        [Fact]
        [DisplayName("FilterCondition 應 round-trip 為正確子型別")]
        public void FilterCondition_RoundTripsAsCorrectSubtype()
        {
            FilterNode source = new FilterCondition("cust_id", ComparisonOperator.Equal, "A01")
            {
                IgnoreIfNull = true,
            };

            var bytes = MessagePackCodec.Serialize(source);
            var result = MessagePackCodec.Deserialize<FilterNode>(bytes);

            var condition = Assert.IsType<FilterCondition>(result);
            Assert.Equal(FilterNodeKind.Condition, condition.Kind);
            Assert.Equal("cust_id", condition.FieldName);
            Assert.Equal(ComparisonOperator.Equal, condition.Operator);
            Assert.Equal("A01", condition.Value);
            Assert.Null(condition.SecondValue);
            Assert.True(condition.IgnoreIfNull);
        }

        [Fact]
        [DisplayName("FilterGroup 應 round-trip 為正確子型別")]
        public void FilterGroup_RoundTripsAsCorrectSubtype()
        {
            FilterNode source = new FilterGroup(LogicalOperator.Or)
            {
                Nodes = [new FilterCondition("a", ComparisonOperator.GreaterThan, 1)],
            };

            var bytes = MessagePackCodec.Serialize(source);
            var result = MessagePackCodec.Deserialize<FilterNode>(bytes);

            var group = Assert.IsType<FilterGroup>(result);
            Assert.Equal(FilterNodeKind.Group, group.Kind);
            Assert.Equal(LogicalOperator.Or, group.Operator);
            Assert.Single(group.Nodes);
            Assert.IsType<FilterCondition>(group.Nodes[0]);
        }

        [Fact]
        [DisplayName("巢狀三層的過濾樹應完整 round-trip")]
        public void NestedFilterTree_RoundTrips()
        {
            FilterNode source = new FilterGroup(LogicalOperator.And)
            {
                Nodes =
                [
                    new FilterCondition("status", ComparisonOperator.Equal, "OPEN"),
                    new FilterGroup(LogicalOperator.Or)
                    {
                        Nodes =
                        [
                            new FilterCondition("amount", ComparisonOperator.GreaterThan, 1000m),
                            new FilterGroup(LogicalOperator.And)
                            {
                                Nodes = [new FilterCondition("region", ComparisonOperator.Equal, "TW")],
                            },
                        ],
                    },
                ],
            };

            var result = MessagePackCodec.Deserialize<FilterNode>(MessagePackCodec.Serialize(source));

            var root = Assert.IsType<FilterGroup>(result);
            Assert.Equal(2, root.Nodes.Count);
            var level2 = Assert.IsType<FilterGroup>(root.Nodes[1]);
            Assert.Equal(LogicalOperator.Or, level2.Operator);
            var level3 = Assert.IsType<FilterGroup>(level2.Nodes[1]);
            var leaf = Assert.IsType<FilterCondition>(level3.Nodes[0]);
            Assert.Equal("region", leaf.FieldName);
            Assert.Equal("TW", leaf.Value);
        }

        [Theory]
        [InlineData("text")]
        [InlineData(42)]
        [InlineData(true)]
        [DisplayName("FilterCondition.Value 各型別皆應 round-trip")]
        public void ConditionValue_VariousTypes_RoundTrip(object value)
        {
            FilterNode source = new FilterCondition("f", ComparisonOperator.Equal, value);

            var result = MessagePackCodec.Deserialize<FilterNode>(MessagePackCodec.Serialize(source));

            Assert.Equal(value, Assert.IsType<FilterCondition>(result).Value);
        }

        [Fact]
        [DisplayName("FilterCondition.Value 為 Guid / DateTime / decimal 亦應 round-trip")]
        public void ConditionValue_FrameworkTypes_RoundTrip()
        {
            var guid = Guid.NewGuid();
            var moment = new DateTime(2026, 8, 9, 13, 45, 0, DateTimeKind.Utc);

            FilterNode source = new FilterGroup(LogicalOperator.And)
            {
                Nodes =
                [
                    new FilterCondition("g", ComparisonOperator.Equal, guid),
                    new FilterCondition("d", ComparisonOperator.Equal, moment),
                    new FilterCondition("m", ComparisonOperator.Equal, 12.34m),
                ],
            };

            var result = Assert.IsType<FilterGroup>(
                MessagePackCodec.Deserialize<FilterNode>(MessagePackCodec.Serialize(source)));

            Assert.Equal(guid, ((FilterCondition)result.Nodes[0]).Value);
            Assert.Equal(moment, ((FilterCondition)result.Nodes[1]).Value);
            Assert.Equal(12.34m, ((FilterCondition)result.Nodes[2]).Value);
        }

        [Fact]
        [DisplayName("Between 條件的 SecondValue 應 round-trip")]
        public void ConditionSecondValue_RoundTrips()
        {
            FilterNode source = new FilterCondition("amount", ComparisonOperator.Between, 100, 200);

            var result = Assert.IsType<FilterCondition>(
                MessagePackCodec.Deserialize<FilterNode>(MessagePackCodec.Serialize(source)));

            Assert.Equal(100, result.Value);
            Assert.Equal(200, result.SecondValue);
        }

        [Fact]
        [DisplayName("FilterNodeCollection 內的元素應保留各自的子型別")]
        public void FilterNodeCollection_PreservesElementSubtypes()
        {
            var source = new FilterNodeCollection
            {
                new FilterCondition("a", ComparisonOperator.Equal, 1),
                new FilterGroup(LogicalOperator.Or),
            };

            var result = MessagePackCodec.Deserialize<FilterNodeCollection>(
                MessagePackCodec.Serialize(source));

            Assert.Equal(2, result.Count);
            Assert.IsType<FilterCondition>(result[0]);
            Assert.IsType<FilterGroup>(result[1]);
        }

        [Fact]
        [DisplayName("Kind 為 null 的過濾節點應 round-trip 為 null")]
        public void NullFilterNode_RoundTrips()
        {
            FilterNode? source = null;

            var result = MessagePackCodec.Deserialize<FilterNode?>(
                MessagePackCodec.Serialize(source));

            Assert.Null(result);
        }
    }
}
