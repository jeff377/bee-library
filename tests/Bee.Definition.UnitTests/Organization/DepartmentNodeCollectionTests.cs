using System.ComponentModel;
using Bee.Definition.Organization;

namespace Bee.Definition.UnitTests.Organization
{
    /// <summary>
    /// <see cref="DepartmentNodeCollection.AddRange"/> 行為的單元測試：
    /// null 輸入不拋例外、有效節點全部加入、集合中的 null 項目自動略過。
    /// </summary>
    public class DepartmentNodeCollectionTests
    {
        private static DepartmentNode NewNode(string deptId) =>
            new DepartmentNode(Guid.NewGuid(), deptId, deptId, Guid.Empty, Guid.Empty);

        [Fact]
        [DisplayName("AddRange null 輸入不應拋例外（直接回傳）")]
        public void AddRange_NullInput_DoesNotThrow()
        {
            var collection = new DepartmentNodeCollection();
            var exception = Record.Exception(() => collection.AddRange(null!));
            Assert.Null(exception);
        }

        [Fact]
        [DisplayName("AddRange 傳入有效節點後集合應包含所有節點")]
        public void AddRange_ValidNodes_AddsAll()
        {
            var collection = new DepartmentNodeCollection();
            var nodes = new[] { NewNode("D1"), NewNode("D2"), NewNode("D3") };

            collection.AddRange(nodes);

            Assert.Equal(3, collection.Count);
        }

        [Fact]
        [DisplayName("AddRange 集合中含 null 項目時應略過 null，只加入非 null 節點")]
        public void AddRange_CollectionWithNullItems_SkipsNulls()
        {
            var collection = new DepartmentNodeCollection();
            var nodes = new List<DepartmentNode> { NewNode("D1"), NewNode("D2") };
            nodes.Insert(1, null!);   // 刻意插入 null 以測試過濾邏輯

            collection.AddRange(nodes);

            Assert.Equal(2, collection.Count);
        }

        [Fact]
        [DisplayName("AddRange 空集合不應拋例外且集合應保持空")]
        public void AddRange_EmptyInput_CollectionRemainsEmpty()
        {
            var collection = new DepartmentNodeCollection();
            var exception = Record.Exception(() => collection.AddRange([]));
            Assert.Null(exception);
            Assert.Empty(collection);
        }
    }
}
