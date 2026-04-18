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
        [DisplayName("VersionFiles 預設值應為空字串")]
        public void VersionFiles_Default_HasEmptyProperties()
        {
            var versionFiles = new VersionFiles();

            Assert.Equal(string.Empty, versionFiles.Version);
            Assert.Equal(string.Empty, versionFiles.Files);
        }

        [Fact]
        [DisplayName("VersionFiles 屬性可設定與讀取")]
        public void VersionFiles_Properties_CanBeSet()
        {
            var versionFiles = new VersionFiles
            {
                Version = "4.0.1",
                Files = "a.dll;b.dll"
            };

            Assert.Equal("4.0.1", versionFiles.Version);
            Assert.Equal("a.dll;b.dll", versionFiles.Files);
        }
    }
}
