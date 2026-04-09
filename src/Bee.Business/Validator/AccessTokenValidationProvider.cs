using Bee.Definition.Security;
using Bee.Core;
using Bee.Definition;
using System;

namespace Bee.Business.Validator
{
    /// <summary>
    /// Provider for validating the validity of an AccessToken.
    /// </summary>
    public class AccessTokenValidationProvider : IAccessTokenValidationProvider
    {
        /// <summary>
        /// Validates whether the specified AccessToken is valid.
        /// </summary>
        /// <param name="accessToken">The access token to validate.</param>
        /// <returns>True if the AccessToken is valid; otherwise, false.</returns>
        public bool ValidateAccessToken(Guid accessToken)
        {
            // If AccessToken is Guid.Empty, throw an unauthorized exception
            if (BaseFunc.IsEmpty(accessToken))
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
