using System.ComponentModel;
using Bee.Api.Core.Authorization;
using Bee.Api.Core.JsonRpc;
using Bee.Definition.Security;

namespace Bee.Api.Core.UnitTests.Authorization
{
    /// <summary>
    /// <see cref="ApiAuthorizationValidator"/> 對 API 金鑰驗證結果的決策：相容態沿用非空檢查、
    /// 嚴格態依驗證結果放行或拒絕、以及金鑰豁免清單只含 <c>System.Ping</c>。
    /// </summary>
    public class ApiKeyGateAuthorizationTests
    {
        private const string RejectedMessage = "Missing or invalid API key.";

        private static ApiAuthorizationContext NewContext(ApiKeyValidationResult validation,
            string apiKey = "some-key", string method = "Foo.Bar")
        {
            return new ApiAuthorizationContext
            {
                ApiKey = apiKey,
                Authorization = "Bearer " + Guid.NewGuid(),
                Method = method,
                ApiKeyValidation = validation,
            };
        }

        [Fact]
        [DisplayName("金鑰有效時應放行")]
        public void Validate_ValidKey_Succeeds()
        {
            var context = NewContext(new ApiKeyValidationResult(ApiKeyStatus.Valid, "app", "App"));

            var result = new ApiAuthorizationValidator().Validate(context);

            Assert.True(result.IsValid);
        }

        [Theory]
        [DisplayName("嚴格態下金鑰未帶或無效時應拒絕，且四種情境訊息一致")]
        [InlineData(ApiKeyStatus.NotProvided)]
        [InlineData(ApiKeyStatus.Invalid)]
        public void Validate_GateInForce_RejectedKey_FailsWithSameMessage(ApiKeyStatus status)
        {
            var context = NewContext(new ApiKeyValidationResult(status));

            var result = new ApiAuthorizationValidator().Validate(context);

            Assert.False(result.IsValid);
            Assert.Equal(JsonRpcErrorCode.InvalidRequest, result.Code);
            Assert.Equal(RejectedMessage, result.ErrorMessage);
        }

        [Theory]
        [DisplayName("相容態(尚未發放金鑰)與行程內呼叫應沿用非空檢查：非空放行")]
        [InlineData(ApiKeyStatus.NotConfigured)]
        [InlineData(ApiKeyStatus.NotChecked)]
        public void Validate_GateNotInForce_NonEmptyKey_Succeeds(ApiKeyStatus status)
        {
            var context = NewContext(new ApiKeyValidationResult(status));

            var result = new ApiAuthorizationValidator().Validate(context);

            Assert.True(result.IsValid);
        }

        [Theory]
        [DisplayName("相容態(尚未發放金鑰)與行程內呼叫應沿用非空檢查：空值拒絕")]
        [InlineData(ApiKeyStatus.NotConfigured)]
        [InlineData(ApiKeyStatus.NotChecked)]
        public void Validate_GateNotInForce_EmptyKey_Fails(ApiKeyStatus status)
        {
            var context = NewContext(new ApiKeyValidationResult(status), apiKey: "  ");

            var result = new ApiAuthorizationValidator().Validate(context);

            Assert.False(result.IsValid);
            Assert.Equal(RejectedMessage, result.ErrorMessage);
        }

        [Fact]
        [DisplayName("System.Ping 免金鑰：嚴格態下未帶金鑰仍應放行")]
        public void Validate_Ping_NoKey_Succeeds()
        {
            var context = NewContext(new ApiKeyValidationResult(ApiKeyStatus.NotProvided),
                apiKey: string.Empty, method: "System.Ping");

            var result = new ApiAuthorizationValidator().Validate(context);

            Assert.True(result.IsValid);
        }

        [Fact]
        [DisplayName("System.Ping 免金鑰：嚴格態下帶錯金鑰仍應放行(狀態另由 PingResult 回報)")]
        public void Validate_Ping_InvalidKey_Succeeds()
        {
            var context = NewContext(new ApiKeyValidationResult(ApiKeyStatus.Invalid, "app", string.Empty),
                method: "System.Ping");

            var result = new ApiAuthorizationValidator().Validate(context);

            Assert.True(result.IsValid);
        }

        [Theory]
        [DisplayName("Bearer 豁免清單不等於金鑰豁免清單：Login 與 GetApiPayloadOptions 仍需金鑰")]
        [InlineData("System.Login")]
        [InlineData("System.GetApiPayloadOptions")]
        public void Validate_BearerExemptMethods_StillRequireApiKey(string method)
        {
            var context = NewContext(new ApiKeyValidationResult(ApiKeyStatus.NotProvided),
                apiKey: string.Empty, method: method);

            var result = new ApiAuthorizationValidator().Validate(context);

            Assert.False(result.IsValid);
            Assert.Equal(RejectedMessage, result.ErrorMessage);
        }

        [Fact]
        [DisplayName("驗證結果為 null 時應退回相容態的非空檢查")]
        public void Validate_NullValidation_FallsBackToPresenceCheck()
        {
            var context = new ApiAuthorizationContext
            {
                ApiKey = "some-key",
                Authorization = "Bearer " + Guid.NewGuid(),
                Method = "Foo.Bar",
                ApiKeyValidation = null!,
            };

            var result = new ApiAuthorizationValidator().Validate(context);

            Assert.True(result.IsValid);
        }
    }
}
