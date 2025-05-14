namespace Bee.Api.Core
{
    /// <summary>
    /// 定義 API 傳輸資料的處理器介面，提供資料加解密、序列化與壓縮等轉換功能。
    /// </summary>
    public interface IApiPayloadTransformer
    {
        /// <summary>
        /// 將指定的物件進行轉換處理，例如序列化、壓縮或加密。
        /// </summary>
        /// <param name="payload">要處理的原始資料物件。</param>
        /// <returns>處理後的資料（通常為位元組陣列形式）。</returns>
        object Encode(object payload);

        /// <summary>
        /// 將處理過的資料還原為原始物件，例如解密、解壓縮與反序列化。
        /// </summary>
        /// <param name="payload">已處理的資料（通常為位元組陣列形式）。</param>
        /// <returns>還原後的原始資料物件。</returns>
        object Decode(object payload);
    }

}
