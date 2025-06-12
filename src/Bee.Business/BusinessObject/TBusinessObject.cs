using System;
using Bee.Define;

namespace Bee.Business
{
    /// <summary>
    /// 業務邏輯物件基底類別。
    /// </summary>
    public abstract class TBusinessObject : IBusinessObject
    {
        #region 建構函式

        /// <summary>
        /// 建構函式。
        /// </summary>
        public TBusinessObject()
        { }

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="accessToken">存取令牌。</param>
        public TBusinessObject(Guid accessToken)
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
        public TSessionInfo SessionInfo { get; private set; }   

        /// <summary>
        /// 執行自訂方法。
        /// </summary>
        /// <param name="args">傳入引數。</param>
        public TExecFuncResult ExecFunc(TExecFuncArgs args)
        {
            var result = new TExecFuncResult();
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
        protected virtual  void DoBeforeExecFunc(TExecFuncArgs args, TExecFuncResult result)
        { }

        /// <summary>
        /// 執行 ExecFunc 方法的實作。
        /// </summary>
        /// <param name="args">傳入引數。</param>
        /// <param name="result">傳出結果。</param>
        protected virtual void DoExecFunc(TExecFuncArgs args, TExecFuncResult result)
        { }

        /// <summary>
        /// 執行 ExecFunc 後的呼叫方法。
        /// </summary>
        /// <param name="args">傳入引數。</param>
        /// <param name="result">傳出結果。</param>
        protected virtual void DoAfterExecFunc(TExecFuncArgs args, TExecFuncResult result)
        { }
    }
}
