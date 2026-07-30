using Bee.Definition.Security;
using Bee.Base;
using Bee.Base.Security;
using Bee.Definition.Identity;

namespace Bee.Business.Providers
{
    /// <summary>
    /// Dynamic encryption key provider that retrieves the session key corresponding to the given AccessToken.
    /// </summary>
    public class DynamicApiEncryptionKeyProvider : IApiEncryptionKeyProvider
    {
        private readonly ISessionInfoService _sessionInfoService;

        /// <summary>
        /// Initializes a new <see cref="DynamicApiEncryptionKeyProvider"/>.
        /// </summary>
        /// <param name="sessionInfoService">The session info access service.</param>
        public DynamicApiEncryptionKeyProvider(ISessionInfoService sessionInfoService)
        {
            _sessionInfoService = sessionInfoService ?? throw new ArgumentNullException(nameof(sessionInfoService));
        }

        /// <summary>
        /// Gets the encryption key for API transmission data.
        /// </summary>
        /// <param name="accessToken">The access token, or <see cref="Guid.Empty"/>.</param>
        /// <returns>A 64-byte combined key (AES + HMAC).</returns>
        public byte[] GetKey(Guid accessToken)
        {
            // If AccessToken is Guid.Empty, throw an unauthorized exception
            if (ValueUtilities.IsEmpty(accessToken))
            {
                throw new UnauthorizedAccessException("Access token is required.");
            }

            var sessionInfo = _sessionInfoService.Get(accessToken);
            return sessionInfo?.ApiEncryptionKey
                ?? throw new UnauthorizedAccessException("Session key not found or expired.");
        }

        /// <summary>
        /// Generates a random encryption key for a newly issued session.
        /// </summary>
        /// <param name="accessToken">The access token of the session being created (unused).</param>
        /// <returns>A 64-byte combined key (AES + HMAC).</returns>
        /// <remarks>
        /// The generated key lives only in <c>SessionInfo.ApiEncryptionKey</c>, so it cannot be
        /// recovered once the cache entry is gone. Deployments that rebuild sessions from
        /// <c>st_session</c> need <see cref="StaticApiEncryptionKeyProvider"/> or
        /// <see cref="DerivedApiEncryptionKeyProvider"/> instead.
        /// </remarks>
        public byte[] GenerateKeyForLogin(Guid accessToken)
        {
            // SessionInfo is created or updated at login and the ApiEncryptionKey is set automatically
            return AesCbcHmacKeyGenerator.GenerateCombinedKey();
        }
    }

}
