using System;
using Bee.Base;

namespace Bee.Api.Core
{
    /// <summary>
    /// 預設 API 傳輸資料的處理器，提供資料加解密、序列化與壓縮等轉換功能。
    /// </summary>
    public class TApiPayloadTransformer : IApiPayloadTransformer
    {
        /// <summary>
        /// 將指定的物件進行轉換處理，例如序列化、壓縮或加密。
        /// </summary>
        /// <param name="payload">要處理的原始資料物件。</param>
        /// <param name="type">物件的型別。</param>
        /// <returns>處理後的資料（通常為位元組陣列形式）。</returns>
        public object Encode(object payload, Type type)
        {
            if (payload == null)
            {
                throw new ArgumentNullException(nameof(payload), "Input data cannot be null.");
            }

            try
            {
                byte[] bytes = ApiServiceOptions.PayloadSerializer.Serialize(payload, type);  // 序列化
                byte[] compressedBytes = GZipFunc.Compress(bytes);  // 壓縮
                return CryptoFunc.AesEncrypt(compressedBytes);  // 加密
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("An error occurred during the data encoding process.", ex);
            }
        }

        /// <summary>
        /// 將處理過的資料還原為原始物件，例如解密、解壓縮與反序列化。
        /// </summary>
        /// <param name="payload">已處理的資料（通常為位元組陣列形式）。</param>
        /// <param name="type">反序列化後的物件型別。</param>
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

                byte[] decryptedBytes = CryptoFunc.AesDecrypt(bytes);  // 解密
                byte[] decompressedBytes = GZipFunc.Uncompress(decryptedBytes);  // 解壓縮
                return ApiServiceOptions.PayloadSerializer.Deserialize(decompressedBytes, type);  // 反序列化
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("An error occurred during the data decoding process.", ex);
            }
        }
    }
}
