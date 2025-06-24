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

        /// <summary>
        /// 執行自訂方法。
        /// </summary>
        /// <param name="args">傳入引數。</param>
        public ExecFuncResult ExecFunc(ExecFuncArgs args)
        {
            var result = new ExecFuncResult();
            DoBeforeExecFunc(args, result);
            DoExecFunc(args, result);
            DoAfterExecFunc(args, result);
            return result;
        }

        /// <summary>
        /// 執行 ExecFunc 前的呼叫方法。
        /// </summary>
        /// <param name="args">傳入引數。</param>
        /// <param name="result">傳出結果。</param>
        protected virtual  void DoBeforeExecFunc(ExecFuncArgs args, ExecFuncResult result)
        { }

        /// <summary>
        /// 執行 ExecFunc 方法的實作。
        /// </summary>
        /// <param name="args">傳入引數。</param>
        /// <param name="result">傳出結果。</param>
        protected virtual void DoExecFunc(ExecFuncArgs args, ExecFuncResult result)
        { }

        /// <summary>
        /// 執行 ExecFunc 後的呼叫方法。
        /// </summary>
        /// <param name="args">傳入引數。</param>
        /// <param name="result">傳出結果。</param>
        protected virtual void DoAfterExecFunc(ExecFuncArgs args, ExecFuncResult result)
        { }
    }
}
