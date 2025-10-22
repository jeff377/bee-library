using Bee.Base;
using Bee.Contracts;
using Bee.Define;
using System;
using System.Runtime.ExceptionServices;

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
        /// <param name="currentRequirement">目前呼叫的授權需求。</param>
        /// <param name="args">傳入引數。</param>
        /// <param name="result">傳出結果。</param>
        public static void InvokeExecFunc(
            IExecFuncHandler execFunc,
            ApiAccessRequirement currentRequirement,
            ExecFuncArgs args,
            ExecFuncResult result)
        {
            try
            {
                // 使用反射，執行 FuncID 對應的自訂方法
                var method = execFunc.GetType().GetMethod(args.FuncId);
                if (method == null)
                    throw new MissingMethodException($"Method {args.FuncId} not found.");

                // 取得 ExecFuncAccessControlAttribute
                var attr = (ExecFuncAccessControlAttribute)Attribute.GetCustomAttribute(
                      method, typeof(ExecFuncAccessControlAttribute));

                // 沒有標註時，預設只允許 Authenticated
                var required = attr?.AccessRequirement ?? ApiAccessRequirement.Authenticated;

                // 判斷授權需求
                if (required == ApiAccessRequirement.Authenticated && currentRequirement == ApiAccessRequirement.Anonymous)
                    throw new UnauthorizedAccessException($"FuncID '{args.FuncId}' requires authentication.");

                method.Invoke(execFunc, new object[] { args, result });
            }
            catch (Exception ex)
            {
                var rootEx = BaseFunc.UnwrapException(ex);
                ExceptionDispatchInfo.Capture(rootEx).Throw();  // 拋出指定的 Exception，並保留它原始的 stack trace
                throw; // 不會執行到，純粹為了編譯器
            }
        }
    }
}
