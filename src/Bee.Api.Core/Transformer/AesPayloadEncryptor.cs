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
        /// <param name="keySet">加密金鑰組。</param>
        /// <returns>解密後的位元組資料。</returns>
        public byte[] Encrypt(byte[] bytes, EncryptionKeySet keySet)
        {
            // 如果沒有提供加密金鑰組，則直接返回原始位元組資料
            if (keySet == null) { return bytes; }
            // 進行 AES-CBC 加密，並附加 HMAC 驗證碼
            var aesHmacKeySet = keySet as AesHmacKeySet;
            return AesCbcHmacCryptor.Encrypt(bytes, aesHmacKeySet.AesKey, aesHmacKeySet.HmacKey);
        }

        /// <summary>
        /// 將原始位元組資料進行加密處理。
        /// </summary>
        /// <param name="bytes">原始位元組資料。</param>
        /// <param name="keySet">加密金鑰組。</param> 
        /// <returns>加密後的位元組資料。</returns>
        public byte[] Decrypt(byte[] bytes, EncryptionKeySet keySet)
        {
            // 如果沒有提供加密金鑰組，則直接返回原始位元組資料
            if (keySet == null) { return bytes; }
            // 使用 AES 解密加密後的位元組資料
            var aesHmacKeySet = keySet as AesHmacKeySet;
            return AesCbcHmacCryptor.Decrypt(bytes, aesHmacKeySet.AesKey, aesHmacKeySet.HmacKey);
        }


    }
}
