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
    public class TRemoteApiServiceProvider : IApiServiceProvider
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
        /// <param name="args">傳入參數。</param>
        public TApiServiceResult Execute(TApiServiceArgs args)
        {
            var header = new NameValueCollection();
            header.Add(ApiHeaders.ApiKey, FrontendInfo.ApiKey);  // 遠端呼叫需傳入 API KEY，驗證呼叫端的合法性
            header.Add(ApiHeaders.Authorization, $"Bearer {AccessToken}");

            args.Encrypt(); // 傳入參數進行加密
            string body = args.ToJson();  // 傳入參數進行 JSON 序列化
            string json = HttpFunc.PostAsync(this.Endpoint, body, header).Result;  // 執行 Web API 方法
            var result = SerializeFunc.JsonToObject<TApiServiceResult>(json);  // 執行 JSON 反序列化
            result.Decrypt();  // 傳出結果進行解密
            return result;
        }

        /// <summary>
        /// 非同步執行 API 方法。
        /// </summary>
        /// <param name="args">傳入參數。</param>
        public Task<string> ExecuteAsync(TApiServiceArgs args)
        {
            var header = new NameValueCollection();
            header.Add(ApiHeaders.ApiKey, FrontendInfo.ApiKey);  // 遠端呼叫需傳入 API KEY，驗證呼叫端的合法性
            header.Add(ApiHeaders.Authorization, $"Bearer {AccessToken}");

            args.Encrypt(); // 傳入參數進行加密
            string body = args.ToJson();  // 傳入參數進行 JSON 序列化
            return HttpFunc.PostAsync(this.Endpoint, body, header);  // 執行 Web API 方法
        }

        /// <summary>
        /// 處理非同步執行 API 方法的回傳結果。
        /// </summary>
        /// <param name="task">非同步的任務。</param>
        public void ExecuteEnd(Task<string> task)
        {
            task.ContinueWith(t =>
            {
                var result = SerializeFunc.JsonToObject<TApiServiceResult>(t.Result);  // 執行 JSON 反序列化
                result.Decrypt();  // 傳出結果進行解密
            }); // 異步處理
        }
    }
}
