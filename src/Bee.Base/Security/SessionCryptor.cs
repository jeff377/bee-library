using System;
using System.Security.Cryptography;

namespace Bee.Base
{
    /// <summary>
    /// 提供工作階段使用的 AES 金鑰與初始向量產生與解析功能。
    /// </summary>
    public static class SessionCryptor
    {
        /// <summary>
        /// 產生 AES 金鑰與初始向量（IV），並合併為 Base64 編碼的 SessionKey。
        /// 格式為：Base64(32-byte Key + 16-byte IV)。
        /// </summary>
        public static string GenerateSessionKey()
        {
            using (var aes = Aes.Create())
            {
                aes.KeySize = 256;
                aes.GenerateKey();
                aes.GenerateIV();

                byte[] sessionKeyBytes = new byte[aes.Key.Length + aes.IV.Length];
                Buffer.BlockCopy(aes.Key, 0, sessionKeyBytes, 0, aes.Key.Length);
                Buffer.BlockCopy(aes.IV, 0, sessionKeyBytes, aes.Key.Length, aes.IV.Length);

                return Convert.ToBase64String(sessionKeyBytes);
            }
        }

        /// <summary>
        /// 從 Base64 格式的 SessionKey 中解析出 AES 的金鑰與 IV。
        /// </summary>
        /// <param name="sessionKeyBase64">Base64 編碼的 SessionKey。</param>
        /// <param name="key">輸出解析後的 AES 金鑰。</param>
        /// <param name="iv">輸出解析後的初始向量。</param>
        public static void ParseSessionKey(string sessionKeyBase64, out byte[] key, out byte[] iv)
        {
            byte[] allBytes = Convert.FromBase64String(sessionKeyBase64);
            key = new byte[32]; // AES-256
            iv = new byte[16];  // 128-bit IV

            Buffer.BlockCopy(allBytes, 0, key, 0, key.Length);
            Buffer.BlockCopy(allBytes, key.Length, iv, 0, iv.Length);
        }
    }
}
