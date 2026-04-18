using System.ComponentModel;
using Bee.Api.Core.Authorization;
using Bee.Api.Core.JsonRpc;

namespace Bee.Api.Core.UnitTests.Authorization
{
    /// <summary>
    /// ApiAuthorizationValidator 單元測試。
    /// </summary>
    public class ApiAuthorizationValidatorTests
    {
        private static ApiAuthorizationValidator CreateValidator() => new();

        [Fact]
        [DisplayName("Validate 於 context 為 null 時應回傳 InvalidRequest 失敗")]
        public void Validate_NullContext_Fails()
        {
            var result = CreateValidator().Validate(null!);

            Assert.False(result.IsValid);
            Assert.Equal(JsonRpcErrorCode.InvalidRequest, result.Code);
            Assert.Equal("Invalid authorization context.", result.ErrorMessage);
        }

        [Theory]
        [DisplayName("Validate 於 ApiKey 缺失或空白時應回傳 InvalidRequest 失敗")]
        [InlineData("")]
        [InlineData("   ")]
        public void Validate_MissingApiKey_Fails(string apiKey)
        {
            var context = new ApiAuthorizationContext
            {
                ApiKey = apiKey,
                Method = "Foo.Bar",
                Authorization = "Bearer " + Guid.NewGuid()
            };

            var result = CreateValidator().Validate(context);

            Assert.False(result.IsValid);
            Assert.Equal(JsonRpcErrorCode.InvalidRequest, result.Code);
            Assert.Equal("Missing or invalid API key.", result.ErrorMessage);
        }

        [Theory]
        [DisplayName("Validate 於免授權方法應回傳成功且 AccessToken 為 Empty")]
        [InlineData("System.Ping")]
        [InlineData("System.GetApiPayloadOptions")]
        [InlineData("System.Login")]
        public void Validate_NoAuthMethod_SucceedsWithEmptyToken(string method)
        {
            var context = new ApiAuthorizationContext
            {
                ApiKey = "test-key",
                Method = method,
                Authorization = string.Empty
            };

            var result = CreateValidator().Validate(context);

            Assert.True(result.IsValid);
            Assert.Equal(Guid.Empty, result.AccessToken);
        }

        [Fact]
        [DisplayName("Validate 於需要授權但缺少 Authorization 標頭時應失敗")]
        public void Validate_MissingAuthorizationHeader_Fails()
        {
            var context = new ApiAuthorizationContext
            {
                ApiKey = "test-key",
                Method = "Foo.Bar",
                Authorization = string.Empty
            };

            var result = CreateValidator().Validate(context);

            Assert.False(result.IsValid);
            Assert.Equal(JsonRpcErrorCode.InvalidRequest, result.Code);
            Assert.Equal("Missing Authorization header.", result.ErrorMessage);
        }

        [Fact]
        [DisplayName("Validate 於 Authorization 格式非 Bearer 時應失敗")]
        public void Validate_NonBearerAuthorization_Fails()
        {
            var context = new ApiAuthorizationContext
            {
                ApiKey = "test-key",
                Method = "Foo.Bar",
                Authorization = "Basic abc123"
            };

            var result = CreateValidator().Validate(context);

            Assert.False(result.IsValid);
            Assert.Equal(JsonRpcErrorCode.InvalidRequest, result.Code);
            Assert.Contains("Bearer", result.ErrorMessage);
        }

        [Fact]
        [DisplayName("Validate 於 Bearer token 非 Guid 格式時應失敗")]
        public void Validate_InvalidBearerToken_Fails()
        {
            var context = new ApiAuthorizationContext
            {
                ApiKey = "test-key",
                Method = "Foo.Bar",
                Authorization = "Bearer not-a-guid"
            };

            var result = CreateValidator().Validate(context);

            Assert.False(result.IsValid);
            Assert.Equal(JsonRpcErrorCode.InvalidRequest, result.Code);
            Assert.Equal("Invalid access token.", result.ErrorMessage);
        }

        [Fact]
        [DisplayName("Validate 於 Bearer 前綴為大小寫變體時仍應成功(OrdinalIgnoreCase)")]
        public void Validate_BearerPrefixCaseInsensitive_Succeeds()
        {
            var token = Guid.NewGuid();
            var context = new ApiAuthorizationContext
            {
                ApiKey = "test-key",
                Method = "Foo.Bar",
                Authorization = "bearer " + token
            };

            var result = CreateValidator().Validate(context);

            Assert.True(result.IsValid);
            Assert.Equal(token, result.AccessToken);
        }

        [Fact]
        [DisplayName("Validate 於有效 Bearer token 應回傳成功並帶 AccessToken")]
        public void Validate_ValidBearerToken_Succeeds()
        {
            var token = Guid.NewGuid();
            var context = new ApiAuthorizationContext
            {
                ApiKey = "test-key",
                Method = "Foo.Bar",
                Authorization = $"Bearer {token}"
            };

            var result = CreateValidator().Validate(context);

            Assert.True(result.IsValid);
            Assert.Equal(token, result.AccessToken);
            Assert.Equal(string.Empty, result.ErrorMessage);
        }
    }
}
