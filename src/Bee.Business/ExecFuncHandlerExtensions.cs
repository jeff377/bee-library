using System.Runtime.ExceptionServices;
using Bee.Base;
using Bee.Business.Attributes;
using Bee.Definition.Security;

namespace Bee.Business
{
    /// <summary>
    /// Extension methods for <see cref="IExecFuncHandler"/>.
    /// </summary>
    public static class ExecFuncHandlerExtensions
    {
        /// <summary>
        /// Invokes an ExecFunc method by reflection.
        /// </summary>
        /// <param name="handler">The handler that implements the method identified by FuncID.</param>
        /// <param name="currentRequirement">The access requirement of the current call.</param>
        /// <param name="args">The input arguments.</param>
        /// <param name="result">The output result.</param>
        public static void InvokeExecFunc(
            this IExecFuncHandler handler,
            ApiAccessRequirement currentRequirement,
            ExecFuncArgs args,
            ExecFuncResult result)
        {
            try
            {
                // Invoke the custom method corresponding to FuncID by reflection
                var method = handler.GetType().GetMethod(args.FuncId);
                if (method == null)
                    throw new MissingMethodException($"Method {args.FuncId} not found.");

                // Get the ExecFuncAccessControlAttribute
                var attr = (ExecFuncAccessControlAttribute?)Attribute.GetCustomAttribute(
                      method, typeof(ExecFuncAccessControlAttribute));

                // When no attribute is present, default to Authenticated
                var required = attr?.AccessRequirement ?? ApiAccessRequirement.Authenticated;

                // Evaluate the access requirement
                if (required == ApiAccessRequirement.Authenticated && currentRequirement == ApiAccessRequirement.Anonymous)
                    throw new UnauthorizedAccessException($"FuncID '{args.FuncId}' requires authentication.");

                method.Invoke(handler, new object[] { args, result });
            }
            catch (Exception ex)
            {
                var rootEx = ex.Unwrap();
                ExceptionDispatchInfo.Capture(rootEx).Throw();  // Re-throw preserving the original stack trace
                throw; // 不會執行到，純粹為了編譯器
            }
        }
    }
}
