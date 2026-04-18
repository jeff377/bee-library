using System.ComponentModel;
using Bee.Api.Core.Authorization;
using Bee.Api.Core.JsonRpc;

namespace Bee.Api.Core.UnitTests
{
    /// <summary>
    /// ApiAuthorizationResult 測試。
    /// </summary>
    public class ApiAuthorizationResultTests
    {
        [Fact]
        [DisplayName("預設建構子應為 IsValid=false、AccessToken=Guid.Empty")]
        public void DefaultConstructor_InitializesDefaults()
        {
            var result = new ApiAuthorizationResult();

            Assert.False(result.IsValid);
            Assert.Equal(Guid.Empty, result.AccessToken);
            Assert.Equal(string.Empty, result.ErrorMessage);
        }

        [Fact]
        [DisplayName("Success 應回傳 IsValid=true 並設定 AccessToken")]
        public void Success_SetsValidTrueAndToken()
        {
            var token = Guid.NewGuid();

            var result = ApiAuthorizationResult.Success(token);

            Assert.True(result.IsValid);
            Assert.Equal(token, result.AccessToken);
            Assert.Equal(string.Empty, result.ErrorMessage);
        }

        [Fact]
        [DisplayName("Fail 應回傳 IsValid=false 並設定 Code 與 ErrorMessage")]
        public void Fail_SetsValidFalseCodeAndMessage()
        {
            var result = ApiAuthorizationResult.Fail(JsonRpcErrorCode.Unauthorized, "無權限");

            Assert.False(result.IsValid);
            Assert.Equal(JsonRpcErrorCode.Unauthorized, result.Code);
            Assert.Equal("無權限", result.ErrorMessage);
            Assert.Equal(Guid.Empty, result.AccessToken);
        }

        [Theory]
        [InlineData(JsonRpcErrorCode.InvalidRequest, "invalid")]
        [InlineData(JsonRpcErrorCode.MethodNotFound, "not found")]
        [InlineData(JsonRpcErrorCode.Unauthorized, "unauthorized")]
        [DisplayName("Fail 應保留指定的 Code 與 ErrorMessage")]
        public void Fail_PreservesCodeAndMessage(JsonRpcErrorCode code, string message)
        {
            var result = ApiAuthorizationResult.Fail(code, message);

            Assert.Equal(code, result.Code);
            Assert.Equal(message, result.ErrorMessage);
        }
    }
}
