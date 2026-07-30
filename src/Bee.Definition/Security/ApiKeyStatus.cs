namespace Bee.Definition.Security
{
    /// <summary>
    /// Outcome of the <c>X-Api-Key</c> check for one API call. Identifies the calling application;
    /// it is not user authentication, which stays with the Bearer access token.
    /// </summary>
    public enum ApiKeyStatus
    {
        /// <summary>
        /// The key gate did not run for this call — an in-process call carries no
        /// <c>X-Api-Key</c> header, so there is nothing to check.
        /// </summary>
        NotChecked = 0,

        /// <summary>
        /// The deployment has no enabled API key, so the gate is not in force: any non-empty key
        /// passes. Signals "the gate is not closed yet", which the startup warning also reports.
        /// </summary>
        NotConfigured = 1,

        /// <summary>
        /// The gate is in force but the request carried no <c>X-Api-Key</c> header.
        /// </summary>
        NotProvided = 2,

        /// <summary>
        /// The gate is in force and the supplied key was rejected. Deliberately merges every
        /// rejection reason (malformed, unknown, disabled, expired) so the response cannot be used
        /// to probe which keys exist; the reasons are separated in the audit record only.
        /// </summary>
        Invalid = 3,

        /// <summary>
        /// The gate is in force and the supplied key identifies an enabled, unexpired application.
        /// </summary>
        Valid = 4,
    }
}
