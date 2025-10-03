using Bee.Define;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bee.Business
{
    /// <summary>
    /// 標註 ExecFunc 方法的存取授權需求屬性。
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = true)]
    public class ExecFuncAccessControlAttribute : Attribute
    {
        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="accessRequirement">是否需要登入。</param>
        public ExecFuncAccessControlAttribute(ApiAccessRequirement accessRequirement = ApiAccessRequirement.Authenticated)
        {
            AccessRequirement = accessRequirement;
        }

        /// <summary>
        /// 存取授權需求（是否需要登入）。
        /// </summary>
        public ApiAccessRequirement AccessRequirement { get; }
    }
}
