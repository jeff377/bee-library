using System.ComponentModel;
using Bee.Api.Core.MessagePack;
using Bee.Definition.Organization;

namespace Bee.Api.Core.UnitTests.Organization
{
    /// <summary>
    /// DepartmentTree 的 MessagePack round-trip（第三棲；XML/JSON 在 Bee.Definition.UnitTests 測）。
    /// 驗證扁平節點跨 MessagePack 還原、查詢 index 重建。
    /// </summary>
    public class DepartmentTreeMessagePackTests
    {
        [Fact]
        [DisplayName("DepartmentTree MessagePack round-trip 還原後查詢一致")]
        public void DepartmentTree_MessagePack_RoundTrip()
        {
            var hq = Guid.NewGuid();
            var sales = Guid.NewGuid();
            var sales1 = Guid.NewGuid();
            var tree = new DepartmentTree("C001",
            [
                new DepartmentRow(hq, "HQ", "總公司", Guid.Empty, Guid.Empty),
                new DepartmentRow(sales, "SALES", "業務部", hq, Guid.Empty),
                new DepartmentRow(sales1, "SALES1", "業務一課", sales, Guid.Empty),
            ]);

            var bytes = MessagePackCodec.Serialize(tree);
            var restored = MessagePackCodec.Deserialize<DepartmentTree>(bytes)!;

            Assert.Equal("C001", restored.CompanyId);
            Assert.Single(restored.Roots!);                                   // 巢狀：單一 root（總公司）
            // index 在反序列化後 lazy 重建
            Assert.Equal(3, restored.GetSelfAndDescendants(hq).Count);
            Assert.Equal(2, restored.GetSelfAndDescendants(sales).Count);
            Assert.Equal(new[] { sales1, sales, hq }, restored.GetSelfAndAncestors(sales1)); // 巢狀父子鏈還原
        }

        [Fact]
        [DisplayName("空 DepartmentTree MessagePack round-trip 不應 NRE")]
        public void EmptyDepartmentTree_MessagePack_RoundTrip()
        {
            var tree = new DepartmentTree("C001", []);

            var bytes = MessagePackCodec.Serialize(tree);
            var restored = MessagePackCodec.Deserialize<DepartmentTree>(bytes)!;

            Assert.Equal("C001", restored.CompanyId);
            Assert.Empty(restored.Roots!);
        }
    }
}
