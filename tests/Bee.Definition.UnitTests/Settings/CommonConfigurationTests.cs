using System.ComponentModel;
using Bee.Definition.Settings;

namespace Bee.Definition.UnitTests.Settings
{
    /// <summary>
    /// CommonConfiguration 單元測試。
    /// </summary>
    public class CommonConfigurationTests
    {
        [Fact]
        [DisplayName("預設建構子應初始化為預設值")]
        public void DefaultConstructor_InitializesDefaults()
        {
            var config = new CommonConfiguration();

            Assert.Equal(string.Empty, config.Version);
            Assert.False(config.IsDebugMode);
            Assert.Equal(string.Empty, config.AllowedTypeNamespaces);
            Assert.NotNull(config.ApiPayloadOptions);
            Assert.Equal("messagepack", config.ApiPayloadOptions.Serializer);
        }

        [Fact]
        [DisplayName("屬性應可被設定並讀回")]
        public void Properties_AreSettable()
        {
            var payload = new ApiPayloadOptions { Serializer = "json" };
            var config = new CommonConfiguration
            {
                Version = "4.0.1",
                IsDebugMode = true,
                AllowedTypeNamespaces = "Custom.Module|ThirdParty.Dto",
                ApiPayloadOptions = payload
            };

            Assert.Equal("4.0.1", config.Version);
            Assert.True(config.IsDebugMode);
            Assert.Equal("Custom.Module|ThirdParty.Dto", config.AllowedTypeNamespaces);
            Assert.Same(payload, config.ApiPayloadOptions);
        }

        [Fact]
        [DisplayName("ToString 應回傳型別名稱")]
        public void ToString_ReturnsTypeName()
        {
            var config = new CommonConfiguration();

            Assert.Equal(nameof(CommonConfiguration), config.ToString());
        }
    }
}
