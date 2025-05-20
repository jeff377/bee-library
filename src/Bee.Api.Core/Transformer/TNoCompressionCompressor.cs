namespace Bee.Api.Core
{
    /// <summary>
    /// 不執行任何壓縮或解壓縮操作的壓縮器實作。
    /// </summary>
    public class TNoCompressionCompressor : IApiPayloadCompressor
    {
        /// <summary>
        /// 壓縮演算法的識別字串，none 表示不進行壓縮。
        /// </summary>
        public string CompressionMethod => "none";

        /// <summary>
        /// 傳回原始資料，未進行壓縮。
        /// </summary>
        public byte[] Compress(byte[] bytes)
        {
            return bytes;
        }

        /// <summary>
        /// 傳回原始資料，未進行解壓縮。
        /// </summary>
        public byte[] Decompress(byte[] bytes)
        {
            return bytes;
        }
    }

}
