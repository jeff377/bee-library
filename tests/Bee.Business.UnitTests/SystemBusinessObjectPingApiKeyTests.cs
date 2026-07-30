using System.ComponentModel;
using Bee.Business.System;
using Bee.Definition.Security;
using Bee.Tests.Shared;

namespace Bee.Business.UnitTests
{
    /// <summary>
    /// <see cref="SystemBusinessObject.Ping"/> 對 API 金鑰狀態的回報，以及「金鑰有效才回版本號」
    /// 的收斂（免金鑰的 ping 不應對全網公開框架版本）。
    /// </summary>
    public class SystemBusinessObjectPingApiKeyTests : IClassFixture<SharedDbFixture>
    {
        private readonly SharedDbFixture _fx;

        public SystemBusinessObjectPingApiKeyTests(SharedDbFixture fx) { _fx = fx; }

        private PingResult PingWith(ApiKeyValidationResult validation)
        {
            var bo = new SystemBusinessObject(TestBeeContext.Create(_fx), Guid.Empty)
            {
                ApiKeyValidation = validation,
            };
            return bo.Ping(new PingArgs { TraceId = "T-1", ClientName = "unit" });
        }

        [Fact]
        [DisplayName("Ping 未經金鑰閘門(行程內呼叫)應回 NotChecked 並帶版本號")]
        public void Ping_NotChecked_ReportsStatusAndVersion()
        {
            var result = PingWith(ApiKeyValidationResult.NotChecked);

            Assert.Equal("ok", result.Status);
            Assert.Equal(ApiKeyStatus.NotChecked, result.ApiKeyStatus);
            Assert.False(string.IsNullOrEmpty(result.Version));
        }

        [Fact]
        [DisplayName("Ping 於尚未發放金鑰的部署應回 NotConfigured 並仍帶版本號")]
        public void Ping_NotConfigured_ReportsStatusAndVersion()
        {
            var result = PingWith(new ApiKeyValidationResult(ApiKeyStatus.NotConfigured));

            Assert.Equal("ok", result.Status);
            Assert.Equal(ApiKeyStatus.NotConfigured, result.ApiKeyStatus);
            Assert.False(string.IsNullOrEmpty(result.Version));
        }

        [Fact]
        [DisplayName("Ping 於金鑰有效時應回 Valid 並帶版本號")]
        public void Ping_ValidKey_ReportsStatusAndVersion()
        {
            var result = PingWith(new ApiKeyValidationResult(ApiKeyStatus.Valid, "app", "App"));

            Assert.Equal(ApiKeyStatus.Valid, result.ApiKeyStatus);
            Assert.False(string.IsNullOrEmpty(result.Version));
        }

        [Fact]
        [DisplayName("Ping 未帶金鑰(嚴格態)仍回 ok，但不含版本號")]
        public void Ping_NotProvided_ReturnsOkWithoutVersion()
        {
            var result = PingWith(new ApiKeyValidationResult(ApiKeyStatus.NotProvided));

            Assert.Equal("ok", result.Status);
            Assert.Equal(ApiKeyStatus.NotProvided, result.ApiKeyStatus);
            Assert.Null(result.Version);
        }

        [Fact]
        [DisplayName("Ping 帶無效金鑰應回報 Invalid，且不含版本號")]
        public void Ping_InvalidKey_ReportsInvalidWithoutVersion()
        {
            var result = PingWith(new ApiKeyValidationResult(ApiKeyStatus.Invalid, "app", string.Empty));

            Assert.Equal("ok", result.Status);
            Assert.Equal(ApiKeyStatus.Invalid, result.ApiKeyStatus);
            Assert.Null(result.Version);
        }
    }
}
