﻿using System;
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
            InvokeExecFunc(args, result);
        }

        /// <summary>
        /// 使用反射，執行 ExecFunc 方法。
        /// </summary>
        /// <param name="args">傳入引數。</param>
        /// <param name="result">傳出結果。</param>
        private void InvokeExecFunc(ExecFuncArgs args, ExecFuncResult result)
        {
            try
            {
                // 使用反射，執行 FuncID 對應的自訂方法
                var execFunc = new FormBusinessExecFunc(this.AccessToken);
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
