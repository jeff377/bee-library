using System.ComponentModel;
using Bee.Api.Core.Transformers;
using Bee.Base;

namespace Bee.Api.Core.UnitTests
{
    /// <summary>
    /// ApiPayloadOptionsFactory 測試。
    /// </summary>
    public class ApiPayloadOptionsFactoryTests
    {
        [Fact]
        [DisplayName("CreateSerializer(\"messagepack\") 應回傳 MessagePackPayloadSerializer")]
        public void CreateSerializer_MessagePack_ReturnsMessagePackSerializer()
        {
            var serializer = ApiPayloadOptionsFactory.CreateSerializer("messagepack");

            Assert.IsType<MessagePackPayloadSerializer>(serializer);
        }

        [Fact]
        [DisplayName("CreateSerializer 傳入未支援名稱應拋出 NotSupportedException")]
        public void CreateSerializer_Unknown_Throws()
        {
            Assert.Throws<NotSupportedException>(() => ApiPayloadOptionsFactory.CreateSerializer("unknown"));
        }

        [Fact]
        [DisplayName("CreateCompressor(\"gzip\") 應回傳 GzipPayloadCompressor")]
        public void CreateCompressor_Gzip_ReturnsGzipCompressor()
        {
            var compressor = ApiPayloadOptionsFactory.CreateCompressor("gzip");

            Assert.IsType<GzipPayloadCompressor>(compressor);
        }

        [Theory]
        [InlineData("none")]
        [InlineData("")]
        [DisplayName("CreateCompressor(\"none\"/\"\") 應回傳 NoCompressionCompressor")]
        public void CreateCompressor_None_ReturnsNoCompressionCompressor(string name)
        {
            var compressor = ApiPayloadOptionsFactory.CreateCompressor(name);

            Assert.IsType<NoCompressionCompressor>(compressor);
        }

        [Fact]
        [DisplayName("CreateCompressor 傳入未支援名稱應拋出 NotSupportedException")]
        public void CreateCompressor_Unknown_Throws()
        {
            Assert.Throws<NotSupportedException>(() => ApiPayloadOptionsFactory.CreateCompressor("lzma"));
        }

        [Fact]
        [DisplayName("CreateEncryptor(\"aes-cbc-hmac\") 應回傳 AesPayloadEncryptor")]
        public void CreateEncryptor_AesCbcHmac_ReturnsAesPayloadEncryptor()
        {
            var encryptor = ApiPayloadOptionsFactory.CreateEncryptor("aes-cbc-hmac");

            Assert.IsType<AesPayloadEncryptor>(encryptor);
        }

        [Theory]
        [InlineData("none")]
        [InlineData("")]
        [DisplayName("CreateEncryptor(\"none\"/\"\") 於 Debug 模式下應回傳 NoEncryptionEncryptor")]
        public void CreateEncryptor_None_DebugMode_ReturnsNoEncryptionEncryptor(string name)
        {
            var originalDebugMode = SysInfo.IsDebugMode;
            try
            {
                SysInfo.IsDebugMode = true;

                var encryptor = ApiPayloadOptionsFactory.CreateEncryptor(name);

                Assert.IsType<NoEncryptionEncryptor>(encryptor);
            }
            finally
            {
                SysInfo.IsDebugMode = originalDebugMode;
            }
        }

        [Fact]
        [DisplayName("CreateEncryptor(\"none\") 於非 Debug 模式下應拋出 InvalidOperationException")]
        public void CreateEncryptor_None_ProductionMode_Throws()
        {
            var originalDebugMode = SysInfo.IsDebugMode;
            try
            {
                SysInfo.IsDebugMode = false;

                Assert.Throws<InvalidOperationException>(() => ApiPayloadOptionsFactory.CreateEncryptor("none"));
            }
            finally
            {
                SysInfo.IsDebugMode = originalDebugMode;
            }
        }

        [Fact]
        [DisplayName("CreateEncryptor 傳入未支援名稱應拋出 NotSupportedException")]
        public void CreateEncryptor_Unknown_Throws()
        {
            Assert.Throws<NotSupportedException>(() => ApiPayloadOptionsFactory.CreateEncryptor("rsa"));
        }
    }
}
