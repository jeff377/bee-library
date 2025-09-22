using System;
using Bee.Define;

namespace Bee.Business
{
    /// <summary>
    /// 表單層級業務邏輯物件。
    /// </summary>
    public class FormBusinessObject : BusinessObject, IFormBusinessObject
    {
        #region 建構函式

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="accessToken">存取令牌。</param>
        /// <param name="progId">程式代碼。</param>
        public FormBusinessObject(Guid accessToken, string progId) : base(accessToken)
        {
            this.ProgId = progId;
        }

        #endregion

        /// <summary>
        /// 程式代碼。
        /// </summary>
        public string ProgId { get; private set; }

        /// <summary>
        /// 執行 ExecFunc 方法的實作。
        /// </summary>
        /// <param name="args">傳入引數。</param>
        /// <param name="result">傳出結果。</param>
        protected override void DoExecFunc(ExecFuncArgs args, ExecFuncResult result)
        {
            BusinessFunc.InvokeExecFunc(new FormExecFuncHandler(AccessToken), args, result);
        }
    }
}
