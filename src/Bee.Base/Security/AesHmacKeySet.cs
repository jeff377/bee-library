using System;

namespace Bee.Base
{
    /// <summary>
    /// AES-CBC 加密與 HMAC-SHA256 驗證所使用的金鑰組。
    /// </summary>
    /// <remarks>
    /// 此金鑰組提供兩組 256-bit 金鑰，分別用於資料的加密與完整性驗證：
    /// - <see cref="AesKey"/>：用於 AES-CBC 加密（需搭配隨機 IV）
    /// - <see cref="HmacKey"/>：用於 HMAC-SHA256 驗證（防止資料被竄改）
    /// </remarks>
    public class AesHmacKeySet : EncryptionKeySet
    {
        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="aesKey">AES 對稱加密金鑰（256-bit）。</param>
        /// <param name="hmacKey">HMAC 驗證金鑰（256-bit）。</param>
        public AesHmacKeySet(byte[] aesKey, byte[] hmacKey)
        {
            AesKey = aesKey ?? throw new ArgumentNullException(nameof(aesKey), "AES Key cannot be null.");
            HmacKey = hmacKey ?? throw new ArgumentNullException(nameof(hmacKey), "HMAC Key cannot be null.");
        }

        /// <summary>
        /// 建構函式，從 Base64 編碼的組合金鑰還原 AES 與 HMAC 金鑰。
        /// </summary>
        /// <param name="base64CombinedKey">Base64 編碼的組合金鑰。</param>
        public AesHmacKeySet(string base64CombinedKey)
        {
            if (string.IsNullOrEmpty(base64CombinedKey))
                throw new ArgumentNullException(nameof(base64CombinedKey), "Base64 combined key cannot be null or empty.");
            byte[] combinedKey = Convert.FromBase64String(base64CombinedKey);
            if (combinedKey.Length != 64)
                throw new ArgumentException("Combined key must be 64 bytes when decoded from Base64.", nameof(base64CombinedKey));

            AesCbcHmacKeyGenerator.FromBase64CombinedKey(base64CombinedKey, out var aesKey, out var hmacKey);
            AesKey = aesKey;
            HmacKey = hmacKey;
        }

        /// <summary>
        /// 加密演算法代碼。
        /// </summary>
        /// <remarks>此屬性固定為 "aes-cbc-hmac"，可用於加密提供者的註冊與選擇。</remarks>
        public override string Algorithm => "aes-cbc-hmac";

        /// <summary>
        /// AES 對稱加密金鑰（256-bit）。
        /// </summary>
        /// <remarks>
        /// 此金鑰用於執行 AES-CBC 模式的資料加密。建議長度為 32 個位元組（256-bit）。
        /// </remarks>
        public byte[] AesKey { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// HMAC 驗證金鑰（256-bit）。
        /// </summary>
        /// <remarks>
        /// 此金鑰用於產生與驗證 HMAC-SHA256 驗證碼，確保資料未遭竄改。
        /// 建議長度為 32 個位元組（256-bit）。
        /// </remarks>
        public byte[] HmacKey { get; set; } = Array.Empty<byte>();
    }
}
