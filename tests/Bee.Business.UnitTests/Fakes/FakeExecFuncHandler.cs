using Bee.Business.Attributes;
using Bee.Definition;

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
        /// 未標記 attribute，預設需 Authenticated。
        /// </summary>
        public static void NoAttribute(ExecFuncArgs args, ExecFuncResult result)
        {
            result.Parameters.Add("Called", "NoAttribute");
        }

        /// <summary>
        /// 測試例外展開：原始例外被 reflection 包成 <see cref="System.Reflection.TargetInvocationException"/>，
        /// 經由 <c>BaseFunc.UnwrapException</c> 應還原為原始型別。
        /// </summary>
        [ExecFuncAccessControl(ApiAccessRequirement.Anonymous)]
        public static void Throws(ExecFuncArgs args, ExecFuncResult result)
        {
            throw new InvalidOperationException("fake-inner-exception");
        }
    }
}
