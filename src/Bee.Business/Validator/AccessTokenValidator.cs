using Bee.Definition.Security;
using Bee.Base;
using Bee.Definition;

namespace Bee.Business.Validator
{
    /// <summary>
    /// Default implementation of <see cref="IAccessTokenValidator"/>.
    /// Validates the access token against the session info store.
    /// </summary>
    public class AccessTokenValidator : IAccessTokenValidator
    {
        /// <summary>
        /// Validates whether the specified access token is valid.
        /// </summary>
        /// <param name="accessToken">The access token to validate.</param>
        /// <returns>True if the access token is valid; otherwise, false.</returns>
        public bool Validate(Guid accessToken)
        {
            // If AccessToken is Guid.Empty, throw an unauthorized exception
            if (ValueUtilities.IsEmpty(accessToken))
            {
                throw new UnauthorizedAccessException("Access token is required.");
            }

            var sessionInfo = BackendInfo.SessionInfoService.Get(accessToken);
            if (sessionInfo == null)
                throw new UnauthorizedAccessException("Session key not found or expired.");

            if (sessionInfo.ExpiredAt < DateTime.UtcNow)
                throw new UnauthorizedAccessException("Session has expired.");

            return sessionInfo.AccessToken == accessToken;
        }
    }
}
