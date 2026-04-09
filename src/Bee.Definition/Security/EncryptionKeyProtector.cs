using System;
using Bee.Base;
using Bee.Base.Security;

namespace Bee.Definition.Security
{
    /// <summary>
    /// Encrypts and decrypts key data using the master key.
    /// </summary>
    public static class EncryptionKeyProtector
    {
        /// <summary>
        /// Generates a new AES+HMAC combined key and encrypts it with the master key, returning a Base64 ciphertext.
        /// </summary>
        /// <param name="masterKey">The master key in CombinedKey format.</param>
        /// <returns>The encrypted Base64-encoded string.</returns>
        public static string GenerateEncryptedKey(byte[] masterKey)
        {
            if (masterKey == null || masterKey.Length == 0)
                throw new ArgumentException("Master key is null or empty.", nameof(masterKey));

            AesCbcHmacKeyGenerator.FromCombinedKey(masterKey, out var aesKey, out var hmacKey);
            byte[] plainKey = AesCbcHmacKeyGenerator.GenerateCombinedKey();
            byte[] encrypted = AesCbcHmacCryptor.Encrypt(plainKey, aesKey, hmacKey);
            return Convert.ToBase64String(encrypted);
        }

        /// <summary>
        /// Decrypts a Base64 ciphertext using the master key and restores the original AES+HMAC combined key.
        /// </summary>
        /// <param name="masterKey">The master key in CombinedKey format.</param>
        /// <param name="base64CipherText">The encrypted Base64-encoded string.</param>
        /// <returns>The restored original key as a byte array.</returns>
        public static byte[] DecryptEncryptedKey(byte[] masterKey, string base64CipherText)
        {
            if (masterKey == null || masterKey.Length == 0)
                throw new ArgumentException("Master key is null or empty.", nameof(masterKey));

            if (string.IsNullOrWhiteSpace(base64CipherText))
                throw new ArgumentException("Cipher text is null or empty.", nameof(base64CipherText));

            AesCbcHmacKeyGenerator.FromCombinedKey(masterKey, out var aesKey, out var hmacKey);
            byte[] encryptedBytes = Convert.FromBase64String(base64CipherText);
            return AesCbcHmacCryptor.Decrypt(encryptedBytes, aesKey, hmacKey);
        }
    }
}
