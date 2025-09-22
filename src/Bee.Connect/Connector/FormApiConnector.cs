using System;
using System.Threading.Tasks;
using Bee.Api.Core;
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
        /// 程式代碼（對應表單層級的 ProgID，用於識別業務物件）。
        /// </summary>
        public string ProgId { get; private set; }

        /// <summary>
        /// 非同步執行 API 方法。
        /// </summary>
        /// <param name="action">執行動作。</param>
        /// <param name="value">對應執行動作的傳入參數。</param>
        /// <param name="format">傳輸資料的封裝格式。</param>
        public async Task<T> ExecuteAsync<T>(string action, object value, PayloadFormat format = PayloadFormat.Encrypted)
        {
            return await base.ExecuteAsync<T>(ProgId, action, value, format).ConfigureAwait(false);
        }

        /// <summary>
        /// 非同步執行自訂方法。
        /// </summary>
        /// <param name="args">傳入引數。</param>
        public async Task<ExecFuncResult> ExecFuncAsync(ExecFuncArgs args)
        {
            return await ExecuteAsync<ExecFuncResult>(SystemActions.ExecFunc, args).ConfigureAwait(false);
        }

        /// <summary>
        /// 執行自訂方法。
        /// </summary>
        /// <param name="args">傳入引數。</param>
        public ExecFuncResult ExecFunc(ExecFuncArgs args)
        {
            return SyncExecutor.Run(() =>
                ExecFuncAsync(args)
            );
        }

        /// <summary>
        /// 非同步執行自訂方法，匿名存取。
        /// </summary>
        /// <param name="args">傳入引數。</param>
        public async Task<ExecFuncResult> ExecFuncAnonymousAsync(ExecFuncArgs args)
        {
            return await ExecuteAsync<ExecFuncResult>(SystemActions.ExecFuncAnonymous, args).ConfigureAwait(false);
        }

        /// <summary>
        /// 非同步執行自訂方法，僅限近端呼叫。
        /// </summary>
        /// <param name="args">傳入引數。</param>
        public async Task<ExecFuncResult> ExecFuncLocalAsync(ExecFuncArgs args)
        {
            return await ExecuteAsync<ExecFuncResult>(SystemActions.ExecFuncLocal, args).ConfigureAwait(false);
        }
    }
}
