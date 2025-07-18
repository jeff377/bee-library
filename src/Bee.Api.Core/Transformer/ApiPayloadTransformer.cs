using System;

namespace Bee.Api.Core
{
    /// <summary>
    /// 預設 API 傳輸資料的處理器，提供資料序列化、壓縮與加解密等轉換功能。
    /// </summary>
    public class ApiPayloadTransformer : IApiPayloadTransformer
    {
        /// <summary>
        /// 將指定的物件進行序列化與壓縮處理。
        /// </summary>
        /// <param name="payload">要處理的原始資料物件。</param>
        /// <param name="type">物件的型別。</param>
        /// <returns>處理後的資料（通常為位元組陣列）。</returns>
        public byte[] Encode(object payload, Type type)
        {
            if (payload == null)
            {
                throw new ArgumentNullException(nameof(payload), "Input data cannot be null.");
            }

            try
            {
                byte[] bytes = ApiServiceOptions.PayloadSerializer.Serialize(payload, type);  // 序列化
                return ApiServiceOptions.PayloadCompressor.Compress(bytes);                    // 壓縮
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("An error occurred during the data encoding process.", ex);
            }
        }

        /// <summary>
        /// 將序列化與壓縮後的資料還原為原始物件。
        /// </summary>
        /// <param name="payload">已處理的資料（通常為位元組陣列）。</param>
        /// <param name="type">目標物件型別。</param>
        /// <returns>還原後的原始資料物件。</returns>
        public object Decode(object payload, Type type)
        {
            if (payload == null)
            {
                throw new ArgumentNullException(nameof(payload), "Input data cannot be null.");
            }

            try
            {
                byte[] bytes = payload as byte[];
                if (bytes == null)
                {
                    throw new InvalidCastException("Invalid data type. The input data must be a byte array.");
                }

                byte[] decompressed = ApiServiceOptions.PayloadCompressor.Decompress(bytes);       // 解壓縮
                return ApiServiceOptions.PayloadSerializer.Deserialize(decompressed, type);        // 反序列化
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("An error occurred during the data decoding process.", ex);
            }
        }

        /// <summary>
        /// 僅對指定的位元組資料進行加密。
        /// </summary>
        /// <param name="rawBytes">原始位元組資料。</param>
        /// <param name="encryptionKey">加密金鑰。</param>
        /// <returns>加密後的資料。</returns>
        public byte[] Encrypt(byte[] rawBytes, byte[] encryptionKey)
        {
            if (rawBytes == null)
            {
                throw new ArgumentNullException(nameof(rawBytes), "Raw data cannot be null.");
            }

            return ApiServiceOptions.PayloadEncryptor.Encrypt(rawBytes, encryptionKey);
        }

        /// <summary>
        /// 僅對指定的位元組資料進行解密。
        /// </summary>
        /// <param name="encryptedBytes">加密後的資料。</param>
        /// <param name="encryptionKey">加密金鑰。</param>
        /// <returns>解密後的原始資料。</returns>
        public byte[] Decrypt(byte[] encryptedBytes, byte[] encryptionKey)
        {
            if (encryptedBytes == null)
            {
                throw new ArgumentNullException(nameof(encryptedBytes), "Encrypted data cannot be null.");
            }

            return ApiServiceOptions.PayloadEncryptor.Decrypt(encryptedBytes, encryptionKey);
        }
    }

}
