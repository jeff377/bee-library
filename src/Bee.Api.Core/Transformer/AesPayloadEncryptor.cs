using Bee.Base;

namespace Bee.Api.Core
{
    /// <summary>
    /// 使用 AES 的 API 傳輸層資料加密器。
    /// </summary>
    public class AesPayloadEncryptor : IApiPayloadEncryptor
    {
        /// <summary>
        /// 加密演算法的識別字串。
        /// </summary>
        public string EncryptionMethod => "aes-cbc-hmac";

        /// <summary>
        /// 將加密過的位元組資料還原為原始資料。
        /// </summary>
        /// <param name="bytes">加密後的位元組資料。</param>
        /// <param name="encryptionKey">加密金鑰。</param>
        /// <returns>解密後的位元組資料。</returns>
        public byte[] Encrypt(byte[] bytes, byte[] encryptionKey)
        {
            // 如果沒有提供加密金鑰組，則直接返回原始位元組資料
            if (encryptionKey == null || encryptionKey.Length == 0) { return bytes; }
            // 進行 AES-CBC 加密
            AesCbcHmacKeyGenerator.FromCombinedKey(encryptionKey, out var aesKey, out var hmacKey);
            return AesCbcHmacCryptor.Encrypt(bytes, aesKey, hmacKey);
        }

        /// <summary>
        /// 將原始位元組資料進行加密處理。
        /// </summary>
        /// <param name="bytes">原始位元組資料。</param>
        /// <param name="encryptionKey">加密金鑰。</param> 
        /// <returns>加密後的位元組資料。</returns>
        public byte[] Decrypt(byte[] bytes, byte[] encryptionKey)
        {
            // 如果沒有提供加密金鑰組，則直接返回原始位元組資料
            if (encryptionKey == null || encryptionKey.Length == 0) { return bytes; }
            // 進行 AES-CBC 解密
            AesCbcHmacKeyGenerator.FromCombinedKey(encryptionKey, out var aesKey, out var hmacKey);
            return AesCbcHmacCryptor.Decrypt(bytes, aesKey, hmacKey);
        }


    }
}
