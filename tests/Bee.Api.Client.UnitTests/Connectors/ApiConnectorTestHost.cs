using System.Reflection;
using Bee.Api.Client.Connectors;
using Bee.Api.Client.Providers;
using Bee.Api.Core.JsonRpc;
using Bee.Api.Core.Messages;

namespace Bee.Api.Client.UnitTests.Connectors
{
    /// <summary>
    /// 以假的 <see cref="IJsonRpcProvider"/> 驅動 <see cref="ApiConnector"/> 走完一次呼叫，
    /// 讓測試能觀察 <c>FinalizeResponse</c> 的實際行為，而不需要真的 server。
    /// </summary>
    /// <remarks>
    /// <c>FinalizeResponse</c> 是 private，刻意不以反射直接呼叫：測試要驗的是
    /// 「呼叫端拿到某個錯誤碼時會得到什麼例外」這個對外行為，走完整條呼叫路徑才驗得到。
    /// </remarks>
    internal static class ApiConnectorTestHost
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

        /// <summary>
        /// 執行一次呼叫，server 以指定的錯誤碼與訊息回應。
        /// </summary>
        /// <param name="code">server 回傳的 JSON-RPC 錯誤碼。</param>
        /// <param name="message">server 回傳的訊息。</param>
        public static Task<string> ExecuteWithErrorAsync(JsonRpcErrorCode code, string message)
        {
            var provider = new FakeJsonRpcProvider
            {
                ResponseFactory = req => new JsonRpcResponse(req)
                {
                    Error = new JsonRpcError((int)code, message)
                }
            };
            return CreateConnector(provider).ExecuteAsync<string>(
                TestProgId, TestAction, new object(), PayloadFormat.Plain);
        }

        /// <summary>
        /// 執行一次呼叫，server 以成功結果回應（預設值 "ok"）。
        /// </summary>
        public static Task<string> ExecuteWithResultAsync()
        {
            return CreateConnector(new FakeJsonRpcProvider()).ExecuteAsync<string>(
                TestProgId, TestAction, new object(), PayloadFormat.Plain);
        }
    }
}
