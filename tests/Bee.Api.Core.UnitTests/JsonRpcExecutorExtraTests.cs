using System.ComponentModel;
using Bee.Api.Core.JsonRpc;
using Bee.Api.Core.System;
using Bee.Definition;

namespace Bee.Api.Core.UnitTests
{
    /// <summary>
    /// JsonRpcExecutor 補強測試：
    /// 涵蓋錯誤路徑（ParseMethod 例外、空 progId、未知 action）、ExecuteAsync 路徑與建構子屬性。
    /// </summary>
    [Collection("Initialize")]
    public class JsonRpcExecutorExtraTests
    {
        [Fact]
        [DisplayName("建構子應正確設定 AccessToken 與 IsLocalCall")]
        public void Constructor_SetsProperties()
        {
            var token = Guid.NewGuid();
            var executor = new JsonRpcExecutor(token, isLocalCall: true);

            Assert.Equal(token, executor.AccessToken);
            Assert.True(executor.IsLocalCall);
        }

        [Fact]
        [DisplayName("建構子 isLocalCall 預設為 false")]
        public void Constructor_IsLocalCall_DefaultsToFalse()
        {
            var executor = new JsonRpcExecutor(Guid.NewGuid());
            Assert.False(executor.IsLocalCall);
        }

        [Fact]
        [DisplayName("ExecuteAsync 應可正確完成 Ping 方法")]
        public async Task ExecuteAsync_Ping_Succeeds()
        {
            var request = new JsonRpcRequest
            {
                Method = $"{SysProgIds.System}.Ping",
                Params = new JsonRpcParams { Value = new PingRequest { ClientName = "T", TraceId = "X" } },
                Id = Guid.NewGuid().ToString()
            };

            var response = await new JsonRpcExecutor(Guid.Empty).ExecuteAsync(request);

            Assert.NotNull(response.Result);
            Assert.Null(response.Error);
            Assert.IsType<PingResponse>(response.Result!.Value);
        }

        [Fact]
        [DisplayName("Execute 於 Method 缺少 '.' 應回傳 FormatException 使用者訊息")]
        public void Execute_MethodMissingDot_ReturnsFormatExceptionMessage()
        {
            var request = new JsonRpcRequest
            {
                Method = "InvalidMethodFormat",
                Params = new JsonRpcParams(),
                Id = "1"
            };

            var response = new JsonRpcExecutor(Guid.Empty).Execute(request);

            Assert.Null(response.Result);
            Assert.NotNull(response.Error);
            Assert.Equal(-1, response.Error!.Code);
            Assert.Contains("Invalid method format", response.Error.Message);
        }

        [Fact]
        [DisplayName("Execute 於 Method 為空字串應回傳 FormatException 使用者訊息")]
        public void Execute_EmptyMethod_ReturnsFormatExceptionMessage()
        {
            var request = new JsonRpcRequest
            {
                Method = string.Empty,
                Params = new JsonRpcParams(),
                Id = "1"
            };

            var response = new JsonRpcExecutor(Guid.Empty).Execute(request);

            Assert.NotNull(response.Error);
            Assert.Contains("Invalid method format", response.Error!.Message);
        }

        [Fact]
        [DisplayName("Execute 於 progId 為空字串應回傳 ArgumentException 使用者訊息")]
        public void Execute_EmptyProgId_ReturnsArgumentExceptionMessage()
        {
            // ".Ping" 經 ParseMethod 後 progId = ""、action = "Ping"
            var request = new JsonRpcRequest
            {
                Method = ".Ping",
                Params = new JsonRpcParams(),
                Id = "1"
            };

            var response = new JsonRpcExecutor(Guid.Empty).Execute(request);

            Assert.NotNull(response.Error);
            Assert.Contains("ProgId", response.Error!.Message);
        }

        [Fact]
        [DisplayName("Execute 於未知 Action 應將 MissingMethodException 遮罩為 Internal server error")]
        public void Execute_UnknownAction_ReturnsGenericInternalError()
        {
            var request = new JsonRpcRequest
            {
                Method = $"{SysProgIds.System}.DefinitelyNotAMethod",
                Params = new JsonRpcParams(),
                Id = "1"
            };

            var response = new JsonRpcExecutor(Guid.Empty).Execute(request);

            Assert.NotNull(response.Error);
            Assert.Equal(-1, response.Error!.Code);
            Assert.Equal("Internal server error", response.Error.Message);
        }

        [Fact]
        [DisplayName("Execute 回傳 Response 應回填 Method 與 Id")]
        public void Execute_Response_EchoesMethodAndId()
        {
            var id = Guid.NewGuid().ToString();
            var request = new JsonRpcRequest
            {
                Method = $"{SysProgIds.System}.Ping",
                Params = new JsonRpcParams { Value = new PingRequest { ClientName = "C", TraceId = "T" } },
                Id = id
            };

            var response = new JsonRpcExecutor(Guid.Empty).Execute(request);

            Assert.Equal(request.Method, response.Method);
            Assert.Equal(id, response.Id);
        }
    }
}
