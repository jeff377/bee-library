using System;

namespace Bee.Define.Security
{
    /// <summary>
    /// Interface for an API encryption key provider.
    /// Supports both a static shared key and per-session keys generated at login.
    /// </summary>
    public interface IApiEncryptionKeyProvider
    {
        /// <summary>
        /// Gets the encryption key for API data transmission.
        /// </summary>
        /// <param name="accessToken">The access token, or <see cref="Guid.Empty"/>.</param>
        /// <returns>A 64-byte combined key (AES + HMAC).</returns>
        byte[] GetKey(Guid accessToken);

        /// <summary>
        /// Generates a key during login; may be a shared or randomly generated key.
        /// </summary>
        /// <returns>A 64-byte combined key (AES + HMAC).</returns>
        byte[] GenerateKeyForLogin();
    }
}
