using Bee.Base;

namespace Bee.Api.Core
{
    /// <summary>
    /// 使用 AES 的 API 傳輸層資料加密器。
    /// </summary>
    public class TAesEncryptor : IApiPayloadEncryptor
    {
        /// <summary>
        /// 加密演算法的識別字串。
        /// </summary>
        public string EncryptionMethod => "aes";

        /// <summary>
        /// 將原始位元組資料進行加密處理。
        /// </summary>
        /// <param name="bytes">原始位元組資料。</param>
        /// <returns>加密後的位元組資料。</returns>
        public byte[] Decrypt(byte[] bytes)
        {
            return CryptoFunc.AesEncrypt(bytes);
        }

        /// <summary>
        /// 將加密過的位元組資料還原為原始資料。
        /// </summary>
        /// <param name="bytes">加密後的位元組資料。</param>
        /// <returns>解密後的位元組資料。</returns>
        public byte[] Encrypt(byte[] bytes)
        {
            return CryptoFunc.AesDecrypt(bytes);
        }
    }
}
