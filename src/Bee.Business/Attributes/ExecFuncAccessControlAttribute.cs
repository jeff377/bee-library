using Bee.Definition.Security;

namespace Bee.Business.Attributes
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

        /// <summary>
        /// Gets or sets whether the method may only be invoked by a local (in-process) caller.
        /// </summary>
        /// <remarks>
        /// Set this on maintenance operations that must never be reachable from a remote client —
        /// schema upgrades, connection probes, and anything else that acts on a caller-supplied
        /// database target. It is the ExecFunc counterpart of
        /// <see cref="ApiProtectionLevel.LocalOnly"/>: authentication alone is not a meaningful
        /// gate for these, because every authenticated user would otherwise pass it.
        /// </remarks>
        public bool LocalOnly { get; set; }
    }
}
