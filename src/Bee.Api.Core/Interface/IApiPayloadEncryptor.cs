namespace Bee.Api.Core
{
    /// <summary>
    /// API 傳輸層資料加解密策略介面。
    /// 提供位元組資料的加密與解密功能，用以保護傳輸過程中的資料安全。
    /// </summary>
    public interface IApiPayloadEncryptor
    {
        /// <summary>
        /// 加密演算法的識別字串，例如 "aes"、"rsa"。
        /// </summary>
        string EncryptionMethod { get; }

        /// <summary>
        /// 將原始位元組資料進行加密處理。
        /// </summary>
        /// <param name="data">原始位元組資料。</param>
        /// <returns>加密後的位元組資料。</returns>
        byte[] Encrypt(byte[] data);

        /// <summary>
        /// 將加密過的位元組資料還原為原始資料。
        /// </summary>
        /// <param name="data">加密後的位元組資料。</param>
        /// <returns>解密後的位元組資料。</returns>
        byte[] Decrypt(byte[] data);
    }

}
