using System.Runtime.ExceptionServices;
using Bee.Base.Exceptions;
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
        /// Invokes an ExecFunc method by reflection, treating the call as remote.
        /// </summary>
        /// <param name="handler">The handler that implements the method identified by FuncID.</param>
        /// <param name="currentRequirement">The access requirement of the current call.</param>
        /// <param name="args">The input arguments.</param>
        /// <param name="result">The output result.</param>
        /// <remarks>
        /// Prefer the overload that takes <c>isLocalCall</c>. This one assumes a remote caller, so a
        /// method declaring <see cref="ExecFuncAccessControlAttribute.LocalOnly"/> is always rejected.
        /// </remarks>
        public static void InvokeExecFunc(
            this IExecFuncHandler handler,
            ApiAccessRequirement currentRequirement,
            ExecFuncArgs args,
            ExecFuncResult result)
        {
            handler.InvokeExecFunc(currentRequirement, isLocalCall: false, args, result);
        }

        /// <summary>
        /// Invokes an ExecFunc method by reflection.
        /// </summary>
        /// <param name="handler">The handler that implements the method identified by FuncID.</param>
        /// <param name="currentRequirement">The access requirement of the current call.</param>
        /// <param name="isLocalCall">Whether the call originates in-process rather than from a remote client.</param>
        /// <param name="args">The input arguments.</param>
        /// <param name="result">The output result.</param>
        /// <remarks>
        /// <para>
        /// WARNING: dispatch is fail-closed. A method without
        /// <see cref="ExecFuncAccessControlAttribute"/> is rejected rather than defaulted to
        /// <see cref="ApiAccessRequirement.Authenticated"/>, matching the behaviour of
        /// <c>ApiAccessValidator</c> for ordinary business object methods.
        /// </para>
        /// <para>
        /// The earlier default treated an unmarked method as merely requiring authentication, which
        /// meant a maintenance operation that someone forgot to mark was reachable by every logged-in
        /// user. Rule BEE3003 reports unmarked methods at build time so the stricter runtime check is
        /// not the first place the omission shows up.
        /// </para>
        /// </remarks>
        public static void InvokeExecFunc(
            this IExecFuncHandler handler,
            ApiAccessRequirement currentRequirement,
            bool isLocalCall,
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

                // Fail closed: an undeclared method is not callable at all.
                if (attr == null)
                {
                    throw new UnauthorizedAccessException(
                        $"FuncID '{args.FuncId}' does not declare [ExecFuncAccessControl] and cannot be invoked.");
                }

                // A local-only method must never be reachable from a remote client.
                if (attr.LocalOnly && !isLocalCall)
                    throw new UnauthorizedAccessException($"FuncID '{args.FuncId}' allows local calls only.");

                // Evaluate the access requirement
                if (attr.AccessRequirement == ApiAccessRequirement.Authenticated && currentRequirement == ApiAccessRequirement.Anonymous)
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
