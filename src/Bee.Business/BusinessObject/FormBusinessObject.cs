using System;
using Bee.Contracts;
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
        /// <param name="isLocalCall">呼叫是否為近端來源。</param>
        public FormBusinessObject(Guid accessToken, string progId, bool isLocalCall = true)
            : base(accessToken, isLocalCall)
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
        protected override void DoExecFunc(ExecFuncArgs args, ExecFuncResult result)
        {
            var handler = new FormExecFuncHandler(AccessToken);
            BusinessFunc.InvokeExecFunc(handler, ApiAccessRequirement.Authenticated, args, result);
        }

        /// <summary>
        /// 執行 ExecFuncAnonymou 方法的實作。
        /// </summary>
        protected override void DoExecFuncAnonymous(ExecFuncArgs args, ExecFuncResult result)
        {
            var handler = new FormExecFuncHandler(AccessToken);
            BusinessFunc.InvokeExecFunc(handler, ApiAccessRequirement.Anonymous, args, result);
        }
    }
}
