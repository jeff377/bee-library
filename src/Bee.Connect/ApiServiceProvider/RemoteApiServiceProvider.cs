using System;
using System.Collections.Specialized;
using System.Threading.Tasks;
using Bee.Api.Core;
using Bee.Base;
using Bee.Define;

namespace Bee.Connect
{
    /// <summary>
    /// 遠端 API 服務提供者（透過網路存取後端業務邏輯）。
    /// </summary>
    public class RemoteApiServiceProvider : IJsonRpcProvider
    {
        #region 建構函式

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="endpoint">API 服務端點。</param>
        /// <param name="accessToken">存取令牌。</param>
        public RemoteApiServiceProvider(string endpoint, Guid accessToken)
        {
            if (string.IsNullOrWhiteSpace(endpoint))
                throw new ArgumentException("Endpoint cannot be null or empty.", nameof(endpoint));

            Endpoint = endpoint;
            AccessToken = accessToken;  // 注意：AccessToken 可為 Guid.Empty，用於尚未登入狀態（如 Login、Ping）
        }

        #endregion

        /// <summary>
        /// 服務端點。
        /// </summary>
        public string Endpoint { get; private set; }

        /// <summary>
        /// 存取令牌。
        /// </summary>
        public Guid AccessToken { get; } = Guid.Empty;

        /// <summary>
        /// 執行 API 方法。
        /// </summary>
        /// <param name="request">JSON-RPC 請求模型。</param>
        public JsonRpcResponse Execute(JsonRpcRequest request)
        {
            // 在當前執行緒上執行 async 任務，死結風險較高（容易 UI 執行緒死結），無額外排程成本，適用呼叫端ＲＲＳ保證不是 UI 執行緒
            // return ExecuteAsync(request).GetAwaiter().GetResult();

            // 新開執行緒執行 async 任務，死結風險較低（因為不佔用 UI 執行緒），有額外排程成本，適用呼叫端可能為 UI 執行緒
            return Task.Run(() => ExecuteAsync(request)).GetAwaiter().GetResult();
        }

        /// <summary>
        /// 非同步執行 API 方法。
        /// </summary>
        /// <param name="request">JSON-RPC 請求模型。</param>
        public async Task<JsonRpcResponse> ExecuteAsync(JsonRpcRequest request)
        {
            var headers = CreateHeaders();
            string body = request.ToJson();  // 傳入參數進行 JSON 序列化
            string json = await HttpFunc.PostAsync(Endpoint, body, headers).ConfigureAwait(false); // 執行 Web API 方法
            var response = SerializeFunc.JsonToObject<JsonRpcResponse>(json);  // 執行 JSON 反序列化
            return response;
        }

        /// <summary>
        /// 建立 HTTP 標頭集合。
        /// </summary>
        private NameValueCollection CreateHeaders()
        {
            return new NameValueCollection
            {
                { ApiHeaders.ApiKey, FrontendInfo.ApiKey },
                { ApiHeaders.Authorization, $"Bearer {AccessToken}" }
            };
        }

    }
}
