using Bee.Base.Serialization;

namespace Bee.Api.Core.Transformer
{
    /// <summary>
    /// GZip-based API transport layer data compressor.
    /// </summary>
    public class GzipPayloadCompressor : IApiPayloadCompressor
    {
        /// <summary>
        /// Gets the identifier string for the compression algorithm.
        /// </summary>
        public string CompressionMethod => "gzip";

        /// <summary>
        /// Compresses the specified raw byte data.
        /// </summary>
        /// <param name="bytes">The raw byte data.</param>
        /// <returns>The compressed byte data.</returns>
        public byte[] Compress(byte[] bytes)
        {
            return GzipFunc.Compress(bytes);
        }

        /// <summary>
        /// Decompresses the specified compressed byte data back to its original form.
        /// </summary>
        /// <param name="bytes">The compressed byte data.</param>
        /// <returns>The decompressed byte data.</returns>
        public byte[] Decompress(byte[] bytes)
        {
            return GzipFunc.Decompress(bytes);
        }
    }
}
