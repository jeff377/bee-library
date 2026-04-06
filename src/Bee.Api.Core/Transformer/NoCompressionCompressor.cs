namespace Bee.Api.Core.Transformer
{
    /// <summary>
    /// A compressor implementation that performs no compression or decompression.
    /// </summary>
    public class NoCompressionCompressor : IApiPayloadCompressor
    {
        /// <summary>
        /// Gets the identifier string for the compression algorithm. "none" indicates no compression is applied.
        /// </summary>
        public string CompressionMethod => "none";

        /// <summary>
        /// Returns the original data unchanged; no compression is performed.
        /// </summary>
        public byte[] Compress(byte[] bytes)
        {
            return bytes;
        }

        /// <summary>
        /// Returns the original data unchanged; no decompression is performed.
        /// </summary>
        public byte[] Decompress(byte[] bytes)
        {
            return bytes;
        }
    }

}
