using System.Security.Cryptography;
using System.Text;
using Bee.Base;
using Bee.Definition.Security;

namespace Bee.Business.Providers
{
    /// <summary>
    /// Encryption key provider that derives a per-session key from a root key and the access
    /// token, so the key never has to be stored.
    /// </summary>
    /// <remarks>
    /// Unlike <see cref="DynamicApiEncryptionKeyProvider"/>, which generates a random key held
    /// only in the session cache, this provider recomputes the same key from the access token on
    /// demand. That makes the key recoverable after the cache entry is evicted and identical on
    /// every node of a multi-node deployment, which is what lets a session be rebuilt from its
    /// database seed without persisting key material.
    ///
    /// The trade-off is that the key is deterministic for a given token rather than random per
    /// login. That is acceptable because the access token is an unpredictable GUID and the root
    /// key never leaves the server; rotating the root key invalidates every live session.
    /// </remarks>
    public class DerivedApiEncryptionKeyProvider : IApiEncryptionKeyProvider
    {
        private const int CombinedKeyLength = 64;

        // Domain separation labels. Keeping them fixed is what makes the derivation reproducible
        // across processes and releases — changing either invalidates every live session.
        private static readonly byte[] s_infoPrefix = Encoding.UTF8.GetBytes("bee-api-session-key");
        private static readonly byte[] s_rootKeyInfo = Encoding.UTF8.GetBytes("bee-api-encryption-root-key");

        private readonly byte[] _rootKey;

        /// <summary>
        /// Initializes a new <see cref="DerivedApiEncryptionKeyProvider"/>.
        /// </summary>
        /// <param name="rootKey">
        /// The root key material, taken from <see cref="Bee.Definition.Settings.SecurityKeySettings.ApiEncryptionKey"/>.
        /// </param>
        /// <exception cref="ArgumentException">Thrown when the root key is empty.</exception>
        public DerivedApiEncryptionKeyProvider(byte[] rootKey)
        {
            ArgumentNullException.ThrowIfNull(rootKey);
            if (rootKey.Length == 0)
            {
                throw new ArgumentException(
                    "ApiEncryptionKey must be configured to use DerivedApiEncryptionKeyProvider.",
                    nameof(rootKey));
            }
            _rootKey = rootKey;
        }

        /// <summary>
        /// Creates a provider whose root key is derived from the master key, for deployments that
        /// have not configured <see cref="Bee.Definition.Settings.SecurityKeySettings.ApiEncryptionKey"/>.
        /// </summary>
        /// <param name="masterKey">The deployment's master key.</param>
        /// <returns>A provider rooted at a key derived from <paramref name="masterKey"/>.</returns>
        /// <remarks>
        /// The master key is used only as HKDF input material under its own domain label, never
        /// as a payload key itself, and the value it produces is a distinct key that cannot be
        /// worked back to it. This keeps the provider usable out of the box: an unconfigured
        /// deployment gets a root key that is stable across restarts and identical on every node
        /// sharing the master key, which is what session rebuild needs. Configuring
        /// <c>ApiEncryptionKey</c> explicitly takes precedence and keeps the two keys separate.
        /// </remarks>
        public static DerivedApiEncryptionKeyProvider FromMasterKey(byte[] masterKey)
        {
            ArgumentNullException.ThrowIfNull(masterKey);
            if (masterKey.Length == 0)
            {
                throw new ArgumentException(
                    "Master key is required to derive an API encryption root key.",
                    nameof(masterKey));
            }

            var rootKey = HKDF.DeriveKey(
                HashAlgorithmName.SHA256,
                ikm: masterKey,
                outputLength: CombinedKeyLength,
                salt: null,
                info: s_rootKeyInfo);
            return new DerivedApiEncryptionKeyProvider(rootKey);
        }

        /// <summary>
        /// Gets the encryption key for API transmission data.
        /// </summary>
        /// <param name="accessToken">The access token of the session.</param>
        /// <returns>A 64-byte combined key (AES + HMAC).</returns>
        public byte[] GetKey(Guid accessToken)
        {
            if (ValueUtilities.IsEmpty(accessToken))
            {
                throw new UnauthorizedAccessException("Access token is required.");
            }
            return Derive(accessToken);
        }

        /// <summary>
        /// Generates the key for a newly issued session by deriving it from the access token.
        /// </summary>
        /// <param name="accessToken">The access token of the session being created.</param>
        /// <returns>A 64-byte combined key (AES + HMAC).</returns>
        /// <remarks>
        /// Returns the same value <see cref="GetKey"/> returns for the same token, so the caller
        /// must generate the access token before calling this.
        /// </remarks>
        public byte[] GenerateKeyForLogin(Guid accessToken)
        {
            if (ValueUtilities.IsEmpty(accessToken))
            {
                throw new ArgumentException(
                    "Access token must be generated before deriving the session key.",
                    nameof(accessToken));
            }
            return Derive(accessToken);
        }

        /// <summary>
        /// Always <c>true</c>: the key is recomputed from the access token, so it survives cache
        /// eviction — this is the property session rebuild is built on.
        /// </summary>
        public bool SupportsSessionRebuild => true;

        /// <summary>
        /// Derives the combined key for the given access token via HKDF-SHA256.
        /// </summary>
        private byte[] Derive(Guid accessToken)
        {
            var tokenBytes = accessToken.ToByteArray();
            var info = new byte[s_infoPrefix.Length + tokenBytes.Length];
            Buffer.BlockCopy(s_infoPrefix, 0, info, 0, s_infoPrefix.Length);
            Buffer.BlockCopy(tokenBytes, 0, info, s_infoPrefix.Length, tokenBytes.Length);

            return HKDF.DeriveKey(
                HashAlgorithmName.SHA256,
                ikm: _rootKey,
                outputLength: CombinedKeyLength,
                salt: null,
                info: info);
        }
    }
}
