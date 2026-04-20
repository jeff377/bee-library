using System.ComponentModel;
using System.Text;
using Bee.Api.Core.Transformer;

namespace Bee.Api.Core.UnitTests
{
    /// <summary>
    /// GzipPayloadCompressor 測試。
    /// </summary>
    public class GzipPayloadCompressorTests
    {
        [Fact]
        [DisplayName("CompressionMethod 應為 \"gzip\"")]
        public void CompressionMethod_IsGzip()
        {
            var compressor = new GzipPayloadCompressor();

            Assert.Equal("gzip", compressor.CompressionMethod);
        }

        [Fact]
        [DisplayName("Compress 後 Decompress 應還原原始內容")]
        public void CompressDecompress_RoundTrip_RestoresOriginalBytes()
        {
            var compressor = new GzipPayloadCompressor();
            var original = Encoding.UTF8.GetBytes(
                "The quick brown fox jumps over the lazy dog. " +
                "The quick brown fox jumps over the lazy dog. " +
                "The quick brown fox jumps over the lazy dog.");

            var compressed = compressor.Compress(original);
            var decompressed = compressor.Decompress(compressed);

            Assert.Equal(original, decompressed);
        }

        [Fact]
        [DisplayName("Compress 壓縮後長度應不同於原始內容")]
        public void Compress_ProducesDifferentBytes()
        {
            var compressor = new GzipPayloadCompressor();
            var original = Encoding.UTF8.GetBytes(new string('A', 1000));

            var compressed = compressor.Compress(original);

            Assert.NotEqual(original, compressed);
            Assert.True(compressed.Length < original.Length);
        }
    }
}
