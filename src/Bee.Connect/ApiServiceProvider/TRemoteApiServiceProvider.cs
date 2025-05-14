using System;
using System.Collections.Specialized;
using System.Threading.Tasks;
using Bee.Api.Core;
using Bee.Base;
using Bee.Define;

namespace Bee.Connect
{
    /// <summary>
    /// 遠端 API 服務提供者（透過網路存取後端商業邏輯）。
    /// </summary>
    public class TRemoteApiServiceProvider : IJsonRpcProvider
    {
        #region 建構函式

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="endpoint">API 服務端點。</param>
        /// <param name="accessToken">存取令牌。</param>
        public TRemoteApiServiceProvider(string endpoint, Guid accessToken)
        {
            Endpoint = endpoint;
            AccessToken = accessToken;
        }

        #endregion

        /// <summary>
        /// 服務端點。
        /// </summary>
        public string Endpoint { get; private set; }

        /// <summary>
        /// 存取令牌。
        /// </summary>
        public Guid AccessToken { get; set; } = Guid.Empty;

        /// <summary>
        /// 執行 API 方法。
        /// </summary>
        /// <param name="request">JSON-RPC 請求模型。</param>
        public TJsonRpcResponse Execute(TJsonRpcRequest request)
        {
            var header = new NameValueCollection();
            header.Add(ApiHeaders.ApiKey, FrontendInfo.ApiKey);  // 遠端呼叫需傳入 API KEY，驗證呼叫端的合法性
            header.Add(ApiHeaders.Authorization, $"Bearer {AccessToken}");

            request.Encrypt(); // 傳入參數進行加密
            string body = request.ToJson();  // 傳入參數進行 JSON 序列化
            string json = HttpFunc.PostAsync(this.Endpoint, body, header).Result;  // 執行 Web API 方法
            var response = SerializeFunc.JsonToObject<TJsonRpcResponse>(json);  // 執行 JSON 反序列化
            response.Decrypt();  // 傳出結果進行解密
            return response;
        }

        /// <summary>
        /// 非同步執行 API 方法。
        /// </summary>
        /// <param name="request">JSON-RPC 請求模型。</param>
        public async Task<TJsonRpcResponse> ExecuteAsync(TJsonRpcRequest request)
        {
            var header = new NameValueCollection();
            header.Add(ApiHeaders.ApiKey, FrontendInfo.ApiKey);  // 遠端呼叫需傳入 API KEY，驗證呼叫端的合法性
            header.Add(ApiHeaders.Authorization, $"Bearer {AccessToken}");

            request.Encrypt(); // 傳入參數進行加密
            string body = request.ToJson();  // 傳入參數進行 JSON 序列化
            string json = await HttpFunc.PostAsync(this.Endpoint, body, header);  // 執行 Web API 方法
            var response = SerializeFunc.JsonToObject<TJsonRpcResponse>(json);  // 執行 JSON 反序列化
            response.Decrypt();  // 傳出結果進行解密
            return response;
        }

    }
}
