using Bee.Definition.Settings;

namespace Bee.Definition.UnitTests.Settings
{
    public class CacheNotifyOptionsTests
    {
        [Fact]
        [DisplayName("CacheNotifyOptions 預設值應符合規格（Enabled=true、IntervalSeconds=5、MarginSeconds=5、DatabaseId=common）")]
        public void DefaultValues_MatchExpectedDefaults()
        {
            var options = new CacheNotifyOptions();
            Assert.True(options.Enabled);
            Assert.Equal(5, options.IntervalSeconds);
            Assert.Equal(5, options.MarginSeconds);
            Assert.Equal("common", options.DatabaseId);
        }

        [Fact]
        [DisplayName("ToString 應回傳型別名稱 CacheNotifyOptions")]
        public void ToString_ReturnsTypeName()
        {
            var options = new CacheNotifyOptions();
            Assert.Equal("CacheNotifyOptions", options.ToString());
        }
    }
}
