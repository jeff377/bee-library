using System.ComponentModel;
using Bee.Api.Core.MessagePack;
using Bee.Api.Core.Messages.System;
using Bee.Definition.Identity;
using Bee.Definition.Settings;

namespace Bee.Api.Core.UnitTests.System
{
    /// <summary>
    /// EnterCompanyRequest / EnterCompanyResponse 經 <see cref="MessagePackCodec"/> 的
    /// wire 層 round-trip 序列化驗證，重點：CompanyInfo 巢狀物件在框架的 composite
    /// resolver 下能正確還原四個 [Key] 欄位。
    /// </summary>
    public class EnterCompanyMessagePackTests
    {
        [Fact]
        [DisplayName("EnterCompanyRequest 帶 CompanyId 應 round-trip 還原")]
        public void EnterCompanyRequest_RoundTrip_PreservesCompanyId()
        {
            var request = new EnterCompanyRequest { CompanyId = "C001" };

            var bytes = MessagePackCodec.Serialize(request);
            var restored = MessagePackCodec.Deserialize<EnterCompanyRequest>(bytes);

            Assert.NotNull(restored);
            Assert.Equal("C001", restored!.CompanyId);
        }

        [Fact]
        [DisplayName("EnterCompanyRequest 預設 CompanyId 應 round-trip 為空字串")]
        public void EnterCompanyRequest_DefaultValue_RoundTrip()
        {
            var request = new EnterCompanyRequest();

            var bytes = MessagePackCodec.Serialize(request);
            var restored = MessagePackCodec.Deserialize<EnterCompanyRequest>(bytes);

            Assert.NotNull(restored);
            Assert.Equal(string.Empty, restored!.CompanyId);
        }

        [Fact]
        [DisplayName("EnterCompanyResponse.Company 應 round-trip 保留 CompanyInfo 四欄位")]
        public void EnterCompanyResponse_RoundTrip_PreservesCompanyInfo()
        {
            var response = new EnterCompanyResponse
            {
                Company = new CompanyInfo
                {
                    CompanyId = "C001",
                    CompanyName = "Acme",
                    CompanyDatabaseId = "biz_shared_01"
                }
            };

            var bytes = MessagePackCodec.Serialize(response);
            var restored = MessagePackCodec.Deserialize<EnterCompanyResponse>(bytes);

            Assert.NotNull(restored);
            Assert.Equal("C001", restored!.Company.CompanyId);
            Assert.Equal("Acme", restored.Company.CompanyName);
            Assert.Equal("biz_shared_01", restored.Company.CompanyDatabaseId);
        }

        [Fact]
        [DisplayName("EnterCompanyResponse.Capabilities 應 round-trip 保留每個 model 的 action mask")]
        public void EnterCompanyResponse_RoundTrip_PreservesCapabilities()
        {
            var response = new EnterCompanyResponse
            {
                Company = new CompanyInfo { CompanyId = "C001" },
                Capabilities = new Dictionary<string, PermissionAction>
                {
                    ["PurchaseOrder"] = PermissionAction.Read | PermissionAction.Update,
                    ["Cost"] = PermissionAction.Read,
                }
            };

            var bytes = MessagePackCodec.Serialize(response);
            var restored = MessagePackCodec.Deserialize<EnterCompanyResponse>(bytes);

            Assert.NotNull(restored);
            Assert.Equal(PermissionAction.Read | PermissionAction.Update, restored!.Capabilities["PurchaseOrder"]);
            Assert.Equal(PermissionAction.Read, restored.Capabilities["Cost"]);
        }

        [Fact]
        [DisplayName("EnterCompanyResponse 預設 Capabilities 應 round-trip 為空字典")]
        public void EnterCompanyResponse_DefaultCapabilities_RoundTripEmpty()
        {
            var response = new EnterCompanyResponse { Company = new CompanyInfo { CompanyId = "C001" } };

            var bytes = MessagePackCodec.Serialize(response);
            var restored = MessagePackCodec.Deserialize<EnterCompanyResponse>(bytes);

            Assert.NotNull(restored);
            Assert.Empty(restored!.Capabilities);
        }
    }
}
