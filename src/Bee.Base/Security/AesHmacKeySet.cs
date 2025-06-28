using System;
using System.Collections.Generic;
using System.Text;

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
