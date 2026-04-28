using System.ComponentModel;
using Bee.Api.Core.Transformers;

namespace Bee.Api.Core.UnitTests
{
    /// <summary>
    /// NoCompressionCompressor 測試。
    /// </summary>
    public class NoCompressionCompressorTests
    {
        [Fact]
        [DisplayName("CompressionMethod 應為 \"none\"")]
        public void CompressionMethod_IsNone()
        {
            var compressor = new NoCompressionCompressor();

            Assert.Equal("none", compressor.CompressionMethod);
        }

        [Fact]
        [DisplayName("Compress 應回傳原始 byte 陣列")]
        public void Compress_ReturnsSameBytes()
        {
            var compressor = new NoCompressionCompressor();
            var data = new byte[] { 1, 2, 3, 4, 5 };

            var result = compressor.Compress(data);

            Assert.Same(data, result);
        }

        [Fact]
        [DisplayName("Decompress 應回傳原始 byte 陣列")]
        public void Decompress_ReturnsSameBytes()
        {
            var compressor = new NoCompressionCompressor();
            var data = new byte[] { 1, 2, 3, 4, 5 };

            var result = compressor.Decompress(data);

            Assert.Same(data, result);
        }
    }
}
