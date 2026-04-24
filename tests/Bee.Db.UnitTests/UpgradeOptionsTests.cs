using System.ComponentModel;
using Bee.Db.Schema;

namespace Bee.Db.UnitTests
{
    [Collection("Initialize")]
    public class UpgradeOptionsTests
    {
        [Fact]
        [DisplayName("預設 AllowColumnNarrowing 應為 false")]
        public void AllowColumnNarrowing_Default_IsFalse()
        {
            var options = new UpgradeOptions();

            Assert.False(options.AllowColumnNarrowing);
        }

        [Fact]
        [DisplayName("Default 靜態實例應回傳預設值")]
        public void Default_ReturnsInstanceWithDefaults()
        {
            var options = UpgradeOptions.Default;

            Assert.NotNull(options);
            Assert.False(options.AllowColumnNarrowing);
        }
    }
}
