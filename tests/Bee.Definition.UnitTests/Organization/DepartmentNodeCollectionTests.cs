using System.ComponentModel;
using Bee.Definition.Organization;

namespace Bee.Definition.UnitTests.Organization
{
    public class DepartmentNodeCollectionTests
    {
        private static DepartmentNode CreateNode(string id)
            => new DepartmentNode(Guid.NewGuid(), id, id, Guid.Empty);

        [Fact]
        [DisplayName("AddRange 傳入 null 應直接返回，不拋例外")]
        public void AddRange_NullInput_DoesNotThrow()
        {
            var collection = new DepartmentNodeCollection();
            var exception = Record.Exception(() => collection.AddRange(null!));
            Assert.Null(exception);
        }

        [Fact]
        [DisplayName("AddRange 傳入空集合應不新增任何節點")]
        public void AddRange_EmptyCollection_DoesNotAddAny()
        {
            var collection = new DepartmentNodeCollection();
            collection.AddRange(Array.Empty<DepartmentNode>());
            Assert.Empty(collection);
        }

        [Fact]
        [DisplayName("AddRange 集合含 null 元素應略過 null，只新增有效節點")]
        public void AddRange_CollectionWithNullElements_SkipsNulls()
        {
            var collection = new DepartmentNodeCollection();
            var node = CreateNode("DEPT01");
            var nodes = new DepartmentNode[] { node, null! };
            collection.AddRange(nodes);
            Assert.Single(collection);
        }

        [Fact]
        [DisplayName("AddRange 傳入有效節點集合應全部新增至 collection")]
        public void AddRange_ValidNodes_AddsAll()
        {
            var collection = new DepartmentNodeCollection();
            var nodes = new[] { CreateNode("A"), CreateNode("B"), CreateNode("C") };
            collection.AddRange(nodes);
            Assert.Equal(3, collection.Count);
        }
    }
}
