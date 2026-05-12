using Bee.Definition.Security;

namespace Bee.Business.Providers
{
    /// <summary>
    /// Static encryption key provider that always returns the shared key supplied at construction time.
    /// </summary>
    public class StaticApiEncryptionKeyProvider : IApiEncryptionKeyProvider
    {
        private readonly byte[] _apiEncryptionKey;

        /// <summary>
        /// Initializes a new <see cref="StaticApiEncryptionKeyProvider"/>.
        /// </summary>
        /// <param name="apiEncryptionKey">The shared API encryption key (64-byte combined AES + HMAC).</param>
        public StaticApiEncryptionKeyProvider(byte[] apiEncryptionKey)
        {
            ArgumentNullException.ThrowIfNull(apiEncryptionKey);
            if (apiEncryptionKey.Length == 0)
                throw new ArgumentException("ApiEncryptionKey cannot be empty.", nameof(apiEncryptionKey));
            _apiEncryptionKey = apiEncryptionKey;
        }

        /// <summary>
        /// Gets the encryption key for API transmission data.
        /// </summary>
        /// <param name="accessToken">The access token, or <see cref="Guid.Empty"/>.</param>
        /// <returns>A 64-byte combined key (AES + HMAC).</returns>
        public byte[] GetKey(Guid accessToken) => _apiEncryptionKey;

        /// <summary>
        /// Generates an encryption key at login time (may be shared or random).
        /// </summary>
        /// <returns>A 64-byte combined key (AES + HMAC).</returns>
        public byte[] GenerateKeyForLogin() => _apiEncryptionKey;
    }

}
