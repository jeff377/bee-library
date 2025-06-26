using System;
using System.Security.Cryptography;

namespace Bee.Base
{
    /// <summary>
    /// AES-CBC 與 HMAC 金鑰的產生器，用於產生或還原加密所需的組合金鑰（64 bytes）。
    /// </summary>
    public static class AesCbcHmacKeyGenerator
    {
        /// <summary>
        /// 建立一組新的 AES 金鑰與 HMAC 金鑰，合併為 64 bytes。
        /// </summary>
        /// <returns>合併後的金鑰資料（前 32 bytes 為 AES Key，後 32 bytes 為 HMAC Key）。</returns>
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
        /// 建立一組新的 AES 與 HMAC 金鑰，並以 Base64 編碼字串回傳。
        /// </summary>
        /// <returns>Base64 編碼的合併金鑰字串。</returns>
        public static string GenerateBase64CombinedKey()
        {
            var combined = GenerateCombinedKey();
            return Convert.ToBase64String(combined);
        }

        /// <summary>
        /// 從 64 bytes 的合併金鑰中還原 AES 與 HMAC 金鑰。
        /// </summary>
        /// <param name="combinedKey">合併後的金鑰資料（長度必須為 64 bytes）。</param>
        /// <param name="aesKey">還原出的 AES 對稱加密金鑰。</param>
        /// <param name="hmacKey">還原出的 HMAC 驗證用金鑰。</param>
        /// <exception cref="ArgumentException">若金鑰長度不為 64 bytes 時擲出。</exception>
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
        /// 從 Base64 編碼的合併金鑰還原 AES 與 HMAC 金鑰。
        /// </summary>
        /// <param name="base64">Base64 編碼的金鑰字串。</param>
        /// <param name="aesKey">還原出的 AES 對稱加密金鑰。</param>
        /// <param name="hmacKey">還原出的 HMAC 驗證用金鑰。</param>
        /// <exception cref="ArgumentException">若金鑰長度不為 64 bytes 時擲出。</exception>
        public static void FromBase64CombinedKey(string base64, out byte[] aesKey, out byte[] hmacKey)
        {
            var combinedKey = Convert.FromBase64String(base64);
            FromCombinedKey(combinedKey, out aesKey, out hmacKey);
        }
    }
}
