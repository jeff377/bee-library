using System.ComponentModel;
using Bee.Api.Core.JsonRpc;

namespace Bee.Api.Core.UnitTests
{
    /// <summary>
    /// JsonRpcException 測試。
    /// </summary>
    public class JsonRpcExceptionTests
    {
        [Fact]
        [DisplayName("建構子應設定 HttpStatusCode、ErrorCode、RpcMessage 與 Message")]
        public void Constructor_SetsAllProperties()
        {
            var ex = new JsonRpcException(400, JsonRpcErrorCode.InvalidRequest, "invalid payload");

            Assert.Equal(400, ex.HttpStatusCode);
            Assert.Equal(JsonRpcErrorCode.InvalidRequest, ex.ErrorCode);
            Assert.Equal("invalid payload", ex.RpcMessage);
            Assert.Equal("invalid payload", ex.Message);
        }

        [Theory]
        [InlineData(JsonRpcErrorCode.ParseError)]
        [InlineData(JsonRpcErrorCode.MethodNotFound)]
        [InlineData(JsonRpcErrorCode.Unauthorized)]
        [DisplayName("建構子應保留指定的 ErrorCode")]
        public void Constructor_PreservesErrorCode(JsonRpcErrorCode code)
        {
            var ex = new JsonRpcException(500, code, "x");

            Assert.Equal(code, ex.ErrorCode);
        }
    }
}
