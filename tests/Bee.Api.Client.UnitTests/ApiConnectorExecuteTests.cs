using System.Collections.Concurrent;
using System.ComponentModel;
using System.Reflection;
using Bee.Api.Client.Providers;
using Bee.Api.Client.Connectors;
using Bee.Api.Core.JsonRpc;
using Bee.Base;
using Bee.Base.Tracing;
using Bee.Api.Core.Messages;
using Bee.Api.Core.Transformers;

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
            SysInfo.TraceListener = new TraceDispatcher(writer);
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

        #region PayloadCodec

        /// <summary>
        /// 模擬 server 的回應：沿用請求宣告的 codec 與格式編碼回應，正是
        /// <c>JsonRpcExecutor</c> 的行為。少了這一步，client 端會拿到一個沒有 TypeName
        /// 的 Result 而在還原時失敗——那是測試骨架的問題，不是待測行為。
        /// </summary>
        private static FakeJsonRpcProvider CreateEchoProvider(PayloadFormat format)
        {
            return new FakeJsonRpcProvider
            {
                ResponseFactory = req =>
                {
                    var response = new JsonRpcResponse(req)
                    {
                        Result = new JsonRpcResult { Value = "echoed", Codec = req.Params.Codec }
                    };
                    // Encrypted 未帶金鑰時 client 端會降級為 Encoded，回應照同一格式編碼。
                    var actual = format == PayloadFormat.Plain ? PayloadFormat.Plain : PayloadFormat.Encoded;
                    ApiPayloadConverter.TransformTo(response.Result, actual);
                    return response;
                }
            };
        }

        [Theory]
        [DisplayName("設定 PayloadCodec 後，非 Plain 的請求應在信封標記該 codec")]
        [InlineData(PayloadFormat.Encoded)]
        [InlineData(PayloadFormat.Encrypted)]
        public async Task ExecuteAsync_WithPayloadCodec_StampsCodecOnRequest(PayloadFormat format)
        {
            var provider = CreateEchoProvider(format);
            var connector = CreateConnector(provider);
            connector.PayloadCodec = PayloadCodecNames.Json;

            // Encrypted 在未設金鑰時會自動降級為 Encoded，兩者都會編碼 body，正是本測試要看的。
            await connector.ExecuteAsync<string>(TestProgId, TestAction, "payload", format);

            Assert.Equal(PayloadCodecNames.Json, provider.LastRequest!.Params.Codec);
            Assert.IsType<byte[]>(provider.LastRequest.Params.Value);
        }

        [Fact]
        [DisplayName("未設定 PayloadCodec 時信封的 codec 應留空，維持既有 MessagePack 行為")]
        public async Task ExecuteAsync_WithoutPayloadCodec_LeavesCodecBlank()
        {
            var provider = CreateEchoProvider(PayloadFormat.Encoded);
            var connector = CreateConnector(provider);

            await connector.ExecuteAsync<string>(TestProgId, TestAction, "payload", PayloadFormat.Encoded);

            Assert.Equal(string.Empty, provider.LastRequest!.Params.Codec);
        }

        [Fact]
        [DisplayName("Plain 請求不帶編碼後的 body，因此不應標記 codec")]
        public async Task ExecuteAsync_PlainFormat_DoesNotStampCodec()
        {
            var provider = CreateEchoProvider(PayloadFormat.Plain);
            var connector = CreateConnector(provider);
            connector.PayloadCodec = PayloadCodecNames.Json;

            await connector.ExecuteAsync<string>(TestProgId, TestAction, "payload", PayloadFormat.Plain);

            Assert.Equal(string.Empty, provider.LastRequest!.Params.Codec);
        }

        [Fact]
        [DisplayName("以 json codec 送出的請求，回應同樣以 json codec 編碼時應能解回原值")]
        public async Task ExecuteAsync_JsonCodec_RoundTripsThroughResponse()
        {
            var provider = CreateEchoProvider(PayloadFormat.Encoded);
            var connector = CreateConnector(provider);
            connector.PayloadCodec = PayloadCodecNames.Json;

            var result = await connector.ExecuteAsync<string>(
                TestProgId, TestAction, "payload", PayloadFormat.Encoded);

            Assert.Equal("echoed", result);
        }

        #endregion

        /// <summary>
        /// 收集追蹤事件的測試用 writer。
        /// </summary>
        /// <remarks>
        /// <c>SysInfo.TraceListener</c> 是 process-wide static；當此測試類別把 listener
        /// 指向本實例時，**所有並行執行的測試類別**透過 Tracer 觸發的事件都會被
        /// 此 writer 捕捉到。為避免「Collection was modified」的並行列舉錯誤，
        /// Events 必須使用執行緒安全容器並提供 snapshot 列舉語意。
        /// </remarks>
        private sealed class CapturingTraceWriter : ITraceWriter
        {
            public ConcurrentQueue<TraceEvent> Events { get; } = new();
            public void Write(TraceEvent evt) => Events.Enqueue(evt);
        }
    }
}
