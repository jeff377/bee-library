namespace Bee.Definition.Security
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
        /// Generates the key for a newly issued session; may be shared, randomly generated,
        /// or derived from the access token.
        /// </summary>
        /// <param name="accessToken">The access token of the session being created.</param>
        /// <returns>A 64-byte combined key (AES + HMAC).</returns>
        /// <remarks>
        /// The access token must already have been generated when this is called, because a
        /// deriving implementation uses it as key material. Implementations that derive the key
        /// must return the same value <see cref="GetKey"/> returns for that token, so a session
        /// rebuilt from the database recovers a working key without persisting it.
        /// </remarks>
        byte[] GenerateKeyForLogin(Guid accessToken);
    }
}
