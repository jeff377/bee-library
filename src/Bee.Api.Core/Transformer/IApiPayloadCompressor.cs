namespace Bee.Api.Core.Transformer
{
    /// <summary>
    /// API 傳輸層資料壓縮策略介面。
    /// 提供位元組資料的壓縮與還原功能，可用於縮小傳輸體積。
    /// </summary>
    public interface IApiPayloadCompressor
    {
        /// <summary>
        /// 壓縮演算法的識別字串，例如 "gzip"、"brotli"。
        /// </summary>
        string CompressionMethod { get; }

        /// <summary>
        /// 將原始位元組資料進行壓縮處理。
        /// </summary>
        /// <param name="bytes">原始位元組資料。</param>
        /// <returns>壓縮後的位元組資料。</returns>
        byte[] Compress(byte[] bytes);

        /// <summary>
        /// 將壓縮過的位元組資料還原為原始資料。
        /// </summary>
        /// <param name="bytes">壓縮後的位元組資料。</param>
        /// <returns>解壓縮後的位元組資料。</returns>
        byte[] Decompress(byte[] bytes);
    }
}
