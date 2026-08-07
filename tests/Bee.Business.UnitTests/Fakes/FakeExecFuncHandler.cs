using Bee.Definition.Collections;
using Bee.Business.Attributes;
using Bee.Definition.Security;

namespace Bee.Business.UnitTests.Fakes
{
    /// <summary>
    /// 測試用 ExecFunc handler，提供涵蓋各種 <see cref="ExecFuncAccessControlAttribute"/> 情境的方法。
    /// </summary>
    public class FakeExecFuncHandler : IExecFuncHandler
    {
        /// <summary>
        /// 標記為 Anonymous。
        /// </summary>
        [ExecFuncAccessControl(ApiAccessRequirement.Anonymous)]
        public static void Anonymous(ExecFuncArgs args, ExecFuncResult result)
        {
            result.Parameters.Add("Called", "Anonymous");
            result.Parameters.Add("FuncId", args.FuncId);
        }

        /// <summary>
        /// 標記為 Authenticated。
        /// </summary>
        [ExecFuncAccessControl(ApiAccessRequirement.Authenticated)]
        public static void Authenticated(ExecFuncArgs args, ExecFuncResult result)
        {
            result.Parameters.Add("Called", "Authenticated");
        }

        /// <summary>
        /// 未標記 attribute。dispatch 為 fail-closed，故無論呼叫端是否已驗證皆應被拒絕。
        /// </summary>
        public static void NoAttribute(ExecFuncArgs args, ExecFuncResult result)
        {
            result.Parameters.Add("Called", "NoAttribute");
        }

        /// <summary>
        /// 標記為 LocalOnly，僅本機（行程內）呼叫可觸達。
        /// </summary>
        [ExecFuncAccessControl(ApiAccessRequirement.Authenticated, LocalOnly = true)]
        public static void LocalOnly(ExecFuncArgs args, ExecFuncResult result)
        {
            result.Parameters.Add("Called", "LocalOnly");
        }

        /// <summary>
        /// 測試例外展開：原始例外被 reflection 包成 <see cref="System.Reflection.TargetInvocationException"/>，
        /// 經由 <c>ExceptionExtensions.Unwrap</c> 應還原為原始型別。
        /// </summary>
        [ExecFuncAccessControl(ApiAccessRequirement.Anonymous)]
        public static void Throws(ExecFuncArgs args, ExecFuncResult result)
        {
            throw new InvalidOperationException("fake-inner-exception");
        }
    }
}
