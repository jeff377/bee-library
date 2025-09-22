using Bee.Define;
using System;

namespace Bee.Business
{
    /// <summary>
    /// 業務邏輯共用函式庫
    /// </summary>
    public static class BusinessFunc
    {
        /// <summary>
        /// 使用反射，執行 ExecFunc 方法。
        /// </summary>
        /// <param name="execFunc">定義處理指定 FuncID 的執行功能之介面。</param>
        /// <param name="args">傳入引數。</param>
        /// <param name="result">傳出結果。</param>
        public static void InvokeExecFunc(IExecFuncHandler execFunc, ExecFuncArgs args, ExecFuncResult result)
        {
            try
            {
                // 使用反射，執行 FuncID 對應的自訂方法
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
                    throw;
            }
        }
    }
}
