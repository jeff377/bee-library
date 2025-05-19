using Bee.Base;

namespace Bee.Api.Core
{
    /// <summary>
    /// 使用 GZip 的 API 傳輸層資料壓縮器。
    /// </summary>
    public class TGZipCompressor : IApiPayloadCompressor
    {
        /// <summary>
        /// 壓縮演算法的識別字串。
        /// </summary>
        public string CompressionMethod => "GZip";

        /// <summary>
        /// 將原始位元組資料進行壓縮處理。
        /// </summary>
        /// <param name="bytes">原始位元組資料。</param>
        /// <returns>壓縮後的位元組資料。</returns>
        public byte[] Compress(byte[] bytes)
        {
            return GZipFunc.Compress(bytes);
        }

        /// <summary>
        /// 將壓縮過的位元組資料還原為原始資料。
        /// </summary>
        /// <param name="bytes">壓縮後的位元組資料。</param>
        /// <returns>解壓縮後的位元組資料。</returns>
        public byte[] Decompress(byte[] bytes)
        {
            return GZipFunc.Decompress(bytes);
        }
    }
}
