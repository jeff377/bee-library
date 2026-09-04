namespace Bee.Definition.Security
{
    /// <summary>
    /// Validates the <c>X-Api-Key</c> header value against the issued API keys. Counterpart of
    /// <see cref="IAccessTokenValidator"/> for the application-identity layer: this decides which
    /// application is calling, the access token decides which user.
    /// </summary>
    /// <remarks>
    /// Resolved from the host container per call by the API controller, because the check needs
    /// backend services (the key cache, and through it the database) that the static
    /// <c>AuthorizationValidator</c> cannot reach.
    /// <para>
    /// WARNING: only a definitive answer from the store may produce
    /// <see cref="ApiKeyStatus.NotConfigured"/>. A lookup failure must propagate rather than being
    /// reported as "this deployment has no keys", and callers must treat a thrown exception as a
    /// rejection — otherwise a database outage would silently reopen the gate. The one exception is
    /// the connectivity probe (<c>System.Ping</c>), which the authorization validator exempts from
    /// the key requirement so health checks still answer while the database is down.
    /// </para>
    /// </remarks>
    public interface IApiKeyValidator
    {
        /// <summary>
        /// Validates the supplied plaintext API key.
        /// </summary>
        /// <param name="apiKey">
        /// The raw <c>X-Api-Key</c> header value, or <c>null</c> / empty when the request carried
        /// none.
        /// </param>
        /// <returns>
        /// The verdict, including the calling application's identity when the key was accepted.
        /// </returns>
        ApiKeyValidationResult Validate(string? apiKey);
    }
}
