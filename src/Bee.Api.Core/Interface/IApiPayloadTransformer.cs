using System;

namespace Bee.Api.Core
{
    /// <summary>
    /// 定義 API 傳輸資料的處理器介面，提供資料序列化、壓縮與加解密功能。
    /// </summary>
    public interface IApiPayloadTransformer
    {
        /// <summary>
        /// 將指定的物件進行序列化與壓縮處理。
        /// </summary>
        /// <param name="payload">要處理的原始資料物件。</param>
        /// <param name="type">物件的型別。</param>
        /// <returns>處理後的資料（通常為位元組陣列）。</returns>
        byte[] Encode(object payload, Type type);

        /// <summary>
        /// 將序列化與壓縮後的資料還原為原始物件。
        /// </summary>
        /// <param name="payload">已處理的資料（通常為位元組陣列）。</param>
        /// <param name="type">目標物件型別。</param>
        /// <returns>還原後的原始資料物件。</returns>
        object Decode(object payload, Type type);

        /// <summary>
        /// 僅對指定的位元組資料進行加密。
        /// </summary>
        /// <param name="rawBytes">原始位元組資料。</param>
        /// <param name="encryptionKey">加密金鑰。</param>
        /// <returns>加密後的資料。</returns>
        byte[] Encrypt(byte[] rawBytes, byte[] encryptionKey);

        /// <summary>
        /// 僅對指定的位元組資料進行解密。
        /// </summary>
        /// <param name="encryptedBytes">加密後的資料。</param>
        /// <param name="encryptionKey">加密金鑰。</param>
        /// <returns>解密後的原始資料。</returns>
        byte[] Decrypt(byte[] encryptedBytes, byte[] encryptionKey);
    }
}
