using System.ComponentModel;
using Bee.Api.Core.MessagePack;
using Bee.Base.Serialization;
using Bee.Definition.Sorting;

namespace Bee.Api.Core.UnitTests
{
    /// <summary>
    /// SPIKE（階段 0 第 1 項）：驗證 <c>BeeObjectFormatter</c> 原型。
    /// 通過條件：以屬性名為鍵 round-trip，且 <c>[WireIgnore]</c> 成員確實不上 wire。
    /// </summary>
    public class SpikeBeeObjectFormatterTests
    {
        [Fact]
        [DisplayName("SPIKE: BeeObjectFormatter 應以屬性名為鍵 round-trip")]
        public void BeeObjectFormatter_RoundTrips()
        {
            var source = new SortField("cust_id", SortDirection.Desc);

            var bytes = MessagePackCodec.Serialize(source);
            var result = MessagePackCodec.Deserialize<SortField>(bytes);

            Assert.Equal("cust_id", result.FieldName);
            Assert.Equal(SortDirection.Desc, result.Direction);
        }

        [Fact]
        [DisplayName("SPIKE: [WireIgnore] 成員不得上 wire")]
        public void BeeObjectFormatter_WireIgnoredMembers_AreExcluded()
        {
            var source = new SortField("cust_id", SortDirection.Asc) { Tag = "should-not-travel" };
            source.SetSerializeState(SerializeState.Serialize);

            var bytes = MessagePackCodec.Serialize(source);
            var result = MessagePackCodec.Deserialize<SortField>(bytes);

            Assert.Null(result.Tag);
            Assert.Equal(SerializeState.None, result.SerializeState);
            Assert.Null(result.Collection);
        }

        [Fact]
        [DisplayName("SPIKE: wire 應為 2 個成員的 map（FieldName / Direction）")]
        public void BeeObjectFormatter_WritesExactlyTheWireMembers()
        {
            var bytes = MessagePackCodec.Serialize(new SortField("a", SortDirection.Asc));

            // fixmap 首位元組為 0x80|n；成員數須與 formatter 宣告的一致。
            Assert.Equal(0x80 | SortFieldFormatter.WireMemberCount, bytes[0]);
        }

        [Fact]
        [DisplayName("SPIKE: 集合內的元素亦應走 BeeObjectFormatter")]
        public void BeeObjectFormatter_AppliesToCollectionElements()
        {
            var source = new SortFieldCollection
            {
                new SortField("a", SortDirection.Asc) { Tag = "x" },
                new SortField("b", SortDirection.Desc),
            };

            var bytes = MessagePackCodec.Serialize(source);
            var result = MessagePackCodec.Deserialize<SortFieldCollection>(bytes);

            Assert.Equal(2, result.Count);
            Assert.Equal("a", result[0].FieldName);
            Assert.Null(result[0].Tag);
            Assert.Equal(SortDirection.Desc, result[1].Direction);
        }
    }
}
