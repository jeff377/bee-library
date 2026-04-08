using Bee.Definition.Security;
using Bee.Definition;
using System;

namespace Bee.Business.Provider
{
    /// <summary>
    /// Static encryption key provider that always returns the shared key from the backend configuration.
    /// </summary>
    public class StaticApiEncryptionKeyProvider : IApiEncryptionKeyProvider
    {
        /// <summary>
        /// Gets the encryption key for API transmission data.
        /// </summary>
        /// <param name="accessToken">The access token, or <see cref="Guid.Empty"/>.</param>
        /// <returns>A 64-byte combined key (AES + HMAC).</returns>
        public byte[] GetKey(Guid accessToken)
        {
            return BackendInfo.ApiEncryptionKey
                ?? throw new InvalidOperationException("BackendInfo.ApiEncryptionKey is not initialized.");
        }

        /// <summary>
        /// Generates an encryption key at login time (may be shared or random).
        /// </summary>
        /// <returns>A 64-byte combined key (AES + HMAC).</returns>
        public byte[] GenerateKeyForLogin()
        {
            // Static key provider does not generate a new key at login; returns the shared key instead
            return GetKey(Guid.Empty);
        }
    }

}
