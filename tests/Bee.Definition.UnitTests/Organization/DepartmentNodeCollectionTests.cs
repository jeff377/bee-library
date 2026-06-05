using System.ComponentModel;
using Bee.Definition.Organization;

namespace Bee.Definition.UnitTests.Organization
{
    /// <summary>
    /// <see cref="DepartmentNodeCollection.AddRange"/> 各路徑的單元測試。
    /// 覆蓋 null 輸入防衛、空集合、以及有效節點加入等三個分支。
    /// </summary>
    public class DepartmentNodeCollectionTests
    {
        private static DepartmentNode MakeNode(string deptId)
            => new DepartmentNode(Guid.NewGuid(), deptId, deptId, Guid.Empty, Guid.Empty);

        [Fact]
        [DisplayName("AddRange null 輸入應直接回傳，不拋例外")]
        public void AddRange_NullInput_DoesNotThrow()
        {
            var collection = new DepartmentNodeCollection();
            IEnumerable<DepartmentNode> nullNodes = null!;
            var exception = Record.Exception(() => collection.AddRange(nullNodes));
            Assert.Null(exception);
        }

        [Fact]
        [DisplayName("AddRange null 輸入後集合應保持空")]
        public void AddRange_NullInput_CollectionRemainsEmpty()
        {
            var collection = new DepartmentNodeCollection();
            IEnumerable<DepartmentNode> nullNodes = null!;
            collection.AddRange(nullNodes);
            Assert.Empty(collection);
        }

        [Fact]
        [DisplayName("AddRange 有效節點清單應全部加入集合")]
        public void AddRange_ValidNodes_AddsAllNodes()
        {
            var collection = new DepartmentNodeCollection();
            var nodes = new List<DepartmentNode>
            {
                MakeNode("A"),
                MakeNode("B"),
                MakeNode("C")
            };

            collection.AddRange(nodes);

            Assert.Equal(3, collection.Count);
        }

        [Fact]
        [DisplayName("AddRange 空清單應保持集合不變")]
        public void AddRange_EmptyCollection_CollectionRemainsEmpty()
        {
            var collection = new DepartmentNodeCollection();
            collection.AddRange(Array.Empty<DepartmentNode>());
            Assert.Empty(collection);
        }
    }
}
