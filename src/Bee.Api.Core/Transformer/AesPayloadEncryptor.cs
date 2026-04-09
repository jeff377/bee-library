using System.Security.Cryptography;
using Bee.Core;
using Bee.Core.Security;

namespace Bee.Api.Core.Transformer
{
    /// <summary>
    /// AES-based API transport layer data encryptor.
    /// </summary>
    public class AesPayloadEncryptor : IApiPayloadEncryptor
    {
        /// <summary>
        /// Gets the identifier string for the encryption algorithm.
        /// </summary>
        public string EncryptionMethod => "aes-cbc-hmac";

        /// <summary>
        /// Encrypts the specified byte data.
        /// </summary>
        /// <param name="bytes">The raw byte data to encrypt.</param>
        /// <param name="encryptionKey">The encryption key.</param>
        /// <returns>The encrypted byte data.</returns>
        /// <exception cref="CryptographicException">Thrown when the encryption key is null or empty.</exception>
        public byte[] Encrypt(byte[] bytes, byte[] encryptionKey)
        {
            if (encryptionKey == null || encryptionKey.Length == 0)
                throw new CryptographicException("Encryption key must not be null or empty.");

            AesCbcHmacKeyGenerator.FromCombinedKey(encryptionKey, out var aesKey, out var hmacKey);
            return AesCbcHmacCryptor.Encrypt(bytes, aesKey, hmacKey);
        }

        /// <summary>
        /// Decrypts the specified encrypted byte data back to its original form.
        /// </summary>
        /// <param name="bytes">The encrypted byte data.</param>
        /// <param name="encryptionKey">The encryption key.</param>
        /// <returns>The decrypted byte data.</returns>
        /// <exception cref="CryptographicException">Thrown when the encryption key is null or empty.</exception>
        public byte[] Decrypt(byte[] bytes, byte[] encryptionKey)
        {
            if (encryptionKey == null || encryptionKey.Length == 0)
                throw new CryptographicException("Encryption key must not be null or empty.");

            AesCbcHmacKeyGenerator.FromCombinedKey(encryptionKey, out var aesKey, out var hmacKey);
            return AesCbcHmacCryptor.Decrypt(bytes, aesKey, hmacKey);
        }
    }
}
