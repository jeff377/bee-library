using System.ComponentModel;
using Bee.Api.Core.MessagePack;
using Bee.Api.Core.Messages.System;
using Bee.Definition.Organization;

namespace Bee.Api.Core.UnitTests.System
{
    /// <summary>
    /// GetDepartmentTree wire DTO 的 MessagePack round-trip：含 ApiResponse base 的 Tree 物件
    /// （含集合）跨 wire 還原、null tree、空 request 邊界。
    /// </summary>
    public class GetDepartmentTreeMessagePackTests
    {
        [Fact]
        [DisplayName("GetDepartmentTreeResponse MessagePack round-trip 保留 Tree 與節點")]
        public void Response_RoundTrip_PreservesTree()
        {
            var hq = Guid.NewGuid();
            var sales = Guid.NewGuid();
            var response = new GetDepartmentTreeResponse
            {
                Tree = new DepartmentTree("C001",
                [
                    new DepartmentRow(hq, "HQ", "總公司", Guid.Empty, Guid.Empty),
                    new DepartmentRow(sales, "SALES", "業務部", hq, Guid.Empty),
                ]),
            };

            var bytes = MessagePackCodec.Serialize(response);
            var restored = MessagePackCodec.Deserialize<GetDepartmentTreeResponse>(bytes);

            Assert.NotNull(restored.Tree);
            Assert.Equal("C001", restored.Tree!.CompanyId);
            Assert.Single(restored.Tree.Roots!);                              // 巢狀：單一 root（總公司）
            Assert.Equal(2, restored.Tree.GetSelfAndDescendants(hq).Count);
        }

        [Fact]
        [DisplayName("GetDepartmentTreeResponse null Tree round-trip 不應 NRE")]
        public void Response_NullTree_RoundTrip()
        {
            var bytes = MessagePackCodec.Serialize(new GetDepartmentTreeResponse { Tree = null });
            var restored = MessagePackCodec.Deserialize<GetDepartmentTreeResponse>(bytes);

            Assert.Null(restored.Tree);
        }

        [Fact]
        [DisplayName("GetDepartmentTreeRequest（無參數）round-trip")]
        public void Request_RoundTrip()
        {
            var bytes = MessagePackCodec.Serialize(new GetDepartmentTreeRequest());
            var restored = MessagePackCodec.Deserialize<GetDepartmentTreeRequest>(bytes);

            Assert.NotNull(restored);
        }
    }
}
