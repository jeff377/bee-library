using System;

namespace Bee.Definition.Security
{
    /// <summary>
    /// Interface for an access token validation provider, used to verify the validity of an access token.
    /// </summary>
    public interface IAccessTokenValidationProvider
    {
        /// <summary>
        /// Validates whether the specified access token is valid.
        /// </summary>
        /// <param name="accessToken">The access token to validate.</param>
        /// <returns>True if the access token is valid; otherwise, false.</returns>
        bool ValidateAccessToken(Guid accessToken);
    }
}
