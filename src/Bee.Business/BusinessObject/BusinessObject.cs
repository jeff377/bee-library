using System;
using Bee.Define;

namespace Bee.Business
{
    /// <summary>
    /// 業務邏輯物件基底類別。
    /// </summary>
    public abstract class BusinessObject : IBusinessObject
    {
        #region 建構函式

        /// <summary>
        /// 建構函式。
        /// </summary>
        public BusinessObject()
        { }

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="accessToken">存取令牌。</param>
        public BusinessObject(Guid accessToken)
        {
            AccessToken = accessToken;
            // SessionInfo = CacheFunc.GetSessionInfo(accessToken);
        }

        #endregion

        /// <summary>
        /// 存取令牌。
        /// </summary>
        public Guid AccessToken { get; private set; }

        /// <summary>
        /// 連線資訊。
        /// </summary>
        public SessionInfo SessionInfo { get; private set; }
        public bool IsLocalCall { get; } = false;

        /// <summary>
        /// 執行自訂方法，開放方法，要求登入。
        /// </summary>
        /// <param name="args">傳入引數。</param>
        [ApiAccessControl(ApiProtectionLevel.Public, ApiAccessRequirement.Authenticated)]
        public ExecFuncResult ExecFunc(ExecFuncArgs args)
        {
            var result = new ExecFuncResult();
            DoExecFunc(args, result);
            return result;
        }

        /// <summary>
        /// 執行 ExecFunc 方法的實作。
        /// </summary>
        /// <param name="args">傳入引數。</param>
        /// <param name="result">傳出結果。</param>
        protected virtual void DoExecFunc(ExecFuncArgs args, ExecFuncResult result)
        { }

        /// <summary>
        /// 執行自訂方法，開放方法，匿名存取。
        /// </summary>
        /// <param name="args">傳入引數。</param>
        [ApiAccessControl(ApiProtectionLevel.Public, ApiAccessRequirement.Anonymous)]
        public ExecFuncResult ExecFuncAnonymous(ExecFuncArgs args)
        {
            var result = new ExecFuncResult();
            DoExecFuncAnonymous(args, result);
            return result;
        }

        /// <summary>
        /// 執行 ExecFuncAuth 方法的實作。
        /// </summary>
        /// <param name="args">傳入引數。</param>
        /// <param name="result">傳出結果。</param>
        protected virtual void DoExecFuncAnonymous(ExecFuncArgs args, ExecFuncResult result)
        { }

    }
}
