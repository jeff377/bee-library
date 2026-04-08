using Bee.Definition;
using System;

namespace Bee.Business
{
    /// <summary>
    /// Attribute for declaring the access requirement of an ExecFunc method.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = true)]
    public class ExecFuncAccessControlAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExecFuncAccessControlAttribute"/> class.
        /// </summary>
        /// <param name="accessRequirement">Whether authentication is required.</param>
        public ExecFuncAccessControlAttribute(ApiAccessRequirement accessRequirement = ApiAccessRequirement.Authenticated)
        {
            AccessRequirement = accessRequirement;
        }

        /// <summary>
        /// Gets the access requirement (whether authentication is required).
        /// </summary>
        public ApiAccessRequirement AccessRequirement { get; }
    }
}
