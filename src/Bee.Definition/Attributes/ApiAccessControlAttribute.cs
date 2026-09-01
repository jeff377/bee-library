using Bee.Definition.Security;

namespace Bee.Definition.Attributes
{
    /// <summary>
    /// Attribute for annotating API method access control.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = true)]
    public class ApiAccessControlAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of <see cref="ApiAccessControlAttribute"/>.
        /// </summary>
        /// <param name="protectionLevel">The access protection level.</param>
        /// <param name="accessRequirement">Whether login is required.</param>
        public ApiAccessControlAttribute(
            ApiProtectionLevel protectionLevel,
            ApiAccessRequirement accessRequirement = ApiAccessRequirement.Authenticated)
        {
            ProtectionLevel = protectionLevel;
            AccessRequirement = accessRequirement;
        }

        /// <summary>
        /// Gets the access protection level (encoding and encryption requirements).
        /// </summary>
        public ApiProtectionLevel ProtectionLevel { get; }

        /// <summary>
        /// Gets the access authorization requirement (whether login is required).
        /// </summary>
        public ApiAccessRequirement AccessRequirement { get; }

        /// <summary>
        /// Gets or sets whether each call must carry a sequence number the session has not used
        /// before. Defaults to <see cref="ApiReplayProtection.None"/>.
        /// </summary>
        /// <remarks>
        /// A settable property rather than a fourth constructor parameter: adding an optional
        /// parameter to a shipped public constructor is a binary-breaking change, and every caller
        /// of the two-argument form would have to be recompiled.
        /// </remarks>
        public ApiReplayProtection ReplayProtection { get; set; } = ApiReplayProtection.None;
    }

}
