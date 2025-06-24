using System;
using Bee.Api.Core;
using Bee.Base;

namespace Bee.Connect
{
    /// <summary>
    /// API 服務連接器基底類別。
    /// </summary>
    public abstract class TApiConnector
    {
        #region 建構函式

        /// <summary>
        /// 建構函式，採用近端連線。
        /// </summary>
        /// <param name="accessToken">存取令牌。</param>
        public TApiConnector(Guid accessToken)
        {
            AccessToken = accessToken;
            Provider = new TLocalApiServiceProvider(accessToken);
        }

        /// <summary>
        /// 建構函式，採用遠端連線。
        /// </summary>
        /// <param name="endpoint">API 服務端點。</param>
        /// <param name="accessToken">存取令牌。</param>
        public TApiConnector(string endpoint, Guid accessToken)
        {
            if (StrFunc.IsEmpty(endpoint))
                throw new ArgumentException("Endpoint cannot be null or empty.", nameof(endpoint));

            AccessToken = accessToken;
            Provider = new TRemoteApiServiceProvider(endpoint, accessToken);
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

            var request = new JsonRpcRequest()
            {
                Method = $"{progId}.{action}",
                Params = new JsonRpcParams()
                {
                    Value = value
                },
                Id = Guid.NewGuid().ToString()
            };
            var response = this.Provider.Execute(request, enableEncoding);
            if (response.Error != null)
                throw new InvalidOperationException($"API error: {response.Error.Message}");
            return (T)response.Result.Value;
        }
    }
}
