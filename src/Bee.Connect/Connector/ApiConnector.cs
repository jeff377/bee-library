using System;
using Bee.Api.Core;
using Bee.Base;
using Bee.Define;

namespace Bee.Connect
{
    /// <summary>
    /// API 服務連接器基底類別。
    /// </summary>
    public abstract class ApiConnector
    {
        #region 建構函式

        /// <summary>
        /// 建構函式，採用近端連線。
        /// </summary>
        /// <param name="accessToken">存取令牌。</param>
        public ApiConnector(Guid accessToken)
        {
            AccessToken = accessToken;
            Provider = new LocalApiServiceProvider(accessToken);
        }

        /// <summary>
        /// 建構函式，採用遠端連線。
        /// </summary>
        /// <param name="endpoint">API 服務端點。</param>
        /// <param name="accessToken">存取令牌。</param>
        public ApiConnector(string endpoint, Guid accessToken)
        {
            if (StrFunc.IsEmpty(endpoint))
                throw new ArgumentException("Endpoint cannot be null or empty.", nameof(endpoint));

            AccessToken = accessToken;
            Provider = new RemoteApiServiceProvider(endpoint, accessToken);
        }

        #endregion

        /// <summary>
        /// 存取令牌。
        /// </summary>
        public Guid AccessToken { get; private set; }

        /// <summary>
        /// API 服務提供者。
        /// </summary>
        public IJsonRpcProvider Provider { get; private set; }

        /// <summary>
        /// 執行 API 方法。
        /// </summary>
        /// <param name="progId">程式代碼。</param>
        /// <param name="action">執行動作。</param>
        /// <param name="value">對應執行動作的傳入參數。</param>
        /// <param name="enableEncoding">是否啟用資料編碼（序列化、壓縮與加密）。</param>
        protected T Execute<T>(string progId, string action, object value, bool enableEncoding = true)
        {
            if (StrFunc.IsEmpty(progId))
                throw new ArgumentException("progId cannot be null or empty.", nameof(progId));
            if (StrFunc.IsEmpty(action))
                throw new ArgumentException("action cannot be null or empty.", nameof(action));

            // 近端連線只有在偵錯模式下才進行編碼
            if (this.Provider is LocalApiServiceProvider && !SysInfo.IsDebugMode)
            {
                enableEncoding = false;
            }

            // 建立 JSON-RPC 請求模型
            var request = new JsonRpcRequest()
            {
                Method = $"{progId}.{action}",
                Params = new JsonRpcParams()
                {
                    Value = value
                },
                Id = Guid.NewGuid().ToString()
            };

            // 傳入資料進行編碼
            if (enableEncoding)
            { 
                request.Encode(FrontendInfo.ApiEncryptionKey);             
            }

            // 執行 API 方法
            var response = this.Provider.Execute(request);

            // 傳出結果進行解碼
            if (enableEncoding)
            { 
                response.Decode(FrontendInfo.ApiEncryptionKey); 
            }

            if (response.Error != null)
                throw new InvalidOperationException($"API error: {response.Error.Message}");
            return (T)response.Result.Value;
        }
    }
}
