using System;
using Bee.Define;

namespace Bee.Connect
{
    /// <summary>
    /// 表單層級 API 服務連接器。
    /// </summary>
    public class FormApiConnector : ApiConnector
    {
        #region 建構函式

        /// <summary>
        /// 建構函式，採用近端連線。
        /// </summary>
        /// <param name="accessToken">存取令牌。</param>
        /// <param name="progId">程式代碼。</param>
        public FormApiConnector(Guid accessToken, string progId) : base(accessToken)
        {
            ProgId = progId;
        }

        /// <summary>
        /// 建構函式，採用遠端連線。
        /// </summary>
        /// <param name="endpoint">服務端點。。</param>
        /// <param name="accessToken">存取令牌。</param>
        /// <param name="progId">程式代碼。</param>
        public FormApiConnector(string endpoint, Guid accessToken, string progId) : base(endpoint, accessToken)
        {
            ProgId = progId;
        }

        #endregion

        /// <summary>
        /// 程式代碼。
        /// </summary>
        public string ProgId { get; private set; }

        /// <summary>
        /// 執行 API 方法。
        /// </summary>
        /// <param name="action">執行動作。</param>
        /// <param name="value">對應執行動作的傳入參數。</param>
        public T Execute<T>(string action, object value)
        {
            return base.Execute<T>(ProgId, action, value, Api.Core.PayloadFormat.Encrypted);
        }

        /// <summary>
        /// 執行自訂方法。
        /// </summary>
        /// <param name="args">傳入引數。</param>
        public ExecFuncResult ExecFunc(ExecFuncArgs args)
        {
            return Execute<ExecFuncResult>(SystemActions.ExecFunc, args);
        }
    }
}
