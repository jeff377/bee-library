namespace Bee.Definition.Security
{
    /// <summary>
    /// Validates the validity of an access token. Counterpart of
    /// <c>IApiAuthorizationValidator</c> for the post-authorization session-token check.
    /// </summary>
    public interface IAccessTokenValidator
    {
        /// <summary>
        /// Validates whether the specified access token is valid.
        /// </summary>
        /// <param name="accessToken">The access token to validate.</param>
        /// <returns>True if the access token is valid; otherwise, false.</returns>
        bool Validate(Guid accessToken);
    }
}
