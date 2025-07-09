using System;
using Bee.Base;

namespace Bee.Define
{
    /// <summary>
    /// 使用主金鑰進行金鑰資料的加密與解密。
    /// </summary>
    public static class EncryptionKeyProtector
    {
        /// <summary>
        /// 產生新的 AES+HMAC 組合金鑰，並使用主金鑰加密為 Base64 密文。
        /// </summary>
        /// <param name="masterKey">主金鑰（CombinedKey 格式）。</param>
        /// <returns>加密後的 Base64 編碼字串。</returns>
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
        /// 使用主金鑰解密 Base64 密文，還原原始 AES+HMAC 組合金鑰。
        /// </summary>
        /// <param name="masterKey">主金鑰（CombinedKey 格式）。</param>
        /// <param name="base64CipherText">加密後的 Base64 編碼字串。</param>
        /// <returns>還原的原始金鑰位元組陣列。</returns>
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
