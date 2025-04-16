using System;
using Bee.Define;

namespace Bee.Connect
{
    /// <summary>
    /// 功能層級服務連線器。
    /// </summary>
    public class TBusinessConnector : TApiConnector
    {
        #region 建構函式

        /// <summary>
        /// 建構函式，採用近端連線。
        /// </summary>
        /// <param name="accessToken">存取令牌。</param>
        /// <param name="progID">程式代碼。</param>
        public TBusinessConnector(Guid accessToken, string progID) : base(accessToken)
        {
            ProgID = progID;
        }

        /// <summary>
        /// 建構函式，採用遠端連線。
        /// </summary>
        /// <param name="endpoint">服務端點。。</param>
        /// <param name="accessToken">存取令牌。</param>
        /// <param name="progID">程式代碼。</param>
        public TBusinessConnector(string endpoint, Guid accessToken, string progID) : base(endpoint, accessToken)
        {
            ProgID = progID;
        }

        #endregion

        /// <summary>
        /// 程式代碼。
        /// </summary>
        public string ProgID { get; private set; }

        /// <summary>
        /// 執行 API 方法。
        /// </summary>
        /// <param name="action">執行動作。</param>
        /// <param name="value">對應執行動作的傳入參數。</param>
        protected object ApiExecute(string action, object value)
        {
            return base.ApiExecute(this.ProgID, action, value);
        }

        /// <summary>
        /// 執行自訂方法。
        /// </summary>
        /// <param name="args">傳入引數。</param>
        public TExecFuncResult ExecFunc(TExecFuncArgs args)
        {
            return ApiExecute(SystemActions.ExecFunc, args) as TExecFuncResult;
        }
    }
}
