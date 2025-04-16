using System;
using Bee.Base;
using Bee.Define;

namespace Bee.Connect
{
    /// <summary>
    /// API 服務連線器基底類別。
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
        public IApiServiceProvider Provider { get; private set; }

        /// <summary>
        /// 執行 API 方法。
        /// </summary>
        /// <param name="progID">程式代碼。</param>
        /// <param name="action">執行動作。</param>
        /// <param name="value">對應執行動作的傳入參數。</param>
        protected object ApiExecute(string progID, string action, object value)
        {
            if (StrFunc.IsEmpty(progID))
                throw new ArgumentException("progID cannot be null or empty.", nameof(progID));
            if (StrFunc.IsEmpty(action))
                throw new ArgumentException("action cannot be null or empty.", nameof(action));

            var args = new TApiServiceArgs(progID, action, value);
            var result = this.Provider.Execute(args);
            if (StrFunc.IsNotEmpty(result.Message))
                throw new TException(result.Message);
            return result.Value;
        }
    }
}
