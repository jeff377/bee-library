using System.Reflection;
using Bee.Api.Client.Connectors;
using Bee.Api.Client.Providers;
using Bee.Api.Core.JsonRpc;
using Bee.Api.Core.Messages;

namespace Bee.Api.Client.UnitTests.Connectors
{
    /// <summary>
    /// 以假的 <see cref="IJsonRpcProvider"/> 驅動 <see cref="ApiConnector"/> 走完一次呼叫，
    /// 讓測試能觀察請求送出前與回應收到後的實際行為，而不需要真的 server。
    /// </summary>
    /// <remarks>
    /// <c>FinalizeResponse</c> 與 <c>PrepareRequest</c> 都是 private，刻意不以反射直接呼叫：
    /// 測試要驗的是呼叫端看得到的行為，而步驟之間的先後順序只有走完整條呼叫路徑才驗得到。
    /// </remarks>
    internal static class ApiConnectorTestHost
    {
        private const string TestProgId = "Unit";
        private const string TestAction = "Echo";

        private sealed class TestApiConnector : ApiConnector
        {
            public TestApiConnector(Guid accessToken) : base(accessToken) { }

            public TestApiConnector(Guid accessToken, ApiSessionContext session) : base(accessToken, session) { }

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

        private static TestApiConnector CreateConnector(IJsonRpcProvider provider, ApiSessionContext? session = null)
        {
            // A dedicated session keeps tests off ApiSessionContext.Ambient, which is process-wide.
            var connector = session == null
                ? new TestApiConnector(Guid.NewGuid())
                : new TestApiConnector(Guid.NewGuid(), session);
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

        /// <summary>
        /// 以指定的使用者時區送出一次請求，server 以成功結果回應（預設值 "ok"）。
        /// </summary>
        /// <param name="value">請求的 payload 值。</param>
        /// <param name="userTimeZoneId">使用者的 IANA 時區 id；空字串代表尚未登入、不做時區換算。</param>
        public static Task<string> ExecuteAsUserAsync(object value, string userTimeZoneId)
        {
            var session = new ApiSessionContext { UserTimeZoneId = userTimeZoneId };
            return CreateConnector(new FakeJsonRpcProvider(), session).ExecuteAsync<string>(
                TestProgId, TestAction, value, PayloadFormat.Plain);
        }
    }
}
