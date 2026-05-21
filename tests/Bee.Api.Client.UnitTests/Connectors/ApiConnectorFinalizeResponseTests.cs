using System.ComponentModel;
using System.Reflection;
using Bee.Api.Client.Connectors;
using Bee.Api.Client.Providers;
using Bee.Api.Core.JsonRpc;
using Bee.Api.Core.Messages;
using Bee.Base.Exceptions;

namespace Bee.Api.Client.UnitTests.Connectors
{
    /// <summary>
    /// Tests for the response-finalization branch of <see cref="ApiConnector"/>,
    /// covering the mapping from <see cref="JsonRpcError.Code"/> back to client-side
    /// exception types (round-trip with <c>JsonRpcExecutor.MapException</c>).
    /// </summary>
    public class ApiConnectorFinalizeResponseTests
    {
        private const string TestProgId = "Unit";
        private const string TestAction = "Echo";

        private sealed class TestApiConnector : ApiConnector
        {
            public TestApiConnector(Guid accessToken) : base(accessToken) { }

            public new Task<T> ExecuteAsync<T>(string progId, string action, object value, PayloadFormat format)
                => base.ExecuteAsync<T>(progId, action, value, format);
        }

        private sealed class FakeJsonRpcProvider : IJsonRpcProvider
        {
            public Func<JsonRpcRequest, JsonRpcResponse> ResponseFactory { get; set; }
                = req => new JsonRpcResponse(req) { Result = new JsonRpcResult { Value = "ok" } };

            public Task<JsonRpcResponse> ExecuteAsync(JsonRpcRequest request)
                => Task.FromResult(ResponseFactory(request));
        }

        private static TestApiConnector CreateConnector(IJsonRpcProvider provider)
        {
            var connector = new TestApiConnector(Guid.NewGuid());
            var prop = typeof(ApiConnector).GetProperty(nameof(ApiConnector.Provider),
                BindingFlags.Public | BindingFlags.Instance)!;
            prop.SetValue(connector, provider);
            return connector;
        }

        [Fact]
        [DisplayName("FinalizeResponse 於 UserMessage code 應拋出 UserMessageException 且訊息純淨無前綴")]
        public async Task FinalizeResponse_UserMessageCode_ThrowsUserMessageException()
        {
            var provider = new FakeJsonRpcProvider
            {
                ResponseFactory = req => new JsonRpcResponse(req)
                {
                    Error = new JsonRpcError((int)JsonRpcErrorCode.UserMessage, "欄位不能為空")
                }
            };
            var connector = CreateConnector(provider);

            var ex = await Assert.ThrowsAsync<UserMessageException>(async () =>
                await connector.ExecuteAsync<string>(TestProgId, TestAction, new object(), PayloadFormat.Plain));

            Assert.Equal("欄位不能為空", ex.Message);
            Assert.DoesNotContain("API error", ex.Message);
        }

        [Fact]
        [DisplayName("FinalizeResponse 於 InternalError code 應拋出 InvalidOperationException 並保留前綴格式")]
        public async Task FinalizeResponse_InternalErrorCode_ThrowsInvalidOperationException()
        {
            var provider = new FakeJsonRpcProvider
            {
                ResponseFactory = req => new JsonRpcResponse(req)
                {
                    Error = new JsonRpcError((int)JsonRpcErrorCode.InternalError, "Internal server error")
                }
            };
            var connector = CreateConnector(provider);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await connector.ExecuteAsync<string>(TestProgId, TestAction, new object(), PayloadFormat.Plain));

            Assert.Contains("API error", ex.Message);
            Assert.Contains("-32000", ex.Message);
            Assert.Contains("Internal server error", ex.Message);
        }

        [Fact]
        [DisplayName("FinalizeResponse 於 ParseError 等其他協定 code 應拋出 InvalidOperationException(迴歸)")]
        public async Task FinalizeResponse_OtherProtocolCode_ThrowsInvalidOperationException()
        {
            var provider = new FakeJsonRpcProvider
            {
                ResponseFactory = req => new JsonRpcResponse(req)
                {
                    Error = new JsonRpcError((int)JsonRpcErrorCode.MethodNotFound, "Method not found")
                }
            };
            var connector = CreateConnector(provider);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await connector.ExecuteAsync<string>(TestProgId, TestAction, new object(), PayloadFormat.Plain));

            Assert.Contains("-32601", ex.Message);
            Assert.Contains("Method not found", ex.Message);
        }

        [Fact]
        [DisplayName("UserMessageException 可被 catch (Exception) 接住(迴歸:既有寬泛 catch 仍能運作)")]
        public async Task FinalizeResponse_UserMessageException_StillCaughtAsException()
        {
            var provider = new FakeJsonRpcProvider
            {
                ResponseFactory = req => new JsonRpcResponse(req)
                {
                    Error = new JsonRpcError((int)JsonRpcErrorCode.UserMessage, "test")
                }
            };
            var connector = CreateConnector(provider);

            Exception? caught = null;
            try
            {
                await connector.ExecuteAsync<string>(TestProgId, TestAction, new object(), PayloadFormat.Plain);
            }
            catch (Exception ex)
            {
                caught = ex;
            }

            Assert.NotNull(caught);
            Assert.IsType<UserMessageException>(caught);
        }

        [Fact]
        [DisplayName("FinalizeResponse 於成功響應應正常回傳結果(迴歸)")]
        public async Task FinalizeResponse_NoError_ReturnsValue()
        {
            var provider = new FakeJsonRpcProvider();  // 預設回 result = "ok"
            var connector = CreateConnector(provider);

            var result = await connector.ExecuteAsync<string>(TestProgId, TestAction, new object(), PayloadFormat.Plain);

            Assert.Equal("ok", result);
        }
    }
}
