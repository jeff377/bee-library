using System.ComponentModel;
using Bee.Definition.Settings;

namespace Bee.Definition.UnitTests.Settings
{
    /// <summary>
    /// 簡易設定類別（WebsiteConfiguration、FrontendConfiguration 等）測試。
    /// </summary>
    public class SystemConfigurationTests
    {
        [Fact]
        [DisplayName("BackgroundServiceConfiguration.ToString 應回傳型別名稱")]
        public void BackgroundServiceConfiguration_ToString_ReturnsTypeName()
        {
            var config = new BackgroundServiceConfiguration();

            Assert.Equal(nameof(BackgroundServiceConfiguration), config.ToString());
        }

        [Fact]
        [DisplayName("FrontendConfiguration.ToString 應回傳型別名稱")]
        public void FrontendConfiguration_ToString_ReturnsTypeName()
        {
            var config = new FrontendConfiguration();

            Assert.Equal(nameof(FrontendConfiguration), config.ToString());
        }

        [Fact]
        [DisplayName("WebsiteConfiguration.ToString 應回傳型別名稱")]
        public void WebsiteConfiguration_ToString_ReturnsTypeName()
        {
            var config = new WebsiteConfiguration();

            Assert.Equal(nameof(WebsiteConfiguration), config.ToString());
        }

        [Fact]
        [DisplayName("BackendConfiguration.ToString 應回傳型別名稱")]
        public void BackendConfiguration_ToString_ReturnsTypeName()
        {
            var config = new BackendConfiguration();

            Assert.Equal(nameof(BackendConfiguration), config.ToString());
        }
    }
}
