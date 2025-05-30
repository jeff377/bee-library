using System;
using Bee.Define;

namespace Bee.Business
{
    /// <summary>
    /// 表單層級業務邏輯物件。
    /// </summary>
    public class TFormObject : TBusinessObject, IFormObject
    {
        #region 建構函式

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="accessToken">存取令牌。</param>
        /// <param name="progID">程式代碼。</param>
        public TFormObject(Guid accessToken, string progID) : base(accessToken)
        {
            this.ProgID = progID;
        }

        #endregion

        /// <summary>
        /// 程式代碼。
        /// </summary>
        public string ProgID { get; private set; }

        /// <summary>
        /// 執行 ExecFunc 方法的實作。
        /// </summary>
        /// <param name="args">傳入引數。</param>
        /// <param name="result">傳出結果。</param>
        protected override void DoExecFunc(TExecFuncArgs args, TExecFuncResult result)
        {
            InvokeExecFunc(args, result);
        }

        /// <summary>
        /// 使用反射，執行 ExecFunc 方法。
        /// </summary>
        /// <param name="args">傳入引數。</param>
        /// <param name="result">傳出結果。</param>
        private void InvokeExecFunc(TExecFuncArgs args, TExecFuncResult result)
        {
            try
            {
                // 使用反射，執行 FuncID 對應的自訂方法
                var execFunc = new TFormExecFunc(this.AccessToken);
                var method = execFunc.GetType().GetMethod(args.FuncID);
                if (method == null)
                    throw new MissingMethodException($"Method {args.FuncID} not found.");
                method.Invoke(execFunc, new object[] { args, result });
            }
            catch (Exception ex)
            {
                // 使用反射時，需抓取 InnerException 才是原始例外錯誤
                if (ex.InnerException != null)
                    throw ex.InnerException;
                else
                    throw ex;
            }
        }
    }
}
