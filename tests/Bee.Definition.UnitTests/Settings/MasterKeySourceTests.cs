using System.ComponentModel;
using Bee.Definition.Settings;

namespace Bee.Definition.UnitTests.Settings
{
    /// <summary>
    /// MasterKeySource 測試。
    /// </summary>
    public class MasterKeySourceTests
    {
        [Fact]
        [DisplayName("預設建構子應以 Type=File、Value=空字串 初始化")]
        public void DefaultConstructor_InitializesDefaults()
        {
            var source = new MasterKeySource();

            Assert.Equal(MasterKeySourceType.File, source.Type);
            Assert.Equal(string.Empty, source.Value);
        }

        [Fact]
        [DisplayName("屬性應可被設定並讀回")]
        public void Properties_AreSettable()
        {
            var source = new MasterKeySource
            {
                Type = MasterKeySourceType.Environment,
                Value = "BEE_MASTER_KEY"
            };

            Assert.Equal(MasterKeySourceType.Environment, source.Type);
            Assert.Equal("BEE_MASTER_KEY", source.Value);
        }

        [Theory]
        [InlineData(MasterKeySourceType.File, "File")]
        [InlineData(MasterKeySourceType.Environment, "Environment")]
        [DisplayName("ToString 應回傳 Type 的字串表示")]
        public void ToString_ReturnsTypeName(MasterKeySourceType type, string expected)
        {
            var source = new MasterKeySource { Type = type };

            Assert.Equal(expected, source.ToString());
        }
    }
}
