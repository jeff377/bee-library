using System.ComponentModel;
using Bee.Api.Core.MessagePack;

namespace Bee.Api.Core.UnitTests
{
    /// <summary>
    /// SerializableDataRelation 測試。
    /// </summary>
    public class SerializableDataRelationTests
    {
        [Fact]
        [DisplayName("預設建構子應初始化空集合與空字串")]
        public void DefaultConstructor_InitializesEmpty()
        {
            var relation = new SerializableDataRelation();

            Assert.Equal(string.Empty, relation.RelationName);
            Assert.Equal(string.Empty, relation.ParentTable);
            Assert.Equal(string.Empty, relation.ChildTable);
            Assert.NotNull(relation.ParentColumns);
            Assert.Empty(relation.ParentColumns);
            Assert.NotNull(relation.ChildColumns);
            Assert.Empty(relation.ChildColumns);
        }

        [Fact]
        [DisplayName("屬性應可被設定並讀回")]
        public void Properties_AreSettable()
        {
            var relation = new SerializableDataRelation
            {
                RelationName = "FK_Orders_Customers",
                ParentTable = "Customers",
                ChildTable = "Orders",
            };
            relation.ParentColumns.Add("CustomerId");
            relation.ChildColumns.Add("CustomerId");

            Assert.Equal("FK_Orders_Customers", relation.RelationName);
            Assert.Equal("Customers", relation.ParentTable);
            Assert.Equal("Orders", relation.ChildTable);
            Assert.Single(relation.ParentColumns);
            Assert.Single(relation.ChildColumns);
        }
    }
}
