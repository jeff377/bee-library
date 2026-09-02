using System.ComponentModel;
using System.Text.Json.Serialization;
using Bee.Base.Serialization;
using Bee.Definition.Filters;

namespace Bee.Definition.UnitTests.Filters
{
    /// <summary>
    /// 守住「宣告型別為 <see cref="FilterNode"/> 的成員」在 JSON 上的多型。
    /// </summary>
    /// <remarks>
    /// System.Text.Json 綁的是宣告型別：把 <see cref="FilterGroup"/> 指派給
    /// <see cref="FilterNode"/> 屬性後，少了 converter 就只會寫出 <c>{"kind":"Group"}</c>，
    /// 運算子與整棵子樹**靜默消失、不擲例外**。
    /// <para>
    /// 這個洞長期存在卻沒浮現，因為編碼過的 body 一直只走 MessagePack（那端有自己的
    /// filter node formatter）。JSON body codec 上線後它就在最常用的清單查詢上生效了。
    /// </para>
    /// </remarks>
    public class FilterNodeJsonConverterTests
    {
        /// <summary>
        /// 承載一個宣告型別為 <see cref="FilterNode"/> 的成員——正是會出問題的形狀。
        /// 標註方式與 wire 上的實際持有者一致（屬性層級，不是型別層級）。
        /// </summary>
        private sealed class FilterHolder
        {
            [JsonConverter(typeof(FilterNodeJsonConverter))]
            public FilterNode? Filter { get; set; }
        }

        private static FilterGroup BuildGroup()
        {
            var group = new FilterGroup(LogicalOperator.Or);
            group.Nodes.Add(new FilterCondition("amount", ComparisonOperator.GreaterThan, 100m));
            group.Nodes.Add(new FilterCondition("name", ComparisonOperator.Like, "A%"));
            return group;
        }

        [Fact]
        [DisplayName("宣告型別為 FilterNode 的成員應寫出完整子樹，而非只有判別碼")]
        public void Serialize_FilterNodeMember_KeepsSubtree()
        {
            var json = JsonCodec.Serialize(new FilterHolder { Filter = BuildGroup() });

            Assert.Contains("\"nodes\"", json, StringComparison.Ordinal);
            Assert.Contains("amount", json, StringComparison.Ordinal);
            Assert.Contains("\"operator\":\"Or\"", json, StringComparison.Ordinal);
        }

        [Fact]
        [DisplayName("宣告型別為 FilterNode 的成員應還原為原本的具體型別與內容")]
        public void RoundTrip_FilterNodeMember_RestoresConcreteType()
        {
            var json = JsonCodec.Serialize(new FilterHolder { Filter = BuildGroup() });

            var restored = JsonCodec.Deserialize<FilterHolder>(json);

            var group = Assert.IsType<FilterGroup>(restored!.Filter);
            Assert.Equal(LogicalOperator.Or, group.Operator);
            Assert.Equal(2, group.Nodes.Count);
            var first = Assert.IsType<FilterCondition>(group.Nodes[0]);
            Assert.Equal("amount", first.FieldName);
            Assert.Equal(ComparisonOperator.GreaterThan, first.Operator);
        }

        [Fact]
        [DisplayName("單一條件指派給 FilterNode 成員時應還原為 FilterCondition")]
        public void RoundTrip_ConditionAsNode_RestoresCondition()
        {
            var holder = new FilterHolder
            {
                Filter = new FilterCondition("sys_id", ComparisonOperator.Equal, "E001")
            };

            var restored = JsonCodec.Deserialize<FilterHolder>(JsonCodec.Serialize(holder));

            var condition = Assert.IsType<FilterCondition>(restored!.Filter);
            Assert.Equal("sys_id", condition.FieldName);
            Assert.Equal(ComparisonOperator.Equal, condition.Operator);
        }

        [Fact]
        [DisplayName("FilterNode 成員為 null 時應原樣還原為 null")]
        public void RoundTrip_NullNode_StaysNull()
        {
            var restored = JsonCodec.Deserialize<FilterHolder>(
                JsonCodec.Serialize(new FilterHolder { Filter = null }));

            Assert.Null(restored!.Filter);
        }
    }
}
