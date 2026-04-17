using Bee.Definition.Settings;
using Bee.Base;
using Bee.Business.Attributes;
using Bee.Business.BusinessObjects;
using Bee.Definition;
using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;

namespace Bee.Business
{
    /// <summary>
    /// Shared utility library for business logic.
    /// </summary>
    public static class BusinessFunc
    {
        /// <summary>
        /// Gets the database item for the specified database identifier.
        /// </summary>
        /// <param name="databaseId">The database identifier.</param>
        public static DatabaseItem GetDatabaseItem(string databaseId)
        {
            if (StrFunc.IsEmpty(databaseId))
                throw new ArgumentNullException(nameof(databaseId));

            var settings = BackendInfo.DefineAccess.GetDatabaseSettings();
            if (!settings.Items!.Contains(databaseId))
                throw new KeyNotFoundException($"{nameof(databaseId)} '{databaseId}' not found.");

            return settings.Items[databaseId];
        }

        /// <summary>
        /// Invokes an ExecFunc method by reflection.
        /// </summary>
        /// <param name="execFunc">The handler that implements the method identified by FuncID.</param>
        /// <param name="currentRequirement">The access requirement of the current call.</param>
        /// <param name="args">The input arguments.</param>
        /// <param name="result">The output result.</param>
        public static void InvokeExecFunc(
            IExecFuncHandler execFunc,
            ApiAccessRequirement currentRequirement,
            ExecFuncArgs args,
            ExecFuncResult result)
        {
            try
            {
                // Invoke the custom method corresponding to FuncID by reflection
                var method = execFunc.GetType().GetMethod(args.FuncId);
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

                method.Invoke(execFunc, new object[] { args, result });
            }
            catch (Exception ex)
            {
                var rootEx = BaseFunc.UnwrapException(ex);
                ExceptionDispatchInfo.Capture(rootEx).Throw();  // Re-throw preserving the original stack trace
                throw; // 不會執行到，純粹為了編譯器
            }
        }


    }
}
