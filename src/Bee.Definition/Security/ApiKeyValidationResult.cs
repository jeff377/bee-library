namespace Bee.Definition.Security
{
    /// <summary>
    /// Result of checking one call's <c>X-Api-Key</c>: the gate outcome plus the identity of the
    /// calling application when the key was accepted.
    /// </summary>
    /// <remarks>
    /// Produced once per call by <see cref="IApiKeyValidator"/> and carried on the authorization
    /// context, so the authorization decision, the ping response and the audit record all read the
    /// same verdict instead of each repeating the lookup.
    /// </remarks>
    public class ApiKeyValidationResult
    {
        /// <summary>
        /// A result for a call the gate never saw (an in-process call).
        /// </summary>
        public static readonly ApiKeyValidationResult NotChecked = new(ApiKeyStatus.NotChecked);

        /// <summary>
        /// Initializes a new <see cref="ApiKeyValidationResult"/> carrying no caller identity.
        /// </summary>
        /// <param name="status">The gate outcome.</param>
        public ApiKeyValidationResult(ApiKeyStatus status)
        {
            Status = status;
        }

        /// <summary>
        /// Initializes a new <see cref="ApiKeyValidationResult"/> for an accepted key.
        /// </summary>
        /// <param name="status">The gate outcome.</param>
        /// <param name="sysId">The key identifier of the calling application.</param>
        /// <param name="sysName">The display name of the calling application.</param>
        public ApiKeyValidationResult(ApiKeyStatus status, string sysId, string sysName)
        {
            Status = status;
            SysId = sysId;
            SysName = sysName;
        }

        /// <summary>
        /// Gets the gate outcome.
        /// </summary>
        public ApiKeyStatus Status { get; }

        /// <summary>
        /// Gets the key identifier the caller presented, or an empty string when no key was supplied
        /// or the value did not parse.
        /// </summary>
        /// <remarks>
        /// Populated for rejections as well as successes, so an audit record can name the attempt.
        /// Safe to log: it is the non-secret leading segment of the key, and its character set is
        /// validated before it reaches here.
        /// </remarks>
        public string SysId { get; } = string.Empty;

        /// <summary>
        /// Gets the display name of the calling application, or an empty string when the key was not
        /// accepted.
        /// </summary>
        public string SysName { get; } = string.Empty;

        /// <summary>
        /// Gets a value indicating whether the call may proceed as far as the key gate is concerned.
        /// </summary>
        /// <remarks>
        /// True for <see cref="ApiKeyStatus.Valid"/>, and for <see cref="ApiKeyStatus.NotChecked"/> /
        /// <see cref="ApiKeyStatus.NotConfigured"/> where the gate is not in force for this call.
        /// </remarks>
        public bool IsAccepted =>
            Status is ApiKeyStatus.Valid or ApiKeyStatus.NotChecked or ApiKeyStatus.NotConfigured;
    }
}
