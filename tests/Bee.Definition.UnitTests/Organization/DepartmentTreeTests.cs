using System.ComponentModel;
using System.Text.Json;
using Bee.Base.Serialization;
using Bee.Definition.Organization;

namespace Bee.Definition.UnitTests.Organization
{
    /// <summary>
    /// DepartmentTree 的查詢邏輯（子樹 / 祖先 / 防環）與三棲序列化（XML / JSON；MessagePack 因
    /// codec 在 Bee.Api.Core，於該層測）的測試。以合成節點驗證、不綁 DB。
    /// </summary>
    public class DepartmentTreeTests
    {
        // 樹：總公司(root) → 業務部 → 業務一課；管理部(root，獨立)
        private static DepartmentTree Build(out Guid hq, out Guid sales, out Guid sales1, out Guid admin)
        {
            hq = Guid.NewGuid();
            sales = Guid.NewGuid();
            sales1 = Guid.NewGuid();
            admin = Guid.NewGuid();
            var nodes = new List<DepartmentNode>
            {
                new(hq, "HQ", "總公司", Guid.Empty, Guid.Empty),
                new(sales, "SALES", "業務部", hq, Guid.Empty),
                new(sales1, "SALES1", "業務一課", sales, Guid.Empty),
                new(admin, "ADMIN", "管理部", Guid.Empty, Guid.Empty),
            };
            return new DepartmentTree("C001", nodes);
        }

        [Fact]
        [DisplayName("GetSelfAndDescendants 根節點回傳整棵子樹")]
        public void GetSelfAndDescendants_Root_ReturnsWholeSubtree()
        {
            var tree = Build(out var hq, out var sales, out var sales1, out _);

            var set = tree.GetSelfAndDescendants(hq);

            Assert.Equal(3, set.Count);
            Assert.Contains(hq, set);
            Assert.Contains(sales, set);
            Assert.Contains(sales1, set);
        }

        [Fact]
        [DisplayName("GetSelfAndDescendants 中間節點回傳自身 + 後代")]
        public void GetSelfAndDescendants_Mid_ReturnsSelfAndDescendants()
        {
            var tree = Build(out var hq, out var sales, out var sales1, out _);

            var set = tree.GetSelfAndDescendants(sales);

            Assert.Equal(2, set.Count);
            Assert.Contains(sales, set);
            Assert.Contains(sales1, set);
            Assert.DoesNotContain(hq, set);
        }

        [Fact]
        [DisplayName("GetSelfAndDescendants 葉節點只回傳自身")]
        public void GetSelfAndDescendants_Leaf_ReturnsSelf()
        {
            var tree = Build(out _, out _, out var sales1, out _);

            var set = tree.GetSelfAndDescendants(sales1);

            Assert.Single(set);
            Assert.Contains(sales1, set);
        }

        [Fact]
        [DisplayName("GetSelfAndDescendants 未知節點回傳空")]
        public void GetSelfAndDescendants_Unknown_ReturnsEmpty()
        {
            var tree = Build(out _, out _, out _, out _);

            Assert.Empty(tree.GetSelfAndDescendants(Guid.NewGuid()));
        }

        [Fact]
        [DisplayName("GetSelfAndAncestors 葉節點回傳到根的鏈")]
        public void GetSelfAndAncestors_Leaf_ReturnsChainToRoot()
        {
            var tree = Build(out var hq, out var sales, out var sales1, out _);

            var chain = tree.GetSelfAndAncestors(sales1);

            Assert.Equal(3, chain.Count);
            Assert.Contains(sales1, chain);
            Assert.Contains(sales, chain);
            Assert.Contains(hq, chain);
        }

        [Fact]
        [DisplayName("Contains / GetNode / Roots 正確")]
        public void ContainsNodeRoots_Correct()
        {
            var tree = Build(out var hq, out _, out var sales1, out var admin);

            Assert.True(tree.Contains(hq));
            Assert.False(tree.Contains(Guid.NewGuid()));
            Assert.Equal("業務一課", tree.GetNode(sales1)!.DeptName);
            Assert.Null(tree.GetNode(Guid.NewGuid()));
            // 兩個 root：總公司、管理部
            Assert.Equal(2, tree.Roots.Count);
            Assert.Contains(tree.Roots, n => n.RowId == hq);
            Assert.Contains(tree.Roots, n => n.RowId == admin);
        }

        [Fact]
        [DisplayName("父子互指的環不應造成無限遞迴")]
        public void GetSelfAndDescendants_Cycle_DoesNotLoopForever()
        {
            var a = Guid.NewGuid();
            var b = Guid.NewGuid();
            var tree = new DepartmentTree("C001",
            [
                new DepartmentNode(a, "A", "A", b, Guid.Empty),  // A 的上級是 B
                new DepartmentNode(b, "B", "B", a, Guid.Empty),  // B 的上級是 A（環）
            ]);

            var ex = Record.Exception(() =>
            {
                _ = tree.GetSelfAndDescendants(a);
                _ = tree.GetSelfAndAncestors(a);
            });

            Assert.Null(ex);
        }

        [Fact]
        [DisplayName("XML round-trip 還原後查詢一致（index 重建）")]
        public void XmlRoundTrip_PreservesNodesAndQueries()
        {
            var tree = Build(out var hq, out _, out _, out _);

            var xml = XmlCodec.Serialize(tree);
            var restored = XmlCodec.Deserialize<DepartmentTree>(xml)!;

            Assert.Equal("C001", restored.CompanyId);
            Assert.Equal(4, restored.Nodes!.Count);
            Assert.Equal(3, restored.GetSelfAndDescendants(hq).Count);
            Assert.Equal(2, restored.Roots.Count);
        }

        [Fact]
        [DisplayName("JSON round-trip 還原後查詢一致（index 重建）")]
        public void JsonRoundTrip_PreservesNodesAndQueries()
        {
            var tree = Build(out var hq, out _, out _, out _);

            var json = JsonSerializer.Serialize(tree);
            var restored = JsonSerializer.Deserialize<DepartmentTree>(json)!;

            Assert.Equal("C001", restored.CompanyId);
            Assert.Equal(4, restored.Nodes!.Count);
            Assert.Equal(3, restored.GetSelfAndDescendants(hq).Count);
        }
    }
}
