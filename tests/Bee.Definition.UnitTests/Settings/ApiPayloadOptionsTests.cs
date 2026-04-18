using System.ComponentModel;
using Bee.Definition.Settings;

namespace Bee.Definition.UnitTests.Settings
{
    /// <summary>
    /// ApiPayloadOptions 單元測試。
    /// </summary>
    public class ApiPayloadOptionsTests
    {
        [Fact]
        [DisplayName("預設建構子應初始化為預設 Serializer/Compressor/Encryptor")]
        public void DefaultConstructor_InitializesDefaults()
        {
            var options = new ApiPayloadOptions();

            Assert.Equal("messagepack", options.Serializer);
            Assert.Equal("gzip", options.Compressor);
            Assert.Equal("aes-cbc-hmac", options.Encryptor);
        }

        [Fact]
        [DisplayName("屬性應可被設定並讀回")]
        public void Properties_AreSettable()
        {
            var options = new ApiPayloadOptions
            {
                Serializer = "json",
                Compressor = "none",
                Encryptor = "none"
            };

            Assert.Equal("json", options.Serializer);
            Assert.Equal("none", options.Compressor);
            Assert.Equal("none", options.Encryptor);
        }

        [Fact]
        [DisplayName("ToString 應回傳完整設定字串")]
        public void ToString_ReturnsFormatted()
        {
            var options = new ApiPayloadOptions
            {
                Serializer = "messagepack",
                Compressor = "gzip",
                Encryptor = "aes-cbc-hmac"
            };

            Assert.Equal(
                "Serializer: messagepack, Compressor: gzip, Encryptor: aes-cbc-hmac",
                options.ToString());
        }
    }
}
