namespace Bee.Api.Core.Transformers
{
    /// <summary>
    /// Interface for the API transport layer data compression strategy.
    /// Provides byte data compression and decompression to reduce transmission size.
    /// </summary>
    public interface IApiPayloadCompressor
    {
        /// <summary>
        /// Gets the identifier string for the compression algorithm, e.g., "gzip" or "brotli".
        /// </summary>
        string CompressionMethod { get; }

        /// <summary>
        /// Compresses the specified raw byte data.
        /// </summary>
        /// <param name="bytes">The raw byte data.</param>
        /// <returns>The compressed byte data.</returns>
        byte[] Compress(byte[] bytes);

        /// <summary>
        /// Decompresses the specified compressed byte data back to its original form.
        /// </summary>
        /// <param name="bytes">The compressed byte data.</param>
        /// <returns>The decompressed byte data.</returns>
        byte[] Decompress(byte[] bytes);
    }
}
