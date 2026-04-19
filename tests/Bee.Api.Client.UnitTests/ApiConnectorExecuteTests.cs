using System.ComponentModel;
using System.Reflection;
using Bee.Api.Client.ApiServiceProvider;
using Bee.Api.Client.Connectors;
using Bee.Api.Core;
using Bee.Api.Core.JsonRpc;
using Bee.Base;
using Bee.Base.Tracing;

namespace Bee.Api.Client.UnitTests
{
    /// <summary>
    /// 針對 <see cref="ApiConnector"/> 的 <c>ExecuteAsync</c> 與輔助流程的純邏輯測試。
    /// 以 <see cref="FakeJsonRpcProvider"/> 取代實際的 JSON-RPC 提供者，避免依賴任何外部服務。
    /// </summary>
    public class ApiConnectorExecuteTests
    {
        private const string TestProgId = "Unit";
        private const string TestAction = "Echo";

        /// <summary>
        /// 公開 <see cref="ApiConnector"/> 的 <c>ExecuteAsync</c>，以便於測試中直接呼叫。
        /// </summary>
        private sealed class TestApiConnector : ApiConnector
        {
            public TestApiConnector(Guid accessToken) : base(accessToken) { }

            public new Task<T> ExecuteAsync<T>(string progId, string action, object value, PayloadFormat format)
                => base.ExecuteAsync<T>(progId, action, value, format);
        }

        /// <summary>
        /// 可自訂回應內容的假 <see cref="IJsonRpcProvider"/>，僅用於單元測試。
        /// </summary>
        private sealed class FakeJsonRpcProvider : IJsonRpcProvider
        {
            public JsonRpcRequest? LastRequest { get; private set; }
            public int AsyncCallCount { get; private set; }
            public Func<JsonRpcRequest, JsonRpcResponse> ResponseFactory { get; set; }
                = req => new JsonRpcResponse(req)
                {
                    Result = new JsonRpcResult { Value = "ok" }
                };

            public Task<JsonRpcResponse> ExecuteAsync(JsonRpcRequest request)
            {
                LastRequest = request;
                AsyncCallCount++;
                return Task.FromResult(ResponseFactory(request));
            }
        }

        /// <summary>
        /// 透過反射將 <see cref="ApiConnector.Provider"/>（private setter）替換為測試用提供者。
        /// </summary>
        private static void InjectProvider(ApiConnector connector, IJsonRpcProvider provider)
        {
            var prop = typeof(ApiConnector).GetProperty(nameof(ApiConnector.Provider),
                BindingFlags.Public | BindingFlags.Instance)!;
            prop.SetValue(connector, provider);
        }

        private static TestApiConnector CreateConnector(FakeJsonRpcProvider provider)
        {
            var connector = new TestApiConnector(Guid.NewGuid());
            InjectProvider(connector, provider);
            return connector;
        }

        [Fact]
        [DisplayName("ExecuteAsync 成功時應回傳 Provider 的結果並轉換為目標型別")]
        public async Task ExecuteAsync_Plain_ReturnsConvertedResult()
        {
            var provider = new FakeJsonRpcProvider();
            var connector = CreateConnector(provider);

            var result = await connector.ExecuteAsync<string>(TestProgId, TestAction, new object(), PayloadFormat.Plain);

            Assert.Equal("ok", result);
            Assert.Equal(1, provider.AsyncCallCount);
            Assert.NotNull(provider.LastRequest);
            Assert.Equal($"{TestProgId}.{TestAction}", provider.LastRequest!.Method);
            Assert.False(string.IsNullOrEmpty(provider.LastRequest.Id));
            Assert.NotNull(provider.LastRequest.Params);
        }

        [Fact]
        [DisplayName("ExecuteAsync 於 Provider 回傳 Error 時應拋出 InvalidOperationException")]
        public async Task ExecuteAsync_WithErrorResponse_ThrowsInvalidOperationException()
        {
            var provider = new FakeJsonRpcProvider
            {
                ResponseFactory = req => new JsonRpcResponse(req)
                {
                    Error = new JsonRpcError(-32601, "Method not found")
                }
            };
            var connector = CreateConnector(provider);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await connector.ExecuteAsync<string>(TestProgId, TestAction, new object(), PayloadFormat.Plain));

            Assert.Contains("-32601", ex.Message);
            Assert.Contains("Method not found", ex.Message);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [DisplayName("ExecuteAsync 空白 progId 應拋 ArgumentException")]
        public async Task ExecuteAsync_EmptyProgId_ThrowsArgumentException(string? progId)
        {
            var connector = CreateConnector(new FakeJsonRpcProvider());
            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await connector.ExecuteAsync<object>(progId!, TestAction, new object(), PayloadFormat.Plain));
        }

        [Fact]
        [DisplayName("ExecuteAsync 啟用 Tracer 時應寫入 Request／Response 追蹤事件")]
        public async Task ExecuteAsync_WithTracerEnabled_WritesTraceEvents()
        {
            var writer = new CapturingTraceWriter();
            var previousListener = SysInfo.TraceListener;
            SysInfo.TraceListener = new TraceListener(writer);
            try
            {
                var provider = new FakeJsonRpcProvider();
                var connector = CreateConnector(provider);

                var result = await connector.ExecuteAsync<string>(TestProgId, TestAction, new object(), PayloadFormat.Plain);

                Assert.Equal("ok", result);
                // 應有 Start (Execute.Unit.Echo)、Request Point、Response Point、End 四筆事件
                Assert.Contains(writer.Events, e => e.Kind == TraceEventKind.Start);
                Assert.Contains(writer.Events, e => e.Kind == TraceEventKind.End);
                Assert.Contains(writer.Events, e => e.Kind == TraceEventKind.Point
                    && (e.Name ?? string.Empty).StartsWith("Request"));
                Assert.Contains(writer.Events, e => e.Kind == TraceEventKind.Point
                    && (e.Name ?? string.Empty).StartsWith("Response"));
            }
            finally
            {
                SysInfo.TraceListener = previousListener;
            }
        }

        /// <summary>
        /// 收集追蹤事件的測試用 writer。
        /// </summary>
        private sealed class CapturingTraceWriter : ITraceWriter
        {
            public List<TraceEvent> Events { get; } = new();
            public void Write(TraceEvent evt) => Events.Add(evt);
        }
    }
}
