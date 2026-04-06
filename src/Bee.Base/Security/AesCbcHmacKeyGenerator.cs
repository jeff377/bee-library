using System;
using System.Security.Cryptography;

namespace Bee.Base.Security
{
    /// <summary>
    /// Generator for AES-CBC and HMAC keys; generates or restores the combined key (64 bytes) required for encryption.
    /// </summary>
    public static class AesCbcHmacKeyGenerator
    {
        /// <summary>
        /// Generates a new AES key and HMAC key, combined into 64 bytes.
        /// </summary>
        /// <returns>The combined key data (first 32 bytes are the AES key, next 32 bytes are the HMAC key).</returns>
        public static byte[] GenerateCombinedKey()
        {
            var aesKey = new byte[32];
            var hmacKey = new byte[32];

            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(aesKey);
                rng.GetBytes(hmacKey);
            }

            var combined = new byte[64];
            Buffer.BlockCopy(aesKey, 0, combined, 0, 32);
            Buffer.BlockCopy(hmacKey, 0, combined, 32, 32);
            return combined;
        }

        /// <summary>
        /// Generates a new AES and HMAC key pair and returns it as a Base64-encoded string.
        /// </summary>
        /// <returns>The Base64-encoded combined key string.</returns>
        public static string GenerateBase64CombinedKey()
        {
            var combined = GenerateCombinedKey();
            return Convert.ToBase64String(combined);
        }

        /// <summary>
        /// Extracts the AES and HMAC keys from a 64-byte combined key.
        /// </summary>
        /// <param name="combinedKey">The combined key data (must be exactly 64 bytes).</param>
        /// <param name="aesKey">The extracted AES symmetric encryption key.</param>
        /// <param name="hmacKey">The extracted HMAC verification key.</param>
        /// <exception cref="ArgumentException">Thrown when the key length is not 64 bytes.</exception>
        public static void FromCombinedKey(byte[] combinedKey, out byte[] aesKey, out byte[] hmacKey)
        {
            if (combinedKey == null || combinedKey.Length != 64)
                throw new ArgumentException("Combined key must be 64 bytes.");

            aesKey = new byte[32];
            hmacKey = new byte[32];
            Buffer.BlockCopy(combinedKey, 0, aesKey, 0, 32);
            Buffer.BlockCopy(combinedKey, 32, hmacKey, 0, 32);
        }

        /// <summary>
        /// Extracts the AES and HMAC keys from a Base64-encoded combined key string.
        /// </summary>
        /// <param name="base64">The Base64-encoded key string.</param>
        /// <param name="aesKey">The extracted AES symmetric encryption key.</param>
        /// <param name="hmacKey">The extracted HMAC verification key.</param>
        /// <exception cref="ArgumentException">Thrown when the key length is not 64 bytes.</exception>
        public static void FromBase64CombinedKey(string base64, out byte[] aesKey, out byte[] hmacKey)
        {
            var combinedKey = Convert.FromBase64String(base64);
            FromCombinedKey(combinedKey, out aesKey, out hmacKey);
        }
    }
}
